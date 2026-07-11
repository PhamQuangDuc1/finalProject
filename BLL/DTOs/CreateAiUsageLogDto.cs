using DAL.Entities;

namespace BLL.DTOs;

public class CreateAiUsageLogDto
{
    public int? UserId { get; set; }

    public int? DocumentId { get; set; }

    public AiOperationType OperationType { get; set; }

    public string ModelName { get; set; } = string.Empty;

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public decimal EstimatedCost { get; set; }
}
