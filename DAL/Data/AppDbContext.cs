using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<Chapter> Chapters => Set<Chapter>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    public DbSet<AiUsageLog> AiUsageLogs => Set<AiUsageLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var createdAt = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);

        ConfigureUsers(modelBuilder);
        ConfigureDepartments(modelBuilder);
        ConfigureSubjects(modelBuilder);
        ConfigureTeacherSubjects(modelBuilder);
        ConfigureChapters(modelBuilder);
        ConfigureDocuments(modelBuilder);
        ConfigureDocumentChunks(modelBuilder);
        ConfigureSystemSettings(modelBuilder);
        ConfigureAiUsageLogs(modelBuilder);
        SeedData(modelBuilder, createdAt);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(user => user.Username)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(512);

            entity.Property(user => user.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(user => user.Username)
                .IsUnique();

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Users_Role",
                "[Role] IN (0, 1, 2)"));
        });
    }

    private static void ConfigureDepartments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.Property(department => department.Code)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(department => department.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(department => department.Description)
                .HasMaxLength(1000);

            entity.HasIndex(department => department.Code)
                .IsUnique();

            entity.HasIndex(department => department.ManagerTeacherId)
                .IsUnique()
                .HasFilter("[ManagerTeacherId] IS NOT NULL");

            entity.HasOne(department => department.ManagerTeacher)
                .WithMany(user => user.ManagedDepartments)
                .HasForeignKey(department => department.ManagerTeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSubjects(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.Property(subject => subject.Code)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(subject => subject.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(subject => subject.Description)
                .HasMaxLength(1000);

            entity.HasIndex(subject => new { subject.DepartmentId, subject.Code })
                .IsUnique();

            entity.HasOne(subject => subject.Department)
                .WithMany(department => department.Subjects)
                .HasForeignKey(subject => subject.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTeacherSubjects(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeacherSubject>(entity =>
        {
            entity.HasIndex(teacherSubject => new { teacherSubject.TeacherId, teacherSubject.SubjectId })
                .IsUnique();

            entity.HasOne(teacherSubject => teacherSubject.Teacher)
                .WithMany(user => user.TeacherSubjects)
                .HasForeignKey(teacherSubject => teacherSubject.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(teacherSubject => teacherSubject.Subject)
                .WithMany(subject => subject.TeacherSubjects)
                .HasForeignKey(teacherSubject => teacherSubject.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureChapters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.Property(chapter => chapter.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(chapter => new { chapter.SubjectId, chapter.OrderIndex })
                .IsUnique();

            entity.HasOne(chapter => chapter.Subject)
                .WithMany(subject => subject.Chapters)
                .HasForeignKey(chapter => chapter.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureDocuments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(document => document.Title)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(document => document.Description)
                .HasMaxLength(1000);

            entity.Property(document => document.OriginalFileName)
                .HasMaxLength(260)
                .IsRequired();

            entity.Property(document => document.StoredFileName)
                .HasMaxLength(260)
                .IsRequired();

            entity.Property(document => document.FilePath)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(document => document.ContentType)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(document => document.ErrorMessage)
                .HasMaxLength(2000);

            entity.HasIndex(document => new { document.SubjectId, document.UploadedByTeacherId });

            entity.HasOne(document => document.Subject)
                .WithMany(subject => subject.Documents)
                .HasForeignKey(document => document.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(document => document.Chapter)
                .WithMany(chapter => chapter.Documents)
                .HasForeignKey(document => document.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(document => document.UploadedByTeacher)
                .WithMany(user => user.UploadedDocuments)
                .HasForeignKey(document => document.UploadedByTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(document => document.ArchivedByTeacher)
                .WithMany(user => user.ArchivedDocuments)
                .HasForeignKey(document => document.ArchivedByTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Documents_Status",
                "[Status] IN (0, 1, 2, 3, 4)"));
        });
    }

    private static void ConfigureDocumentChunks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.Property(chunk => chunk.Content)
                .IsRequired();

            entity.HasIndex(chunk => new { chunk.DocumentId, chunk.ChunkIndex })
                .IsUnique();

            entity.HasOne(chunk => chunk.Document)
                .WithMany(document => document.Chunks)
                .HasForeignKey(chunk => chunk.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSystemSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasOne(setting => setting.UpdatedByAdmin)
                .WithMany(user => user.UpdatedSystemSettings)
                .HasForeignKey(setting => setting.UpdatedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SystemSettings_ChunkStrategy",
                "[ChunkStrategy] IN (0, 1, 2)"));
        });
    }

    private static void ConfigureAiUsageLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiUsageLog>(entity =>
        {
            entity.Property(log => log.ModelName)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(log => log.EstimatedCost)
                .HasColumnType("decimal(18,6)");

            entity.HasIndex(log => log.CreatedAt);
            entity.HasIndex(log => new { log.ModelName, log.CreatedAt });

            entity.HasOne(log => log.User)
                .WithMany(user => user.AiUsageLogs)
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(log => log.Document)
                .WithMany(document => document.AiUsageLogs)
                .HasForeignKey(log => log.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_AiUsageLogs_OperationType",
                "[OperationType] IN (0, 1, 2, 3)"));
        });
    }

    private static void SeedData(ModelBuilder modelBuilder, DateTime createdAt)
    {
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Username = "admin", PasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92", FullName = "Admin", Role = UserRole.Admin, IsActive = true, CreatedAt = createdAt },
            new User { Id = 2, Username = "teacherA", PasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92", FullName = "Teacher A", Role = UserRole.Teacher, IsActive = true, CreatedAt = createdAt },
            new User { Id = 3, Username = "teacherB", PasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92", FullName = "Teacher B", Role = UserRole.Teacher, IsActive = true, CreatedAt = createdAt },
            new User { Id = 4, Username = "student", PasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92", FullName = "Student", Role = UserRole.Student, IsActive = true, CreatedAt = createdAt });

        modelBuilder.Entity<Department>().HasData(
            new Department { Id = 1, Code = "SE", Name = "Bộ môn Kỹ thuật phần mềm", Description = "Quản lý các môn học thuộc nhóm kỹ thuật phần mềm.", ManagerTeacherId = 2, CreatedAt = createdAt });

        modelBuilder.Entity<Subject>().HasData(
            new Subject { Id = 1, DepartmentId = 1, Code = "PRN222", Name = "Lập trình .NET", Description = "Môn học về ASP.NET Core và EF Core.", IsActive = true, CreatedAt = createdAt },
            new Subject { Id = 2, DepartmentId = 1, Code = "SWT301", Name = "Kiểm thử phần mềm", Description = "Môn học về kiểm thử và đảm bảo chất lượng phần mềm.", IsActive = true, CreatedAt = createdAt });

        modelBuilder.Entity<TeacherSubject>().HasData(
            new TeacherSubject { Id = 1, TeacherId = 2, SubjectId = 1, AssignedAt = createdAt },
            new TeacherSubject { Id = 2, TeacherId = 3, SubjectId = 2, AssignedAt = createdAt });

        modelBuilder.Entity<SystemSetting>().HasData(
            new SystemSetting { Id = 1, ChunkStrategy = ChunkStrategy.FixedSize, ChunkSize = 1100, ChunkOverlap = 150, TopK = 5, UpdatedAt = createdAt, UpdatedByAdminId = 1 });
    }
}
