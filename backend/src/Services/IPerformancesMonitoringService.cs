namespace OllamaLlmApp.Backend.Services
{
    public interface IPerformanceMonitoringService
    {
        void RecordRequestDuration(string endpoint, double durationMs);
        void RecordModelUsage(string modelName, int tokenCount);
        Task<Dictionary<string, object>> GetMetricsAsync();
    }
}