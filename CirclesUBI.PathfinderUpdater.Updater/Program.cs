﻿using CirclesUBI.PathfinderUpdater.Indexer;
using CirclesUBI.Pathfinder.Models;
using CirclesUBI.PathfinderUpdater.PathfinderRpc;

namespace CirclesUBI.PathfinderUpdater.Updater;
public static class Program
{
    private static readonly Logger Logger = new();
    
    private static readonly HealthMonitor _blockUpdateHealth = new HealthMonitor("Indexer", Config.BlockUpdateHealthThreshold);
    private static readonly HealthMonitor _pathfinderResponseHealth = new HealthMonitor("Pathfinder", Config.PathfinderResponseHealthThreshold);
    
    private static readonly HealthEndpoint HealthEndpoint = new("http://+:8794/", new HealthMonitor[]
    {
        _blockUpdateHealth,
        _pathfinderResponseHealth
    });
    
    private static IndexerSubscription? _indexerSubscription;
    private static RpcEndpoint _pathfinderRpc = null!;
    private static Config _config = null!;

    private static bool _isInitialized;

    private static long _currentBlock;
    private static long _lastFullUpdate;
    private static long _lastIncrementalUpdate;

    private static int _working;
    
    public static async Task Main(string[] args)
    {
        _config = Config.Read(args);

        if (_config.EnableIncrementalUpdates)
        {
            throw new NotSupportedException("The pathfinder2 doesn't support incremental updates yet.");
        }

        _pathfinderRpc = new RpcEndpoint(_config.PathfinderUrl);
        
        _indexerSubscription = new IndexerSubscription(_config.IndexerWebsocketUrl);
        _indexerSubscription.SubscriptionEvent += OnIndexerSubscriptionEvent;
        
        await _indexerSubscription.Run();
    }

    private static void OnIndexerSubscriptionEvent(object? sender, IndexerSubscriptionEventArgs e)
    {
        if (e.Error != null)
        {
            OnFatalError(e.Error);
        }

        Logger.Call("On indexer websocket message", async () =>
            {
                Logger.Log($" _working = {_working}");

                if (Interlocked.CompareExchange(ref _working, 1, 0) != 0)
                {
                    Logger.Log($"Still working. Ignore the incoming message.");
                    return;
                }

                if (e.Message!.TransactionHashes.Contains(Constants.DeadBeefTxHash))
                {
                    OnReorgOccurred();
                }
                else
                {
                    await OnNewBlock(e.Message.TransactionHashes);
                }

                Interlocked.Exchange(ref _working, 0);
            })
            .ContinueWith(result =>
            {
                if (result.Exception != null)
                {
                    OnFatalError(result.Exception);
                }
            });
    }

    private static void OnFatalError(Exception e)
    {
        Logger.Log($"An error occurred:");
        Logger.Log(e.Message);
        Logger.Log(e.StackTrace ?? "");

        Environment.Exit(99);
    }

    private static async Task OnNewBlock(string[] transactionHashes)
    {
        if (transactionHashes.Length == 0)
        {
            Logger.Log("Ignore empty block");
            return;
        }

        await Logger.Call("On new block", async () =>
        {
            _blockUpdateHealth.KeepAlive();
            
            await Logger.Call("Find block number", async () =>
            {
                _currentBlock = await Block.FindByTransactionHash(
                    _config.IndexerDbConnectionString,
                    transactionHashes[0]);
                
                Logger.Log($"Block No.: {_currentBlock}");
            });

            if (!_config.EnableIncrementalUpdates)
            {
                Logger.Log("Set 'isInitialized = false' because incremental updates are disabled");
                _isInitialized = false;
            }
            
            if (!_isInitialized)
            {
                await OnInit();
            }
            else
            {
                await OnIncrementalUpdate();
            }
        });
    }

    private static async Task OnInit()
    {
        await Logger.Call("Initialize the pathfinder2 with a new capacity graph", async () =>
        {
            await Logger.Call($"Export graph to '{_config.InternalCapacityGraphPath}'", async () =>
            {
                await using var outFileStream = await ExportUtil.Program.ExportToBinaryFile(_config.InternalCapacityGraphPath, _config.IndexerDbConnectionString);
                /*var runtimes = await CapacityGraph.ToBinaryFile(
                    _config.IndexerDbConnectionString,
                    _config.InternalCapacityGraphPath);

                Logger.Log($"SQL query took                      {runtimes.queryDuration}");
                Logger.Log($"Download took                       {runtimes.downloadDuration}");
                Logger.Log($"Writing the edges took              {runtimes.writeEdgesDuration}");
                Logger.Log($"Writing the nodes took              {runtimes.writeNodesDuration}");
                Logger.Log($"Concatenating nodes and edges took  {runtimes.concatDumpFilesDuration}");
*/
            });
            
            await Logger.Call($"Call 'load_safes_binary' on pathfinder at '{_config.PathfinderUrl}'", async () =>
            {
                var callResult = await _pathfinderRpc.Call(
                    RpcCalls.LoadSafesBinary(_config.ExternalCapacityGraphPath));

                Logger.Log("Response body: ");
                Logger.Log(callResult.resultBody);
                
                _pathfinderResponseHealth.KeepAlive();
            });

            _isInitialized = true;
            _lastFullUpdate = _currentBlock;
            _lastIncrementalUpdate = _currentBlock;
            
            Logger.Log($"Pathfinder2 initialized up to block {_lastFullUpdate}");
        });
    }

    private static void OnReorgOccurred()
    {
        Logger.Call("On reorg (the indexer sent the '0xDEADBEEF..' transaction hash)", () =>
        {
            _isInitialized = false;
            Logger.Log("isInitialized = false -> Re-initialize on next block.");
        });
    }

    private static async Task OnIncrementalUpdate()
    {
        await Logger.Call("On incremental update", async () =>
        {
            Logger.Log($"Last incremental update at block: {_lastIncrementalUpdate}");
            Logger.Log($"Current block:                    {_lastIncrementalUpdate}");

            var updateSinceBlock = _lastIncrementalUpdate + 1;
            IEnumerable<IncrementalExportRow> rows = new IncrementalExportRow[]{};
            
             await Logger.Call($"Load changes since block {updateSinceBlock}", async () =>
             {
                 var edgeReaderResult = await CapacityGraph.SinceBlock(
                     _config.IndexerDbConnectionString, 
                     updateSinceBlock);
                 
                 rows = edgeReaderResult.result;

                 Logger.Log($"SQL query took {edgeReaderResult.queryDuration}");
                 Logger.Log($"Download took  {edgeReaderResult.downloadDuration}");
             });
// TODO: Re-implement live updates
/*
             await Logger.Call($"Call 'update_edges' on pathfinder at '{_config.PathfinderUrl}'", async () =>
             {
                 var callResult = await _pathfinderRpc.Call(RpcCalls.UpdateEdges(rows));

                 Logger.Log("Response body: ");
                 Logger.Log(callResult.resultBody);

                 _lastIncrementalUpdate = _currentBlock;
             });
*/
        });
    }
}