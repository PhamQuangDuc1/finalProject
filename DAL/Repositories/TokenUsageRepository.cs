using DAL.Data;
using DAL.Entities;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class TokenUsageRepository : ITokenUsageRepository
{
    private readonly AppDbContext _dbContext;

    public TokenUsageRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AiUsageLog>> GetLogsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AiUsageLogs
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
