using Microsoft.AspNetCore.Mvc;
using TacticalSync.Core;
using TacticalSync.Models;
using TacticalSync.Server.Models;

namespace TacticalSync.Server.Controllers;

/// <summary>
/// REST API controller 
/// Acts as the "Big Peer" server node 
/// </summary>
[ApiController]
// For simplicity, i'm using a single controller for all sync operations. Not a good practice for a production application.
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    /// <summary>
    /// Server-side node that stores all synced reports (Big Peer)
    /// </summary>
    private static readonly Node _serverNode = new Node("BigPeer_Server");
    private static readonly object _syncLock = new object();

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            NodeId = _serverNode.NodeId,
            ReportCount = _serverNode.GetAllReports().Count,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get all reports from server
    /// </summary>
    [HttpGet("reports")]
    public ActionResult<List<IntelligenceReport>> GetReports()
    {
        Console.WriteLine($"[SERVER] GET /reports - Returning {_serverNode.GetAllReports().Count} reports");
        return Ok(_serverNode.GetAllReports());
    }

    /// <summary>
    /// Get specific report by ID
    /// </summary>
    [HttpGet("reports/{id}")]
    public ActionResult<IntelligenceReport> GetReport(string id)
    {
        var report = _serverNode.GetReport(id);
        if (report == null)
        {
            Console.WriteLine($"[SERVER] GET /reports/{id} - Not found");
            return NotFound(new { Error = "Report not found", Id = id });
        }

        Console.WriteLine($"[SERVER] GET /reports/{id} - Found: {report.Activity}");
        return Ok(report);
    }

    /// <summary>
    /// Push reports from client to server (client sends their reports)
    /// </summary>
    [HttpPost("push")]
    public ActionResult<SyncResponse> PushReports([FromBody] SyncRequest request)
    {
        Console.WriteLine($"\n[SERVER] POST /push from {request.NodeId} with {request.Reports.Count} reports");

        lock (_syncLock)
        {
            var response = new SyncResponse { Success = true };

            foreach (var clientReport in request.Reports)
            {
                var serverReport = _serverNode.GetReport(clientReport.Id);

                if (serverReport == null)
                {
                    // New report - store it directly
                    // We need to add it to the server's store
                    InternalAddReport(clientReport);
                    response.ReportsReceived++;
                    Console.WriteLine($"[SERVER]   + New report: {clientReport.Id.Substring(0, 8)}... - {clientReport.Activity}");
                }
                else
                {
                    // Existing report - resolve conflict
                    var resolved = ConflictResolver.Resolve(serverReport, clientReport);

                    if (resolved.VectorClock.CompareTo(serverReport.VectorClock) != 0)
                    {
                        // Update with resolved version
                        _serverNode.UpdateReport(resolved.Id, r =>
                        {
                            r.Activity = resolved.Activity;
                            r.Size = resolved.Size;
                            r.Location = resolved.Location;
                            r.Unit = resolved.Unit;
                            r.Equipment = new HashSet<string>(resolved.Equipment);
                            r.VectorClock = resolved.VectorClock.Clone();
                            r.LastModified = resolved.LastModified;
                            r.LastModifiedBy = resolved.LastModifiedBy;
                        });
                        response.ConflictsResolved++;
                        Console.WriteLine($"[SERVER]   ~ Conflict resolved: {clientReport.Id.Substring(0, 8)}...");
                    }
                    else
                    {
                        response.ReportsUpdated++;
                    }
                }
            }

            response.ServerReports = _serverNode.GetAllReports();
            Console.WriteLine($"[SERVER] Push complete: Received={response.ReportsReceived}, Conflicts={response.ConflictsResolved}");
            return Ok(response);
        }
    }

    /// <summary>
    /// Pull reports from server (server sends reports client doesn't have)
    /// </summary>
    [HttpPost("pull")]
    public ActionResult<SyncResponse> PullReports([FromBody] PullRequest request)
    {
        Console.WriteLine($"\n[SERVER] POST /pull from {request.NodeId}");

        var allReports = _serverNode.GetAllReports();
        var clientKnownIds = request.KnownReportIds ?? new List<string>();

        // Send only reports the client doesn't have
        var newReports = allReports
            .Where(r => !clientKnownIds.Contains(r.Id))
            .ToList();

        Console.WriteLine($"[SERVER] Sending {newReports.Count} new reports to {request.NodeId}");

        return Ok(new SyncResponse
        {
            Success = true,
            ServerReports = newReports
        });
    }

    /// <summary>
    /// Full bidirectional sync - push client reports and return all server reports
    /// </summary>
    [HttpPost("sync")]
    public ActionResult<SyncResponse> Sync([FromBody] SyncRequest request)
    {
        Console.WriteLine($"\n[SERVER] POST /sync from {request.NodeId}");
        Console.WriteLine($"[SERVER] Received {request.Reports.Count} reports from client");

        lock (_syncLock)
        {
            var response = new SyncResponse { Success = true };

            // Process incoming reports from client
            foreach (var clientReport in request.Reports)
            {
                var serverReport = _serverNode.GetReport(clientReport.Id);

                if (serverReport == null)
                {
                    InternalAddReport(clientReport);
                    response.ReportsReceived++;
                    Console.WriteLine($"[SERVER]   + Received: {clientReport.Id.Substring(0, 8)}... - {clientReport.Activity}");
                }
                else
                {
                    var resolved = ConflictResolver.Resolve(serverReport, clientReport);
                    
                    // Check if we need to update
                    bool needsUpdate = resolved.VectorClock.CompareTo(serverReport.VectorClock) != 0 ||
                                       resolved.Activity != serverReport.Activity ||
                                       resolved.Size != serverReport.Size;

                    if (needsUpdate)
                    {
                        _serverNode.UpdateReport(resolved.Id, r =>
                        {
                            r.Activity = resolved.Activity;
                            r.Size = resolved.Size;
                            r.Location = resolved.Location;
                            r.Unit = resolved.Unit;
                            r.Equipment = new HashSet<string>(resolved.Equipment);
                            r.VectorClock = resolved.VectorClock.Clone();
                            r.LastModified = resolved.LastModified;
                            r.LastModifiedBy = resolved.LastModifiedBy;
                        });
                        response.ConflictsResolved++;
                        Console.WriteLine($"[SERVER]   ~ Merged: {clientReport.Id.Substring(0, 8)}...");
                    }
                }
            }

            // Return all server reports for client to reconcile
            response.ServerReports = _serverNode.GetAllReports();
            Console.WriteLine($"[SERVER] Sync complete: Received={response.ReportsReceived}, Conflicts={response.ConflictsResolved}, Returning={response.ServerReports.Count} reports");
            
            return Ok(response);
        }
    }

    /// <summary>
    /// Get server state for debugging
    /// </summary>
    [HttpGet("state")]
    public ActionResult<object> GetState()
    {
        var reports = _serverNode.GetAllReports();
        var auditTrail = _serverNode.GetAuditTrail();

        return Ok(new
        {
            NodeId = _serverNode.NodeId,
            ReportCount = reports.Count,
            Reports = reports.Select(r => new
            {
                r.Id,
                r.Activity,
                r.Size,
                r.Location,
                Equipment = string.Join(", ", r.Equipment),
                VectorClock = r.VectorClock.ToString(),
                r.LastModifiedBy,
                r.LastModified
            }),
            AuditTrailCount = auditTrail.Count,
            AuditChainValid = _serverNode.VerifyAuditChain()
        });
    }

    /// <summary>
    /// Internal helper to add a report directly (bypassing CreateReport to preserve ID)
    /// </summary>
    private void InternalAddReport(IntelligenceReport report)
    {
        // Create a new report on the server with the same data
        var serverReport = _serverNode.CreateReport(
            report.Activity,
            report.Size,
            report.Location,
            report.Unit,
            report.Equipment.ToArray()
        );

        // Now update it to match the incoming report's metadata
        _serverNode.UpdateReport(serverReport.Id, r =>
        {
            // We can't change the ID, but we update everything else
            r.VectorClock = report.VectorClock.Clone();
            r.LastModified = report.LastModified;
            r.LastModifiedBy = report.LastModifiedBy;
        });
    }
}

