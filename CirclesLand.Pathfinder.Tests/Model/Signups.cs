using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace CirclesLand.Pathfinder.Tests.Model;

public class Token
{
    public string Address { get; }
    public string Owner { get; }

    public Token(string address, string owner)
    {
        Address = address;
        Owner = owner;
    }
}

public class Signups
{
    public HashSet<string> People { get; private init; } = null!;
    public HashSet<string> Organizations { get; private init; } = null!;
    public HashSet<string> All { get; private init; } = null!;
    public Dictionary<string, Token> TokensByAddress { get; private init; } = null!;
    public Dictionary<string, Token> TokensByOwner { get; private init; } = null!;

    private Signups()
    {
    }
    
    public static async Task<Signups> Load()
    {
        await using var connection = new NpgsqlConnection(Settings.IndexerConnectionString);
        await connection.OpenAsync();
        
        var signupReader = await new NpgsqlCommand("select \"user\", token from crc_all_signups;", connection)
            .ExecuteReaderAsync();

        var tokens = new Signups
        {
            All = new HashSet<string>(150000),
            People = new HashSet<string>(150000),
            Organizations = new HashSet<string>(10000),
            TokensByAddress = new Dictionary<string, Token>(100000),
            TokensByOwner = new Dictionary<string, Token>(100000)
        };

        while (signupReader.Read())
        {
            var safeAddress = signupReader.GetString(0).Substring(2);
            tokens.All.Add(safeAddress);

            if (signupReader.IsDBNull(1))
            {
                tokens.Organizations.Add(safeAddress);                
            }
            else
            {
                var token = signupReader.GetString(1).Substring(2);
                var tokenObj = new Token(token, safeAddress);

                tokens.People.Add(safeAddress);
                tokens.TokensByAddress.Add(token, tokenObj);
                tokens.TokensByOwner.Add(safeAddress, tokenObj);   
            }
        }

        return tokens;
    }
}