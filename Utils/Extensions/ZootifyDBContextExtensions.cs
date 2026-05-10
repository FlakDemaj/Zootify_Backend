using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Enums;
using Zootify_Backend.Models.DataAccess.DBContext;
using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Extensions;

/// <summary>
/// Extensions for ArchitektusContext
/// </summary>
public static class ZootifyDbContextExtensions
{
	public static async Task<TSaveType> CreateAsync<TSaveType>(
		this ZootifyDbContext context,
		TSaveType saveData)
		where TSaveType : BaseGuidEntity
	{
		try
		{

			var result = context.Add(saveData);
			await context.SaveChangesAsync();
			return result.Entity;
		}
		finally
		{
			context.Entry(saveData).State = EntityState.Detached;
		}
	}

	public static async Task DeleteAsync<TDeleteType>(
		this ZootifyDbContext context,
		TDeleteType deleteData)
		where TDeleteType : BaseGuidEntity
	{
		context.Set<TDeleteType>().Remove(deleteData);
		await context.SaveChangesAsync();
	}

	public static async Task UpdateAndSaveChangesAsync(
		this ZootifyDbContext context,
		object updateData)
	{
		try
		{
			if (context.Entry(updateData).State == EntityState.Detached)
			{
				context.Entry(updateData).State = EntityState.Modified;
			}
			await context.SaveChangesAsync();
		}
		catch (Exception e)
		{
			// This can happen, if the data was already updated, happens if
			// two requests happen concurrently. So we the Server just ignores the Exception
			if (e is DbUpdateConcurrencyException)
			{
				return;
			}

			throw new ZootifyException(ErrorCode.DatabaseError, e);
		}
		finally
		{
			context.Entry(updateData).State = EntityState.Detached;
		}
	}
	
	public static IQueryable<T> GetTrackingSet<T>(
		this ZootifyDbContext context,
		bool tracking = false)
		where T : class
	{
		return tracking ? context.Set<T>().AsTracking() : context.Set<T>().AsNoTracking();
	}
}