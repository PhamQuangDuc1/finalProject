using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class AdminSubscriptionService : IAdminSubscriptionService
{
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly ISubscriptionPackageRepository _packageRepository;

    public AdminSubscriptionService(
        IUserSubscriptionRepository userSubscriptionRepository,
        ISubscriptionPackageRepository packageRepository)
    {
        _userSubscriptionRepository = userSubscriptionRepository;
        _packageRepository = packageRepository;
    }

    public async Task<IReadOnlyList<AdminUserSubscriptionSummaryDto>> GetAllActiveSubscriptionsAsync(
        CurrentUserDto currentUser,
        CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var now = DateTime.UtcNow;
        await _userSubscriptionRepository.ProcessScheduledDowngradesAsync(now, cancellationToken);
        await _userSubscriptionRepository.DeactivateExpiredAsync(cancellationToken);

        var active = await _userSubscriptionRepository.GetAllActiveWithUsersAsync(cancellationToken);
        return active.Select(ToSummary).ToList();
    }

    public async Task<AdminChangePackageFormDto?> GetChangePackageFormAsync(
        CurrentUserDto currentUser,
        int subscriptionId,
        CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var subscription = await _userSubscriptionRepository.GetByIdWithPackageAsync(subscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription was not found.");

        if (!subscription.IsActive || subscription.EndDate <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("This subscription is no longer active and cannot be changed.");
        }

        var packages = await _packageRepository.GetActiveAsync(cancellationToken);

        return new AdminChangePackageFormDto
        {
            CurrentSubscription = ToSummary(subscription),
            AvailablePackages = packages.Select(ToPackageDto).ToList()
        };
    }

    public async Task<AdminUserSubscriptionSummaryDto?> CancelSubscriptionAsync(
        CurrentUserDto currentUser,
        int subscriptionId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        var subscription = await _userSubscriptionRepository.GetByIdWithPackageAsync(subscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription was not found.");

        if (!subscription.IsActive)
        {
            return ToSummary(subscription);
        }

        subscription.IsActive = false;
        subscription.DeactivatedAt = DateTime.UtcNow;
        subscription.ScheduledDowngradePackageId = null;

        await _userSubscriptionRepository.UpdateAsync(subscription, cancellationToken);

        var refreshed = await _userSubscriptionRepository.GetByIdWithPackageAsync(subscriptionId, cancellationToken);
        return refreshed is null ? null : ToSummary(refreshed);
    }

    public async Task<AdminUserSubscriptionSummaryDto?> ChangePackageAsync(
        CurrentUserDto currentUser,
        SubscriptionChangeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);

        if (request is null)
        {
            throw new InvalidOperationException("Request payload is required.");
        }

        var subscription = await _userSubscriptionRepository.GetByIdWithPackageAsync(request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription was not found.");

        if (!subscription.IsActive || subscription.EndDate <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("This subscription is no longer active and cannot be changed.");
        }

        var targetPackage = await _packageRepository.GetByIdAsync(request.TargetPackageId, cancellationToken)
            ?? throw new InvalidOperationException("Target subscription package was not found.");

        if (!targetPackage.IsActive)
        {
            throw new InvalidOperationException("Target subscription package is not active.");
        }

        if (targetPackage.Id == subscription.SubscriptionPackageId)
        {
            throw new InvalidOperationException("Target package must be different from the current package.");
        }

        var now = DateTime.UtcNow;
        var remainingDays = (int)Math.Ceiling((subscription.EndDate - now).TotalDays);
        if (remainingDays < 0)
        {
            remainingDays = 0;
        }

        var isUpgrade = targetPackage.MaxTokens > (subscription.SubscriptionPackage?.MaxTokens ?? 0);
        var applyMode = (request.ApplyMode ?? "Immediate").Trim();

        if (isUpgrade)
        {
            subscription.IsActive = false;
            subscription.DeactivatedAt = now;
            subscription.ScheduledDowngradePackageId = null;

            var startDate = now;
            var replacement = new UserSubscription
            {
                UserId = subscription.UserId,
                SubscriptionPackageId = targetPackage.Id,
                PaymentId = subscription.PaymentId,
                StartDate = startDate,
                EndDate = startDate.AddDays(targetPackage.DurationDays + remainingDays),
                RemainingTokens = targetPackage.MaxTokens,
                IsActive = true,
                CreatedAt = now
            };
            _userSubscriptionRepository.Add(replacement);
        }
        else
        {
            if (string.Equals(applyMode, "AtExpiry", StringComparison.OrdinalIgnoreCase))
            {
                subscription.ScheduledDowngradePackageId = targetPackage.Id;
                await _userSubscriptionRepository.UpdateAsync(subscription, cancellationToken);
            }
            else
            {
                subscription.IsActive = false;
                subscription.DeactivatedAt = now;
                subscription.ScheduledDowngradePackageId = null;

                var startDate = now;
                var replacement = new UserSubscription
                {
                    UserId = subscription.UserId,
                    SubscriptionPackageId = targetPackage.Id,
                    PaymentId = subscription.PaymentId,
                    StartDate = startDate,
                    EndDate = startDate.AddDays(targetPackage.DurationDays + remainingDays),
                    RemainingTokens = targetPackage.MaxTokens,
                    IsActive = true,
                    CreatedAt = now
                };
                _userSubscriptionRepository.Add(replacement);
            }
        }

        if (isUpgrade || string.Equals(applyMode, "Immediate", StringComparison.OrdinalIgnoreCase))
        {
            await _userSubscriptionRepository.SaveChangesAsync(cancellationToken);
        }

        var refreshed = await _userSubscriptionRepository.GetByIdWithPackageAsync(request.SubscriptionId, cancellationToken);
        return refreshed is null ? null : ToSummary(refreshed);
    }

    private static AdminUserSubscriptionSummaryDto ToSummary(UserSubscription subscription)
    {
        return new AdminUserSubscriptionSummaryDto
        {
            SubscriptionId = subscription.Id,
            UserId = subscription.UserId,
            Username = subscription.User?.Username ?? string.Empty,
            FullName = subscription.User?.FullName ?? string.Empty,
            Role = subscription.User?.Role.ToString() ?? string.Empty,
            SubscriptionPackageId = subscription.SubscriptionPackageId,
            SubscriptionPackageName = subscription.SubscriptionPackage?.Name ?? string.Empty,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            RemainingTokens = subscription.RemainingTokens,
            MaxTokens = subscription.SubscriptionPackage?.MaxTokens ?? 0,
            ScheduledDowngradePackageId = subscription.ScheduledDowngradePackageId,
            ScheduledDowngradePackageName = subscription.ScheduledDowngradePackage?.Name
        };
    }

    private static SubscriptionPackageDto ToPackageDto(SubscriptionPackage package)
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