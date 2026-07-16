using DAL.Entities;

namespace DAL.Interfaces;

public interface ISubscriptionPackageRepository
{
    Task<IReadOnlyList<SubscriptionPackage>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPackage>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<SubscriptionPackage?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, int? excludedId = null, CancellationToken cancellationToken = default);

    Task AddAsync(SubscriptionPackage package, CancellationToken cancellationToken = default);

    Task UpdateAsync(SubscriptionPackage package, CancellationToken cancellationToken = default);
}