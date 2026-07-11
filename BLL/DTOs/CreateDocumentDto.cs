namespace BLL.DTOs;

public class CreateDocumentDto
{
    public int SubjectId { get; set; }

    public int UploadedByTeacherId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }
}
