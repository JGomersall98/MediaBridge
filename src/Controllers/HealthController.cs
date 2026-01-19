using MediaBridge.Models.Health;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MediaBridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var healthReport = await _healthCheckService.CheckHealthAsync();

                var response = new HealthCheckResponse
                {
                    Status = healthReport.Status.ToString(),
                    TotalDurationMs = Math.Round(healthReport.TotalDuration.TotalMilliseconds, 2)
                };

                return healthReport.Status == HealthStatus.Healthy
                    ? Ok(response)
                    : StatusCode(503, response);
            }
            catch (Exception)
            {
                var errorResponse = new HealthCheckResponse
                {
                    Status = "Unhealthy",
                    TotalDurationMs = 0
                };

                return StatusCode(503, errorResponse);
            }
        }
    }
}