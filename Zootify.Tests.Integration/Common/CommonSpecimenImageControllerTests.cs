using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

public class CommonSpecimenImageControllerTests : IntegrationTestBase
{
    public CommonSpecimenImageControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetSpecimenImage_WithAnonymUser_ReturnImage()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var image = await SpecimenRepo.CreateImageForSpecimenAsync(
            species.Guid, location.Guid, imageBase64: "Image", createdByUserGuid: user.Guid);

        var response = await Client.GetAsync($"{Routings.CommonSpecimenImageRouting}/{image.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();

        var content = await response.Content.ReadFromJsonAsync<GetImageBase64ResponseDto>();

        content.Should().NotBeNull();
        content!.Base64.Should().Be("Image");
    }

    [Fact]
    public async Task GetSpecimenImage_WithAuthenticatedUser_ReturnImage()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var image = await SpecimenRepo.CreateImageForSpecimenAsync(
            species.Guid, location.Guid, imageBase64: "Image", createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonSpecimenImageRouting}/{image.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();

        var content = await response.Content.ReadFromJsonAsync<GetImageBase64ResponseDto>();

        content.Should().NotBeNull();
        content!.Base64.Should().Be("Image");
    }

    [Fact]
    public async Task GetSpecimenImage_WithInvalidGuid_ReturnsNotFound()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        await SpecimenRepo.CreateImageForSpecimenAsync(
            species.Guid, location.Guid, imageBase64: "Image", createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonSpecimenImageRouting}/{Guid.CreateVersion7()}");

        var error = await ReadErrorAsync(response);

        error.Should().NotBeNull();
        error.ErrorCode.Should().Be((int)ErrorCode.ImageNotFound);
    }
}
