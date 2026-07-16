using System.Security.Claims;
using System.Text.Json;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IDocumentService _documentService;
    private readonly IAiQuestionAnsweringService _aiQuestionAnsweringService;
    private readonly IAiUsageService _aiUsageService;

    public DashboardController(
        IDashboardService dashboardService,
        IDocumentService documentService,
        IAiQuestionAnsweringService aiQuestionAnsweringService,
        IAiUsageService aiUsageService)
    {
        _dashboardService = dashboardService;
        _documentService = documentService;
        _aiQuestionAnsweringService = aiQuestionAnsweringService;
        _aiUsageService = aiUsageService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.GetDashboardAsync(GetCurrentUser(), cancellationToken);

        return View(dashboard);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AskAi(string question, CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser();
        if (currentUser.Role != UserRole.Student)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            TempData["DashboardAiErrorMessage"] = "Vui lòng nhập câu hỏi.";
            return RedirectToAction(nameof(Index));
        }

        var documents = await _documentService.GetDocumentsForStudentAsync(currentUser, cancellationToken);
        if (documents.Count == 0)
        {
            TempData["DashboardAiErrorMessage"] = "Chưa có tài liệu đã index để hỏi AI.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var answer = await _aiQuestionAnsweringService.AnswerDocumentsQuestionAsync(
                documents,
                question,
                cancellationToken);
            await _aiUsageService.LogUsageAsync(new CreateAiUsageLogDto
            {
                UserId = currentUser.UserId,
                DocumentId = null,
                OperationType = AiOperationType.ChatCompletion,
                ModelName = answer.ModelName,
                PromptTokens = answer.PromptTokens,
                CompletionTokens = answer.CompletionTokens
            }, cancellationToken);

            TempData["DashboardAiQuestion"] = answer.Question;
            TempData["DashboardAiAnswer"] = answer.Answer;
            TempData["DashboardAiCitations"] = JsonSerializer.Serialize(answer.Citations);
        }
        catch (InvalidOperationException exception)
        {
            TempData["DashboardAiErrorMessage"] = exception.Message;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            TempData["DashboardAiErrorMessage"] = $"Không thể gọi AI lúc này: {exception.Message}";
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
