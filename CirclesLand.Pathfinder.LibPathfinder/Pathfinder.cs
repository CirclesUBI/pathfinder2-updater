using System.Runtime.InteropServices;

namespace CirclesLand.Pathfinder.LibPathfinder;

/*
    size_t loadDB(char const* _data, size_t _length);
    size_t edgeCount();
    void delayEdgeUpdates();
    void performEdgeUpdates();
    void signup(char const* _user, char const* _token);
    void organizationSignup(char const* _organization);
    void trust(char const* _canSendTo, char const* _user, int _limitPercentage);
    void transfer(char const* _token, char const* _from, char const* _to, char const* _value);
    char const* adjacencies(char const* _user);
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

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong edgeCount();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void importDB(string safesJson, string destFile);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong delayEdgeUpdates();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong performEdgeUpdates();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong signup(string user, string token);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong organizationSignup(string organization);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong trust(string canSendTo, string user, int limitPercentage);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong transfer(string token, string from, string to, string value);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong adjacencies(string user);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern string flow(string input);
}