namespace Zootify_Backend.DTOs.Admin.Response;

public class BorrowerPickerDto
{
    public Guid UserGuid { get; set; }

    public required string FullName { get; set; }
}
