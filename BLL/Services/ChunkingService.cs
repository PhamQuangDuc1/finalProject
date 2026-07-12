using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;

namespace BLL.Services;

public class ChunkingService : IChunkingService
{
    private readonly ISystemSettingService _systemSettingService;

    public ChunkingService(ISystemSettingService systemSettingService)
    {
        _systemSettingService = systemSettingService;
    }

    public Task<SystemSettingDto> GetCurrentChunkSettingAsync(CancellationToken cancellationToken = default)
    {
        return _systemSettingService.GetCurrentAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunk>> CreateChunksAsync(
        int documentId,
        string text,
        CancellationToken cancellationToken = default)
    {
        var setting = await _systemSettingService.GetCurrentAsync(cancellationToken);

        return setting.ChunkStrategy switch
        {
            ChunkStrategy.Paragraph => CreateParagraphChunks(documentId, text, setting),
            ChunkStrategy.Characters => CreateCharacterChunks(documentId, text, setting),
            ChunkStrategy.Words => CreateWordChunks(documentId, text, setting),
            ChunkStrategy.FixedSize => CreateWordChunks(documentId, text, setting),
            _ => throw new InvalidOperationException("Chunk strategy is not valid.")
        };
    }

    private static IReadOnlyList<DocumentChunk> CreateWordChunks(int documentId, string text, SystemSettingDto setting)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunkSize = Math.Max(1, setting.ChunkSize);
        var overlap = Math.Clamp(setting.ChunkOverlap, 0, Math.Max(0, chunkSize - 1));
        var step = Math.Max(1, chunkSize - overlap);
        var chunks = new List<DocumentChunk>();

        for (var start = 0; start < words.Length; start += step)
        {
            var chunkWords = words.Skip(start).Take(chunkSize).ToArray();
            if (chunkWords.Length == 0)
            {
                break;
            }

            chunks.Add(CreateChunk(documentId, chunks.Count, string.Join(' ', chunkWords), start, start + chunkWords.Length));

            if (start + chunkWords.Length >= words.Length)
            {
                break;
            }
        }

        return chunks;
    }

    private static IReadOnlyList<DocumentChunk> CreateCharacterChunks(int documentId, string text, SystemSettingDto setting)
    {
        var chunkSize = Math.Max(1, setting.ChunkSize);
        var overlap = Math.Clamp(setting.ChunkOverlap, 0, Math.Max(0, chunkSize - 1));
        var step = Math.Max(1, chunkSize - overlap);
        var chunks = new List<DocumentChunk>();

        for (var start = 0; start < text.Length; start += step)
        {
            var length = Math.Min(chunkSize, text.Length - start);
            if (length <= 0)
            {
                break;
            }

            var content = text.Substring(start, length).Trim();
            if (!string.IsNullOrWhiteSpace(content))
            {
                chunks.Add(CreateChunk(documentId, chunks.Count, content, start, start + length));
            }

            if (start + length >= text.Length)
            {
                break;
            }
        }

        return chunks;
    }

    private static IReadOnlyList<DocumentChunk> CreateParagraphChunks(int documentId, string text, SystemSettingDto setting)
    {
        var paragraphs = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (paragraphs.Length <= 1)
        {
            return CreateWordChunks(documentId, text, setting);
        }

        var chunks = new List<DocumentChunk>();
        foreach (var paragraph in paragraphs)
        {
            chunks.AddRange(CreateWordChunks(documentId, paragraph, setting)
                .Select(chunk =>
                {
                    chunk.ChunkIndex = chunks.Count;
                    return chunk;
                }));
        }

        return chunks;
    }

    private static DocumentChunk CreateChunk(int documentId, int index, string content, int startPosition, int endPosition)
    {
        var tokenCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return new DocumentChunk
        {
            DocumentId = documentId,
            ChunkIndex = index,
            Content = content,
            StartPosition = startPosition,
            EndPosition = endPosition,
            WordCount = tokenCount,
            TokenCount = tokenCount,
            CreatedAt = DateTime.UtcNow
        };
    }
}
