namespace CirclesUBI.PathfinderUpdater;

public static class Queries
{
    public static string BlockByTransactionHash(string txHash)
    {
        return $"select block_number from transaction_2 where hash = '{txHash}';";
    }

    // public const string CapacityGraph = @"
    //             select token_holder, can_send_to, token_owner, capacity::text
    //             from crc_capacity_graph
    //             where capacity > 0;";

    public const string CapacityGraph = @"
                select ""from"", ""to"", token_owner, capacity::text
                from crc_capacity_graph_2
                where capacity > 0;";

    public static string GetChanges(long sinceBlock)
    {
        return $"select token_holder, can_send_to, token_owner, capacity::text from get_capacity_changes_since_block({sinceBlock});";
    }
}