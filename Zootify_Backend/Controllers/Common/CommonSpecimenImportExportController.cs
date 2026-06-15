using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

/// <summary>
/// Exposes Excel import and export endpoints for specimens, accessible to all authenticated roles.
/// </summary>
[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class CommonSpecimenImportExportController : BaseRestController
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly ICommonSpecimenImportExportService _importExportService;

    /// <summary>
    /// Initializes a new <see cref="CommonSpecimenImportExportController"/> with its required service.
    /// </summary>
    /// <param name="importExportService">The service handling specimen import and export.</param>
    public CommonSpecimenImportExportController(ICommonSpecimenImportExportService importExportService)
    {
        _importExportService = importExportService;
    }

    /// <summary>
    /// Imports specimens from an uploaded Excel workbook. Failed rows are skipped and reported.
    /// </summary>
    /// <param name="file">The uploaded .xlsx workbook.</param>
    /// <returns>The import result with the imported count and the failed rows.</returns>
    /// <exception cref="ZootifyException">Thrown when no readable file is provided.</exception>
    [HttpPost("import")]
    public async Task<ActionResult<ImportSpecimenResultDto>> ImportAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ZootifyException(ErrorCode.InvalidImportFile);
        }

        await using var stream = file.OpenReadStream();
        return Ok(await _importExportService.ImportAsync(Authentication, stream));
    }

    /// <summary>
    /// Exports the specimens of the given collection (respecting visibility rules) as an Excel workbook.
    /// </summary>
    /// <param name="collectionGuid">The GUID of the collection whose specimens to export.</param>
    /// <returns>An .xlsx file containing the collection's specimens.</returns>
    [HttpGet("export")]
    public async Task<IActionResult> ExportAsync([FromQuery] Guid collectionGuid)
    {
        var content = await _importExportService.ExportCollectionAsync(Authentication, collectionGuid);
        return File(content, ExcelContentType, "specimens.xlsx");
    }
}
