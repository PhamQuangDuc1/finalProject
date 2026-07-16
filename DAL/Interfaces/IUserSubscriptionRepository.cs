using DAL.Entities;

namespace DAL.Interfaces;

public interface IUserSubscriptionRepository
{
    Task<IReadOnlyList<UserSubscription>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserSubscription?> GetActiveByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserSubscription?> GetLatestActiveByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserSubscription?> GetByIdWithPackageAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSubscription>> GetAllActiveWithUsersAsync(CancellationToken cancellationToken = default);

    Task<bool> HasActiveSubscriptionAsync(int userId, CancellationToken cancellationToken = default);

    void Add(UserSubscription subscription);

    Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserSubscription subscription, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task DeactivateExpiredAsync(CancellationToken cancellationToken = default);

    Task<int> ProcessScheduledDowngradesAsync(DateTime now, CancellationToken cancellationToken = default);
}