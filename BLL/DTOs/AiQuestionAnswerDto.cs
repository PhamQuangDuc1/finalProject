namespace BLL.DTOs;

public class AiQuestionAnswerDto
{
    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public IReadOnlyList<AiAnswerCitationDto> Citations { get; set; } = Array.Empty<AiAnswerCitationDto>();

    public string ModelName { get; set; } = string.Empty;

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }
}

public class AiAnswerCitationDto
{
    public int DocumentId { get; set; }

    public string DocumentTitle { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public string Excerpt { get; set; } = string.Empty;
}
