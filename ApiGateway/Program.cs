using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

// Add logging to console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add YARP reverse proxy
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
        }
    },
    new[]
    {
        new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "products_cluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
            {
                ["products_dest"] = new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://localhost:5001/" }
            }
        },
        new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "order_cluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
            {
                ["order_dest"] = new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://localhost:5002/" }
            }
        }
    });

var app = builder.Build();

app.MapReverseProxy();

app.Run();
