using BLL.DTOs;
using BLL.Interfaces;
using BLL.Services;
using DAL.Entities;
using DAL.Interfaces;

namespace BLL.Tests;

public class AiUsageServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ReturnsMonthlyTotals_ForAdmin()
    {
        var logs = new[]
        {
            new AiUsageLog
            {
                ModelName = "gpt-4o-mini",
                OperationType = AiOperationType.ChatCompletion,
                PromptTokens = 100,
                CompletionTokens = 50,
                TotalTokens = 150,
                EstimatedCost = 0.001m,
                CreatedAt = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc)
            },
            new AiUsageLog
            {
                ModelName = "text-embedding-3-small",
                OperationType = AiOperationType.DocumentEmbedding,
                PromptTokens = 200,
                CompletionTokens = 0,
                TotalTokens = 200,
                EstimatedCost = 0.002m,
                CreatedAt = new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc)
            }
        };
        var service = new AiUsageService(new FakeAiUsageRepository(logs), new AiCostEstimator());

        var dashboard = await service.GetDashboardAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin },
            new AiUsageDashboardFilterDto { Year = 2026, Month = 7 });

        Assert.Equal(300, dashboard.TotalPromptTokensThisMonth);
        Assert.Equal(50, dashboard.TotalCompletionTokensThisMonth);
        Assert.Equal(350, dashboard.TotalTokensThisMonth);
        Assert.Equal(0.003m, dashboard.EstimatedCostThisMonth);
        Assert.Equal(2, dashboard.RequestsThisMonth);
        Assert.Equal(175m, dashboard.AverageTokensPerRequest);
        Assert.Equal(31, dashboard.DailySummaries.Count);
        Assert.Equal(2, dashboard.ModelSummaries.Count);
        Assert.Equal(2, dashboard.OperationSummaries.Count);
    }

    [Fact]
    public async Task GetDashboardAsync_Throws_WhenUserIsNotAdmin()
    {
        var service = new AiUsageService(new FakeAiUsageRepository(Array.Empty<AiUsageLog>()), new AiCostEstimator());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetDashboardAsync(
            new CurrentUserDto { UserId = 2, Role = UserRole.Teacher },
            new AiUsageDashboardFilterDto { Year = 2026, Month = 7 }));
    }

    private sealed class FakeAiUsageRepository : IAiUsageRepository
    {
        private readonly IReadOnlyList<AiUsageLog> _logs;

        public FakeAiUsageRepository(IReadOnlyList<AiUsageLog> logs)
        {
            _logs = logs;
        }

        public Task AddAsync(AiUsageLog usageLog, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<AiUsageLog>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_logs);
        }
    }
}
