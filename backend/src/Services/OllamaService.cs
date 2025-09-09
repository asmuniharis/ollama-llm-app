using OllamaLlmApp.Backend.Models;
using OllamaLlmApp.Backend.Services;
using Newtonsoft.Json;
using System.Text;

namespace OllamaLlmApp.Backend.Services
{
    public class OllamaService : IOllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaService> _logger;
        private readonly string _baseUrl;

        public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration.GetValue<string>("Ollama:BaseUrl") ?? "http://localhost:11434";
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public async Task<ApiResponse<ChatResponse>> GenerateAsync(ChatRequest request)
        {
            try
            {
                _logger.LogInformation("Generating response for model: {Model}", request.Model);

                var payload = new
                {
                    model = request.Model,
                    prompt = request.Prompt,
                    stream = request.Stream,
                    options = request.Options != null ? new
                    {
                        temperature = request.Options.Temperature,
                        num_predict = request.Options.MaxTokens
                    } : null
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/generate", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var chatResponse = JsonConvert.DeserializeObject<ChatResponse>(responseContent);
                    
                    return new ApiResponse<ChatResponse>
                    {
                        Success = true,
                        Data = chatResponse
                    };
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Ollama API error: {Error}", error);
                    
                    return new ApiResponse<ChatResponse>
                    {
                        Success = false,
                        Error = $"API Error: {response.StatusCode} - {error}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating response");
                return new ApiResponse<ChatResponse>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResponse<List<ModelInfo>>> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tags");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(content);
                    
                    var models = new List<ModelInfo>();
                    if (result?.models != null)
                    {
                        foreach (var model in result.models)
                        {
                            models.Add(new ModelInfo
                            {
                                Name = model.name,
                                Size = model.size?.ToString() ?? "Unknown",
                                ModifiedAt = DateTime.Parse(model.modified_at?.ToString() ?? DateTime.Now.ToString())
                            });
                        }
                    }
                    
                    return new ApiResponse<List<ModelInfo>>
                    {
                        Success = true,
                        Data = models
                    };
                }
                else
                {
                    return new ApiResponse<List<ModelInfo>>
                    {
                        Success = false,
                        Error = $"Failed to fetch models: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available models");
                return new ApiResponse<List<ModelInfo>>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResponse<bool>> PullModelAsync(string modelName)
        {
            try
            {
                var payload = new { name = modelName };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/pull", content);
                
                return new ApiResponse<bool>
                {
                    Success = response.IsSuccessStatusCode,
                    Data = response.IsSuccessStatusCode,
                    Error = response.IsSuccessStatusCode ? null : await response.Content.ReadAsStringAsync()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pulling model: {ModelName}", modelName);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Error = ex.Message
                };
            }
        }

        // ADD THIS METHOD - it was missing!
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}