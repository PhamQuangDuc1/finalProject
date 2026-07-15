using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BLL.Services;

public class DocumentService : IDocumentService
{
    private const int MinimumArchiveDelayDays = 7;
    private const int MaxEditableContentLength = 200_000;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".pptx"
    };

    private readonly IDocumentRepository _documentRepository;
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly IChunkingService _chunkingService;

    public DocumentService(
        IDocumentRepository documentRepository,
        ITeacherSubjectRepository teacherSubjectRepository,
        IChunkingService chunkingService)
    {
        _documentRepository = documentRepository;
        _teacherSubjectRepository = teacherSubjectRepository;
        _chunkingService = chunkingService;
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsForAdminAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var documents = await _documentRepository.GetAllForAdminAsync(cancellationToken);
        await ApplyDueScheduledArchivesAsync(documents, cancellationToken);

        return documents.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsForTeacherAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        return await GetDocumentsForTeacherAsync(currentUser, new DocumentFilterDto(), cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsForTeacherAsync(CurrentUserDto currentUser, DocumentFilterDto filter, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Teacher);

        var documents = await _documentRepository.GetByTeacherAsync(currentUser.UserId, cancellationToken);
        await ApplyDueScheduledArchivesAsync(documents, cancellationToken);

        return documents
            .Where(document => !filter.SubjectId.HasValue || document.SubjectId == filter.SubjectId.Value)
            .Where(document => !filter.Status.HasValue || document.Status == filter.Status.Value)
            .Where(document => !filter.ChapterId.HasValue || document.ChapterId == filter.ChapterId.Value)
            .Where(document => string.IsNullOrWhiteSpace(filter.Search)
                || document.Title.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                || document.OriginalFileName.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                || (document.Description?.Contains(filter.Search, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(ToDto)
            .ToList();
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsForStudentAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Student);

        var documents = await _documentRepository.GetIndexedActiveAsync(cancellationToken);
        await ApplyDueScheduledArchivesAsync(documents, cancellationToken);

        return documents
            .Where(document => !IsScheduledArchiveDue(document))
            .Select(ToDto)
            .ToList();
    }

    public async Task<DocumentDto?> GetDocumentByIdAsync(CurrentUserDto currentUser, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);

        if (document is null)
        {
            return null;
        }

        await ApplyDueScheduledArchivesAsync(new[] { document }, cancellationToken);

        if (currentUser.Role == UserRole.Admin)
        {
            return ToDto(document);
        }

        if (currentUser.Role == UserRole.Teacher && document.UploadedByTeacherId == currentUser.UserId)
        {
            return ToDto(document);
        }

        if (currentUser.Role == UserRole.Student
            && document.Status == DocumentStatus.Indexed
            && !document.IsArchived
            && document.Subject?.IsActive == true)
        {
            return ToDto(document);
        }

        throw new UnauthorizedAccessException("The current user is not allowed to view this document.");
    }

    public async Task<DocumentDto?> GetEditableDocumentForTeacherAsync(int documentId, int teacherId, CancellationToken cancellationToken = default)
    {
        var currentUser = new CurrentUserDto { UserId = teacherId, Role = UserRole.Teacher };
        var document = await GetOwnedTeacherDocumentAsync(currentUser, documentId, cancellationToken);

        await EnsureStoredContentAsync(document, cancellationToken);

        return ToDto(document);
    }

    public async Task<DocumentUploadOptionsDto> GetUploadOptionsForTeacherAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Teacher);

        var assignments = await _teacherSubjectRepository.GetByTeacherAsync(currentUser.UserId, cancellationToken);
        var activeAssignments = assignments
            .Where(assignment => assignment.Subject?.IsActive == true)
            .ToList();

        return new DocumentUploadOptionsDto
        {
            Subjects = activeAssignments
                .Select(assignment => new SubjectOptionDto
                {
                    Id = assignment.SubjectId,
                    DisplayName = $"{assignment.Subject!.Code} - {assignment.Subject.Name}"
                })
                .ToList(),
            Chapters = activeAssignments
                .SelectMany(assignment => assignment.Subject!.Chapters.Select(chapter => new DocumentChapterOptionDto
                {
                    Id = chapter.Id,
                    SubjectId = chapter.SubjectId,
                    DisplayName = $"{assignment.Subject.Code} - {chapter.Name}"
                }))
                .ToList()
        };
    }

    public async Task<int> UploadDocumentAsync(CurrentUserDto currentUser, CreateDocumentDto document, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Teacher);

        if (document.UploadedByTeacherId != currentUser.UserId)
        {
            throw new UnauthorizedAccessException("Teachers can only upload documents as themselves.");
        }

        if (!await _teacherSubjectRepository.ExistsAsync(currentUser.UserId, document.SubjectId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Teachers can only upload documents to assigned Subjects.");
        }

        await ValidateChapterForTeacherSubjectAsync(currentUser, document.SubjectId, document.ChapterId, cancellationToken);

        var extension = Path.GetExtension(document.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only PDF, DOCX, or PPTX files are allowed.");
        }

        if (document.FileContent.Length == 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }

        if (string.IsNullOrWhiteSpace(document.StorageRootPath))
        {
            throw new InvalidOperationException("Document storage path was not provided.");
        }

        Directory.CreateDirectory(document.StorageRootPath);
        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(document.StorageRootPath, storedFileName);

        var entity = new Document
        {
            SubjectId = document.SubjectId,
            ChapterId = document.ChapterId,
            UploadedByTeacherId = currentUser.UserId,
            Title = document.Title,
            Description = document.Description,
            OriginalFileName = document.FileName,
            StoredFileName = storedFileName,
            FilePath = filePath,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            Status = DocumentStatus.Processing,
            ContentVersion = 1,
            UploadedAt = DateTime.UtcNow
        };

        await _documentRepository.AddAsync(entity, cancellationToken);

        try
        {
            await File.WriteAllBytesAsync(filePath, document.FileContent, cancellationToken);
            var extractedText = ExtractText(document.FileContent, extension);
            entity.ExtractedContent = extractedText;
            var chunks = await _chunkingService.CreateChunksAsync(entity.Id, extractedText, cancellationToken);

            entity.Chunks.Clear();
            foreach (var chunk in chunks)
            {
                entity.Chunks.Add(chunk);
            }

            entity.Status = DocumentStatus.Indexed;
            entity.ErrorMessage = null;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            entity.Status = DocumentStatus.Failed;
            entity.ErrorMessage = ex.Message;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _documentRepository.UpdateAsync(entity, cancellationToken);

        return entity.Id;
    }

    public async Task UpdateDocumentAsync(CurrentUserDto currentUser, UpdateDocumentDto document, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedTeacherDocumentAsync(currentUser, document.Id, cancellationToken);
        await ValidateChapterForTeacherSubjectAsync(currentUser, document.SubjectId, document.ChapterId, cancellationToken);

        entity.SubjectId = document.SubjectId;
        entity.Title = document.Title;
        entity.Description = document.Description;
        entity.ChapterId = document.ChapterId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _documentRepository.UpdateAsync(entity, cancellationToken);
    }

    public async Task ArchiveDocumentAsync(
        CurrentUserDto currentUser,
        int documentId,
        DateTime scheduledArchiveAtUtc,
        CancellationToken cancellationToken = default)
    public async Task UpdateDocumentContentAsync(
        int documentId,
        int teacherId,
        string title,
        int subjectId,
        int? chapterId,
        string? description,
        string content,
        CancellationToken cancellationToken = default)
    {
        var currentUser = new CurrentUserDto { UserId = teacherId, Role = UserRole.Teacher };
        var document = await GetOwnedTeacherDocumentAsync(currentUser, documentId, cancellationToken);

        await ValidateChapterForTeacherSubjectAsync(currentUser, subjectId, chapterId, cancellationToken);

        var normalizedTitle = string.IsNullOrWhiteSpace(title)
            ? throw new InvalidOperationException("Tiêu đề không được để trống.")
            : title.Trim();
        var normalizedContent = NormalizeEditableContent(content);
        var previousContent = GetWorkingContent(document);
        var previousVersionNumber = Math.Max(1, document.ContentVersion);

        var now = DateTime.UtcNow;
        var previousVersion = string.IsNullOrWhiteSpace(previousContent)
            ? null
            : new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = previousVersionNumber,
                Content = previousContent,
                UpdatedByTeacherId = teacherId,
                UpdatedAt = now,
                ChangeNote = "Nội dung trước khi giảng viên chỉnh sửa."
            };

        document.Title = normalizedTitle;
        document.SubjectId = subjectId;
        document.ChapterId = chapterId;
        document.Description = description;
        document.EditedContent = normalizedContent;
        document.HasManualEdits = true;
        document.ContentUpdatedAt = now;
        document.ContentUpdatedByTeacherId = teacherId;
        document.ContentVersion = previousVersionNumber + 1;
        document.Status = DocumentStatus.Processing;
        document.ErrorMessage = null;
        document.UpdatedAt = now;

        try
        {
            var chunks = await _chunkingService.CreateChunksAsync(document.Id, normalizedContent, cancellationToken);

            document.Status = DocumentStatus.Indexed;
            document.ErrorMessage = null;
            document.UpdatedAt = DateTime.UtcNow;

            await _documentRepository.UpdateContentInTransactionAsync(document, previousVersion, chunks, cancellationToken);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            document.UpdatedAt = DateTime.UtcNow;

            await _documentRepository.UpdateAsync(document, cancellationToken);
            throw new InvalidOperationException("Không thể cập nhật nội dung tài liệu.", ex);
        }
    }

    public async Task ArchiveDocumentAsync(CurrentUserDto currentUser, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await GetOwnedTeacherDocumentAsync(currentUser, documentId, cancellationToken);

        if (document.IsArchived)
        {
            return;
        }

        var minimumArchiveAt = DateTime.UtcNow.AddDays(MinimumArchiveDelayDays);
        if (scheduledArchiveAtUtc < minimumArchiveAt)
        {
            throw new InvalidOperationException("Thoi gian an tai lieu phai cach hien tai it nhat 7 ngay.");
        }

        document.ScheduledArchiveAt = scheduledArchiveAtUtc;
        document.ScheduledArchiveByTeacherId = currentUser.UserId;
        document.UpdatedAt = DateTime.UtcNow;

        await _documentRepository.UpdateAsync(document, cancellationToken);
    }

    public async Task ReindexDocumentAsync(CurrentUserDto currentUser, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await GetOwnedTeacherDocumentAsync(currentUser, documentId, cancellationToken);

        if (document.IsArchived)
        {
            throw new InvalidOperationException("Archived documents cannot be re-indexed.");
        }

        var now = DateTime.UtcNow;
        document.Status = DocumentStatus.Processing;
        document.ErrorMessage = null;
        document.UpdatedAt = now;

        try
        {
            var workingContent = GetWorkingContent(document);
            if (string.IsNullOrWhiteSpace(workingContent))
            {
                if (!File.Exists(document.FilePath))
                {
                    throw new FileNotFoundException("The stored document file could not be found.", document.FilePath);
                }

                var content = await File.ReadAllBytesAsync(document.FilePath, cancellationToken);
                var extension = Path.GetExtension(document.OriginalFileName);
                workingContent = ExtractText(content, extension);
                document.ExtractedContent = workingContent;
            }

            var chunks = await _chunkingService.CreateChunksAsync(document.Id, workingContent, cancellationToken);

            document.Status = DocumentStatus.Indexed;
            document.ErrorMessage = null;
            document.UpdatedAt = DateTime.UtcNow;

            await _documentRepository.ReplaceChunksInTransactionAsync(document, chunks, cancellationToken);
        }
        catch (Exception ex)
        {
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            document.UpdatedAt = DateTime.UtcNow;

            await _documentRepository.UpdateAsync(document, cancellationToken);
        }
    }

    public Task<IReadOnlyList<DocumentDto>> GetDocumentsForAdminAsync(CancellationToken cancellationToken = default)
    {
        return GetDocumentsForAdminAsync(new CurrentUserDto { UserId = 1, Role = UserRole.Admin }, cancellationToken);
    }

    public Task<IReadOnlyList<DocumentDto>> GetDocumentsForTeacherAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        return GetDocumentsForTeacherAsync(new CurrentUserDto { UserId = teacherId, Role = UserRole.Teacher }, cancellationToken);
    }

    public Task<int> RegisterDocumentAsync(CreateDocumentDto document, CancellationToken cancellationToken = default)
    {
        return UploadDocumentAsync(new CurrentUserDto { UserId = document.UploadedByTeacherId, Role = UserRole.Teacher }, document, cancellationToken);
    }

    private async Task<Document> GetOwnedTeacherDocumentAsync(CurrentUserDto currentUser, int documentId, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new InvalidOperationException("Document was not found.");

        AuthorizationGuard.RequireDocumentOwner(currentUser, document);

        return document;
    }

    private async Task ValidateChapterForTeacherSubjectAsync(
        CurrentUserDto currentUser,
        int subjectId,
        int? chapterId,
        CancellationToken cancellationToken)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Teacher);

        var assignments = await _teacherSubjectRepository.GetByTeacherAsync(currentUser.UserId, cancellationToken);
        var assignedSubject = assignments
            .Select(assignment => assignment.Subject)
            .FirstOrDefault(subject => subject?.Id == subjectId && subject.IsActive);

        if (assignedSubject is null)
        {
            throw new UnauthorizedAccessException("Teachers can only use assigned active Subjects.");
        }

        if (chapterId.HasValue && assignedSubject.Chapters.All(chapter => chapter.Id != chapterId.Value))
        {
            throw new InvalidOperationException("Selected Chapter does not belong to the selected Subject.");
        }
    }

    private static DocumentDto ToDto(Document document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            SubjectId = document.SubjectId,
            SubjectName = document.Subject?.Name ?? string.Empty,
            DepartmentName = document.Subject?.Department?.Name ?? string.Empty,
            UploadedByTeacherId = document.UploadedByTeacherId,
            UploadedByTeacherName = document.UploadedByTeacher?.FullName ?? string.Empty,
            ChapterId = document.ChapterId,
            ChapterName = document.Chapter?.Name ?? string.Empty,
            Title = document.Title,
            Description = document.Description,
            FileName = document.OriginalFileName,
            FilePath = document.FilePath,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            Status = document.Status.ToString(),
            ChunkCount = document.Chunks.Count,
            Chunks = document.Chunks
                .OrderBy(chunk => chunk.ChunkIndex)
                .Select(chunk => new DocumentChunkDto
                {
                    Id = chunk.Id,
                    ChunkIndex = chunk.ChunkIndex,
                    Content = chunk.Content,
                    StartPosition = chunk.StartPosition,
                    EndPosition = chunk.EndPosition,
                    WordCount = chunk.WordCount,
                    TokenCount = chunk.TokenCount,
                    CreatedAtUtc = chunk.CreatedAt
                })
                .ToList(),
            IsArchived = document.IsArchived,
            UploadedAtUtc = document.UploadedAt,
            UpdatedAtUtc = document.UpdatedAt,
            ArchivedAtUtc = document.ArchivedAt,
            ArchivedByTeacherId = document.ArchivedByTeacherId,
            ErrorMessage = document.ErrorMessage,
            CurrentContent = GetWorkingContent(document),
            HasManualEdits = document.HasManualEdits,
            ContentUpdatedAtUtc = document.ContentUpdatedAt,
            ContentUpdatedByTeacherId = document.ContentUpdatedByTeacherId,
            ContentUpdatedByTeacherName = document.ContentUpdatedByTeacher?.FullName ?? string.Empty,
            ContentVersion = document.ContentVersion,
            Versions = document.Versions
                .OrderByDescending(version => version.VersionNumber)
                .Select(version => new DocumentVersionDto
                {
                    Id = version.Id,
                    VersionNumber = version.VersionNumber,
                    Content = version.Content,
                    UpdatedByTeacherId = version.UpdatedByTeacherId,
                    UpdatedByTeacherName = version.UpdatedByTeacher?.FullName ?? string.Empty,
                    UpdatedAtUtc = version.UpdatedAt,
                    ChangeNote = version.ChangeNote
                })
                .ToList()
        };
    }

    private async Task EnsureStoredContentAsync(Document document, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(document.EditedContent) || !string.IsNullOrWhiteSpace(document.ExtractedContent))
        {
            return;
        }

        var contentFromChunks = BuildContentFromChunks(document);
        if (string.IsNullOrWhiteSpace(contentFromChunks))
        {
            return;
        }

        document.ExtractedContent = contentFromChunks;
        document.UpdatedAt = DateTime.UtcNow;

        await _documentRepository.UpdateAsync(document, cancellationToken);
    }

    private static string NormalizeEditableContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Nội dung tài liệu không được để trống.");
        }

        var normalized = content.Trim();
        if (normalized.Length > MaxEditableContentLength)
        {
            throw new InvalidOperationException($"Nội dung tài liệu không được vượt quá {MaxEditableContentLength:N0} ký tự.");
        }

        return normalized;
    }

    private static string GetWorkingContent(Document document)
    {
        if (!string.IsNullOrWhiteSpace(document.EditedContent))
        {
            return document.EditedContent;
        }

        if (!string.IsNullOrWhiteSpace(document.ExtractedContent))
        {
            return document.ExtractedContent;
        }

        return BuildContentFromChunks(document);
    }

    private static string BuildContentFromChunks(Document document)
    {
        return string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            document.Chunks
                .OrderBy(chunk => chunk.ChunkIndex)
                .Select(chunk => chunk.Content)
                .Where(content => !string.IsNullOrWhiteSpace(content)));
    }

    private static string ExtractText(byte[] content, string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".docx" => ExtractOpenXmlText(content, "word/document.xml"),
            ".pptx" => ExtractOpenXmlText(content, "ppt/slides/slide"),
            ".pdf" => ExtractPdfText(content),
            _ => throw new InvalidOperationException("Unsupported document type.")
        };
    }

    private static string ExtractOpenXmlText(byte[] content, string entryPrefix)
    {
        using var stream = new MemoryStream(content);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var builder = new StringBuilder();

        foreach (var entry in archive.Entries.Where(entry => entry.FullName.StartsWith(entryPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            using var entryStream = entry.Open();
            var xml = XDocument.Load(entryStream);
            var textNodes = xml.Descendants()
                .Where(element => element.Name.LocalName == "t")
                .Select(element => element.Value);

            builder.AppendLine(string.Join(" ", textNodes));
        }

        return NormalizeExtractedText(builder.ToString());
    }

    private static string ExtractPdfText(byte[] content)
    {
        var raw = Encoding.UTF8.GetString(content);
        raw = Regex.Replace(raw, @"\\[rn]", " ");
        raw = Regex.Replace(raw, @"[^\u0009\u000A\u000D\u0020-\u007EÀ-ỹ]+", " ");

        return NormalizeExtractedText(raw);
    }

    private static string NormalizeExtractedText(string text)
    {
        text = Regex.Replace(text, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Could not extract text from the uploaded document.");
        }

        return text;
    }

}
