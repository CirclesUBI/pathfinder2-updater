using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Npgsql;

namespace CirclesLand.Pathfinder.Tests.Model;

public class Balances
{
    public Dictionary<string, Dictionary<string, BigInteger>> ByTokenAndSafe { get; private init; } = null!;
    public Dictionary<string, Dictionary<string, BigInteger>> BySafeAndToken { get; private init; } = null!;

    private Balances()
    {
    }

    public static async Task<Balances> Load()
    {
        await using var connection = new NpgsqlConnection(Settings.IndexerConnectionString);
        await connection.OpenAsync();

        var balanceReader = await new NpgsqlCommand(
                @"select token, safe_address, balance::text
                  from cache_crc_balances_by_safe_and_token
                  where balance > 0;",
                connection)
            .ExecuteReaderAsync();

        var balances = new Balances
        {
            BySafeAndToken = new Dictionary<string, Dictionary<string, BigInteger>>(100000),
            ByTokenAndSafe = new Dictionary<string, Dictionary<string, BigInteger>>(100000)
        };
        
        while (balanceReader.Read())
        {
            var tokenAddress = balanceReader.GetString(0).Substring(2);
            var safeAddress = balanceReader.GetString(1).Substring(2);
            var balance = BigInteger.Parse(balanceReader.GetString(2));

            if (!balances.ByTokenAndSafe.TryGetValue(tokenAddress, out var safeBalances))
            {
                balances.ByTokenAndSafe.Add(tokenAddress, new Dictionary<string, BigInteger> {{safeAddress, balance}});
            }
            else
            {
                safeBalances.Add(safeAddress, balance);
            }

            if (!balances.BySafeAndToken.TryGetValue(safeAddress, out var tokenBalances))
            {
                balances.BySafeAndToken.Add(safeAddress, new Dictionary<string, BigInteger> {{tokenAddress, balance}});
            }
            else
            {
                tokenBalances.Add(tokenAddress, balance);
            }
        }

        return balances;
    }
}