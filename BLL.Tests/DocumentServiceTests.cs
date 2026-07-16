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
    public async Task GetDocumentByIdAsync_ReturnsChunkDetailsInIndexOrder()
    {
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Lecture 1", 10, "PRN222", "Software Engineering", 0);
        document.Chunks = new List<DocumentChunk>
        {
            new()
            {
                Id = 11,
                ChunkIndex = 1,
                Content = "second chunk",
                StartPosition = 10,
                EndPosition = 22,
                TokenCount = 2,
                WordCount = 2,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 10,
                ChunkIndex = 0,
                Content = "first chunk",
                StartPosition = 0,
                EndPosition = 9,
                TokenCount = 2,
                WordCount = 2,
                CreatedAt = DateTime.UtcNow
            }
        };
        var service = new DocumentService(new FakeDocumentRepository(new[] { document }), new FakeTeacherSubjectRepository(), new FakeChunkingService());

        var result = await service.GetDocumentByIdAsync(new CurrentUserDto { UserId = 2, Role = UserRole.Teacher }, document.Id);

        Assert.NotNull(result);
        var chunksProperty = typeof(DocumentDto).GetProperty("Chunks");
        Assert.NotNull(chunksProperty);
        var chunks = Assert.IsAssignableFrom<IEnumerable<object>>(chunksProperty.GetValue(result));
        var chunkList = chunks.ToList();
        Assert.Equal(2, chunkList.Count);
        Assert.Equal(0, GetIntProperty(chunkList[0], "ChunkIndex"));
        Assert.Equal("first chunk", GetStringProperty(chunkList[0], "Content"));
        Assert.Equal(1, GetIntProperty(chunkList[1], "ChunkIndex"));
        Assert.Equal("second chunk", GetStringProperty(chunkList[1], "Content"));
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
    public async Task GetDocumentsForTeacherAsync_ReturnsDocumentsForAssignedSubjects()
    {
        var documents = new[]
        {
            CreateDocument(1, 5, DocumentStatus.Indexed, false, "Shared Subject", 10, "PRN222", "Software Engineering", 1),
            CreateDocument(2, 5, DocumentStatus.Indexed, false, "Other Subject", 11, "SWE201", "Software Engineering", 1)
        };
        var service = new DocumentService(
            new FakeDocumentRepository(documents),
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }),
            new FakeChunkingService());

        var result = await service.GetDocumentsForTeacherAsync(new CurrentUserDto { UserId = 2, Role = UserRole.Teacher });

        var document = Assert.Single(result);
        Assert.Equal(1, document.Id);
        Assert.Equal(5, document.UploadedByTeacherId);
    }

    [Fact]
    public async Task GetDocumentByIdAsync_AllowsAssignedTeacherToViewSubjectDocument()
    {
        var document = CreateDocument(1, 5, DocumentStatus.Indexed, false, "Shared Subject", 10, "PRN222", "Software Engineering", 1);
        var service = new DocumentService(
            new FakeDocumentRepository(new[] { document }),
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }),
            new FakeChunkingService());

        var result = await service.GetDocumentByIdAsync(new CurrentUserDto { UserId = 2, Role = UserRole.Teacher }, document.Id);

        Assert.NotNull(result);
        Assert.Equal(document.Id, result.Id);
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
    public async Task UploadDocumentAsync_Throws_WhenAssignedTeacherIsNotDepartmentManager()
    {
        var service = new DocumentService(
            new FakeDocumentRepository(Array.Empty<Document>()),
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }, managerTeacherId: 5),
            new FakeChunkingService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UploadDocumentAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            new CreateDocumentDto
            {
                SubjectId = 10,
                UploadedByTeacherId = 2,
                Title = "Forbidden",
                FileName = "forbidden.pdf",
                ContentType = "application/pdf",
                FileContent = "content"u8.ToArray(),
                StorageRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
            }));
    }

    [Fact]
    public async Task UploadDocumentAsync_AllowsDepartmentManagerForManagedSubject()
    {
        var repository = new FakeDocumentRepository(Array.Empty<Document>());
        var service = new DocumentService(
            repository,
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }, managerTeacherId: 2),
            new FakeChunkingService());

        var documentId = await service.UploadDocumentAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            new CreateDocumentDto
            {
                SubjectId = 10,
                UploadedByTeacherId = 2,
                Title = "Allowed",
                FileName = "allowed.pdf",
                ContentType = "application/pdf",
                FileSize = 7,
                FileContent = "content"u8.ToArray(),
                StorageRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
            });

        Assert.True(documentId > 0);
        Assert.Single(repository.SavedDocuments);
    }

    [Fact]
    public async Task GetUploadOptionsForTeacherAsync_ReturnsAllSubjectsInManagedDepartments()
    {
        var subjects = new[]
        {
            CreateManagedSubject(10, "PRN222", "Lập trình .NET", managerTeacherId: 2),
            CreateManagedSubject(11, "SWT301", "Kiểm thử phần mềm", managerTeacherId: 2),
            CreateManagedSubject(12, "DBI202", "Cơ sở dữ liệu", managerTeacherId: 3)
        };
        var service = new DocumentService(
            new FakeDocumentRepository(Array.Empty<Document>()),
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }, managerTeacherId: 2),
            new FakeSubjectRepository(subjects),
            new FakeChunkingService());

        var options = await service.GetUploadOptionsForTeacherAsync(new CurrentUserDto { UserId = 2, Role = UserRole.Teacher });

        Assert.Equal(new[] { 10, 11 }, options.Subjects.Select(subject => subject.Id).OrderBy(id => id));
        Assert.Contains(options.Chapters, chapter => chapter.SubjectId == 10);
        Assert.Contains(options.Chapters, chapter => chapter.SubjectId == 11);
        Assert.DoesNotContain(options.Chapters, chapter => chapter.SubjectId == 12);
    }

    [Fact]
    public async Task UploadDocumentAsync_CreatesChapter_WhenChapterNameIsNewForSubject()
    {
        var subject = CreateManagedSubject(10, "PRN222", "Lập trình .NET", managerTeacherId: 2);
        subject.Chapters.Clear();
        var subjectRepository = new FakeSubjectRepository(new[] { subject });
        var repository = new FakeDocumentRepository(Array.Empty<Document>());
        var service = new DocumentService(
            repository,
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }, managerTeacherId: 2),
            subjectRepository,
            new FakeChunkingService());

        await service.UploadDocumentAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            new CreateDocumentDto
            {
                SubjectId = 10,
                UploadedByTeacherId = 2,
                Title = "New chapter upload",
                ChapterName = "Chương 2",
                FileName = "chapter.pdf",
                ContentType = "application/pdf",
                FileContent = "content for chapter"u8.ToArray(),
                StorageRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
            });

        var chapter = Assert.Single(subject.Chapters);
        Assert.Equal("Chương 2", chapter.Name);
        Assert.Equal(1, chapter.OrderIndex);
        Assert.Equal(chapter.Id, Assert.Single(repository.SavedDocuments).ChapterId);
        Assert.Equal(1, subjectRepository.UpdateCalls);
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
    public async Task UploadDocumentAsync_Throws_WhenChapterDoesNotBelongToSelectedSubject()
    {
        var service = new DocumentService(
            new FakeDocumentRepository(Array.Empty<Document>()),
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }, managerTeacherId: 2),
            new FakeChunkingService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadDocumentAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            new CreateDocumentDto
            {
                SubjectId = 10,
                ChapterId = 999,
                UploadedByTeacherId = 2,
                Title = "Wrong chapter",
                FileName = "wrong.pdf",
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
    public async Task GetDocumentsForStudentAsync_IncludesScheduledArchiveWarningData()
    {
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Scheduled", 10, "PRN222", "Software Engineering", 1);
        document.ScheduledArchiveAt = DateTime.UtcNow.AddDays(8);
        var service = new DocumentService(new FakeDocumentRepository(new[] { document }), new FakeTeacherSubjectRepository(), new FakeChunkingService());

        var result = await service.GetDocumentsForStudentAsync(new CurrentUserDto { UserId = 4, Role = UserRole.Student });

        Assert.Equal(document.ScheduledArchiveAt, Assert.Single(result).ScheduledArchiveAtUtc);
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
    public async Task GetEditableDocumentForTeacherAsync_Throws_WhenTeacherIsNotDepartmentManager()
    {
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Owned", 10, "PRN222", "Software Engineering", 1);
        var service = new DocumentService(
            new FakeDocumentRepository(new[] { document }),
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }, managerTeacherId: 5),
            new FakeChunkingService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetEditableDocumentForTeacherAsync(document.Id, 2));
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
    public async Task UpdateDocumentContentAsync_CreatesChapter_WhenChapterNameIsNewForSubject()
    {
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Owned", 10, "PRN222", "Software Engineering", 1);
        var subject = CreateManagedSubject(10, "PRN222", "Lập trình .NET", managerTeacherId: 2);
        subject.Chapters.Clear();
        var subjectRepository = new FakeSubjectRepository(new[] { subject });
        var service = new DocumentService(
            new FakeDocumentRepository(new[] { document }),
            new FakeTeacherSubjectRepository(assignedSubjectIds: new[] { 10 }, managerTeacherId: 2),
            subjectRepository,
            new FakeChunkingService());

        await service.UpdateDocumentContentAsync(
            document.Id,
            teacherId: 2,
            title: "Updated title",
            subjectId: 10,
            chapterId: null,
            description: null,
            content: "updated content",
            chapterName: "Chương mới");

        var chapter = Assert.Single(subject.Chapters);
        Assert.Equal("Chương mới", chapter.Name);
        Assert.Equal(chapter.Id, document.ChapterId);
        Assert.Equal(1, subjectRepository.UpdateCalls);
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

    [Fact]
    public async Task ReindexIndexedDocumentsAsync_ReplacesChunksForIndexedDocuments_WhenAdminChangesChunkConfiguration()
    {
        var indexedDocument = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Indexed", 10, "PRN222", "Software Engineering", 1);
        indexedDocument.ExtractedContent = "one two three four five six seven eight";
        var failedDocument = CreateDocument(2, 2, DocumentStatus.Failed, false, "Failed", 10, "PRN222", "Software Engineering", 1);
        failedDocument.ExtractedContent = "alpha beta gamma delta";
        var repository = new FakeDocumentRepository(new[] { indexedDocument, failedDocument });
        var service = new DocumentService(
            repository,
            new FakeTeacherSubjectRepository(),
            new FakeChunkingService(chunkSize: 3, chunkOverlap: 0));

        var reindexed = await service.ReindexIndexedDocumentsAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin });

        var document = Assert.Single(reindexed);
        Assert.Equal(indexedDocument.Id, document.Id);
        Assert.Collection(
            indexedDocument.Chunks,
            _ => { },
            _ => { },
            _ => { });
        Assert.Single(failedDocument.Chunks);
        Assert.Equal(1, repository.ReplaceChunkTransactionCalls);
    }

    [Fact]
    public async Task ArchiveDocumentAsync_Throws_WhenScheduledTimeIsLessThanSevenDaysAway()
    {
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Owned", 10, "PRN222", "Software Engineering", 1);
        var service = new DocumentService(new FakeDocumentRepository(new[] { document }), new FakeTeacherSubjectRepository(), new FakeChunkingService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ArchiveDocumentAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            document.Id,
            DateTime.UtcNow.AddDays(6)));
    }

    [Fact]
    public async Task ArchiveDocumentAsync_SchedulesArchive_WhenScheduledTimeIsAtLeastSevenDaysAway()
    {
        var document = CreateDocument(1, 2, DocumentStatus.Indexed, false, "Owned", 10, "PRN222", "Software Engineering", 1);
        var service = new DocumentService(new FakeDocumentRepository(new[] { document }), new FakeTeacherSubjectRepository(), new FakeChunkingService());
        var scheduledAt = DateTime.UtcNow.AddDays(8);

        await service.ArchiveDocumentAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            document.Id,
            scheduledAt);

        Assert.False(document.IsArchived);
        Assert.Equal(DocumentStatus.Indexed, document.Status);
        Assert.Equal(scheduledAt, document.ScheduledArchiveAt);
        Assert.Equal(2, document.ScheduledArchiveByTeacherId);
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

    private static Subject CreateManagedSubject(int id, string code, string name, int managerTeacherId)
    {
        return new Subject
        {
            Id = id,
            Code = code,
            Name = name,
            IsActive = true,
            Department = new Department { ManagerTeacherId = managerTeacherId },
            Chapters = new List<Chapter>
            {
                new() { Id = id * 100 + 1, SubjectId = id, Name = "Chapter 1" }
            }
        };
    }

    private static int GetIntProperty(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName);
        Assert.NotNull(property);

        return Assert.IsType<int>(property.GetValue(value));
    }

    private static string GetStringProperty(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName);
        Assert.NotNull(property);

        return Assert.IsType<string>(property.GetValue(value));
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

        public Task<IReadOnlyList<Document>> GetBySubjectIdsAsync(IReadOnlyCollection<int> subjectIds, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Document>>(_documents.Where(document => subjectIds.Contains(document.SubjectId)).ToList());
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
        private readonly int? _managerTeacherId;

        public FakeTeacherSubjectRepository(IEnumerable<int>? assignedSubjectIds = null, int? managerTeacherId = null)
        {
            _assignedSubjectIds = assignedSubjectIds?.ToHashSet() ?? new HashSet<int> { 10 };
            _managerTeacherId = managerTeacherId;
        }

        public Task<IReadOnlyList<TeacherSubject>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TeacherSubject>>(Array.Empty<TeacherSubject>());
        }

        public Task<TeacherSubject?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<TeacherSubject?>(null);
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
                        Code = $"SUB{subjectId}",
                        Name = $"Subject {subjectId}",
                        IsActive = true,
                        Department = new Department { ManagerTeacherId = _managerTeacherId ?? teacherId },
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

        public Task DeleteAsync(TeacherSubject teacherSubject, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeSubjectRepository : ISubjectRepository
    {
        private readonly IReadOnlyList<Subject> _subjects;

        public FakeSubjectRepository(IReadOnlyList<Subject> subjects)
        {
            _subjects = subjects;
        }

        public Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_subjects);
        }

        public Task<Subject?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_subjects.FirstOrDefault(subject => subject.Id == id));
        }

        public Task AddAsync(Subject subject, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Subject subject, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            foreach (var chapter in subject.Chapters.Where(chapter => chapter.Id == 0))
            {
                chapter.Id = subject.Id * 100 + subject.Chapters.Count;
            }

            return Task.CompletedTask;
        }

        public int UpdateCalls { get; private set; }
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
