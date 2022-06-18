using System.Diagnostics;
using System.Numerics;

namespace CirclesLand.Pathfinder.ImportExport;


public class BinaryExporter
{
    private Stream? _f;
    private Dictionary<string, uint>? _addressIndex;

    public void Export(Db db, Stream stream)
    {
        _f = stream;
        
        WriteUInt32(db.Block);
        
        WriteUInt32((uint)db.Addresses.Length);
        WriteAddresses(db.Addresses);
        
        _addressIndex = new Dictionary<string, uint>(db.Addresses.Length + 1);
        for (uint i = 0; i < db.Addresses.Length; i++)
        {
            _addressIndex.Add(db.Addresses[i], i);
        }
        
        WriteUInt32((uint)db.Safes.Count);
        WriteSafes(db.Safes);
        
        _f.Close();
    }

    private void WriteSafes(Dictionary<string,Safe> dbSafes)
    {
        foreach (var safe in dbSafes)
        {
            Debug.Assert(_addressIndex != null, nameof(_addressIndex) + " != null");
            Debug.Assert(_addressIndex.ContainsKey(safe.Value.SafeAddress));
            if (safe.Value.TokenAddress != null)
            {
                Debug.Assert(_addressIndex.ContainsKey(safe.Value.TokenAddress));
            }

            WriteUInt32(_addressIndex[safe.Value.SafeAddress]);
            WriteUInt32(_addressIndex[safe.Value.TokenAddress ?? "0000000000000000000000000000000000000000"]);
            WriteUInt32((UInt32)safe.Value.Balances.Count);
            WriteBalances(safe.Value.Balances);
            WriteUInt32((UInt32)safe.Value.LimitPercentage.Count);
            WriteLimits(safe.Value.LimitPercentage);
            WriteBoolean(safe.Value.Organization);
        }
    }

    private void WriteBoolean(bool value)
    {
        Debug.Assert(_f != null, nameof(_f) + " != null");
        _f.Write(BitConverter.GetBytes(value));
    }

    private void WriteLimits(Dictionary<string,uint> limitPercentage)
    {
        foreach (var limit in limitPercentage)
        {
            WriteLimit(limit);
        }
    }

    private void WriteLimit(KeyValuePair<string,uint> limit)
    {
        Debug.Assert(_addressIndex != null, nameof(_addressIndex) + " != null");
        
        WriteUInt32(_addressIndex[limit.Key]);
        WriteUInt32(limit.Value);
    }

    private void WriteBalances(Dictionary<string,BigInteger> valueBalances)
    {
        foreach (var balance in valueBalances)
        {
            WriteBalance(balance);
        }
    }

    private void WriteBalance(KeyValuePair<string,BigInteger> balance)
    {
        Debug.Assert(_addressIndex != null, nameof(_addressIndex) + " != null");
        
        WriteUInt32(_addressIndex[balance.Key]);
        WriteUInt256(balance.Value);
    }

    private void WriteUInt256(BigInteger balanceValue)
    {
        Debug.Assert(_f != null, nameof(_f) + " != null");
        
        var bytes = balanceValue.ToByteArray(true);
        _f.WriteByte((byte) bytes.Length);
        for (int i = bytes.Length - 1; i >= 0; i--)
        {
            _f.WriteByte(bytes[i]);   
        }
    }

    private void WriteAddresses(string[] addresses)
    {
        foreach (var address in addresses)
        {
            WriteAddress(address);
        }
    }

    private void WriteAddress(string address)
    {
        Debug.Assert(_f != null, nameof(_f) + " != null");
        _f.Write(Convert.FromHexString(address));
    }

    private void WriteUInt32(UInt32 value)
    {
        Debug.Assert(_f != null, nameof(_f) + " != null");
        var buffer = BitConverter.GetBytes(value);
        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            _f.WriteByte(buffer[i]);   
        }
    }
}