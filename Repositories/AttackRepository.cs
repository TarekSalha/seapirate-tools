using Azure.Data.Tables;
using SEAPIRATE.Models;
using SEAPIRATE.Data;

namespace SEAPIRATE.Repositories;

public class AttackRepository
{
    private readonly TableClient _tableClient;

    public AttackRepository(TableClientFactory factory)
    {
        _tableClient = factory.GetClient("attacks");
    }

    public async Task<List<AttackEntity>> GetAttacksByStatusAsync(string userId, AttackEntity.AttackStatus status)
    {
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {userId} and StatusString eq {status.ToString()}");
        return await QueryAttacksAsync(filter);
    }

    public async Task<AttackEntity?> GetAttackByIdAsync(string userId, Guid id)
    {
        var response = await _tableClient.GetEntityIfExistsAsync<AttackEntity>(userId, id.ToString());
        return response.HasValue ? response.Value : null;
    }

    public async Task AddAttackAsync(string userId, AttackEntity entity)
    {
        entity.PartitionKey = userId;
        entity.RowKey = entity.Id.ToString();
        await _tableClient.AddEntityAsync(entity);
    }

    public async Task UpdateAttackAsync(string userId, AttackEntity entity)
    {
        entity.PartitionKey = userId;
        entity.RowKey = entity.Id.ToString();
        await _tableClient.UpdateEntityAsync(entity, entity.ETag);
    }

    public async Task DeleteAttackAsync(string userId, Guid id)
    {
        await _tableClient.DeleteEntityAsync(userId, id.ToString());
    }

    public async Task UpdateAttackStatusAsync(
        string userId,
        Guid id,
        AttackEntity.AttackStatus status,
        string? fightReportId = null,
        int? actualResourcesGained = null,
        string? notes = null,
        DateTime? completedAt = null,
        DateTime? startedAt = null)
    {
        var entity = await GetAttackByIdAsync(userId, id);
        if (entity == null) throw new Exception("Attack not found");
        entity.Status = status;
        if (fightReportId != null) entity.FightReportId = fightReportId;
        if (notes != null) entity.Notes = notes;
        if (completedAt.HasValue) entity.CompletedAt = completedAt;
        if (startedAt.HasValue) entity.StartedAt = startedAt;
        await _tableClient.UpdateEntityAsync(entity, entity.ETag);
    }

    public async Task UpdateAttackAsync(
        string userId,
        Guid id,
        AttackEntity? updatedEntity = null,
        AttackEntity.AttackStatus? status = null,
        string? fightReportId = null,
        string? notes = null,
        DateTime? completedAt = null,
        DateTime? startedAt = null)
    {
        var entity = await GetAttackByIdAsync(userId, id);
        if (entity == null) throw new Exception("Attack not found");

        if (updatedEntity != null)
        {
            // Overwrite all updatable fields from updatedEntity
            entity.TargetIsland = updatedEntity.TargetIsland;
            entity.Latitude = updatedEntity.Latitude;
            entity.Longitude = updatedEntity.Longitude;
            entity.Ocean = updatedEntity.Ocean;
            entity.IslandGroup = updatedEntity.IslandGroup;
            entity.IslandNumber = updatedEntity.IslandNumber;
            entity.StartedAt = updatedEntity.StartedAt;
            entity.CompletedAt = updatedEntity.CompletedAt;
            entity.Status = updatedEntity.Status;
            entity.Notes = updatedEntity.Notes;
            entity.FightReportId = updatedEntity.FightReportId;
            entity.CreatedAt = updatedEntity.CreatedAt;
        }

        if (status.HasValue) entity.Status = status.Value;
        if (fightReportId != null) entity.FightReportId = fightReportId;
        if (notes != null) entity.Notes = notes;
        if (completedAt.HasValue) entity.CompletedAt = completedAt;
        if (startedAt.HasValue) entity.StartedAt = startedAt;

        await _tableClient.UpdateEntityAsync(entity, entity.ETag);
    }

    public async Task<AttackEntity?> FindPendingAttackByTargetAsync(string userId, string targetIsland)
    {
        var filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {userId} and StatusString eq 'Pending' and TargetIsland eq {targetIsland}");
        var results = await QueryAttacksAsync(filter);
        return results.FirstOrDefault();
    }

    private async Task<List<AttackEntity>> QueryAttacksAsync(string filter)
    {
        var entities = new List<AttackEntity>();
        await foreach (var entity in _tableClient.QueryAsync<AttackEntity>(filter))
        {
            entities.Add(entity);
        }
        return entities;
    }
}