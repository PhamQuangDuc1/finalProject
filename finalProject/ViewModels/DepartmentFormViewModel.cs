using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.ViewModels;

public class DepartmentFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public int? ManagerTeacherId { get; set; }

    public IReadOnlyList<SelectListItem> TeacherOptions { get; set; } = Array.Empty<SelectListItem>();
}
