using System.ComponentModel.DataAnnotations;

namespace finalProject.ViewModels;

public class DocumentUploadViewModel
{
    [Required]
    [Display(Name = "ID môn học")]
    public int SubjectId { get; set; }

    [Display(Name = "ID giảng viên")]
    public int UploadedByTeacherId { get; set; }

    [Required]
    [StringLength(250)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Tệp tài liệu")]
    public IFormFile? File { get; set; }
}
