using FluentAssertions;
using Xunit;
using Zootify_Backend.DTOs.Base;

namespace Zootify.Tests.Unit.Dtos;

/// <summary>
/// Tests for Base Sort And Paging Dto.
/// </summary>
public class BaseSortAndPagingDtoTests
{
    /// <summary>
    /// Tests Page Size – Above Max – Is Clamped To Max.
    /// </summary>
    [Fact]
    public void PageSize_AboveMax_IsClampedToMax()
    {
        var dto = new BaseSortAndPagingDto { PageSize = 1000 };

        dto.PageSize.Should().Be(BaseSortAndPagingDto.MaxPageSize);
    }

    /// <summary>
    /// Tests Page Size – Below One – Is Clamped To One.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void PageSize_BelowOne_IsClampedToOne(int value)
    {
        var dto = new BaseSortAndPagingDto { PageSize = value };

        dto.PageSize.Should().Be(1);
    }

    /// <summary>
    /// Tests Page Size – Within Range – Is Kept.
    /// </summary>
    [Fact]
    public void PageSize_WithinRange_IsKept()
    {
        var dto = new BaseSortAndPagingDto { PageSize = 25 };

        dto.PageSize.Should().Be(25);
    }

    /// <summary>
    /// Tests Page – Below One – Is Clamped To One.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Page_BelowOne_IsClampedToOne(int value)
    {
        var dto = new BaseSortAndPagingDto { Page = value };

        dto.Page.Should().Be(1);
    }
}
