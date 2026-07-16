using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class SubscriptionPackageService : ISubscriptionPackageService
{
    private const int MaximumDurationDays = 3650;
    private const decimal MaximumPrice = 99_999_999.99m;

    private readonly ISubscriptionPackageRepository _packageRepository;

    public SubscriptionPackageService(ISubscriptionPackageRepository packageRepository)
    {
        _packageRepository = packageRepository;
    }

    public async Task<IReadOnlyList<SubscriptionPackageDto>> GetPackagesAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var packages = activeOnly
            ? await _packageRepository.GetActiveAsync(cancellationToken)
            : await _packageRepository.GetAllAsync(cancellationToken);

        return packages.Select(ToDto).ToList();
    }

    public async Task<SubscriptionPackageDto?> GetPackageByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var package = await _packageRepository.GetByIdAsync(id, cancellationToken);
        return package is null ? null : ToDto(package);
    }

    public async Task<int> CreatePackageAsync(CurrentUserDto currentUser, CreatePackageDto package, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);
        await ValidatePackageAsync(package.Name, package.Price, package.DurationDays, package.MaxTokens, null, cancellationToken);

        var entity = new SubscriptionPackage
        {
            Name = package.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(package.Description) ? null : package.Description.Trim(),
            Price = package.Price,
            DurationDays = package.DurationDays,
            MaxTokens = package.MaxTokens,
            IsActive = package.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _packageRepository.AddAsync(entity, cancellationToken);
        return entity.Id;
    }

    public async Task UpdatePackageAsync(CurrentUserDto currentUser, UpdatePackageDto package, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);
        await ValidatePackageAsync(package.Name, package.Price, package.DurationDays, package.MaxTokens, package.Id, cancellationToken);

        var entity = await _packageRepository.GetByIdAsync(package.Id, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");

        entity.Name = package.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(package.Description) ? null : package.Description.Trim();
        entity.Price = package.Price;
        entity.DurationDays = package.DurationDays;
        entity.MaxTokens = package.MaxTokens;
        entity.IsActive = package.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _packageRepository.UpdateAsync(entity, cancellationToken);
    }

    public async Task ActivateAsync(CurrentUserDto currentUser, int packageId, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var entity = await _packageRepository.GetByIdAsync(packageId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");

        entity.IsActive = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await _packageRepository.UpdateAsync(entity, cancellationToken);
    }

    public async Task DeactivateAsync(CurrentUserDto currentUser, int packageId, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var entity = await _packageRepository.GetByIdAsync(packageId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await _packageRepository.UpdateAsync(entity, cancellationToken);
    }

    private async Task ValidatePackageAsync(string name, decimal price, int durationDays, int maxTokens, int? excludedId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Package name is required.");
        }

        if (price < 0 || price > MaximumPrice)
        {
            throw new InvalidOperationException($"Package price must be between 0 and {MaximumPrice:N0}.");
        }

        if (durationDays <= 0 || durationDays > MaximumDurationDays)
        {
            throw new InvalidOperationException($"Package duration must be between 1 and {MaximumDurationDays} days.");
        }

        if (maxTokens < 0)
        {
            throw new InvalidOperationException("Package max tokens must be greater than or equal to 0.");
        }

        if (await _packageRepository.ExistsByNameAsync(name.Trim(), excludedId, cancellationToken))
        {
            throw new InvalidOperationException("A package with the same name already exists.");
        }
    }

    private static SubscriptionPackageDto ToDto(SubscriptionPackage package)
    {
        return new SubscriptionPackageDto
        {
            Id = package.Id,
            Name = package.Name,
            Description = package.Description,
            Price = package.Price,
            DurationDays = package.DurationDays,
            MaxTokens = package.MaxTokens,
            IsActive = package.IsActive,
            CreatedAt = package.CreatedAt,
            UpdatedAt = package.UpdatedAt,
            NumberOfPayments = package.Payments?.Count ?? 0
        };
    }
}