// File: Controllers/HealthController.cs
using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.Data;

namespace unipos_basic_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class HealthController (PostgresDb postgresDb, ILogger<HealthController> logger) : ControllerBase
    {
        private readonly PostgresDb _postgresDb = postgresDb;
        private readonly ILogger<HealthController> _logger = logger;

        /// <summary>
        /// Checks if the application can connect to PostgreSQL.
        /// Returns 200 if healthy, 503 if database is unreachable.
        /// </summary>
        [HttpGet("v1/database")]
        [ProducesResponseType(typeof(HealthResponse), 200)]
        [ProducesResponseType(typeof(HealthResponse), 503)]
        public async Task<IActionResult> CheckDatabase([FromQuery] int timeoutMs = 5000)
        {
            var cts = new CancellationTokenSource(timeoutMs);
            try
            {
                var isHealthy = await _postgresDb.IsHealthyAsync(cts.Token);

                var response = new HealthResponse
                {
                    Status = isHealthy ? "Healthy" : "Unhealthy",
                    CheckedAt = DateTime.UtcNow
                };

                return isHealthy
                    ? Ok(response)
                    : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
            }
            catch (OperationCanceledException) when (!cts.Token.IsCancellationRequested)
            {
                _logger.LogWarning("Database health check timed out after {Timeout}ms.", timeoutMs);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new HealthResponse
                {
                    Status = "Timeout",
                    CheckedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during database health check.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new HealthResponse
                {
                    Status = "Error",
                    CheckedAt = DateTime.UtcNow
                });
            }
        }

        /// Simple ping endpoint (always returns 200)
        [HttpGet("v1/ping")]
        public IActionResult Ping() => Ok(new { message = "pong", time = DateTime.UtcNow });
    }

    public class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
    }
}