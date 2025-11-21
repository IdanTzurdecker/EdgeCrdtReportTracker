namespace TacticalSync.Models;

using System.Text.Json.Serialization;

public class IntelligenceReport
{
    // Should follow SALUTE format
    // sha-256 hash
    // is System.Text.Json.Serialization deterministic?
    
    /// <summary>
    /// Unique identifier for this report (UUID v4).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Vector clock tracking the causal history of this specific report.
    /// </summary>
    [JsonPropertyName("vectorClock")]
    public VectorClock VectorClock { get; set; }

    /// <summary>
    /// Estimated number of enemy combatants
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// Description of enemy activity
    /// </summary>
    [JsonPropertyName("activity")]
    public string Activity { get; set; }

    /// <summary>
    /// coordinates 
    /// Format: "lat,longitude"
    /// </summary>
    [JsonPropertyName("location")]
    public string Location { get; set; }

    /// <summary>
    /// Description of enemy unit type
    /// </summary>
    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    /// <summary>
    /// ISO 8601 timestamp 
    /// </summary>
    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    /// <summary>
    /// Equipment observed
    /// </summary>
    [JsonPropertyName("equipment")]
    public HashSet<string> Equipment { get; set; }
    
    /// <summary>
    /// Last modified timestamp (wall clock) for LWW tie-breaking.
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Node ID of the node that last modified this report.
    /// </summary>
    [JsonPropertyName("lastModifiedBy")]
    public string LastModifiedBy { get; set; }

    /// <summary>
    /// Hash of the latest audit entry associated with this report.
    /// </summary>
    [JsonPropertyName("auditHash")]
    public string AuditHash { get; set; }
    
    public IntelligenceReport()
    {
        Id = Guid.NewGuid().ToString();
        VectorClock = new VectorClock();
        Equipment = new HashSet<string>();
        Time = DateTime.UtcNow;
        LastModified = DateTime.UtcNow;
    }

    public IntelligenceReport Clone()
    {
        return new IntelligenceReport
        {
            Id = this.Id,
            VectorClock = this.VectorClock.Clone(),
            Size = this.Size,
            Activity = this.Activity,
            Location = this.Location,
            Unit = this.Unit,
            Time = this.Time,
            Equipment = new HashSet<string>(this.Equipment),
            LastModified = this.LastModified,
            LastModifiedBy = this.LastModifiedBy,
            AuditHash = this.AuditHash
        };
    }
}