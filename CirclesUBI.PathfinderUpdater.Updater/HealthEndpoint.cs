using System.Net;

namespace CirclesUBI.PathfinderUpdater.Updater;

/**
 * Uses a HttpListener to respond to health checks.
 * When healthy returns 200 else 500. 
 */
public class HealthEndpoint
{
    readonly HttpListener  _listener = new ();
    private readonly HealthMonitor[] _healthMonitors;
    
    public HealthEndpoint(string listenAtUrl, IEnumerable<HealthMonitor> healthMonitors)
    {
        _listener.Prefixes.Add(listenAtUrl);
        _listener.Start();
        
        _healthMonitors = healthMonitors.ToArray();
        
        _listener.BeginGetContext(ListenerCallback, null);
    }

    private void ListenerCallback(IAsyncResult ar)
    {
        var context = _listener.EndGetContext(ar);  
        var response = context.Response;
        var content = "";
        
        try
        {
            var systemHealth = _healthMonitors.Select(o => o.IsHealthy).Aggregate((a, b) => a && b);
            if (systemHealth)
            {
                content = "Healthy";
                response.StatusCode = 200;
            }
            else
            {
                content = _healthMonitors
                    .Select(o => $"            <li>{o.Name}: {o.IsHealthy}</li>\n")
                    .Aggregate((a, b) => a + b);

                response.StatusCode = 500;
            }
        }
        catch (Exception e)
        {
            content = "Couldn't get health status. See logs for details.";
            Console.WriteLine(e.Message + "\r\n" + e.StackTrace);
            response.StatusCode = 500;
        }

        var responseString = @$"
<html>
    <body>
        <h1>pathfinder-updater health:</h1>
        <ul>
{content}
        </ul>
    </body>
</html>";
        
        var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();

        _listener.BeginGetContext(ListenerCallback, null);
    }
}