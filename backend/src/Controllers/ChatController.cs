using Microsoft.AspNetCore.Mvc;
using OllamaLlmApp.Backend.Models;
using OllamaLlmApp.Backend.Services;

namespace OllamaLlmApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IOllamaService _ollamaService;
        private readonly ILogger<ChatController> _logger;
        private readonly IPerformanceMonitoringService _performanceMonitoring;

        public ChatController(
            IOllamaService ollamaService, 
            ILogger<ChatController> logger,
            IPerformanceMonitoringService performanceMonitoring)
        {
            _ollamaService = ollamaService;
            _logger = logger;
            _performanceMonitoring = performanceMonitoring;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<ApiResponse<ChatResponse>>> Generate([FromBody] ChatRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                if (string.IsNullOrWhiteSpace(request.Prompt))
                {
                    return BadRequest(new ApiResponse<ChatResponse>
                    {
                        Success = false,
                        Error = "Prompt is required"
                    });
                }

                _logger.LogInformation("Processing chat request for model: {Model}", request.Model);

                var result = await _ollamaService.GenerateAsync(request);

                // Record performance metrics
                stopwatch.Stop();
                _performanceMonitoring.RecordRequestDuration("chat/generate", stopwatch.ElapsedMilliseconds);
                
                if (result.Success && result.Data != null)
                {
                    // Estimate token count (rough approximation)
                    var tokenCount = EstimateTokenCount(result.Data.Response);
                    _performanceMonitoring.RecordModelUsage(request.Model, tokenCount);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error processing chat request");
                
                return StatusCode(500, new ApiResponse<ChatResponse>
                {
                    Success = false,
                    Error = "Internal server error occurred"
                });
            }
        }

        [HttpGet("models")]
        public async Task<ActionResult<ApiResponse<List<ModelInfo>>>> GetModels()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("Fetching available models");

                var result = await _ollamaService.GetAvailableModelsAsync();
                
                stopwatch.Stop();
                _performanceMonitoring.RecordRequestDuration("chat/models", stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error fetching models");
                
                return StatusCode(500, new ApiResponse<List<ModelInfo>>
                {
                    Success = false,
                    Error = "Failed to fetch models"
                });
            }
        }

        [HttpPost("pull-model")]
        public async Task<ActionResult<ApiResponse<bool>>> PullModel([FromBody] string modelName)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                if (string.IsNullOrWhiteSpace(modelName))
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Error = "Model name is required"
                    });
                }

                _logger.LogInformation("Pulling model: {ModelName}", modelName);

                var result = await _ollamaService.PullModelAsync(modelName);
                
                stopwatch.Stop();
                _performanceMonitoring.RecordRequestDuration("chat/pull-model", stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error pulling model: {ModelName}", modelName);
                
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Error = $"Failed to pull model: {modelName}"
                });
            }
        }

        private static int EstimateTokenCount(string text)
        {
            // Rough estimation: 1 token ≈ 4 characters for English text
            return Math.Max(1, text.Length / 4);
        }
    }
}