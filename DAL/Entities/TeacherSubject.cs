namespace DAL.Entities;

public class TeacherSubject
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public int SubjectId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public User? Teacher { get; set; }

    public Subject? Subject { get; set; }
}
