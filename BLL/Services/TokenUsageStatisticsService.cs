using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;

namespace BLL.Services;

public class TokenUsageStatisticsService : ITokenUsageStatisticsService
{
    private readonly ITokenUsageRepository _tokenUsageRepository;
    private readonly IAiUsageService _aiUsageService;

    public TokenUsageStatisticsService(ITokenUsageRepository tokenUsageRepository, IAiUsageService aiUsageService)
    {
        _tokenUsageRepository = tokenUsageRepository;
        _aiUsageService = aiUsageService;
    }

    public async Task<IReadOnlyList<TokenUsageMonthlySummaryDto>> GetMonthlySummariesAsync(CancellationToken cancellationToken = default)
    {
        var summaries = await _aiUsageService.GetMonthlySummaryAsync(
            new CurrentUserDto { UserId = 1, Role = DAL.Entities.UserRole.Admin },
            cancellationToken);

        return summaries.Select(summary => new TokenUsageMonthlySummaryDto
            {
                Year = summary.Year,
                Month = summary.Month,
                PromptTokens = summary.TotalTokens,
                CompletionTokens = 0,
                EstimatedCostUsd = summary.EstimatedCost
            }).ToList();
    }
}
