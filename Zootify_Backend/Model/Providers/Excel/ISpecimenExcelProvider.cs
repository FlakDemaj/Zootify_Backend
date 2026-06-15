using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;

namespace Zootify_Backend.Model.Providers.Excel;

/// <summary>
/// The outcome of parsing an import workbook: the successfully parsed rows and the rows that
/// could not be read (e.g. missing required fields or invalid values).
/// </summary>
public class SpecimenImportParseResult
{
    public List<ImportSpecimenRowDto> Rows { get; init; } = [];

    public List<ImportSpecimenFailureDto> Failures { get; init; } = [];
}

/// <summary>
/// Reads and writes specimen import/export workbooks (.xlsx).
/// </summary>
public interface ISpecimenExcelProvider
{
    /// <summary>
    /// Parses the rows of an import workbook into DTOs, collecting per-row parse failures.
    /// </summary>
    /// <param name="stream">The workbook stream.</param>
    /// <returns>The parsed rows and any parse failures.</returns>
    /// <exception cref="ZootifyException">Thrown when the stream is not a readable workbook.</exception>
    SpecimenImportParseResult ReadImportRows(Stream stream);

    /// <summary>
    /// Writes the given specimen rows into an .xlsx workbook.
    /// </summary>
    /// <param name="rows">The rows to write.</param>
    /// <returns>The workbook content as a byte array.</returns>
    byte[] WriteExport(IEnumerable<ImportSpecimenRowDto> rows);
}
