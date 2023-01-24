namespace CirclesUBI.PathfinderUpdater.Updater;

public class HealthMonitor
{
    public readonly string Name;
    public bool IsHealthy => DateTime.Now.Subtract(_lastUpdate).TotalSeconds < _problemThresholdSeconds;
    
    private readonly int _problemThresholdSeconds;
    private DateTime _lastUpdate = DateTime.Now;
    
    public HealthMonitor(string name, int problemThresholdSeconds)
    {
        Name = name;
        _problemThresholdSeconds = problemThresholdSeconds;
    }
    
    public void KeepAlive()
    {
        _lastUpdate = DateTime.Now;
    }
}