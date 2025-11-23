using Xunit;

namespace TacticalSync.Tests;

/// <summary>
/// Unit tests for AuditLog covering:
/// - Hash calculation (SHA-256)
/// - Hash verification
/// - Tamper detection
/// - NIST SP 800-53 Control AU-3 compliance
/// </summary>
public class AuditLogTests
{
    [Fact]
    public void Constructor_ShouldGenerateEventIdAndTimestamp()
    {
        var log = new AuditLog();
        
        Assert.NotNull(log.EventId);
        Assert.NotEqual(default(DateTime), log.Timestamp);
        Assert.True(Guid.TryParse(log.EventId, out _)); // Valid GUID
    }

    [Fact]
    public void CalculateHash_ShouldGenerateNonEmptyHash()
    {
        var log = new AuditLog
        {
            ActorId = "analyst_001",
            Action = "CREATE_REPORT",
            ResourceId = "report_123",
            Outcome = "SUCCESS",
            PreviousHash = "START",
            Details = "Created intelligence report"
        };

        log.CalculateHash();

        Assert.NotNull(log.CurrentHash);
        Assert.NotEmpty(log.CurrentHash);
    }

    [Fact]
    public void CalculateHash_ShouldBeDeterministic()
    {
        var log1 = CreateTestLog();
        var log2 = CreateTestLog();

        log2.EventId = log1.EventId;
        log2.Timestamp = log1.Timestamp;
        log2.ActorId = log1.ActorId;
        log2.Action = log1.Action;
        log2.ResourceId = log1.ResourceId;
        log2.Outcome = log1.Outcome;
        log2.PreviousHash = log1.PreviousHash;
        log2.Details = log1.Details;

        // Act
        log1.CalculateHash();
        log2.CalculateHash();

        // Assert
        Assert.Equal(log1.CurrentHash, log2.CurrentHash);
    }

    [Fact]
    public void CalculateHash_ShouldChangeDifferentInputs()
    {
        var log1 = CreateTestLog();
        log1.Details = "First details";

        var log2 = CreateTestLog();
        log2.Details = "Different details";

        log1.CalculateHash();
        log2.CalculateHash();

        Assert.NotEqual(log1.CurrentHash, log2.CurrentHash);
    }

    [Fact]
    public void VerifyHash_ShouldReturnFalse_WhenDataIsTampered()
    {
        var log = CreateTestLog();
        log.CalculateHash();

        log.Details = "TAMPERED DATA";
        var isValid = log.VerifyHash();

        Assert.False(isValid);
    }


    [Fact]
    public void HashChaining_ShouldLinkEntries()
    {
        // Arrange
        var entry1 = CreateTestLog();
        entry1.PreviousHash = "START";
        entry1.CalculateHash();

        var entry2 = CreateTestLog();
        entry2.PreviousHash = entry1.CurrentHash; // Link to previous
        entry2.CalculateHash();

        Assert.Equal(entry1.CurrentHash, entry2.PreviousHash);
        Assert.NotEqual(entry1.CurrentHash, entry2.CurrentHash);
    }
    
    [Fact]
    public void AuditLog_ShouldCaptureNISTRequiredFields()
    {
        // Arrange & Act - NIST SP 800-53 Control AU-3 requirements
        var log = new AuditLog
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,           // When
            ActorId = "abcd",          // Who
            Action = "UPDATE_REPORT",              // What (type of event)
            ResourceId = "01",             // Where (resource)
            Outcome = "SUCCESS",                   // Outcome
            Details = "Updated Size"         // Additional details
        };

        Assert.NotNull(log.EventId);
        Assert.NotEqual(default(DateTime), log.Timestamp);
        Assert.NotNull(log.ActorId);
        Assert.NotNull(log.Action);
        Assert.NotNull(log.ResourceId);
        Assert.NotNull(log.Outcome);
    }
    
    // Helper method
    private AuditLog CreateTestLog()
    {
        return new AuditLog
        {
            ActorId = "test_actor",
            Action = "TEST_ACTION",
            ResourceId = "test_resource",
            Outcome = "SUCCESS",
            PreviousHash = "START",
            Details = "Test audit entry"
        };
    }
}

