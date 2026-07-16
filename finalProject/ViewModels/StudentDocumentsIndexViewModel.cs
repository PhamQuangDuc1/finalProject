using BLL.DTOs;

namespace finalProject.ViewModels;

public class StudentDocumentsIndexViewModel
{
    public IReadOnlyList<DocumentDto> Documents { get; set; } = Array.Empty<DocumentDto>();

    public IReadOnlyList<DocumentDto> UpcomingArchiveDocuments =>
        Documents
            .Where(document => document.ScheduledArchiveAtUtc.HasValue)
            .OrderBy(document => document.ScheduledArchiveAtUtc)
            .ToList();
}
