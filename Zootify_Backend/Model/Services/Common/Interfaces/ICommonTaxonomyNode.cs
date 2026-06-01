using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

public interface ICommonTaxonomyNode
{
    Task<CreatedObjectResponseDto> CreateTaxonomyNodeAsync(
        CreateTaxonomyNodeRequestDto requestDto);

    Task<List<List<GetAllTaxonomyNodesByFilterDto>>> GetAllTaxonomyNodesAsync(
        string? startsWith,
        TaxonomyRank rank);
}