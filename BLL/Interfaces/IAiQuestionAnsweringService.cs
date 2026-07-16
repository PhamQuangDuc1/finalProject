using BLL.DTOs;

namespace BLL.Interfaces;

public interface IAiQuestionAnsweringService
{
    Task<AiQuestionAnswerDto> AnswerDocumentQuestionAsync(
        DocumentDto document,
        string question,
        CancellationToken cancellationToken = default);

    Task<AiQuestionAnswerDto> AnswerDocumentsQuestionAsync(
        IReadOnlyList<DocumentDto> documents,
        string question,
        CancellationToken cancellationToken = default);
}
