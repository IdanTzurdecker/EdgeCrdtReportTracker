// See https://aka.ms/new-console-template for more information

using TacticalSync.Core;

class Program
{
    static void Main(string[] args)
    {
        RunScenario_BasicSync();
        RunScenario2_NetworkPartition();
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
        commandCenter.CreateReport("Artillery position identified", 6, "412,142", "Artillery Battery", "Howitzer");

        network.TrySync(fobAlpha, commandCenter);

        fobAlpha.PrintState();
        commandCenter.PrintState();
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine(new string('=', 70));
    }

    /// <summary>
    /// Scenario 2: Network partition and divergence, then reconciliation.
    /// </summary>
    static void RunScenario2_NetworkPartition()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("SCENARIO 2: Network Partition and Recovery (DDIL Connectivity)");
        Console.WriteLine(new string('=', 70));

        var network = new NetworkController();
        var fobAlpha = new Node("FOB_Alpha");
        var fobBravo = new Node("FOB_Bravo");

        // Initial sync
        Console.WriteLine("\n[Phase 1] Initial sync");
        var report1 = fobAlpha.CreateReport("Enemy convoy moving north", 20, "123,123", "Motorized Infantry",
            "Truck", "AK-47");
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
            r.Activity = "Enemy convoy stopped, digging defensive positions";
            r.Equipment.Add("Mortar");
        });

        fobBravo.UpdateReport(report1.Id, r =>
        {
            r.Size = 18;
            r.Activity = "Enemy convoy moving north, increased speed";
            r.Equipment.Add("Machine Gun");
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
}