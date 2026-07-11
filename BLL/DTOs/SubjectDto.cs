namespace BLL.DTOs;

public class SubjectDto
{
    public int Id { get; set; }

    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public IReadOnlyList<string> AssignedTeacherNames { get; set; } = Array.Empty<string>();
}
