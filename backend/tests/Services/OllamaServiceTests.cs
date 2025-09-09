using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OllamaLlmApp.Backend.Models;
using OllamaLlmApp.Backend.Services;
using System.Net;
using Xunit;

namespace OllamaLlmApp.Backend.Tests.Services
{
    public class OllamaServiceTests
    {
        private readonly Mock<ILogger<OllamaService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly OllamaService _ollamaService;

        public OllamaServiceTests()
        {
            _loggerMock = new Mock<ILogger<OllamaService>>();
            _configurationMock = new Mock<IConfiguration>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            
            _configurationMock.Setup(c => c.GetValue<string>("Ollama:BaseUrl", It.IsAny<string>()))
                             .Returns("http://localhost:11434");
            
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _ollamaService = new OllamaService(_httpClient, _loggerMock.Object, _configurationMock.Object);
        }

        [Fact]
        public async Task GenerateAsync_ValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new ChatRequest
            {
                Model = "llama2",
                Prompt = "Hello, world!",
                Stream = false
            };

            var expectedResponse = new ChatResponse
            {
                Model = "llama2",
                Response = "Hello! How can I help you today?",
                Done = true,
                CreatedAt = DateTime.UtcNow
            };

            // Mock HTTP response
            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(expectedResponse))
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _ollamaService.GenerateAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(expectedResponse.Response, result.Data.Response);
        }

        [Fact]
        public async Task IsHealthyAsync_OllamaAvailable_ReturnsTrue()
        {
            // Arrange
            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK);
            
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _ollamaService.IsHealthyAsync();

            // Assert
            Assert.True(result);
        }
    }
}