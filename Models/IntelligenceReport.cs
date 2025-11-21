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
    /// Estimated number of enemy combatants (LWW-Register).
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// Description of enemy activity, e.g., "Digging in", "Moving north" (LWW-Register).
    /// </summary>
    [JsonPropertyName("activity")]
    public string Activity { get; set; }

    /// <summary>
    /// GeoJSON coordinates of enemy position (LWW-Register).
    /// Format: "latitude,longitude"
    /// </summary>
    [JsonPropertyName("location")]
    public string Location { get; set; }

    /// <summary>
    /// Identification of enemy unit, e.g., "Republican Guard" (LWW-Register).
    /// </summary>
    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    /// <summary>
    /// ISO 8601 timestamp when the event was observed (Immutable).
    /// </summary>
    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    /// <summary>
    /// Equipment observed (G-Set - Grow-Only Set).
    /// Multiple analysts might spot different weapons; union provides complete picture.
    /// </summary>
    [JsonPropertyName("equipment")]
    public HashSet<string> Equipment { get; set; }

    /// <summary>
    /// Classification level for access control.
    /// </summary>
    [JsonPropertyName("classificationLevel")]
    public string ClassificationLevel { get; set; }

    /// <summary>
    /// Tombstone flag for logical deletion in CRDT systems.
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }

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
}