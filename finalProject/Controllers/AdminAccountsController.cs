using System.Security.Claims;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using finalProject.Authorization;
using finalProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.Controllers;

[Authorize(Roles = StudyMateRoles.Admin)]
public class AdminAccountsController : Controller
{
    private readonly IUserManagementService _userManagementService;

    public AdminAccountsController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, UserRole? role, AccountStatus? accountStatus, CancellationToken cancellationToken)
    {
        var filter = new AccountFilterDto
        {
            Search = search,
            Role = role,
            AccountStatus = accountStatus
        };

        var accounts = await _userManagementService.GetAccountsAsync(GetCurrentUser(), filter, cancellationToken);

        return View(new AdminAccountsIndexViewModel
        {
            Accounts = accounts,
            Search = search,
            Role = role,
            AccountStatus = accountStatus,
            RoleOptions = GetRoleOptions(role),
            StatusOptions = GetStatusOptions(accountStatus)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(AssignAccountRoleViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn vai trò hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _userManagementService.AssignRoleAsync(GetCurrentUser(), model.UserId, model.Role, cancellationToken);
            TempData["StatusMessage"] = "Đã phân quyền và kích hoạt tài khoản.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _userManagementService.LockAccountAsync(GetCurrentUser(), id, cancellationToken);
            TempData["StatusMessage"] = "Đã khóa tài khoản.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _userManagementService.ActivateAccountAsync(GetCurrentUser(), id, cancellationToken);
            TempData["StatusMessage"] = "Đã kích hoạt lại tài khoản.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Index));
    }

    private CurrentUserDto GetCurrentUser()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleValue = User.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(userIdValue, out var userId) ||
            !Enum.TryParse<UserRole>(roleValue, out var role))
        {
            throw new InvalidOperationException("Current user claims are invalid.");
        }

        return new CurrentUserDto { UserId = userId, Role = role };
    }

    private static IReadOnlyList<SelectListItem> GetRoleOptions(UserRole? selectedRole)
    {
        return new[]
        {
            new SelectListItem("Chưa phân quyền", UserRole.Pending.ToString(), selectedRole == UserRole.Pending),
            new SelectListItem("Teacher", UserRole.Teacher.ToString(), selectedRole == UserRole.Teacher),
            new SelectListItem("Student", UserRole.Student.ToString(), selectedRole == UserRole.Student),
            new SelectListItem("Admin", UserRole.Admin.ToString(), selectedRole == UserRole.Admin)
        };
    }

    private static IReadOnlyList<SelectListItem> GetStatusOptions(AccountStatus? selectedStatus)
    {
        return new[]
        {
            new SelectListItem("Chờ phân quyền", AccountStatus.Pending.ToString(), selectedStatus == AccountStatus.Pending),
            new SelectListItem("Đang hoạt động", AccountStatus.Active.ToString(), selectedStatus == AccountStatus.Active),
            new SelectListItem("Đã khóa", AccountStatus.Locked.ToString(), selectedStatus == AccountStatus.Locked)
        };
    }
}
