using DAL.Entities;

namespace BLL.DTOs;

public class AccountFilterDto
{
    public string? Search { get; set; }

    public UserRole? Role { get; set; }

    public AccountStatus? AccountStatus { get; set; }
}
