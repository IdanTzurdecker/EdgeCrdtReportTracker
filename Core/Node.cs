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
        private readonly Dictionary<string, string> _localStore;

        private VectorClock _vectorClock;

        private readonly List<AuditLog> _auditTrail;
        // datastruck <auditModel>
        /// <summary>
        /// Lock for thread-safe operations.
        /// </summary>
        private readonly object _lock = new object();

        public Node(string nodeId)
        {
            NodeId = nodeId;
            _localStore = new Dictionary<string, string>();
            _vectorClock = new VectorClock();
            _auditTrail = new List<AuditLog>();
        }
    }
}