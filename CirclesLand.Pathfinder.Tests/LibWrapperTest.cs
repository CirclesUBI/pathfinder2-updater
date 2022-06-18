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
            Assert.Greater(block, 0);
            Console.WriteLine("Imported till block: {0}", block);
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
        Assert.Greater(block, 0);
        Console.WriteLine("Imported till block: {0}", block);
    }
}