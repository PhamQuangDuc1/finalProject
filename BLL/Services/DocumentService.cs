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
    private readonly ISystemSettingService _systemSettingService;

    public DocumentService(
        IDocumentRepository documentRepository,
        ITeacherSubjectRepository teacherSubjectRepository,
        ISystemSettingService systemSettingService)
    {
        _documentRepository = documentRepository;
        _teacherSubjectRepository = teacherSubjectRepository;
        _systemSettingService = systemSettingService;
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

        if (currentUser.Role == UserRole.Student && document.Status == DocumentStatus.Indexed && !document.IsArchived)
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
            var setting = await _systemSettingService.GetCurrentAsync(cancellationToken);
            var chunks = CreateChunks(entity.Id, extractedText, setting);

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

        entity.Title = document.Title;
        entity.Description = document.Description;
        entity.ChapterId = document.ChapterId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _documentRepository.UpdateAsync(entity, cancellationToken);
    }

    public async Task ArchiveDocumentAsync(CurrentUserDto currentUser, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await GetOwnedTeacherDocumentAsync(currentUser, documentId, cancellationToken);

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

        document.Status = DocumentStatus.Processing;
        document.ErrorMessage = null;
        document.UpdatedAt = DateTime.UtcNow;

        await _documentRepository.UpdateAsync(document, cancellationToken);
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
            FileName = document.OriginalFileName,
            FilePath = document.FilePath,
            FileSize = document.FileSize,
            Status = document.Status.ToString(),
            ChunkCount = document.Chunks.Count,
            IsArchived = document.IsArchived,
            UploadedAtUtc = document.UploadedAt
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

    private static IReadOnlyList<DocumentChunk> CreateChunks(int documentId, string text, SystemSettingDto setting)
    {
        return setting.ChunkStrategy switch
        {
            ChunkStrategy.Paragraph => CreateParagraphChunks(documentId, text, setting),
            ChunkStrategy.Hybrid => CreateParagraphChunks(documentId, text, setting),
            _ => CreateFixedSizeChunks(documentId, text, setting)
        };
    }

    private static IReadOnlyList<DocumentChunk> CreateFixedSizeChunks(int documentId, string text, SystemSettingDto setting)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunkSize = Math.Max(1, setting.ChunkSize);
        var overlap = Math.Clamp(setting.ChunkOverlap, 0, Math.Max(0, chunkSize - 1));
        var step = Math.Max(1, chunkSize - overlap);
        var chunks = new List<DocumentChunk>();

        for (var start = 0; start < words.Length; start += step)
        {
            var chunkWords = words.Skip(start).Take(chunkSize).ToArray();
            if (chunkWords.Length == 0)
            {
                break;
            }

            chunks.Add(CreateChunk(documentId, chunks.Count, string.Join(' ', chunkWords), start, start + chunkWords.Length));

            if (start + chunkWords.Length >= words.Length)
            {
                break;
            }
        }

        return chunks;
    }

    private static IReadOnlyList<DocumentChunk> CreateParagraphChunks(int documentId, string text, SystemSettingDto setting)
    {
        var paragraphs = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (paragraphs.Length <= 1)
        {
            return CreateFixedSizeChunks(documentId, text, setting);
        }

        var chunks = new List<DocumentChunk>();
        foreach (var paragraph in paragraphs)
        {
            chunks.AddRange(CreateFixedSizeChunks(documentId, paragraph, setting)
                .Select(chunk =>
                {
                    chunk.ChunkIndex = chunks.Count;
                    return chunk;
                }));
        }

        return chunks;
    }

    private static DocumentChunk CreateChunk(int documentId, int index, string content, int startPosition, int endPosition)
    {
        var tokenCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return new DocumentChunk
        {
            DocumentId = documentId,
            ChunkIndex = index,
            Content = content,
            StartPosition = startPosition,
            EndPosition = endPosition,
            WordCount = tokenCount,
            TokenCount = tokenCount,
            CreatedAt = DateTime.UtcNow
        };
    }
}
