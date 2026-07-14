using DAL.Data;
using DAL.Entities;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _dbContext;

    public DocumentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Document>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .Include(document => document.Subject)
                .ThenInclude(subject => subject!.Department)
            .Include(document => document.UploadedByTeacher)
            .Include(document => document.ContentUpdatedByTeacher)
            .Include(document => document.Chapter)
            .Include(document => document.Chunks)
            .Include(document => document.Versions)
                .ThenInclude(version => version.UpdatedByTeacher)
            .OrderByDescending(document => document.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByTeacherAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .Include(document => document.Subject)
                .ThenInclude(subject => subject!.Department)
            .Include(document => document.UploadedByTeacher)
            .Include(document => document.ContentUpdatedByTeacher)
            .Include(document => document.Chapter)
            .Where(document => document.UploadedByTeacherId == teacherId)
            .Include(document => document.Chunks)
            .Include(document => document.Versions)
                .ThenInclude(version => version.UpdatedByTeacher)
            .OrderByDescending(document => document.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetIndexedActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .Include(document => document.Subject)
                .ThenInclude(subject => subject!.Department)
            .Include(document => document.UploadedByTeacher)
            .Include(document => document.ContentUpdatedByTeacher)
            .Include(document => document.Chapter)
            .Include(document => document.Chunks)
            .Include(document => document.Versions)
                .ThenInclude(version => version.UpdatedByTeacher)
            .Where(document => document.Status == DocumentStatus.Indexed
                && !document.IsArchived
                && document.Subject != null
                && document.Subject.IsActive)
            .OrderByDescending(document => document.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Document?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .Include(document => document.Subject)
                .ThenInclude(subject => subject!.Department)
            .Include(document => document.UploadedByTeacher)
            .Include(document => document.ContentUpdatedByTeacher)
            .Include(document => document.Chapter)
            .Include(document => document.Chunks)
            .Include(document => document.Versions)
                .ThenInclude(version => version.UpdatedByTeacher)
            .FirstOrDefaultAsync(document => document.Id == id, cancellationToken);
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        _dbContext.Documents.Update(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceChunksInTransactionAsync(Document document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (_dbContext.Entry(document).State == EntityState.Detached)
        {
            _dbContext.Documents.Attach(document);
        }

        var existingChunks = await _dbContext.DocumentChunks
            .Where(chunk => chunk.DocumentId == document.Id)
            .ToListAsync(cancellationToken);

        _dbContext.DocumentChunks.RemoveRange(existingChunks);
        document.Chunks.Clear();

        foreach (var chunk in chunks)
        {
            chunk.DocumentId = document.Id;
            _dbContext.DocumentChunks.Add(chunk);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateContentInTransactionAsync(
        Document document,
        DocumentVersion? previousVersion,
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (_dbContext.Entry(document).State == EntityState.Detached)
        {
            _dbContext.Documents.Attach(document);
        }

        if (previousVersion is not null)
        {
            _dbContext.DocumentVersions.Add(previousVersion);
        }

        var existingChunks = await _dbContext.DocumentChunks
            .Where(chunk => chunk.DocumentId == document.Id)
            .ToListAsync(cancellationToken);

        _dbContext.DocumentChunks.RemoveRange(existingChunks);
        document.Chunks.Clear();

        foreach (var chunk in chunks)
        {
            chunk.DocumentId = document.Id;
            _dbContext.DocumentChunks.Add(chunk);
        }

        _dbContext.Documents.Update(document);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
