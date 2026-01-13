using Babylon.Alfred.Api.Features.RecurringSchedules.Models.Requests;
using Babylon.Alfred.Api.Features.RecurringSchedules.Models.Responses;
using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Repositories;

namespace Babylon.Alfred.Api.Features.RecurringSchedules.Services;

public class RecurringScheduleService(
    IRecurringScheduleRepository recurringScheduleRepository,
    ISecurityRepository securityRepository) : IRecurringScheduleService
{
    public async Task<RecurringScheduleDto> CreateOrUpdateAsync(Guid userId, CreateRecurringScheduleRequest request)
    {
        var effectiveUserId = userId;

        // Step 1: Check if Security exists, create if not
        var security = await securityRepository.GetByTickerAsync(request.Ticker);
        if (security == null)
        {
            var newSecurity = new Security
            {
                Id = Guid.NewGuid(),
                Ticker = request.Ticker,
                SecurityName = request.SecurityName,
                SecurityType = SecurityType.Stock,
                LastUpdated = DateTime.UtcNow
            };
            await securityRepository.AddOrUpdateAsync(newSecurity);
            security = newSecurity;
        }

        // Step 2: Check if RecurringSchedule exists
        var existingSchedule = await recurringScheduleRepository.GetByUserIdAndSecurityIdAsync(effectiveUserId, security.Id);

        if (existingSchedule != null)
        {
            // Update existing
            existingSchedule.IsActive = true;
            existingSchedule.Platform = request.Platform;
            existingSchedule.TargetAmount = request.TargetAmount ?? 0; // Assuming 0 if null
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

        // Create new
        var newSchedule = new RecurringSchedule
        {
            Id = Guid.NewGuid(),
            UserId = effectiveUserId,
            SecurityId = security.Id,
            Platform = request.Platform,
            TargetAmount = request.TargetAmount ?? 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await recurringScheduleRepository.AddAsync(newSchedule);

        return new RecurringScheduleDto
        {
            Id = newSchedule.Id,
            Ticker = security.Ticker,
            SecurityName = security.SecurityName,
            Platform = newSchedule.Platform,
            TargetAmount = newSchedule.TargetAmount
        };
    }

    public async Task<List<RecurringScheduleDto>> GetActiveByUserIdAsync(Guid userId)
    {
        var effectiveUserId = userId;

        var schedules = await recurringScheduleRepository.GetActiveByUserIdAsync(effectiveUserId);

        var dtos = new List<RecurringScheduleDto>();
        foreach (var schedule in schedules)
        {
             // If Security navigation property is loaded
             var ticker = schedule.Security?.Ticker ?? "UNKNOWN";
             var name = schedule.Security?.SecurityName ?? "Unknown";

             dtos.Add(new RecurringScheduleDto
             {
                 Id = schedule.Id,
                 Ticker = ticker,
                 SecurityName = name,
                 Platform = schedule.Platform,
                 TargetAmount = schedule.TargetAmount
             });
        }

        return dtos;
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
