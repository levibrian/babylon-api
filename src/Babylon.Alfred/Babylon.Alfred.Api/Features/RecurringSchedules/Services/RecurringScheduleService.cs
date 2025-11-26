using Babylon.Alfred.Api.Features.RecurringSchedules.Models.Requests;
using Babylon.Alfred.Api.Features.RecurringSchedules.Models.Responses;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.RecurringSchedules.Services;

public class RecurringScheduleService(
    IRecurringScheduleRepository recurringScheduleRepository,
    ISecurityRepository securityRepository) : IRecurringScheduleService
{
    public async Task<RecurringScheduleDto> CreateOrUpdateAsync(Guid? userId, CreateRecurringScheduleRequest request)
    {
        var effectiveUserId = userId ?? Constants.User.RootUserId;

        // Step 1: Check if Security exists, create if not
        var security = await securityRepository.GetByTickerAsync(request.Ticker);
        if (security == null)
        {
            var newSecurity = new Security
            {
                Ticker = request.Ticker,
                SecurityName = request.SecurityName,
                SecurityType = SecurityType.Stock, // Default to Stock as specified
                LastUpdated = DateTime.UtcNow
            };
            security = await securityRepository.AddOrUpdateAsync(newSecurity);
        }

        // Step 2: Check if schedule exists for this User+Security
        var existingSchedule = await recurringScheduleRepository.GetByUserIdAndSecurityIdAsync(effectiveUserId, security.Id);

        if (existingSchedule != null)
        {
            // Reactivate and update
            existingSchedule.IsActive = true;
            existingSchedule.Platform = request.Platform;
            existingSchedule.TargetAmount = request.TargetAmount;
            await recurringScheduleRepository.UpdateAsync(existingSchedule);

            return new RecurringScheduleDto
            {
                Id = existingSchedule.Id,
                Ticker = security.Ticker,
                SecurityName = security.SecurityName,
                Platform = existingSchedule.Platform,
                TargetAmount = existingSchedule.TargetAmount
            };
        }

        // Create new schedule
        var newSchedule = new RecurringSchedule
        {
            UserId = effectiveUserId,
            SecurityId = security.Id,
            Platform = request.Platform,
            TargetAmount = request.TargetAmount,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdSchedule = await recurringScheduleRepository.AddAsync(newSchedule);

        return new RecurringScheduleDto
        {
            Id = createdSchedule.Id,
            Ticker = security.Ticker,
            SecurityName = security.SecurityName,
            Platform = createdSchedule.Platform,
            TargetAmount = createdSchedule.TargetAmount
        };
    }

    public async Task<List<RecurringScheduleDto>> GetActiveByUserIdAsync(Guid? userId)
    {
        var effectiveUserId = userId ?? Constants.User.RootUserId;

        var schedules = await recurringScheduleRepository.GetActiveByUserIdAsync(effectiveUserId);

        return schedules.Select(s => new RecurringScheduleDto
        {
            Id = s.Id,
            Ticker = s.Security.Ticker,
            SecurityName = s.Security.SecurityName,
            Platform = s.Platform,
            TargetAmount = s.TargetAmount
        }).ToList();
    }

    public async Task DeleteAsync(Guid id)
    {
        var schedule = await recurringScheduleRepository.GetByIdAsync(id);
        if (schedule == null)
        {
            throw new InvalidOperationException($"Recurring schedule with id {id} not found.");
        }

        schedule.IsActive = false;
        await recurringScheduleRepository.UpdateAsync(schedule);
    }
}

