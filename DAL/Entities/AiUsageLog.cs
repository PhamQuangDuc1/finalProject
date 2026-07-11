namespace DAL.Entities;

public class AiUsageLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? DocumentId { get; set; }

    public AiOperationType OperationType { get; set; }

    public string ModelName { get; set; } = string.Empty;

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }

    public decimal EstimatedCost { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    public Document? Document { get; set; }
}
