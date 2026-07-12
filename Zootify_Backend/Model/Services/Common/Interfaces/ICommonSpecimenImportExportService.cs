using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

/// <summary>
/// Defines import and export operations for specimens via Excel workbooks.
/// </summary>
public interface ICommonSpecimenImportExportService
{
    /// <summary>
    /// Imports specimens from an Excel workbook. Each row's species is resolved by a fuzzy match on the
    /// latin name; rows that cannot be parsed, matched, or created are skipped and reported. Created
    /// specimens are owned by the authenticated user.
    /// </summary>
    /// <param name="authentication">The authentication context of the importing user.</param>
    /// <param name="excelStream">The uploaded workbook stream.</param>
    /// <returns>The number of imported specimens and the list of failed rows with reasons.</returns>
    /// <exception cref="ZootifyException">Thrown when the stream is not a readable workbook.</exception>
    Task<ImportSpecimenResultDto> ImportAsync(IAuthentication authentication, Stream excelStream);

    /// <summary>
    /// Exports the specimens of the given collection (respecting the caller's visibility rules) to an
    /// Excel workbook. Only active specimens are exported.
    /// </summary>
    /// <param name="authentication">The authentication context of the exporting user.</param>
    /// <param name="collectionGuid">The GUID of the collection whose specimens to export.</param>
    /// <returns>The workbook content as a byte array.</returns>
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    Task<byte[]> ExportCollectionAsync(IAuthentication authentication, Guid collectionGuid);
}
