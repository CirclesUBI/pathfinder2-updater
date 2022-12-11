using System.Data;
using System.Diagnostics;
using CirclesUBI.Pathfinder.Models;
using Npgsql;

namespace CirclesUBI.PathfinderUpdater;

public class BalanceReader : IDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly string _queryString;
    private readonly Dictionary<string, uint> _addressIndexes;

    public BalanceReader(string connectionString, string queryString, Dictionary<string, uint> addressIndexes)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
        
        _queryString = queryString;
        _addressIndexes = addressIndexes;
    }

    public async Task<IEnumerable<Balance>> ReadBalances(
        Stopwatch? queryStopWatch = null)
    {
        queryStopWatch?.Start();
        
        var cmd = new NpgsqlCommand(_queryString, _connection); 
        var capacityReader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

        queryStopWatch?.Stop();
        
        return CreateBalanceReader(capacityReader);
    }

    private IEnumerable<Balance> CreateBalanceReader(NpgsqlDataReader capacityReader)
    {
        while (true)
        {
            var end = !capacityReader.Read();
            if (end)
            {
                break;
            }

            var safeAddress = capacityReader.GetString(0).Substring(2);
            var tokenOwner = capacityReader.GetString(1).Substring(2);
            if (!_addressIndexes.TryGetValue(safeAddress, out var safeAddressIdx)
             || !_addressIndexes.TryGetValue(tokenOwner, out var tokenOwnerAddressIdx))
            {
                // Console.WriteLine($"Warning: Ignoring balance of address {safeAddress} with token {tokenOwner}");
                continue;
            }

            var balance = capacityReader.GetString(2);
            var balanceBn = CapacityEdgeReader.ParsePgBigInt(balance);

            yield return new Balance(safeAddressIdx, tokenOwnerAddressIdx, balanceBn);
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}