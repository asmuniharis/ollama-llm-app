namespace OllamaLlmApp.Backend.Models
{
    public class ChatRequest
    {
        public string Model { get; set; } = "llama2";
        public string Prompt { get; set; } = string.Empty;
        public bool Stream { get; set; } = false;
        public ChatOptions? Options { get; set; }
    }

    public class ChatOptions
    {
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 1000;
    }

    public class ChatResponse
    {
        public string Model { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public bool Done { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ModelInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public DateTime ModifiedAt { get; set; }
    }
}