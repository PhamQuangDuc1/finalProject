using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.ViewModels;

public class AssignManagerViewModel
{
    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public string? CurrentManagerName { get; set; }

    [Required]
    [Display(Name = "Manager Teacher")]
    public int TeacherId { get; set; }

    public IReadOnlyList<SelectListItem> TeacherOptions { get; set; } = Array.Empty<SelectListItem>();
}
