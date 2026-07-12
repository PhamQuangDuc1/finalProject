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

[Authorize(Roles = StudyMateRoles.Teacher)]
public class TeacherDocumentsController : Controller
{
    private static readonly HashSet<string> AllowedUploadExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".pptx"
    };

    private readonly IDocumentService _documentService;
    private readonly IWebHostEnvironment _environment;

    public TeacherDocumentsController(IDocumentService documentService, IWebHostEnvironment environment)
    {
        _documentService = documentService;
        _environment = environment;
    }

    public async Task<IActionResult> Index(int? subjectId, DocumentStatus? status, int? chapterId, string? search, CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser();
        var filter = new DocumentFilterDto
        {
            SubjectId = subjectId,
            Status = status,
            ChapterId = chapterId,
            Search = search
        };
        var documents = await _documentService.GetDocumentsForTeacherAsync(currentUser, filter, cancellationToken);
        var allTeacherDocuments = await _documentService.GetDocumentsForTeacherAsync(currentUser, cancellationToken);

        return View(new TeacherDocumentsIndexViewModel
        {
            Documents = documents,
            SubjectId = subjectId,
            Status = status,
            ChapterId = chapterId,
            Search = search,
            SubjectOptions = await GetSubjectOptionsAsync(currentUser.UserId, cancellationToken),
            StatusOptions = GetStatusOptions(),
            ChapterOptions = GetChapterOptions(allTeacherDocuments)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken)
    {
        return View(await BuildUploadViewModelAsync(new DocumentUploadViewModel(), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(DocumentUploadViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.File is null)
        {
            await BuildUploadViewModelAsync(model, cancellationToken);
            return View(model);
        }

        if (model.File.Length == 0)
        {
            ModelState.AddModelError(nameof(model.File), "Tệp tải lên không được rỗng.");
            await BuildUploadViewModelAsync(model, cancellationToken);
            return View(model);
        }

        var extension = Path.GetExtension(model.File.FileName);
        if (!AllowedUploadExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(model.File), "Chỉ cho phép tải lên file PDF, DOCX hoặc PPTX.");
            await BuildUploadViewModelAsync(model, cancellationToken);
            return View(model);
        }

        await using var memoryStream = new MemoryStream();
        await model.File.CopyToAsync(memoryStream, cancellationToken);
        var currentUser = GetCurrentUser();
        int documentId;
        try
        {
            documentId = await _documentService.UploadDocumentAsync(currentUser, new CreateDocumentDto
            {
                SubjectId = model.SubjectId,
                ChapterId = model.ChapterId,
                UploadedByTeacherId = currentUser.UserId,
                Title = model.Title,
                Description = model.Description,
                FileName = model.File.FileName,
                ContentType = model.File.ContentType,
                FileSize = model.File.Length,
                FileContent = memoryStream.ToArray(),
                StorageRootPath = Path.Combine(_environment.WebRootPath, "uploads", "documents")
            }, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await BuildUploadViewModelAsync(model, cancellationToken);
            return View(model);
        }

        TempData["StatusMessage"] = $"Đã upload tài liệu #{documentId}. Hệ thống đã xử lý chunk và index theo cấu hình hiện tại.";

        return RedirectToAction(nameof(Index));
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

        return document is null ? NotFound() : Content($"{document.Title}\n{document.SubjectName}\n{document.Status}", "text/plain");
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
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

        TempData["StatusMessage"] = "Màn hình sửa tài liệu sẽ được triển khai ở giai đoạn sau.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _documentService.ArchiveDocumentAsync(GetCurrentUser(), id, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        TempData["StatusMessage"] = "Đã archive tài liệu.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reindex(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _documentService.ReindexDocumentAsync(GetCurrentUser(), id, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        TempData["StatusMessage"] = "Đã đưa tài liệu vào trạng thái re-index.";

        return RedirectToAction(nameof(Index));
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

    private async Task<IReadOnlyList<SelectListItem>> GetSubjectOptionsAsync(int teacherId, CancellationToken cancellationToken)
    {
        var options = await _documentService.GetUploadOptionsForTeacherAsync(new CurrentUserDto
        {
            UserId = teacherId,
            Role = UserRole.Teacher
        }, cancellationToken);

        return options.Subjects.Select(subject => new SelectListItem(subject.DisplayName, subject.Id.ToString())).ToList();
    }

    private async Task<DocumentUploadViewModel> BuildUploadViewModelAsync(DocumentUploadViewModel model, CancellationToken cancellationToken)
    {
        var options = await _documentService.GetUploadOptionsForTeacherAsync(GetCurrentUser(), cancellationToken);
        model.SubjectOptions = options.Subjects
            .Select(subject => new SelectListItem(subject.DisplayName, subject.Id.ToString(), subject.Id == model.SubjectId))
            .ToList();
        model.ChapterOptions = options.Chapters
            .Select(chapter => new SelectListItem(chapter.DisplayName, chapter.Id.ToString(), chapter.Id == model.ChapterId))
            .ToList();

        return model;
    }

    private static IReadOnlyList<SelectListItem> GetStatusOptions()
    {
        return Enum.GetValues<DocumentStatus>()
            .Select(status => new SelectListItem(status.ToString(), status.ToString()))
            .ToList();
    }

    private static IReadOnlyList<SelectListItem> GetChapterOptions(IReadOnlyList<DocumentDto> documents)
    {
        return documents
            .Where(document => document.ChapterId.HasValue && !string.IsNullOrWhiteSpace(document.ChapterName))
            .GroupBy(document => document.ChapterId!.Value)
            .Select(group => new SelectListItem(group.First().ChapterName, group.Key.ToString()))
            .ToList();
    }

    private CurrentUserDto GetCurrentUser()
    {
        return new CurrentUserDto
        {
            UserId = GetCurrentUserId(),
            Role = UserRole.Teacher
        };
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Current user id claim is missing.");
    }
}
