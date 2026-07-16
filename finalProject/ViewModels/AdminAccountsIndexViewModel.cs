using BLL.DTOs;
using DAL.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.ViewModels;

public class AdminAccountsIndexViewModel
{
    public IReadOnlyList<AccountDto> Accounts { get; set; } = Array.Empty<AccountDto>();

    public string? Search { get; set; }

    public UserRole? Role { get; set; }

    public AccountStatus? AccountStatus { get; set; }

    public IReadOnlyList<SelectListItem> RoleOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = Array.Empty<SelectListItem>();
}
