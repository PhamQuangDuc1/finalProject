namespace BLL.DTOs;

public class DepartmentDto
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ManagerTeacherId { get; set; }

    public string ManagerTeacherName { get; set; } = string.Empty;

    public int NumberOfSubjects { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
