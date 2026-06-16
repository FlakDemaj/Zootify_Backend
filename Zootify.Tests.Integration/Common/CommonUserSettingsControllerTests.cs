using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

/// <summary>
/// Tests for Common User Settings Controller.
/// </summary>
public class CommonUserSettingsControllerTests : IntegrationTestBase
{
    public CommonUserSettingsControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region GetUserSettings

    /// <summary>
    /// Tests Get User Settings – Existing Settings – Returns Current Values.
    /// </summary>
    [Fact]
    public async Task GetUserSettings_ExistingSettings_ReturnsCurrentValues()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CreateUserSettingsInDbAsync(
            user.Guid,
            darkMode: true,
            sendEmail: true,
            sendPush: false,
            language: "en");
        AuthenticateAs(user);

        var response = await Client.GetAsync(Routings.CommonUserSettingsRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<GetUserSettingsResponseDto>();
        settings.Should().NotBeNull();
        settings!.DarkMode.Should().BeTrue();
        settings.SendEmail.Should().BeTrue();
        settings.SendPushNotification.Should().BeFalse();
        settings.Language.Should().Be("en");
    }

    /// <summary>
    /// Tests Get User Settings – No Existing Settings – Returns User Settings Not Found.
    /// </summary>
    [Fact]
    public async Task GetUserSettings_NoExistingSettings_ReturnsUserSettingsNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de", withSettings: false);
        AuthenticateAs(user);

        var response = await Client.GetAsync(Routings.CommonUserSettingsRouting);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.UserSettingsNotFound);
    }

    /// <summary>
    /// Tests Get User Settings – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetUserSettings_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync(Routings.CommonUserSettingsRouting);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region UpdateUserSettings

    /// <summary>
    /// Tests Update User Settings – Existing Settings – Updates Dark Mode – Returns Ok.
    /// </summary>
    [Fact]
    public async Task UpdateUserSettings_ExistingSettings_UpdatesDarkMode_ReturnsOk()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, darkMode: false, language: "de");
        AuthenticateAs(user);

        var request = new UpdateUserSettingsRequestDto
        {
            SendEmail = true, 
            SendPushNotification = true,
            DarkMode = true, 
            Language = "de"
        };
        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await GetUserSettingsFromDbAsync(user.Guid);
        settings!.DarkMode.Should().BeTrue();
    }

    /// <summary>
    /// Tests Update User Settings – Existing Settings – Updates Language – Returns Ok.
    /// </summary>
    [Fact]
    public async Task UpdateUserSettings_ExistingSettings_UpdatesLanguage_ReturnsOk()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, darkMode: false, language: "de");
        AuthenticateAs(user);

        var request = new UpdateUserSettingsRequestDto
        {
            SendEmail = true, 
            SendPushNotification = true,
            DarkMode = true, 
            Language = "en"
        };
        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await GetUserSettingsFromDbAsync(user.Guid);
        settings!.Language.Should().Be("en");
    }
    
    /// <summary>
    /// Tests Update User Settings – Existing Settings – Updates Notification – Returns Ok.
    /// </summary>
    [Fact]
    public async Task UpdateUserSettings_ExistingSettings_UpdatesNotification_ReturnsOk()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, darkMode: false, language: "de");
        AuthenticateAs(user);

        var request = new UpdateUserSettingsRequestDto
        {
            SendEmail = true, 
            SendPushNotification = false,
            DarkMode = false, 
            Language = "de"
        };
        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await GetUserSettingsFromDbAsync(user.Guid);
        settings!.SendPushNotification.Should().BeFalse();
    }

    /// <summary>
    /// Tests Update User Settings – Existing Settings – Updates Notification False – Returns Error Code.
    /// </summary>
    [Fact]
    public async Task UpdateUserSettings_ExistingSettings_UpdatesNotificationFalse_ReturnsErrorCode()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, darkMode: false, language: "de");
        AuthenticateAs(user);

        var request = new UpdateUserSettingsRequestDto
        {
            SendEmail = false, 
            SendPushNotification = false,
            DarkMode = false, 
            Language = "de"
        };
        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting, request);

        var error = await ReadErrorAsync(response);
        
        error.Should().NotBeNull();
        
        error.ErrorCode.Should().Be((int)ErrorCode.OneNotificationNeedsToBeActivated);
    }


    /// <summary>
    /// Tests Update User Settings – Existing Settings – Updates All Values – Returns Ok.
    /// </summary>
    [Fact]
    public async Task UpdateUserSettings_ExistingSettings_UpdatesAllValues_ReturnsOk()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, darkMode: false, language: "de");
        AuthenticateAs(user);

        var request = new UpdateUserSettingsRequestDto
        {
            SendEmail = true, 
            SendPushNotification = true,
            DarkMode = true, 
            Language = "en"
        };
        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await GetUserSettingsFromDbAsync(user.Guid);
        settings!.DarkMode.Should().BeTrue();
        settings.Language.Should().Be("en");
        settings.SendEmail.Should().BeTrue();
        settings.SendPushNotification.Should().BeTrue();
    }
    
    /// <summary>
    /// Tests Update User Settings Language – Existing Settings – Returns Ok.
    /// </summary>
    [Fact]
    public async Task UpdateUserSettingsLanguage_ExistingSettings_ReturnsOk()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, darkMode: false, language: "de");
        AuthenticateAs(user);

        var request = new UpdateUserSettingsLanguageRequestDto { Language = "en" };
        var response = await Client.PatchAsJsonAsync($"{Routings.CommonUserSettingsRouting}/language",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await GetUserSettingsFromDbAsync(user.Guid);
        settings!.Language.Should().Be("en");
    }

    #endregion

    #region UpdateUserSettings – Error Cases

    /// <summary>
    /// Tests Update User Settings – No Existing Settings – Returns User Settings Not Found.
    /// </summary>
    [Fact]
    public async Task UpdateUserSettings_NoExistingSettings_ReturnsUserSettingsNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de", withSettings: false);
        AuthenticateAs(user);

        var request = new UpdateUserSettingsRequestDto
        {
            SendEmail = true,
            SendPushNotification = true,
            DarkMode = true,
            Language = "de"
        };
        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.UserSettingsNotFound);
    }

    /// <summary>
    /// Tests Update User Settings – Same Data Sent Twice – Returns No Updated Data.
    /// </summary>
    [Fact]
    public async Task UpdateUserSettings_SameDataSentTwice_ReturnsNoUpdatedData()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, darkMode: true, language: "en");
        AuthenticateAs(user);

        var request = new UpdateUserSettingsRequestDto
        {
            SendEmail = true, 
            SendPushNotification = true,
            DarkMode = true, 
            Language = "en"
        };
        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }

    /// <summary>
    /// Tests Update User Settings – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task UpdateUserSettings_NotAuthenticated_ReturnsUnauthorized()
    {
        var request = new UpdateUserSettingsRequestDto
        {
            SendEmail = true, 
            SendPushNotification = true,
            DarkMode = true, 
            Language = "de"
        };
        var response = await Client.PatchAsJsonAsync(Routings.CommonUserSettingsRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    /// <summary>
    /// Tests Update User Settings Language – Language Is The Same – Returns Error.
    /// </summary>
    [Fact]
    public async Task UpdateUserSettingsLanguage_LanguageIsTheSame_ReturnsError()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, darkMode: false, language: "en");
        AuthenticateAs(user);

        var request = new UpdateUserSettingsLanguageRequestDto { Language = "en" };
        var response = await Client.PatchAsJsonAsync($"{Routings.CommonUserSettingsRouting}/language",
            request);
        
        var error = await ReadErrorAsync(response);

        error.Should().NotBeNull();
        
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }

    #endregion
}
