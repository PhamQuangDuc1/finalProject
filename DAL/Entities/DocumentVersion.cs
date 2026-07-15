namespace DAL.Entities;

public class DocumentVersion
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public int VersionNumber { get; set; }

    public string Content { get; set; } = string.Empty;

    public int UpdatedByTeacherId { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? ChangeNote { get; set; }

    public Document? Document { get; set; }

    public User? UpdatedByTeacher { get; set; }
}
