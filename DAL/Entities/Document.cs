namespace DAL.Entities;

public class Document
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public int SubjectId { get; set; }

    public int? ChapterId { get; set; }

    public int UploadedByTeacherId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Uploading;

    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public int? ArchivedByTeacherId { get; set; }

    public DateTime? ScheduledArchiveAt { get; set; }

    public int? ScheduledArchiveByTeacherId { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ExtractedContent { get; set; }

    public string? EditedContent { get; set; }

    public bool HasManualEdits { get; set; }

    public DateTime? ContentUpdatedAt { get; set; }

    public int? ContentUpdatedByTeacherId { get; set; }

    public int ContentVersion { get; set; } = 1;

    public Subject? Subject { get; set; }

    public Chapter? Chapter { get; set; }

    public User? UploadedByTeacher { get; set; }

    public User? ArchivedByTeacher { get; set; }

    public User? ScheduledArchiveByTeacher { get; set; }
    public User? ContentUpdatedByTeacher { get; set; }

    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

    public ICollection<AiUsageLog> AiUsageLogs { get; set; } = new List<AiUsageLog>();
}
