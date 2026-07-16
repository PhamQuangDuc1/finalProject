using System.Security.Claims;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using finalProject.Authorization;
using finalProject.Hubs;
using finalProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;

namespace finalProject.Controllers;

[Authorize(Policy = StudyMatePolicies.ManageChunkConfiguration)]
public class ChunkConfigurationsController : Controller
{
    private readonly IChunkConfigurationService _chunkConfigurationService;
    private readonly IDocumentService _documentService;
    private readonly IHubContext<DocumentProcessingHub> _hubContext;

    public ChunkConfigurationsController(
        IChunkConfigurationService chunkConfigurationService,
        IDocumentService documentService,
        IHubContext<DocumentProcessingHub> hubContext)
    {
        _chunkConfigurationService = chunkConfigurationService;
        _documentService = documentService;
        _hubContext = hubContext;
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
            var currentUser = GetCurrentUser();
            await _chunkConfigurationService.UpdateAsync(currentUser, new UpdateSystemSettingDto
            {
                ChunkStrategy = model.ChunkStrategy,
                ChunkSize = model.ChunkSize,
                ChunkOverlap = model.ChunkOverlap,
                TopK = model.TopK
            }, cancellationToken);

            var reindexedDocuments = await _documentService.ReindexIndexedDocumentsAsync(currentUser, cancellationToken);
            foreach (var document in reindexedDocuments)
            {
                await BroadcastDocumentReindexedAsync(document, cancellationToken);
            }

            TempData["StatusMessage"] = $"Đã lưu cấu hình chunk và tạo lại chỉ mục cho {reindexedDocuments.Count} tài liệu.";
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

    private async Task BroadcastDocumentReindexedAsync(DocumentDto document, CancellationToken cancellationToken)
    {
        var payload = new DocumentRealtimeEventDto
        {
            DocumentId = document.Id,
            TeacherUploader = document.UploadedByTeacherName,
            Document = document.Title,
            Subject = document.SubjectName,
            Title = document.Title,
            SubjectName = document.SubjectName,
            UpdatedByTeacherName = document.UploadedByTeacherName,
            UpdatedAtUtc = DateTime.UtcNow,
            Action = "Tạo lại chỉ mục theo cấu hình chunk mới",
            Status = document.Status,
            ChunkCount = document.ChunkCount,
            OccurredAtUtc = DateTime.UtcNow
        };

        await _hubContext.Clients.Group(DocumentProcessingHub.AdminGroup)
            .SendAsync("DocumentReindexed", payload, cancellationToken);
        await _hubContext.Clients.Group(DocumentProcessingHub.GetTeacherGroupName(document.UploadedByTeacherId))
            .SendAsync("DocumentReindexed", payload, cancellationToken);
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
