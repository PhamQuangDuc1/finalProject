namespace DAL.Entities;

public class SystemSetting
{
    public int Id { get; set; }

    public ChunkStrategy ChunkStrategy { get; set; }

    public int ChunkSize { get; set; }

    public int ChunkOverlap { get; set; }

    public int TopK { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int UpdatedByAdminId { get; set; }

    public User? UpdatedByAdmin { get; set; }
}
