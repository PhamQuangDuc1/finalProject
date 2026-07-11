using BLL.DTOs;

namespace finalProject.ViewModels;

public class TeacherAssignmentsIndexViewModel
{
    public TeacherAssignmentFormViewModel Form { get; set; } = new();

    public IReadOnlyList<TeacherAssignmentDto> Assignments { get; set; } = Array.Empty<TeacherAssignmentDto>();
}
