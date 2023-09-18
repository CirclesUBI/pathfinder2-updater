using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;

namespace CirclesUBI.PathfinderUpdater.ExportUtil;

public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("CirclesUBI.PathfinderUpdater.ExportUtil output_file connection_string");
            Console.WriteLine("   output_file: Where to store the output");
            Console.WriteLine("   connection_string: Connection string to an indexer db");
            return;
        }

        var connectionString = args[1];
        var outFilePath = args[0];

        await using var outFile = await ExportToBinaryFile(outFilePath, connectionString);
        ValidateData(outFile);
    }

    public static async Task<FileStream> ExportToBinaryFile(string outFilePath, string connectionString)
    {
        var usersFilePath = Path.GetTempFileName();
        var orgsFilePath = Path.GetTempFileName();
        var trustsFilePath = Path.GetTempFileName();
        var balancesFilePath = Path.GetTempFileName();

        Console.WriteLine($"Reading users and orgs ..");
        using var u = new Users(connectionString, Queries.Users);
        await u.Read();

        Console.WriteLine($"Writing users ..");
        await using var usersFile = File.Create(usersFilePath);
        usersFile.Write(BitConverter.GetBytes((uint)BinaryPrimitives.ReverseEndianness(u.UserAddressIndexes.Count)));
        foreach (var (key, _) in u.UserAddressIndexes.OrderBy(o => o.Value))
        {
            usersFile.Write(Convert.FromHexString(key));
        }

        Console.WriteLine($"Writing orgs ..");
        await using var orgsFile = File.Create(orgsFilePath);
        orgsFile.Write(BitConverter.GetBytes((uint)BinaryPrimitives.ReverseEndianness(u.OrgAddressIndexes.Count)));
        foreach (var (_, value) in u.OrgAddressIndexes.OrderBy(o => o.Value))
        {
            orgsFile.Write(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(value)));
        }

        Console.WriteLine($"Reading trusts ..");
        await using var trustsFile = File.Create(trustsFilePath);
        using var t = new TrustReader(connectionString, Queries.TrustEdges, u.UserAddressIndexes);
        var trustReader = await t.ReadTrustEdges();
        uint edgeCounter = 0;
        Console.WriteLine($"Writing trusts ..");
        trustsFile.Write(BitConverter.GetBytes((uint)BinaryPrimitives.ReverseEndianness(0)));
        foreach (var trustEdge in trustReader)
        {
            edgeCounter++;
            trustEdge.Serialize(trustsFile);
        }

        trustsFile.Position = 0;
        trustsFile.Write(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(edgeCounter)));

        Console.WriteLine($"Reading balances ..");
        await using var balancesFile = File.Create(balancesFilePath);
        using var b = new BalanceReader(connectionString, Queries.BalancesBySafeAndToken, u.UserAddressIndexes);
        var balanceReader = await b.ReadBalances();
        Console.WriteLine($"Writing balances ..");
        uint balanceCounter = 0;
        balancesFile.Write(BitConverter.GetBytes((uint)BinaryPrimitives.ReverseEndianness(0)));
        foreach (var balance in balanceReader)
        {
            balanceCounter++;
            balance.Serialize(balancesFile);
        }

        balancesFile.Position = 0;
        balancesFile.Write(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(balanceCounter)));

        var outFileStream = File.Create(outFilePath);
        Console.WriteLine($"Writing output to {outFilePath} ..");

        usersFile.Position = 0;
        Console.WriteLine($"Writing users to offset {outFileStream.Position} ..");
        await usersFile.CopyToAsync(outFileStream);

        orgsFile.Position = 0;
        Console.WriteLine($"Writing orgs to offset {outFileStream.Position} ..");
        await orgsFile.CopyToAsync(outFileStream);

        trustsFile.Position = 0;
        Console.WriteLine($"Writing trusts to offset {outFileStream.Position} ..");
        await trustsFile.CopyToAsync(outFileStream);

        balancesFile.Position = 0;
        Console.WriteLine($"Writing balances to offset {outFileStream.Position} ..");
        await balancesFile.CopyToAsync(outFileStream);

        File.Delete(usersFilePath);
        File.Delete(orgsFilePath);
        File.Delete(trustsFilePath);
        File.Delete(balancesFilePath);

        outFileStream.Flush();
        outFileStream.Position = 0;
        
        return outFileStream;
    }

    static void ValidateData(FileStream fileStream)
    {
        /*
        {
           addressCount: uint32  
           addresses: byte[20][] 
           organizationsCount: uint32
           organizations: uint32[]
           trustEdgesCount: uint32
           trustEdges: {
              userAddress: uint32 
              canSendToAddress: uint32 
              limit: byte   
           }[]
           balancesCount: uint32
           balances: {
              userAddress: uint32  
              tokenOwnerAddress: uint32  
              balance: uint256 
           }[]
        }
         */
        var buffer = new byte[4];
        Debug.Assert(fileStream.Read(buffer) == 4);
        var userCount = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(buffer));
        const uint addressLength = 20;
        var userSectionEnd = 4 + (userCount * addressLength);
        Console.WriteLine($"User section of file is from {0} to {userSectionEnd}");

        fileStream.Position = userSectionEnd;
        Debug.Assert(fileStream.Read(buffer) == 4);
        var orgaCount = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(buffer));
        var orgaSectionEnd = userSectionEnd + 4 + (orgaCount * 4);
        Console.WriteLine($"Orga section of file is from {userSectionEnd} to {orgaSectionEnd}");

        fileStream.Position = orgaSectionEnd;
        Debug.Assert(fileStream.Read(buffer) == 4);
        var trustCount = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(buffer));
        const uint trustLength = 4 + 4 + 1;
        var trustSectionEnd = orgaSectionEnd + 4 + (trustCount * trustLength);
        Console.WriteLine($"Trust section of file is from {orgaSectionEnd} to {trustSectionEnd}");

        fileStream.Position = trustSectionEnd;
        Debug.Assert(fileStream.Read(buffer) == 4);
        var balanceCount = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(buffer));
        var readBalanceCount = 0;
        var balanceSectionEnd = trustSectionEnd + 4;
        
        var headerBuffer = new byte[9];
        while (true)
        {
            if (fileStream.Read(headerBuffer) != headerBuffer.Length)
            {
                break;
            }

            var balanceHolder = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(new ReadOnlySpan<byte>(headerBuffer, 0, 4)));
            var pos = fileStream.Position;
            fileStream.Position = 4 + balanceHolder * 20;
            var balanceHolderAddressBuffer = new byte[20];
            Debug.Assert(fileStream.Read(balanceHolderAddressBuffer) == balanceHolderAddressBuffer.Length);
            var balanceHolderAddress = Convert.ToHexString(balanceHolderAddressBuffer);
            fileStream.Position = pos;

            var tokenOwner = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(new ReadOnlySpan<byte>(headerBuffer, 4, 4)));
            pos = fileStream.Position;
            fileStream.Position = 4 + tokenOwner * 20;
            var tokenOwnerAddressBuffer = new byte[20];
            Debug.Assert(fileStream.Read(tokenOwnerAddressBuffer) == tokenOwnerAddressBuffer.Length);
            var tokenOwnerAddress = Convert.ToHexString(tokenOwnerAddressBuffer);
            fileStream.Position = pos;

            var balanceFieldLength = new ReadOnlySpan<byte>(headerBuffer, 8, 1)[0];
            var balanceFieldBuffer = new byte[balanceFieldLength];
            Debug.Assert(fileStream.Read(balanceFieldBuffer) == balanceFieldBuffer.Length);
            
            var balance = new BigInteger(balanceFieldBuffer, true, true);
            Console.WriteLine($"{balanceHolderAddress};{tokenOwnerAddress};{balance}");

            balanceSectionEnd += (uint)(headerBuffer.Length + balanceFieldBuffer.Length);
            readBalanceCount++;
        }
        
        Debug.Assert(readBalanceCount == balanceCount);

        Console.WriteLine($"Balance section of file is from {trustSectionEnd} to {balanceSectionEnd}");
        Debug.Assert(balanceSectionEnd == fileStream.Length);
        
        Console.WriteLine($"File length is {fileStream.Length}. Read bytes are: {balanceSectionEnd} File seems to be {(fileStream.Length == balanceSectionEnd ? "o.k." : "not o.k.")}");
        fileStream.Position = 0;
    }
}