using System.ComponentModel.DataAnnotations;

namespace finalProject.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập hoặc email là bắt buộc.")]
    [Display(Name = "Tên đăng nhập hoặc email")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
