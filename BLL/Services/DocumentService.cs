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

        return documents.Select(ToDto).ToList();
    }

    public async Task<DocumentDto?> GetDocumentByIdAsync(CurrentUserDto currentUser, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);

        if (document is null)
        {
            return null;
        }

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
            UploadedAt = DateTime.UtcNow
        };

        await _documentRepository.AddAsync(entity, cancellationToken);

        try
        {
            await File.WriteAllBytesAsync(filePath, document.FileContent, cancellationToken);
            var extractedText = ExtractText(document.FileContent, extension);
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

    public async Task ArchiveDocumentAsync(CurrentUserDto currentUser, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await GetOwnedTeacherDocumentAsync(currentUser, documentId, cancellationToken);

        if (document.IsArchived)
        {
            return;
        }

        document.IsArchived = true;
        document.Status = DocumentStatus.Archived;
        document.ArchivedAt = DateTime.UtcNow;
        document.ArchivedByTeacherId = currentUser.UserId;
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
            if (!File.Exists(document.FilePath))
            {
                throw new FileNotFoundException("The stored document file could not be found.", document.FilePath);
            }

            var content = await File.ReadAllBytesAsync(document.FilePath, cancellationToken);
            var extension = Path.GetExtension(document.OriginalFileName);
            var extractedText = ExtractText(content, extension);
            var chunks = await _chunkingService.CreateChunksAsync(document.Id, extractedText, cancellationToken);

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
            IsArchived = document.IsArchived,
            UploadedAtUtc = document.UploadedAt,
            UpdatedAtUtc = document.UpdatedAt,
            ArchivedAtUtc = document.ArchivedAt,
            ArchivedByTeacherId = document.ArchivedByTeacherId,
            ErrorMessage = document.ErrorMessage
        };
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
