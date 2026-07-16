using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class AiUsageService : IAiUsageService
{
    private readonly IAiUsageRepository _aiUsageRepository;
    private readonly IAiCostEstimator _costEstimator;

    public AiUsageService(IAiUsageRepository aiUsageRepository, IAiCostEstimator costEstimator)
    {
        _aiUsageRepository = aiUsageRepository;
        _costEstimator = costEstimator;
    }

    public Task LogUsageAsync(CreateAiUsageLogDto usageLog, CancellationToken cancellationToken = default)
    {
        var estimatedCost = usageLog.EstimatedCost > 0
            ? usageLog.EstimatedCost
            : _costEstimator.EstimateCost(usageLog.ModelName, usageLog.PromptTokens, usageLog.CompletionTokens);

        var entity = new AiUsageLog
        {
            UserId = usageLog.UserId,
            DocumentId = usageLog.DocumentId,
            OperationType = usageLog.OperationType,
            ModelName = usageLog.ModelName,
            PromptTokens = usageLog.PromptTokens,
            CompletionTokens = usageLog.CompletionTokens,
            TotalTokens = usageLog.PromptTokens + usageLog.CompletionTokens,
            EstimatedCost = estimatedCost,
            CreatedAt = DateTime.UtcNow
        };

        return _aiUsageRepository.AddAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<AiUsageMonthlySummaryDto>> GetMonthlySummaryAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var logs = await _aiUsageRepository.GetAllAsync(cancellationToken);

        return logs
            .GroupBy(log => new { log.CreatedAt.Year, log.CreatedAt.Month })
            .Select(group => new AiUsageMonthlySummaryDto
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                TotalTokens = group.Sum(log => log.TotalTokens),
                EstimatedCost = group.Sum(GetEstimatedCost)
            })
            .OrderByDescending(summary => summary.Year)
            .ThenByDescending(summary => summary.Month)
            .ToList();
    }

    public async Task<IReadOnlyList<AiUsageBreakdownDto>> GetSummaryByModelAndOperationAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var logs = await _aiUsageRepository.GetAllAsync(cancellationToken);

        return logs
            .GroupBy(log => new { log.ModelName, log.OperationType })
            .Select(group => new AiUsageBreakdownDto
            {
                ModelName = group.Key.ModelName,
                OperationType = group.Key.OperationType,
                TotalTokens = group.Sum(log => log.TotalTokens),
                EstimatedCost = group.Sum(GetEstimatedCost)
            })
            .OrderBy(summary => summary.ModelName)
            .ThenBy(summary => summary.OperationType)
            .ToList();
    }

    public async Task<AiUsageDashboardDto> GetDashboardAsync(
        CurrentUserDto currentUser,
        AiUsageDashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var year = filter.Year > 0 ? filter.Year : DateTime.UtcNow.Year;
        var month = filter.Month is >= 1 and <= 12 ? filter.Month : DateTime.UtcNow.Month;
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);
        var logs = await _aiUsageRepository.GetAllAsync(cancellationToken);
        var monthLogs = logs
            .Where(log => log.CreatedAt >= monthStart && log.CreatedAt < nextMonthStart)
            .ToList();

        var availableModels = monthLogs
            .Select(log => log.ModelName)
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(model => model)
            .ToList();
        var availableOperationTypes = Enum.GetValues<AiOperationType>().ToList();

        var filteredLogs = monthLogs
            .Where(log => string.IsNullOrWhiteSpace(filter.ModelName)
                || string.Equals(log.ModelName, filter.ModelName, StringComparison.OrdinalIgnoreCase))
            .Where(log => !filter.OperationType.HasValue || log.OperationType == filter.OperationType.Value)
            .ToList();
        var selectedDay = filter.DateScope == AiUsageDateScope.Today
            ? (filter.Day?.Date ?? DateTime.UtcNow.Date)
            : (DateTime?)null;
        if (selectedDay.HasValue)
        {
            filteredLogs = filteredLogs
                .Where(log => log.CreatedAt.Date == selectedDay.Value)
                .ToList();
        }
        var selectedWeekStart = filter.DateScope == AiUsageDateScope.ThisWeek
            ? GetWeekStart(filter.Day?.Date ?? DateTime.UtcNow.Date)
            : (DateTime?)null;
        if (selectedWeekStart.HasValue)
        {
            var weekEndExclusive = selectedWeekStart.Value.AddDays(7);
            filteredLogs = filteredLogs
                .Where(log => log.CreatedAt >= selectedWeekStart.Value && log.CreatedAt < weekEndExclusive)
                .ToList();
        }

        var requests = filteredLogs.Count;
        var totalTokens = filteredLogs.Sum(log => log.TotalTokens);
        var summaryDates = selectedDay.HasValue
            ? new[] { selectedDay.Value }
            : selectedWeekStart.HasValue
                ? Enumerable.Range(0, 7).Select(offset => selectedWeekStart.Value.AddDays(offset))
            : Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                .Select(day => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));
        var dailySummaries = summaryDates
            .Select(day =>
            {
                var date = DateTime.SpecifyKind(day.Date, DateTimeKind.Utc);
                var dayLogs = filteredLogs.Where(log => log.CreatedAt.Date == date).ToList();

                return new AiUsageDailySummaryDto
                {
                    Date = date,
                    PromptTokens = dayLogs.Sum(log => log.PromptTokens),
                    CompletionTokens = dayLogs.Sum(log => log.CompletionTokens),
                    TotalTokens = dayLogs.Sum(log => log.TotalTokens),
                    EstimatedCost = dayLogs.Sum(GetEstimatedCost),
                    Requests = dayLogs.Count
                };
            })
            .ToList();
        dailySummaries = SortDailySummaries(dailySummaries, filter.SortBy).ToList();

        return new AiUsageDashboardDto
        {
            Year = year,
            Month = month,
            SelectedModelName = filter.ModelName,
            SelectedOperationType = filter.OperationType,
            TotalPromptTokensThisMonth = filteredLogs.Sum(log => log.PromptTokens),
            TotalCompletionTokensThisMonth = filteredLogs.Sum(log => log.CompletionTokens),
            TotalTokensThisMonth = totalTokens,
            EstimatedCostThisMonth = filteredLogs.Sum(GetEstimatedCost),
            RequestsThisMonth = requests,
            AverageTokensPerRequest = requests == 0 ? 0 : decimal.Round(totalTokens / (decimal)requests, 2),
            DailySummaries = dailySummaries,
            ModelSummaries = filteredLogs
                .GroupBy(log => log.ModelName)
                .Select(group => new AiUsageModelSummaryDto
                {
                    ModelName = group.Key,
                    PromptTokens = group.Sum(log => log.PromptTokens),
                    CompletionTokens = group.Sum(log => log.CompletionTokens),
                    TotalTokens = group.Sum(log => log.TotalTokens),
                    EstimatedCost = group.Sum(GetEstimatedCost),
                    Requests = group.Count()
                })
                .OrderByDescending(summary => summary.TotalTokens)
                .ThenBy(summary => summary.ModelName)
                .ToList(),
            OperationSummaries = filteredLogs
                .GroupBy(log => log.OperationType)
                .Select(group => new AiUsageOperationSummaryDto
                {
                    OperationType = group.Key,
                    PromptTokens = group.Sum(log => log.PromptTokens),
                    CompletionTokens = group.Sum(log => log.CompletionTokens),
                    TotalTokens = group.Sum(log => log.TotalTokens),
                    EstimatedCost = group.Sum(GetEstimatedCost),
                    Requests = group.Count()
                })
                .OrderBy(summary => summary.OperationType)
                .ToList(),
            AvailableModels = availableModels,
            AvailableOperationTypes = availableOperationTypes
        };
    }

    private decimal GetEstimatedCost(AiUsageLog log)
    {
        return log.EstimatedCost > 0
            ? log.EstimatedCost
            : _costEstimator.EstimateCost(log.ModelName, log.PromptTokens, log.CompletionTokens);
    }

    private static IEnumerable<AiUsageDailySummaryDto> SortDailySummaries(
        IEnumerable<AiUsageDailySummaryDto> dailySummaries,
        AiUsageDailySortBy sortBy)
    {
        return sortBy switch
        {
            AiUsageDailySortBy.TotalTokens => dailySummaries
                .OrderByDescending(summary => summary.TotalTokens)
                .ThenByDescending(summary => summary.Date),
            AiUsageDailySortBy.EstimatedCost => dailySummaries
                .OrderByDescending(summary => summary.EstimatedCost)
                .ThenByDescending(summary => summary.Date),
            _ => dailySummaries.OrderByDescending(summary => summary.Date)
        };
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return DateTime.SpecifyKind(date.Date.AddDays(-daysSinceMonday), DateTimeKind.Utc);
    }
}
