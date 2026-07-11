using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.ViewModels;

public class SubjectFormViewModel
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

    [Required]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; set; } = Array.Empty<SelectListItem>();
}
