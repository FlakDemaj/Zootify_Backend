using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.Mappers;

namespace Zootify.Tests.Unit.Mappers;

/// <summary>
/// Tests for Public Specimen Overview Mapper.
/// </summary>
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

        var image = withImage ? new SpecimenImageEntity { ImageBase64 = "data" } : null;

        return new SpecimenEntity
        {
            Name = name,
            LatinName = latinName,
            Latitude = latitude,
            Longitude = longitude,
            Species = species,
            GeographicLocation = house,
            // The specimen owns the foreign key, so mirror the image reference on ImageGuid.
            Image = image,
            ImageGuid = image?.Guid,
        };
    }

    /// <summary>
    /// Tests Map – Populates All Required Fields.
    /// </summary>
    [Fact]
    public void Map_PopulatesAllRequiredFields()
    {
        var specimen = BuildSpecimen();

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.Name.Should().Be(specimen.Name);
        dto.LatinName.Should().Be(specimen.LatinName);
        dto.Latitude.Should().Be(specimen.Latitude);
        dto.Longitude.Should().Be(specimen.Longitude);
        dto.LocationPath.Should().Contain(n => n.Level == GeographicLevel.City && n.Name == "Siegen");
        dto.TaxonGroup.Should().Be("Lepidoptera");
        dto.ImageGuid.Should().Be(specimen.Image!.Guid);
        dto.CreatedAt.Should().Be(specimen.CreationDate);
        dto.LocationPath.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests Map – Taxon Group – Contains Taxonomic Order.
    /// </summary>
    [Theory]
    [InlineData("Papilio machaon", "Lepidoptera")]
    [InlineData("Vanessa atalanta", "Lepidoptera")]
    [InlineData("Carabus coriaceus", "Coleoptera")]
    public void Map_TaxonGroup_ContainsTaxonomicOrder(string latinName, string expectedOrder)
    {
        var specimen = BuildSpecimen(orderName: expectedOrder, latinName: latinName);

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.TaxonGroup.Should().Be(expectedOrder);
    }

    /// <summary>
    /// Tests Map – Taxon Group – No Order Ancestor – Returns Empty.
    /// </summary>
    [Fact]
    public void Map_TaxonGroup_NoOrderAncestor_ReturnsEmpty()
    {
        var specimen = BuildSpecimen();
        // Replace the species chain with a species that has no order ancestor.
        specimen.Species = new TaxonomyNodeEntity { Name = "Lonely", Rank = TaxonomyRank.Species };

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.TaxonGroup.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Map – Location Path – Returns Full Chain Root To Leaf.
    /// </summary>
    [Fact]
    public void Map_LocationPath_ReturnsFullChainRootToLeaf()
    {
        var specimen = BuildSpecimen();

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.LocationPath.Select(p => p.Name).Should()
            .ContainInOrder("Deutschland", "NRW", "Siegen", "Buchhauerstraße 37");
        dto.LocationPath.Select(p => p.Level).Should()
            .ContainInOrder(GeographicLevel.Country, GeographicLevel.State, GeographicLevel.City, GeographicLevel.HouseNumber);
    }

    /// <summary>
    /// Tests Map – Latitude And Longitude – Are Copied.
    /// </summary>
    [Fact]
    public void Map_LatitudeAndLongitude_AreCopied()
    {
        var specimen = BuildSpecimen(latitude: 12.34, longitude: 56.78);

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.Latitude.Should().Be(12.34);
        dto.Longitude.Should().Be(56.78);
    }

    /// <summary>
    /// Tests Map – Image Guid – Is Returned.
    /// </summary>
    [Fact]
    public void Map_ImageGuid_IsReturned()
    {
        var specimen = BuildSpecimen(withImage: true);

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.ImageGuid.Should().Be(specimen.Image!.Guid);
    }

    /// <summary>
    /// Tests Map – No Image – Image Guid Is Null.
    /// </summary>
    [Fact]
    public void Map_NoImage_ImageGuidIsNull()
    {
        var specimen = BuildSpecimen(withImage: false);

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.ImageGuid.Should().BeNull();
    }

    /// <summary>
    /// Tests Map – Location Path – Contains City Level Node.
    /// </summary>
    [Fact]
    public void Map_LocationPath_ContainsCityLevelNode()
    {
        var specimen = BuildSpecimen();

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.LocationPath.Should().Contain(n => n.Level == GeographicLevel.City && n.Name == "Siegen");
    }

    /// <summary>
    /// Tests Map – No Loans – Loan Status Is Available.
    /// </summary>
    [Fact]
    public void Map_NoLoans_LoanStatusIsAvailable()
    {
        var specimen = BuildSpecimen();

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.LoanStatus.Should().Be(SpecimenLoanStatus.Available);
    }

    /// <summary>
    /// Tests Map – Open Loan – Loan Status Is On Loan.
    /// </summary>
    [Fact]
    public void Map_OpenLoan_LoanStatusIsOnLoan()
    {
        var specimen = BuildSpecimen();
        specimen.Loans.Add(new LoanEntity { ReturnDate = null });

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.LoanStatus.Should().Be(SpecimenLoanStatus.OnLoan);
    }

    /// <summary>
    /// Tests Map – Only Returned Loans – Loan Status Is Available.
    /// </summary>
    [Fact]
    public void Map_OnlyReturnedLoans_LoanStatusIsAvailable()
    {
        var specimen = BuildSpecimen();
        specimen.Loans.Add(new LoanEntity { ReturnDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.LoanStatus.Should().Be(SpecimenLoanStatus.Available);
    }

    /// <summary>
    /// Tests Map – Pending Request – Loan Status Is Requested.
    /// </summary>
    [Fact]
    public void Map_PendingRequest_LoanStatusIsRequested()
    {
        var specimen = BuildSpecimen();
        specimen.LoanRequests.Add(new LoanRequestEntity { Status = LoanRequestStatus.Pending });

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.LoanStatus.Should().Be(SpecimenLoanStatus.Requested);
    }

    /// <summary>
    /// Tests Map – Open Loan And Pending Request – Loan Status Is On Loan.
    /// </summary>
    [Fact]
    public void Map_OpenLoanAndPendingRequest_LoanStatusIsOnLoan()
    {
        var specimen = BuildSpecimen();
        specimen.Loans.Add(new LoanEntity { ReturnDate = null });
        specimen.LoanRequests.Add(new LoanRequestEntity { Status = LoanRequestStatus.Pending });

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        // An active loan takes precedence over a pending request.
        dto.LoanStatus.Should().Be(SpecimenLoanStatus.OnLoan);
    }

    /// <summary>
    /// Tests Map – Non Pending Request – Loan Status Is Available.
    /// </summary>
    [Fact]
    public void Map_NonPendingRequest_LoanStatusIsAvailable()
    {
        var specimen = BuildSpecimen();
        specimen.LoanRequests.Add(new LoanRequestEntity { Status = LoanRequestStatus.Rejected });

        var dto = _mapper.Map<GetPublicSpecimenOverviewDto>(specimen);

        dto.LoanStatus.Should().Be(SpecimenLoanStatus.Available);
    }
}
