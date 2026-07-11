namespace BLL.DTOs;

public class AiUsageMonthlySummaryDto
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int TotalTokens { get; set; }

    public decimal EstimatedCost { get; set; }
}
