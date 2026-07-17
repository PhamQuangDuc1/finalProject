using BLL.DTOs;
using BLL.Interfaces;
using BLL.Services;
using DAL.Entities;
using DAL.Interfaces;
using Microsoft.Extensions.Options;

namespace BLL.Tests;

public class PaymentServiceTests
{
    [Fact]
    public async Task GetAllPaymentsAsync_ReturnsAllPayments_WhenAdminRequests()
    {
        var package = new SubscriptionPackage { Id = 1, Name = "Gói Pro", Price = 100_000m };
        var user = new User { Id = 4, FullName = "Student", Role = UserRole.Student };
        var payments = new[]
        {
            new Payment { Id = 1, UserId = 4, User = user, SubscriptionPackage = package, Amount = 100_000m, Status = PaymentStatus.Pending, CreatedAt = DateTime.UtcNow }
        };
        var service = CreateService(payments: payments);

        var result = await service.GetAllPaymentsAsync(new CurrentUserDto { UserId = 1, Role = UserRole.Admin });

        var payment = Assert.Single(result);
        Assert.Equal(1, payment.Id);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal("Student", payment.UserFullName);
        Assert.Equal("Gói Pro", payment.SubscriptionPackageName);
    }

    [Fact]
    public async Task GetAllPaymentsAsync_Throws_WhenCurrentUserIsNotAdmin()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAllPaymentsAsync(
            new CurrentUserDto { UserId = 4, Role = UserRole.Student }));
    }

    [Fact]
    public async Task GetMyPaymentsAsync_ReturnsOnlyOwnPayments()
    {
        var service = CreateService(payments: Array.Empty<Payment>());

        var result = await service.GetMyPaymentsAsync(new CurrentUserDto { UserId = 4, Role = UserRole.Student });

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPaymentByIdAsync_Throws_WhenUserViewsOtherUserPayment()
    {
        var payment = new Payment { Id = 1, UserId = 2, SubscriptionPackageId = 1, Amount = 100m, Status = PaymentStatus.Pending, CreatedAt = DateTime.UtcNow };
        var service = CreateService(payments: new[] { payment });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetPaymentByIdAsync(
            new CurrentUserDto { UserId = 4, Role = UserRole.Student }, 1));
    }

    [Fact]
    public async Task CreatePaymentAsync_ReturnsPaymentWithPendingStatus_WhenTeacherBuysPackage()
    {
        var package = new SubscriptionPackage { Id = 1, Name = "Gói Pro", Price = 100_000m, IsActive = true, DurationDays = 30, MaxTokens = 5000 };
        var paymentRepository = new FakePaymentRepository();
        var subscriptionRepository = new FakeUserSubscriptionRepository();
        var vnPay = new FakeVnPayGateway();
        var service = CreateService(
            payments: Array.Empty<Payment>(),
            packages: new[] { package },
            paymentRepository: paymentRepository,
            subscriptionRepository: subscriptionRepository,
            vnPayGateway: vnPay);

        var result = await service.CreatePaymentAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher }, 1, "127.0.0.1");

        Assert.True(result.PaymentId > 0);
        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Pending, result.Status);
        Assert.Equal(PaymentGateway.VNPay, result.Gateway);
        Assert.False(string.IsNullOrWhiteSpace(result.OrderId));
        Assert.False(string.IsNullOrWhiteSpace(result.PayUrl));
        Assert.NotNull(result.ExpiresAt);

        var saved = Assert.Single(paymentRepository.Payments);
        Assert.Equal(PaymentStatus.Pending, saved.Status);
        Assert.Equal(PaymentGateway.VNPay, saved.Gateway);
        Assert.Equal(result.OrderId, saved.GatewayOrderId);
        Assert.Empty(subscriptionRepository.Subscriptions);
    }

    [Fact]
    public async Task HandleVnPayReturnAsync_ActivatesSubscription_WhenResponseCodeIsZero()
    {
        var package = new SubscriptionPackage { Id = 1, Name = "Gói Pro", Price = 100_000m, IsActive = true, DurationDays = 30, MaxTokens = 5000 };
        var payment = new Payment
        {
            Id = 7,
            UserId = 4,
            SubscriptionPackageId = 1,
            Amount = 100_000m,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Gateway = PaymentGateway.VNPay,
            GatewayOrderId = "VNP20260716000007"
        };
        var paymentRepository = new FakePaymentRepository(new[] { payment });
        var subscriptionRepository = new FakeUserSubscriptionRepository();
        var service = CreateService(
            payments: new[] { payment },
            packages: new[] { package },
            paymentRepository: paymentRepository,
            subscriptionRepository: subscriptionRepository);

        var dto = await service.HandleVnPayReturnAsync(new Dictionary<string, string?>
        {
            ["vnp_TxnRef"] = "VNP20260716000007",
            ["vnp_ResponseCode"] = "00",
            ["vnp_TransactionStatus"] = "00",
            ["vnp_TransactionNo"] = "TRANS-123",
            ["vnp_Amount"] = "10000000"
        });

        Assert.NotNull(dto);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal("TRANS-123", payment.GatewayTransactionId);
        var subscription = Assert.Single(subscriptionRepository.Subscriptions);
        Assert.Equal(4, subscription.UserId);
        Assert.True(subscription.IsActive);
        Assert.Equal(5000, subscription.RemainingTokens);
    }

    [Fact]
    public async Task HandleVnPayIpnAsync_MarksFailed_WhenResponseCodeIsNotZero()
    {
        var package = new SubscriptionPackage { Id = 1, Name = "Gói Pro", Price = 100_000m, IsActive = true };
        var payment = new Payment
        {
            Id = 8,
            UserId = 4,
            SubscriptionPackageId = 1,
            Amount = 100_000m,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Gateway = PaymentGateway.VNPay,
            GatewayOrderId = "VNP20260716000008"
        };
        var paymentRepository = new FakePaymentRepository(new[] { payment });
        var subscriptionRepository = new FakeUserSubscriptionRepository();
        var service = CreateService(
            payments: new[] { payment },
            packages: new[] { package },
            paymentRepository: paymentRepository,
            subscriptionRepository: subscriptionRepository);

        await service.HandleVnPayIpnAsync(new Dictionary<string, string?>
        {
            ["vnp_TxnRef"] = "VNP20260716000008",
            ["vnp_ResponseCode"] = "24",
            ["vnp_TransactionStatus"] = "24"
        });

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Empty(subscriptionRepository.Subscriptions);
    }

    [Fact]
    public async Task CreatePaymentAsync_Throws_WhenAdminTriesToBuy()
    {
        var package = new SubscriptionPackage { Id = 1, Name = "Gói Pro", Price = 100_000m, IsActive = true };
        var service = CreateService(packages: new[] { package });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreatePaymentAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin }, 1, "127.0.0.1"));
    }

    [Fact]
    public async Task CreatePaymentAsync_Throws_WhenPackageIsInactive()
    {
        var package = new SubscriptionPackage { Id = 1, Name = "Gói Pro", Price = 100_000m, IsActive = false };
        var service = CreateService(packages: new[] { package });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePaymentAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher }, 1, "127.0.0.1"));
    }

    [Fact]
    public async Task CreatePaymentAsync_Throws_WhenUserAlreadyHasActiveSubscription()
    {
        var package = new SubscriptionPackage { Id = 1, Name = "Gói Pro", Price = 100_000m, IsActive = true };
        var subscriptionRepository = new FakeUserSubscriptionRepository(hasActive: true);
        var service = CreateService(
            packages: new[] { package },
            subscriptionRepository: subscriptionRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePaymentAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher }, 1, "127.0.0.1"));
    }

    [Fact]
    public async Task MarkCompletedAsync_ActivatesSubscription_WhenAdminApprovesPayment()
    {
        var package = new SubscriptionPackage { Id = 1, Name = "Gói Pro", Price = 100_000m, IsActive = true, DurationDays = 30, MaxTokens = 5000 };
        var payment = new Payment
        {
            Id = 1,
            UserId = 4,
            SubscriptionPackageId = 1,
            SubscriptionPackage = package,
            Amount = 100_000m,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        var paymentRepository = new FakePaymentRepository(new[] { payment });
        var subscriptionRepository = new FakeUserSubscriptionRepository();
        var service = CreateService(
            payments: new[] { payment },
            packages: new[] { package },
            paymentRepository: paymentRepository,
            subscriptionRepository: subscriptionRepository);

        await service.MarkCompletedAsync(new CurrentUserDto { UserId = 1, Role = UserRole.Admin }, 1, "Đã nhận chuyển khoản");

        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal("Đã nhận chuyển khoản", payment.Note);
        Assert.Equal(1, payment.ReviewedByAdminId);
        Assert.NotNull(payment.CompletedAt);
        var subscription = Assert.Single(subscriptionRepository.Subscriptions);
        Assert.Equal(4, subscription.UserId);
        Assert.Equal(1, subscription.SubscriptionPackageId);
        Assert.True(subscription.IsActive);
        Assert.Equal(5000, subscription.RemainingTokens);
        Assert.True(subscription.EndDate > subscription.StartDate);
    }

    [Fact]
    public async Task MarkCompletedAsync_Throws_WhenPaymentAlreadyProcessed()
    {
        var payment = new Payment
        {
            Id = 1,
            UserId = 4,
            SubscriptionPackageId = 1,
            Amount = 100_000m,
            Status = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
        var service = CreateService(payments: new[] { payment });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkCompletedAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin }, 1, null));
    }

    [Fact]
    public async Task MarkFailedAsync_UpdatesPaymentStatus_WithoutCreatingSubscription()
    {
        var payment = new Payment
        {
            Id = 1,
            UserId = 4,
            SubscriptionPackageId = 1,
            Amount = 100_000m,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        var subscriptionRepository = new FakeUserSubscriptionRepository();
        var service = CreateService(
            payments: new[] { payment },
            subscriptionRepository: subscriptionRepository);

        await service.MarkFailedAsync(new CurrentUserDto { UserId = 1, Role = UserRole.Admin }, 1, "Chuyển khoản sai");

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Chuyển khoản sai", payment.Note);
        Assert.Empty(subscriptionRepository.Subscriptions);
    }

    [Fact]
    public async Task MarkFailedAsync_Throws_WhenCurrentUserIsNotAdmin()
    {
        var payment = new Payment { Id = 1, UserId = 4, SubscriptionPackageId = 1, Amount = 100m, Status = PaymentStatus.Pending, CreatedAt = DateTime.UtcNow };
        var service = CreateService(payments: new[] { payment });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.MarkFailedAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher }, 1, null));
    }

    [Fact]
    public async Task MarkCompletedAsync_Throws_WhenPaymentDoesNotExist()
    {
        var service = CreateService(payments: Array.Empty<Payment>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkCompletedAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin }, 999, null));
    }

    [Fact]
    public async Task CancelMyActiveSubscriptionAsync_DeactivatesCurrentUsersActiveSubscription()
    {
        var subscription = new UserSubscription
        {
            Id = 12,
            UserId = 4,
            SubscriptionPackageId = 1,
            PaymentId = 1,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(29),
            RemainingTokens = 48_000,
            IsActive = true,
            SubscriptionPackage = new SubscriptionPackage { Id = 1, Name = "Gói Sinh viên", MaxTokens = 50_000 }
        };
        var subscriptionRepository = new FakeUserSubscriptionRepository();
        subscriptionRepository.Add(subscription);
        var service = CreateService(subscriptionRepository: subscriptionRepository);

        var result = await service.CancelMyActiveSubscriptionAsync(
            new CurrentUserDto { UserId = 4, Role = UserRole.Student });

        Assert.NotNull(result);
        Assert.False(subscription.IsActive);
        Assert.NotNull(subscription.DeactivatedAt);
        Assert.Equal(subscription.Id, result.Id);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task CancelMyActiveSubscriptionAsync_Throws_WhenAdminRequests()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CancelMyActiveSubscriptionAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin }));
    }

    private static PaymentService CreateService(
        IReadOnlyList<Payment>? payments = null,
        IReadOnlyList<SubscriptionPackage>? packages = null,
        FakePaymentRepository? paymentRepository = null,
        FakeUserSubscriptionRepository? subscriptionRepository = null,
        IVnPayGateway? vnPayGateway = null)
    {
        paymentRepository ??= new FakePaymentRepository(payments ?? Array.Empty<Payment>());
        var packageRepository = new FakeSubscriptionPackageRepository(packages ?? Array.Empty<SubscriptionPackage>());
        subscriptionRepository ??= new FakeUserSubscriptionRepository();
        vnPayGateway ??= new FakeVnPayGateway();

        return new PaymentService(
            paymentRepository,
            packageRepository,
            subscriptionRepository,
            new FakeUserRepository(),
            vnPayGateway);
    }

    private sealed class FakeVnPayGateway : IVnPayGateway
    {
        public VNPayCreatePaymentResult CreatePaymentUrl(VNPayCreatePaymentRequest request)
        {
            return new VNPayCreatePaymentResult
            {
                PaymentId = request.PaymentId,
                OrderId = request.OrderId,
                PayUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?txnRef={request.OrderId}",
                QrCodeUrl = $"/Payments/DemoQr/{request.PaymentId}",
                Message = "OK"
            };
        }

        public bool ValidateSignature(IDictionary<string, string?> vnpayData) => true;

        public string BuildOrderId(int paymentId) => $"VNP20260716{paymentId:000000}";
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        private readonly IReadOnlyList<Payment> _payments;

        public FakePaymentRepository(IReadOnlyList<Payment>? payments = null)
        {
            _payments = payments ?? Array.Empty<Payment>();
        }

        public List<Payment> Payments { get; } = new();

        public Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_payments);
        }

        public Task<IReadOnlyList<Payment>> GetByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Payment>>(
                _payments.Where(payment => payment.UserId == userId).ToList());
        }

        public Task<IReadOnlyList<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Payment>>(
                _payments.Where(payment => payment.Status == status).ToList());
        }

        public Task<Payment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_payments.FirstOrDefault(payment => payment.Id == id));
        }

        public Task<Payment?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_payments.FirstOrDefault(payment => payment.GatewayOrderId == orderId));
        }

        public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            payment.Id = Payments.Count + 1;
            Payments.Add(payment);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSubscriptionPackageRepository : ISubscriptionPackageRepository
    {
        private readonly IReadOnlyList<SubscriptionPackage> _packages;

        public FakeSubscriptionPackageRepository(IReadOnlyList<SubscriptionPackage>? packages = null)
        {
            _packages = packages ?? Array.Empty<SubscriptionPackage>();
        }

        public Task<IReadOnlyList<SubscriptionPackage>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_packages);
        }

        public Task<IReadOnlyList<SubscriptionPackage>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SubscriptionPackage>>(
                _packages.Where(package => package.IsActive).ToList());
        }

        public Task<SubscriptionPackage?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_packages.FirstOrDefault(package => package.Id == id));
        }

        public Task<bool> ExistsByNameAsync(string name, int? excludedId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_packages.Any(package => package.Name == name));
        }

        public Task AddAsync(SubscriptionPackage package, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(SubscriptionPackage package, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserSubscriptionRepository : IUserSubscriptionRepository
    {
        private readonly bool _hasActive;

        public FakeUserSubscriptionRepository(bool hasActive = false)
        {
            _hasActive = hasActive;
        }

        public List<UserSubscription> Subscriptions { get; } = new();

        public Task<IReadOnlyList<UserSubscription>> GetByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<UserSubscription>>(Subscriptions);
        }

        public Task<UserSubscription?> GetActiveByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UserSubscription?>(Subscriptions.FirstOrDefault());
        }

        public Task<UserSubscription?> GetLatestActiveByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UserSubscription?>(Subscriptions.FirstOrDefault());
        }

        public Task<bool> HasActiveSubscriptionAsync(int userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_hasActive);
        }

        public Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default)
        {
            subscription.Id = Subscriptions.Count + 1;
            Subscriptions.Add(subscription);

            return Task.CompletedTask;
        }

        public void Add(UserSubscription subscription)
        {
            subscription.Id = Subscriptions.Count + 1;
            Subscriptions.Add(subscription);
        }

        public Task UpdateAsync(UserSubscription subscription, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeactivateExpiredAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<UserSubscription?> GetByIdWithPackageAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UserSubscription?>(Subscriptions.FirstOrDefault(s => s.Id == id));
        }

        public Task<IReadOnlyList<UserSubscription>> GetAllActiveWithUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<UserSubscription>>(Subscriptions);
        }

        public Task<int> ProcessScheduledDowngradesAsync(DateTime now, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<User?> ValidateUserAsync(string username, string passwordHash, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
