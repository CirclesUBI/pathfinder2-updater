using System.Diagnostics;
using System.Numerics;
using Npgsql;

namespace CirclesLand.PathfinderExport;

public static class BinaryExport
{
    public static async Task ExportCapacityGraph(string connectionString, string outputFile)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var sw = new Stopwatch();
        sw.Start();
        
        await using var capacityReader = await new NpgsqlCommand(
                @"
                select token_holder, can_send_to, token, capacity::text
                from crc_capacity_graph
                where capacity > 0;",   
                connection)
            .ExecuteReaderAsync();

        sw.Stop();
        Console.WriteLine($"SQL Query took {sw.Elapsed.TotalMilliseconds} ms");
        sw.Reset();
        sw.Start();
        
        /*
            
            uint32: number_of_addresses
            [ bytes20: address ] * number_of_addresses
            uint32: number_of_edges
            [
            uint32: from_address_index
            uint32: to_address_index
            uint32: token_owner_address_index
            uint256: capacity
            ] * number_of_edges
            
         */

        var addressIndexMap = new Dictionary<string, UInt32>();
        UInt32 addressIndex = 0;
        
        var rows = new List<(UInt32, UInt32, UInt32, BigInteger)>();
        
        while (capacityReader.Read())
        {
            var sender = capacityReader.GetString(0).Substring(2);
            var receiver = capacityReader.GetString(1).Substring(2);
            var token = capacityReader.GetString(2).Substring(2);
            var capacity = capacityReader.GetString(3);

            if (!addressIndexMap.ContainsKey(sender))
            {
                addressIndexMap.Add(sender, addressIndex);
                addressIndex++;
            }

            if (!addressIndexMap.ContainsKey(receiver))
            {
                addressIndexMap.Add(receiver, addressIndex);
                addressIndex++;
            }

            if (!addressIndexMap.ContainsKey(token))
            {
                addressIndexMap.Add(token, addressIndex);
                addressIndex++;
            }

            var fuck = capacity.IndexOf(".", StringComparison.Ordinal);
            if (fuck > -1)
            {
                capacity = capacity.Substring(0, fuck);
            }
            if (!BigInteger.TryParse(capacity, out var capacityBigInteger))
            {
                
            }
            
            rows.Add((
                addressIndexMap[sender], 
                addressIndexMap[receiver], 
                addressIndexMap[token], 
                capacityBigInteger));
        }
        
        sw.Stop();
        Console.WriteLine($"Data preparation took {sw.Elapsed.TotalMilliseconds} ms");
        sw.Reset();
        sw.Start();

        if (File.Exists(outputFile))
        {
            File.Delete(outputFile);
        }

        await using (var fileStream = File.Create(outputFile))
        {
            WriteUInt32(fileStream, (UInt32)addressIndexMap.Count);
            foreach (var keyValuePair in addressIndexMap.OrderBy(o => o.Value))
            {
                WriteAddress(fileStream, keyValuePair.Key);
            }
            WriteUInt32(fileStream, (UInt32)rows.Count);
            foreach (var row in rows)
            {
                WriteUInt32(fileStream, row.Item1);
                WriteUInt32(fileStream, row.Item2);
                WriteUInt32(fileStream, row.Item3);
                WriteUInt256(fileStream, row.Item4);
            }
        }
        
        sw.Stop();
        Console.WriteLine($"Writing to file took {sw.Elapsed.TotalMilliseconds} ms");
    }
    
    private static void WriteAddress(Stream stream, string address)
    {
        stream.Write(Convert.FromHexString(address));
    }
    
    private static void WriteUInt32(Stream stream, UInt32 value)
    {
        var buffer = BitConverter.GetBytes(value);
        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            stream.WriteByte(buffer[i]);   
        }
    }
    
    private static void WriteUInt256(Stream stream, BigInteger balanceValue)
    {
        var bytes = balanceValue.ToByteArray(true);
        stream.WriteByte((byte) bytes.Length);
        for (int i = bytes.Length - 1; i >= 0; i--)
        {
            stream.WriteByte(bytes[i]);   
        }
    }
}