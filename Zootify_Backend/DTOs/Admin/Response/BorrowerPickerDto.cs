namespace Zootify_Backend.DTOs.Admin.Response;

public class BorrowerPickerDto
{
    public Guid UserGuid { get; init; }

    public required string FullName { get; init; }
}
