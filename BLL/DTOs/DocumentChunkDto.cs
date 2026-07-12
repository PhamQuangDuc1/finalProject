namespace BLL.DTOs;

public class DocumentChunkDto
{
    public int Id { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public int StartPosition { get; set; }

    public int EndPosition { get; set; }

    public int WordCount { get; set; }

    public int TokenCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
