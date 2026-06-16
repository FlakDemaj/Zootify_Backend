using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

/// <summary>
/// Tests for Common Tag Controller.
/// </summary>
public class CommonTagControllerTests : IntegrationTestBase
{
    public CommonTagControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    /// <summary>
    /// Tests Get All Tags – Returns All Tags.
    /// </summary>
    [Fact]
    public async Task GetAllTags_ReturnsAllTags()
    {
        var tagOne = await TagRepo.CreateTagAsync(
            "A");
        var tagTwo = await TagRepo.CreateTagAsync(
            "B");
        var tagThree = await TagRepo.CreateTagAsync(
            "C");
        var tagFour = await TagRepo.CreateTagAsync(
            "D");
        var tagFive = await TagRepo.CreateTagAsync(
            "E");

        var requester = await UserRepo.CreateUserAsync(
            "test@test.de");
        
        AuthenticateAs(requester);
        
        var response = await Client.GetFromJsonAsync<List<GetAllTagsByFilterDto>>(
            $"{Routings.CommonTagRouting}");
        
        response.Should().NotBeNull();
        response!.Count.Should().Be(5);

        response[0].TagGuid.Should().Be(tagOne.Guid);
        response[0].TagName.Should().Be(tagOne.Tag);
        
        response[1].TagGuid.Should().Be(tagTwo.Guid);
        response[1].TagName.Should().Be(tagTwo.Tag);
        
        response[2].TagGuid.Should().Be(tagThree.Guid);
        response[2].TagName.Should().Be(tagThree.Tag);

        response[3].TagGuid.Should().Be(tagFour.Guid);
        response[3].TagName.Should().Be(tagFour.Tag);

        response[4].TagGuid.Should().Be(tagFive.Guid);
        response[4].TagName.Should().Be(tagFive.Tag);

    }
    
    /// <summary>
    /// Tests Get All Tags – With Filter – Returns Tags.
    /// </summary>
    [Fact]
    public async Task GetAllTags_WithFilter_ReturnsTags()
    {
        var tagOne = await TagRepo.CreateTagAsync(
            "TestA");
        await TagRepo.CreateTagAsync(
            "BTag");
        await TagRepo.CreateTagAsync(
            "CTag");
        var tagFour = await TagRepo.CreateTagAsync(
            "TestD");
        await TagRepo.CreateTagAsync(
            "SuperE");

        var requester = await UserRepo.CreateUserAsync(
            "test@test.de");
        
        AuthenticateAs(requester);

        var filterBy = "Te";
        
        var response = await Client.GetFromJsonAsync<List<GetAllTagsByFilterDto>>(
            $"{Routings.CommonTagRouting}?filter={filterBy}");
        
        response.Should().NotBeNull();
        response!.Count.Should().Be(2);
        
        response[0].TagGuid.Should().Be(tagOne.Guid);
        response[0].TagName.Should().Be(tagOne.Tag);

        response[1].TagGuid.Should().Be(tagFour.Guid);
        response[1].TagName.Should().Be(tagFour.Tag);
    }
    
    /// <summary>
    /// Tests Get All Tags – With Filter – Returns Nothing.
    /// </summary>
    [Fact]
    public async Task GetAllTags_WithFilter_ReturnsNothing()
    {
        await TagRepo.CreateTagAsync(
            "TestA");
        await TagRepo.CreateTagAsync(
            "BTag");
        await TagRepo.CreateTagAsync(
            "CTag");
        await TagRepo.CreateTagAsync(
            "TestD");
        await TagRepo.CreateTagAsync(
            "SuperE");

        var requester = await UserRepo.CreateUserAsync(
            "test@test.de");
        
        AuthenticateAs(requester);

        var filterBy = "zzz";

        var response = await Client.GetFromJsonAsync<List<GetAllTagsByFilterDto>>(
            $"{Routings.CommonTagRouting}?filter={filterBy}");

        response.Should().NotBeNull();
        response!.Count.Should().Be(0);
    }

    /// <summary>
    /// Tests Get All Tags – Fuzzy Filter – Finds Tag Despite Typo.
    /// </summary>
    [Fact]
    public async Task GetAllTags_FuzzyFilter_FindsTagDespiteTypo()
    {
        var endangered = await TagRepo.CreateTagAsync("Endangered");
        await TagRepo.CreateTagAsync("Mammal");

        var requester = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(requester);

        // "endangerd" is a typo of "Endangered" → fuzzy matching still finds it.
        var response = await Client.GetFromJsonAsync<List<GetAllTagsByFilterDto>>(
            $"{Routings.CommonTagRouting}?filter=endangerd");

        response.Should().NotBeNull();
        response!.Should().Contain(t => t.TagGuid == endangered.Guid);
        response.Should().NotContain(t => t.TagName == "Mammal");
    }

    /// <summary>
    /// Tests Get All Tags – With Filter – Matches Substring Not Only Prefix.
    /// </summary>
    [Fact]
    public async Task GetAllTags_WithFilter_MatchesSubstringNotOnlyPrefix()
    {
        await TagRepo.CreateTagAsync("BTag");
        await TagRepo.CreateTagAsync("CTag");
        await TagRepo.CreateTagAsync("SuperE");

        var requester = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(requester);

        // "ag" is contained in "BTag"/"CTag" but is not a prefix of either, proving substring matching.
        var response = await Client.GetFromJsonAsync<List<GetAllTagsByFilterDto>>(
            $"{Routings.CommonTagRouting}?filter=ag");

        response.Should().NotBeNull();
        response!.Select(t => t.TagName).Should().BeEquivalentTo("BTag", "CTag");
    }
}