using DAL.Data;
using DAL.Entities;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly AppDbContext _dbContext;

    public UserSubscriptionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserSubscription>> GetByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserSubscriptions
            .Include(subscription => subscription.SubscriptionPackage)
            .Where(subscription => subscription.UserId == userId)
            .OrderByDescending(subscription => subscription.StartDate)
            .ToListAsync(cancellationToken);
    }

    public Task<UserSubscription?> GetActiveByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserSubscriptions
            .Include(subscription => subscription.SubscriptionPackage)
            .Where(subscription => subscription.UserId == userId
                && subscription.IsActive
                && subscription.EndDate > DateTime.UtcNow)
            .OrderByDescending(subscription => subscription.EndDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<UserSubscription?> GetLatestActiveByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserSubscriptions
            .Where(subscription => subscription.UserId == userId
                && subscription.IsActive
                && subscription.EndDate > DateTime.UtcNow)
            .OrderByDescending(subscription => subscription.EndDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasActiveSubscriptionAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserSubscriptions.AnyAsync(
            subscription => subscription.UserId == userId
                && subscription.IsActive
                && subscription.EndDate > DateTime.UtcNow,
            cancellationToken);
    }

    public async Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default)
    {
        _dbContext.UserSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Add(UserSubscription subscription)
    {
        _dbContext.UserSubscriptions.Add(subscription);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserSubscription subscription, CancellationToken cancellationToken = default)
    {
        _dbContext.UserSubscriptions.Update(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expired = await _dbContext.UserSubscriptions
            .Where(subscription => subscription.IsActive
                && subscription.EndDate <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var subscription in expired)
        {
            subscription.IsActive = false;
            subscription.DeactivatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<UserSubscription?> GetByIdWithPackageAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserSubscriptions
            .Include(subscription => subscription.SubscriptionPackage)
            .Include(subscription => subscription.User)
            .FirstOrDefaultAsync(subscription => subscription.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<UserSubscription>> GetAllActiveWithUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserSubscriptions
            .Include(subscription => subscription.SubscriptionPackage)
            .Include(subscription => subscription.ScheduledDowngradePackage)
            .Include(subscription => subscription.User)
            .Where(subscription => subscription.IsActive && subscription.EndDate > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ProcessScheduledDowngradesAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var due = await _dbContext.UserSubscriptions
            .Include(subscription => subscription.SubscriptionPackage)
            .Where(subscription => subscription.ScheduledDowngradePackageId != null
                && subscription.IsActive
                && subscription.EndDate <= now)
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
        {
            return 0;
        }

        var processed = 0;
        foreach (var current in due)
        {
            var targetPackageId = current.ScheduledDowngradePackageId!.Value;
            var targetPackage = await _dbContext.SubscriptionPackages
                .FirstOrDefaultAsync(package => package.Id == targetPackageId, cancellationToken);

            if (targetPackage is null)
            {
                current.ScheduledDowngradePackageId = null;
                continue;
            }

            // Only proceed if no replacement already exists (avoid double-create race)
            var hasReplacement = await _dbContext.UserSubscriptions
                .AnyAsync(s => s.UserId == current.UserId
                    && s.SubscriptionPackageId == targetPackage.Id
                    && s.IsActive
                    && s.StartDate >= current.EndDate.AddMinutes(-5),
                    cancellationToken);

            if (hasReplacement)
            {
                current.ScheduledDowngradePackageId = null;
                continue;
            }

            current.IsActive = false;
            current.DeactivatedAt = now;
            current.ScheduledDowngradePackageId = null;

            var startDate = now;
            var replacement = new UserSubscription
            {
                UserId = current.UserId,
                SubscriptionPackageId = targetPackage.Id,
                PaymentId = current.PaymentId,
                StartDate = startDate,
                EndDate = startDate.AddDays(targetPackage.DurationDays),
                RemainingTokens = targetPackage.MaxTokens,
                IsActive = true,
                CreatedAt = now
            };
            _dbContext.UserSubscriptions.Add(replacement);
            processed++;
        }

        if (processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return processed;
    }
}