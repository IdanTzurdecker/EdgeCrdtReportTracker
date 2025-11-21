using System;
using System.Linq;
using TacticalSync.Models;

namespace TacticalSync.Core
{
    /// <summary>
    ///  hybrid conflict resolution 
    /// "Causal Dominance Check" using Vector Clocks
    /// Concurrency Detection
    /// Phase 3: Deterministic LWW (Last-Write-Wins) with tie-breaking
    /// </summary>
    public static class ConflictResolver
    {
        /// <summary>
        /// Resolve conflict between local and remote intelligence reports.
        /// Returns the winning report (or merged report).
        /// </summary>
        public static IntelligenceReport Resolve(IntelligenceReport local, IntelligenceReport remote)
        {
            if (local == null && remote == null)
            {
                return null;
            }
            if (local == null)
            {
                return remote.Clone();
            }
            if (remote == null)
            {
                return local.Clone();
            }

            // Must have the same ID to be conflicting versions
            if (local.Id != remote.Id)
            {
                throw new ArgumentException("Cannot resolve reports with different IDs");
            }

            // Step 1: Causal Dominance Check using Vector Clocks
            int comparison = local.VectorClock.CompareTo(remote.VectorClock);

            if (comparison == 1)
            {
                // Local causally follows remote -> keep local
                return local.Clone();
            }
            else if (comparison == -1)
            {
                // Remote causally follows local -> accept remote
                return remote.Clone();
            }

            // Step 2: Concurrency Detected - neither causally precedes the other
            // Step 3: Deterministic Resolution using Last-Write-Wins

            // latest timestamp win
            if (remote.LastModified > local.LastModified)
            {
                return MergeEquipment(remote, local); // Remote wins, but merge equipment
            }
            else if (local.LastModified > remote.LastModified)
            {
                return MergeEquipment(local, remote); // Local wins, but merge equipment
            }

            // if by chance the timestamps are the same, (alphabetically higher wins). nodes cant have identitcal names
            int nodeComparison = string.Compare(remote.LastModifiedBy, local.LastModifiedBy, StringComparison.Ordinal);
            if (nodeComparison > 0)
            {
                return MergeEquipment(remote, local);
            }
            else
            {
                return MergeEquipment(local, remote);
            }
        }

        /// <summary>
        /// Merge equipment sets from both reports (G-Set union).
        /// The winner's data is used, but equipment from both is preserved.
        /// </summary>
        private static IntelligenceReport MergeEquipment(IntelligenceReport winner, IntelligenceReport loser)
        {
            var merged = winner.Clone();
            
            // Union of equipment sets (Grow-Only Set)
            foreach (var equipment in loser.Equipment)
            {
                merged.Equipment.Add(equipment);
            }

            // Merge vector clocks to reflect both histories
            merged.VectorClock.Merge(loser.VectorClock);

            return merged;
        }

        /// <summary>
        /// Merge two reports into a new report, preserving all intelligence.
        /// Used for three-way merge scenarios.
        /// </summary>
        public static IntelligenceReport ThreeWayMerge(IntelligenceReport local, IntelligenceReport remote, IntelligenceReport common)
        {
            // First resolve local vs remote
            var resolved = Resolve(local, remote);

            // Then merge with common ancestor if available
            if (common != null)
            {
                resolved.VectorClock.Merge(common.VectorClock);
            }

            return resolved;
        }
    }
}

