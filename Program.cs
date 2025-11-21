// See https://aka.ms/new-console-template for more information

using TacticalSync.Core;

class Program
{
    static void Main(string[] args)
    {
        var bar = new Node("foo");
        
        bar.CreateReport("patrolling", 5, "34.0522,-118.2437", "infantry", "rifles", "radios");
        Console.WriteLine("Created report on node 'foo'.");
        var barfoo = bar.GetAllReports();
        bar.UpdateReport(barfoo[0].Id, r =>             {
            r.Size = 25;
            r.Activity = "Enemy convoy stopped, digging defensive positions";
            r.Equipment.Add("Mortar");
        });
        Console.WriteLine("Created report on node 'foo'.");
        Console.WriteLine("Hello World!");
    }
}