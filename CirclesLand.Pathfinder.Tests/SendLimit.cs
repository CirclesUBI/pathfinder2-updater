using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using CirclesLand.Pathfinder.Tests.Model;
using NUnit.Framework;

namespace CirclesLand.Pathfinder.Tests;

public class SendLimit
{
    [Test]
    public async Task CalculateMaxTransferableAmount()
    {
        var signups = Signups.Load();
        var balances = Balances.Load();
        var trusts = Trusts.Load();

        await Task.WhenAll(signups, balances, trusts);

        var edges = new List<(string From, string To, BigInteger capacity)>();

        var csv = File.CreateText("/home/daniel/Desktop/edges.csv");
        csv.WriteLine("from;to;capacity");
        
        foreach (var from in signups.Result.All)
        {
            if (!trusts.Result.BySender.ContainsKey(from))
            {
                continue;
            }
            
            foreach (var to in trusts.Result.BySender[from])
            {
                var capacity = CalculateEdgeCapacity(signups.Result, balances.Result, from, to.Key, to.Value.Token, to.Value.Limit);
                if (capacity > 0)
                {
                    csv.WriteLine($"{from};{to.Key};{capacity}");
                    edges.Add((from, to.Key, capacity));
                }
            }
        }

        Console.WriteLine($"total organizations: {signups.Result.Organizations.Count}");
        Console.WriteLine($"total people: {signups.Result.People.Count}");
        Console.WriteLine($"total safes: {balances.Result.BySafeAndToken.Count}");
        Console.WriteLine($"total tokens: {balances.Result.ByTokenAndSafe.Count}");
        Console.WriteLine($"total edges: {edges.Count}");
        
        csv.Flush();
        csv.Close();
    }

    private BigInteger CalculateEdgeCapacity(
        Signups signups
        , Balances balances
        , string from
        , string to
        , string token
        , int limit)
    {
        /*
        Safe const* senderSafe = safeMaybe(_user);
        Safe const* receiverSafe = safeMaybe(_canSendTo);
        if (!senderSafe || !receiverSafe)
            return {};

        uint32_t sendToPercentage = senderSafe->sendToPercentage(_canSendTo);
        if (sendToPercentage == 0)
            return {};
            
        if (receiverSafe->organization)
            return senderSafe->balance(senderSafe->tokenAddress);
        */
        var receiverIsOrganization = signups.Organizations.Contains(to);
        if (receiverIsOrganization)
        {
            return balances.BySafeAndToken[from][token];
        }

        /*
        Token const* receiverToken = tokenMaybe(receiverSafe->tokenAddress);
        if (!receiverToken)
            return {};
         */
        if (!signups.TokensByAddress.ContainsKey(to))
        {
            return BigInteger.Zero;
        }

        /*
        Int receiverBalance = receiverSafe->balance(senderSafe->tokenAddress);
         */
        var receiverBalances = balances.BySafeAndToken[to];
        var sendersOwnToken = signups.TokensByOwner[from];
        var receiversOwnToken = signups.TokensByOwner[to];
        
        var receiversHoldingsOfSendersTokens = receiverBalances[sendersOwnToken.Address];
        var receiversHoldingsOfReceiversTokens = receiverBalances[receiversOwnToken.Address];
        
        BigInteger amount = receiversHoldingsOfReceiversTokens * limit / 100;
        amount = amount < receiversHoldingsOfSendersTokens 
            ? BigInteger.Zero 
            : amount - receiversHoldingsOfSendersTokens;
        
        return BigInteger.Min(amount, balances.BySafeAndToken[from][sendersOwnToken.Address]);
    }
}