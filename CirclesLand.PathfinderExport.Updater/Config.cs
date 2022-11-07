using System.Diagnostics;

namespace CirclesLand.PathfinderExport.Updater;

public class Config
{
    /// <summary>
    /// The connection string to a indexer db.
    /// </summary>
    public string IndexerDbConnectionString { get; }

    public string IndexerWebsocketUrl { get; }

    /// <summary>
    /// The location (e.g. within a docker container) where the capacity graph binary should be dumped.
    /// </summary>
    public string InternalCapacityGraphPath { get; }

    /// <summary>
    /// The location where the pathfinder2 can find the capacity graph binary (e.g. outside of a docker container).
    /// </summary>
    public string ExternalCapacityGraphPath { get; }

    /// <summary>
    /// The url to the rpc endpoint of a running pathfinder2 instance.
    /// </summary>
    public string PathfinderUrl { get; }
    
    private Config(string indexerDbConnectionString
                 , string indexerWebsocketUrl
                 , string internalCapacityGraphPath
                 , string externalCapacityGraphPath
                 , string pathfinderUrl)
    {
        IndexerDbConnectionString = indexerDbConnectionString;
        IndexerWebsocketUrl = indexerWebsocketUrl;
        InternalCapacityGraphPath = internalCapacityGraphPath;
        ExternalCapacityGraphPath = externalCapacityGraphPath;
        PathfinderUrl = pathfinderUrl;
    }

    public static Config? Read(string[] args)
    {
        switch (args.Length)
        {
            case 0:
                // Configured via env-vars?
                return new Config(
                    Environment.GetEnvironmentVariable("INDEXER_DB_CONNECTION_STRING") ?? ""
                    , Environment.GetEnvironmentVariable("INDEXER_WS_URL") ?? ""
                    , Environment.GetEnvironmentVariable("INTERNAL_CAPACITY_GRAPH_PATH") ?? ""
                    , Environment.GetEnvironmentVariable("EXTERNAL_CAPACITY_GRAPH_PATH") ?? ""
                    , Environment.GetEnvironmentVariable("PATHFINDER_RPC_URL") ?? "");
            case 4:
                return new Config(
                    args[0]
                    , args[1]
                    , args[2]
                    , args[3]
                    , args[4]);
            default:
                Console.WriteLine($"Usage:");
                Console.WriteLine($"{Process.GetCurrentProcess().ProcessName}");
                Console.WriteLine($"  arg 0: ADO.Net connection string to the indexer database");
                Console.WriteLine($"  arg 1: URL to the indexer websocket");
                Console.WriteLine(
                    $"  arg 2: The location (e.g. within a docker container) where the capacity graph binary should be dumped");
                Console.WriteLine(
                    $"  arg 3: The location where the pathfinder2 can find the capacity graph binary (e.g. outside of a docker container)");
                Console.WriteLine($"  arg 4: The URL to the running pathfinder2 json rpc interface");
                Console.WriteLine("");
                Console.WriteLine(
                    "Alternatively you can use the following environment variables to configure the service:");
                Console.WriteLine("   INDEXER_DB_CONNECTION_STRING");
                Console.WriteLine("   INDEXER_WS_URL");
                Console.WriteLine("   INTERNAL_CAPACITY_GRAPH_PATH");
                Console.WriteLine("   EXTERNAL_CAPACITY_GRAPH_PATH");
                Console.WriteLine("   PATHFINDER_RPC_URL");
                return null;
        }
    }
}