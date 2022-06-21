using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CirclesLand.Pathfinder.ImportExport;
using NUnit.Framework;

namespace CirclesLand.Pathfinder.Tests;

public class LibWrapperTest
{
    private Db _db;
    
    [SetUp]
    public async Task SetUp()
    {
        var indexerImporter = new IndexerImporter();
        _db = await indexerImporter.Import(Settings.IndexerConnectionString);
    }

    [Test, Order(1)]
    public void LoadDbFromFile()
    {
        Console.WriteLine($"Running test in proc: {Process.GetCurrentProcess().Id}");
        
        var tmpFile = Path.GetTempFileName();
        try
        {
            var fileStream = File.Create(tmpFile);
            new BinaryExporter().Export(_db, fileStream);
            fileStream.Flush();
            fileStream.Close();

            var block = LibPathfinder.Pathfinder.LoadDbFromFile(tmpFile);
            var edgeCount = LibPathfinder.Pathfinder.edgeCount();
            Assert.Greater(block, 0);
            Assert.Greater(edgeCount, 0);
            Console.WriteLine("Imported till block: {0}", block);
            Console.WriteLine("Edge count: {0}", edgeCount);
        }
        catch (Exception e)
        {
            Assert.Fail(e.Message);
        }
        finally
        {
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }
        }
    }
    
    [Test, Order(2)]
    public void LoadDbFromBytes()
    {
        Console.WriteLine($"Running test in proc: {Process.GetCurrentProcess().Id}");
        
        var memStream = new MemoryStream();
        new BinaryExporter().Export(_db, memStream);
        memStream.Position = 0;

        var block = LibPathfinder.Pathfinder.LoadDbFromBytes(memStream.GetBuffer());
        var edgeCount = LibPathfinder.Pathfinder.edgeCount();
        Assert.Greater(block, 0);
        Assert.Greater(edgeCount, 0);
        Console.WriteLine("Imported till block: {0}", block);
        Console.WriteLine("Edge count: {0}", edgeCount);
    }

    [Test, Order(3)]
    public void TestFlow()
    {
        var from = "0xc7c8fcd303a55aa860210a284afbbd3f95b63103";
        var to = "0x580816b8beb4d1bca5b48af07cd989b6ffed6904";
        var value = "999999999999999999999999";
        
        var json = $"{{\"from\": \"{from}\", \"to\": \"{to}\", \"value\": \"{value}\"}}";
        var result = LibPathfinder.Pathfinder.flow(json);
        
        Console.WriteLine(result);
        
        var json2 = $"{{\"from\": \"{from}\", \"to\": \"{to}\", \"value\": \"{value}\"}}";
        var result2 = LibPathfinder.Pathfinder.flow(json2);
        
        Console.WriteLine(result2);
    }
}