using System.Threading.Tasks;
using CirclesLand.PathfinderExport;
using NUnit.Framework;

namespace CirclesLand.Pathfinder.Tests;

public class Pathfinder2Export
{
    private const string OutputFile = "/home/daniel/Desktop/capacity_graph.db";
    private const string ConnectionString = "Server=localhost;Port=5429;Database=indexer;User ID=postgres;Password=postgres;Command Timeout=240";

    [Test]
    public async Task TestBinaryExport()
    {
        await BinaryExport.ExportCapacityGraph(ConnectionString, OutputFile);
    }
}