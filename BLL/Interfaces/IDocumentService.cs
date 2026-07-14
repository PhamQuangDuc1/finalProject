using BLL.DTOs;

namespace BLL.Interfaces;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentDto>> GetDocumentsForAdminAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetDocumentsForTeacherAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetDocumentsForTeacherAsync(CurrentUserDto currentUser, DocumentFilterDto filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetDocumentsForStudentAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default);

    Task<DocumentDto?> GetDocumentByIdAsync(CurrentUserDto currentUser, int documentId, CancellationToken cancellationToken = default);

    Task<DocumentDto?> GetEditableDocumentForTeacherAsync(int documentId, int teacherId, CancellationToken cancellationToken = default);

    Task<DocumentUploadOptionsDto> GetUploadOptionsForTeacherAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default);

    Task<int> UploadDocumentAsync(CurrentUserDto currentUser, CreateDocumentDto document, CancellationToken cancellationToken = default);

    Task UpdateDocumentAsync(CurrentUserDto currentUser, UpdateDocumentDto document, CancellationToken cancellationToken = default);

    Task UpdateDocumentContentAsync(
        int documentId,
        int teacherId,
        string title,
        int subjectId,
        int? chapterId,
        string? description,
        string content,
        CancellationToken cancellationToken = default);

    Task ArchiveDocumentAsync(CurrentUserDto currentUser, int documentId, CancellationToken cancellationToken = default);

    Task ReindexDocumentAsync(CurrentUserDto currentUser, int documentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetDocumentsForAdminAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetDocumentsForTeacherAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<int> RegisterDocumentAsync(CreateDocumentDto document, CancellationToken cancellationToken = default);
}
