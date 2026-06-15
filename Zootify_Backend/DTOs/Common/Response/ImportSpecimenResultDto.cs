namespace Zootify_Backend.DTOs.Common.Response;

public class ImportSpecimenResultDto
{
    public int ImportedCount { get; init; }

    public int FailedCount { get; init; }

    public List<ImportSpecimenFailureDto> Failures { get; init; } = [];
}

public class ImportSpecimenFailureDto
{
    public int RowNumber { get; init; }

    public required string Reason { get; init; }
}
