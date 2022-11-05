using System.Net.WebSockets;
using System.Numerics;
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
    
    public static async Task Main(string[] args)
    {
        IndexerDbConnectionString = args[0];
        IndexerWebsocketUrl = args[1];
        CapacityGraphFile = args[2];
        PathfinderUrl = args[3];
        
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
                    Console.WriteLine($"Incremental update starting from block: {currentIncrementalBlock}");
                }

                if (needsFullImport)
                {
                    await BinaryExport.ExportCapacityGraph(IndexerDbConnectionString, CapacityGraphFile);   
                    var client = new HttpClient();
                    var requestJsonBody = "{\n    \"id\":\"" + DateTime.Now.Ticks + "\", \n    \"method\": \"load_edges_binary\", \n    \"params\": {\n        \"file\": \"" + CapacityGraphFile + "\"\n    }\n}";
                    Console.WriteLine(requestJsonBody);
                    var content = new StringContent(requestJsonBody, Encoding.UTF8, "application/json");
                    var rpcResult = client.PostAsync(PathfinderUrl, content).Result;
                    Console.WriteLine(rpcResult);
                    
                    needsFullImport = false;
                    lastIncrementalBlock = currentIncrementalBlock;
                }
                else
                {
                    var getChangesSql = "select token_holder, can_send_to, token, capacity::text from get_capacity_changes_since_block(" +
                                        (lastIncrementalBlock + 1) + ");";
                    await using var connection = new NpgsqlConnection(IndexerDbConnectionString);
                    await connection.OpenAsync();

                    await using var capacityReader = await new NpgsqlCommand(getChangesSql, connection).ExecuteReaderAsync();
                    var requestJsonBody = GenerateIncrementalUpdateJson(capacityReader);
                    Console.WriteLine(requestJsonBody);
                    var client = new HttpClient();
                    var content = new StringContent(requestJsonBody, Encoding.UTF8, "application/json");
                    var rpcResult = client.PostAsync(PathfinderUrl, content).Result;
                    Console.WriteLine(rpcResult);
                    
                    lastIncrementalBlock = currentIncrementalBlock;
                }
            }
        }
    }

    private static string GenerateIncrementalUpdateJson(NpgsqlDataReader reader)
    {
        var rows = new List<IncrementalUpdateRow>();

        while (reader.Read())
        {
            var sender = reader.GetString(0).Substring(2);
            var receiver = reader.GetString(1).Substring(2);
            var token = reader.GetString(2).Substring(2);
            var capacity = reader.GetString(3);

            var fuck = capacity.IndexOf(".", StringComparison.Ordinal);
            if (fuck > -1)
            {
                capacity = capacity.Substring(0, fuck);
            }
            if (!BigInteger.TryParse(capacity, out var capacityBigInteger))
            {
            }
            
            rows.Add(new IncrementalUpdateRow
            {
                from = sender,
                to = receiver,
                token = token,
                capacity = capacityBigInteger.ToString()
            });
        }

        // update_edges([{from, to, token, capacity}, ...])
        var requestJsonBody = "{\n    \"id\":\"" + DateTime.Now.Ticks +
                              "\", \n    \"method\": \"update_edges\", \n    \"params\": " +
                              JsonConvert.SerializeObject(rows) + "\n}";

        return requestJsonBody;
    }

    class IncrementalUpdateRow
    {
        public string from { get; set; }
        public string to { get; set; }
        public string token { get; set; }
        public string capacity { get; set; }
    }
}