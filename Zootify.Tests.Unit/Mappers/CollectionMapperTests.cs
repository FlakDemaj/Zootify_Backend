using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.Mappers;

namespace Zootify.Tests.Unit.Mappers;

/// <summary>
/// Tests for Collection Mapper.
/// </summary>
public class CollectionMapperTests
{
    private readonly IMapper _mapper;

    public CollectionMapperTests()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<CollectionMapper>(),
            NullLoggerFactory.Instance);

        configuration.AssertConfigurationIsValid();
        _mapper = configuration.CreateMapper();
    }

    /// <summary>
    /// Tests Map – Copies Description To Response.
    /// </summary>
    [Fact]
    public void Map_CopiesDescriptionToResponse()
    {
        var collection = new CollectionEntity
        {
            Name = "Beetles",
            Description = "A collection of beetles",
        };

        var dto = _mapper.Map<GetCollectionResponseDto>(collection);

        dto.Description.Should().Be("A collection of beetles");
    }

    /// <summary>
    /// Tests Map – Null Description – Maps To Null.
    /// </summary>
    [Fact]
    public void Map_NullDescription_MapsToNull()
    {
        var collection = new CollectionEntity { Name = "Beetles" };

        var dto = _mapper.Map<GetCollectionResponseDto>(collection);

        dto.Description.Should().BeNull();
    }

    /// <summary>
    /// Tests Map – No Creator – Created By Is Null.
    /// </summary>
    [Fact]
    public void Map_NoCreator_CreatedByIsNull()
    {
        var collection = new CollectionEntity { Name = "Beetles" };

        var dto = _mapper.Map<GetCollectionResponseDto>(collection);

        dto.CreatedBy.Should().BeNull();
    }
}
