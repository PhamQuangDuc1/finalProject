using BLL.DTOs;
using BLL.Interfaces;
using BLL.Services;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Tests;

public class DocumentServiceTests
{
    [Fact]
    public async Task GetDocumentsForAdminAsync_ReturnsDepartmentAndChunkCount()
    {
        var documents = new[]
        {
            CreateDocument(
                id: 1,
                teacherId: 2,
                status: DocumentStatus.Indexed,
                isArchived: false,
                title: "Lecture 1",
                subjectId: 10,
                subjectName: "PRN222",
                departmentName: "Software Engineering",
                chunkCount: 3)
        };
        var service = new DocumentService(new FakeDocumentRepository(documents), new FakeTeacherSubjectRepository(), new FakeSystemSettingService());

        var result = await service.GetDocumentsForAdminAsync(new CurrentUserDto { UserId = 1, Role = UserRole.Admin });

        var document = Assert.Single(result);
        Assert.Equal("Software Engineering", document.DepartmentName);
        Assert.Equal(3, document.ChunkCount);
        Assert.Equal("PRN222", document.SubjectName);
    }

    [Fact]
    public async Task GetDocumentsForTeacherAsync_AppliesTeacherFiltersInService()
    {
        var documents = new[]
        {
            CreateDocument(1, 2, DocumentStatus.Indexed, false, "Match Me", 10, "PRN222", "Software Engineering", 1),
            CreateDocument(2, 2, DocumentStatus.Failed, false, "Wrong Status", 10, "PRN222", "Software Engineering", 1),
            CreateDocument(3, 2, DocumentStatus.Indexed, false, "Wrong Subject", 11, "SWE201", "Software Engineering", 1),
            CreateDocument(4, 3, DocumentStatus.Indexed, false, "Other Teacher", 10, "PRN222", "Software Engineering", 1)
        };
        var service = new DocumentService(new FakeDocumentRepository(documents), new FakeTeacherSubjectRepository(), new FakeSystemSettingService());

        var result = await service.GetDocumentsForTeacherAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            new DocumentFilterDto { SubjectId = 10, Status = DocumentStatus.Indexed, Search = "match" });

        var document = Assert.Single(result);
        Assert.Equal(1, document.Id);
        Assert.Equal(2, document.UploadedByTeacherId);
    }

    [Fact]
    public async Task UploadDocumentAsync_IndexesChunksUsingSystemSetting_WhenTeacherIsAssigned()
    {
        var repository = new FakeDocumentRepository(Array.Empty<Document>());
        var service = new DocumentService(
            repository,
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }),
            new FakeSystemSettingService(chunkSize: 5, chunkOverlap: 1));

        var documentId = await service.UploadDocumentAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            new CreateDocumentDto
            {
                SubjectId = 10,
                UploadedByTeacherId = 2,
                Title = "Upload Test",
                FileName = "upload.pdf",
                ContentType = "application/pdf",
                FileSize = 36,
                FileContent = "one two three four five six seven eight"u8.ToArray(),
                StorageRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
            });

        var saved = Assert.Single(repository.SavedDocuments);
        Assert.Equal(saved.Id, documentId);
        Assert.Equal(DocumentStatus.Indexed, saved.Status);
        Assert.Equal(2, saved.UploadedByTeacherId);
        Assert.Equal(10, saved.SubjectId);
        Assert.True(saved.Chunks.Count >= 2);
        Assert.All(saved.Chunks, chunk => Assert.True(chunk.TokenCount <= 5));
    }

    [Fact]
    public async Task UploadDocumentAsync_Throws_WhenTeacherUploadsToUnassignedSubject()
    {
        var service = new DocumentService(
            new FakeDocumentRepository(Array.Empty<Document>()),
            new FakeTeacherSubjectRepository(assignedSubjectIds: Array.Empty<int>()),
            new FakeSystemSettingService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UploadDocumentAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            new CreateDocumentDto
            {
                SubjectId = 99,
                UploadedByTeacherId = 2,
                Title = "Forbidden",
                FileName = "forbidden.pdf",
                ContentType = "application/pdf",
                FileContent = "content"u8.ToArray(),
                StorageRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
            }));
    }

    private static Document CreateDocument(
        int id,
        int teacherId,
        DocumentStatus status,
        bool isArchived,
        string title,
        int subjectId,
        string subjectName,
        string departmentName,
        int chunkCount)
    {
        return new Document
        {
            Id = id,
            Title = title,
            OriginalFileName = $"{title}.pdf",
            SubjectId = subjectId,
            UploadedByTeacherId = teacherId,
            UploadedByTeacher = new User { Id = teacherId, FullName = $"Teacher {teacherId}", Role = UserRole.Teacher },
            Subject = new Subject
            {
                Id = subjectId,
                Name = subjectName,
                IsActive = true,
                Department = new Department { Name = departmentName }
            },
            Status = status,
            IsArchived = isArchived,
            UploadedAt = DateTime.UtcNow,
            Chunks = Enumerable.Range(1, chunkCount)
                .Select(index => new DocumentChunk { Id = index, ChunkIndex = index - 1 })
                .ToList()
        };
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        private readonly IReadOnlyList<Document> _documents;

        public List<Document> SavedDocuments { get; } = new();

        public FakeDocumentRepository(IReadOnlyList<Document> documents)
        {
            _documents = documents;
        }

        public Task<IReadOnlyList<Document>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_documents);
        }

        public Task<IReadOnlyList<Document>> GetByTeacherAsync(int teacherId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Document>>(_documents.Where(document => document.UploadedByTeacherId == teacherId).ToList());
        }

        public Task<IReadOnlyList<Document>> GetIndexedActiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Document>>(_documents
                .Where(document => document.Status == DocumentStatus.Indexed && !document.IsArchived && document.Subject?.IsActive == true)
                .ToList());
        }

        public Task<Document?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_documents.FirstOrDefault(document => document.Id == id));
        }

        public Task AddAsync(Document document, CancellationToken cancellationToken = default)
        {
            document.Id = _documents.Count + SavedDocuments.Count + 1;
            SavedDocuments.Add(document);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTeacherSubjectRepository : ITeacherSubjectRepository
    {
        private readonly HashSet<int> _assignedSubjectIds;

        public FakeTeacherSubjectRepository(IEnumerable<int>? assignedSubjectIds = null)
        {
            _assignedSubjectIds = assignedSubjectIds?.ToHashSet() ?? new HashSet<int> { 10 };
        }

        public Task<IReadOnlyList<TeacherSubject>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TeacherSubject>>(Array.Empty<TeacherSubject>());
        }

        public Task<IReadOnlyList<TeacherSubject>> GetByTeacherAsync(int teacherId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TeacherSubject>>(Array.Empty<TeacherSubject>());
        }

        public Task<bool> ExistsAsync(int teacherId, int subjectId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_assignedSubjectIds.Contains(subjectId));
        }

        public Task AddAsync(TeacherSubject teacherSubject, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeSystemSettingService : ISystemSettingService
    {
        private readonly int _chunkSize;
        private readonly int _chunkOverlap;

        public FakeSystemSettingService(int chunkSize = 1100, int chunkOverlap = 150)
        {
            _chunkSize = chunkSize;
            _chunkOverlap = chunkOverlap;
        }

        public Task<SystemSettingDto> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SystemSettingDto
            {
                Id = 1,
                ChunkStrategy = ChunkStrategy.FixedSize,
                ChunkSize = _chunkSize,
                ChunkOverlap = _chunkOverlap,
                TopK = 5,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public Task UpdateAsync(CurrentUserDto currentUser, UpdateSystemSettingDto setting, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
