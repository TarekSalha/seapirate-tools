using Azure.Data.Tables;
using SEAPIRATE.Website.Data;
using SEAPIRATE.Website.Models;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SEAPIRATE.Website.Services;

public class FightReportService
{
    private readonly TableClient _tableClient;
    private readonly AttackService _attackService;
    private readonly string _userId = "default";
    private readonly ILogger<FightReportService> _logger;

    public FightReportService(
        IConfiguration configuration, 
        AttackService attackService,
        ILogger<FightReportService> logger)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage") 
            ?? configuration["AzureStorage:ConnectionString"];
        
        var serviceClient = new TableServiceClient(connectionString);
        _tableClient = serviceClient.GetTableClient("fightreports");
        _attackService = attackService;
        _logger = logger;
        
        // Create table if it doesn't exist
        _tableClient.CreateIfNotExists();
    }

    public async Task<List<ParsedFightReport>> ProcessMultipleFightReportsAsync(string rawReports, string language)
    {
        var reports = new List<ParsedFightReport>();
        
        try
        {
            // Split multiple reports
            var individualReports = SplitMultipleReports(rawReports);
            
            foreach (var report in individualReports)
            {
                if (!string.IsNullOrWhiteSpace(report))
                {
                    var parsedReport = await ProcessSingleFightReportAsync(report, language);
                    reports.Add(parsedReport);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing multiple fight reports");
            throw;
        }
        
        return reports;
    }

    private List<string> SplitMultipleReports(string rawReports)
    {
        // Split by "Kampfbericht" for German reports
        var reports = new List<string>();
        var parts = rawReports.Split(new[] { "Kampfbericht" }, StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < parts.Length; i++)
        {
            if (i == 0 && !rawReports.StartsWith("Kampfbericht"))
            {
                // Skip the first part if it doesn't start with "Kampfbericht"
                continue;
            }
            
            // Add "Kampfbericht" back to each part (except the first one if it was already there)
            var report = "Kampfbericht" + parts[i].Trim();
            reports.Add(report);
        }
        
        return reports;
    }

    public async Task<ParsedFightReport> ProcessSingleFightReportAsync(string rawReport, string language)
    {
        try
        {
            _logger.LogInformation("Processing single fight report in {Language}", language);
            
            // Generate unique ID for this report
            var reportId = Guid.NewGuid().ToString();
            
            // Parse the fight report
            var parsedReport = ParseFightReport(rawReport, language);
            
            // Create and store the fight report entity
            var entity = new FightReportEntity(reportId, rawReport, language, _userId)
            {
                AttackerName = parsedReport.AttackerName,
                DefenderName = parsedReport.DefenderName,
                TargetIsland = parsedReport.TargetIsland,
                ResourcesGained = parsedReport.TotalResourcesGained,
                AttackSuccessful = parsedReport.AttackSuccessful,
                AttackTime = parsedReport.AttackTime,
                
                // Detailed resources - using German resource names
                WoodGained = parsedReport.HolzGained,
                StoneGained = parsedReport.SteineGained,
                IronGained = parsedReport.GoldGained, // Gold is treated as the main currency/resource
                
                // Units
                AttackerUnits = JsonSerializer.Serialize(parsedReport.AttackerUnits),
                DefenderUnits = JsonSerializer.Serialize(parsedReport.DefenderUnits),
                AttackerLosses = JsonSerializer.Serialize(parsedReport.AttackerLosses),
                DefenderLosses = JsonSerializer.Serialize(parsedReport.DefenderLosses),
                
                // Parsing metadata
                ParsedData = JsonSerializer.Serialize(parsedReport),
                ParseSuccessful = parsedReport.ParseErrors.Count == 0,
                ParseErrors = parsedReport.ParseErrors.Count > 0 ? 
                    string.Join("; ", parsedReport.ParseErrors) : null
            };

            // Try to find and link to a pending attack
            if (!string.IsNullOrEmpty(parsedReport.TargetIsland))
            {
                var pendingAttack = await _attackService.FindPendingAttackByTargetAsync(
                    parsedReport.TargetIsland, 
                    parsedReport.DefenderName ?? "unbenannt");
                
                if (pendingAttack != null)
                {
                    _logger.LogInformation("Matched report to pending attack {AttackId}", pendingAttack.Id);
                    entity.AttackId = pendingAttack.Id.ToString();
                    
                    // Complete the attack
                    await _attackService.CompleteAttackAsync(
                        pendingAttack.Id, 
                        reportId, 
                        parsedReport.TotalResourcesGained ?? 0);
                }
            }

            // Store the fight report
            await _tableClient.AddEntityAsync(entity);
            _logger.LogInformation("Stored fight report with ID {ReportId}", reportId);
            
            return parsedReport;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing fight report");
            throw;
        }
    }

    private ParsedFightReport ParseFightReport(string rawReport, string language)
    {
        var result = new ParsedFightReport
        {
            RawReport = rawReport,
            Language = language
        };

        try
        {
            // Normalize line endings and clean up the report
            rawReport = NormalizeReportText(rawReport);
            
            // Parse based on the actual German format from the samples
            ParseGermanInselkampfReport(rawReport, result);
            
            // Calculate total resources
            result.TotalResourcesGained = (result.GoldGained ?? 0) + 
                                         (result.SteineGained ?? 0) + 
                                         (result.HolzGained ?? 0);
            
            // All attacks in the samples were successful (resources were gained)
            result.AttackSuccessful = result.TotalResourcesGained > 0;
            
            // Validate parsed data
            ValidateParsedData(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing fight report");
            result.ParseErrors.Add($"Parsing error: {ex.Message}");
        }

        return result;
    }

    private void ParseGermanInselkampfReport(string report, ParsedFightReport result)
    {
        // Extract date and time - format: "vom DD.MM.YYYY HH:MM"
        var dateMatch = Regex.Match(report, @"vom\s+(\d{2}\.\d{2}\.\d{4}\s+\d{2}:\d{2})");
        if (dateMatch.Success)
        {
            var germanCulture = new CultureInfo("de-DE");
            if (DateTime.TryParse(dateMatch.Groups[1].Value, germanCulture, DateTimeStyles.None, out DateTime attackTime))
            {
                result.AttackTime = attackTime;
            }
        }

        // Extract attacker info - format: "Einheiten des Angreifers PlayerName (x:y:z)"
        var attackerMatch = Regex.Match(report, @"Einheiten des Angreifers\s+([^\s]+)\s+$$(\d+:\d+:\d+)$$");
        if (attackerMatch.Success)
        {
            result.AttackerName = attackerMatch.Groups[1].Value.Trim();
            result.AttackerIsland = attackerMatch.Groups[2].Value.Trim();
        }

        // Extract defender info - format: "Einheiten des Verteidigers unbenannt (x:y:z)"
        var defenderMatch = Regex.Match(report, @"Einheiten des Verteidigers\s+([^\s]+)\s+$$(\d+:\d+:\d+)$$");
        if (defenderMatch.Success)
        {
            result.DefenderName = defenderMatch.Groups[1].Value.Trim();
            result.TargetIsland = defenderMatch.Groups[2].Value.Trim();
        }

        // Extract attacker units and losses
        ExtractGermanUnits(report, result);

        // Extract resources - format under "Geplünderte Rohstoffe"
        ExtractGermanResources(report, result);
    }

    private void ExtractGermanUnits(string report, ParsedFightReport result)
    {
        // Find the section between attacker and defender
        var attackerSection = ExtractSection(report, "Einheiten des Angreifers", "Einheiten des Verteidigers");
        
        if (!string.IsNullOrEmpty(attackerSection))
        {
            // Extract unit lines - format: "UnitName\tTotal\tLosses"
            var unitLines = attackerSection.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line) && 
                              !line.Contains("Gesamt") && 
                              !line.Contains("Verluste") &&
                              !line.Contains("Einheiten des Angreifers"))
                .ToList();

            foreach (var line in unitLines)
            {
                // Split by tabs or multiple spaces
                var parts = Regex.Split(line.Trim(), @"\s{2,}|\t+")
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToArray();

                if (parts.Length >= 3)
                {
                    var unitName = parts[0].Trim();
                    if (int.TryParse(parts[1], out int total))
                    {
                        result.AttackerUnits[unitName] = total;
                    }
                    if (int.TryParse(parts[2], out int losses))
                    {
                        result.AttackerLosses[unitName] = losses;
                    }
                }
            }
        }
    }

    private void ExtractGermanResources(string report, ParsedFightReport result)
    {
        // Find the resources section
        var resourceSection = ExtractSection(report, "Geplünderte Rohstoffe", null);
        
        if (!string.IsNullOrEmpty(resourceSection))
        {
            // Extract Gold
            var goldMatch = Regex.Match(resourceSection, @"Gold\s+(\d+)");
            if (goldMatch.Success && int.TryParse(goldMatch.Groups[1].Value, out int gold))
            {
                result.GoldGained = gold;
            }

            // Extract Steine (Stone)
            var stoneMatch = Regex.Match(resourceSection, @"Steine\s+(\d+)");
            if (stoneMatch.Success && int.TryParse(stoneMatch.Groups[1].Value, out int stone))
            {
                result.SteineGained = stone;
            }

            // Extract Holz (Wood)
            var woodMatch = Regex.Match(resourceSection, @"Holz\s+(\d+)");
            if (woodMatch.Success && int.TryParse(woodMatch.Groups[1].Value, out int wood))
            {
                result.HolzGained = wood;
            }
        }
    }

    private string NormalizeReportText(string report)
    {
        // Normalize line endings
        report = report.Replace("\r\n", "\n").Replace("\r", "\n");
        
        // Remove leading/trailing whitespace
        report = report.Trim();
        
        return report;
    }

    private string ExtractSection(string text, string sectionStart, string? sectionEnd)
    {
        int startIndex = text.IndexOf(sectionStart, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
            return string.Empty;
        
        startIndex += sectionStart.Length;
        
        int endIndex;
        if (sectionEnd != null)
        {
            endIndex = text.IndexOf(sectionEnd, startIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex < 0)
                endIndex = text.Length;
        }
        else
        {
            endIndex = text.Length;
        }
        
        return text.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private void ValidateParsedData(ParsedFightReport result)
    {
        // Check if we have the minimum required data
        if (string.IsNullOrEmpty(result.TargetIsland))
        {
            result.ParseErrors.Add("Could not extract target island coordinates");
        }
        
        if (string.IsNullOrEmpty(result.AttackerName))
        {
            result.ParseErrors.Add("Could not extract attacker name");
        }
        
        if (result.TotalResourcesGained == null || result.TotalResourcesGained == 0)
        {
            result.ParseErrors.Add("Could not extract any resource information");
        }

        if (result.AttackTime == null)
        {
            result.ParseErrors.Add("Could not extract attack time");
        }
    }

    public async Task<List<FightReportEntity>> GetRecentReportsAsync(int count = 50)
    {
        try
        {
            var filter = TableClient.CreateQueryFilter($"PartitionKey eq {_userId}");
            var entities = await _tableClient.QueryAsync<FightReportEntity>(filter).ToListAsync();
            return entities.OrderByDescending(e => e.Timestamp).Take(count).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent reports");
            return new List<FightReportEntity>();
        }
    }
}

public class ParsedFightReport
{
    public string RawReport { get; set; } = null!;
    public string Language { get; set; } = null!;
    public string? AttackerName { get; set; }
    public string? AttackerIsland { get; set; }
    public string? DefenderName { get; set; }
    public string? TargetIsland { get; set; }
    public int? TotalResourcesGained { get; set; }
    public bool? AttackSuccessful { get; set; }
    public DateTime? AttackTime { get; set; }
    
    // German-specific resources
    public int? GoldGained { get; set; }
    public int? SteineGained { get; set; }
    public int? HolzGained { get; set; }
    
    // Units
    public Dictionary<string, int> AttackerUnits { get; set; } = new();
    public Dictionary<string, int> DefenderUnits { get; set; } = new();
    public Dictionary<string, int> AttackerLosses { get; set; } = new();
    public Dictionary<string, int> DefenderLosses { get; set; } = new();
    
    // Parsing metadata
    public List<string> ParseErrors { get; set; } = new();
}
