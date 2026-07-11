namespace DAL.Entities;

public class Department
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ManagerTeacherId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public User? ManagerTeacher { get; set; }

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
