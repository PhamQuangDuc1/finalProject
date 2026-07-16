using System.ComponentModel.DataAnnotations;

namespace finalProject.ViewModels;

public class SubscriptionPackageFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên gói.")]
    [StringLength(200, ErrorMessage = "Tên gói không được vượt quá 200 ký tự.")]
    [Display(Name = "Tên gói")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá gói.")]
    [Range(0, 99999999.99, ErrorMessage = "Giá phải nằm trong khoảng 0 đến 99.999.999,99.")]
    [Display(Name = "Giá (VNĐ)")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập thời hạn.")]
    [Range(1, 3650, ErrorMessage = "Thời hạn phải từ 1 đến 3650 ngày.")]
    [Display(Name = "Thời hạn (ngày)")]
    public int DurationDays { get; set; } = 30;

    [Required(ErrorMessage = "Vui lòng nhập số token tối đa.")]
    [Range(0, int.MaxValue, ErrorMessage = "Số token phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Số token AI tối đa")]
    public int MaxTokens { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;
}