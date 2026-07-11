using BLL.DTOs;

namespace BLL.Interfaces;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentDto>> GetDocumentsForAdminAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetDocumentsForTeacherAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<int> RegisterDocumentAsync(CreateDocumentDto document, CancellationToken cancellationToken = default);
}
