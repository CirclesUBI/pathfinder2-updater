using System.Diagnostics;
using System.Numerics;
using CirclesUBI.Pathfinder.Models;

namespace CirclesUBI.PathfinderUpdater;

public static class CapacityGraph
{
    public static async Task<(
        IEnumerable<IncrementalExportRow> result,
        TimeSpan queryDuration,
        TimeSpan downloadDuration,
        TimeSpan totalDuration
        )> SinceBlock(string connectionString, long sinceBlockNo)
    {
        using var capacityEdgeReader = new CapacityEdgeReader(connectionString, Queries.GetChanges(sinceBlockNo));

        var queryStopWatch = new Stopwatch();
        var totalStopWatch = new Stopwatch();
        totalStopWatch.Start();

        var edgeIterator = await capacityEdgeReader.ReadCapacityEdges(
            queryStopWatch);

        var rows = new List<IncrementalExportRow>();

        foreach (var edge in edgeIterator)
        {
            rows.Add(new IncrementalExportRow
            (
                from: edge.senderAddress,
                to: edge.receiverAddress,
                tokenOwner: edge.tokenOwnerAddress,
                capacity: edge.capacity.ToString()
            ));
        }

        totalStopWatch.Stop();
        
        return (rows, queryStopWatch.Elapsed, totalStopWatch.Elapsed - queryStopWatch.Elapsed, totalStopWatch.Elapsed);
    }

    public static async Task<(
        TimeSpan queryDuration, 
        TimeSpan downloadDuration,
        TimeSpan writeEdgesDuration,
        TimeSpan writeNodesDuration,
        TimeSpan concatDumpFilesDuration,
        TimeSpan totalDuration)> ToBinaryFile(string connectionString, string outputFile)
    {
        /*
            uint32: number_of_addresses
            [ 
                bytes20: address 
            ] * number_of_addresses
            uint32: number_of_edges
            [
                uint32: from_address_index
                uint32: to_address_index
                uint32: token_owner_address_index
                uint256: capacity
            ] * number_of_edges
            
         */
        
        using var capacityEdgeReader = new CapacityEdgeReader(connectionString, Queries.CapacityGraph);

        var queryStopWatch = new Stopwatch();
        var totalStopWatch = new Stopwatch();
        totalStopWatch.Start();

        var edgeIterator = await capacityEdgeReader.ReadCapacityEdges(
            queryStopWatch);

        await using var nodesStream = File.Create(outputFile);
        await using var edgesStream = File.Create(outputFile + ".edges");
        
        var addresses = new Dictionary<string, int>();

        WriteUInt32(edgesStream, 0);
        uint edgeCount = 0;
        
        var writeEdgeStopwatch = new Stopwatch();
        
        foreach (var edge in edgeIterator)
        {
            var senderAddressId = addresses.TryAdd(edge.senderAddress, addresses.Count)
                ? addresses.Count - 1
                : addresses[edge.senderAddress];

            writeEdgeStopwatch.Start();
            WriteUInt32(edgesStream, (uint)senderAddressId);
            writeEdgeStopwatch.Stop();

            var receiverAddressId = addresses.TryAdd(edge.receiverAddress, addresses.Count)
                ? addresses.Count - 1
                : addresses[edge.receiverAddress];

            writeEdgeStopwatch.Start();
            WriteUInt32(edgesStream, (uint)receiverAddressId);
            writeEdgeStopwatch.Stop();

            var tokenOwnerAddressId = addresses.TryAdd(edge.tokenOwnerAddress, addresses.Count)
                ? addresses.Count - 1
                : addresses[edge.tokenOwnerAddress];

            writeEdgeStopwatch.Start();
            WriteUInt32(edgesStream, (uint)tokenOwnerAddressId);
            WriteUInt256(edgesStream, edge.capacity);
            writeEdgeStopwatch.Stop();

            edgeCount++;
        }

        writeEdgeStopwatch.Start();
        edgesStream.Position = 0;
        WriteUInt32(edgesStream, edgeCount);
        edgesStream.Close();
        writeEdgeStopwatch.Stop();

        var writeNodesStopwatch = new Stopwatch();
        writeNodesStopwatch.Start();
        
        WriteUInt32(nodesStream, (uint)addresses.Count);
        
        foreach (var kvp in addresses)
        {
            WriteAddress(nodesStream, kvp.Key);
        }
        
        nodesStream.Close();
        writeNodesStopwatch.Stop();

        var concatFileStopwatch = new Stopwatch();
        concatFileStopwatch.Start();
        
        await using var input = File.OpenRead(outputFile + ".edges");
        await using var output = new FileStream(outputFile, FileMode.Append, FileAccess.Write, FileShare.None);
        
        await input.CopyToAsync(output);
        
        File.Delete(outputFile + ".edges");
        
        concatFileStopwatch.Stop();
        
        return (queryStopWatch.Elapsed, 
                totalStopWatch.Elapsed - queryStopWatch.Elapsed - writeEdgeStopwatch.Elapsed,
                writeEdgeStopwatch.Elapsed,
                writeNodesStopwatch.Elapsed,
                concatFileStopwatch.Elapsed,
                totalStopWatch.Elapsed);
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
        stream.WriteByte((byte)bytes.Length);
        for (int i = bytes.Length - 1; i >= 0; i--)
        {
            stream.WriteByte(bytes[i]);
        }
    }
}