namespace Zootify.Tests.Integration.Utils;

/// <summary>
/// Routings — test support type.
/// </summary>
internal static class Routings
{
    #region System

    internal const string SystemAuthRouting = "/rest/auth";

    #endregion

    #region Admin

    internal const string AdminUserRouting              = "/rest/AdminModeratorUser";
    internal const string AdminTaxonomyRouting          = "/rest/AdminTaxonomy";
    internal const string AdminTaxonomyForApprovalRouting = "/rest/AdminTaxonomy/for-approval";

    #endregion

    #region Common

    internal const string CommonUserRouting = "/rest/CommonUser";
    internal const string CommonSpecimenRouting = "/rest/CommonSpecimen";
    internal const string CommonSpecimenImageRouting = "/rest/CommonSpecimenImage";
    internal const string CommonCollectionRouting = "/rest/CommonCollection";
    internal const string CommonTagRouting = "/rest/CommonTag";
    internal const string CommonTaxonomyRouting = "/rest/CommonTaxonomy";
    internal const string CommonTaxonomyTreeRouting = "/rest/CommonTaxonomy/tree";
    internal const string CommonUserSettingsRouting = "/rest/CommonUserSettings";
    internal const string CommonSpecimenImportExportRouting = "/rest/CommonSpecimenImportExport";
    internal const string CommonLoanRequestRouting = "/rest/CommonLoanRequest";
    internal const string CommonLoanRequestPendingRouting = "/rest/CommonLoanRequest/pending";
    internal const string CommonLoanRouting = "/rest/CommonLoan";
    internal const string CommonLoanActiveRouting = "/rest/CommonLoan/active";
    internal const string CommonLoanBorrowedRouting = "/rest/CommonLoan/borrowed";

    #endregion

    #region Home

    internal const string HomeAnimalsRouting = "/rest/Home/animals";

    #endregion

}
