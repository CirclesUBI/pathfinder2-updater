using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;

namespace CirclesUBI.PathfinderUpdater.Indexer;

public class IndexerSubscription : IDisposable
{
    private readonly ClientWebSocket _clientWebSocket;
    private readonly string _indexerUrl;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public event EventHandler<IndexerSubscriptionEventArgs>? SubscriptionEvent;

    public IndexerSubscription(string indexerUrl)
    {
        _clientWebSocket = new ClientWebSocket();
        _indexerUrl = indexerUrl;
    }

    public async Task Unsubscribe()
    {
        _cancellationTokenSource.Cancel();
        await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Goodbye", CancellationToken.None);
    }

    public void Subscribe()
    {
        Task.Factory.StartNew(async () =>
        {
            try
            {
                await _clientWebSocket.ConnectAsync(new Uri(_indexerUrl), _cancellationTokenSource.Token);

                while (_clientWebSocket.State == WebSocketState.Open)
                {
                    var blockUpdateMessage = await ReceiveWsMessage();

                    var transactionHashesInLastBlock = JsonConvert.DeserializeObject<string[]>(blockUpdateMessage);
                    if (transactionHashesInLastBlock == null)
                    {
                        throw new Exception($"Received an invalid block update via websocket: {blockUpdateMessage}");
                    }

                    SubscriptionEvent?.Invoke(this,
                        new IndexerSubscriptionEventArgs(
                            new NewBlockMessage(transactionHashesInLastBlock)));
                }
            }
            catch (Exception exception)
            {
                SubscriptionEvent?.Invoke(this, new IndexerSubscriptionEventArgs(exception));
                throw;
            }
        }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private async Task<string> ReceiveWsMessage()
    {
        var receiving = true;
        var buffer = new byte[4096];
        var mem = new Memory<byte>(buffer);
        var fullMessageBuffer = new List<byte[]>();

        while (receiving)
        {
            var result = await _clientWebSocket.ReceiveAsync(mem, _cancellationTokenSource.Token);
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

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _clientWebSocket.Dispose();
    }
}