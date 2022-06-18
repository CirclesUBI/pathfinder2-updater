using System.Numerics;
using Npgsql;

namespace CirclesLand.Pathfinder.ImportExport;

public class IndexerImporter
{
    public async Task<Db> Import(string connectionString)
    {
        var blockNumber = ReadCurrentBlockNumber(connectionString);
        var safes = ReadSafes(connectionString);
        var limits = ReadLimits(connectionString);
        var balances = ReadBalances(connectionString);

        await Task.WhenAll(blockNumber, safes, limits, balances);

        var addresses = new HashSet<string> {"0000000000000000000000000000000000000000"};

        foreach (var safeKvp in safes.Result)
        {
            var safe = safeKvp.Value;
            addresses.Add(safe.SafeAddress);
            if (safe.TokenAddress != null)
            {
                addresses.Add(safe.TokenAddress);
            }

            if (!balances.Result.TryGetValue(safeKvp.Key, out var safeBalances))
            {
                continue;
            }
            foreach (var balance in safeBalances)
            {
                addresses.Add(balance.Item1);
                safe.Balances.Add(balance.Item1, balance.Item2);   
            }
        }

        foreach (var limitKvp in limits.Result)
        {;
            if (!safes.Result.TryGetValue(limitKvp.Key, out var safe))
            {
                continue;
            }
            addresses.Add(safe.SafeAddress);
            if (safe.TokenAddress != null)
            {
                addresses.Add(safe.TokenAddress);
            }

            foreach (var limit in limitKvp.Value)
            {
                addresses.Add(limit.Item1);
                safe.LimitPercentage.Add(limit.Item1, limit.Item2);
            }
        }
        
        var db = new Db
        {
            Addresses = addresses.ToArray(),
            Block = (uint)blockNumber.Result,
            Safes = safes.Result
        };

        return db;
    }
    
    private async Task<long> ReadCurrentBlockNumber(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        
        var blockNumber =
            (Int64) (await new NpgsqlCommand("select max(number) from block;", connection).ExecuteScalarAsync()
                     ?? throw new InvalidOperationException("select max(number) from block yielded 'null'"));
        return blockNumber;
    }
    
    private async Task<Dictionary<string, Safe>> ReadSafes(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var safeReader = await new NpgsqlCommand(
                "select \"user\", token, token is null as organization from crc_all_signups;", connection)
            .ExecuteReaderAsync();

        var safes = new Dictionary<string, Safe>(125000);
        while (safeReader.Read())
        {
            var safeAddressStr = safeReader.GetString(0).Substring(2);
            var tokenAddress = safeReader.IsDBNull(1)
                ? null
                : safeReader.GetString(1).Substring(2);
            
            safes.Add(safeAddressStr, new Safe(safeAddressStr, tokenAddress, safeReader.GetBoolean(2))
            {
                Balances = new Dictionary<string, BigInteger>(),
                LimitPercentage = new Dictionary<string, uint>()
            });
        }

        return safes;
    }

    private async Task<Dictionary<string, List<(string, uint)>>> ReadLimits(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var limitsReader = await new NpgsqlCommand(
                "select \"user\", can_send_to, \"limit\" from cache_crc_current_trust;", connection)
            .ExecuteReaderAsync();

        var result = new Dictionary<string, List<(string, uint)>>(1000000);
        while (limitsReader.Read())
        {
            var safeAddress = limitsReader.GetString(0).Substring(2);
            if (!result.TryGetValue(safeAddress, out var limitsOfSafe))
            {
                limitsOfSafe = new List<(string, uint)>();
                result.Add(safeAddress, limitsOfSafe);
            }
            limitsOfSafe.Add((limitsReader.GetString(1).Substring(2), (uint)limitsReader.GetInt32(2)));
        }

        return result;
    }

    private async Task<Dictionary<string, List<(string, BigInteger)>>> ReadBalances(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var balanceReader = await new NpgsqlCommand(
                "select safe_address, token, balance::text from cache_crc_balances_by_safe_and_token where balance > 0;", 
                connection)
            .ExecuteReaderAsync();

        var result = new Dictionary<string, List<(string, BigInteger)>>(1000000);
        while (balanceReader.Read())
        {
            var safeAddress = balanceReader.GetString(0).Substring(2);
            var tokenAddress = balanceReader.GetString(1).Substring(2);
            if (!result.TryGetValue(safeAddress, out var balancesOfSafe))
            {
                balancesOfSafe = new List<(string, BigInteger)>();
                result.Add(safeAddress, balancesOfSafe);
            }
            balancesOfSafe.Add((tokenAddress, BigInteger.Parse(balanceReader.GetString(2))));
        }

        return result;
    }
}