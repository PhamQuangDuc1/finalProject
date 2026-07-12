namespace BLL.DTOs;

public class UpdateDocumentDto
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ChapterId { get; set; }
}
