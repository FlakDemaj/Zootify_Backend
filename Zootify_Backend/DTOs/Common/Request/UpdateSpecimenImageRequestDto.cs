namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// Request payload for updating (or clearing) a specimen's image.
/// </summary>
public class UpdateSpecimenImageRequestDto
{
    /// <summary>The new base64-encoded image, or <c>null</c> to remove the existing image.</summary>
    public string? ImageBase64 { get; init; }
}
