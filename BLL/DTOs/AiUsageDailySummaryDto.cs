namespace BLL.DTOs;

public class AiUsageDailySummaryDto
{
    public DateTime Date { get; set; }

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }

    public decimal EstimatedCost { get; set; }

    public int Requests { get; set; }
}
