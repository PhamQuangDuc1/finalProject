using BLL.DTOs;

namespace BLL.Interfaces;

public interface ISubscriptionPackageService
{
    Task<IReadOnlyList<SubscriptionPackageDto>> GetPackagesAsync(bool activeOnly, CancellationToken cancellationToken = default);

    Task<SubscriptionPackageDto?> GetPackageByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreatePackageAsync(CurrentUserDto currentUser, CreatePackageDto package, CancellationToken cancellationToken = default);

    Task UpdatePackageAsync(CurrentUserDto currentUser, UpdatePackageDto package, CancellationToken cancellationToken = default);

    Task ActivateAsync(CurrentUserDto currentUser, int packageId, CancellationToken cancellationToken = default);

    Task DeactivateAsync(CurrentUserDto currentUser, int packageId, CancellationToken cancellationToken = default);
}