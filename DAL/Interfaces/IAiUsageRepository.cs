using DAL.Entities;

namespace DAL.Interfaces;

public interface IAiUsageRepository
{
    Task AddAsync(AiUsageLog usageLog, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiUsageLog>> GetAllAsync(CancellationToken cancellationToken = default);
}
