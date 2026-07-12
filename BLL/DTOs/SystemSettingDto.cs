using DAL.Entities;

namespace BLL.DTOs;

public class SystemSettingDto
{
    public int Id { get; set; }

    public ChunkStrategy ChunkStrategy { get; set; }

    public int ChunkSize { get; set; }

    public int ChunkOverlap { get; set; }

    public int TopK { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int UpdatedByAdminId { get; set; }

    public string UpdatedByAdminName { get; set; } = string.Empty;
}
