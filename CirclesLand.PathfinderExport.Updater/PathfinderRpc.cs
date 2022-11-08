using System.Diagnostics;
using System.Text;

namespace CirclesLand.PathfinderExport.Updater;

public static class PathfinderRpc
{
    public static async Task<(string resultBody, Stopwatch spentTime)> Call(string rpcUrl, string requestJsonBody)
    {
        var requestStopWatch = new Stopwatch();
        requestStopWatch.Start();

        var content = new StringContent(requestJsonBody, Encoding.UTF8, "application/json");

        using var client = new HttpClient();
        using var rpcResult = client.PostAsync(rpcUrl, content).Result;
        var responseStream = await rpcResult.Content.ReadAsStreamAsync();
        using var streamReader = new StreamReader(responseStream);
        var responseBody = await streamReader.ReadToEndAsync();
        
        requestStopWatch.Stop();

        return (responseBody, requestStopWatch);
    }
}