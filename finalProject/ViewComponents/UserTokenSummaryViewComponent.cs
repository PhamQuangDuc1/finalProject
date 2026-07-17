using System.Security.Claims;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using Microsoft.AspNetCore.Mvc;

namespace finalProject.ViewComponents;

public class UserTokenSummaryViewComponent : ViewComponent
{
    private readonly IPaymentService _paymentService;

    public UserTokenSummaryViewComponent(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(new UserTokenSummaryViewModel());
        }

        var userIdClaim = (User as ClaimsPrincipal)?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return View(new UserTokenSummaryViewModel());
        }

        var currentUser = new CurrentUserDto
        {
            UserId = userId,
            Role = Enum.TryParse<UserRole>((User as ClaimsPrincipal)?.FindFirstValue(ClaimTypes.Role), out var role) ? role : UserRole.Student
        };

        var subscription = await _paymentService.GetMyActiveSubscriptionAsync(currentUser, cancellationToken);
        var model = new UserTokenSummaryViewModel
        {
            HasSubscription = subscription is not null,
            RemainingTokens = subscription?.RemainingTokens ?? 0,
            MaxTokens = subscription?.MaxTokens ?? 0,
            SubscriptionName = subscription?.SubscriptionPackageName ?? string.Empty
        };

        return View(model);
    }
}

public class UserTokenSummaryViewModel
{
    public bool HasSubscription { get; set; }
    public int RemainingTokens { get; set; }
    public int MaxTokens { get; set; }
    public string SubscriptionName { get; set; } = string.Empty;

    public int UsedTokens => MaxTokens - RemainingTokens;
    public double UsagePercentage => MaxTokens > 0 ? (double)UsedTokens / MaxTokens * 100 : 0;
}
