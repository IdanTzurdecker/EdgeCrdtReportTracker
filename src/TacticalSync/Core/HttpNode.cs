using System.Net.Http.Json;
using System.Text.Json;
using TacticalSync.Models;

namespace TacticalSync.Core;

/// <summary>
/// Node with HTTP synchronization capabilities.
/// Can sync with a remote server over HTTP REST API.
/// based off node
/// </summary>
public class HttpNode : Node
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpNode(string nodeId, string serverUrl) : base(nodeId)
    {
        _serverUrl = serverUrl.TrimEnd('/');
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_serverUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Keep PascalCase
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Full bidirectional sync with remote HTTP server.
    /// Pushes local reports and receives server reports.
    /// </summary>
    public async Task<SyncResult> SyncWithServerAsync()
    {
        var result = new SyncResult();

        try
        {
            Console.WriteLine($"\n[{NodeId}] Starting HTTP sync with {_serverUrl}...");

            // Prepare request with all local reports
            var request = new
            {
                NodeId = NodeId,
                Reports = GetAllReports()
            };

            Console.WriteLine($"[{NodeId}] Sending {request.Reports.Count} reports to server");

            // Send sync request
            var response = await _httpClient.PostAsJsonAsync("/api/sync/sync", request, _jsonOptions);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[{NodeId}] HTTP sync failed: {response.StatusCode} - {error}");
                return result;
            }

            var syncResponse = await response.Content.ReadFromJsonAsync<HttpSyncResponse>(_jsonOptions);

            if (syncResponse?.Success == true)
            {
                Console.WriteLine($"[{NodeId}] Received {syncResponse.ServerReports.Count} reports from server");

                // Process reports from server
                foreach (var serverReport in syncResponse.ServerReports)
                {
                    var localReport = GetReport(serverReport.Id);

                    if (localReport == null)
                    {
                        // New report from server - create locally
                        var newReport = CreateReport(
                            serverReport.Activity,
                            serverReport.Size,
                            serverReport.Location,
                            serverReport.Unit,
                            serverReport.Equipment.ToArray()
                        );
                        
                        // Update vector clock to match server's
                        UpdateReport(newReport.Id, r =>
                        {
                            r.VectorClock = serverReport.VectorClock.Clone();
                            r.LastModified = serverReport.LastModified;
                            r.LastModifiedBy = serverReport.LastModifiedBy;
                        });
                        
                        result.ReportsReceived++;
                        Console.WriteLine($"[{NodeId}]   + Received: {serverReport.Id.Substring(0, 8)}... - {serverReport.Activity}");
                    }
                    else
                    {
                        // Existing report - resolve conflict
                        var resolved = ConflictResolver.Resolve(localReport, serverReport);
                        
                        bool hasChanges = resolved.Activity != localReport.Activity ||
                                          resolved.Size != localReport.Size ||
                                          !resolved.Equipment.SetEquals(localReport.Equipment);

                        if (hasChanges)
                        {
                            UpdateReport(resolved.Id, r =>
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
                            result.ConflictsResolved++;
                            Console.WriteLine($"[{NodeId}]   ~ Merged: {serverReport.Id.Substring(0, 8)}...");
                        }
                    }
                }

                Console.WriteLine($"[{NodeId}] HTTP Sync complete: Received={result.ReportsReceived}, Conflicts={result.ConflictsResolved}");
            }
            else
            {
                Console.WriteLine($"[{NodeId}] Server returned unsuccessful response");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[{NodeId}] HTTP Sync failed (network error): {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine($"[{NodeId}] HTTP Sync timed out");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{NodeId}] HTTP Sync error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Push local reports to server only (one-way)
    /// </summary>
    public async Task<bool> PushToServerAsync()
    {
        try
        {
            Console.WriteLine($"[{NodeId}] Pushing {GetAllReports().Count} reports to server...");

            var request = new
            {
                NodeId = NodeId,
                Reports = GetAllReports()
            };

            var response = await _httpClient.PostAsJsonAsync("/api/sync/push", request, _jsonOptions);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[{NodeId}] Push successful");
                return true;
            }
            
            Console.WriteLine($"[{NodeId}] Push failed: {response.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{NodeId}] Push error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Pull reports from server only (one-way)
    /// </summary>
    public async Task<List<IntelligenceReport>> PullFromServerAsync()
    {
        try
        {
            var knownIds = GetAllReports().Select(r => r.Id).ToList();
            
            Console.WriteLine($"[{NodeId}] Pulling from server (have {knownIds.Count} reports locally)...");

            var request = new
            {
                NodeId = NodeId,
                KnownReportIds = knownIds
            };

            var response = await _httpClient.PostAsJsonAsync("/api/sync/pull", request, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[{NodeId}] Pull failed: {response.StatusCode}");
                return new List<IntelligenceReport>();
            }

            var syncResponse = await response.Content.ReadFromJsonAsync<HttpSyncResponse>(_jsonOptions);
            var newReports = syncResponse?.ServerReports ?? new List<IntelligenceReport>();
            
            Console.WriteLine($"[{NodeId}] Pulled {newReports.Count} new reports from server");
            return newReports;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{NodeId}] Pull error: {ex.Message}");
            return new List<IntelligenceReport>();
        }
    }

    /// <summary>
    /// Check server health/connectivity
    /// </summary>
    public async Task<bool> CheckServerHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/sync/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Dispose HTTP client
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

/// <summary>
/// Response DTO from HTTP sync operations
/// </summary>
public class HttpSyncResponse
{
    public bool Success { get; set; }
    public List<IntelligenceReport> ServerReports { get; set; } = new();
    public int ReportsReceived { get; set; }
    public int ReportsUpdated { get; set; }
    public int ConflictsResolved { get; set; }
    public string? ErrorMessage { get; set; }
}

