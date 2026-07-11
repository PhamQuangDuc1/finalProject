using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;

    public DocumentService(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsForAdminAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetAllForAdminAsync(cancellationToken);

        return documents.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsForTeacherAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetByTeacherAsync(teacherId, cancellationToken);

        return documents.Select(ToDto).ToList();
    }

    public async Task<int> RegisterDocumentAsync(CreateDocumentDto document, CancellationToken cancellationToken = default)
    {
        var entity = new Document
        {
            SubjectId = document.SubjectId,
            UploadedByTeacherId = document.UploadedByTeacherId,
            Title = document.Title,
            OriginalFileName = document.FileName,
            StoredFileName = document.FileName,
            FilePath = string.Empty,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            Status = DocumentStatus.Uploading
        };

        await _documentRepository.AddAsync(entity, cancellationToken);

        return entity.Id;
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
