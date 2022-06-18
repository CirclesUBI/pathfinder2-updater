using System.Numerics;

namespace CirclesLand.Pathfinder.ImportExport;

/*
    Binary DB file layout:
    {
      4 byte 			(block no. uint32)
      4 byte			(address count uint32)
      N x {
        20 byte			    (address)
      }
      4 byte			(safe count uint32)
      N x {
        4 byte 			    (safe address index  uint32)
        4 byte 			    (token address index uint32)
        4 byte 			    (balance count uint32 uint32)
        N x {
          4 byte			    (token address index uint32)
          32 byte			    (balance uint256)
        }
        4 byte			    (limit uint32)
        N x {
          4 byte			    (sendTo address index uint32)
          4 byte			    (limit uint32)
        }
        1 byte			    (is organization flag)
      }
    }
    
    
    Binary Edge file layout:
    {
      4 byte			(address count uint32)
      N x {
        20 byte			    (address)
      }
      4 byte			(edge count uint32)
      N x {
        4 byte 			    (from address index uint32)
        4 byte 			    (to address index uint32)
        4 byte 			    (token address index uint32)
        32 byte			    (capacity uint32)
      }
    }
 */

public class Safe
{
    public readonly string SafeAddress;
    public readonly string? TokenAddress;
    public readonly bool Organization;
    public Dictionary<string, BigInteger> Balances = new();
    public Dictionary<string, UInt32> LimitPercentage = new();

    public Safe(string safeAddress, string? tokenAddress, bool organization)
    {
        SafeAddress = safeAddress;
        TokenAddress = tokenAddress;
        Organization = organization;
    }

    public override int GetHashCode()
    {
        return SafeAddress.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return SafeAddress == (obj as Safe)?.SafeAddress;
    }
}

public struct Db
{
    public Dictionary<string, Safe> Safes;
    public uint Block { get; set; }
    public string[] Addresses { get; set; }
}