namespace TacticalSync.Core
{
    /// <summary>
    /// Simulates network connectivity between nodes.
    /// Controls partitions (SATCOM outages) for testing offline-first behavior.
    /// </summary>
    public class NetworkController
    {
        private readonly HashSet<(string, string)> _disconnectedPairs;

        public NetworkController()
        {
            _disconnectedPairs = new HashSet<(string, string)>();
        }

        /// <summary>
        /// Disconnect two nodes
        /// Example : SATCOM outage
        /// </summary>
        public void Disconnect(string nodeA, string nodeB)
        {
            var pair1 = (nodeA, nodeB);
            var pair2 = (nodeB, nodeA);
            
            _disconnectedPairs.Add(pair1);
            _disconnectedPairs.Add(pair2);
            
            Console.WriteLine($"[NETWORK] Disconnected {nodeA} <-> {nodeB} (SATCOM OUTAGE)");
        }

        /// <summary>
        /// Reconnect two nodes (restore SATCOM link).
        /// </summary>
        public void Connect(string nodeA, string nodeB)
        {
            var pair1 = (nodeA, nodeB);
            var pair2 = (nodeB, nodeA);
            
            _disconnectedPairs.Remove(pair1);
            _disconnectedPairs.Remove(pair2);
            
            Console.WriteLine($"[NETWORK] Connected {nodeA} <-> {nodeB} (SATCOM RESTORED)");
        }

        /// <summary>
        /// Check if two nodes can communicate.
        /// Assuming each node is conected to the network controller
        /// </summary>
        public bool CanCommunicate(string nodeA, string nodeB)
        {
            return !_disconnectedPairs.Contains((nodeA, nodeB));
        }
        
        /// <summary>
        /// Simulate a complete network partition (all nodes disconnected).
        /// </summary>
        public void PartitionAll(params Node[] nodes)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                for (int j = i + 1; j < nodes.Length; j++)
                {
                    Disconnect(nodes[i].NodeId, nodes[j].NodeId);
                }
            }
        }

        /// <summary>
        /// Attempt synchronization between two nodes.
        /// Syncs unless disconnected.
        /// Returns null if nodes are disconnected.
        /// </summary>
        public SyncResult TrySync(Node nodeA, Node nodeB)
        {
            if (!CanCommunicate(nodeA.NodeId, nodeB.NodeId))
            {
                Console.WriteLine($"[NETWORK] Sync blocked: {nodeA.NodeId} <-> {nodeB.NodeId} (No connectivity)");
                return null;
            }

            var resultA = nodeA.SyncWith(nodeB);
            var resultB = nodeB.SyncWith(nodeA);

            // Combine results
            return new SyncResult
            {
                ReportsReceived = resultA.ReportsReceived + resultB.ReportsReceived,
                ReportsUpdated = resultA.ReportsUpdated + resultB.ReportsUpdated,
                ConflictsResolved = resultA.ConflictsResolved + resultB.ConflictsResolved
            };
        }
    }
}

