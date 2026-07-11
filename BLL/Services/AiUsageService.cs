using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class AiUsageService : IAiUsageService
{
    private readonly IAiUsageRepository _aiUsageRepository;

    public AiUsageService(IAiUsageRepository aiUsageRepository)
    {
        _aiUsageRepository = aiUsageRepository;
    }

    public Task LogUsageAsync(CreateAiUsageLogDto usageLog, CancellationToken cancellationToken = default)
    {
        var entity = new AiUsageLog
        {
            UserId = usageLog.UserId,
            DocumentId = usageLog.DocumentId,
            OperationType = usageLog.OperationType,
            ModelName = usageLog.ModelName,
            PromptTokens = usageLog.PromptTokens,
            CompletionTokens = usageLog.CompletionTokens,
            TotalTokens = usageLog.PromptTokens + usageLog.CompletionTokens,
            EstimatedCost = usageLog.EstimatedCost,
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
                EstimatedCost = group.Sum(log => log.EstimatedCost)
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
                EstimatedCost = group.Sum(log => log.EstimatedCost)
            })
            .OrderBy(summary => summary.ModelName)
            .ThenBy(summary => summary.OperationType)
            .ToList();
    }
}
