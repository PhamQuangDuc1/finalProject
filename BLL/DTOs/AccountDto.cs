using DAL.Entities;

namespace BLL.DTOs;

public class AccountDto
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public string RoleLabel { get; set; } = string.Empty;

    public AccountStatus AccountStatus { get; set; }

    public string AccountStatusLabel { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public int? ApprovedByAdminId { get; set; }

    public string ApprovedByAdminName { get; set; } = string.Empty;
}
