using DAL.Entities;

namespace BLL.DTOs;

public class DocumentFilterDto
{
    public int? SubjectId { get; set; }

    public DocumentStatus? Status { get; set; }

    public int? ChapterId { get; set; }

    public string? Search { get; set; }
}
