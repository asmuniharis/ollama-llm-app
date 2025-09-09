using OllamaLlmApp.Backend.Models;
using OllamaLlmApp.Backend.Services;
using Newtonsoft.Json;
using System.Text;

namespace OllamaLlmApp.Backend.Services
{
    public class ModelManagerService : IModelManagerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ModelManagerService> _logger;

        public ModelManagerService(HttpClient httpClient, ILogger<ModelManagerService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            var baseUrl = configuration.GetValue<string>("Ollama:BaseUrl") ?? "http://localhost:11434";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.Timeout = TimeSpan.FromMinutes(10); // Longer timeout for model operations
        }

        public async Task<ApiResponse<List<ModelInfo>>> ListModelsAsync()
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
                            var modelName = model.name?.ToString() ?? "Unknown";
                            var modelSize = FormatSize(model.size?.ToString());
                            
                            // Fix: Explicit DateTime conversion instead of TryParse with var
                            DateTime modifiedAt;
                            if (!DateTime.TryParse(model.modified_at?.ToString(), out modifiedAt))
                            {
                                modifiedAt = DateTime.Now;
                            }

                            models.Add(new ModelInfo
                            {
                                Name = modelName,
                                Size = modelSize,
                                ModifiedAt = modifiedAt
                            });
                        }
                    }
                    
                    return new ApiResponse<List<ModelInfo>>
                    {
                        Success = true,
                        Data = models.OrderBy(m => m.Name).ToList()
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
                _logger.LogError(ex, "Error listing models");
                return new ApiResponse<List<ModelInfo>>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResponse<bool>> PullModelAsync(string modelName, IProgress<string>? progress = null)
        {
            try
            {
                _logger.LogInformation("Starting to pull model: {ModelName}", modelName);
                progress?.Report($"Starting download of {modelName}...");

                var payload = new { name = modelName, stream = true };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync("/api/pull", content);
                
                if (response.IsSuccessStatusCode)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync();
                    using var reader = new StreamReader(stream);
                    
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        try
                        {
                            var progressData = JsonConvert.DeserializeObject<dynamic>(line);
                            var status = progressData?.status?.ToString();
                            
                            if (!string.IsNullOrEmpty(status))
                            {
                                progress?.Report(status);
                                // Fix: Cast to string explicitly instead of using dynamic
                                _logger.LogInformation("Pull progress: {Status}", (string)status);
                                
                                if (status.Contains("success"))
                                {
                                    progress?.Report($"Successfully downloaded {modelName}");
                                    return new ApiResponse<bool> { Success = true, Data = true };
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // Ignore malformed JSON lines
                            continue;
                        }
                    }
                    
                    return new ApiResponse<bool> { Success = true, Data = true };
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Error = $"Pull failed: {response.StatusCode} - {error}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pulling model: {ModelName}", modelName);
                progress?.Report($"Error downloading {modelName}: {ex.Message}");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteModelAsync(string modelName)
        {
            try
            {
                var payload = new { name = modelName };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/delete")
                {
                    Content = content
                });
                
                return new ApiResponse<bool>
                {
                    Success = response.IsSuccessStatusCode,
                    Data = response.IsSuccessStatusCode,
                    Error = response.IsSuccessStatusCode ? null : await response.Content.ReadAsStringAsync()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting model: {ModelName}", modelName);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResponse<ModelDetails>> GetModelDetailsAsync(string modelName)
        {
            try
            {
                var payload = new { name = modelName };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/show", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    
                    // Fix: Explicit DateTime conversion
                    DateTime modifiedAt;
                    if (!DateTime.TryParse(result?.modified_at?.ToString(), out modifiedAt))
                    {
                        modifiedAt = DateTime.Now;
                    }

                    var details = new ModelDetails
                    {
                        Name = modelName,
                        Size = FormatSize(result?.details?.size?.ToString()),
                        Format = result?.details?.format?.ToString() ?? "Unknown",
                        Family = result?.details?.family?.ToString() ?? "Unknown",
                        Parameters = ExtractParameters(result?.details?.parameters),
                        ModifiedAt = modifiedAt
                    };
                    
                    return new ApiResponse<ModelDetails>
                    {
                        Success = true,
                        Data = details
                    };
                }
                else
                {
                    return new ApiResponse<ModelDetails>
                    {
                        Success = false,
                        Error = $"Failed to get model details: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model details: {ModelName}", modelName);
                return new ApiResponse<ModelDetails>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResponse<bool>> IsModelAvailableAsync(string modelName)
        {
            var modelsResponse = await ListModelsAsync();
            if (modelsResponse.Success && modelsResponse.Data != null)
            {
                var isAvailable = modelsResponse.Data.Any(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = isAvailable
                };
            }
            
            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Error = modelsResponse.Error
            };
        }

        private static string FormatSize(string? sizeStr)
        {
            if (string.IsNullOrEmpty(sizeStr) || !long.TryParse(sizeStr, out var size))
                return "Unknown";
            
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        private static List<string> ExtractParameters(dynamic? parameters)
        {
            var result = new List<string>();
            
            if (parameters != null)
            {
                try
                {
                    // Safe iteration over dynamic object
                    foreach (var param in parameters)
                    {
                        var name = param.Name?.ToString() ?? "Unknown";
                        var value = param.Value?.ToString() ?? "Unknown";
                        result.Add($"{name}: {value}");
                    }
                }
                catch (Exception)
                {
                    // If iteration fails, return empty list
                    result.Clear();
                }
            }
            
            return result;
        }
    }
}