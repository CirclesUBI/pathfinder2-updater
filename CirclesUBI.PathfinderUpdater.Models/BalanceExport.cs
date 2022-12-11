using System.Text;

namespace CirclesUBI.Pathfinder.Models;

public class BalanceExport
{
    public string[] Addresses { get; }
    public string[] OrganizationAddress { get; }
    public TrustEdge[] Edges { get; }
    public Balance[] Balances { get; }

    public BalanceExport(
        string[] addresses, 
        string[] organizationAddress, 
        TrustEdge[] edges, 
        Balance[] balances)
    {
        Addresses = addresses;
        OrganizationAddress = organizationAddress;
        Edges = edges;
        Balances = balances;
    }

    public void Serialize(Stream stream)
    {
        /*
            AddressCount: UInt32
            Addresses: Byte[20] x N
            OrganizationCount: UInt32
            OrganizationAddress: UInt32 x N
            TrustEdgeCount: UInt32
            Edges: {
              UserAddress: UInt32
              CanSendToAddress:UInt32
              Limit: Byte
            }[]
            BalanceCount: UInt32
            Balances: {
              UserAddress: UInt32
              TokenAddress:UInt32
              Balance: UInt256
            }[]
        */
        stream.Write(BitConverter.GetBytes(Addresses.Length));
        foreach (var s in Addresses)
        {
            stream.Write(Encoding.UTF8.GetBytes(s));
        }
        
        stream.Write(BitConverter.GetBytes(OrganizationAddress.Length));
        foreach (var s in OrganizationAddress)
        {
            stream.Write(Encoding.UTF8.GetBytes(s));
        }
        
        stream.Write(BitConverter.GetBytes(Convert.ToUInt32(Edges.Length)));
        foreach (var s in Edges)
        {
            s.Serialize(stream);
        }
        
        stream.Write(BitConverter.GetBytes(Convert.ToUInt32(Balances.Length)));
        foreach (var s in Balances)
        {
            s.Serialize(stream);
        }
    }
}