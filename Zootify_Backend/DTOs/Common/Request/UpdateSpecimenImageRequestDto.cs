namespace Zootify_Backend.DTOs.Common.Request;

public class UpdateSpecimenImageRequestDto
{
    public string? ImageBase64 { get; set; }
    
    public List<string> Tags { get; set; }
}