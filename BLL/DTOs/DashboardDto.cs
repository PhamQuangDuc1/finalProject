using DAL.Entities;

namespace BLL.DTOs;

public class DashboardDto
{
    public UserRole Role { get; set; }

    public string Title { get; set; } = string.Empty;

    public IReadOnlyList<DashboardMetricDto> Metrics { get; set; } = Array.Empty<DashboardMetricDto>();

    public IReadOnlyList<TeacherDocumentCountDto> DocumentsByTeacher { get; set; } = Array.Empty<TeacherDocumentCountDto>();

    public IReadOnlyList<RecentUploadDto> RecentUploads { get; set; } = Array.Empty<RecentUploadDto>();

    public IReadOnlyList<string> Subjects { get; set; } = Array.Empty<string>();
}
