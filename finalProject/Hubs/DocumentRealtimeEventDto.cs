namespace finalProject.Hubs;

public class DocumentRealtimeEventDto
{
    public int DocumentId { get; set; }

    public string TeacherUploader { get; set; } = string.Empty;

    public string Document { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public string UpdatedByTeacherName { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int ChunkCount { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
