using BLL.DTOs;
using DAL.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.ViewModels;

public class AiUsageDashboardViewModel
{
    public AiUsageDashboardDto Dashboard { get; set; } = new();

    public string Month { get; set; } = string.Empty;

    public string? ModelName { get; set; }

    public AiOperationType? OperationType { get; set; }

    public IReadOnlyList<SelectListItem> MonthOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> ModelOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> OperationOptions { get; set; } = Array.Empty<SelectListItem>();
}
