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
        /// Node's vector clock tracking its logical time.
        /// </summary>
        private readonly Dictionary<string, IntelligenceReport> _localStore;

        /// <summary>
        /// Node's vector clock tracking its logical time.
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
            _auditTrail = new List<AuditLog>();
        }
        
        /// <summary>
        /// Create a new intelligence report on this node based on SALUTE format.
        /// </summary>
        public IntelligenceReport CreateReport(string activity, int size, string location, string unit, params string[] equipment)
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
                return report;
            }
        }
    }
}