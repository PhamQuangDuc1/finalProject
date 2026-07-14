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
        var service = new DocumentService(new FakeDocumentRepository(documents), new FakeTeacherSubjectRepository(), new FakeChunkingService());

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
        var service = new DocumentService(new FakeDocumentRepository(documents), new FakeTeacherSubjectRepository(), new FakeChunkingService());

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
            new FakeChunkingService(chunkSize: 5, chunkOverlap: 1));

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
            new FakeChunkingService());

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

    [Fact]
    public async Task GetDocumentByIdAsync_ThrowsForStudent_WhenSubjectIsInactive()
    {
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Inactive", 10, "PRN222", "Software Engineering", 1);
        document.Subject!.IsActive = false;
        var service = new DocumentService(new FakeDocumentRepository(new[] { document }), new FakeTeacherSubjectRepository(), new FakeChunkingService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetDocumentByIdAsync(
            new CurrentUserDto { UserId = 4, Role = UserRole.Student },
            document.Id));
    }

    [Fact]
    public async Task UpdateDocumentAsync_Throws_WhenTeacherSelectsUnassignedSubject()
    {
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Owned", 10, "PRN222", "Software Engineering", 1);
        var service = new DocumentService(
            new FakeDocumentRepository(new[] { document }),
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }),
            new FakeChunkingService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateDocumentAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            new UpdateDocumentDto
            {
                Id = document.Id,
                SubjectId = 99,
                Title = "New title"
            }));
    }

    [Fact]
    public async Task GetEditableDocumentForTeacherAsync_StoresContentFromChunks_WhenFullContentMissing()
    {
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Owned", 10, "PRN222", "Software Engineering", 0);
        document.Chunks = new List<DocumentChunk>
        {
            new() { ChunkIndex = 1, Content = "second chunk" },
            new() { ChunkIndex = 0, Content = "first chunk" }
        };
        var repository = new FakeDocumentRepository(new[] { document });
        var service = new DocumentService(repository, new FakeTeacherSubjectRepository(), new FakeChunkingService());

        var result = await service.GetEditableDocumentForTeacherAsync(document.Id, 2);

        Assert.NotNull(result);
        Assert.Equal($"first chunk{Environment.NewLine}{Environment.NewLine}second chunk", result.CurrentContent);
        Assert.Equal(result.CurrentContent, document.ExtractedContent);
        Assert.Equal(1, repository.UpdateCalls);
    }

    [Fact]
    public async Task GetEditableDocumentForTeacherAsync_Throws_WhenTeacherDoesNotOwnDocument()
    {
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Owned", 10, "PRN222", "Software Engineering", 1);
        var service = new DocumentService(new FakeDocumentRepository(new[] { document }), new FakeTeacherSubjectRepository(), new FakeChunkingService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetEditableDocumentForTeacherAsync(document.Id, 3));
    }

    [Fact]
    public async Task UpdateDocumentContentAsync_ReplacesChunksAndStoresManualEdit()
    {
        var storagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        await File.WriteAllTextAsync(storagePath, "original file still exists");
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Owned", 10, "PRN222", "Software Engineering", 1);
        document.FilePath = storagePath;
        document.ExtractedContent = "old extracted content";
        var repository = new FakeDocumentRepository(new[] { document });
        var service = new DocumentService(
            repository,
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }),
            new FakeChunkingService(chunkSize: 3, chunkOverlap: 0));

        await service.UpdateDocumentContentAsync(
            document.Id,
            teacherId: 2,
            title: "Updated title",
            subjectId: 10,
            chapterId: null,
            description: "Updated description",
            content: "alpha beta gamma delta epsilon");

        Assert.Empty(repository.SavedDocuments);
        Assert.True(File.Exists(storagePath));
        Assert.Equal("Updated title", document.Title);
        Assert.Equal("Updated description", document.Description);
        Assert.Equal("alpha beta gamma delta epsilon", document.EditedContent);
        Assert.True(document.HasManualEdits);
        Assert.Equal(2, document.ContentUpdatedByTeacherId);
        Assert.Equal(2, document.ContentVersion);
        Assert.Equal(DocumentStatus.Indexed, document.Status);
        Assert.Equal(1, repository.UpdateContentTransactionCalls);
        Assert.Equal(2, document.Chunks.Count);
        Assert.Equal("alpha beta gamma", document.Chunks.First().Content);
        Assert.Single(document.Versions);
        Assert.Equal(1, document.Versions.Single().VersionNumber);
        Assert.Equal("old extracted content", document.Versions.Single().Content);
    }

    [Fact]
    public async Task UpdateDocumentContentAsync_Throws_WhenTeacherDoesNotOwnDocument()
    {
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Owned", 10, "PRN222", "Software Engineering", 1);
        var service = new DocumentService(
            new FakeDocumentRepository(new[] { document }),
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }),
            new FakeChunkingService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateDocumentContentAsync(
            document.Id,
            teacherId: 3,
            title: "Updated title",
            subjectId: 10,
            chapterId: null,
            description: null,
            content: "updated content"));
    }

    [Fact]
    public async Task ReindexDocumentAsync_ReplacesChunksUsingLatestSystemSetting()
    {
        var storagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        await File.WriteAllTextAsync(storagePath, "one two three four five six seven eight");
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Owned", 10, "PRN222", "Software Engineering", 1);
        document.FilePath = storagePath;
        document.OriginalFileName = "owned.pdf";
        var repository = new FakeDocumentRepository(new[] { document });
        var service = new DocumentService(
            repository,
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }),
            new FakeChunkingService(chunkSize: 3, chunkOverlap: 0));

        await service.ReindexDocumentAsync(new CurrentUserDto { UserId = 2, Role = UserRole.Teacher }, document.Id);

        Assert.Equal(DocumentStatus.Indexed, document.Status);
        Assert.True(repository.ReplaceChunkTransactionCalls > 0);
        Assert.True(document.Chunks.Count >= 3);
        Assert.All(document.Chunks, chunk => Assert.True(chunk.TokenCount <= 3));
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

        public int UpdateCalls { get; private set; }

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
            UpdateCalls++;
            return Task.CompletedTask;
        }

        public int ReplaceChunkTransactionCalls { get; private set; }

        public int UpdateContentTransactionCalls { get; private set; }

        public Task ReplaceChunksInTransactionAsync(Document document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
        {
            ReplaceChunkTransactionCalls++;
            document.Chunks.Clear();
            foreach (var chunk in chunks)
            {
                document.Chunks.Add(chunk);
            }

            return Task.CompletedTask;
        }

        public Task UpdateContentInTransactionAsync(Document document, DocumentVersion? previousVersion, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
        {
            UpdateContentTransactionCalls++;
            if (previousVersion is not null)
            {
                document.Versions.Add(previousVersion);
            }

            document.Chunks.Clear();
            foreach (var chunk in chunks)
            {
                document.Chunks.Add(chunk);
            }

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
            var assignments = _assignedSubjectIds
                .Select(subjectId => new TeacherSubject
                {
                    TeacherId = teacherId,
                    SubjectId = subjectId,
                    Subject = new Subject
                    {
                        Id = subjectId,
                        IsActive = true,
                        Chapters = new List<Chapter>
                        {
                            new() { Id = subjectId * 100 + 1, SubjectId = subjectId, Name = "Chapter 1" }
                        }
                    }
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<TeacherSubject>>(assignments);
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

    private sealed class FakeChunkingService : IChunkingService
    {
        private readonly int _chunkSize;
        private readonly int _chunkOverlap;

        public FakeChunkingService(int chunkSize = 1100, int chunkOverlap = 150)
        {
            _chunkSize = chunkSize;
            _chunkOverlap = chunkOverlap;
        }

        public Task<SystemSettingDto> GetCurrentChunkSettingAsync(CancellationToken cancellationToken = default)
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

        public async Task<IReadOnlyList<DocumentChunk>> CreateChunksAsync(int documentId, string text, CancellationToken cancellationToken = default)
        {
            var setting = await GetCurrentChunkSettingAsync(cancellationToken);
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var step = Math.Max(1, setting.ChunkSize - setting.ChunkOverlap);
            var chunks = new List<DocumentChunk>();

            for (var start = 0; start < words.Length; start += step)
            {
                var chunkWords = words.Skip(start).Take(setting.ChunkSize).ToArray();
                if (chunkWords.Length == 0)
                {
                    break;
                }

                chunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    ChunkIndex = chunks.Count,
                    Content = string.Join(' ', chunkWords),
                    TokenCount = chunkWords.Length,
                    WordCount = chunkWords.Length
                });

                if (start + chunkWords.Length >= words.Length)
                {
                    break;
                }
            }

            return chunks;
        }
    }
}
