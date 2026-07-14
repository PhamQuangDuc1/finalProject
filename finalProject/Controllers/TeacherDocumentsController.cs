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
    private readonly IHubContext<DocumentProcessingHub> _hubContext;

    public TeacherDocumentsController(
        IDocumentService documentService,
        IWebHostEnvironment environment,
        IHubContext<DocumentProcessingHub> hubContext)
    {
        _documentService = documentService;
        _environment = environment;
        _hubContext = hubContext;
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
            ModelState.AddModelError(nameof(model.File), "Tep tai len khong duoc rong.");
            await BuildUploadViewModelAsync(model, cancellationToken);
            return View(model);
        }

        var extension = Path.GetExtension(model.File.FileName);
        if (!AllowedUploadExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(model.File), "Chi cho phep tai len file PDF, DOCX hoac PPTX.");
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

        await BroadcastDocumentEventAsync("DocumentCreated", "Created", documentId, currentUser, cancellationToken);

        TempData["StatusMessage"] = $"Da upload tai lieu #{documentId}. He thong da xu ly chunk va index theo cau hinh hien tai.";

        return RedirectToAction(nameof(Index));
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

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        DocumentDto? document;
        var currentUser = GetCurrentUser();
        try
        {
            document = await _documentService.GetEditableDocumentForTeacherAsync(id, currentUser.UserId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        if (document is null)
        {
            return NotFound();
        }

        return View(await BuildEditViewModelAsync(new DocumentEditViewModel
        {
            Id = document.Id,
            SubjectId = document.SubjectId,
            ChapterId = document.ChapterId,
            Title = document.Title,
            Description = document.Description,
            Content = document.CurrentContent,
            OriginalFileName = document.FileName,
            CurrentStatus = document.Status,
            ContentVersion = document.ContentVersion
        }, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DocumentEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildEditViewModelAsync(model, cancellationToken));
        }

        var currentUser = GetCurrentUser();
        try
        {
            await _documentService.UpdateDocumentContentAsync(
                model.Id,
                currentUser.UserId,
                model.Title,
                model.SubjectId,
                model.ChapterId,
                model.Description,
                model.Content,
                cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(await BuildEditViewModelAsync(model, cancellationToken));
        }

        await BroadcastDocumentEventAsync("DocumentUpdated", "Cập nhật", model.Id, currentUser, cancellationToken);

        TempData["StatusMessage"] = "Đã cập nhật nội dung tài liệu thành công.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser();
        try
        {
            await _documentService.ArchiveDocumentAsync(currentUser, id, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        await BroadcastDocumentEventAsync("DocumentArchived", "Archived", id, currentUser, cancellationToken);

        TempData["StatusMessage"] = "Da archive tai lieu.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reindex(int id, CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser();
        try
        {
            await _documentService.ReindexDocumentAsync(currentUser, id, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            TempData["StatusMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        await BroadcastDocumentEventAsync("DocumentReindexed", "Re-indexed", id, currentUser, cancellationToken);

        TempData["StatusMessage"] = "Da re-index tai lieu theo cau hinh chunk hien tai.";

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
            TempData["StatusMessage"] = "Tep tai lieu chua co tren he thong luu tru.";
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

    private async Task<DocumentEditViewModel> BuildEditViewModelAsync(DocumentEditViewModel model, CancellationToken cancellationToken)
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

    private async Task BroadcastDocumentEventAsync(
        string eventName,
        string action,
        int documentId,
        CurrentUserDto currentUser,
        CancellationToken cancellationToken)
    {
        var document = await _documentService.GetDocumentByIdAsync(currentUser, documentId, cancellationToken);
        if (document is null)
        {
            return;
        }

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
            Action = action,
            Status = document.Status,
            OccurredAtUtc = DateTime.UtcNow
        };

        await _hubContext.Clients.Group(DocumentProcessingHub.AdminGroup).SendAsync(eventName, payload, cancellationToken);
        await _hubContext.Clients.Group(DocumentProcessingHub.GetTeacherGroupName(document.UploadedByTeacherId)).SendAsync(eventName, payload, cancellationToken);
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
