namespace DAL.Entities;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Department> ManagedDepartments { get; set; } = new List<Department>();

    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();

    public ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();

    public ICollection<Document> ArchivedDocuments { get; set; } = new List<Document>();

    public ICollection<Document> ScheduledArchiveDocuments { get; set; } = new List<Document>();

    public ICollection<Document> ContentUpdatedDocuments { get; set; } = new List<Document>();

    public ICollection<DocumentVersion> DocumentVersions { get; set; } = new List<DocumentVersion>();

    public ICollection<SystemSetting> UpdatedSystemSettings { get; set; } = new List<SystemSetting>();

    public ICollection<AiUsageLog> AiUsageLogs { get; set; } = new List<AiUsageLog>();
}
