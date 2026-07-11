using DAL.Entities;

namespace BLL.DTOs;

public class UpdateSystemSettingDto
{
    public ChunkStrategy ChunkStrategy { get; set; }

    public int ChunkSize { get; set; }

    public int ChunkOverlap { get; set; }

    public int TopK { get; set; }
}
