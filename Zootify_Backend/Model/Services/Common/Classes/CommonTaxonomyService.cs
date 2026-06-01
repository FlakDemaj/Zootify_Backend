using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.SignalR;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

public class CommonTaxonomyService : ICommonTaxonomyNode
{
    private readonly ITaxonomyRepository  _taxonomyRepository;
    
    private readonly IUserRepository _userRepository;
    
    private readonly IHubContext<NotificationHub> _hubContext;
    
    private readonly IMapper _mapper;

    public CommonTaxonomyService(
            ITaxonomyRepository taxonomyRepository,
            IMapper mapper,
            IUserRepository userRepository, IHubContext<NotificationHub> hubContext)
    {
        _taxonomyRepository = taxonomyRepository;
        _mapper = mapper;
        _userRepository = userRepository;
        _hubContext = hubContext;
    }

    [Transaction(ReadOnly = false)]
    public async Task<CreatedObjectResponseDto> CreateTaxonomyNodeAsync(
        CreateTaxonomyNodeRequestDto requestDto)
    {
        var mappedTaxonomy = _mapper.Map<TaxonomyNodeEntity>(requestDto);
        
        mappedTaxonomy.Rank = await GetTaxonomyRank(requestDto.ParentGuid);
        
        var createdTaxonomy = await _taxonomyRepository.CreateAsync(mappedTaxonomy);

        await SendNotificationAsync();
        
        return new CreatedObjectResponseDto
        {
            CreatedObjectGuid = createdTaxonomy.Guid,
        };
    }

    [Transaction(ReadOnly = true)]
    public async Task<List<List<GetAllTaxonomyNodesByFilterDto>>> GetAllTaxonomyNodesAsync(
        string? startsWith,
        TaxonomyRank rank)
    {
        var taxonomyNodes = await
            _taxonomyRepository
                .GetTaxonomyNodeByStartsWithNameAsync(startsWith, rank);

        var taxonomyTrees = new List<List<GetAllTaxonomyNodesByFilterDto>>();

        foreach (var taxonomyNode in taxonomyNodes)
        {
            var taxonomyTreeList = new List<TaxonomyNodeEntity>();

            var taxonomyTree = await GetTaxonomyTreeAsync(
                taxonomyNode, taxonomyTreeList);

            var mappedTaxonomyTree = _mapper.Map<List<GetAllTaxonomyNodesByFilterDto>>(taxonomyTree);

            mappedTaxonomyTree = mappedTaxonomyTree
                .OrderBy(x => x.Rank)
                .ToList();

            taxonomyTrees.Add(mappedTaxonomyTree);
        }

        return taxonomyTrees;
    }

    private async Task<TaxonomyRank> GetTaxonomyRank(Guid? parentGuid)
    {
        if (parentGuid == null)
        {
            return TaxonomyRank.Phylum;
        }
        
        var parentNode = await _taxonomyRepository.GetTaxonomyNodeByGuidAsync((Guid)parentGuid);

        if (parentNode == null)
        {
            throw new ZootifyException(ErrorCode.NoTaxonomyWithThisGuid);
        }

        if (parentNode.Rank == TaxonomyRank.Species)
        {
            throw new ZootifyException(ErrorCode.TaxonomyRankIsAtTheDeepestLevel);
        }
        
        return parentNode.Rank + 1;
    }

    private async Task<List<TaxonomyNodeEntity>> GetTaxonomyTreeAsync(
        TaxonomyNodeEntity taxonomyNode,
        List<TaxonomyNodeEntity> taxonomyNodes)
    {
        taxonomyNodes.Add(taxonomyNode);

        if (taxonomyNode.ParentGuid == null)
        {
            return taxonomyNodes;
        }

        var parentTaxonomy = await _taxonomyRepository
            .GetTaxonomyNodeByGuidAsync((Guid)taxonomyNode.ParentGuid);

        if (parentTaxonomy == null)
        {
            throw new ZootifyException(ErrorCode.NoTaxonomyWithThisGuid);
        }

        return await GetTaxonomyTreeAsync(parentTaxonomy, taxonomyNodes);
    }
    
    private async Task SendNotificationAsync()
    {
          
        var users = await _userRepository.GetAllAdminsAndModeratorUsersWithSettingsAsync();
        var message = "New Taxonomy was created. Please check the new Taxonomy.";
        
        foreach (var user in users)
        {
            
            if (user.Settings != null)
            {
                message = user.Settings.Language == "de"
                    ? "Eine neue Taxonomie wurde erstellt. Bitte überprüfen Sie die neue Taxonomie."
                    : message;   
            }
            
            await _hubContext.Clients
                .User(user.Guid.ToString())
                .SendAsync("ReceiveNotification", message);
        }       
    }
}