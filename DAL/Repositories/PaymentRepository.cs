using DAL.Data;
using DAL.Entities;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _dbContext;

    public PaymentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(payment => payment.User)
            .Include(payment => payment.SubscriptionPackage)
            .Include(payment => payment.ReviewedByAdmin)
            .OrderByDescending(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(payment => payment.SubscriptionPackage)
            .Include(payment => payment.ReviewedByAdmin)
            .Where(payment => payment.UserId == userId)
            .OrderByDescending(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(payment => payment.User)
            .Include(payment => payment.SubscriptionPackage)
            .Include(payment => payment.ReviewedByAdmin)
            .Where(payment => payment.Status == status)
            .OrderByDescending(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Payment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Payments
            .Include(payment => payment.User)
            .Include(payment => payment.SubscriptionPackage)
            .Include(payment => payment.ReviewedByAdmin)
            .FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);
    }

    public Task<Payment?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Payments
            .Include(payment => payment.User)
            .Include(payment => payment.SubscriptionPackage)
            .Include(payment => payment.ReviewedByAdmin)
            .FirstOrDefaultAsync(payment => payment.GatewayOrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _dbContext.Payments.Update(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}