using Babylon.Alfred.Api.Shared.Data.Models;

namespace Babylon.Alfred.Api.Shared.Repositories;

public interface IRecurringScheduleRepository
{
    Task<RecurringSchedule?> GetByUserIdAndSecurityIdAsync(Guid userId, Guid securityId);
    Task<List<RecurringSchedule>> GetActiveByUserIdAsync(Guid userId);
    Task<RecurringSchedule> AddAsync(RecurringSchedule schedule);
    Task<RecurringSchedule> UpdateAsync(RecurringSchedule schedule);
    Task<RecurringSchedule?> GetByIdAsync(Guid id);
}

