using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Npgsql;

namespace CirclesLand.PathfinderExport.Updater;

public static class Program
{
    public const string DeadBeefTxHashJsonArr = "[\"0xDEADBEEF00000000000000000000000000000000000000000000000000000000\"]";

    public static string IndexerDbConnectionString { get; set; } =
        "Server=localhost;Port=5429;Database=indexer;User ID=postgres;Password=postgres;Command Timeout=240";
    public static string IndexerWebsocketUrl { get; set; } = "ws://localhost:8675/";
    public static string CapacityGraphFile { get; set; } = "/home/daniel/Desktop/capacity_graph.db";

    public static string PathfinderUrl { get; set; } = "http://localhost:1234";
    
    public static  async Task Main()
    {
        using (var ws = new ClientWebSocket())
        {
            await ws.ConnectAsync(new Uri(IndexerWebsocketUrl), CancellationToken.None);
            var buffer = new byte[4096];
            var mem = new Memory<byte>(buffer);
            var fullMessageBuffer = new List<byte[]>();
            var needsFullImport = true;
            var lastIncrementalBlock = 0L;
            
            while (ws.State == WebSocketState.Open)
            {
                var receiving = true;
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
                var currentIncrementalBlock = 0L;

                if (fullMessageString == DeadBeefTxHashJsonArr)
                {
                    Console.WriteLine("A reorg occurred. Re-seeding the whole flow graph with the next received block...");
                    needsFullImport = true;
                }
                else
                {
                    var transactionHashesInLastBlock = JsonConvert.DeserializeObject<string[]>(fullMessageString);
                    if (transactionHashesInLastBlock.Length > 0)
                    {
                        Console.WriteLine($"New block. First Tx: {transactionHashesInLastBlock[0]}");
                        
                        var blockLookupSql = $"select block_number from transaction_2 where hash = '{transactionHashesInLastBlock[0]}';";
                        
                        await using var connection = new NpgsqlConnection(IndexerDbConnectionString);
                        await connection.OpenAsync();

                        var cmd = new NpgsqlCommand(blockLookupSql, connection);
                        var blockNo = Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
                        if (blockNo == 0)
                        {
                            throw new Exception($"Couldn't find the block no. by tx hash '{transactionHashesInLastBlock[0]}'");
                        }

                        currentIncrementalBlock = blockNo;
                    }
                }

                if (needsFullImport)
                {
                    await BinaryExport.ExportCapacityGraph(IndexerDbConnectionString, CapacityGraphFile);   
                    var client = new HttpClient();
                    var requestJsonBody = "{\n    \"id\":\"" + DateTime.Now.Ticks + "\", \n    \"method\": \"load_edges_binary\", \n    \"params\": {\n        \"file\": \"" + CapacityGraphFile + "\"\n    }\n}";
                    var content = new StringContent(requestJsonBody, Encoding.UTF8, "application/json");
                    var rpcResult = client.PostAsync(PathfinderUrl, content).Result;
                    
                    Console.WriteLine(rpcResult);
                    needsFullImport = false;
                    currentIncrementalBlock = 0L;
                    lastIncrementalBlock = 0L;
                }
                else
                {
                    
                    Console.WriteLine($"Simulating incremental import for {currentIncrementalBlock - lastIncrementalBlock} blocks ...");
                    lastIncrementalBlock = currentIncrementalBlock;
                }
            }
        }
    }
}