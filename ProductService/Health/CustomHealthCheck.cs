using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace ProductService.Health
{
    public class CustomHealthCheck : IHealthCheck
    {
        // In a real app, this might be a service that reports the status
        // of a critical background task or an important configuration.
        private bool _isApplicationHealthy = true;

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (_isApplicationHealthy)
            {
                // You can provide useful data in the response
                var data = new Dictionary<string, object>
                {
                    { "Reason", "Everything is running smoothly." }
                };
                return Task.FromResult(
                    HealthCheckResult.Healthy("A custom check for application state is healthy.", data));
            }

            return Task.FromResult(
                new HealthCheckResult(
                    context.Registration.FailureStatus,
                    "A custom check for application state is unhealthy."));
        }
    }
} 