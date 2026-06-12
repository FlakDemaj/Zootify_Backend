namespace Zootify.Tests.Integration.Utils;

internal static class Routings
{
    #region System

    internal const string SystemAuthRouting = "/rest/auth";

    #endregion

    #region Admin

    internal const string AdminUserRouting              = "/rest/AdminModeratorUser";
    internal const string AdminLoanRouting              = "/rest/AdminLoan";
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

    #endregion

}
