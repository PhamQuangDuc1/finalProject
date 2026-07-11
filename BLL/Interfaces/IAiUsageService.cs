using BLL.DTOs;

namespace BLL.Interfaces;

public interface IAiUsageService
{
    Task LogUsageAsync(CreateAiUsageLogDto usageLog, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiUsageMonthlySummaryDto>> GetMonthlySummaryAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiUsageBreakdownDto>> GetSummaryByModelAndOperationAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default);
}
