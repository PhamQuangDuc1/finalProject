using System.ComponentModel.DataAnnotations;
using DAL.Entities;

namespace finalProject.ViewModels;

public class AssignAccountRoleViewModel
{
    [Required]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [Display(Name = "Vai trò")]
    public UserRole Role { get; set; }
}
