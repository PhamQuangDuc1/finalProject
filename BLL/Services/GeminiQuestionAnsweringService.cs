using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.Extensions.Options;

namespace BLL.Services;

public class GeminiQuestionAnsweringService : IAiQuestionAnsweringService
{
    private const int MaxContextCharacters = 12000;
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public GeminiQuestionAnsweringService(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<AiQuestionAnswerDto> AnswerDocumentQuestionAsync(
        DocumentDto document,
        string question,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question is required.", nameof(question));
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Gemini:ApiKey is not configured.");
        }

        var request = new GeminiGenerateContentRequest(new[]
        {
            new GeminiContent(new[]
            {
                new GeminiPart(BuildPrompt(document, question.Trim()))
            })
        });
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-goog-api-key", _options.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini request failed with status {(int)response.StatusCode}: {responseBody}");
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (geminiResponse is null)
        {
            throw new InvalidOperationException("Gemini did not return a valid response.");
        }

        var answer = geminiResponse.Candidates?
            .SelectMany(candidate => candidate.Content?.Parts ?? Array.Empty<GeminiPart>())
            .Select(part => part.Text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException("Gemini did not return an answer.");
        }

        return new AiQuestionAnswerDto
        {
            Question = question.Trim(),
            Answer = answer.Trim(),
            ModelName = string.IsNullOrWhiteSpace(geminiResponse.ModelVersion)
                ? _options.Model
                : geminiResponse.ModelVersion,
            PromptTokens = geminiResponse.UsageMetadata?.PromptTokenCount ?? 0,
            CompletionTokens = geminiResponse.UsageMetadata?.CandidatesTokenCount ?? 0,
            TotalTokens = geminiResponse.UsageMetadata?.TotalTokenCount ?? 0
        };
    }

    private static string BuildPrompt(DocumentDto document, string question)
    {
        var context = BuildContext(document);

        return $"""
        You are StudyMate AI. Answer the student's question in Vietnamese.
        Use only the document context below. If the answer is not in the context, say you do not have enough information.

        Document title: {document.Title}
        Subject: {document.SubjectName}
        Chapter: {document.ChapterName}

        Context:
        {context}

        Question:
        {question}
        """;
    }

    private static string BuildContext(DocumentDto document)
    {
        var builder = new StringBuilder();
        foreach (var chunk in document.Chunks.OrderBy(chunk => chunk.ChunkIndex))
        {
            if (builder.Length >= MaxContextCharacters)
            {
                break;
            }

            builder.AppendLine($"Chunk #{chunk.ChunkIndex}:");
            builder.AppendLine(chunk.Content);
            builder.AppendLine();
        }

        var context = builder.ToString();
        return context.Length <= MaxContextCharacters
            ? context
            : context[..MaxContextCharacters];
    }

    private sealed record GeminiGenerateContentRequest(IReadOnlyList<GeminiContent> Contents);

    private sealed record GeminiGenerateContentResponse(
        IReadOnlyList<GeminiCandidate>? Candidates,
        GeminiUsageMetadata? UsageMetadata,
        string? ModelVersion);

    private sealed record GeminiUsageMetadata(
        int PromptTokenCount,
        int CandidatesTokenCount,
        int TotalTokenCount);

    private sealed record GeminiCandidate(GeminiContent? Content);

    private sealed record GeminiContent(IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart(string Text);
}
