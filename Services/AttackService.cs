using SEAPIRATE.Models;
using SEAPIRATE.Repositories;
using SEAPIRATE.Data;

namespace SEAPIRATE.Services;

public class AttackService
{
    private readonly AttackRepository _repo;
    private readonly UserService _userService;

    public AttackService(AttackRepository repo, UserService userService)
    {
        _repo = repo;
        _userService = userService;
    }

    private async Task<string> GetCurrentUserIdAsync()
        => await _userService.GetCurrentUserIdAsync();

    public async Task<List<AttackDto>> GetAttacksByStatusAsync(AttackEntity.AttackStatus status)
    {
        var userId = await GetCurrentUserIdAsync();
        var entities = await _repo.GetAttacksByStatusAsync(userId, status);
        return entities.Select(e => e.ToDto()).ToList();
    }

    public async Task<AttackDto?> GetAttackByIdAsync(Guid id)
    {
        var userId = await GetCurrentUserIdAsync();
        var entity = await _repo.GetAttackByIdAsync(userId, id);
        return entity?.ToDto();
    }

    public async Task AddAttackAsync(AttackDto attackDto)
    {
        var userId = await GetCurrentUserIdAsync();
        var entity = new AttackEntity
        {
            Id = attackDto.Id,
            TargetIsland = attackDto.TargetIsland,
            ScheduledAt = attackDto.ScheduledAt,
            ActualResourcesGained = attackDto.ActualResourcesGained,
            StartedAt = attackDto.StartedAt,
            CompletedAt = attackDto.CompletedAt,
            FailedAt = attackDto.FailedAt,
            Status = attackDto.Status,
            Notes = attackDto.Notes,
            FightReportId = attackDto.FightReportId,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.AddAttackAsync(userId, entity);
    }

    public async Task UpdateAttackAsync(AttackDto attackDto)
    {
        var userId = await GetCurrentUserIdAsync();
        var entity = new AttackEntity
        {
            Id = attackDto.Id,
            TargetIsland = attackDto.TargetIsland,
            ScheduledAt = attackDto.ScheduledAt,
            ActualResourcesGained = attackDto.ActualResourcesGained,
            StartedAt = attackDto.StartedAt,
            CompletedAt = attackDto.CompletedAt,
            FailedAt = attackDto.FailedAt,
            Status = attackDto.Status,
            Notes = attackDto.Notes,
            FightReportId = attackDto.FightReportId,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.UpdateAttackAsync(userId, entity);
    }

    public async Task DeleteAttackAsync(Guid id)
    {
        var userId = await GetCurrentUserIdAsync();
        await _repo.DeleteAttackAsync(userId, id);
    }

    public async Task UpdateAttackStatusAsync(Guid id, AttackEntity.AttackStatus status, DateTime? startedAt = null)
    {
        var userId = await GetCurrentUserIdAsync();
        var attack = await _repo.GetAttackByIdAsync(userId, id);
        if (attack == null) return;
        attack.Status = status;
        if (startedAt.HasValue)
            attack.StartedAt = startedAt.Value;
        await _repo.UpdateAttackAsync(userId, attack);
    }

    public async Task CompleteAttackAsync(Guid id, string fightReportId, int actualResourcesGained, DateTime completedAt)
    {
        var userId = await GetCurrentUserIdAsync();
        // To mark as completed:
        var attack = await _repo.GetAttackByIdAsync(userId, id);
        if (attack == null) return;
        attack.Status = AttackEntity.AttackStatus.Succeeded;
        attack.FightReportId = fightReportId;
        attack.ActualResourcesGained = actualResourcesGained;
        attack.CompletedAt = completedAt;
        await _repo.UpdateAttackAsync(userId, attack);
    }

    public async Task FailAttackAsync(Guid id, string? notes, DateTime failedAt)
    {
        var userId = await GetCurrentUserIdAsync();
        // To mark as failed:
        var attack = await _repo.GetAttackByIdAsync(userId, id);
        if (attack == null) return;
        attack.Status = AttackEntity.AttackStatus.Failed;
        attack.Notes = notes;
        attack.FailedAt = failedAt;
        await _repo.UpdateAttackAsync(userId, attack);
    }
}