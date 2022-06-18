using System.IO;
using System.Threading.Tasks;
using CirclesLand.Pathfinder.ImportExport;
using NUnit.Framework;

namespace CirclesLand.Pathfinder.Tests;

public class ImportExportTest
{
    private Db _db;
    private MemoryStream? _stream;

    [Test, Order(1)]
    public async Task IndexerToDb()
    {
        var indexerImporter = new IndexerImporter();
        _db = await indexerImporter.Import(Settings.IndexerConnectionString);
        
        Assert.Greater(_db.Safes.Count, 0);
        Assert.Greater(_db.Addresses.Length, 0);
        
        Assert.Pass();
    }

    [Test, Order(2)]
    public void DbToBinary()
    {
        _stream = new MemoryStream();
        var binaryExporter = new BinaryExporter();
        binaryExporter.Export(_db, _stream);
     
        Assert.Greater(_stream.Position, 0);
        Assert.Pass();
    }

    [Test, Order(3)]
    public void BinaryToDb()
    {
        var binaryImporter = new BinaryImporter();
        
        Assert.NotNull(_stream);
        
        _stream!.Position = 0;
        var db = binaryImporter.Import(_stream);
        
        Assert.AreEqual(_db.Addresses.Length, db.Addresses.Length);
        Assert.AreEqual(_db.Safes.Count, db.Safes.Count);
        Assert.AreEqual(_db.Block, db.Block);
        
        Assert.Pass();
    }
}