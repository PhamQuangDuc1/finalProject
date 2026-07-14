namespace BLL.DTOs;

public class DocumentVersionDto
{
    public int Id { get; set; }

    public int VersionNumber { get; set; }

    public string Content { get; set; } = string.Empty;

    public int UpdatedByTeacherId { get; set; }

    public string UpdatedByTeacherName { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }

    public string? ChangeNote { get; set; }
}
