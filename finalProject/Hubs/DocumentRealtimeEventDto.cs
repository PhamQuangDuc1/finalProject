namespace finalProject.Hubs;

public class DocumentRealtimeEventDto
{
    public int DocumentId { get; set; }

    public string TeacherUploader { get; set; } = string.Empty;

    public string Document { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }
}
