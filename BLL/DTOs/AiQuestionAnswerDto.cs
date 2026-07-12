namespace BLL.DTOs;

public class AiQuestionAnswerDto
{
    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }
}
