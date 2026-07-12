using BLL.DTOs;

namespace BLL.Interfaces;

public interface IAiQuestionAnsweringService
{
    Task<AiQuestionAnswerDto> AnswerDocumentQuestionAsync(
        DocumentDto document,
        string question,
        CancellationToken cancellationToken = default);
}
