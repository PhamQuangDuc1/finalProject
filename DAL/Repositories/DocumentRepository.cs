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
            .Include(document => document.Chapter)
            .Include(document => document.Chunks)
            .OrderByDescending(document => document.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByTeacherAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .Include(document => document.Subject)
                .ThenInclude(subject => subject!.Department)
            .Include(document => document.UploadedByTeacher)
            .Include(document => document.Chapter)
            .Where(document => document.UploadedByTeacherId == teacherId)
            .Include(document => document.Chunks)
            .OrderByDescending(document => document.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetIndexedActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .Include(document => document.Subject)
                .ThenInclude(subject => subject!.Department)
            .Include(document => document.UploadedByTeacher)
            .Include(document => document.Chapter)
            .Include(document => document.Chunks)
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
            .Include(document => document.Chapter)
            .Include(document => document.Chunks)
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
}
