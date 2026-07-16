using System.Security.Claims;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using finalProject.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.Controllers;

[Authorize(Roles = StudyMateRoles.Admin)]
public class AdminUsersController : Controller
{
    private readonly IAdminSubscriptionService _adminSubscriptionService;

    public AdminUsersController(IAdminSubscriptionService adminSubscriptionService)
    {
        _adminSubscriptionService = adminSubscriptionService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser();
        var subscriptions = await _adminSubscriptionService.GetAllActiveSubscriptionsAsync(currentUser, cancellationToken);
        return View(subscriptions);
    }

    [HttpGet]
    public async Task<IActionResult> ChangePackage(int id, CancellationToken cancellationToken)
    {
        try
        {
            var form = await _adminSubscriptionService.GetChangePackageFormAsync(GetCurrentUser(), id, cancellationToken);
            if (form is null)
            {
                return NotFound();
            }
            return View(form);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePackage(SubscriptionChangeRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            await _adminSubscriptionService.ChangePackageAsync(GetCurrentUser(), request, cancellationToken);
            TempData["StatusMessage"] = "Đã cập nhật gói đăng ký cho người dùng.";
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
    public async Task<IActionResult> Cancel(int id, string? reason, CancellationToken cancellationToken)
    {
        try
        {
            await _adminSubscriptionService.CancelSubscriptionAsync(GetCurrentUser(), id, reason, cancellationToken);
            TempData["StatusMessage"] = "Đã hủy gói đăng ký của người dùng.";
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
}