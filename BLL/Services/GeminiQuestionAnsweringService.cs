using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.Extensions.Options;

namespace BLL.Services;

public class GeminiQuestionAnsweringService : IAiQuestionAnsweringService
{
    private const int MaxContextCharacters = 12000;
    private const int MaxCitationCount = 5;
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
        return await AnswerDocumentsQuestionAsync(new[] { document }, question, cancellationToken);
    }

    public async Task<AiQuestionAnswerDto> AnswerDocumentsQuestionAsync(
        IReadOnlyList<DocumentDto> documents,
        string question,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question is required.", nameof(question));
        }

        if (documents.Count == 0)
        {
            throw new InvalidOperationException("Chưa có tài liệu sẵn sàng để hỏi AI.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Gemini:ApiKey is not configured.");
        }

        var selectedChunks = SelectRelevantChunks(documents, question.Trim());
        if (selectedChunks.Count == 0)
        {
            throw new InvalidOperationException("Chưa có nội dung chunk sẵn sàng để hỏi AI.");
        }

        var request = new GeminiGenerateContentRequest(new[]
        {
            new GeminiContent(new[]
            {
                new GeminiPart(BuildPrompt(selectedChunks, question.Trim()))
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
            Citations = selectedChunks
                .Take(MaxCitationCount)
                .Select(chunk => new AiAnswerCitationDto
                {
                    DocumentId = chunk.Document.Id,
                    DocumentTitle = chunk.Document.Title,
                    SubjectName = chunk.Document.SubjectName,
                    ChunkIndex = chunk.Chunk.ChunkIndex,
                    Excerpt = BuildExcerpt(chunk.Chunk.Content)
                })
                .ToList(),
            ModelName = string.IsNullOrWhiteSpace(geminiResponse.ModelVersion)
                ? _options.Model
                : geminiResponse.ModelVersion,
            PromptTokens = geminiResponse.UsageMetadata?.PromptTokenCount ?? 0,
            CompletionTokens = geminiResponse.UsageMetadata?.CandidatesTokenCount ?? 0,
            TotalTokens = geminiResponse.UsageMetadata?.TotalTokenCount ?? 0
        };
    }

    private static string BuildPrompt(IReadOnlyList<RelevantChunk> chunks, string question)
    {
        var context = BuildContext(chunks);

        return $"""
        Bạn là StudyMate AI. Hãy trả lời câu hỏi của sinh viên bằng tiếng Việt có dấu.
        Chỉ sử dụng phần ngữ cảnh tài liệu bên dưới. Nếu ngữ cảnh không đủ thông tin, hãy nói rõ rằng bạn chưa có đủ thông tin.
        Khi câu trả lời dựa trên tài liệu, hãy nhắc đến mã trích dẫn dạng [Tài liệu <id> - Chunk <index>] phù hợp.

        Ngữ cảnh:
        {context}

        Câu hỏi:
        {question}
        """;
    }

    private static string BuildContext(IReadOnlyList<RelevantChunk> chunks)
    {
        var builder = new StringBuilder();
        foreach (var item in chunks)
        {
            if (builder.Length >= MaxContextCharacters)
            {
                break;
            }

            builder.AppendLine($"[Tài liệu {item.Document.Id} - Chunk {item.Chunk.ChunkIndex}]");
            builder.AppendLine($"Tiêu đề: {item.Document.Title}");
            builder.AppendLine($"Môn học: {item.Document.SubjectName}");
            if (!string.IsNullOrWhiteSpace(item.Document.ChapterName))
            {
                builder.AppendLine($"Chương: {item.Document.ChapterName}");
            }

            builder.AppendLine(item.Chunk.Content);
            builder.AppendLine();
        }

        var context = builder.ToString();
        return context.Length <= MaxContextCharacters
            ? context
            : context[..MaxContextCharacters];
    }

    private static IReadOnlyList<RelevantChunk> SelectRelevantChunks(IReadOnlyList<DocumentDto> documents, string question)
    {
        var terms = Tokenize(question).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var chunks = documents
            .SelectMany(document => document.Chunks.Select(chunk => new RelevantChunk(
                document,
                chunk,
                ScoreChunk(document, chunk, terms))))
            .Where(item => !string.IsNullOrWhiteSpace(item.Chunk.Content))
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Document.UpdatedAtUtc ?? item.Document.UploadedAtUtc)
            .ThenBy(item => item.Document.Title)
            .ThenBy(item => item.Chunk.ChunkIndex)
            .Take(MaxCitationCount)
            .ToList();

        return chunks;
    }

    private static int ScoreChunk(DocumentDto document, DocumentChunkDto chunk, IReadOnlySet<string> terms)
    {
        if (terms.Count == 0)
        {
            return 0;
        }

        var searchableText = $"{document.Title} {document.SubjectName} {document.ChapterName} {chunk.Content}";
        var normalizedText = RemoveVietnameseTone(searchableText).ToLowerInvariant();

        return terms.Count(term => normalizedText.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return Regex.Matches(RemoveVietnameseTone(text).ToLowerInvariant(), @"[\p{L}\p{N}]+")
            .Select(match => match.Value)
            .Where(term => term.Length >= 3);
    }

    private static string BuildExcerpt(string content)
    {
        var normalized = Regex.Replace(content, @"\s+", " ").Trim();

        return normalized.Length <= 240
            ? normalized
            : $"{normalized[..240]}...";
    }

    private static string RemoveVietnameseTone(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
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

    private sealed record RelevantChunk(DocumentDto Document, DocumentChunkDto Chunk, int Score);
}
