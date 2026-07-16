using DAL.Entities;

namespace BLL.DTOs;

public class AiUsageDashboardFilterDto
{
    public int Year { get; set; }

    public int Month { get; set; }

    public string? ModelName { get; set; }

    public AiOperationType? OperationType { get; set; }

    public AiUsageDateScope DateScope { get; set; } = AiUsageDateScope.Month;

    public DateTime? Day { get; set; }

    public AiUsageDailySortBy SortBy { get; set; } = AiUsageDailySortBy.NewestDate;
}
