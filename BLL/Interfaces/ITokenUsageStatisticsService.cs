using BLL.DTOs;

namespace BLL.Interfaces;

public interface ITokenUsageStatisticsService
{
    Task<IReadOnlyList<TokenUsageMonthlySummaryDto>> GetMonthlySummariesAsync(CancellationToken cancellationToken = default);
}
