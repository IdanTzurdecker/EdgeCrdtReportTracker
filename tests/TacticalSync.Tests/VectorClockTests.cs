using TacticalSync.Models;
using Xunit;

namespace TacticalSync.Tests;

/// <summary>
/// Unit tests for VectorClock implementation covering:
/// - Increment operations
/// - Merge operations (max of each counter)
/// - Comparison logic (causal dominance, equality, concurrency)
/// - Clone operations
/// </summary>
public class VectorClockTests
{
    [Fact]
    public void Constructor_ShouldCreateEmptyVectorClock()
    {
        var vc = new VectorClock();

        Assert.NotNull(vc.Clocks);
        Assert.Empty(vc.Clocks);
    }

    [Fact]
    public void Increment_ShouldInitializeNodeToOne()
    {
        var vc = new VectorClock();

        vc.Increment("FOB_Alpha");

        Assert.Single(vc.Clocks);
        Assert.Equal(1, vc.Clocks["FOB_Alpha"]);
    }

    [Fact]
    public void Increment_ShouldIncrementExistingNode()
    {
        var vc = new VectorClock();
        vc.Increment("FOB_Alpha");

        vc.Increment("FOB_Alpha");
        vc.Increment("FOB_Alpha");

        Assert.Equal(3, vc.Clocks["FOB_Alpha"]);
    }
    
    [Fact]
    public void Merge_ShouldTakeMaximumOfEachCounter()
    {
        var vc1 = new VectorClock();
        vc1.Increment("FOB_Alpha");
        vc1.Increment("FOB_Alpha");
        vc1.Increment("FOB_Bravo");

        var vc2 = new VectorClock();
        vc2.Increment("FOB_Alpha");
        vc2.Increment("FOB_Bravo");
        vc2.Increment("FOB_Bravo");
        vc2.Increment("FOB_Charlie");

        vc1.Merge(vc2);

        Assert.Equal(2, vc1.Clocks["FOB_Alpha"]); // max(2, 1) = 2
        Assert.Equal(2, vc1.Clocks["FOB_Bravo"]); // max(1, 2) = 2
        Assert.Equal(1, vc1.Clocks["FOB_Charlie"]); // max(0, 1) = 1
    }

    [Fact]
    public void Merge_ShouldAddMissingNodes()
    {
        var vc1 = new VectorClock();
        vc1.Increment("FOB_Alpha");

        var vc2 = new VectorClock();
        vc2.Increment("FOB_Bravo");

        vc1.Merge(vc2);

        Assert.Equal(2, vc1.Clocks.Count);
        Assert.Equal(1, vc1.Clocks["FOB_Alpha"]);
        Assert.Equal(1, vc1.Clocks["FOB_Bravo"]);
    }

    [Fact]
    public void CompareTo_ShouldReturnZero_WhenClocksAreEqual()
    {
        var vc1 = new VectorClock();
        vc1.Increment("FOB_Alpha");
        vc1.Increment("FOB_Bravo");

        var vc2 = new VectorClock();
        vc2.Increment("FOB_Alpha");
        vc2.Increment("FOB_Bravo");

        var result = vc1.CompareTo(vc2); // 0 = equal

        Assert.Equal(0, result);
    }

    [Fact]
    public void CompareTo_ShouldReturnMinusOne_WhenThisCausallyPrecedesOther()
    {
        var vc1 = new VectorClock();
        vc1.Increment("FOB_Alpha");

        var vc2 = new VectorClock();
        vc2.Increment("FOB_Alpha");
        vc2.Increment("FOB_Alpha"); // vc2 has higher counter

        var result = vc1.CompareTo(vc2);

        Assert.Equal(-1, result); // vc1 < vc2
    }

    [Fact]
    public void CompareTo_ShouldReturnOne_WhenThisCausallyFollowsOther()
    {
        var vc1 = new VectorClock();
        vc1.Increment("FOB_Alpha");
        vc1.Increment("FOB_Alpha");
        vc1.Increment("FOB_Alpha");

        var vc2 = new VectorClock();
        vc2.Increment("FOB_Alpha");

        var result = vc1.CompareTo(vc2);

        Assert.Equal(1, result); // vc1 > vc2
    }

    [Fact]
    public void CompareTo_ShouldReturnZero_WhenClocksAreConcurrent()
    {
        var vc1 = new VectorClock();
        vc1.Increment("FOB_Alpha");
        vc1.Increment("FOB_Alpha"); // Alpha: 2, Bravo: 0

        var vc2 = new VectorClock();
        vc2.Increment("FOB_Bravo");
        vc2.Increment("FOB_Bravo"); // Alpha: 0, Bravo: 2

        var result = vc1.CompareTo(vc2);

        Assert.Equal(0, result); // Concurrent
    }

    [Fact]
    public void CompareTo_ShouldHandleMixedConcurrentScenario()
    {
        var vc1 = new VectorClock();
        vc1.Increment("FOB_Alpha");
        vc1.Increment("FOB_Alpha");
        vc1.Increment("FOB_Bravo");

        var vc2 = new VectorClock();
        vc2.Increment("FOB_Alpha");
        vc2.Increment("FOB_Bravo");
        vc2.Increment("FOB_Bravo");

        // vc1: {Alpha: 2, Bravo: 1}
        // vc2: {Alpha: 1, Bravo: 2}
        // Neither dominates -> concurrent

        var result = vc1.CompareTo(vc2);

        Assert.Equal(0, result); // Concurrent
    }

    [Fact]
    public void Clone_ShouldCreateIndependentCopy()
    {
        var original = new VectorClock();
        original.Increment("FOB_Alpha");
        original.Increment("FOB_Bravo");

        var clone = original.Clone();
        clone.Increment("FOB_Charlie"); // Modify clone

        Assert.Equal(2, original.Clocks.Count); // Original unchanged
        Assert.Equal(3, clone.Clocks.Count); // Clone modified
        Assert.False(original.Clocks.ContainsKey("FOB_Charlie"));
        Assert.True(clone.Clocks.ContainsKey("FOB_Charlie"));
    }
    
    [Theory]
    [InlineData(1, 0, 1)]  // vc1 > vc2
    [InlineData(0, 1, -1)] // vc1 < vc2
    [InlineData(1, 1, 0)]  // vc1 == vc2
    public void CompareTo_ShouldHandleSimpleScenarios(int count1, int count2, int expected)
    {
        var vc1 = new VectorClock();
        var vc2 = new VectorClock();

        for (int i = 0; i < count1; i++)
            vc1.Increment("FOB_Alpha");

        for (int i = 0; i < count2; i++)
            vc2.Increment("FOB_Alpha");

        var result = vc1.CompareTo(vc2);

        Assert.Equal(expected, result);
    }
}

