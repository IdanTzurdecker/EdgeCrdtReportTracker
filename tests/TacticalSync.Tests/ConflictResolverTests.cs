using TacticalSync.Core;
using TacticalSync.Models;
using Xunit;

namespace TacticalSync.Tests;

/// <summary>
/// Unit tests for ConflictResolver covering:
/// - Causal dominance detection via Vector Clocks
/// - Concurrency detection
/// - Last-Write-Wins (LWW) tie-breaking
/// </summary>
public class ConflictResolverTests
{
    [Fact]
    public void Resolve_ShouldReturnRemote_WhenLocalIsNull()
    {
        var remote = CreateReport("FOB_Alpha", "Enemy spotted");

        var result = ConflictResolver.Resolve(null, remote);

        Assert.NotNull(result);
        Assert.Equal(remote.Id, result.Id);
        Assert.Equal("Enemy spotted", result.Activity);
    }

    [Fact]
    public void Resolve_ShouldReturnLocal_WhenRemoteIsNull()
    {
        var local = CreateReport("FOB_Alpha", "Enemy spotted");

        var result = ConflictResolver.Resolve(local, null);

        Assert.NotNull(result);
        Assert.Equal(local.Id, result.Id);
        Assert.Equal("Enemy spotted", result.Activity);
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenBothAreNull()
    {
        var result = ConflictResolver.Resolve(null, null);

        Assert.Null(result);
    }


    [Fact]
    public void Resolve_ShouldKeepLocal_WhenLocalCausallyFollowsRemote()
    {
        var reportId = Guid.NewGuid().ToString();
        
        var local = new IntelligenceReport
        {
            Id = reportId,
            Activity = "Enemy advancing",
            Size = 25,
            VectorClock = new VectorClock()
        };
        local.VectorClock.Increment("FOB_Alpha");
        local.VectorClock.Increment("FOB_Alpha"); // Version 2

        var remote = new IntelligenceReport
        {
            Id = reportId,
            Activity = "Enemy stopped",
            Size = 20,
            VectorClock = new VectorClock()
        };
        remote.VectorClock.Increment("FOB_Alpha"); // Version 1

        var result = ConflictResolver.Resolve(local, remote);

        Assert.Equal("Enemy advancing", result.Activity);
        Assert.Equal(25, result.Size);
    }

    [Fact]
    public void Resolve_ShouldAcceptRemote_WhenRemoteCausallyFollowsLocal()
    {
        var reportId = Guid.NewGuid().ToString();
        
        var local = new IntelligenceReport
        {
            Id = reportId,
            Activity = "Enemy stopped",
            Size = 20,
            VectorClock = new VectorClock(),
            LastModified = DateTime.UtcNow.AddMinutes(-5)
        };
        local.VectorClock.Increment("FOB_Alpha"); // Version 1

        var remote = new IntelligenceReport
        {
            Id = reportId,
            Activity = "Enemy advancing",
            Size = 25,
            VectorClock = new VectorClock(),
            LastModified = DateTime.UtcNow
        };
        remote.VectorClock.Increment("FOB_Alpha");
        remote.VectorClock.Increment("FOB_Alpha"); // Version 2

        // Act
        var result = ConflictResolver.Resolve(local, remote);

        // Assert - Remote should win
        Assert.Equal("Enemy advancing", result.Activity);
        Assert.Equal(25, result.Size);
    }

    [Fact]
    public void Resolve_ShouldUseLWW_WhenClocksAreConcurrent()
    {
        var reportId = Guid.NewGuid().ToString();
        
        var local = new IntelligenceReport
        {
            Id = reportId,
            Activity = "Local update",
            Size = 20,
            VectorClock = new VectorClock(),
            LastModified = DateTime.UtcNow.AddMinutes(-5),
            LastModifiedBy = "FOB_Alpha"
        };
        local.VectorClock.Increment("FOB_Alpha");

        var remote = new IntelligenceReport
        {
            Id = reportId,
            Activity = "Remote update",
            Size = 25,
            VectorClock = new VectorClock(),
            LastModified = DateTime.UtcNow, // Later timestamp
            LastModifiedBy = "FOB_Bravo"
        };
        remote.VectorClock.Increment("FOB_Bravo");

        // Act
        var result = ConflictResolver.Resolve(local, remote);

        Assert.Equal("Remote update", result.Activity);
        Assert.Equal(25, result.Size);
    }

    [Fact]
    public void Resolve_ShouldUseNodeIdTieBreaker_WhenTimestampsEqual()
    {
        var reportId = Guid.NewGuid().ToString();
        var timestamp = DateTime.UtcNow;
        
        var local = new IntelligenceReport
        {
            Id = reportId,
            Activity = "Local update",
            VectorClock = new VectorClock(),
            LastModified = timestamp,
            LastModifiedBy = "FOB_Alpha"
        };
        local.VectorClock.Increment("FOB_Alpha");

        var remote = new IntelligenceReport
        {
            Id = reportId,
            Activity = "Remote update",
            VectorClock = new VectorClock(),
            LastModified = timestamp,
            LastModifiedBy = "FOB_Bravo" 
        };
        remote.VectorClock.Increment("FOB_Bravo");

        var result = ConflictResolver.Resolve(local, remote);

        Assert.Equal("Remote update", result.Activity);
        Assert.Equal("FOB_Bravo", result.LastModifiedBy);
    }

    [Fact]
    public void Resolve_ShouldMergeEquipmentSets()
    {
        var reportId = Guid.NewGuid().ToString();
        
        var local = new IntelligenceReport
        {
            Id = reportId,
            Activity = "Local observation",
            Equipment = new HashSet<string> { "Tank", "APC" },
            VectorClock = new VectorClock(),
            LastModified = DateTime.UtcNow.AddMinutes(-5),
            LastModifiedBy = "FOB_Alpha"
        };
        local.VectorClock.Increment("FOB_Alpha");

        var remote = new IntelligenceReport
        {
            Id = reportId,
            Activity = "Remote observation",
            Equipment = new HashSet<string> { "RPG", "Mortar" },
            VectorClock = new VectorClock(),
            LastModified = DateTime.UtcNow,
            LastModifiedBy = "FOB_Bravo"
        };
        remote.VectorClock.Increment("FOB_Bravo");

        var result = ConflictResolver.Resolve(local, remote);

        Assert.Equal(4, result.Equipment.Count);
        Assert.Contains("Tank", result.Equipment);
        Assert.Contains("APC", result.Equipment);
        Assert.Contains("RPG", result.Equipment);
        Assert.Contains("Mortar", result.Equipment);
    }

    [Fact]
    public void Resolve_ShouldMergeVectorClocks()
    {
        var reportId = Guid.NewGuid().ToString();
        
        var local = new IntelligenceReport
        {
            Id = reportId,
            Activity = "Local",
            VectorClock = new VectorClock(),
            LastModified = DateTime.UtcNow.AddMinutes(-5)
        };
        local.VectorClock.Increment("FOB_Alpha");
        local.VectorClock.Increment("FOB_Alpha");

        var remote = new IntelligenceReport
        {
            Id = reportId,
            Activity = "Remote",
            VectorClock = new VectorClock(),
            LastModified = DateTime.UtcNow
        };
        remote.VectorClock.Increment("FOB_Bravo");

        var result = ConflictResolver.Resolve(local, remote);

        Assert.True(result.VectorClock.Clocks.ContainsKey("FOB_Alpha"));
        Assert.True(result.VectorClock.Clocks.ContainsKey("FOB_Bravo"));
    }
    
    [Fact]
    public void ThreeWayMerge_ShouldResolveCorrectly()
    {
        var reportId = Guid.NewGuid().ToString();
        
        var common = CreateReportWithId(reportId, "FOB_Common", "Common ancestor");
        var local = CreateReportWithId(reportId, "FOB_Alpha", "Local update");
        var remote = CreateReportWithId(reportId, "FOB_Bravo", "Remote update");

        var result = ConflictResolver.ThreeWayMerge(local, remote, common);

        Assert.NotNull(result);
        Assert.Equal(reportId, result.Id);
    }

    private IntelligenceReport CreateReport(string nodeId, string activity)
    {
        var report = new IntelligenceReport
        {
            Activity = activity,
            Size = 10,
            Location = "0,0",
            Unit = "Test Unit",
            LastModifiedBy = nodeId,
            VectorClock = new VectorClock()
        };
        report.VectorClock.Increment(nodeId);
        return report;
    }

    private IntelligenceReport CreateReportWithId(string id, string nodeId, string activity)
    {
        var report = new IntelligenceReport
        {
            Id = id,
            Activity = activity,
            Size = 10,
            Location = "0,0",
            Unit = "Test Unit",
            LastModifiedBy = nodeId,
            VectorClock = new VectorClock()
        };
        report.VectorClock.Increment(nodeId);
        return report;
    }
}

