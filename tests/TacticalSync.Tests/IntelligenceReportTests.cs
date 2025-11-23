using TacticalSync.Models;


namespace TacticalSync.Tests;

/// <summary>
/// Unit tests for IntelligenceReport covering:
/// - SALUTE format (Size, Activity, Location, Unit, Time, Equipment)
/// - Clone operations
/// - Initialization
/// </summary>
public class IntelligenceReportTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var report = new IntelligenceReport();

        Assert.NotNull(report.Id);
        Assert.True(Guid.TryParse(report.Id, out _)); // Valid GUID
        Assert.NotNull(report.VectorClock);
        Assert.NotNull(report.Equipment);
        Assert.Empty(report.Equipment);
        Assert.NotEqual(default(DateTime), report.Time);
        Assert.NotEqual(default(DateTime), report.LastModified);
    }
    
    [Fact]
    public void Clone_ShouldPreserveAllFields()
    {
        var original = new IntelligenceReport
        {
            Id = Guid.NewGuid().ToString(),
            Activity = "Enemy patrol",
            Size = 15,
            Location = "40.7128,-74.0060",
            Unit = "Infantry Squad",
            Equipment = new HashSet<string> { "Rifle", "Radio" },
            LastModifiedBy = "FOB_Alpha",
            AuditHash = "test_hash"
        };
        original.VectorClock.Increment("FOB_Alpha");

        var clone = original.Clone();

        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Activity, clone.Activity);
        Assert.Equal(original.Size, clone.Size);
        Assert.Equal(original.Location, clone.Location);
        Assert.Equal(original.Unit, clone.Unit);
        Assert.Equal(original.Equipment.Count, clone.Equipment.Count);
        Assert.Equal(original.LastModifiedBy, clone.LastModifiedBy);
        Assert.Equal(original.AuditHash, clone.AuditHash);
    }

    [Fact]
    public void Clone_ShouldDeepCopyVectorClock()
    {
        var original = new IntelligenceReport();
        original.VectorClock.Increment("FOB_Alpha");
        original.VectorClock.Increment("FOB_Bravo");

        var clone = original.Clone();
        clone.VectorClock.Increment("FOB_Charlie"); // Modify clone's vector clock

        Assert.Equal(2, original.VectorClock.Clocks.Count); // Original unchanged
        Assert.Equal(3, clone.VectorClock.Clocks.Count);
    }
    
    [Fact]
    public void CompleteReport_ShouldFollowSALUTEFormat()
    {
        var report = new IntelligenceReport
        {
            Size = 30,                                      // Size
            Activity = " moving northeast",    // Activity
            Location = "12,32",                 //  Location
            Unit = "Infantry",          //  Unit
            Time = DateTime.UtcNow,                        //Time
            Equipment = new HashSet<string>               // Equipment
            { 
                "tank", 
                "humvee", 
                "radio" 
            }
        };

        Assert.Equal(30, report.Size);
        Assert.NotNull(report.Activity);
        Assert.NotNull(report.Location);
        Assert.NotNull(report.Unit);
        Assert.NotEqual(default(DateTime), report.Time);
        Assert.NotEmpty(report.Equipment);
    }
}

