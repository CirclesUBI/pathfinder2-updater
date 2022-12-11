using System.Data;
using System.Diagnostics;
using CirclesUBI.Pathfinder.Models;
using Npgsql;

namespace CirclesUBI.PathfinderUpdater;

public class TrustReader : IDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly string _queryString;
    private readonly Dictionary<string, uint> _addressIndexes;

    public TrustReader(string connectionString, string queryString, Dictionary<string, uint> addressIndexes)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
        
        _queryString = queryString;
        _addressIndexes = addressIndexes;
    }

    public async Task<IEnumerable<TrustEdge>> ReadTrustEdges(
        Stopwatch? queryStopWatch = null)
    {
        queryStopWatch?.Start();
        
        var cmd = new NpgsqlCommand(_queryString, _connection); 
        var capacityReader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

        queryStopWatch?.Stop();
        
        return CreateTrustEdgeReader(capacityReader);
    }

    private IEnumerable<TrustEdge> CreateTrustEdgeReader(NpgsqlDataReader capacityReader)
    {
        while (true)
        {
            var end = !capacityReader.Read();
            if (end)
            {
                break;
            }

            var user = capacityReader.GetString(0).Substring(2);
            if (!_addressIndexes.TryGetValue(user, out var userAddressIdx))
            {
                Console.WriteLine($"Warning: Address {user} doesn't have an address index.");
                continue;
            }
            var canSendTo = capacityReader.GetString(1).Substring(2);
            if (!_addressIndexes.TryGetValue(canSendTo, out var canSendToAddressIdx))
            {
                Console.WriteLine($"Warning: Address {user} doesn't have an address index.");
                continue;
            }
            var limit = (byte)capacityReader.GetInt32(2);

            yield return new TrustEdge(userAddressIdx, canSendToAddressIdx, limit);
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}