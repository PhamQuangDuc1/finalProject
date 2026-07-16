using BLL.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.ViewModels;

public class AiUsageDashboardViewModel
{
    public AiUsageDashboardDto Dashboard { get; set; } = new();

    public string Month { get; set; } = string.Empty;

    public string? ModelName { get; set; }

    public AiUsageDateScope DateScope { get; set; } = AiUsageDateScope.Month;

    public AiUsageDailySortBy SortBy { get; set; } = AiUsageDailySortBy.NewestDate;

    public IReadOnlyList<SelectListItem> MonthOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> ModelOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> DateScopeOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> SortOptions { get; set; } = Array.Empty<SelectListItem>();
}
