using Zootify_Backend.Configuration;

namespace Zootify_Backend.Utils.Configuration;

public class ZootifyConfiguration : BaseConfiguration
{
    #region ConnectionStrings

    public static string DatabaseDefaultConnectionString =>
        SafeGet("ConnectionStrings:DefaultConnectionString", string.Empty);

    public static string DatabaseOverrideConnectionString =>
        SafeGet("ConnectionStrings:OverrideConnectionString", string.Empty);

    #endregion

    #region AccessToken

    public static string AccessTokenIssuer =>
        SafeGet("AccessToken:Issuer", string.Empty);

    public static string AccessTokenAudience =>
        SafeGet("AccessToken:Audience", string.Empty);

    public static string AccessTokenKey =>
        SafeGet("AccessToken:Key", string.Empty);

    #endregion
}
