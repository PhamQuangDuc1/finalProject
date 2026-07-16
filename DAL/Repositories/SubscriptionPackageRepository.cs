using DAL.Data;
using DAL.Entities;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class SubscriptionPackageRepository : ISubscriptionPackageRepository
{
    private readonly AppDbContext _dbContext;

    public SubscriptionPackageRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SubscriptionPackage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SubscriptionPackages
            .OrderBy(package => package.Price)
            .ThenBy(package => package.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionPackage>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SubscriptionPackages
            .Where(package => package.IsActive)
            .OrderBy(package => package.Price)
            .ThenBy(package => package.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<SubscriptionPackage?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.SubscriptionPackages
            .FirstOrDefaultAsync(package => package.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string name, int? excludedId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SubscriptionPackages
            .Where(package => package.Name == name);

        if (excludedId.HasValue)
        {
            query = query.Where(package => package.Id != excludedId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(SubscriptionPackage package, CancellationToken cancellationToken = default)
    {
        _dbContext.SubscriptionPackages.Add(package);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SubscriptionPackage package, CancellationToken cancellationToken = default)
    {
        _dbContext.SubscriptionPackages.Update(package);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}