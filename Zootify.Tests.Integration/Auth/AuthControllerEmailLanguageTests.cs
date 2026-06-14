using System.Globalization;
using Zootify_Backend.DTOs.System.Request;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Auth;

/// <summary>
/// Verifies that auth emails (registration and password reset) are sent in the language the caller
/// requested via the <c>Accept-Language</c> header — these flows are not tied to a stored user
/// preference (the account is new or the caller is anonymous), so the request language is used.
///
/// The real <c>BrevoProvider</c> picks English vs German from <see cref="CultureInfo.CurrentUICulture"/>,
/// which the localization middleware sets from the <c>Accept-Language</c> header. Because the provider's
/// <c>BrevoClient.SendAsync</c> is non-virtual it cannot be mocked to capture the produced content, so
/// we capture the active UI culture at the moment the mocked <c>IBrevoProvider</c> is invoked — that
/// culture is exactly the input the real provider uses to choose the language.
/// </summary>
public class AuthControllerEmailLanguageTests : IntegrationTestBase
{
    public AuthControllerEmailLanguageTests(IntegrationTestFixture fixture) : base(fixture) { }

    #region Registration email language

    [Theory]
    [InlineData("en", "en")]
    [InlineData("de", "de")]
    public async Task Register_SendsEmailInRequestedLanguage(string acceptLanguage, string expectedLanguage)
    {
        string? emailLanguage = null;
        Fixture.BrevoMock
            .Setup(b => b.SendRegistrationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => emailLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            .ReturnsAsync(true);

        SetAcceptLanguage(acceptLanguage);

        var request = new RegisterUserRequestDto
        {
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "lang-register@example.com",
            Password = "TestPass123!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/Register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        emailLanguage.Should().Be(expectedLanguage);
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("de", "de")]
    public async Task SendNewRegistrationEmail_SendsEmailInRequestedLanguage(string acceptLanguage, string expectedLanguage)
    {
        await UserRepo.CreateUserAsync("lang-resend@test.com", status: UserStatus.Pending);

        string? emailLanguage = null;
        Fixture.BrevoMock
            .Setup(b => b.SendRegistrationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => emailLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            .ReturnsAsync(true);

        SetAcceptLanguage(acceptLanguage);

        var request = new SendNewRegistrationEmailRequestDto { Email = "lang-resend@test.com" };

        var response = await Client.PostAsJsonAsync(
            $"{Routings.SystemAuthRouting}/new-registration-email", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        emailLanguage.Should().Be(expectedLanguage);
    }

    #endregion

    #region Reset-password email language

    [Theory]
    [InlineData("en", "en")]
    [InlineData("de", "de")]
    public async Task SendNewResetPasswordEmail_SendsEmailInRequestedLanguage(string acceptLanguage, string expectedLanguage)
    {
        await UserRepo.CreateUserAsync("lang-reset-req@test.com");

        string? emailLanguage = null;
        Fixture.BrevoMock
            .Setup(b => b.SendResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => emailLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            .ReturnsAsync(true);

        SetAcceptLanguage(acceptLanguage);

        var request = new SendNewResetPasswordEmailRequestDto { Email = "lang-reset-req@test.com" };

        var response = await Client.PostAsJsonAsync(
            $"{Routings.SystemAuthRouting}/new/reset-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        emailLanguage.Should().Be(expectedLanguage);
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("de", "de")]
    public async Task ResetPassword_SendsConfirmationEmailInRequestedLanguage(string acceptLanguage, string expectedLanguage)
    {
        var user = await UserRepo.CreateUserAsync("lang-reset-confirm@test.com", withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        string? emailLanguage = null;
        Fixture.BrevoMock
            .Setup(b => b.SendConfirmationResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => emailLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            .ReturnsAsync(true);

        SetAcceptLanguage(acceptLanguage);

        var request = new ResetPasswordRequestDto
        {
            Password = "NewPass456!",
            RepeatedPassword = "NewPass456!"
        };

        var response = await Client.PostAsJsonAsync(
            $"{Routings.SystemAuthRouting}/reset-password?token={resetToken}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        emailLanguage.Should().Be(expectedLanguage);
    }

    #endregion
}
