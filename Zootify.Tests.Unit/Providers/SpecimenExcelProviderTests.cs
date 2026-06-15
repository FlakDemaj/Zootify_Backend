using ClosedXML.Excel;
using FluentAssertions;
using Xunit;
using Zootify_Backend;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Providers.Excel;

namespace Zootify.Tests.Unit.Providers;

public class SpecimenExcelProviderTests
{
    private readonly SpecimenExcelProvider _provider = new();

    [Fact]
    public void WriteThenRead_RoundTripsAllFields()
    {
        var original = new ImportSpecimenRowDto
        {
            RowNumber = 0,
            Name = "Schwalbenschwanz",
            LatinName = "Papilio machaon",
            Latitude = 50.8743,
            Longitude = 8.0211,
            DateOfDiscovery = new DateTime(2024, 5, 1),
            TaxonomySpeciesLatinName = "Papilio machaon",
            Comment = "Found near the river",
            StorageBuilding = "Building A",
            StorageCabinet = "Cabinet 3",
            StorageShelf = "Shelf 2",
            StorageCompartment = "Compartment 1",
            Tags = "Big, Blue",
        };

        var bytes = _provider.WriteExport([original]);

        using var stream = new MemoryStream(bytes);
        var result = _provider.ReadImportRows(stream);

        result.Failures.Should().BeEmpty();
        result.Rows.Should().ContainSingle();

        var row = result.Rows.Single();
        row.Name.Should().Be(original.Name);
        row.LatinName.Should().Be(original.LatinName);
        row.Latitude.Should().Be(original.Latitude);
        row.Longitude.Should().Be(original.Longitude);
        row.DateOfDiscovery.Should().Be(original.DateOfDiscovery);
        row.TaxonomySpeciesLatinName.Should().Be(original.TaxonomySpeciesLatinName);
        row.Comment.Should().Be(original.Comment);
        row.StorageBuilding.Should().Be(original.StorageBuilding);
        row.StorageCabinet.Should().Be(original.StorageCabinet);
        row.StorageShelf.Should().Be(original.StorageShelf);
        row.StorageCompartment.Should().Be(original.StorageCompartment);
        row.Tags.Should().Be(original.Tags);
        row.RowNumber.Should().Be(2); // first data row after the header
    }

    [Fact]
    public void ReadImportRows_TemplateLayout_FindsHeaderAndSkipsBannerAndDescription()
    {
        var bytes = BuildWorkbook(worksheet =>
        {
            // Rows 1-2: banner / subtitle.
            worksheet.Cell(1, 1).Value = "  ZOOTIFY — Specimen Import Template";
            worksheet.Cell(2, 1).Value = "Fill in one specimen per row · Required fields are marked with *";

            // Row 3: header row exactly as in the real import template (separate "Latin Name" and
            // "Species" columns; "*" marks required fields).
            worksheet.Cell(3, 1).Value = "Name *";
            worksheet.Cell(3, 2).Value = "Latin Name ";
            worksheet.Cell(3, 3).Value = "Species *";
            worksheet.Cell(3, 4).Value = "Latitude *";
            worksheet.Cell(3, 5).Value = "Longitude *";
            worksheet.Cell(3, 6).Value = "Date of Discovery *";
            worksheet.Cell(3, 7).Value = "Comment";
            worksheet.Cell(3, 8).Value = "Storage Building";
            worksheet.Cell(3, 9).Value = "Storage Cabinet";
            worksheet.Cell(3, 10).Value = "Tags";

            // Row 4: description / hint row (non-numeric latitude).
            worksheet.Cell(4, 1).Value = "Common name of the specimen";
            worksheet.Cell(4, 2).Value = "Common Latin name of the specimen";
            worksheet.Cell(4, 3).Value = "Scientific species name (exact match)";
            worksheet.Cell(4, 4).Value = "Decimal degrees  e.g. 51.5074";
            worksheet.Cell(4, 6).Value = "Format: YYYY-MM-DD";

            // Row 5: actual data, matching the template's example row.
            worksheet.Cell(5, 1).Value = "Golden Eagle";
            worksheet.Cell(5, 2).Value = "Aquila chrysaetos";
            worksheet.Cell(5, 3).Value = "Aquila";
            worksheet.Cell(5, 4).Value = 51.5074;
            worksheet.Cell(5, 5).Value = -0.1278;
            worksheet.Cell(5, 6).Value = new DateTime(2024, 6, 15);
            worksheet.Cell(5, 7).Value = "Found near river bank";
            worksheet.Cell(5, 8).Value = "B3";
            worksheet.Cell(5, 9).Value = "C-12";
            worksheet.Cell(5, 10).Value = "Raptor, Endangered";
        });

        using var stream = new MemoryStream(bytes);
        var result = _provider.ReadImportRows(stream);

        result.Failures.Should().BeEmpty();
        result.Rows.Should().ContainSingle();

        var row = result.Rows.Single();
        row.RowNumber.Should().Be(5);
        row.Name.Should().Be("Golden Eagle");
        row.LatinName.Should().Be("Aquila chrysaetos");
        row.TaxonomySpeciesLatinName.Should().Be("Aquila"); // taken from the dedicated Species column
        row.Latitude.Should().Be(51.5074);
        row.Longitude.Should().Be(-0.1278);
        row.DateOfDiscovery.Should().Be(new DateTime(2024, 6, 15));
        row.Comment.Should().Be("Found near river bank");
        row.StorageBuilding.Should().Be("B3");
        row.StorageCabinet.Should().Be("C-12");
        row.Tags.Should().Be("Raptor, Endangered");
    }

    [Fact]
    public void ReadImportRows_NoSpeciesColumn_UsesLatinNameAsSpeciesKey()
    {
        var bytes = BuildWorkbook(worksheet =>
        {
            // A simplified sheet with no dedicated "Species" column: the "Latin Name" column then
            // doubles as the scientific species name used for matching.
            worksheet.Cell(1, 1).Value = "Name";
            worksheet.Cell(1, 2).Value = "Latin Name";
            worksheet.Cell(1, 3).Value = "Latitude";
            worksheet.Cell(1, 4).Value = "Longitude";
            worksheet.Cell(1, 5).Value = "Date of Discovery";

            worksheet.Cell(2, 1).Value = "Golden Eagle";
            worksheet.Cell(2, 2).Value = "Aquila chrysaetos";
            worksheet.Cell(2, 3).Value = 51.5074;
            worksheet.Cell(2, 4).Value = -0.1278;
            worksheet.Cell(2, 5).Value = new DateTime(2024, 6, 15);
        });

        using var stream = new MemoryStream(bytes);
        var result = _provider.ReadImportRows(stream);

        result.Failures.Should().BeEmpty();
        var row = result.Rows.Should().ContainSingle().Subject;
        row.LatinName.Should().Be("Aquila chrysaetos");
        row.TaxonomySpeciesLatinName.Should().Be("Aquila chrysaetos");
    }

    [Fact]
    public void ReadImportRows_MissingRequiredFields_AreReportedAsFailures()
    {
        var bytes = BuildWorkbook(worksheet =>
        {
            // Header
            worksheet.Cell(1, 1).Value = "Name";
            worksheet.Cell(1, 2).Value = "Latitude";
            worksheet.Cell(1, 3).Value = "Longitude";
            worksheet.Cell(1, 4).Value = "DateOfDiscovery";
            worksheet.Cell(1, 5).Value = "TaxonomySpeciesLatinName";

            // Row 2: missing Name
            worksheet.Cell(2, 2).Value = 50.0;
            worksheet.Cell(2, 3).Value = 8.0;
            worksheet.Cell(2, 4).Value = new DateTime(2024, 1, 1);
            worksheet.Cell(2, 5).Value = "Papilio machaon";

            // Row 3: valid
            worksheet.Cell(3, 1).Value = "Valid";
            worksheet.Cell(3, 2).Value = 50.0;
            worksheet.Cell(3, 3).Value = 8.0;
            worksheet.Cell(3, 4).Value = new DateTime(2024, 1, 1);
            worksheet.Cell(3, 5).Value = "Papilio machaon";
        });

        using var stream = new MemoryStream(bytes);
        var result = _provider.ReadImportRows(stream);

        result.Rows.Should().ContainSingle(r => r.Name == "Valid");
        result.Failures.Should().ContainSingle(f => f.RowNumber == 2);
    }

    [Fact]
    public void ReadImportRows_InvalidFile_Throws()
    {
        using var stream = new MemoryStream("this is not an excel file"u8.ToArray());

        var act = () => _provider.ReadImportRows(stream);

        act.Should().Throw<ZootifyException>()
            .Which.ErrorCode.Should().Be(ErrorCode.InvalidImportFile);
    }

    private static byte[] BuildWorkbook(Action<IXLWorksheet> configure)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Specimens");
        configure(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
