using System.Security.Claims;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using finalProject.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.Controllers;

[Authorize(Roles = StudyMateRoles.Admin)]
public class DocumentsController : Controller
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var documents = await _documentService.GetDocumentsForAdminAsync(GetCurrentUser(), cancellationToken);

        return View(documents);
    }

    public IActionResult ViewDocument(int id)
    {
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var document = await _documentService.GetDocumentByIdAsync(GetCurrentUser(), id, cancellationToken);

        return document is null ? NotFound() : View(document);
    }

    [HttpGet]
    public async Task<IActionResult> RealtimeSnapshot(int id, CancellationToken cancellationToken)
    {
        var document = await _documentService.GetDocumentByIdAsync(GetCurrentUser(), id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        return Json(new
        {
            id = document.Id,
            title = document.Title,
            subjectName = document.SubjectName,
            departmentName = document.DepartmentName,
            uploadedByTeacherName = document.UploadedByTeacherName,
            status = document.Status,
            fileName = document.FileName,
            chunkCount = document.ChunkCount,
            contentVersion = document.ContentVersion,
            currentContent = document.CurrentContent,
            description = document.Description,
            hasManualEdits = document.HasManualEdits,
            contentUpdatedAtUtc = document.ContentUpdatedAtUtc,
            contentUpdatedAtText = document.ContentUpdatedAtUtc?.ToLocalTime().ToString("g"),
            updatedByTeacherName = document.ContentUpdatedByTeacherName,
            chunks = document.Chunks.Select(chunk => new
            {
                chunkIndex = chunk.ChunkIndex,
                content = chunk.Content,
                startPosition = chunk.StartPosition,
                endPosition = chunk.EndPosition,
                wordCount = chunk.WordCount,
                tokenCount = chunk.TokenCount
            })
        });
    }

    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var document = await _documentService.GetDocumentByIdAsync(GetCurrentUser(), id, cancellationToken);

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
            Role = UserRole.Admin
        };
    }
}
