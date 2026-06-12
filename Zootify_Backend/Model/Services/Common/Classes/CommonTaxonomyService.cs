using AutoMapper;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.SignalR;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

public class CommonTaxonomyService : ICommonTaxonomyNodeService
{
    private readonly ITaxonomyRepository  _taxonomyRepository;
    
    private readonly IUserRepository _userRepository;
    
    private readonly INotificationProvider _notificationProvider;
    
    private readonly IMapper _mapper;

    public CommonTaxonomyService(
            ITaxonomyRepository taxonomyRepository,
            IMapper mapper,
            IUserRepository userRepository, 
            INotificationProvider notificationProvider)
    {
        _taxonomyRepository = taxonomyRepository;
        _mapper = mapper;
        _userRepository = userRepository;
        _notificationProvider = notificationProvider;
    }

    [Transaction(ReadOnly = false)]
    public async Task<List<CreatedObjectResponseDto>> CreateTaxonomyNodeAsync(
        IAuthentication authentication,
        CreateTaxonomyNodeRequestDto requestDto)
    {
        var sortedTaxonomy = requestDto.TaxonomyNodes.OrderBy(tn => tn.Key).ToDictionary();

        var parentGuid = requestDto.ParentGuid;

        if (parentGuid != null)
        {
            var parent = await _taxonomyRepository.GetTaxonomyNodeByGuidAsync(parentGuid.Value);

            if (parent == null)
            {
                throw new ZootifyException(ErrorCode.NoTaxonomyWithThisGuid);
            }
        }

        var needsApproval = authentication.Role == UserRole.User;
        
        var nodesToCreate = new List<TaxonomyNodeEntity>();
        
        foreach (var taxonomyNode in sortedTaxonomy)
        {
            var node = _mapper.Map<TaxonomyNodeEntity>(taxonomyNode);
            node.SetCreatedBy(authentication.UserGuid);
            node.ParentGuid = parentGuid;
            parentGuid = node.Guid;

            if (!needsApproval)
            {
                node.ApprovalStatus = ApprovalStatus.Approved;
            }
            
            nodesToCreate.Add(node);
        }
        
        var createdNodes = await _taxonomyRepository.MassCreateAsync(nodesToCreate);

        await SendNotificationAsync(needsApproval);
        
        return _mapper.Map<List<CreatedObjectResponseDto>>(createdNodes);
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
    
    [Transaction(ReadOnly = true)]
    public async Task<GetTaxonomyTreeResponseDto> GetAllTaxonomyNodesAsTreeAsync(string? filterByName)
    {
        var allNodes = await _taxonomyRepository.GetAllNodesForTreeAsync();
        var byParent = allNodes.ToLookup(n => n.ParentGuid);

        var roots = byParent[null].AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filterByName))
        {
            roots = roots.Where(n => n.Name.Contains(filterByName, StringComparison.OrdinalIgnoreCase));
        }

        var nodes = roots
            .OrderBy(n => n.Name)
            .Select(r => BuildTreeNode(r, byParent))
            .ToList();

        return new GetTaxonomyTreeResponseDto
        {
            TotalCount = nodes.Count,
            Nodes      = nodes,
        };
    }

    [Transaction(ReadOnly = true)]
    public async Task<List<TaxonomyNodeEntity>> GetTaxonomyTreeAsync(
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
    
    private async Task SendNotificationAsync(bool needsApproval)
    {
        if (!needsApproval)
        {
            return;
        }
        
        var users = await _userRepository.GetAllAdminsAndModeratorUsersWithSettingsAsync();
        
        _ = _notificationProvider.SendNotificationAsync(users);
    }
    
    private static GetTaxonomyTreeNodeDto BuildTreeNode(
        TaxonomyNodeEntity node,
        ILookup<Guid?, TaxonomyNodeEntity> byParent) => new()
    {
        TaxonomyGuid   = node.Guid,
        Name           = node.Name,
        Rank           = node.Rank,
        ApprovalStatus = node.ApprovalStatus,
        Children = byParent[node.Guid]
            .OrderBy(c => c.Name)
            .Select(c => BuildTreeNode(c, byParent))
            .ToList(),
    };
}