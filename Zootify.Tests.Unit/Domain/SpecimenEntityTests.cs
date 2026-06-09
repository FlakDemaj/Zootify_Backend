using Zootify_Backend;

namespace Zootify.Tests.Unit.Domain;

public class SpecimenEntityTests
{
    private static SpecimenEntity CreateSpecimen(
        string name = "Lion",
        string latinName = "Panthera leo",
        string? comment = null,
        bool isPublic = true,
        SpecimenStatus status = SpecimenStatus.Active,
        DateTime? dateOfDiscovery = null)
    {
        return new SpecimenEntity
        {
            Name = name,
            LatinName = latinName,
            Comment = comment,
            IsPublic = isPublic,
            Status = status,
            DateOfDiscovery = dateOfDiscovery ?? new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SpecimensTags = new List<RelationSpecimensTagEntity>()
        };
    }

    #region CheckChanges

    [Fact]
    public void CheckChanges_IdenticalData_ReturnsFalse()
    {
        var specimen = CreateSpecimen();
        var same = CreateSpecimen();

        specimen.CheckChanges(same).Should().BeFalse();
    }

    [Fact]
    public void CheckChanges_DifferentName_ReturnsTrue()
    {
        var specimen = CreateSpecimen(name: "Lion");
        var updated = CreateSpecimen(name: "Tiger");

        specimen.CheckChanges(updated).Should().BeTrue();
    }

    [Fact]
    public void CheckChanges_DifferentLatinName_ReturnsTrue()
    {
        var specimen = CreateSpecimen(latinName: "Panthera leo");
        var updated = CreateSpecimen(latinName: "Panthera tigris");

        specimen.CheckChanges(updated).Should().BeTrue();
    }

    [Fact]
    public void CheckChanges_DifferentDateOfDiscovery_ReturnsTrue()
    {
        var specimen = CreateSpecimen(dateOfDiscovery: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var updated = CreateSpecimen(dateOfDiscovery: new DateTime(2021, 6, 15, 0, 0, 0, DateTimeKind.Utc));

        specimen.CheckChanges(updated).Should().BeTrue();
    }

    [Fact]
    public void CheckChanges_DifferentComment_ReturnsTrue()
    {
        var specimen = CreateSpecimen(comment: null);
        var updated = CreateSpecimen(comment: "new comment");

        specimen.CheckChanges(updated).Should().BeTrue();
    }

    [Fact]
    public void CheckChanges_DifferentIsPublic_ReturnsTrue()
    {
        var specimen = CreateSpecimen(isPublic: true);
        var updated = CreateSpecimen(isPublic: false);

        specimen.CheckChanges(updated).Should().BeTrue();
    }

    [Fact]
    public void CheckChanges_DifferentStatus_ReturnsTrue()
    {
        var specimen = CreateSpecimen(status: SpecimenStatus.Active);
        var updated = CreateSpecimen(status: SpecimenStatus.Pending);

        specimen.CheckChanges(updated).Should().BeTrue();
    }

    #endregion

    #region UpdateData

    [Fact]
    public void UpdateData_CopiesAllFieldsFromNewData()
    {
        var specimen = CreateSpecimen(
            name: "Old Name",
            latinName: "Old latinname",
            comment: null,
            isPublic: true,
            status: SpecimenStatus.Active,
            dateOfDiscovery: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var newData = CreateSpecimen(
            name: "New Name",
            latinName: "New latinname",
            comment: "new comment",
            isPublic: false,
            status: SpecimenStatus.Pending,
            dateOfDiscovery: new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc));

        var requester = Guid.CreateVersion7();
        specimen.UpdateData(requester, newData);

        specimen.Name.Should().Be("New Name");
        specimen.LatinName.Should().Be("New latinname");
        specimen.Comment.Should().Be("new comment");
        specimen.IsPublic.Should().BeFalse();
        specimen.Status.Should().Be(SpecimenStatus.Pending);
        specimen.DateOfDiscovery.Should().Be(new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void UpdateData_SetsUpdatedByUserGuid()
    {
        var specimen = CreateSpecimen();
        var newData = CreateSpecimen(name: "Updated");
        var requester = Guid.CreateVersion7();

        specimen.UpdateData(requester, newData);

        specimen.UpdatedByUserGuid.Should().Be(requester);
    }

    [Fact]
    public void UpdateData_SetsUpdatedDate()
    {
        var specimen = CreateSpecimen();
        var newData = CreateSpecimen(name: "Updated");
        var before = DateTime.UtcNow;

        specimen.UpdateData(Guid.CreateVersion7(), newData);

        specimen.UpdatedDate.Should().BeOnOrAfter(before);
    }

    #endregion

    #region CreateImage

    [Fact]
    public void CreateImage_SetsImageOnSpecimen()
    {
        var specimen = CreateSpecimen();

        specimen.CreateImage("base64data", Guid.CreateVersion7());

        specimen.Image.Should().NotBeNull();
    }

    [Fact]
    public void CreateImage_SetsCorrectImageBase64()
    {
        var specimen = CreateSpecimen();

        specimen.CreateImage("myBase64String", Guid.CreateVersion7());

        specimen.Image!.ImageBase64.Should().Be("myBase64String");
    }

    [Fact]
    public void CreateImage_SetsSpecimenGuidOnImage()
    {
        var specimen = CreateSpecimen();

        specimen.CreateImage("data", Guid.CreateVersion7());

        specimen.Image!.SpecimenGuid.Should().Be(specimen.Guid);
    }

    [Fact]
    public void CreateImage_SetsUpdatedByUserGuid()
    {
        var specimen = CreateSpecimen();
        var requester = Guid.CreateVersion7();

        specimen.CreateImage("data", requester);

        specimen.UpdatedByUserGuid.Should().Be(requester);
    }

    #endregion

    #region SetLocation

    [Fact]
    public void SetLocation_SetsLatitude()
    {
        var specimen = CreateSpecimen();

        specimen.SetLocation(48.137, 11.575);

        specimen.Latitude.Should().BeApproximately(48.137, 0.001);
    }

    [Fact]
    public void SetLocation_SetsLongitude()
    {
        var specimen = CreateSpecimen();

        specimen.SetLocation(48.137, 11.575);

        specimen.Longitude.Should().BeApproximately(11.575, 0.001);
    }

    #endregion

    #region SetSpecies

    [Fact]
    public void SetSpecies_WithSpeciesNode_SetsTaxonomySpeciesGuid()
    {
        var specimen = CreateSpecimen();
        var speciesNode = new TaxonomyNodeEntity
        {
            Rank = TaxonomyRank.Species,
            Name = "Panthera leo",
            ApprovalStatus = ApprovalStatus.Approved
        };

        specimen.SetSpecies([speciesNode]);

        specimen.TaxonomySpeciesGuid.Should().Be(speciesNode.Guid);
    }

    [Fact]
    public void SetSpecies_WithMultipleNodes_PicksMostSpecificRank()
    {
        var specimen = CreateSpecimen();
        var genusNode = new TaxonomyNodeEntity { Rank = TaxonomyRank.Genus, Name = "Panthera", ApprovalStatus = ApprovalStatus.Approved };
        var speciesNode = new TaxonomyNodeEntity { Rank = TaxonomyRank.Species, Name = "leo", ApprovalStatus = ApprovalStatus.Approved };

        specimen.SetSpecies([genusNode, speciesNode]);

        specimen.TaxonomySpeciesGuid.Should().Be(speciesNode.Guid);
    }

    [Fact]
    public void SetSpecies_WithEmptyList_ThrowsSpeciesNotFound()
    {
        var specimen = CreateSpecimen();

        var act = () => specimen.SetSpecies([]);

        act.Should().Throw<ZootifyException>()
            .Which.ErrorCode.Should().Be(ErrorCode.SpeciesNotFound);
    }

    #endregion
}
