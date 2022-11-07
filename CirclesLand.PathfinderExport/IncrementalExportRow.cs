namespace CirclesLand.PathfinderExport;

public class IncrementalExportRow
{
    public string from { get; }
    public string to { get; }
    public string token_owner { get; }
    public string capacity { get; }

    public IncrementalExportRow(string from, string to, string tokenOwner, string capacity)
    {
        this.from = from;
        this.to = to;
        this.token_owner = tokenOwner;
        this.capacity = capacity;
    }
}