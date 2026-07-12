using BLL.DTOs;
using DAL.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.ViewModels;

public class TeacherDocumentsIndexViewModel
{
    public IReadOnlyList<DocumentDto> Documents { get; set; } = Array.Empty<DocumentDto>();

    public int? SubjectId { get; set; }

    public DocumentStatus? Status { get; set; }

    public int? ChapterId { get; set; }

    public string? Search { get; set; }

    public IReadOnlyList<SelectListItem> SubjectOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> ChapterOptions { get; set; } = Array.Empty<SelectListItem>();
}
