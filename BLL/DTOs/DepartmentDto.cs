namespace BLL.DTOs;

public class DepartmentDto
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ManagerTeacherName { get; set; } = string.Empty;
}
