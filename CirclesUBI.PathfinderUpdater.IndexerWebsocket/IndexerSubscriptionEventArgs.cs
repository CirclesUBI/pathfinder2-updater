namespace CirclesUBI.PathfinderUpdater.Indexer;

public sealed class IndexerSubscriptionEventArgs : EventArgs
{
    public Exception? Error { get; }
    public NewBlockMessage? Message { get; }

    public IndexerSubscriptionEventArgs(Exception error)
    {
        Error = error;
    }
    public IndexerSubscriptionEventArgs(NewBlockMessage message)
    {
        Message = message;
    }
}