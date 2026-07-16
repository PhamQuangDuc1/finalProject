using System.Security.Claims;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using finalProject.Authorization;
using finalProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.Controllers;

[Authorize(Roles = StudyMateRoles.Admin)]
public class SubscriptionPackagesController : Controller
{
    private readonly ISubscriptionPackageService _packageService;

    public SubscriptionPackagesController(ISubscriptionPackageService packageService)
    {
        _packageService = packageService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var packages = await _packageService.GetPackagesAsync(activeOnly: false, cancellationToken);
        return View(packages);
    }

    public IActionResult Create()
    {
        return View(new SubscriptionPackageFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubscriptionPackageFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _packageService.CreatePackageAsync(GetCurrentUser(), new CreatePackageDto
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                DurationDays = model.DurationDays,
                MaxTokens = model.MaxTokens,
                IsActive = model.IsActive
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["StatusMessage"] = "Tạo gói đăng ký thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var package = await _packageService.GetPackageByIdAsync(id, cancellationToken);
        if (package is null)
        {
            return NotFound();
        }

        return View(new SubscriptionPackageFormViewModel
        {
            Id = package.Id,
            Name = package.Name,
            Description = package.Description,
            Price = package.Price,
            DurationDays = package.DurationDays,
            MaxTokens = package.MaxTokens,
            IsActive = package.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SubscriptionPackageFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _packageService.UpdatePackageAsync(GetCurrentUser(), new UpdatePackageDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                DurationDays = model.DurationDays,
                MaxTokens = model.MaxTokens,
                IsActive = model.IsActive
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["StatusMessage"] = "Cập nhật gói đăng ký thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        await _packageService.ActivateAsync(GetCurrentUser(), id, cancellationToken);
        TempData["StatusMessage"] = "Đã kích hoạt gói đăng ký.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _packageService.DeactivateAsync(GetCurrentUser(), id, cancellationToken);
        TempData["StatusMessage"] = "Đã tạm ngưng gói đăng ký.";
        return RedirectToAction(nameof(Index));
    }

    private CurrentUserDto GetCurrentUser()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return new CurrentUserDto
        {
            UserId = int.TryParse(userIdValue, out var userId) ? userId : 0,
            Role = UserRole.Admin
        };
    }
}