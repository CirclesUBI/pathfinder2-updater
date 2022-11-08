using System.Diagnostics;
using System.Text;

namespace CirclesUBI.PathfinderUpdater.PathfinderRpc;

public class RpcEndpoint
{
    private readonly string _rpcUrl;
    public RpcEndpoint(string rpcUrl)
    {
        _rpcUrl = rpcUrl;
    }
    
    public async Task<(string resultBody, Stopwatch spentTime)> Call(string requestJsonBody)
    {
        var requestStopWatch = new Stopwatch();
        requestStopWatch.Start();

        var content = new StringContent(requestJsonBody, Encoding.UTF8, "application/json");

        using var client = new HttpClient();
        using var rpcResult = client.PostAsync(_rpcUrl, content).Result;
        var responseStream = await rpcResult.Content.ReadAsStreamAsync();
        using var streamReader = new StreamReader(responseStream);
        var responseBody = await streamReader.ReadToEndAsync();
        
        requestStopWatch.Stop();

        return (responseBody, requestStopWatch);
    }
}