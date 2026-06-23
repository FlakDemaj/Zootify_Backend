using AutoMapper;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

/// <summary>
/// Handles the public homepage animal overview, applying visibility rules, paging, sorting, and city filtering.
/// </summary>
public class CommonHomeService : ICommonHomeService
{
    private readonly ISpecimenRepository _specimenRepository;

    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="CommonHomeService"/> with its required dependencies.
    /// </summary>
    /// <param name="specimenRepository">Repository for specimen queries.</param>
    /// <param name="mapper">AutoMapper instance for object mapping.</param>
    public CommonHomeService(ISpecimenRepository specimenRepository, IMapper mapper)
    {
        _specimenRepository = specimenRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns a paged public overview of active specimens, newest first, optionally filtered by city.
    /// Anonymous users see only active public specimens, regular users additionally see their own active
    /// specimens, and moderators and admins see all active specimens.
    /// </summary>
    /// <param name="authentication">The optional authentication context; anonymous users see only active public specimens, regular users additionally see their own active specimens, and moderators and admins see all active specimens.</param>
    /// <param name="filterByCity">Optional case-insensitive city name to filter by.</param>
    /// <param name="paging">Paging parameters (page and page size).</param>
    /// <returns>A paged response with the total count and the page of overview items.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<GetPublicSpecimenOverviewResponseDto> GetPublicSpecimenOverviewAsync(
        IAuthentication? authentication,
        string? filterByCity,
        BaseSortAndPagingDto paging)
    {
        var (items, total) = await _specimenRepository.GetPublicOverviewPagedAsync(
            authentication,
            filterByCity,
            paging);

        return new GetPublicSpecimenOverviewResponseDto
        {
            TotalCount = total,
            Specimens = _mapper.Map<List<PublicSpecimenOverviewDto>>(items),
        };
    }
}
