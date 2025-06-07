using Azure.Data.Tables;
using SEAPIRATE.Website.Data;
using SEAPIRATE.Website.Models;

namespace SEAPIRATE.Website.Services;

public class AttackService
{
    private readonly TableClient _tableClient;
    private readonly string _userId = "default"; // For now, using a default user. Can be extended for multi-user support

    public AttackService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage") 
            ?? configuration["AzureStorage:ConnectionString"];
        
        var serviceClient = new TableServiceClient(connectionString);
        _tableClient = serviceClient.GetTableClient("attacks");
        
        // Create table if it doesn't exist
        _tableClient.CreateIfNotExists();
        
        // Seed sample data if table is empty
        _ = Task.Run(SeedSampleDataIfEmpty);
    }

    private async Task SeedSampleDataIfEmpty()
    {
        try
        {
            var existingAttacks = await GetSuggestedAttacksAsync();
            if (!existingAttacks.Any())
            {
                var sampleAttacks = new List<AttackDto>
                {
                    new AttackDto
                    {
                        TargetIsland = "9:92:10",
                        TargetPlayer = "unbenannt",
                        ExpectedResources = 250,
                        SuggestedTime = DateTime.Now.AddHours(1),
                        Status = AttackStatus.Suggested,
                        Notes = "Small inactive island, easy target"
                    },
                    new AttackDto
                    {
                        TargetIsland = "6:63:9",
                        TargetPlayer = "unbenannt",
                        ExpectedResources = 150,
                        SuggestedTime = DateTime.Now.AddHours(2),
                        Status = AttackStatus.Suggested,
                        Notes = "Low resources but safe attack"
                    },
                    new AttackDto
                    {
                        TargetIsland = "8:36:6",
                        TargetPlayer = "unbenannt",
                        ExpectedResources = 300,
                        SuggestedTime = DateTime.Now.AddHours(3),
                        Status = AttackStatus.Suggested,
                        Notes = "Medium value target, attack during night"
                    }
                };

                foreach (var attack in sampleAttacks)
                {
                    await AddAttackAsync(attack);
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the service initialization
            Console.WriteLine($"Error seeding sample data: {ex.Message}");
        }
    }

    public async Task<List<AttackDto>> GetSuggestedAttacksAsync()
    {
        try
        {
            var filter = TableClient.CreateQueryFilter($"PartitionKey eq {_userId} and Status eq {AttackStatus.Suggested}");
            var entities = await _tableClient.QueryAsync<AttackEntity>(filter).ToListAsync();
            return entities.Select(e => e.ToDto()).OrderBy(a => a.SuggestedTime).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting suggested attacks: {ex.Message}");
            return new List<AttackDto>();
        }
    }

    public async Task<List<AttackDto>> GetPendingAttacksAsync()
    {
        try
        {
            var filter = TableClient.CreateQueryFilter($"PartitionKey eq {_userId} and Status eq {AttackStatus.Pending}");
            var entities = await _tableClient.QueryAsync<AttackEntity>(filter).ToListAsync();
            return entities.Select(e => e.ToDto()).OrderBy(a => a.StartedAt).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting pending attacks: {ex.Message}");
            return new List<AttackDto>();
        }
    }

    public async Task<List<AttackDto>> GetCompletedAttacksAsync()
    {
        try
        {
            var filter = TableClient.CreateQueryFilter($"PartitionKey eq {_userId} and (Status eq {AttackStatus.Completed} or Status eq {AttackStatus.Failed})");
            var entities = await _tableClient.QueryAsync<AttackEntity>(filter).ToListAsync();
            return entities.Select(e => e.ToDto()).OrderByDescending(a => a.CompletedAt).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting completed attacks: {ex.Message}");
            return new List<AttackDto>();
        }
    }

    public async Task<AttackDto?> GetAttackByIdAsync(Guid id)
    {
        try
        {
            var response = await _tableClient.GetEntityIfExistsAsync<AttackEntity>(_userId, id.ToString());
            return response.HasValue ? response.Value.ToDto() : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting attack by ID: {ex.Message}");
            return null;
        }
    }

    public async Task StartAttackAsync(Guid id)
    {
        try
        {
            var response = await _tableClient.GetEntityIfExistsAsync<AttackEntity>(_userId, id.ToString());
            if (response.HasValue)
            {
                var entity = response.Value;
                entity.Status = AttackStatus.Pending.ToString();
                entity.StartedAt = DateTime.Now;
                await _tableClient.UpdateEntityAsync(entity, entity.ETag);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting attack: {ex.Message}");
            throw;
        }
    }

    public async Task CompleteAttackAsync(Guid id, string fightReportId, int actualResourcesGained)
    {
        try
        {
            var response = await _tableClient.GetEntityIfExistsAsync<AttackEntity>(_userId, id.ToString());
            if (response.HasValue)
            {
                var entity = response.Value;
                entity.Status = AttackStatus.Completed.ToString();
                entity.CompletedAt = DateTime.Now;
                entity.FightReportId = fightReportId;
                entity.ActualResourcesGained = actualResourcesGained;
                await _tableClient.UpdateEntityAsync(entity, entity.ETag);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error completing attack: {ex.Message}");
            throw;
        }
    }

    public async Task FailAttackAsync(Guid id, string? notes = null)
    {
        try
        {
            var response = await _tableClient.GetEntityIfExistsAsync<AttackEntity>(_userId, id.ToString());
            if (response.HasValue)
            {
                var entity = response.Value;
                entity.Status = AttackStatus.Failed.ToString();
                entity.CompletedAt = DateTime.Now;
                if (!string.IsNullOrEmpty(notes))
                {
                    entity.Notes = string.IsNullOrEmpty(entity.Notes) 
                        ? notes 
                        : $"{entity.Notes}\n{notes}";
                }
                await _tableClient.UpdateEntityAsync(entity, entity.ETag);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error failing attack: {ex.Message}");
            throw;
        }
    }

    public async Task AddAttackAsync(AttackDto attack)
    {
        try
        {
            var entity = new AttackEntity(attack, _userId);
            await _tableClient.AddEntityAsync(entity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding attack: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateAttackAsync(AttackDto attack)
    {
        try
        {
            var response = await _tableClient.GetEntityIfExistsAsync<AttackEntity>(_userId, attack.Id.ToString());
            if (response.HasValue)
            {
                var entity = new AttackEntity(attack, _userId)
                {
                    ETag = response.Value.ETag,
                    Timestamp = response.Value.Timestamp
                };
                await _tableClient.UpdateEntityAsync(entity, entity.ETag);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating attack: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteAttackAsync(Guid id)
    {
        try
        {
            await _tableClient.DeleteEntityAsync(_userId, id.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting attack: {ex.Message}");
            throw;
        }
    }

    public async Task<AttackDto?> FindPendingAttackByTargetAsync(string targetIsland, string targetPlayer)
    {
        try
        {
            var filter = TableClient.CreateQueryFilter($"PartitionKey eq {_userId} and Status eq {AttackStatus.Pending} and TargetIsland eq {targetIsland} and TargetPlayer eq {targetPlayer}");
            var entities = await _tableClient.QueryAsync<AttackEntity>(filter).ToListAsync();
            return entities.FirstOrDefault()?.ToDto();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding pending attack by target: {ex.Message}");
            return null;
        }
    }
}
