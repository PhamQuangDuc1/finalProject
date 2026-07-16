using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs;

public class UpdatePackageDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Tên gói")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Range(0, 99999999.99)]
    [Display(Name = "Giá (VNĐ)")]
    public decimal Price { get; set; }

    [Range(1, 3650)]
    [Display(Name = "Thời hạn (ngày)")]
    public int DurationDays { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Số token tối đa")]
    public int MaxTokens { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; }
}