using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.ViewModels;

public class DocumentEditViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Môn học")]
    public int SubjectId { get; set; }

    [Display(Name = "Chương")]
    public int? ChapterId { get; set; }

    [Required]
    [StringLength(250)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    public IReadOnlyList<SelectListItem> SubjectOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> ChapterOptions { get; set; } = Array.Empty<SelectListItem>();
}
