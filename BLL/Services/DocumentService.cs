using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;

    public DocumentService(IDocumentRepository documentRepository, ITeacherSubjectRepository teacherSubjectRepository)
    {
        _documentRepository = documentRepository;
        _teacherSubjectRepository = teacherSubjectRepository;
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsForAdminAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var documents = await _documentRepository.GetAllForAdminAsync(cancellationToken);

        return documents.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsForTeacherAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Teacher);

        var documents = await _documentRepository.GetByTeacherAsync(currentUser.UserId, cancellationToken);

        return documents.Select(ToDto).ToList();
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

        var entity = new Document
        {
            SubjectId = document.SubjectId,
            UploadedByTeacherId = currentUser.UserId,
            Title = document.Title,
            OriginalFileName = document.FileName,
            StoredFileName = document.FileName,
            FilePath = string.Empty,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            Status = DocumentStatus.Uploading,
            UploadedAt = DateTime.UtcNow
        };

        await _documentRepository.AddAsync(entity, cancellationToken);

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
            UploadedByTeacherName = document.UploadedByTeacher?.FullName ?? string.Empty,
            Title = document.Title,
            FileName = document.OriginalFileName,
            FileSize = document.FileSize,
            Status = document.Status.ToString(),
            UploadedAtUtc = document.UploadedAt
        };
    }
}
