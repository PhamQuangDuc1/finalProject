namespace BLL.DTOs;

public class DocumentDto
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public string UploadedByTeacherName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime UploadedAtUtc { get; set; }
}
