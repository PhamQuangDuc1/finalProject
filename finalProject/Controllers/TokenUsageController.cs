using System.Globalization;
using System.Security.Claims;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using finalProject.Authorization;
using finalProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.Controllers;

[Authorize(Policy = StudyMatePolicies.ViewTokenUsageStatistics)]
public class TokenUsageController : Controller
{
    private readonly IAiUsageService _aiUsageService;

    public TokenUsageController(IAiUsageService aiUsageService)
    {
        _aiUsageService = aiUsageService;
    }

    public async Task<IActionResult> Index(
        string? month,
        string? modelName,
        AiOperationType? operationType,
        AiUsageDateScope dateScope = AiUsageDateScope.Month,
        AiUsageDailySortBy sortBy = AiUsageDailySortBy.NewestDate,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var selectedMonth = dateScope == AiUsageDateScope.Today
            ? new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            : ParseMonth(month);
        var filter = new AiUsageDashboardFilterDto
        {
            Year = selectedMonth.Year,
            Month = selectedMonth.Month,
            ModelName = modelName,
            OperationType = operationType,
            DateScope = dateScope,
            Day = dateScope == AiUsageDateScope.Today ? today : null,
            SortBy = sortBy
        };
        var dashboard = await _aiUsageService.GetDashboardAsync(GetCurrentUser(), filter, cancellationToken);

        return View(new AiUsageDashboardViewModel
        {
            Dashboard = dashboard,
            Month = $"{dashboard.Year:D4}-{dashboard.Month:D2}",
            ModelName = modelName,
            OperationType = operationType,
            DateScope = dateScope,
            SortBy = sortBy,
            MonthOptions = GetMonthOptions(selectedMonth),
            ModelOptions = GetModelOptions(dashboard.AvailableModels, modelName),
            OperationOptions = GetOperationOptions(operationType),
            DateScopeOptions = GetDateScopeOptions(dateScope),
            SortOptions = GetSortOptions(sortBy)
        });
    }

    private static DateTime ParseMonth(string? month)
    {
        if (DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return new DateTime(parsed.Year, parsed.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private static IReadOnlyList<SelectListItem> GetMonthOptions(DateTime selectedMonth)
    {
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return Enumerable.Range(0, 12)
            .Select(offset => currentMonth.AddMonths(-offset))
            .Select(month => new SelectListItem(
                month.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                month.Year == selectedMonth.Year && month.Month == selectedMonth.Month))
            .ToList();
    }

    private static IReadOnlyList<SelectListItem> GetModelOptions(IReadOnlyList<string> models, string? selectedModelName)
    {
        return models
            .Select(model => new SelectListItem(model, model, string.Equals(model, selectedModelName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static IReadOnlyList<SelectListItem> GetOperationOptions(AiOperationType? selectedOperationType)
    {
        return Enum.GetValues<AiOperationType>()
            .Select(operation => new SelectListItem(GetOperationLabel(operation), operation.ToString(), operation == selectedOperationType))
            .ToList();
    }

    private static IReadOnlyList<SelectListItem> GetSortOptions(AiUsageDailySortBy selectedSortBy)
    {
        return new[]
        {
            new SelectListItem("Ngày gần nhất", AiUsageDailySortBy.NewestDate.ToString(), selectedSortBy == AiUsageDailySortBy.NewestDate),
            new SelectListItem("Token nhiều nhất", AiUsageDailySortBy.TotalTokens.ToString(), selectedSortBy == AiUsageDailySortBy.TotalTokens),
            new SelectListItem("Chi phí nhiều nhất", AiUsageDailySortBy.EstimatedCost.ToString(), selectedSortBy == AiUsageDailySortBy.EstimatedCost)
        };
    }

    private static IReadOnlyList<SelectListItem> GetDateScopeOptions(AiUsageDateScope selectedDateScope)
    {
        return new[]
        {
            new SelectListItem("Cả tháng", AiUsageDateScope.Month.ToString(), selectedDateScope == AiUsageDateScope.Month),
            new SelectListItem("Hôm nay", AiUsageDateScope.Today.ToString(), selectedDateScope == AiUsageDateScope.Today)
        };
    }

    private static string GetOperationLabel(AiOperationType operationType)
    {
        return operationType switch
        {
            AiOperationType.DocumentEmbedding => "Embedding",
            AiOperationType.ChatCompletion => "Chat Completion",
            AiOperationType.ReIndexDocument => "Document Indexing",
            AiOperationType.Benchmark => "Benchmark",
            _ => operationType.ToString()
        };
    }

    private CurrentUserDto GetCurrentUser()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return new CurrentUserDto
        {
            UserId = int.TryParse(userIdValue, out var userId) ? userId : 0,
            Role = UserRole.Admin
        };
    }
}
