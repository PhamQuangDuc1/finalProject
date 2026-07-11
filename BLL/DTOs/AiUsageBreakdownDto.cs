using DAL.Entities;

namespace BLL.DTOs;

public class AiUsageBreakdownDto
{
    public string ModelName { get; set; } = string.Empty;

    public AiOperationType OperationType { get; set; }

    public int TotalTokens { get; set; }

    public decimal EstimatedCost { get; set; }
}
