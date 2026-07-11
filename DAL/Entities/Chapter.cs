namespace DAL.Entities;

public class Chapter
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    public Subject? Subject { get; set; }

    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
