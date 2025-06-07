using Azure;
using Azure.Data.Tables;

namespace SEAPIRATE.Website.Models;

public class FightReportEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Basic report info
    public string RawReport { get; set; } = default!;
    public string Language { get; set; } = default!;
    public DateTime ReportDate { get; set; }
    
    // Attack details
    public string? AttackId { get; set; }
    public string? AttackerName { get; set; }
    public string? DefenderName { get; set; }
    public string? TargetIsland { get; set; }
    public int? ResourcesGained { get; set; }
    public bool? AttackSuccessful { get; set; }
    public DateTime? AttackTime { get; set; }
    
    // Detailed resources
    public int? WoodGained { get; set; }
    public int? StoneGained { get; set; }
    public int? IronGained { get; set; }
    public int? FoodGained { get; set; }
    
    // Units
    public string? AttackerUnits { get; set; }
    public string? DefenderUnits { get; set; }
    public string? AttackerLosses { get; set; }
    public string? DefenderLosses { get; set; }
    
    // Building damage
    public string? BuildingsDamaged { get; set; }
    
    // Parsing metadata
    public string? ParsedData { get; set; }
    public bool ParseSuccessful { get; set; }
    public string? ParseErrors { get; set; }

    public FightReportEntity() { }

    public FightReportEntity(string reportId, string rawReport, string language, string userId)
    {
        PartitionKey = userId;
        RowKey = reportId;
        RawReport = rawReport;
        Language = language;
        ReportDate = DateTime.UtcNow;
    }
}
