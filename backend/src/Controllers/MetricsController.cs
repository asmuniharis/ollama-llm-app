using Microsoft.AspNetCore.Mvc;
using OllamaLlmApp.Backend.Services;

namespace OllamaLlmApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetricsController : ControllerBase
    {
        private readonly IPerformanceMonitoringService _performanceMonitoring;
        private readonly ILogger<MetricsController> _logger;

        public MetricsController(
            IPerformanceMonitoringService performanceMonitoring,
            ILogger<MetricsController> logger)
        {
            _performanceMonitoring = performanceMonitoring;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMetrics()
        {
            try
            {
                var metrics = await _performanceMonitoring.GetMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving metrics");
                return StatusCode(500, new { Error = "Failed to retrieve metrics" });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var metrics = await _performanceMonitoring.GetMetricsAsync();
                
                var summary = new
                {
                    TotalRequests = metrics.ContainsKey("Requests") ? 
                        ((Dictionary<string, object>)metrics["Requests"]).Values.Sum(v => 
                        {
                            var requestMetric = v as dynamic;
                            return requestMetric?.Count ?? 0;
                        }) : 0,
                    TotalModelUsage = metrics.ContainsKey("ModelUsage") ? 
                        ((Dictionary<string, object>)metrics["ModelUsage"]).Values.Sum(v => Convert.ToInt64(v)) : 0,
                    SystemInfo = metrics.ContainsKey("System") ? metrics["System"] : null,
                    Timestamp = DateTime.UtcNow
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving metrics summary");
                return StatusCode(500, new { Error = "Failed to retrieve metrics summary" });
            }
        }
    }
}