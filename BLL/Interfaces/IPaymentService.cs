using BLL.DTOs;
using DAL.Entities;

namespace BLL.Interfaces;

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentDto>> GetAllPaymentsAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentDto>> GetMyPaymentsAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default);

    Task<PaymentDto?> GetPaymentByIdAsync(CurrentUserDto currentUser, int paymentId, CancellationToken cancellationToken = default);

    Task<PaymentResultDto> CreatePaymentAsync(CurrentUserDto currentUser, int packageId, string? ipAddress, CancellationToken cancellationToken = default);

    Task<PaymentDto?> HandleVnPayReturnAsync(IDictionary<string, string?> vnpayData, CancellationToken cancellationToken = default);

    Task<PaymentDto?> HandleVnPayIpnAsync(IDictionary<string, string?> vnpayData, CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(CurrentUserDto currentUser, int paymentId, string? note, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(CurrentUserDto currentUser, int paymentId, string? note, CancellationToken cancellationToken = default);

    Task<UserSubscriptionDto?> GetMyActiveSubscriptionAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSubscriptionDto>> GetMySubscriptionsAsync(CurrentUserDto currentUser, CancellationToken cancellationToken = default);

    Task DemoConfirmAsync(CurrentUserDto currentUser, int paymentId, string? note, CancellationToken cancellationToken = default);
}
