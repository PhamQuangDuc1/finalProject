namespace BLL.DTOs;

public class DocumentUploadOptionsDto
{
    public IReadOnlyList<SubjectOptionDto> Subjects { get; set; } = Array.Empty<SubjectOptionDto>();

    public IReadOnlyList<DocumentChapterOptionDto> Chapters { get; set; } = Array.Empty<DocumentChapterOptionDto>();
}
