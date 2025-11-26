using Babylon.Alfred.Api.Features.RecurringSchedules.Models.Requests;
using Babylon.Alfred.Api.Features.RecurringSchedules.Models.Responses;

namespace Babylon.Alfred.Api.Features.RecurringSchedules.Services;

public interface IRecurringScheduleService
{
    Task<RecurringScheduleDto> CreateOrUpdateAsync(Guid? userId, CreateRecurringScheduleRequest request);
    Task<List<RecurringScheduleDto>> GetActiveByUserIdAsync(Guid? userId);
    Task DeleteAsync(Guid id);
}

