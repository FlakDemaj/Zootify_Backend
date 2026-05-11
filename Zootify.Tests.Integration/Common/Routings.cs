namespace Zootify.Tests.Integration.Common;

internal static class Routings
{
    internal const string AuthRouting = "/rest/auth/";

    #region Auth
    internal const string AuthRegister = AuthRouting + "Register";
    internal const string AuthConfirmRegistration = AuthRouting + "confirm-registration";
    internal const string AuthNewRegistrationEmail = AuthRouting + "new/registration-email";
    internal const string AuthLogin = AuthRouting + "login";
    internal const string AuthNewResetPassword = AuthRouting + "new/reset-password";
    internal const string AuthResetPassword = AuthRouting + "reset-password";
    #endregion
}
