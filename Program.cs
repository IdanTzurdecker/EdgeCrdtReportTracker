// See https://aka.ms/new-console-template for more information

using TacticalSync.Core;

class Program
{
    static void Main(string[] args)
    {
        //RunScenario_BasicSync();
        //RunScenario_NetworkPartition();
        RunScenario_ConcurrentConflicts();
        RunScenario_AuditIntegrity();
    }

    static void RunScenario_BasicSync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("SCENARIO 1: Basic Synchronization");
        Console.WriteLine(new string('=', 70));

        var network = new NetworkController(); //satcom
        var fobAlpha = new Node("FOB_Alpha");
        var commandCenter = new Node("Command_Center"); // is a command center node 

        // Both nodes online and synced
        Console.WriteLine("\n[Phase 1] Initial sync - both nodes online");
        fobAlpha.CreateReport("Enemy patrol observed", 12, "123,123", "Infantry Squad", "AK-47", "RPG");
        commandCenter.CreateReport("Artillery position identified", 6, "412,142", "Artillery Battery", "tank");

        network.TrySync(fobAlpha, commandCenter);

        fobAlpha.PrintState();
        commandCenter.PrintState();
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine(new string('=', 70));
    }

    /// <summary>
    /// Scenario 2: Network partition and divergence, then reconciliation.
    /// </summary>
    static void RunScenario_NetworkPartition()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("SCENARIO 2: Network Partition and Recovery");
        Console.WriteLine(new string('=', 70));

        var network = new NetworkController();
        var fobAlpha = new Node("FOB_Alpha");
        var fobBravo = new Node("FOB_Bravo");

        // Initial sync
        Console.WriteLine("\n[Phase 1] Initial sync");
        var report1 = fobAlpha.CreateReport("Enemy convoy moving north", 20, "123,123", "Motorized Infantry",
            "Truck", "Guns");
        network.TrySync(fobAlpha, fobBravo);

        // Simulate SATCOM outage
        Console.WriteLine("\n[Phase 2] Network partition (SATCOM OUTAGE)");
        network.Disconnect("FOB_Alpha", "FOB_Bravo");
        Thread.Sleep(100); // Simulate time passing

        // Divergent updates while disconnected
        Console.WriteLine("\n[Phase 3] Divergence - both nodes update independently");
        fobAlpha.UpdateReport(report1.Id, r =>
        {
            r.Size = 25;
            r.Activity = "Enemy stopped moving";
            r.Equipment.Add("Mortar");
        });

        fobBravo.UpdateReport(report1.Id, r =>
        {
            r.Size = 18;
            r.Activity = "Enemy moving north";
            r.Equipment.Add("Gun");
        });

        Console.WriteLine("\n[Phase 4] Attempting sync during outage (should fail)");
        network.TrySync(fobAlpha, fobBravo);

        // Restore connectivity
        Console.WriteLine("\n[Phase 5] Network healed (SATCOM RESTORED)");
        network.Connect("FOB_Alpha", "FOB_Bravo");

        // Sync and resolve conflict
        Console.WriteLine("\n[Phase 6] Sync with conflict resolution");
        network.TrySync(fobAlpha, fobBravo);

        fobAlpha.PrintState();
        fobBravo.PrintState();

        // Verify both converged to same state
        var alphaReport = fobAlpha.GetReport(report1.Id);
        var bravoReport = fobBravo.GetReport(report1.Id);

        Console.WriteLine($"Convergence Check:");
        Console.WriteLine($"  Equipment sets match: {alphaReport.Equipment.SetEquals(bravoReport.Equipment)}");
        Console.WriteLine($"  Equipment count: {alphaReport.Equipment.Count} items");
        Console.WriteLine($"  Equipment: {string.Join(", ", alphaReport.Equipment)}");
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine(new string('=', 70));
    }

    
    /// <summary>
    /// Scenario 3: 3 nodes all with concurrent conflicting updates, then they resolve 
    /// </summary>
    static void RunScenario_ConcurrentConflicts()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("SCENARIO 3: Concurrent Conflicts (Three FOBs)");
        Console.WriteLine(new string('=', 70));

        var network = new NetworkController();
        var fobAlpha = new Node("FOB_Alpha");
        var fobBravo = new Node("FOB_Bravo");
        var fobCharlie = new Node("FOB_Charlie");

        // All nodes start synced
        Console.WriteLine("\n[Phase 1] All nodes synced");
        var report = fobAlpha.CreateReport(" FIRST MESSGE: Enemies Gathering", 30, "123,123", "Fleet", "Tank");
        network.TrySync(fobAlpha, fobBravo);
        network.TrySync(fobBravo, fobCharlie);

        // seperate all nodes
        Console.WriteLine("\n[Phase 2] Complete network partition");
        network.PartitionAll(fobAlpha, fobBravo, fobCharlie);

        // Each node makes concurrent updates
        Console.WriteLine("\n[Phase 3] Concurrent updates on all nodes");
        Thread.Sleep(50); //  different report timestamps
        fobAlpha.UpdateReport(report.Id, r =>
        {
            r.Size = 35;
            r.Activity = "Hostile forces ADVANCING";
            r.Equipment.Add("TRUCKS");
        });

        Thread.Sleep(50);
        fobBravo.UpdateReport(report.Id, r =>
        {
            r.Size = 32;
            r.Activity = "Hostile forces assembling STOPPED";
            r.Equipment.Add("Helicopter");
        });

        Thread.Sleep(50);
        fobCharlie.UpdateReport(report.Id, r =>
        {
            r.Size = 40;
            r.Activity = "Hostile forces assembling PREPARING";
            r.Equipment.Add("Artillery");
        });

        // Reconnect network
        // can be done in any order, as per communicative prop of crdts
        Console.WriteLine("\n[Phase 4] network reconnection and sync");
        network.Connect("FOB_Alpha", "FOB_Bravo");
        network.TrySync(fobAlpha, fobBravo);

        network.Connect("FOB_Bravo", "FOB_Charlie");
        network.TrySync(fobBravo, fobCharlie);

        network.Connect("FOB_Alpha", "FOB_Charlie");
        network.TrySync(fobAlpha, fobCharlie);

        // Final states
        Console.WriteLine("\n[Phase 5] Final converged state");
        fobAlpha.PrintState();
        fobBravo.PrintState();
        fobCharlie.PrintState();

        // Verify 
        var alphaReport = fobAlpha.GetReport(report.Id);
        var bravoReport = fobBravo.GetReport(report.Id);
        var charlieReport = fobCharlie.GetReport(report.Id);

        bool converged = alphaReport.Equipment.SetEquals(bravoReport.Equipment) &&
                         bravoReport.Equipment.SetEquals(charlieReport.Equipment);

        Console.WriteLine($"Three-way Convergence: {(converged ? "✓ SUCCESS" : "✗ FAILED")}");
        Console.WriteLine(
            $"Final Equipment: {string.Join(", ", alphaReport.Equipment)} ({alphaReport.Equipment.Count} items)");
    }

    /// <summary>
    /// Scenario 4: Audit integrity verification.
    /// </summary>
    static void RunScenario_AuditIntegrity()
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("SCENARIO 4: Audit Trail Integrity ( using the NIST SP 800-53 Compliance)");
        Console.WriteLine(new string('=', 50));

        var fob = new Node("FOB_Test");

        Console.WriteLine("\n[Phase 1] Create audit trail with multiple operations");
        var report1 = fob.CreateReport("Test operation 1", 10, "123,123", "Test Unit", "Equipment1");
        var report2 = fob.CreateReport("Test operation 2", 15, "123", "Test Unit", "Equipment2");
        fob.UpdateReport(report1.Id, r => r.Size = 12);
        fob.UpdateReport(report2.Id, r => r.Activity = "Updated activity");

        Console.WriteLine("\n[Phase 2] Verify audit chain integrity");
        var trail = fob.GetAuditTrail();
        bool chainValid = fob.VerifyAuditChain();

        Console.WriteLine($"Audit Trail Entries: {trail.Count}");
        Console.WriteLine($"Chain Integrity: {(chainValid ? "is VALID" : "is BROKEN")}");

        Console.WriteLine("\nAudit Trail:");
        foreach (var entry in trail)
        {
            Console.WriteLine(
                $"  [{entry.Timestamp:HH:mm:ss}] {entry.Action} on {entry.ResourceId} by {entry.ActorId}");
            Console.WriteLine($"    Outcome: {entry.Outcome}");
            Console.WriteLine($"    Hash: {entry.CurrentHash.Substring(0, 16)}...");
            Console.WriteLine(
                $"    Prev: {entry.PreviousHash.Substring(0, Math.Min(16, entry.PreviousHash.Length))}...");
        }

        // Demonstrate tamper detection
        Console.WriteLine("\n[Phase 3] test for tamper");
        Console.WriteLine("modifying the audit entry");

        var auditCopy = fob.GetAuditTrail();
        if (auditCopy.Count > 2)
        {
            auditCopy[1].Details = "TAMPERED DATA";
            bool isValid = auditCopy[1].VerifyHash();
            Console.WriteLine($"Tampered entry verification: {(isValid ? "is UNDETECTED" : "is DETECTED")}");
        }

        Console.WriteLine($"\nOriginal chain still valid: {(fob.VerifyAuditChain() ? "YES" : "NO")}");
    }
}