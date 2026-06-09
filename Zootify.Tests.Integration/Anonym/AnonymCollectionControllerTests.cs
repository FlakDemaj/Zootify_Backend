using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Anonym;

public class AnonymCollectionControllerTests : IntegrationTestBase
{
    public AnonymCollectionControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetPublicCollections_AllowsUnauthenticatedAccess()
    {
        await CollectionRepo.CreateCollectionAsync(name: "Public", isPrivate: false);

        var response = await Client.GetAsync(Routings.AnonymCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetCollectionResponseDto>>();
        result.Should().HaveCount(1);
    }
    
    [Fact]
    public async Task GetPublicCollections_Unauthenticated_ContainsNoStaticCollections()
    {
        var response = await Client.GetAsync(Routings.AnonymCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetCollectionResponseDto>>();
        result.Should().BeEmpty();
    }
}