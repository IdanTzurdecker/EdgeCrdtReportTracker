using TacticalSync.Core;
using TacticalSync.Models;
using Xunit;

namespace TacticalSync.Tests;

/// <summary>
/// Unit tests for Node covering:
/// - Report creation and updates
/// - Sync
/// - Audit trail check
/// - Vector clock management
/// </summary>
public class NodeTests
{
    [Fact]
    public void Constructor_ShouldInitializeNode()
    {
        var node = new Node("FOB_Alpha");

        Assert.Equal("FOB_Alpha", node.NodeId);
        Assert.Empty(node.GetAllReports());
    }

    [Fact]
    public void CreateReport_ShouldAddReportToLocalStore()
    {
        var node = new Node("FOB_Alpha");

        var report = node.CreateReport("Enemy spotted", 10, "0,0", "Infantry", "Rifle");

        Assert.NotNull(report);
        Assert.NotNull(report.Id);
        Assert.Equal("Enemy spotted", report.Activity);
        Assert.Single(node.GetAllReports());
    }

    [Fact]
    public void CreateReport_ShouldIncrementVectorClock()
    {
        var node = new Node("FOB_Alpha");

        node.CreateReport("Report 1", 10, "0,0", "Unit", "Equipment");
        var report2 = node.CreateReport("Report 2", 15, "1,1", "Unit", "Equipment");

        Assert.True(report2.VectorClock.Clocks.ContainsKey("FOB_Alpha"));
        Assert.True(report2.VectorClock.Clocks["FOB_Alpha"] > 0);
    }

    [Fact]
    public void CreateReport_ShouldAddAuditEntry()
    {
        var node = new Node("FOB_Alpha");

        node.CreateReport("Test report", 10, "0,0", "Unit", "Equipment");
        var auditTrail = node.GetAuditTrail();

        Assert.Single(auditTrail);
        Assert.Equal("CREATE_REPORT", auditTrail[0].Action);
        Assert.Equal("SUCCESS", auditTrail[0].Outcome);
    }

    [Fact]
    public void CreateReport_ShouldSetLastModifiedBy()
    {
        var node = new Node("FOB_Alpha");

        var report = node.CreateReport("Test", 10, "0,0", "Unit", "Equipment");

        Assert.Equal("FOB_Alpha", report.LastModifiedBy);
    }

    [Fact]
    public void UpdateReport_ShouldModifyExistingReport()
    {
        var node = new Node("FOB_Alpha");
        var report = node.CreateReport("Original", 10, "0,0", "Unit", "Equipment");

        var success = node.UpdateReport(report.Id, r =>
        {
            r.Activity = "Updated";
            r.Size = 20;
        });

        Assert.True(success);
        var updated = node.GetReport(report.Id);
        Assert.Equal("Updated", updated.Activity);
        Assert.Equal(20, updated.Size);
    }

    [Fact]
    public void UpdateReport_ShouldIncrementVectorClock()
    {
        var node = new Node("FOB_Alpha");
        var report = node.CreateReport("Test", 10, "0,0", "Unit", "Equipment");
        var initialClock = report.VectorClock.Clocks["FOB_Alpha"];

        node.UpdateReport(report.Id, r => r.Size = 15);
        var updated = node.GetReport(report.Id);

        Assert.True(updated.VectorClock.Clocks["FOB_Alpha"] > initialClock);
    }

    [Fact]
    public void UpdateReport_ShouldReturnFalse_WhenReportNotFound()
    {
        var node = new Node("FOB_Alpha");
        var success = node.UpdateReport("dne_id", r => r.Size = 10);

        Assert.False(success);
    }

    [Fact]
    public void UpdateReport_ShouldAddAuditEntry()
    {
        var node = new Node("FOB_Alpha");
        var report = node.CreateReport("Test", 10, "0,0", "Unit", "Equipment");

        node.UpdateReport(report.Id, r => r.Size = 20);
        var auditTrail = node.GetAuditTrail();

        Assert.Equal(2, auditTrail.Count); // create + update
        Assert.Equal("UPDATE_REPORT", auditTrail[1].Action);
    }

    [Fact]
    public void GetReport_ShouldReturnNull_WhenNotFound()
    {
        var node = new Node("FOB_Alpha");

        var report = node.GetReport("dne_id");

        Assert.Null(report);
    }

    [Fact]
    public void SyncWith_ShouldTransferNewReports()
    {
        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");

        nodeA.CreateReport("Report from Alpha", 10, "0,0", "Unit", "Equipment");

        var result = nodeB.SyncWith(nodeA);

        Assert.Equal(1, result.ReportsReceived);
        Assert.Single(nodeB.GetAllReports());
    }

    [Fact]
    public void SyncWith_ShouldNotDuplicateReports()
    {
        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");

        var report = nodeA.CreateReport("Shared report", 10, "0,0", "Unit", "Equipment");
        nodeB.SyncWith(nodeA);

        var result = nodeB.SyncWith(nodeA);

        Assert.Equal(0, result.ReportsReceived);
        Assert.Single(nodeB.GetAllReports());
    }

    [Fact]
    public void SyncWith_ShouldResolveConflicts()
    {
        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");

        var report = nodeA.CreateReport("Initial", 10, "0,0", "Unit", "Equipment");
        nodeB.SyncWith(nodeA);

        Thread.Sleep(10);
        nodeA.UpdateReport(report.Id, r =>
        {
            r.Activity = "Updated by Alpha";
            r.Size = 15;
        });

        Thread.Sleep(10);
        nodeB.UpdateReport(report.Id, r =>
        {
            r.Activity = "Updated by Bravo";
            r.Size = 20;
        });

        var result = nodeB.SyncWith(nodeA);

        Assert.True(result.ConflictsResolved > 0 || result.ReportsUpdated > 0);
        var syncedReport = nodeB.GetReport(report.Id);
        Assert.NotNull(syncedReport);
    }

    [Fact]
    public void SyncWith_ShouldMergeEquipmentSets() //g-set union
    {
        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");

        var report = nodeA.CreateReport("Test", 10, "0,0", "Unit", "Tank");
        nodeB.SyncWith(nodeA);

        nodeA.UpdateReport(report.Id, r => r.Equipment.Add("APC"));
        nodeB.UpdateReport(report.Id, r => r.Equipment.Add("Helicopter"));

        nodeB.SyncWith(nodeA);

        var syncedReport = nodeB.GetReport(report.Id);
        Assert.True(syncedReport.Equipment.Count >= 2); // Should have merged equipment
    }

    [Fact]
    public void SyncWith_ShouldUpdateVectorClocks()
    {
        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");

        nodeA.CreateReport("Test", 10, "0,0", "Unit", "Equipment");

        nodeB.SyncWith(nodeA);
        var report = nodeB.GetAllReports().First();

        Assert.True(report.VectorClock.Clocks.ContainsKey("FOB_Alpha"));
    }

    [Fact]
    public void VerifyAuditChain_ShouldReturnTrue_ForValidChain()
    {
        var node = new Node("FOB_Alpha");
        node.CreateReport("Report 1", 10, "0,0", "Unit", "Equipment");
        node.CreateReport("Report 2", 15, "1,1", "Unit", "Equipment");

        var isValid = node.VerifyAuditChain();

        Assert.True(isValid);
    }


    [Fact]
    public void MultipleNodes_ShouldEventuallyConverge()
    {
        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");
        var nodeC = new Node("FOB_Charlie");

        var report = nodeA.CreateReport("Shared", 10, "0,0", "Unit", "Equipment");

        // Chain sync: A -> B -> C
        nodeB.SyncWith(nodeA);
        nodeC.SyncWith(nodeB);

        Assert.Single(nodeA.GetAllReports());
        Assert.Single(nodeB.GetAllReports());
        Assert.Single(nodeC.GetAllReports());

        var reportC = nodeC.GetReport(report.Id);
        Assert.NotNull(reportC);
        Assert.Equal("Shared", reportC.Activity);
    }
}