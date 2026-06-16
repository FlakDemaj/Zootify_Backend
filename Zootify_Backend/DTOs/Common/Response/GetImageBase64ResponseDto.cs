namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// Response carrying an image encoded as a base64 string.
/// </summary>
public class GetImageBase64ResponseDto
{
    /// <summary>The base64-encoded image content.</summary>
    public required string Base64 { get; set; }
}
