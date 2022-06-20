using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace CirclesLand.Pathfinder.Tests.Model;

public class Trust
{
    public string Sender { get; }
    public string Receiver { get; }
    public string Token { get; }
    public int Limit { get; }
    
    public Trust(string sender, string receiver, string token, int limit)
    {
        Sender = sender;
        Receiver = receiver;
        Token = token;
        Limit = limit;
    }
}

public class Trusts
{
    public Dictionary<string, Dictionary<string, Trust>> BySender { get; private init; } = null!;
    public Dictionary<string, Dictionary<string, Trust>> ByReceiver { get; private init; } = null!;

    private Trusts()
    {
    }

    public static async Task<Trusts> Load()
    {
        await using var connection = new NpgsqlConnection(Settings.IndexerConnectionString);
        await connection.OpenAsync();

        var trustReader = await new NpgsqlCommand(
                @"select can_send_to as receiver
                     , ""user"" as sender
                     , user_token as accepted_token
                     , ""limit""
                from cache_crc_current_trust
                where can_send_to != ""user""
                and user_token is not null
                and ""limit"" > 0;",
                connection)
            .ExecuteReaderAsync();

        var trusts = new Trusts
        {
            BySender = new Dictionary<string, Dictionary<string, Trust>>(100000),
            ByReceiver = new Dictionary<string, Dictionary<string, Trust>>(100000),
        };

        while (trustReader.Read())
        {
            var receiver = trustReader.GetString(0).Substring(2);
            var sender = trustReader.GetString(1).Substring(2);
            var token = trustReader.GetString(2).Substring(2);
            var limit = trustReader.GetInt32(3);

            var trustObj = new Trust(sender, receiver, token, limit);

            if (!trusts.BySender.TryGetValue(sender, out var possibleRecipients))
            {
                trusts.BySender.Add(sender, new Dictionary<string, Trust>{{receiver, trustObj}});
            }
            else
            {
                possibleRecipients.Add(receiver, trustObj);
            }

            if (!trusts.ByReceiver.TryGetValue(receiver, out var possibleSenders))
            {
                trusts.ByReceiver.Add(receiver, new Dictionary<string, Trust>{{sender, trustObj}});
            }
            else
            {
                possibleSenders.Add(sender, trustObj);
            }
        }

        return trusts;
    }
}