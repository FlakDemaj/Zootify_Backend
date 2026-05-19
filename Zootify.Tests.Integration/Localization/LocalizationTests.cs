using Zootify_Backend.DTOs.Auth.Request;

namespace Zootify.Tests.Integration.Localization;

public class LocalizationTests : IntegrationTestBase
{
    private const string UnknownEmail = "nobody@test.com";

    private const string NoUserWithThisEmailDe = "Ein Benutzer mit dieser E-Mail-Adresse existiert nicht.";
    private const string NoUserWithThisEmailEn = "A user with this email address does not exist.";

    public LocalizationTests(IntegrationTestFixture fixture) : base(fixture) { }

    #region Accept-Language: de

    /// <summary>
    /// Verifies that an explicit <c>Accept-Language: de</c> header causes the error message
    /// to be returned in German.
    /// </summary>
    [Fact]
    public async Task ErrorMessage_AcceptLanguageDe_ReturnsGermanMessage()
    {
        Client.DefaultRequestHeaders.Add("Accept-Language", "de");

        var request = new LoginUserRequestDto { Email = UnknownEmail, Password = "Test123!" };

        var response = await Client.PostAsJsonAsync($"{Routings.AuthRouting}/login", request);

        var error = await ReadErrorAsync(response);
        error.ErrorMessage.Should().Be(NoUserWithThisEmailDe);
    }

    #endregion

    #region Accept-Language: en

    /// <summary>
    /// Verifies that <c>Accept-Language: en</c> causes the error message to be returned in English.
    /// </summary>
    [Fact]
    public async Task ErrorMessage_AcceptLanguageEn_ReturnsEnglishMessage()
    {
        Client.DefaultRequestHeaders.Add("Accept-Language", "en");

        var request = new LoginUserRequestDto { Email = UnknownEmail, Password = "Test123!" };

        var response = await Client.PostAsJsonAsync($"{Routings.AuthRouting}/login", request);

        var error = await ReadErrorAsync(response);
        error.ErrorMessage.Should().Be(NoUserWithThisEmailEn);
    }
    
    #endregion
}
