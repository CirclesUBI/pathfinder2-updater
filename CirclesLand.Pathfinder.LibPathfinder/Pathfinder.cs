using System.Runtime.InteropServices;

namespace CirclesLand.Pathfinder.LibPathfinder;

/*
    size_t loadDB(char const* _data, size_t _length);
    size_t edgeCount();
    char const* flow(char const* _input);
 */
public static unsafe class Pathfinder {
    private const string DllName = "libpathfinder.so";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong loadDB(byte* data, ulong length);

    public static ulong LoadDbFromFile(string path)
    {
        using var dbFile = File.Open(path, FileMode.Open, FileAccess.Read);
        
        var dbData = new List<byte>((int) dbFile.Length);
        int readByteCount;
        do
        {
            var buffer = new byte[16384];
            readByteCount = dbFile.Read(buffer);

            dbData.AddRange(
                readByteCount == buffer.Length 
                    ? buffer 
                    : buffer[new Range(0, readByteCount)]
            );
        } while (readByteCount > 0);

        return LoadDbFromBytes(dbData.ToArray());
    }

    public static ulong LoadDbFromBytes(byte[] bytes)
    {
        fixed (byte* pointer = bytes)
        {
            byte* ptr = pointer;
            return loadDB(ptr, (ulong) bytes.Length);
        }
    }

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern ulong edgeCount();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern string flow(string input);
}