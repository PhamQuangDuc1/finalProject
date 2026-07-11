namespace BLL.DTOs;

public class UpdateSubjectDto
{
    public int Id { get; set; }

    public int DepartmentId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
