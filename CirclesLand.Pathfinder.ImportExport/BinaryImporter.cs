using System.Diagnostics;
using System.Numerics;

namespace CirclesLand.Pathfinder.ImportExport;

public class BinaryImporter
{
    private Stream? _stream;

    public Db Import(Stream stream)
    {
        _stream = stream;

        var blockCount = ReadUInt32();
        var addressCount = ReadUInt32();
        
        var addresses = new string[addressCount];
        for (uint i = 0; i < addressCount; i++)
        {
            addresses[i] = ReadAddress();
        }

        var safeCount = ReadUInt32();
        var safes = new Safe[safeCount];
        for (uint i = 0; i < safeCount; i++)
        {
            safes[i] = ReadSafe(addresses);
        }

        return new Db
        {
            Addresses = addresses,
            Block = blockCount,
            Safes = safes.ToDictionary(o => o.SafeAddress, o => o)
        };
    }

    private Safe ReadSafe(string[] addresses)
    {
        var safeAddressIndex = ReadUInt32();
        var safeAddress = addresses[safeAddressIndex];

        var tokenAddressIndex = ReadUInt32();
        var tokenAddress = addresses[tokenAddressIndex];

        var balancesCount = ReadUInt32();
        var balances = Enumerable.Range(0, (int) balancesCount)
            .Select(_ => ReadBalance(addresses))
            .ToArray();

        var limitsCount = ReadUInt32();
        var limits = Enumerable.Range(0, (int) limitsCount)
            .Select(_ => ReadLimit(addresses))
            .ToArray();

        var isOrganization = ReadBoolean();

        return new Safe(safeAddress, tokenAddress, isOrganization)
        {
            Balances = new Dictionary<string, BigInteger>(balances),
            LimitPercentage = new Dictionary<string, UInt32>(limits),
        };
    }

    private KeyValuePair<string, UInt32> ReadLimit(string[] addresses)
    {
        var sendToAddressIndex = ReadUInt32();
        var sendToAddress = addresses[sendToAddressIndex];
        var limit = ReadUInt32();
        return new(sendToAddress, limit);
    }

    private KeyValuePair<string, BigInteger> ReadBalance(string[] addresses)
    {
        var tokenAddressIndex = ReadUInt32();
        var tokenAddress = addresses[tokenAddressIndex];

        var balance = ReadUInt256();
        return new(tokenAddress, balance);
    }

    private UInt32 ReadUInt32()
    {
        Debug.Assert(_stream != null, nameof(_stream) + " != null");
        
        var buffer = new byte[4];
        var readBytes = _stream.Read(buffer);
        Debug.Assert(readBytes == 4);
        
        return BitConverter.ToUInt32(buffer.Reverse().ToArray());
    }

    private BigInteger ReadUInt256()
    {
        Debug.Assert(_stream != null, nameof(_stream) + " != null");
        
        var lengthBuffer = new byte[1];
        var readBytes = _stream.Read(lengthBuffer);
        Debug.Assert(readBytes == 1);

        var length = lengthBuffer[0];
        Debug.Assert(length is <= 32 and > 0);

        var dataBuffer = new byte[length];
        readBytes = _stream.Read(dataBuffer);
        Debug.Assert(readBytes == length);

        return new BigInteger(dataBuffer.Reverse().ToArray(), true);
    }

    private string ReadAddress()
    {
        Debug.Assert(_stream != null, nameof(_stream) + " != null");
        
        var buffer = new byte[20];
        var readBytes = _stream.Read(buffer);
        Debug.Assert(readBytes == 20);
        return Convert.ToHexString(buffer);
    }

    private bool ReadBoolean()
    {
        Debug.Assert(_stream != null, nameof(_stream) + " != null");
        
        var buffer = new byte[1];
        var readBytes = _stream.Read(buffer);
        Debug.Assert(readBytes == 1);
        return BitConverter.ToBoolean(buffer);
    }
}