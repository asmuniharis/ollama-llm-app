using Microsoft.AspNetCore.Mvc;
using OllamaLlmApp.Backend.Services;

namespace OllamaLlmApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IOllamaService _ollamaService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(IOllamaService ollamaService, ILogger<HealthController> logger)
        {
            _ollamaService = ollamaService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Check()
        {
            try
            {
                var isHealthy = await _ollamaService.IsHealthyAsync();
                
                var status = new
                {
                    Status = isHealthy ? "Healthy" : "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Services = new
                    {
                        Ollama = isHealthy ? "Connected" : "Disconnected"
                    },
                    Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown"
                };

                if (isHealthy)
                {
                    _logger.LogInformation("Health check passed");
                    return Ok(status);
                }
                else
                {
                    _logger.LogWarning("Health check failed - Ollama service unavailable");
                    return StatusCode(503, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check encountered an error");
                
                var errorStatus = new
                {
                    Status = "Error",
                    Timestamp = DateTime.UtcNow,
                    Error = ex.Message,
                    Services = new
                    {
                        Ollama = "Error"
                    }
                };

                return StatusCode(503, errorStatus);
            }
        }

        [HttpGet("detailed")]
        public async Task<IActionResult> DetailedCheck()
        {
            try
            {
                var ollamaHealthy = await _ollamaService.IsHealthyAsync();
                var modelsResult = await _ollamaService.GetAvailableModelsAsync();
                
                var detailedStatus = new
                {
                    Status = ollamaHealthy ? "Healthy" : "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Services = new
                    {
                        Ollama = new
                        {
                            Status = ollamaHealthy ? "Connected" : "Disconnected",
                            ModelsAvailable = modelsResult.Success ? modelsResult.Data?.Count ?? 0 : 0,
                            LastChecked = DateTime.UtcNow
                        }
                    },
                    System = new
                    {
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                        MachineName = Environment.MachineName,
                        ProcessorCount = Environment.ProcessorCount,
                        WorkingSet = GC.GetTotalMemory(false) / (1024 * 1024) // MB
                    }
                };

                return ollamaHealthy ? Ok(detailedStatus) : StatusCode(503, detailedStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed health check failed");
                return StatusCode(500, new { Status = "Error", Error = ex.Message });
            }
        }
    }
}