using Zootify_Backend.DTOs.Common.Request;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

public class CommonUserSettingsControllerTests : IntegrationTestBase
{
    public CommonUserSettingsControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region UpdateUserSettings – Happy Path

    [Fact]
    public async Task UpdateUserSettings_NoExistingSettings_CreatesSettingsAndReturnsOk()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var request = new UpdateUserSettingsRequestDto { DarkMode = true, Language = "en" };

        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await GetUserSettingsFromDbAsync(user.Guid);
        settings.Should().NotBeNull();
        settings!.DarkMode.Should().BeTrue();
        settings.Language.Should().Be("en");
    }

    [Fact]
    public async Task UpdateUserSettings_ExistingSettings_UpdatesDarkMode_ReturnsOk()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting,
            new UpdateUserSettingsRequestDto { DarkMode = false, Language = "de" });

        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting,
            new UpdateUserSettingsRequestDto { DarkMode = true, Language = "de" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await GetUserSettingsFromDbAsync(user.Guid);
        settings!.DarkMode.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserSettings_ExistingSettings_UpdatesLanguage_ReturnsOk()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting,
            new UpdateUserSettingsRequestDto { DarkMode = false, Language = "de" });

        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting,
            new UpdateUserSettingsRequestDto { DarkMode = false, Language = "en" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await GetUserSettingsFromDbAsync(user.Guid);
        settings!.Language.Should().Be("en");
    }

    [Fact]
    public async Task UpdateUserSettings_ExistingSettings_UpdatesBothValues_ReturnsOk()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting,
            new UpdateUserSettingsRequestDto { DarkMode = false, Language = "de" });

        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting,
            new UpdateUserSettingsRequestDto { DarkMode = true, Language = "en" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await GetUserSettingsFromDbAsync(user.Guid);
        settings!.DarkMode.Should().BeTrue();
        settings.Language.Should().Be("en");
    }

    #endregion

    #region UpdateUserSettings – No Changes

    [Fact]
    public async Task UpdateUserSettings_SameDataSentTwice_ReturnsNoUpdatedData()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var request = new UpdateUserSettingsRequestDto { DarkMode = true, Language = "en" };

        await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting, request);
        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }

    #endregion

    #region UpdateUserSettings – Error Cases

    [Fact]
    public async Task UpdateUserSettings_NotAuthenticated_ReturnsUnauthorized()
    {
        var request = new UpdateUserSettingsRequestDto { DarkMode = true, Language = "en" };

        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
