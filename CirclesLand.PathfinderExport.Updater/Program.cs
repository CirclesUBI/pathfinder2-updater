using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Npgsql;

namespace CirclesLand.PathfinderExport.Updater;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var config = Config.Read(args);
        if (config == null)
        {
            return;
        }

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(config.IndexerWebsocketUrl), CancellationToken.None);
            
        var needsFullImport = true;
        var lastIncrementalBlock = 0L;
            
        while (ws.State == WebSocketState.Open)
        {
            var blockUpdateMessage = await ReceiveWsMessage(ws);
            var currentIncrementalBlock = 0L;

            if (blockUpdateMessage == Constants.DeadBeefTxHashJsonArr)
            {
                Console.WriteLine("A reorg occurred. Re-seeding the whole flow graph with the next received block...");
                needsFullImport = true;
            }
                
            var transactionHashesInLastBlock = JsonConvert.DeserializeObject<string[]>(blockUpdateMessage);
            if (transactionHashesInLastBlock == null)
            {
                throw new Exception($"Received an invalid block update via websocket: {blockUpdateMessage}");
            }
                
            if (transactionHashesInLastBlock.Length > 0)
            {
                var firstTxInBlock = transactionHashesInLastBlock[0];
                Console.WriteLine($"Received new block. First tx in block: {firstTxInBlock}");
                    
                var blockLookupSql = Queries.BlockByTransactionHash(firstTxInBlock);
                await using var connection = new NpgsqlConnection(config.IndexerDbConnectionString);
                await connection.OpenAsync();

                var cmd = new NpgsqlCommand(blockLookupSql, connection);
                var blockNo = Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
                if (blockNo == 0)
                {
                    throw new Exception($"Couldn't find the block no. by tx hash '{firstTxInBlock}'");
                }

                currentIncrementalBlock = blockNo;
                Console.WriteLine($"Incremental update starting from block: {currentIncrementalBlock}");
            }

            if (needsFullImport)
            {
                await CapacityGraph.ToBinaryFile(config.IndexerDbConnectionString, config.InternalCapacityGraphPath);   
                
                var requestJsonBody = "{\n    \"id\":\"" + DateTime.Now.Ticks + "\", \n    \"method\": \"load_edges_binary\", \n    \"params\": {\n        \"file\": \"" + config.ExternalCapacityGraphPath + "\"\n    }\n}";
                await DoRpcCAll(config.PathfinderUrl, requestJsonBody);
                    
                needsFullImport = false;
                lastIncrementalBlock = currentIncrementalBlock;
            }
            else
            {
                var updateEdges = await CapacityGraph.SinceBlock(config.IndexerDbConnectionString, lastIncrementalBlock + 1);
                var requestJsonBody = "{\n    \"id\":\"" + DateTime.Now.Ticks +
                                      "\", \n    \"method\": \"update_edges\", \n    \"params\": " +
                                      JsonConvert.SerializeObject(updateEdges) + "\n}";
                await DoRpcCAll(config.PathfinderUrl, requestJsonBody);

                lastIncrementalBlock = currentIncrementalBlock;
            }
        }
    }

    private static async Task<string> ReceiveWsMessage(ClientWebSocket ws)
    {
        var receiving = true;
        var buffer = new byte[4096];
        var mem = new Memory<byte>(buffer);
        var fullMessageBuffer = new List<byte[]>();

        while (receiving)
        {
            var result = await ws.ReceiveAsync(mem, CancellationToken.None);
            var data = new ArraySegment<byte>(buffer, 0, result.Count).ToArray();
            fullMessageBuffer.Add(data);
            receiving = !result.EndOfMessage;
        }

        var fullLength = fullMessageBuffer.Sum(o => o.Length);
        var fullMessageBytes = new byte[fullLength];
        var currentIdx = 0;
        
        foreach (var part in fullMessageBuffer)
        {
            Array.Copy(part, 0, fullMessageBytes, currentIdx, part.Length);
            currentIdx += part.Length;
        }

        fullMessageBuffer.Clear();

        var fullMessageString = Encoding.UTF8.GetString(fullMessageBytes);
        return fullMessageString;
    }

    private static async Task DoRpcCAll(string rpcUrl, string requestJsonBody)
    {
        var requestStopWatch = new Stopwatch();
        requestStopWatch.Start();
        
        Console.WriteLine($"Posting to '{rpcUrl}' ..");
        Console.WriteLine(requestJsonBody);
        
        using var client = new HttpClient();
        var content = new StringContent(requestJsonBody, Encoding.UTF8, "application/json");
        using var rpcResult = client.PostAsync(rpcUrl, content).Result;
        var responseStream = await rpcResult.Content.ReadAsStreamAsync();
        using var streamReader = new StreamReader(responseStream);
        var responseBody = await streamReader.ReadToEndAsync();
        
        requestStopWatch.Stop();
        
        Console.WriteLine($"Posting to '{rpcUrl}' returned in {requestStopWatch.Elapsed}..");
        Console.WriteLine(responseBody);
    }
}