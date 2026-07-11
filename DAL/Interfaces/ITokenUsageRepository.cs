using DAL.Entities;

namespace DAL.Interfaces;

public interface ITokenUsageRepository
{
    Task<IReadOnlyList<AiUsageLog>> GetLogsAsync(CancellationToken cancellationToken = default);
}
