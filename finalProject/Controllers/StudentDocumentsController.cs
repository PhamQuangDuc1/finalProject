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

    public StudentDocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var documents = await _documentService.GetDocumentsForStudentAsync(GetCurrentUser(), cancellationToken);

        return View(documents);
    }

    public async Task<IActionResult> ViewDocument(int id, CancellationToken cancellationToken)
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

        return document is null ? NotFound() : Content($"{document.Title}\n{document.SubjectName}", "text/plain");
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
