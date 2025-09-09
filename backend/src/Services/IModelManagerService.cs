// Services/IModelManagerService.cs
using OllamaLlmApp.Backend.Models;

namespace OllamaLlmApp.Backend.Services
{
    public interface IModelManagerService
    {
        Task<ApiResponse<List<ModelInfo>>> ListModelsAsync();
        Task<ApiResponse<bool>> PullModelAsync(string modelName, IProgress<string>? progress = null);
        Task<ApiResponse<bool>> DeleteModelAsync(string modelName);
        Task<ApiResponse<ModelDetails>> GetModelDetailsAsync(string modelName);
        Task<ApiResponse<bool>> IsModelAvailableAsync(string modelName);
    }

    public class ModelDetails
    {
        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public List<string> Parameters { get; set; } = new();
        public DateTime ModifiedAt { get; set; }
    }
}