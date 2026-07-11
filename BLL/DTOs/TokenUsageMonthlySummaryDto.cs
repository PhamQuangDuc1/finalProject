namespace BLL.DTOs;

public class TokenUsageMonthlySummaryDto
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public decimal EstimatedCostUsd { get; set; }
}
