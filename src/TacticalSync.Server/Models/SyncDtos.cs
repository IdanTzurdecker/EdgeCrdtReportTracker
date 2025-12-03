using TacticalSync.Models;

namespace TacticalSync.Server.Models;

/// <summary>
/// Request payload for sync operations
/// </summary>
public class SyncRequest
{
    public string NodeId { get; set; } = "";
    public List<IntelligenceReport> Reports { get; set; } = new();
}

/// <summary>
/// Request payload for pull operations
/// </summary>
public class PullRequest
{
    public string NodeId { get; set; } = "";
    public List<string>? KnownReportIds { get; set; }
}

/// <summary>
/// Response payload for sync operations
/// </summary>
public class SyncResponse
{
    public bool Success { get; set; }
    public List<IntelligenceReport> ServerReports { get; set; } = new();
    public int ReportsReceived { get; set; }
    public int ReportsUpdated { get; set; }
    public int ConflictsResolved { get; set; }
    public string? ErrorMessage { get; set; }
}

