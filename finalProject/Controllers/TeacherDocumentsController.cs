using BLL.DTOs;
using BLL.Interfaces;
using finalProject.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.Controllers;

public class TeacherDocumentsController : Controller
{
    private readonly IDocumentService _documentService;

    public TeacherDocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public async Task<IActionResult> Index(int teacherId, CancellationToken cancellationToken)
    {
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

        var documentId = await _documentService.RegisterDocumentAsync(new CreateDocumentDto
        {
            SubjectId = model.SubjectId,
            UploadedByTeacherId = model.UploadedByTeacherId,
            Title = model.Title,
            FileName = model.File.FileName,
            ContentType = model.File.ContentType,
            FileSize = model.File.Length
        }, cancellationToken);

        TempData["StatusMessage"] = $"Document metadata registered with ID {documentId}. Chunking and indexing will be added in a later step.";

        return RedirectToAction(nameof(Index), new { teacherId = model.UploadedByTeacherId });
    }
}
