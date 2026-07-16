using DAL.Entities;

namespace DAL.Interfaces;

public interface IPaymentRepository
{
    Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Payment?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);

    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
}