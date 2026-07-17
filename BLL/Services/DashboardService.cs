using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class DashboardService : IDashboardService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAiUsageRepository _aiUsageRepository;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;

    public DashboardService(
        IDocumentRepository documentRepository,
        IDepartmentRepository departmentRepository,
        ISubjectRepository subjectRepository,
        IUserRepository userRepository,
        IAiUsageRepository aiUsageRepository,
        IUserSubscriptionRepository userSubscriptionRepository)
    {
        _documentRepository = documentRepository;
        _departmentRepository = departmentRepository;
        _subjectRepository = subjectRepository;
        _userRepository = userRepository;
        _aiUsageRepository = aiUsageRepository;
        _userSubscriptionRepository = userSubscriptionRepository;
    }

    public async Task<DashboardDto> GetDashboardAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        return currentUser.Role switch
        {
            UserRole.Admin => await GetAdminDashboardAsync(cancellationToken),
            UserRole.Teacher => await GetTeacherDashboardAsync(currentUser.UserId, cancellationToken),
            UserRole.Student => await GetStudentDashboardAsync(currentUser.UserId, cancellationToken),
            _ => throw new UnauthorizedAccessException("Unsupported dashboard role.")
        };
    }

    private async Task<DashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.GetAllForAdminAsync(cancellationToken);
        var departments = await _departmentRepository.GetAllAsync(cancellationToken);
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);
        var teachers = await _userRepository.GetByRoleAsync(UserRole.Teacher, cancellationToken);
        var usageLogs = await _aiUsageRepository.GetAllAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var thisMonthLogs = usageLogs.Where(log => log.CreatedAt.Year == now.Year && log.CreatedAt.Month == now.Month).ToList();

        return new DashboardDto
        {
            Role = UserRole.Admin,
            Title = "Dashboard Admin",
            Metrics = new[]
            {
                new DashboardMetricDto { Label = "Tổng tài liệu", Value = documents.Count.ToString() },
                new DashboardMetricDto { Label = "Tổng bộ môn", Value = departments.Count.ToString() },
                new DashboardMetricDto { Label = "Tổng môn học", Value = subjects.Count.ToString() },
                new DashboardMetricDto { Label = "Tổng giảng viên", Value = teachers.Count.ToString() },
                new DashboardMetricDto { Label = "Token tháng này", Value = thisMonthLogs.Sum(log => log.TotalTokens).ToString("N0") },
                new DashboardMetricDto { Label = "Chi phí AI tháng này", Value = $"${thisMonthLogs.Sum(log => log.EstimatedCost):0.000000}" }
            },
            DocumentsByTeacher = documents
                .GroupBy(document => document.UploadedByTeacher?.FullName ?? "Chưa rõ")
                .Select(group => new TeacherDocumentCountDto { TeacherName = group.Key, DocumentCount = group.Count() })
                .OrderByDescending(item => item.DocumentCount)
                .ToList(),
            RecentUploads = documents
                .OrderByDescending(document => document.UploadedAt)
                .Take(5)
                .Select(ToRecentUpload)
                .ToList()
        };
    }

    private async Task<DashboardDto> GetTeacherDashboardAsync(int teacherId, CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.GetByTeacherAsync(teacherId, cancellationToken);

        return new DashboardDto
        {
            Role = UserRole.Teacher,
            Title = "Dashboard Giảng viên",
            Metrics = new[]
            {
                new DashboardMetricDto { Label = "Tài liệu của tôi", Value = documents.Count.ToString() },
                new DashboardMetricDto { Label = "Đã index", Value = documents.Count(document => document.Status == DocumentStatus.Indexed).ToString() },
                new DashboardMetricDto { Label = "Đang xử lý", Value = documents.Count(document => document.Status == DocumentStatus.Processing || document.Status == DocumentStatus.Uploading).ToString() },
                new DashboardMetricDto { Label = "Tổng chunk", Value = documents.Sum(document => document.Chunks.Count).ToString("N0") }
            },
            RecentUploads = documents
                .OrderByDescending(document => document.UploadedAt)
                .Take(5)
                .Select(ToRecentUpload)
                .ToList()
        };
    }

    private async Task<DashboardDto> GetStudentDashboardAsync(int userId, CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.GetIndexedActiveAsync(cancellationToken);
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);
        await _userSubscriptionRepository.ProcessScheduledDowngradesAsync(DateTime.UtcNow, cancellationToken);
        await _userSubscriptionRepository.DeactivateExpiredAsync(cancellationToken);
        var activeSubscription = await _userSubscriptionRepository.GetActiveByUserAsync(userId, cancellationToken);

        return new DashboardDto
        {
            Role = UserRole.Student,
            Title = "Dashboard Sinh viên",
            Metrics = new[]
            {
                new DashboardMetricDto { Label = "Tài liệu khả dụng", Value = documents.Count.ToString() },
                new DashboardMetricDto { Label = "Đã xem gần đây", Value = "0" },
                new DashboardMetricDto { Label = "Môn học", Value = subjects.Count(subject => subject.IsActive).ToString() },
                new DashboardMetricDto { Label = "Token còn lại", Value = (activeSubscription?.RemainingTokens ?? 0).ToString("N0") }
            },
            RecentUploads = documents
                .OrderByDescending(document => document.UploadedAt)
                .Take(5)
                .Select(ToRecentUpload)
                .ToList(),
            Subjects = subjects
                .Where(subject => subject.IsActive)
                .Select(subject => subject.Name)
                .Take(8)
                .ToList()
        };
    }

    private static RecentUploadDto ToRecentUpload(Document document)
    {
        return new RecentUploadDto
        {
            Id = document.Id,
            Title = document.Title,
            SubjectName = document.Subject?.Name ?? string.Empty,
            TeacherName = document.UploadedByTeacher?.FullName ?? string.Empty,
            UploadedAt = document.UploadedAt
        };
    }
}
