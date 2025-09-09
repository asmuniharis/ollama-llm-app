using Microsoft.AspNetCore.Mvc;
using OllamaLlmApp.Backend.Models;
using OllamaLlmApp.Backend.Services;

namespace OllamaLlmApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModelManagerController : ControllerBase
    {
        private readonly IModelManagerService _modelManager;
        private readonly ILogger<ModelManagerController> _logger;

        public ModelManagerController(
            IModelManagerService modelManager,
            ILogger<ModelManagerController> logger)
        {
            _modelManager = modelManager;
            _logger = logger;
        }

        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse<List<ModelInfo>>>> ListModels()
        {
            try
            {
                _logger.LogInformation("Listing all available models");
                var result = await _modelManager.ListModelsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing models");
                return StatusCode(500, new ApiResponse<List<ModelInfo>>
                {
                    Success = false,
                    Error = "Failed to list models"
                });
            }
        }

        [HttpPost("pull")]
        public async Task<ActionResult<ApiResponse<bool>>> PullModel([FromBody] PullModelRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ModelName))
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Error = "Model name is required"
                    });
                }

                _logger.LogInformation("Starting to pull model: {ModelName}", request.ModelName);
                
                var progress = new Progress<string>(status => 
                {
                    _logger.LogInformation("Pull progress for {ModelName}: {Status}", request.ModelName, status);
                });

                var result = await _modelManager.PullModelAsync(request.ModelName, progress);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pulling model: {ModelName}", request.ModelName);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Error = $"Failed to pull model: {request.ModelName}"
                });
            }
        }

        [HttpDelete("{modelName}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteModel(string modelName)
        {
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

                _logger.LogInformation("Deleting model: {ModelName}", modelName);
                var result = await _modelManager.DeleteModelAsync(modelName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting model: {ModelName}", modelName);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Error = $"Failed to delete model: {modelName}"
                });
            }
        }

        [HttpGet("{modelName}/details")]
        public async Task<ActionResult<ApiResponse<ModelDetails>>> GetModelDetails(string modelName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modelName))
                {
                    return BadRequest(new ApiResponse<ModelDetails>
                    {
                        Success = false,
                        Error = "Model name is required"
                    });
                }

                _logger.LogInformation("Getting details for model: {ModelName}", modelName);
                var result = await _modelManager.GetModelDetailsAsync(modelName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model details: {ModelName}", modelName);
                return StatusCode(500, new ApiResponse<ModelDetails>
                {
                    Success = false,
                    Error = $"Failed to get model details: {modelName}"
                });
            }
        }

        [HttpGet("{modelName}/available")]
        public async Task<ActionResult<ApiResponse<bool>>> IsModelAvailable(string modelName)
        {
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

                var result = await _modelManager.IsModelAvailableAsync(modelName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking model availability: {ModelName}", modelName);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Error = $"Failed to check model availability: {modelName}"
                });
            }
        }
    }

    public class PullModelRequest
    {
        public string ModelName { get; set; } = string.Empty;
    }
}