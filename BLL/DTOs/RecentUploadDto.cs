namespace BLL.DTOs;

public class RecentUploadDto
{
    public string Title { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }
}
