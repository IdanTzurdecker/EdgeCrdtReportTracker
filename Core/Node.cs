using TacticalSync.Models;

namespace TacticalSync.Core
{
    /// <summary>
    /// Represents a distributed node with
    /// local storage, sync logic, vector clock management, and audit trail
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Local data store - thread-safe dictionary of intelligence reports.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Node's vector clock tracking its logical time iteration
        /// </summary>
        private readonly Dictionary<string, IntelligenceReport> _localStore;

        /// <summary>
        /// Node's vector clock tracking its logical time iteration
        /// </summary>
        private VectorClock _nodeClock;


        /// <summary>
        /// Append-only audit log with cryptographic hash chain.
        /// </summary>
        private readonly List<AuditLog> _auditTrail;

        /// <summary>
        /// Lock for thread-safe operations.
        /// </summary>
        private readonly object _lock = new object();

        public Node(string nodeId)
        {
            NodeId = nodeId;
            _localStore = new Dictionary<string, IntelligenceReport>();
            _nodeClock = new VectorClock();
            _nodeClock.Increment(nodeId); // Initialize with 1
            _auditTrail = new List<AuditLog>();
        }

        /// <summary>
        /// Create a new intelligence report on this node based on SALUTE format.
        /// </summary>
        public IntelligenceReport CreateReport(string activity, int size, string location, string unit,
            params string[] equipment)
        {
            lock (_lock)
            {
                var report = new IntelligenceReport
                {
                    Activity = activity,
                    Size = size, // possibly create class for this with descriptive properties?
                    Location = location, // TODO- use Tuple coordinates 
                    Unit = unit,
                    Equipment = new HashSet<string>(equipment),
                    LastModifiedBy = NodeId,
                    LastModified = DateTime.UtcNow,
                    VectorClock = _nodeClock.Clone() // Clone the vector clock to avoid reference issues
                };

                _nodeClock.Increment(NodeId); // Increment the vector clock for the node
                report.VectorClock.Increment(NodeId); // Increment the vector clock for the node

                _localStore[report.Id] = report;

                AddAuditEntry("CREATE_REPORT", report.Id, "SUCCESS", $"Created report: {activity}");
                report.AuditHash = _auditTrail.Last().CurrentHash;

                Console.WriteLine($"[{NodeId}] Created report {report.Id}: {activity}");
                return report;
            }
        }

        public bool UpdateReport(string reportId, Action<IntelligenceReport> updateAction)
        {
            lock (_lock)
            {
                if (!_localStore.ContainsKey(reportId))
                {
                    Console.WriteLine($"[{NodeId}] Report {reportId} not found");
                    return false;
                }

                var report = _localStore[reportId];
                updateAction(report);

                report.LastModifiedBy = NodeId;
                report.LastModified = DateTime.UtcNow;
                _nodeClock.Increment(NodeId);
                report.VectorClock.Increment(NodeId);

                AddAuditEntry("UPDATE_REPORT", reportId, "SUCCESS", "Updated report");
                report.AuditHash = _auditTrail.Last().CurrentHash;

                Console.WriteLine($"[{NodeId}] Updated report {reportId}");
                return true;
            }
        }


        /// <summary>
        /// Get a report from local storage.
        /// </summary>
        public IntelligenceReport GetReport(string reportId)
        {
            lock (_lock) //lock to prevent corruption during concurrent access
            {
                return _localStore.ContainsKey(reportId) ? _localStore[reportId].Clone() : null;
            }
        }

        /// <summary>
        /// Get all reports from local storage.
        /// </summary>
        public List<IntelligenceReport> GetAllReports()
        {
            lock (_lock)
            {
                return _localStore.Values.Select(r => r.Clone()).ToList();
            }
        }
        
         /// <summary>
        /// Synchronize with another node (push-pull delta sync).
        /// </summary>
        public SyncResult SyncWith(Node otherNode)
        {
            lock (_lock)
            {
                var result = new SyncResult();
                
                Console.WriteLine($"\n[SYNC] {NodeId} <-> {otherNode.NodeId}");
                
                // Phase 1: Knowledge Exchange (compare what each node has)
                var localReportIds = new HashSet<string>(_localStore.Keys);
                var remoteReportIds = new HashSet<string>(otherNode._localStore.Keys);
                
                // Phase 2: Determine Delta (what needs to be sent/received)
                // Ven diagram using set operations
                var toReceive = remoteReportIds.Except(localReportIds).ToList();
                //var toSend = localReportIds.Except(remoteReportIds).ToList();
                var toMerge = localReportIds.Intersect(remoteReportIds).ToList();
                
                // Phase 3: Receive new reports from remote
                foreach (var reportId in toReceive)
                {
                    var remoteReport = otherNode.GetReport(reportId);
                    _localStore[reportId] = remoteReport;
                    _nodeClock.Merge(remoteReport.VectorClock);
                    result.ReportsReceived++;
                    
                    AddAuditEntry("SYNC", reportId, "SUCCESS", $"Received new report from {otherNode.NodeId}");
                }
                
                // Phase 4: Merge and Reconcile conflicting reports
                foreach (var reportId in toMerge)
                {
                    var localReport = _localStore[reportId];
                    var remoteReport = otherNode.GetReport(reportId);
                    
                    // Check if they're already identical
                    if (localReport.VectorClock.CompareTo(remoteReport.VectorClock) == 0 &&
                        localReport.LastModified == remoteReport.LastModified)
                    {
                        continue;
                    }
                    
                    // Resolve conflict
                    var resolved = ConflictResolver.Resolve(localReport, remoteReport);
                    
                    if (resolved.Id != localReport.Id)
                    {
                        throw new InvalidOperationException("Conflict resolution did not resolve correctly. Mismatched IDs.");
                    }
                    
                    bool wasConflict = localReport.VectorClock.CompareTo(remoteReport.VectorClock) == 0;
                    
                    _localStore[reportId] = resolved;
                    _nodeClock.Merge(resolved.VectorClock);
                    
                    if (wasConflict)
                    {
                        result.ConflictsResolved++;
                        AddAuditEntry("SYNC", reportId, "CONFLICT_RESOLVED", 
                            $"Resolved conflict with {otherNode.NodeId}");
                    }
                    else
                    {
                        result.ReportsUpdated++;
                        AddAuditEntry("SYNC", reportId, "SUCCESS", 
                            $"Updated from {otherNode.NodeId}");
                    }
                }
                
                Console.WriteLine($"[SYNC] {NodeId} <- {otherNode.NodeId}: " +
                                $"Received={result.ReportsReceived}, " +
                                $"Updated={result.ReportsUpdated}, " +
                                $"Conflicts={result.ConflictsResolved}");
                
                return result;
            }
        }

        /// <summary>
        /// Add an entry to the audit trail with hash chain integrity.
        /// </summary>
        private void AddAuditEntry(string action, string resourceId, string outcome, string details)
        {
            var entry = new AuditLog
            {
                ActorId = $"node_{NodeId}",
                Action = action,
                ResourceId = resourceId,
                Outcome = outcome,
                Details = details,
                PreviousHash =
                    _auditTrail.Count > 0 ? _auditTrail.Last().CurrentHash : "START" // Genesis hash for first entry
            };

            entry.CalculateHash();
            _auditTrail.Add(entry);
        }

        /// <summary>
        /// Verify the integrity of the entire audit chain.
        /// </summary>
        public bool VerifyAuditChain()
        {
            lock (_lock)
            {
                for (int i = 0; i < _auditTrail.Count; i++)
                {
                    var entry = _auditTrail[i];

                    // Verify hash
                    if (!entry.VerifyHash())
                    {
                        Console.WriteLine($"[{NodeId}] Audit chain broken at index {i}: Hash mismatch");
                        return false;
                    }

                    // Verify chain link
                    if (i > 0)
                    {
                        var prevEntry = _auditTrail[i - 1];
                        if (entry.PreviousHash != prevEntry.CurrentHash)
                        {
                            Console.WriteLine($"[{NodeId}] Audit chain broken at index {i}: Chain link mismatch");
                            return false;
                        }
                    }
                }

                return true;
            }
        }
        
        /// <summary>
        /// Get audit trail for inspection.
        /// </summary>
        public List<AuditLog> GetAuditTrail()
        {
            lock (_lock)
            {
                return new List<AuditLog>(_auditTrail);
            }
        }

        /// <summary>
        /// Print current state of the node for debugging.
        /// </summary>
        public void PrintState()
        {
            lock (_lock)
            {
                Console.WriteLine($"\n=== Node {NodeId} State ===");
                Console.WriteLine($"Vector Clock: {_nodeClock}");
                Console.WriteLine($"Reports: {_localStore.Count}");
                    
                foreach (var report in _localStore.Values.OrderBy(r => r.Id))
                {
                    Console.WriteLine($"  [{report.Id.Substring(0, 8)}] {report.Activity} " +
                                      $"(Size: {report.Size}, Location: {report.Location}) " +
                                      $"[Equipment: {string.Join(", ", report.Equipment)}] " +
                                      $"VC: {report.VectorClock} " +
                                      $"Modified: {report.LastModified:HH:mm:ss} by {report.LastModifiedBy}");
                }
                    
                Console.WriteLine($"Audit Trail: {_auditTrail.Count} entries");
                Console.WriteLine($"Audit Chain Valid: {VerifyAuditChain()}");
                Console.WriteLine("===================\n");
            }
        }
    }
    
    /// <summary>
    /// Result of a synchronization operation.
    /// </summary>
    public class SyncResult
    {
        public int ReportsReceived { get; set; }
        public int ReportsUpdated { get; set; }
        public int ConflictsResolved { get; set; }
    }
}