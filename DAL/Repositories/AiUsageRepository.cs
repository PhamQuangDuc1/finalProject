using DAL.Data;
using DAL.Entities;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class AiUsageRepository : IAiUsageRepository
{
    private readonly AppDbContext _dbContext;

    public AiUsageRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AiUsageLog usageLog, CancellationToken cancellationToken = default)
    {
        _dbContext.AiUsageLogs.Add(usageLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiUsageLog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AiUsageLogs
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
