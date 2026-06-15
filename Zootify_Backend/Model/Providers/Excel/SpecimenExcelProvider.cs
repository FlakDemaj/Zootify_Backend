using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Providers.Excel;

/// <summary>
/// Reads and writes specimen import/export workbooks using ClosedXML.
///
/// Columns are matched by their header name, normalized to ignore case, whitespace, and the
/// "*" required marker. Both the Zootify import template headers (e.g. "Species *", "Date of
/// Discovery *") and the legacy export headers (e.g. "TaxonomySpeciesLatinName") are accepted.
/// The header row is detected automatically (so banner/title rows above it are ignored), and a
/// description/hint row directly below the header (as in the template) is skipped.
/// </summary>
public class SpecimenExcelProvider : ISpecimenExcelProvider
{
    private const string ImportSheetName = "Import";

    // Canonical field keys.
    private const string FieldName = "Name";
    private const string FieldLatinName = "LatinName";
    private const string FieldSpecies = "Species";
    private const string FieldLatitude = "Latitude";
    private const string FieldLongitude = "Longitude";
    private const string FieldDate = "DateOfDiscovery";
    private const string FieldComment = "Comment";
    private const string FieldStorageBuilding = "StorageBuilding";
    private const string FieldStorageCabinet = "StorageCabinet";
    private const string FieldStorageShelf = "StorageShelf";
    private const string FieldStorageCompartment = "StorageCompartment";
    private const string FieldTags = "Tags";

    private static readonly string[] RequiredFields =
        [FieldName, FieldLatitude, FieldLongitude, FieldDate];

    /// <summary>Maps a normalized header (lowercase, alphanumeric only) to a canonical field key.</summary>
    private static readonly Dictionary<string, string> HeaderAliases = new()
    {
        ["name"] = FieldName,
        ["latinname"] = FieldLatinName,
        ["species"] = FieldSpecies,
        ["taxonomyspecieslatinname"] = FieldSpecies,
        ["latitude"] = FieldLatitude,
        ["longitude"] = FieldLongitude,
        ["dateofdiscovery"] = FieldDate,
        ["comment"] = FieldComment,
        ["storagebuilding"] = FieldStorageBuilding,
        ["storagecabinet"] = FieldStorageCabinet,
        ["storageshelf"] = FieldStorageShelf,
        ["storagecompartment"] = FieldStorageCompartment,
        ["tags"] = FieldTags,
    };

    // Export column order with the template's header labels.
    private static readonly (string Field, string Header)[] ExportColumns =
    [
        (FieldName, "Name"),
        (FieldLatinName, "Latin Name"),
        (FieldSpecies, "Species"),
        (FieldLatitude, "Latitude"),
        (FieldLongitude, "Longitude"),
        (FieldDate, "Date of Discovery"),
        (FieldComment, "Comment"),
        (FieldStorageBuilding, "Storage Building"),
        (FieldStorageCabinet, "Storage Cabinet"),
        (FieldStorageShelf, "Storage Shelf"),
        (FieldStorageCompartment, "Storage Compartment"),
        (FieldTags, "Tags"),
    ];

    /// <summary>
    /// Parses the rows of an import workbook into DTOs, collecting per-row parse failures.
    /// </summary>
    /// <param name="stream">The workbook stream.</param>
    /// <returns>The parsed rows and any parse failures.</returns>
    /// <exception cref="ZootifyException">Thrown when the stream is not a readable workbook or has no recognizable header row.</exception>
    public SpecimenImportParseResult ReadImportRows(Stream stream)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception)
        {
            throw new ZootifyException(ErrorCode.InvalidImportFile);
        }

        using (workbook)
        {
            var result = new SpecimenImportParseResult();
            var worksheet = SelectImportWorksheet(workbook);
            if (worksheet == null)
            {
                return result;
            }

            var (headerRowNumber, columns) = FindHeaderRow(worksheet)
                ?? throw new ZootifyException(ErrorCode.InvalidImportFile);

            var dataRows = worksheet.RowsUsed()
                .Where(r => r.RowNumber() > headerRowNumber)
                .ToList();

            // The import template places a description/hint row directly below the header; detect it
            // by a non-numeric latitude and skip it (real data rows have a numeric latitude).
            if (dataRows.Count > 0 && IsDescriptionRow(dataRows[0], columns))
            {
                dataRows.RemoveAt(0);
            }

            foreach (var row in dataRows)
            {
                ParseRow(row, columns, row.RowNumber(), result);
            }

            return result;
        }
    }

    /// <summary>
    /// Writes the given specimen rows into an .xlsx workbook using the template's header labels.
    /// </summary>
    /// <param name="rows">The rows to write.</param>
    /// <returns>The workbook content as a byte array.</returns>
    public byte[] WriteExport(IEnumerable<ImportSpecimenRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(ImportSheetName);

        for (var i = 0; i < ExportColumns.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = ExportColumns[i].Header;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowIndex, 1).Value = row.Name;
            worksheet.Cell(rowIndex, 2).Value = row.LatinName;
            worksheet.Cell(rowIndex, 3).Value = row.TaxonomySpeciesLatinName;
            worksheet.Cell(rowIndex, 4).Value = row.Latitude;
            worksheet.Cell(rowIndex, 5).Value = row.Longitude;
            worksheet.Cell(rowIndex, 6).Value = row.DateOfDiscovery;
            worksheet.Cell(rowIndex, 7).Value = row.Comment;
            worksheet.Cell(rowIndex, 8).Value = row.StorageBuilding;
            worksheet.Cell(rowIndex, 9).Value = row.StorageCabinet;
            worksheet.Cell(rowIndex, 10).Value = row.StorageShelf;
            worksheet.Cell(rowIndex, 11).Value = row.StorageCompartment;
            worksheet.Cell(rowIndex, 12).Value = row.Tags;
            rowIndex++;
        }

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Returns the worksheet named "Import" (case-insensitive), or the first worksheet if none matches.
    /// </summary>
    private static IXLWorksheet? SelectImportWorksheet(XLWorkbook workbook)
    {
        return workbook.Worksheets.FirstOrDefault(w =>
                   string.Equals(w.Name, ImportSheetName, StringComparison.OrdinalIgnoreCase))
               ?? workbook.Worksheets.FirstOrDefault();
    }

    /// <summary>
    /// Finds the header row: the first used row whose mapped columns include all required fields.
    /// </summary>
    private static (int RowNumber, Dictionary<string, int> Columns)? FindHeaderRow(IXLWorksheet worksheet)
    {
        foreach (var row in worksheet.RowsUsed())
        {
            var columns = MapColumns(row);

            // A header is valid when all required fields are present and it carries the species match
            // key in either a dedicated "Species" column or (as in the import template) the "Latin Name"
            // column, which doubles as the scientific species name.
            var hasSpeciesKey = columns.ContainsKey(FieldSpecies) || columns.ContainsKey(FieldLatinName);
            if (RequiredFields.All(columns.ContainsKey) && hasSpeciesKey)
            {
                return (row.RowNumber(), columns);
            }
        }

        return null;
    }

    /// <summary>
    /// Maps the canonical field keys present in the given row to their column numbers.
    /// </summary>
    private static Dictionary<string, int> MapColumns(IXLRow row)
    {
        var columns = new Dictionary<string, int>();

        foreach (var cell in row.CellsUsed())
        {
            var normalized = Normalize(cell.GetString());
            if (HeaderAliases.TryGetValue(normalized, out var field) && !columns.ContainsKey(field))
            {
                columns[field] = cell.Address.ColumnNumber;
            }
        }

        return columns;
    }

    /// <summary>
    /// Normalizes a header to lowercase alphanumeric characters only (drops spaces and the "*" marker).
    /// </summary>
    private static string Normalize(string header)
    {
        var builder = new StringBuilder(header.Length);
        foreach (var c in header)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns <c>true</c> if the row looks like a description/hint row: its latitude cell holds
    /// non-numeric text rather than a coordinate.
    /// </summary>
    private static bool IsDescriptionRow(IXLRow row, Dictionary<string, int> columns)
    {
        if (!columns.TryGetValue(FieldLatitude, out var column))
        {
            return false;
        }

        var cell = row.Cell(column);
        if (cell.DataType == XLDataType.Number)
        {
            return false;
        }

        var text = cell.GetString().Trim();
        return text.Length > 0 && !TryParseDouble(text, out _);
    }

    /// <summary>
    /// Parses a single data row, adding either a successfully parsed DTO or a failure to the result.
    /// Fully empty rows are ignored.
    /// </summary>
    private static void ParseRow(
        IXLRow row,
        Dictionary<string, int> columns,
        int rowNumber,
        SpecimenImportParseResult result)
    {
        if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString())))
        {
            return;
        }

        var name = GetString(row, columns, FieldName);
        var latinName = GetString(row, columns, FieldLatinName);

        // Use the dedicated "Species" column when present; otherwise the "Latin Name" column carries
        // the scientific species name used for matching (as in the import template).
        var species = GetString(row, columns, FieldSpecies) ?? latinName;

        if (string.IsNullOrWhiteSpace(name))
        {
            result.Failures.Add(Failure(rowNumber, "Name is required."));
            return;
        }

        if (string.IsNullOrWhiteSpace(species))
        {
            result.Failures.Add(Failure(rowNumber, "Latin Name (species) is required."));
            return;
        }

        if (!TryGetDouble(row, columns, FieldLatitude, out var latitude))
        {
            result.Failures.Add(Failure(rowNumber, "Latitude is missing or not a number."));
            return;
        }

        if (!TryGetDouble(row, columns, FieldLongitude, out var longitude))
        {
            result.Failures.Add(Failure(rowNumber, "Longitude is missing or not a number."));
            return;
        }

        if (!TryGetDateTime(row, columns, FieldDate, out var dateOfDiscovery))
        {
            result.Failures.Add(Failure(rowNumber, "Date of Discovery is missing or not a valid date."));
            return;
        }

        result.Rows.Add(new ImportSpecimenRowDto
        {
            RowNumber = rowNumber,
            Name = name!.Trim(),
            LatinName = latinName,
            Latitude = latitude,
            Longitude = longitude,
            DateOfDiscovery = dateOfDiscovery,
            TaxonomySpeciesLatinName = species!.Trim(),
            Comment = GetString(row, columns, FieldComment),
            StorageBuilding = GetString(row, columns, FieldStorageBuilding),
            StorageCabinet = GetString(row, columns, FieldStorageCabinet),
            StorageShelf = GetString(row, columns, FieldStorageShelf),
            StorageCompartment = GetString(row, columns, FieldStorageCompartment),
            Tags = GetString(row, columns, FieldTags),
        });
    }

    private static ImportSpecimenFailureDto Failure(int rowNumber, string reason) =>
        new() { RowNumber = rowNumber, Reason = reason };

    private static string? GetString(IXLRow row, Dictionary<string, int> columns, string field)
    {
        if (!columns.TryGetValue(field, out var column))
        {
            return null;
        }

        var value = row.Cell(column).GetString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static bool TryGetDouble(IXLRow row, Dictionary<string, int> columns, string field, out double value)
    {
        value = 0;
        if (!columns.TryGetValue(field, out var column))
        {
            return false;
        }

        var cell = row.Cell(column);
        if (cell.DataType == XLDataType.Number)
        {
            value = cell.GetDouble();
            return true;
        }

        var text = cell.GetString().Trim();
        return text.Length > 0 && TryParseDouble(text, out value);
    }

    private static bool TryParseDouble(string text, out double value)
    {
        // Accept both invariant ("12.34") and comma decimal separators ("12,34").
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
               || double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetDateTime(IXLRow row, Dictionary<string, int> columns, string field, out DateTime value)
    {
        value = default;
        if (!columns.TryGetValue(field, out var column))
        {
            return false;
        }

        var cell = row.Cell(column);
        if (cell.DataType == XLDataType.DateTime)
        {
            value = cell.GetDateTime();
            return true;
        }

        var text = cell.GetString().Trim();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value)
               || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out value);
    }
}
