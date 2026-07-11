namespace BLL.DTOs;

public class TeacherAssignmentDto
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public string TeacherName { get; set; } = string.Empty;

    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public DateTime AssignedAt { get; set; }
}
