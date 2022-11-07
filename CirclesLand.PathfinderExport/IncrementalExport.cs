using System.Numerics;
using Npgsql;

namespace CirclesLand.PathfinderExport;

public static class IncrementalExport
{
    public static async Task<IEnumerable<IncrementalExportRow>> ExportFromBlock(string connectionString, long blockNo)
    {
        var getChangesSql = Queries.GetChanges(blockNo);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var capacityReader = await new NpgsqlCommand(getChangesSql, connection).ExecuteReaderAsync();
        
        var rows = new List<IncrementalExportRow>();

        while (capacityReader.Read())
        {
            var sender = capacityReader.GetString(0).Substring(2);
            var receiver = capacityReader.GetString(1).Substring(2);
            var token_owner = capacityReader.GetString(2).Substring(2);
            var capacity = capacityReader.GetString(3);

            var fuck = capacity.IndexOf(".", StringComparison.Ordinal);
            if (fuck > -1)
            {
                capacity = capacity.Substring(0, fuck);
            }
            if (!BigInteger.TryParse(capacity, out var capacityBigInteger))
            {
            }
            
            rows.Add(new IncrementalExportRow
            (
                from: sender,
                to: receiver,
                tokenOwner: token_owner,
                capacity: capacityBigInteger.ToString()
            ));
        }

        return rows;
    }
}