using System.ComponentModel.DataAnnotations;
using DAL.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalProject.ViewModels;

public class ChunkConfigurationViewModel : IValidatableObject
{
    [Display(Name = "Chunk Strategy")]
    public ChunkStrategy ChunkStrategy { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chunk Size phải lớn hơn 0.")]
    [Display(Name = "Chunk Size")]
    public int ChunkSize { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Chunk Overlap phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Chunk Overlap")]
    public int ChunkOverlap { get; set; }

    [Range(1, 50, ErrorMessage = "Top-K phải nằm trong khoảng 1 đến 50.")]
    [Display(Name = "Top-K")]
    public int TopK { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string UpdatedByAdminName { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> StrategyOptions { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChunkOverlap >= ChunkSize)
        {
            yield return new ValidationResult(
                "Chunk Overlap phải nhỏ hơn Chunk Size.",
                new[] { nameof(ChunkOverlap) });
        }
    }
}
