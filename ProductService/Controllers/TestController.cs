using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ProductService.Controllers;

[ApiController]
[Route("test/products")]
public class TestController : ControllerBase
{
    private static int _requestCount = 0;
    private readonly ILogger<TestController> _logger;
    private static readonly Random _random = new Random();

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    [HttpGet("timeout")]
    public async Task<IActionResult> SimulateTimeout()
    {
        _logger.LogInformation("Simulating a timeout...");
        await Task.Delay(15000); // 15 seconds delay, longer than our 10-second timeout
        return Ok("This should never be returned due to timeout");
    }

    [HttpGet("circuit")]
    public IActionResult SimulateCircuitBreaker()
    {
        _requestCount++;
        _logger.LogInformation("Circuit breaker test request #{Count}", _requestCount);

        if (_requestCount <= 5) // First 5 requests will fail
        {
            _logger.LogWarning("Simulating failure for request #{Count}", _requestCount);
            return StatusCode((int)HttpStatusCode.ServiceUnavailable, "Simulated failure");
        }

        _requestCount = 0; // Reset counter after 5 requests
        return Ok("Success! Circuit breaker should be open now");
    }

    [HttpGet("random")]
    public IActionResult SimulateRandomFailure()
    {
        var shouldFail = _random.NextDouble() < 0.5; // 50% chance of failure

        if (shouldFail)
        {
            _logger.LogWarning("Simulating a random transient failure");
            return StatusCode((int)HttpStatusCode.ServiceUnavailable, "Random transient failure");
        }

        return Ok("Success! This request worked");
    }

    [HttpGet("slow")]
    public async Task<IActionResult> SimulateSlowResponse()
    {
        var delay = _random.Next(1000, 12000); // Random delay between 1 and 12 seconds
        _logger.LogInformation("Simulating slow response with {Delay}ms delay", delay);
        
        await Task.Delay(delay);
        return Ok($"Response after {delay}ms delay");
    }

    [HttpGet("reset")]
    public IActionResult ResetCounters()
    {
        _requestCount = 0;
        return Ok("Counters reset successfully");
    }
} 