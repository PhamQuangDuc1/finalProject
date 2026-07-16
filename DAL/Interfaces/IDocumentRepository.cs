using DAL.Entities;

namespace DAL.Interfaces;

public interface IDocumentRepository
{
    Task<IReadOnlyList<Document>> GetAllForAdminAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> GetByTeacherAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> GetBySubjectIdsAsync(IReadOnlyCollection<int> subjectIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> GetIndexedActiveAsync(CancellationToken cancellationToken = default);

    Task<Document?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Document document, CancellationToken cancellationToken = default);

    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);

    Task ReplaceChunksInTransactionAsync(Document document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    Task UpdateContentInTransactionAsync(
        Document document,
        DocumentVersion? previousVersion,
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default);
}
