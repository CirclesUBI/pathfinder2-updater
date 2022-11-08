namespace CirclesUBI.PathfinderUpdater.Indexer;

public sealed class NewBlockMessage
{
    public string[] TransactionHashes { get; }
    
    public NewBlockMessage(string[] transactionHashes)
    {
        TransactionHashes = transactionHashes;
    }
}