using System.ComponentModel.DataAnnotations;

namespace Auran.Clinic.Api.Contracts.Files;

public sealed class CreateFileUploadSessionApiRequest
{
    [Required]
    [StringLength(255)]
    public string? FileName { get; set; }

    [Required]
    [StringLength(200)]
    public string? ContentType { get; set; }

    [Required]
    [Range(1, 104857600, ErrorMessage = "File size must be between 1 byte and 100 MB.")]
    public long? Size { get; set; }
}
