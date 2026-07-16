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

    [Fact]
    public async Task GetDashboardAsync_SortsDailySummariesByNewestDateByDefault()
    {
        var logs = new[]
        {
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 10,
                CompletionTokens = 5,
                TotalTokens = 15
            },
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 10,
                CompletionTokens = 5,
                TotalTokens = 15
            },
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 40,
                CompletionTokens = 10,
                TotalTokens = 50
            }
        };
        var service = new AiUsageService(new FakeAiUsageRepository(logs), new AiCostEstimator());

        var dashboard = await service.GetDashboardAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin },
            new AiUsageDashboardFilterDto { Year = 2026, Month = 7 });

        Assert.Equal(new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc), dashboard.DailySummaries[0].Date);
        Assert.Equal(new DateTime(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc), dashboard.DailySummaries[1].Date);
    }

    [Fact]
    public async Task GetDashboardAsync_SortsDailySummariesByTotalTokensThenNewestDate()
    {
        var logs = new[]
        {
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 10,
                CompletionTokens = 5,
                TotalTokens = 15
            },
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 10,
                CompletionTokens = 5,
                TotalTokens = 15
            },
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 40,
                CompletionTokens = 10,
                TotalTokens = 50
            }
        };
        var service = new AiUsageService(new FakeAiUsageRepository(logs), new AiCostEstimator());

        var dashboard = await service.GetDashboardAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin },
            new AiUsageDashboardFilterDto { Year = 2026, Month = 7, SortBy = AiUsageDailySortBy.TotalTokens });

        Assert.Equal(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc), dashboard.DailySummaries[0].Date);
        Assert.Equal(new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), dashboard.DailySummaries[1].Date);
    }

    [Fact]
    public async Task GetDashboardAsync_SortsDailySummariesByEstimatedCostThenNewestDate()
    {
        var logs = new[]
        {
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 10,
                CompletionTokens = 5,
                TotalTokens = 15,
                EstimatedCost = 0.10m
            },
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 40,
                CompletionTokens = 10,
                TotalTokens = 50,
                EstimatedCost = 0.10m
            },
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 1,
                CompletionTokens = 1,
                TotalTokens = 2,
                EstimatedCost = 0.50m
            }
        };
        var service = new AiUsageService(new FakeAiUsageRepository(logs), new AiCostEstimator());

        var dashboard = await service.GetDashboardAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin },
            new AiUsageDashboardFilterDto { Year = 2026, Month = 7, SortBy = AiUsageDailySortBy.EstimatedCost });

        Assert.Equal(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc), dashboard.DailySummaries[0].Date);
        Assert.Equal(new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), dashboard.DailySummaries[1].Date);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenDateScopeToday_ReturnsOnlySelectedDayTotals()
    {
        var logs = new[]
        {
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 100,
                CompletionTokens = 20,
                TotalTokens = 120,
                EstimatedCost = 0.10m
            },
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 30,
                CompletionTokens = 10,
                TotalTokens = 40,
                EstimatedCost = 0.20m
            },
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 16, 15, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 5,
                CompletionTokens = 5,
                TotalTokens = 10,
                EstimatedCost = 0.05m
            }
        };
        var service = new AiUsageService(new FakeAiUsageRepository(logs), new AiCostEstimator());

        var dashboard = await service.GetDashboardAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin },
            new AiUsageDashboardFilterDto
            {
                Year = 2026,
                Month = 7,
                DateScope = AiUsageDateScope.Today,
                Day = new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc)
            });

        Assert.Equal(35, dashboard.TotalPromptTokensThisMonth);
        Assert.Equal(15, dashboard.TotalCompletionTokensThisMonth);
        Assert.Equal(50, dashboard.TotalTokensThisMonth);
        Assert.Equal(0.25m, dashboard.EstimatedCostThisMonth);
        Assert.Equal(2, dashboard.RequestsThisMonth);
        var day = Assert.Single(dashboard.DailySummaries);
        Assert.Equal(new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), day.Date);
        Assert.Equal(50, day.TotalTokens);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenDateScopeThisWeek_ReturnsOnlySelectedWeekTotals()
    {
        var logs = new[]
        {
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 100,
                CompletionTokens = 20,
                TotalTokens = 120,
                EstimatedCost = 0.10m
            },
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 30,
                CompletionTokens = 10,
                TotalTokens = 40,
                EstimatedCost = 0.20m
            },
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 16, 15, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 5,
                CompletionTokens = 5,
                TotalTokens = 10,
                EstimatedCost = 0.05m
            },
            new AiUsageLog
            {
                CreatedAt = new DateTime(2026, 7, 20, 9, 0, 0, DateTimeKind.Utc),
                ModelName = "gemini",
                PromptTokens = 200,
                CompletionTokens = 20,
                TotalTokens = 220,
                EstimatedCost = 0.40m
            }
        };
        var service = new AiUsageService(new FakeAiUsageRepository(logs), new AiCostEstimator());

        var dashboard = await service.GetDashboardAsync(
            new CurrentUserDto { UserId = 1, Role = UserRole.Admin },
            new AiUsageDashboardFilterDto
            {
                Year = 2026,
                Month = 7,
                DateScope = AiUsageDateScope.ThisWeek,
                Day = new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc)
            });

        Assert.Equal(35, dashboard.TotalPromptTokensThisMonth);
        Assert.Equal(15, dashboard.TotalCompletionTokensThisMonth);
        Assert.Equal(50, dashboard.TotalTokensThisMonth);
        Assert.Equal(0.25m, dashboard.EstimatedCostThisMonth);
        Assert.Equal(2, dashboard.RequestsThisMonth);
        Assert.Equal(7, dashboard.DailySummaries.Count);
        Assert.Equal(new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc), dashboard.DailySummaries[0].Date);
        Assert.Equal(new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc), dashboard.DailySummaries[^1].Date);
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
