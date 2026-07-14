namespace BLL.DTOs;

public class DocumentDto
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public int UploadedByTeacherId { get; set; }

    public string UploadedByTeacherName { get; set; } = string.Empty;

    public int? ChapterId { get; set; }

    public string ChapterName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string Status { get; set; } = string.Empty;

    public int ChunkCount { get; set; }

    public bool IsArchived { get; set; }

    public DateTime UploadedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }

    public int? ArchivedByTeacherId { get; set; }

    public string? ErrorMessage { get; set; }

    public string CurrentContent { get; set; } = string.Empty;

    public bool HasManualEdits { get; set; }

    public DateTime? ContentUpdatedAtUtc { get; set; }

    public int? ContentUpdatedByTeacherId { get; set; }

    public string ContentUpdatedByTeacherName { get; set; } = string.Empty;

    public int ContentVersion { get; set; } = 1;

    public IReadOnlyList<DocumentVersionDto> Versions { get; set; } = Array.Empty<DocumentVersionDto>();
}
