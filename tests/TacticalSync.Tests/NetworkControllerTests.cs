using TacticalSync.Core;
using Xunit;

namespace TacticalSync.Tests;

/// <summary>
/// Unit tests for NetworkController covering:
/// - Network partition simulation (SATCOM outages)
/// - Connection management
/// - Sync blocking during disconnection
/// - Network restoration
/// </summary>
public class NetworkControllerTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var network = new NetworkController();

        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");
        Assert.True(network.CanCommunicate("FOB_Alpha", "FOB_Bravo"));
    }

    [Fact]
    public void Disconnect_ShouldBlockCommunication()
    {
        var network = new NetworkController();
        network.Disconnect("FOB_Alpha", "FOB_Bravo");
        Assert.False(network.CanCommunicate("FOB_Alpha", "FOB_Bravo"));
    }
    
    [Fact]
    public void Connect_ShouldRestoreCommunication()
    {
        var network = new NetworkController();
        network.Disconnect("FOB_Alpha", "FOB_Bravo");
        network.Connect("FOB_Alpha", "FOB_Bravo");

        Assert.True(network.CanCommunicate("FOB_Alpha", "FOB_Bravo"));
    }

    [Fact]
    public void Connect_ShouldRestoreBidirectionalCommunication()
    {
        var network = new NetworkController();
        network.Disconnect("FOB_Alpha", "FOB_Bravo");

        network.Connect("FOB_Alpha", "FOB_Bravo");

        Assert.True(network.CanCommunicate("FOB_Alpha", "FOB_Bravo"));
        Assert.True(network.CanCommunicate("FOB_Bravo", "FOB_Alpha"));
    }

    [Fact]
    public void TrySync_ShouldSucceed_WhenNodesConnected()
    {
        var network = new NetworkController();
        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");
        
        nodeA.CreateReport("Test report", 10, "0,0", "Unit", "Equipment");

        var result = network.TrySync(nodeA, nodeB);

        Assert.NotNull(result);
        Assert.Single(nodeB.GetAllReports());
    }

    [Fact]
    public void TrySync_ShouldReturnNull_WhenNodesDisconnected()
    {
        var network = new NetworkController();
        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");
        
        network.Disconnect("FOB_Alpha", "FOB_Bravo");
        nodeA.CreateReport("Test report", 10, "0,0", "Unit", "Equipment");

        var result = network.TrySync(nodeA, nodeB);

        Assert.Null(result);
        Assert.Empty(nodeB.GetAllReports()); // No sync occurred
    }

    [Fact]
    public void TrySync_ShouldBeBidirectional()
    {
        var network = new NetworkController();
        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");
        
        nodeA.CreateReport("From Alpha", 10, "0,0", "Unit", "Equipment");
        nodeB.CreateReport("From Bravo", 15, "1,1", "Unit", "Equipment");

        var result = network.TrySync(nodeA, nodeB);

        Assert.NotNull(result);
        Assert.Equal(2, nodeA.GetAllReports().Count); // A received from B
        Assert.Equal(2, nodeB.GetAllReports().Count); // B received from A
    }

    [Fact] //helper
    public void PartitionAll_ShouldDisconnectAllNodes()
    {
        var network = new NetworkController();
        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");
        var nodeC = new Node("FOB_Charlie");

        network.PartitionAll(nodeA, nodeB, nodeC);

        Assert.False(network.CanCommunicate("FOB_Alpha", "FOB_Bravo"));
        Assert.False(network.CanCommunicate("FOB_Alpha", "FOB_Charlie"));
        Assert.False(network.CanCommunicate("FOB_Bravo", "FOB_Charlie"));
    }
    
    [Fact]
    public void MultipleDisconnections_ShouldBeIndependent()
    {
        var network = new NetworkController();
        
        network.Disconnect("FOB_Alpha", "FOB_Bravo");
        network.Disconnect("FOB_Charlie", "FOB_Delta");

        Assert.False(network.CanCommunicate("FOB_Alpha", "FOB_Bravo"));
        Assert.False(network.CanCommunicate("FOB_Charlie", "FOB_Delta"));
        Assert.True(network.CanCommunicate("FOB_Alpha", "FOB_Charlie")); // Not disconnected
    }
    
    [Fact]
    public void TrySync_ShouldCombineResults()
    {
        var network = new NetworkController();
        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");
        
        nodeA.CreateReport("Report A1", 10, "0,0", "Unit", "Equipment");
        nodeA.CreateReport("Report A2", 15, "1,1", "Unit", "Equipment");
        nodeB.CreateReport("Report B1", 20, "2,2", "Unit", "Equipment");

        var result = network.TrySync(nodeA, nodeB);

        Assert.NotNull(result);
        Assert.True(result.ReportsReceived > 0); 
    }

    [Fact]
    public void CanCommunicate_ShouldReturnTrue_OnInit()
    {
        var network = new NetworkController();

        Assert.True(network.CanCommunicate("Node1", "Node2"));
        Assert.True(network.CanCommunicate("Any", "Node"));
    }

    [Fact]
    public void Disconnect_ThenReconnect_ShouldFullyRestore()
    {
        var network = new NetworkController();
        var nodeA = new Node("FOB_Alpha");
        var nodeB = new Node("FOB_Bravo");

        network.Disconnect("FOB_Alpha", "FOB_Bravo");
        network.Connect("FOB_Alpha", "FOB_Bravo");
        network.Disconnect("FOB_Alpha", "FOB_Bravo");
        network.Connect("FOB_Alpha", "FOB_Bravo");

        Assert.True(network.CanCommunicate("FOB_Alpha", "FOB_Bravo"));
        
        nodeA.CreateReport("Test", 10, "0,0", "Unit", "Equipment");
        var result = network.TrySync(nodeA, nodeB);
        Assert.NotNull(result);
    }
}

