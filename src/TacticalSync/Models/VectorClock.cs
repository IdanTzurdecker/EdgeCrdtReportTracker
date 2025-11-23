using System.Text.Json.Serialization;

namespace TacticalSync.Models;

public class VectorClock
{
    // <summary>
    // vector clock for tracking causal relationships
    // (nodeId -> counter)
    // </summary>
    [JsonPropertyName("clocks")]
    public Dictionary<string, int> Clocks { get; set; }

    public VectorClock()
    {
        Clocks = new Dictionary<string, int>();
    }

    public VectorClock(Dictionary<string, int> clocks)
    {
        Clocks = new Dictionary<string, int>(clocks);
    }

    /// <summary>
    /// Increment the counter for the inputted node.
    /// </summary>
    public void Increment(string nodeId)
    {
        if (!Clocks.ContainsKey(nodeId))
        {
            Clocks[nodeId] = 0;
        }

        Clocks[nodeId]++;
    }

    /// <summary>
    /// Merge this vector clock with another (take max of each counter).
    /// </summary>
    public void Merge(VectorClock other)
    {
        foreach (var kvp in other.Clocks)
        {
            if (!Clocks.ContainsKey(kvp.Key))
            {
                Clocks[kvp.Key] = kvp.Value;
            }
            else
            {
                Clocks[kvp.Key] = Math.Max(Clocks[kvp.Key], kvp.Value);
            }
        }
    }

    /// <summary>
    /// Compare two vector clocks to determine causal relationship.
    /// Returns: Int:
    ///           -1 if this < other (this causally precedes other)
    ///           0 if concurrent (neither causally precedes the other)
    ///           1 if this > other (this causally follows other)
    /// </summary>
    ///
    /// set notation All V_a[i] >= V_b[i] for all i, and there exists some index j where V_a[j] > V_b[j] → Accept V_a as winnder
    /// 
    public int CompareTo(VectorClock other)
    {
        bool thisVectorLessOrEqual = true;
        bool otherLessOrEqual = true;

        var allKeys = new HashSet<string>(Clocks.Keys);
        allKeys.UnionWith(other.Clocks.Keys); // [A, B, C] union [B, C, D] = [A, B, C, D]

        foreach (var key in allKeys)
        {
            int thisValue = Clocks.ContainsKey(key) ? Clocks[key] : 0;
            int otherValue = other.Clocks.ContainsKey(key) ? other.Clocks[key] : 0;

            if (thisValue > otherValue)
            {
                thisVectorLessOrEqual = false; // this is NOT <= other
            }

            if (otherValue > thisValue)
            {
                otherLessOrEqual = false; // other is NOT <= this
            }
        }

        if (thisVectorLessOrEqual && otherLessOrEqual)
        {
            return 0; // Equal
        }

        if (thisVectorLessOrEqual)
        {
            return -1; // This vectorClock is before the other
        }

        if (otherLessOrEqual)
        {
            return 1; // This causally follows after other
        }

        return 0; // concurrent
        
        // If neither is less than nor equal to the other, they are concurrent`
    }

    public VectorClock Clone()
    {
        return new VectorClock(new Dictionary<string, int>(Clocks));
    }
    
    public override string ToString()
    {
        var items = Clocks.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}:{kvp.Value}");
        return $"{{{string.Join(", ", items)}}}";
    }
}