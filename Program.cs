// See https://aka.ms/new-console-template for more information

using TacticalSync.Core;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("SCENARIO 1: Basic Synchronization");
        Console.WriteLine(new string('=', 70));

        var network = new NetworkController();
        var fobAlpha = new Node("FOB_Alpha");
        var commandCenter = new Node("Command_Center"); // is a command center node

        // Both nodes online and synced
        Console.WriteLine("\n[Phase 1] Initial sync - both nodes online");
        fobAlpha.CreateReport("Enemy patrol observed", 12, "123,123", "Infantry Squad", "AK-47", "RPG");
        commandCenter.CreateReport("Artillery position identified", 6, "123,123", "Artillery Battery", "Howitzer");

        network.TrySync(fobAlpha, commandCenter);

        fobAlpha.PrintState();
        commandCenter.PrintState();
    }
}