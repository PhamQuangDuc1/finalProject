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

[Authorize(Policy = StudyMatePolicies.ManageChunkConfiguration)]
public class ChunkConfigurationsController : Controller
{
    private readonly IChunkConfigurationService _chunkConfigurationService;

    public ChunkConfigurationsController(IChunkConfigurationService chunkConfigurationService)
    {
        _chunkConfigurationService = chunkConfigurationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var setting = await _chunkConfigurationService.GetCurrentAsync(cancellationToken);

        return View(BuildViewModel(setting));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ChunkConfigurationViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.StrategyOptions = GetStrategyOptions(model.ChunkStrategy);
            return View(model);
        }

        try
        {
            await _chunkConfigurationService.UpdateAsync(GetCurrentUser(), new UpdateSystemSettingDto
            {
                ChunkStrategy = model.ChunkStrategy,
                ChunkSize = model.ChunkSize,
                ChunkOverlap = model.ChunkOverlap,
                TopK = model.TopK
            }, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.StrategyOptions = GetStrategyOptions(model.ChunkStrategy);
            return View(model);
        }

        TempData["StatusMessage"] = "Đã lưu cấu hình chunk.";
        return RedirectToAction(nameof(Index));
    }

    private static ChunkConfigurationViewModel BuildViewModel(SystemSettingDto setting)
    {
        return new ChunkConfigurationViewModel
        {
            ChunkStrategy = setting.ChunkStrategy,
            ChunkSize = setting.ChunkSize,
            ChunkOverlap = setting.ChunkOverlap,
            TopK = setting.TopK,
            UpdatedAt = setting.UpdatedAt,
            UpdatedByAdminName = setting.UpdatedByAdminName,
            StrategyOptions = GetStrategyOptions(setting.ChunkStrategy)
        };
    }

    private static IReadOnlyList<SelectListItem> GetStrategyOptions(ChunkStrategy selectedStrategy)
    {
        var strategies = new[]
        {
            ChunkStrategy.Paragraph,
            ChunkStrategy.Words,
            ChunkStrategy.Characters,
            ChunkStrategy.FixedSize
        };

        return strategies
            .Select(strategy => new SelectListItem(strategy.ToString(), strategy.ToString(), strategy == selectedStrategy))
            .ToList();
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
