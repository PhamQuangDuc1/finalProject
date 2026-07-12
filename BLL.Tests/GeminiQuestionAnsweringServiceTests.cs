using System.Net;
using BLL.DTOs;
using BLL.Services;
using Microsoft.Extensions.Options;

namespace BLL.Tests;

public class GeminiQuestionAnsweringServiceTests
{
    [Fact]
    public async Task AnswerDocumentQuestionAsync_ReturnsText_FromGeminiResponse()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [
                          { "text": "AI learns patterns from data." }
                        ]
                      }
                    }
                  ],
                  "usageMetadata": {
                    "promptTokenCount": 8,
                    "candidatesTokenCount": 10,
                    "totalTokenCount": 18
                  },
                  "modelVersion": "gemini-3.5-flash"
                }
                """)
            }));
        var service = new GeminiQuestionAnsweringService(
            httpClient,
            Options.Create(new GeminiOptions { ApiKey = "test-key", Model = "gemini-flash-latest" }));
        var document = new DocumentDto
        {
            Title = "Intro AI",
            SubjectName = "AI",
            Chunks = new[]
            {
                new DocumentChunkDto { ChunkIndex = 1, Content = "AI systems learn patterns from examples." }
            }
        };

        var answer = await service.AnswerDocumentQuestionAsync(document, "AI works how?");

        Assert.Equal("AI learns patterns from data.", answer.Answer);
        Assert.Equal("gemini-3.5-flash", answer.ModelName);
        Assert.Equal(8, answer.PromptTokens);
        Assert.Equal(10, answer.CompletionTokens);
        Assert.Equal(18, answer.TotalTokens);
    }

    [Fact]
    public async Task AnswerDocumentQuestionAsync_Throws_WhenApiKeyMissing()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new GeminiQuestionAnsweringService(httpClient, Options.Create(new GeminiOptions()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AnswerDocumentQuestionAsync(new DocumentDto(), "What is this?"));

        Assert.Contains("Gemini:ApiKey", exception.Message);
    }

    [Fact]
    public async Task AnswerDocumentQuestionAsync_Throws_WhenQuestionIsBlank()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new GeminiQuestionAnsweringService(
            httpClient,
            Options.Create(new GeminiOptions { ApiKey = "test-key" }));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AnswerDocumentQuestionAsync(new DocumentDto(), "   "));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
