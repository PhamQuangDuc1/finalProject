using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;

namespace BLL.Services;

public class TokenUsageStatisticsService : ITokenUsageStatisticsService
{
    private readonly ITokenUsageRepository _tokenUsageRepository;

    public TokenUsageStatisticsService(ITokenUsageRepository tokenUsageRepository)
    {
        _tokenUsageRepository = tokenUsageRepository;
    }

    public async Task<IReadOnlyList<TokenUsageMonthlySummaryDto>> GetMonthlySummariesAsync(CancellationToken cancellationToken = default)
    {
        var logs = await _tokenUsageRepository.GetLogsAsync(cancellationToken);

        return logs
            .GroupBy(log => new { log.CreatedAt.Year, log.CreatedAt.Month })
            .Select(group => new TokenUsageMonthlySummaryDto
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                PromptTokens = group.Sum(log => log.PromptTokens),
                CompletionTokens = group.Sum(log => log.CompletionTokens),
                EstimatedCostUsd = group.Sum(log => log.EstimatedCost)
            })
            .OrderByDescending(summary => summary.Year)
            .ThenByDescending(summary => summary.Month)
            .ToList();
    }
}
