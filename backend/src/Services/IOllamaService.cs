using OllamaLlmApp.Backend.Models;

namespace OllamaLlmApp.Backend.Services
{
    public interface IOllamaService
    {
        Task<ApiResponse<ChatResponse>> GenerateAsync(ChatRequest request);
        Task<ApiResponse<List<ModelInfo>>> GetAvailableModelsAsync();
        Task<ApiResponse<bool>> PullModelAsync(string modelName);
        Task<bool> IsHealthyAsync();
    }
}