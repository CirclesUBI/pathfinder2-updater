using System.Net.WebSockets;
using System.Text;

namespace CirclesLand.PathfinderExport.Updater;

public static class Program
{
    public static string IndexerDbConnectionString { get; set; } =
        "Server=localhost;Port=5429;Database=indexer;User ID=postgres;Password=postgres;Command Timeout=240";
    public static string IndexerWebsocketUrl { get; set; } = "wss://index.circles.land";
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
                
                Console.WriteLine(Encoding.UTF8.GetString(fullMessageBytes));
                
                await BinaryExport.ExportCapacityGraph(IndexerDbConnectionString, CapacityGraphFile);
                
                var client = new HttpClient();
                var requestJsonBody = "{\n    \"id\":\"" + DateTime.Now.Ticks + "\", \n    \"method\": \"load_edges_binary\", \n    \"params\": {\n        \"file\": \"" + CapacityGraphFile + "\"\n    }\n}";
                var content = new StringContent(requestJsonBody, Encoding.UTF8, "application/json");
                var rpcResult = client.PostAsync(PathfinderUrl, content).Result;
                
                Console.WriteLine(rpcResult);
            }
        }
    }
}