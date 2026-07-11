namespace DAL.Entities;

public class Subject
{
    public int Id { get; set; }

    public int DepartmentId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Department? Department { get; set; }

    public ICollection<Document> Documents { get; set; } = new List<Document>();

    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
