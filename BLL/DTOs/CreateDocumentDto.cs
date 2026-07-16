namespace BLL.DTOs;

public class CreateDocumentDto
{
    public int SubjectId { get; set; }

    public int UploadedByTeacherId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ChapterId { get; set; }

    public string? ChapterName { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public byte[] FileContent { get; set; } = Array.Empty<byte>();

    public string StorageRootPath { get; set; } = string.Empty;
}
