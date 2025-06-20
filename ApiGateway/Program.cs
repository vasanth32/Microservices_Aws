using Yarp.ReverseProxy;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Polly.CircuitBreaker;
using System.Net;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add logging to console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configure Polly policies
var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));

var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger<Program>();

var retry = HttpPolicyExtensions
    .HandleTransientHttpError()
    .Or<TimeoutRejectedException>()
    .WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (result, timeSpan, retryCount, context) =>
        {
            var exception = result.Exception;
            logger.LogWarning("Request failed with {ExceptionType}. Retry attempt {RetryCount} after {TimeSpan}s",
                exception?.GetType().Name ?? "Unknown", retryCount, timeSpan.TotalSeconds);
        });

var circuitBreaker = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (exception, duration) =>
        {
            logger.LogWarning("Circuit breaker opened for {DurationSeconds}s due to: {Exception}",
                duration.TotalSeconds, exception.Exception.Message);
        },
        onReset: () =>
        {
            logger.LogInformation("Circuit breaker reset");
        });

// Combine policies
var resilientPolicy = Policy.WrapAsync(retry, circuitBreaker, timeout);

// Add HTTP client with resilience policies
builder.Services.AddHttpClient("ResilientClient")
    .AddPolicyHandler(resilientPolicy);

// Add YARP reverse proxy with resilience policies
builder.Services.AddReverseProxy()
    .LoadFromMemory(new[]
    {
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "products_route",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch { Path = "/products/{**catchall}" },
            ClusterId = "products_cluster"
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "order_route",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch { Path = "/order/{**catchall}" },
            ClusterId = "order_cluster"
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "products_test_route",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch { Path = "/test/products/{**catchall}" },
            ClusterId = "products_cluster"
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "order_test_route",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch { Path = "/test/orders/{**catchall}" },
            ClusterId = "order_cluster"
        }
    },
    new[]
    {
        new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "products_cluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
            {
                ["products_dest"] = new Yarp.ReverseProxy.Configuration.DestinationConfig 
                { 
                    Address = "http://localhost:5001/",
                    Health = "http://localhost:5001/health"
                }
            }
        },
        new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "order_cluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
            {
                ["order_dest"] = new Yarp.ReverseProxy.Configuration.DestinationConfig 
                { 
                    Address = "http://localhost:5002/",
                    Health = "http://localhost:5002/health"
                }
            }
        }
    });

var app = builder.Build();

// Use error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An unhandled exception occurred");
        
        context.Response.ContentType = "application/json";
        
        switch (ex)
        {
            case TimeoutRejectedException:
                context.Response.StatusCode = (int)HttpStatusCode.GatewayTimeout;
                await context.Response.WriteAsJsonAsync(new { error = "Request timed out" });
                break;
            case BrokenCircuitException:
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { error = "Service is currently unavailable. Please try again later" });
                break;
            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred" });
                break;
        }
    }
});

app.MapReverseProxy();

app.Run();
