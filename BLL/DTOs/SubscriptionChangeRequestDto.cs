namespace BLL.DTOs;

public class SubscriptionChangeRequestDto
{
    public int SubscriptionId { get; set; }

    public int TargetPackageId { get; set; }

    public string ApplyMode { get; set; } = "Immediate";

    public string? Reason { get; set; }
}