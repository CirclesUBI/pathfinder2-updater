using Npgsql;

namespace CirclesUBI.PathfinderUpdater;

public static class Block
{
    public static async Task<long> FindByTransactionHash(string connectionString, string transactionHash)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var cmd = new NpgsqlCommand(Queries.BlockByTransactionHash(transactionHash), connection);
        return (long)(await cmd.ExecuteScalarAsync() 
                      ?? throw new InvalidOperationException(
                          "Couldn't find a block that contains the supplied transaction or an other error occurred."));
    }
}