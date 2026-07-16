using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.ViewModels;

public class DocumentEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn môn học.")]
    [Display(Name = "Môn học")]
    public int SubjectId { get; set; }

    [Display(Name = "Chương")]
    public int? ChapterId { get; set; }

    [StringLength(200, ErrorMessage = "Tên chương không được vượt quá 200 ký tự.")]
    [Display(Name = "Chương")]
    public string? ChapterName { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự.")]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Nội dung tài liệu không được để trống.")]
    [StringLength(200000, ErrorMessage = "Nội dung tài liệu không được vượt quá 200.000 ký tự.")]
    [Display(Name = "Nội dung tài liệu")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Tên file gốc")]
    public string OriginalFileName { get; set; } = string.Empty;

    [Display(Name = "Trạng thái hiện tại")]
    public string CurrentStatus { get; set; } = string.Empty;

    public int ContentVersion { get; set; } = 1;

    public IReadOnlyList<SelectListItem> SubjectOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> ChapterOptions { get; set; } = Array.Empty<SelectListItem>();
}
