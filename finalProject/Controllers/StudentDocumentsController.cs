using System.Security.Claims;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using finalProject.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.Controllers;

[Authorize(Roles = StudyMateRoles.Student)]
public class StudentDocumentsController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly IAiQuestionAnsweringService _aiQuestionAnsweringService;
    private readonly IAiUsageService _aiUsageService;

    public StudentDocumentsController(
        IDocumentService documentService,
        IAiQuestionAnsweringService aiQuestionAnsweringService,
        IAiUsageService aiUsageService)
    {
        _documentService = documentService;
        _aiQuestionAnsweringService = aiQuestionAnsweringService;
        _aiUsageService = aiUsageService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var documents = await _documentService.GetDocumentsForStudentAsync(GetCurrentUser(), cancellationToken);

        return View(documents);
    }

    public IActionResult ViewDocument(int id)
    {
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        DocumentDto? document;
        try
        {
            document = await _documentService.GetDocumentByIdAsync(GetCurrentUser(), id, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        return document is null ? NotFound() : View(document);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AskAi(int id, string question, CancellationToken cancellationToken)
    {
        DocumentDto? document;
        try
        {
            document = await _documentService.GetDocumentByIdAsync(GetCurrentUser(), id, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        if (document is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            TempData["AiErrorMessage"] = "Vui long nhap cau hoi.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            var answer = await _aiQuestionAnsweringService.AnswerDocumentQuestionAsync(
                document,
                question,
                cancellationToken);
            await _aiUsageService.LogUsageAsync(new CreateAiUsageLogDto
            {
                UserId = GetCurrentUser().UserId,
                DocumentId = document.Id,
                OperationType = AiOperationType.ChatCompletion,
                ModelName = answer.ModelName,
                PromptTokens = answer.PromptTokens,
                CompletionTokens = answer.CompletionTokens
            }, cancellationToken);
            TempData["AiQuestion"] = answer.Question;
            TempData["AiAnswer"] = answer.Answer;
        }
        catch (InvalidOperationException exception)
        {
            TempData["AiErrorMessage"] = exception.Message;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            TempData["AiErrorMessage"] = $"Khong the goi AI luc nay: {exception.Message}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        DocumentDto? document;
        try
        {
            document = await _documentService.GetDocumentByIdAsync(GetCurrentUser(), id, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        if (document is null)
        {
            return NotFound();
        }

        if (!System.IO.File.Exists(document.FilePath))
        {
            TempData["StatusMessage"] = "Tệp tài liệu chưa có trên hệ thống lưu trữ.";
            return RedirectToAction(nameof(Index));
        }

        return PhysicalFile(document.FilePath, "application/octet-stream", document.FileName);
    }

    private CurrentUserDto GetCurrentUser()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return new CurrentUserDto
        {
            UserId = int.TryParse(userIdValue, out var userId) ? userId : 0,
            Role = UserRole.Student
        };
    }
}
