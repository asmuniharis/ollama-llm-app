// Services/IConversationService.cs
using OllamaLlmApp.Backend.Models;

namespace OllamaLlmApp.Backend.Services
{
    public interface IConversationService
    {
        Task<string> CreateConversationAsync(string userId);
        Task<ApiResponse<bool>> AddMessageAsync(string conversationId, string message, bool isUser);
        Task<ApiResponse<List<ConversationMessage>>> GetConversationHistoryAsync(string conversationId);
        Task<ApiResponse<bool>> ClearConversationAsync(string conversationId);
        Task<string> BuildContextPromptAsync(string conversationId, string newMessage, int maxTokens = 2000);
    }

    public class ConversationMessage
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; }
        public string Model { get; set; } = string.Empty;
    }
}
