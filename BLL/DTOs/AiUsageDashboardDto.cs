using DAL.Entities;

namespace BLL.DTOs;

public class AiUsageDashboardDto
{
    public int Year { get; set; }

    public int Month { get; set; }

    public string? SelectedModelName { get; set; }

    public AiOperationType? SelectedOperationType { get; set; }

    public int TotalPromptTokensThisMonth { get; set; }

    public int TotalCompletionTokensThisMonth { get; set; }

    public int TotalTokensThisMonth { get; set; }

    public decimal EstimatedCostThisMonth { get; set; }

    public int RequestsThisMonth { get; set; }

    public decimal AverageTokensPerRequest { get; set; }

    public IReadOnlyList<AiUsageDailySummaryDto> DailySummaries { get; set; } = Array.Empty<AiUsageDailySummaryDto>();

    public IReadOnlyList<AiUsageModelSummaryDto> ModelSummaries { get; set; } = Array.Empty<AiUsageModelSummaryDto>();

    public IReadOnlyList<AiUsageOperationSummaryDto> OperationSummaries { get; set; } = Array.Empty<AiUsageOperationSummaryDto>();

    public IReadOnlyList<string> AvailableModels { get; set; } = Array.Empty<string>();

    public IReadOnlyList<AiOperationType> AvailableOperationTypes { get; set; } = Array.Empty<AiOperationType>();
}
