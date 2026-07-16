using BLL.DTOs;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISubscriptionPackageRepository _packageRepository;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IVnPayGateway _vnPayGateway;

    public PaymentService(
        IPaymentRepository paymentRepository,
        ISubscriptionPackageRepository packageRepository,
        IUserSubscriptionRepository userSubscriptionRepository,
        IUserRepository userRepository,
        IVnPayGateway vnPayGateway)
    {
        _paymentRepository = paymentRepository;
        _packageRepository = packageRepository;
        _userSubscriptionRepository = userSubscriptionRepository;
        _userRepository = userRepository;
        _vnPayGateway = vnPayGateway;
    }

    public async Task<IReadOnlyList<PaymentDto>> GetAllPaymentsAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);
        var payments = await _paymentRepository.GetAllAsync(cancellationToken);
        return payments.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<PaymentDto>> GetMyPaymentsAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireAuthenticated(currentUser);
        var payments = await _paymentRepository.GetByUserAsync(currentUser.UserId, cancellationToken);
        return payments.Select(ToDto).ToList();
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(CurrentUserDto currentUser, int paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            return null;
        }

        if (currentUser.Role != UserRole.Admin && payment.UserId != currentUser.UserId)
        {
            throw new UnauthorizedAccessException("You can only view your own payments.");
        }

        return ToDto(payment);
    }

    public async Task<PaymentResultDto> CreatePaymentAsync(CurrentUserDto currentUser, int packageId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireBuyer(currentUser);

        var package = await _packageRepository.GetByIdAsync(packageId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription package was not found.");

        if (!package.IsActive)
        {
            throw new InvalidOperationException("This subscription package is no longer available.");
        }

        await _userSubscriptionRepository.ProcessScheduledDowngradesAsync(DateTime.UtcNow, cancellationToken);
        await _userSubscriptionRepository.DeactivateExpiredAsync(cancellationToken);

        if (await _userSubscriptionRepository.HasActiveSubscriptionAsync(currentUser.UserId, cancellationToken))
        {
            throw new InvalidOperationException("You already have an active subscription. Please wait until it expires before buying a new package.");
        }

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(15);
        var tempOrderId = _vnPayGateway.BuildOrderId(0);

        var payment = new Payment
        {
            UserId = currentUser.UserId,
            SubscriptionPackageId = package.Id,
            Amount = package.Price,
            Status = PaymentStatus.Pending,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            Gateway = PaymentGateway.VNPay,
            GatewayOrderId = tempOrderId,
            Note = "Chờ VNPay IPN"
        };

        await _paymentRepository.AddAsync(payment, cancellationToken);

        var finalOrderId = _vnPayGateway.BuildOrderId(payment.Id);
        payment.GatewayOrderId = finalOrderId;
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        var vnPayRequest = new VNPayCreatePaymentRequest
        {
            PaymentId = payment.Id,
            OrderId = finalOrderId,
            Amount = (long)Math.Round(payment.Amount),
            OrderInfo = $"Thanh toan goi {package.Name}",
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress!
        };

        var vnPayResult = _vnPayGateway.CreatePaymentUrl(vnPayRequest);

        payment.Note = string.IsNullOrWhiteSpace(vnPayResult.Message) ? "Chờ VNPay IPN" : $"VNPay: {vnPayResult.Message}";
        payment.GatewayPayUrl = vnPayResult.PayUrl;
        payment.GatewayQrUrl = vnPayResult.QrCodeUrl;
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        return new PaymentResultDto
        {
            PaymentId = payment.Id,
            Amount = payment.Amount,
            Status = payment.Status,
            StatusLabel = GetStatusLabel(payment.Status),
            Gateway = payment.Gateway,
            GatewayLabel = payment.Gateway.ToString(),
            OrderId = finalOrderId,
            PayUrl = vnPayResult.PayUrl,
            QrCodeUrl = vnPayResult.QrCodeUrl,
            ExpiresAt = expiresAt,
            Success = vnPayResult.IsSuccess,
            Message = vnPayResult.Message
        };
    }

    public Task<PaymentDto?> HandleVnPayReturnAsync(IDictionary<string, string?> vnpayData, CancellationToken cancellationToken = default)
    {
        var success = vnpayData.TryGetValue("vnp_ResponseCode", out var code)
                      && code == "00"
                      && vnpayData.TryGetValue("vnp_TransactionStatus", out var status)
                      && status == "00";
        return ProcessVnPayResultAsync(vnpayData, success, "ReturnUrl", cancellationToken);
    }

    public Task<PaymentDto?> HandleVnPayIpnAsync(IDictionary<string, string?> vnpayData, CancellationToken cancellationToken = default)
    {
        var success = vnpayData.TryGetValue("vnp_ResponseCode", out var code)
                      && code == "00"
                      && vnpayData.TryGetValue("vnp_TransactionStatus", out var status)
                      && status == "00";
        return ProcessVnPayResultAsync(vnpayData, success, "IPN", cancellationToken);
    }

    private async Task<PaymentDto?> ProcessVnPayResultAsync(IDictionary<string, string?> vnpayData, bool success, string source, CancellationToken cancellationToken)
    {
        var orderId = vnpayData.TryGetValue("vnp_TxnRef", out var o) ? o ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new InvalidOperationException($"VNPay {source} missing vnp_TxnRef");
        }

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException($"VNPay {source}: payment not found for order {orderId}.");

        if (payment.Status != PaymentStatus.Pending)
        {
            return ToDto(payment);
        }

        var transId = vnpayData.TryGetValue("vnp_TransactionNo", out var t) ? t : null;
        var amountValue = vnpayData.TryGetValue("vnp_Amount", out var a) ? a : null;
        var responseCode = vnpayData.TryGetValue("vnp_ResponseCode", out var rc) ? rc : "unknown";

        if (success)
        {
            payment.Status = PaymentStatus.Completed;
            payment.CompletedAt = DateTime.UtcNow;
            payment.GatewayTransactionId = string.IsNullOrWhiteSpace(transId) ? null : transId;
            payment.Note = $"VNPay thành công qua {source} (transId={payment.GatewayTransactionId}, amount={amountValue})";
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            var package = await _packageRepository.GetByIdAsync(payment.SubscriptionPackageId, cancellationToken)
                ?? throw new InvalidOperationException("Subscription package for this payment was not found.");
            await ActivateSubscriptionAsync(payment, package, cancellationToken);
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.Note = $"VNPay that bai qua {source} (code={responseCode})";
            await _paymentRepository.UpdateAsync(payment, cancellationToken);
        }

        var refreshed = await _paymentRepository.GetByIdAsync(payment.Id, cancellationToken);
        return refreshed is null ? null : ToDto(refreshed);
    }

    public async Task MarkCompletedAsync(CurrentUserDto currentUser, int paymentId, string? note, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);
        await UpdatePaymentStatusAsync(currentUser, paymentId, PaymentStatus.Completed, note, cancellationToken);
    }

    public async Task MarkFailedAsync(CurrentUserDto currentUser, int paymentId, string? note, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireRole(currentUser, UserRole.Admin);
        await UpdatePaymentStatusAsync(currentUser, paymentId, PaymentStatus.Failed, note, cancellationToken);
    }

    public async Task<UserSubscriptionDto?> GetMyActiveSubscriptionAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireAuthenticated(currentUser);
        var now = DateTime.UtcNow;
        await _userSubscriptionRepository.ProcessScheduledDowngradesAsync(now, cancellationToken);
        await _userSubscriptionRepository.DeactivateExpiredAsync(cancellationToken);

        var subscription = await _userSubscriptionRepository.GetActiveByUserAsync(currentUser.UserId, cancellationToken);
        return subscription is null ? null : ToDto(subscription);
    }

    public async Task<IReadOnlyList<UserSubscriptionDto>> GetMySubscriptionsAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default)
    {
        AuthorizationGuard.RequireAuthenticated(currentUser);
        var now = DateTime.UtcNow;
        await _userSubscriptionRepository.ProcessScheduledDowngradesAsync(now, cancellationToken);
        await _userSubscriptionRepository.DeactivateExpiredAsync(cancellationToken);

        var subscriptions = await _userSubscriptionRepository.GetByUserAsync(currentUser.UserId, cancellationToken);
        return subscriptions.Select(ToDto).ToList();
    }

    private async Task UpdatePaymentStatusAsync(
        CurrentUserDto currentUser,
        int paymentId,
        PaymentStatus newStatus,
        string? note,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken)
            ?? throw new InvalidOperationException("Payment was not found.");

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException($"Only Pending payments can be updated. Current status: {payment.Status}.");
        }

        var now = DateTime.UtcNow;
        payment.Status = newStatus;
        payment.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        payment.ReviewedByAdminId = currentUser.UserId;

        if (newStatus == PaymentStatus.Completed)
        {
            payment.CompletedAt = now;
        }

        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        if (newStatus == PaymentStatus.Completed)
        {
            var package = await _packageRepository.GetByIdAsync(payment.SubscriptionPackageId, cancellationToken)
                ?? throw new InvalidOperationException("Subscription package for this payment was not found.");
            await ActivateSubscriptionAsync(payment, package, cancellationToken);
        }
    }

    private async Task ActivateSubscriptionAsync(Payment payment, SubscriptionPackage package, CancellationToken cancellationToken)
    {
        await _userSubscriptionRepository.DeactivateExpiredAsync(cancellationToken);

        var current = await _userSubscriptionRepository.GetActiveByUserAsync(payment.UserId, cancellationToken);
        var startDate = current is null
            ? DateTime.UtcNow
            : current.EndDate;

        var subscription = new UserSubscription
        {
            UserId = payment.UserId,
            SubscriptionPackageId = package.Id,
            PaymentId = payment.Id,
            StartDate = startDate,
            EndDate = startDate.AddDays(package.DurationDays),
            RemainingTokens = package.MaxTokens,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userSubscriptionRepository.AddAsync(subscription, cancellationToken);
    }

    private static PaymentDto ToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            UserId = payment.UserId,
            UserFullName = payment.User?.FullName ?? string.Empty,
            UserRole = payment.User?.Role.ToString() ?? string.Empty,
            SubscriptionPackageId = payment.SubscriptionPackageId,
            SubscriptionPackageName = payment.SubscriptionPackage?.Name ?? string.Empty,
            Amount = payment.Amount,
            Status = payment.Status,
            StatusLabel = GetStatusLabel(payment.Status),
            CreatedAt = payment.CreatedAt,
            CompletedAt = payment.CompletedAt,
            ReviewedByAdminId = payment.ReviewedByAdminId,
            ReviewedByAdminName = payment.ReviewedByAdmin?.FullName ?? string.Empty,
            Note = payment.Note,
            Gateway = payment.Gateway,
            GatewayLabel = GetGatewayLabel(payment.Gateway),
            GatewayQrUrl = payment.GatewayQrUrl,
            GatewayPayUrl = payment.GatewayPayUrl,
            GatewayDeeplink = payment.GatewayDeeplink,
            GatewayOrderId = payment.GatewayOrderId,
            ExpiresAt = payment.ExpiresAt
        };
    }

    private static UserSubscriptionDto ToDto(UserSubscription subscription)
    {
        return new UserSubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            SubscriptionPackageId = subscription.SubscriptionPackageId,
            SubscriptionPackageName = subscription.SubscriptionPackage?.Name ?? string.Empty,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            RemainingTokens = subscription.RemainingTokens,
            MaxTokens = subscription.SubscriptionPackage?.MaxTokens ?? 0,
            IsActive = subscription.IsActive && subscription.EndDate > DateTime.UtcNow,
            DeactivatedAt = subscription.DeactivatedAt
        };
    }

    private static string GetStatusLabel(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "Chờ VNPay xác nhận",
        PaymentStatus.Completed => "Đã thanh toán",
        PaymentStatus.Failed => "Thất bại",
        _ => status.ToString()
    };

    private static string GetGatewayLabel(PaymentGateway gateway) => gateway switch
    {
        PaymentGateway.None => "Không",
        PaymentGateway.MoMo => "MoMo",
        PaymentGateway.VNPay => "VNPay",
        _ => gateway.ToString()
    };
}
