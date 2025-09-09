using System.Collections.Concurrent;
using System.Diagnostics;

namespace OllamaLlmApp.Backend.Services
{
    public class PerformanceMonitoringService : IPerformanceMonitoringService
    {
        private readonly ConcurrentDictionary<string, List<double>> _requestTimes = new();
        private readonly ConcurrentDictionary<string, long> _modelUsage = new();
        private readonly ILogger<PerformanceMonitoringService> _logger;

        public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
        {
            _logger = logger;
        }

        public void RecordRequestDuration(string endpoint, double durationMs)
        {
            _requestTimes.AddOrUpdate(endpoint, 
                new List<double> { durationMs },
                (key, existing) => 
                {
                    existing.Add(durationMs);
                    // Keep only last 1000 entries per endpoint
                    if (existing.Count > 1000)
                        existing.RemoveRange(0, existing.Count - 1000);
                    return existing;
                });

            _logger.LogInformation("Request to {Endpoint} took {Duration}ms", endpoint, durationMs);
        }

        public void RecordModelUsage(string modelName, int tokenCount)
        {
            _modelUsage.AddOrUpdate(modelName, tokenCount, (key, existing) => existing + tokenCount);
            _logger.LogInformation("Model {ModelName} used {TokenCount} tokens", modelName, tokenCount);
        }

        public Task<Dictionary<string, object>> GetMetricsAsync()
        {
            var metrics = new Dictionary<string, object>();

            // Request performance metrics
            var requestMetrics = new Dictionary<string, object>();
            foreach (var kvp in _requestTimes)
            {
                var times = kvp.Value;
                if (times.Any())
                {
                    requestMetrics[kvp.Key] = new
                    {
                        Count = times.Count,
                        AverageMs = times.Average(),
                        MinMs = times.Min(),
                        MaxMs = times.Max(),
                        P95Ms = times.OrderBy(t => t).Skip((int)(times.Count * 0.95)).FirstOrDefault()
                    };
                }
            }
            metrics["Requests"] = requestMetrics;

            // Model usage metrics
            metrics["ModelUsage"] = _modelUsage.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value
            );

            // System metrics
            var process = Process.GetCurrentProcess();
            metrics["System"] = new
            {
                MemoryUsageMB = process.WorkingSet64 / (1024 * 1024),
                CpuTime = process.TotalProcessorTime.TotalMilliseconds,
                ThreadCount = process.Threads.Count,
                Uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime()
            };

            return Task.FromResult(metrics);
        }
    }
}