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

    /// <summary>
    /// Verifies that <c>Accept-Language: en-US</c> falls back to the parent culture <c>en</c>
    /// and returns the English message.
    /// </summary>
    [Fact]
    public async Task ErrorMessage_AcceptLanguageEnUs_ReturnsEnglishMessage()
    {
        Client.DefaultRequestHeaders.Add("Accept-Language", "en-US");

        var request = new LoginUserRequestDto { Email = UnknownEmail, Password = "Test123!" };

        var response = await Client.PostAsJsonAsync($"{Routings.AuthRouting}/login", request);

        var error = await ReadErrorAsync(response);
        error.ErrorMessage.Should().Be(NoUserWithThisEmailEn);
    }

    #endregion

    #region No Accept-Language header

    /// <summary>
    /// Verifies that a missing <c>Accept-Language</c> header falls back to the default culture
    /// (German) and returns the German message.
    /// </summary>
    [Fact]
    public async Task ErrorMessage_NoAcceptLanguageHeader_ReturnsGermanMessage()
    {
        var request = new LoginUserRequestDto { Email = UnknownEmail, Password = "Test123!" };

        var response = await Client.PostAsJsonAsync($"{Routings.AuthRouting}/login", request);

        var error = await ReadErrorAsync(response);
        error.ErrorMessage.Should().Be(NoUserWithThisEmailDe);
    }

    #endregion

    #region Unsupported language

    /// <summary>
    /// Verifies that an unsupported language (e.g. <c>fr</c>) falls back to the default culture
    /// (German) and returns the German message.
    /// </summary>
    [Fact]
    public async Task ErrorMessage_UnsupportedLanguage_FallsBackToGerman()
    {
        Client.DefaultRequestHeaders.Add("Accept-Language", "fr");

        var request = new LoginUserRequestDto { Email = UnknownEmail, Password = "Test123!" };

        var response = await Client.PostAsJsonAsync($"{Routings.AuthRouting}/login", request);

        var error = await ReadErrorAsync(response);
        error.ErrorMessage.Should().Be(NoUserWithThisEmailDe);
    }

    #endregion
}
