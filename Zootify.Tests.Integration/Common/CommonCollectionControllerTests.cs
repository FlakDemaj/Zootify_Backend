using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

public class CommonCollectionControllerTests : IntegrationTestBase
{
    public CommonCollectionControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region CreateCollection

    [Fact]
    public async Task CreateCollection_ValidRequest_ReturnsCreatedGuid()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var request = new CreateCollectionRequestDto
        {
            Name = "My Collection",
            Description = "A test collection",
            IsPrivate = false
        };

        var response = await Client.PostAsJsonAsync(Routings.CommonCollectionRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        result.Should().NotBeNull();

        var collection = await GetCollectionFromDbAsync(result!.CreatedObjectGuid);
        collection.Should().NotBeNull();
        collection!.Name.Should().Be("My Collection");
        collection.CreatedByUserGuid.Should().Be(user.Guid);
    }

    [Fact]
    public async Task CreateCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(Routings.CommonCollectionRouting,
            new CreateCollectionRequestDto { Name = "X", IsPrivate = false });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetPublicCollections

    [Fact]
    public async Task GetPublicCollections_ReturnsPublicCollections()
    {
        await CollectionRepo.CreateCollectionAsync(name: "Collection A", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Collection B", isPrivate: false);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetCollectionResponseDto>>();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPublicCollections_PrivateCollectionsNotReturned()
    {
        await CollectionRepo.CreateCollectionAsync(name: "Public", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Private", isPrivate: true);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetCollectionResponseDto>>();
        result.Should().HaveCount(1);
        result!.Single().Name.Should().Be("Public");
    }

    [Fact]
    public async Task GetPublicCollections_AllowsUnauthenticatedAccess()
    {
        await CollectionRepo.CreateCollectionAsync(name: "Public", isPrivate: false);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetCollectionResponseDto>>();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPublicCollections_SoftDeletedCollectionsNotReturned()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "Active", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Deleted", isPrivate: false,
            deletedByUserGuid: user.Guid);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetCollectionResponseDto>>();
        result.Should().HaveCount(1);
        result!.Single().Name.Should().Be("Active");
    }

    #endregion

    #region UpdateCollection

    [Fact]
    public async Task UpdateCollection_AllData_UpdatesCollection()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Old Name",
            description: "Old Description",
            isPrivate: false,
            createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateCollectionRequestDto
        {
            Name = "New Name",
            Description = "New Description",
            IsPrivate = true
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetCollectionFromDbAsync(collection.Guid);
        updated!.Name.Should().Be("New Name");
        updated.Description.Should().Be("New Description");
        updated.IsPrivate.Should().BeTrue();
        updated.UpdatedByUserGuid.Should().Be(user.Guid);
        updated.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateCollection_JustName_UpdatesName()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Old Name",
            isPrivate: false,
            createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateCollectionRequestDto
        {
            Name = "New Name",
            Description = collection.Description,
            IsPrivate = collection.IsPrivate
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetCollectionFromDbAsync(collection.Guid);
        updated!.Name.Should().Be("New Name");
        updated.UpdatedByUserGuid.Should().Be(user.Guid);
    }

    [Fact]
    public async Task UpdateCollection_NoChanges_ReturnsNoUpdatedData()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Name",
            isPrivate: false,
            createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateCollectionRequestDto
        {
            Name = collection.Name,
            Description = collection.Description,
            IsPrivate = collection.IsPrivate
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }

    [Fact]
    public async Task UpdateCollection_CollectionNotFound_ReturnsCollectionNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var request = new UpdateCollectionRequestDto
        {
            Name = "New Name",
            IsPrivate = false
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

    [Fact]
    public async Task UpdateCollection_AsUserOnOtherUsersCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var otherUser = await UserRepo.CreateUserAsync("other@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Owners Collection",
            isPrivate: false,
            createdByUserGuid: owner.Guid);

        AuthenticateAs(otherUser);

        var request = new UpdateCollectionRequestDto
        {
            Name = "Hijacked Name",
            IsPrivate = false
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    [Fact]
    public async Task UpdateCollection_AsAdmin_CanUpdateAnyCollection()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Old Name",
            isPrivate: false,
            createdByUserGuid: owner.Guid);

        AuthenticateAs(admin);

        var request = new UpdateCollectionRequestDto
        {
            Name = "Updated By Admin",
            IsPrivate = true
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetCollectionFromDbAsync(collection.Guid);
        updated!.Name.Should().Be("Updated By Admin");
        updated.UpdatedByUserGuid.Should().Be(admin.Guid);
    }

    [Fact]
    public async Task UpdateCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}",
            new UpdateCollectionRequestDto { Name = "X", IsPrivate = false });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region DeleteCollection

    [Fact]
    public async Task DeleteCollection_AsOwner_SoftDeletesCollection()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleted = await GetCollectionFromDbAsync(collection.Guid);
        deleted.Should().NotBeNull();
        deleted!.DeletedByUserGuid.Should().Be(user.Guid);
        deleted.DeletionDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteCollection_AsAdmin_CanSoftDeleteAnyCollection()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: owner.Guid);

        AuthenticateAs(admin);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleted = await GetCollectionFromDbAsync(collection.Guid);
        deleted.Should().NotBeNull();
        deleted!.DeletedByUserGuid.Should().Be(admin.Guid);
        deleted.DeletionDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteCollection_AsUserOnOtherUsersCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var otherUser = await UserRepo.CreateUserAsync("other@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: owner.Guid);

        AuthenticateAs(otherUser);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    [Fact]
    public async Task DeleteCollection_CollectionNotFound_ReturnsCollectionNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

    [Fact]
    public async Task DeleteCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region ActivateCollection

    [Fact]
    public async Task ActivateCollection_AsOwner_ActivatesCollection()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            createdByUserGuid: user.Guid,
            deletedByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/activate/{collection.Guid}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var activated = await GetCollectionFromDbAsync(collection.Guid);
        activated!.DeletionDate.Should().BeNull();
        activated.DeletedByUserGuid.Should().BeNull();
        activated.UpdatedByUserGuid.Should().Be(user.Guid);
    }

    [Fact]
    public async Task ActivateCollection_AsAdmin_CanActivateAnyCollection()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var collection = await CollectionRepo.CreateCollectionAsync(
            createdByUserGuid: owner.Guid,
            deletedByUserGuid: owner.Guid);

        AuthenticateAs(admin);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/activate/{collection.Guid}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var activated = await GetCollectionFromDbAsync(collection.Guid);
        activated!.DeletionDate.Should().BeNull();
        activated.DeletedByUserGuid.Should().BeNull();
        activated.UpdatedByUserGuid.Should().Be(admin.Guid);
    }

    [Fact]
    public async Task ActivateCollection_AsUserOnOtherUsersCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var otherUser = await UserRepo.CreateUserAsync("other@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            createdByUserGuid: owner.Guid,
            deletedByUserGuid: owner.Guid);

        AuthenticateAs(otherUser);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/activate/{collection.Guid}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    [Fact]
    public async Task ActivateCollection_CollectionNotFound_ReturnsCollectionNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/activate/{Guid.CreateVersion7()}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

    [Fact]
    public async Task ActivateCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/activate/{Guid.CreateVersion7()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region AddSpecimenToCollection

    [Fact]
    public async Task AddSpecimenToCollection_ValidRequest_SpecimenLinkedToCollection()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var link = await GetSpecimenCollectionFromDbAsync(collection.Guid, specimen.Guid);
        link.Should().NotBeNull();
    }

    [Fact]
    public async Task AddSpecimenToCollection_CollectionNotFound_ReturnsCollectionNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}/specimen/{specimen.Guid}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

    [Fact]
    public async Task AddSpecimenToCollection_SpecimenNotFound_ReturnsNoSpecimenWithThisGuid()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{Guid.CreateVersion7()}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenWithThisGuid);
    }

    [Fact]
    public async Task AddSpecimenToCollection_SpecimenAlreadyInCollection_ReturnsError()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.SpecimenAlreadyInCollection);
    }

    [Fact]
    public async Task AddSpecimenToCollection_AsUserOnOtherUsersCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var otherUser = await UserRepo.CreateUserAsync("other@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: owner.Guid);

        AuthenticateAs(otherUser);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    [Fact]
    public async Task AddSpecimenToCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}/specimen/{Guid.CreateVersion7()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region RemoveSpecimenFromCollection

    [Fact]
    public async Task RemoveSpecimenFromCollection_ValidRequest_SpecimenRemovedFromCollection()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        AuthenticateAs(user);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var link = await GetSpecimenCollectionFromDbAsync(collection.Guid, specimen.Guid);
        link.Should().BeNull();
    }

    [Fact]
    public async Task RemoveSpecimenFromCollection_CollectionNotFound_ReturnsCollectionNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);

        AuthenticateAs(user);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}/specimen/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

    [Fact]
    public async Task RemoveSpecimenFromCollection_AsUserOnOtherUsersCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var otherUser = await UserRepo.CreateUserAsync("other@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        AuthenticateAs(otherUser);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    [Fact]
    public async Task RemoveSpecimenFromCollection_SpecimenNotInCollection_ReturnsError()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.SpecimenNotInCollection);
    }

    [Fact]
    public async Task RemoveSpecimenFromCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}/specimen/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
