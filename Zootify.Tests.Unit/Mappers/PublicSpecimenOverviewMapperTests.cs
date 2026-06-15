using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.Mappers;

namespace Zootify.Tests.Unit.Mappers;

public class PublicSpecimenOverviewMapperTests
{
    private readonly IMapper _mapper;

    public PublicSpecimenOverviewMapperTests()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<PublicSpecimenOverviewMapper>(),
            NullLoggerFactory.Instance);

        configuration.AssertConfigurationIsValid();
        _mapper = configuration.CreateMapper();
    }

    /// <summary>
    /// Builds a fully populated specimen: species under the given order, a Country → State → City →
    /// HouseNumber location chain, and an image.
    /// </summary>
    private static SpecimenEntity BuildSpecimen(
        string orderName = "Lepidoptera",
        string latinName = "Papilio machaon",
        string name = "Schwalbenschwanz",
        double latitude = 50.8743,
        double longitude = 8.0211,
        bool withImage = true)
    {
        var order = new TaxonomyNodeEntity { Name = orderName, Rank = TaxonomyRank.Order };
        var species = new TaxonomyNodeEntity { Name = latinName, Rank = TaxonomyRank.Species, Parent = order };

        var country = new GeographicLocationEntity { Name = "Deutschland", Level = GeographicLevel.Country };
        var state = new GeographicLocationEntity { Name = "NRW", Level = GeographicLevel.State, Parent = country };
        var city = new GeographicLocationEntity { Name = "Siegen", Level = GeographicLevel.City, Parent = state };
        var house = new GeographicLocationEntity { Name = "Buchhauerstraße 37", Level = GeographicLevel.HouseNumber, Parent = city };

        return new SpecimenEntity
        {
            Name = name,
            LatinName = latinName,
            Latitude = latitude,
            Longitude = longitude,
            Species = species,
            GeographicLocation = house,
            Image = withImage ? new SpecimenImageEntity { ImageBase64 = "data" } : null,
        };
    }

    [Fact]
    public void Map_PopulatesAllRequiredFields()
    {
        var specimen = BuildSpecimen();

        var dto = _mapper.Map<PublicSpecimenOverviewDto>(specimen);

        dto.Name.Should().Be(specimen.Name);
        dto.LatinName.Should().Be(specimen.LatinName);
        dto.Latitude.Should().Be(specimen.Latitude);
        dto.Longitude.Should().Be(specimen.Longitude);
        dto.City.Should().Be("Siegen");
        dto.TaxonGroup.Should().Be("Lepidoptera");
        dto.ImageGuid.Should().Be(specimen.Image!.Guid);
        dto.CreatedAt.Should().Be(specimen.CreationDate);
        dto.LocationPath.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Papilio machaon", "Lepidoptera")]
    [InlineData("Vanessa atalanta", "Lepidoptera")]
    [InlineData("Carabus coriaceus", "Coleoptera")]
    public void Map_TaxonGroup_ContainsTaxonomicOrder(string latinName, string expectedOrder)
    {
        var specimen = BuildSpecimen(orderName: expectedOrder, latinName: latinName);

        var dto = _mapper.Map<PublicSpecimenOverviewDto>(specimen);

        dto.TaxonGroup.Should().Be(expectedOrder);
    }

    [Fact]
    public void Map_TaxonGroup_NoOrderAncestor_ReturnsEmpty()
    {
        var specimen = BuildSpecimen();
        // Replace the species chain with a species that has no order ancestor.
        specimen.Species = new TaxonomyNodeEntity { Name = "Lonely", Rank = TaxonomyRank.Species };

        var dto = _mapper.Map<PublicSpecimenOverviewDto>(specimen);

        dto.TaxonGroup.Should().BeEmpty();
    }

    [Fact]
    public void Map_LocationPath_ReturnsFullChainRootToLeaf()
    {
        var specimen = BuildSpecimen();

        var dto = _mapper.Map<PublicSpecimenOverviewDto>(specimen);

        dto.LocationPath.Select(p => p.Name).Should()
            .ContainInOrder("Deutschland", "NRW", "Siegen", "Buchhauerstraße 37");
        dto.LocationPath.Select(p => p.Level).Should()
            .ContainInOrder(GeographicLevel.Country, GeographicLevel.State, GeographicLevel.City, GeographicLevel.HouseNumber);
    }

    [Fact]
    public void Map_LatitudeAndLongitude_AreCopied()
    {
        var specimen = BuildSpecimen(latitude: 12.34, longitude: 56.78);

        var dto = _mapper.Map<PublicSpecimenOverviewDto>(specimen);

        dto.Latitude.Should().Be(12.34);
        dto.Longitude.Should().Be(56.78);
    }

    [Fact]
    public void Map_ImageGuid_IsReturned()
    {
        var specimen = BuildSpecimen(withImage: true);

        var dto = _mapper.Map<PublicSpecimenOverviewDto>(specimen);

        dto.ImageGuid.Should().Be(specimen.Image!.Guid);
    }

    [Fact]
    public void Map_NoImage_ImageGuidIsNull()
    {
        var specimen = BuildSpecimen(withImage: false);

        var dto = _mapper.Map<PublicSpecimenOverviewDto>(specimen);

        dto.ImageGuid.Should().BeNull();
    }

    [Fact]
    public void Map_City_ReturnsCityLevelNodeName()
    {
        var specimen = BuildSpecimen();

        var dto = _mapper.Map<PublicSpecimenOverviewDto>(specimen);

        dto.City.Should().Be("Siegen");
    }
}
