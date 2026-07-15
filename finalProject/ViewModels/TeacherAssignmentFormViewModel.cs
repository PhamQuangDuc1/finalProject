using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.ViewModels;

public class TeacherAssignmentFormViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn giảng viên.")]
    [Display(Name = "Giảng viên")]
    public int TeacherId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn môn học.")]
    [Display(Name = "Môn học")]
    public int SubjectId { get; set; }

    public IReadOnlyList<SelectListItem> TeacherOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> SubjectOptions { get; set; } = Array.Empty<SelectListItem>();
}
