using Babylon.Alfred.Api.Shared.Data;
using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Repositories;

public class RecurringScheduleRepository(BabylonDbContext context) : IRecurringScheduleRepository
{
    public async Task<RecurringSchedule?> GetByUserIdAndSecurityIdAsync(Guid userId, Guid securityId)
    {
        return await context.RecurringSchedules
            .Include(s => s.Security)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SecurityId == securityId);
    }

    public async Task<List<RecurringSchedule>> GetActiveByUserIdAsync(Guid userId)
    {
        return await context.RecurringSchedules
            .Include(s => s.Security)
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.Platform ?? string.Empty)
            .ThenBy(s => s.Security.Ticker)
            .ToListAsync();
    }

    public async Task<RecurringSchedule> AddAsync(RecurringSchedule schedule)
    {
        if (schedule.Id == Guid.Empty)
        {
            schedule.Id = Guid.NewGuid();
        }

        await context.RecurringSchedules.AddAsync(schedule);
        await context.SaveChangesAsync();
        return schedule;
    }

    public async Task<RecurringSchedule> UpdateAsync(RecurringSchedule schedule)
    {
        context.RecurringSchedules.Update(schedule);
        await context.SaveChangesAsync();
        return schedule;
    }

    public async Task<RecurringSchedule?> GetByIdAsync(Guid id)
    {
        return await context.RecurringSchedules
            .Include(s => s.Security)
            .FirstOrDefaultAsync(s => s.Id == id);
    }
}

