using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class UserRepository : BaseRepository<UserEntity>, IUserRepository
{
    public UserRepository(ZootifyDbContext context) : base(context)
    {
    }

    public Task<UserEntity?> GetUserByGuidWithAuditInformationAsync(Guid guid)
    {
        return Context
            .GetTrackingSet<UserEntity>()
            .Include(u => u.UpdatedByUser)
            .Include(u => u.DeletedByUser)
            .FirstOrDefaultAsync(u => u.Guid == guid);
    }

    public async Task<UserEntity?> GetActiveUserByGuid(Guid guid)
    {
        return await Context.GetTrackingSet<UserEntity>()
            .FirstOrDefaultAsync(u => u.Guid == guid
                                      && u.Status == UserStatus.Active);
    }

    public async Task<UserEntity?> GetDeletedUserByGuid(Guid guid)
    {
        return await Context.GetTrackingSet<UserEntity>()
            .FirstOrDefaultAsync(u => u.Guid == guid
                                      && u.Status == UserStatus.Deleted);
    }

    public async Task<List<UserEntity>> GetAllUsersAsync(
        GetAllUsersFilterDto query)
    {
        var userQuery = Context
            .GetTrackingSet<UserEntity>()
            .OrderBy(u => u.Guid)
            .AsQueryable();
        
        var filteredQuery = FilterUserQuery(userQuery, query);
        
        var sortedQuery = SortUserQuery(
            filteredQuery,
            query.SortBy,
            query.SortDirection);
        
        return await sortedQuery
            .Skip((query.Page-1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();
    }

    private static IQueryable<UserEntity> FilterUserQuery(
        IQueryable<UserEntity> userQuery,
        GetAllUsersFilterDto query)
    {
        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            userQuery = userQuery.Where(q => q.Email.Contains(query.Email));
        }
        if (!string.IsNullOrWhiteSpace(query.FirstName))
        {
            userQuery = userQuery.Where(q => q.FirstName.Contains(query.FirstName));
        }
        if (!string.IsNullOrWhiteSpace(query.LastName))
        {
            userQuery = userQuery.Where(q => q.LastName.Contains(query.LastName));
        }
        if (query.Role != null)
        {
            userQuery = userQuery.Where(q => q.Role == query.Role);
        }
        if (query.Status != null)
        {
            userQuery = userQuery.Where(q => q.Status == query.Status);
        }
        
        return userQuery;
    }

    private static IQueryable<UserEntity> SortUserQuery(
        IQueryable<UserEntity> userQuery,
        string? sortBy,
        SortDirection sortDirection)
    {
        switch (sortBy)
        {
            case "firstName":
                return ApplySort(userQuery, u => u.FirstName, sortDirection);
            case "lastName":
                return ApplySort(userQuery, u => u.LastName, sortDirection);
            case "email":
                return ApplySort(userQuery, u => u.Email, sortDirection);
            case "role":
                return ApplySort(userQuery, u => u.Role, sortDirection);
            case "status":
                return ApplySort(userQuery, u => u.Status, sortDirection);
            default:
                return ApplySort(userQuery, u => u.Guid, sortDirection);
        }
    }
    
    private static IOrderedQueryable<UserEntity> ApplySort<TKey>(
        IQueryable<UserEntity> userQuery,
        Expression<Func<UserEntity, TKey>> keySelector,
        SortDirection direction)
    {
        if (direction == SortDirection.DESC)
        {
            return userQuery.OrderByDescending(keySelector).ThenBy(q => q.Guid);
        }
        
        return userQuery.OrderBy(keySelector).ThenBy(q => q.Guid);
    }
}