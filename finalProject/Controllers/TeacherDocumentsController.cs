using System.Security.Claims;
using BLL.DTOs;
using BLL.Interfaces;
using finalProject.Authorization;
using finalProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.Controllers;

[Authorize(Roles = StudyMateRoles.Teacher)]
public class TeacherDocumentsController : Controller
{
    private readonly IDocumentService _documentService;

    public TeacherDocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        var documents = await _documentService.GetDocumentsForTeacherAsync(teacherId, cancellationToken);

        return View(documents);
    }

    [HttpGet]
    public IActionResult Upload()
    {
        return View(new DocumentUploadViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(DocumentUploadViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.File is null)
        {
            return View(model);
        }

        var teacherId = GetCurrentUserId();
        var documentId = await _documentService.RegisterDocumentAsync(new CreateDocumentDto
        {
            SubjectId = model.SubjectId,
            UploadedByTeacherId = teacherId,
            Title = model.Title,
            FileName = model.File.FileName,
            ContentType = model.File.ContentType,
            FileSize = model.File.Length
        }, cancellationToken);

        TempData["StatusMessage"] = $"Document metadata registered with ID {documentId}. Chunking and indexing will be added in a later step.";

        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Current user id claim is missing.");
    }
}
