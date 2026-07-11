using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.ViewModels;

public class TeacherAssignmentFormViewModel
{
    [Required]
    [Display(Name = "Teacher")]
    public int TeacherId { get; set; }

    [Required]
    [Display(Name = "Subject")]
    public int SubjectId { get; set; }

    public IReadOnlyList<SelectListItem> TeacherOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> SubjectOptions { get; set; } = Array.Empty<SelectListItem>();
}
