namespace BLL.DTOs;

public class AiUsageModelSummaryDto
{
    public string ModelName { get; set; } = string.Empty;

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }

    public decimal EstimatedCost { get; set; }

    public int Requests { get; set; }
}
