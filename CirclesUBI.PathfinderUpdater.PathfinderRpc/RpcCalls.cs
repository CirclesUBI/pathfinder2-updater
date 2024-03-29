using CirclesUBI.Pathfinder.Models;
using Newtonsoft.Json;

namespace CirclesUBI.PathfinderUpdater.PathfinderRpc;

public static class RpcCalls
{
    public static string LoadEdgesBinary(string pathToBinaryDump)
    {
        return "{\n    \"id\":\"" + DateTime.Now.Ticks +
               "\", \n    \"method\": \"load_edges_binary\", \n    \"params\": {\n        \"file\": \"" +
               pathToBinaryDump + "\"\n    }\n}";
    }
    public static string LoadSafesBinary(string pathToBinaryDump)
    {
        return "{\n    \"id\":\"" + DateTime.Now.Ticks +
               "\", \n    \"method\": \"load_safes_binary\", \n    \"params\": {\n        \"file\": \"" +
               pathToBinaryDump + "\"\n    }\n}";
    }

    public static string UpdateEdges(IEnumerable<IncrementalExportRow> rows)
    {
        return "{\n    \"id\":\"" + DateTime.Now.Ticks +
               "\", \n    \"method\": \"update_edges\", \n    \"params\": " +
               JsonConvert.SerializeObject(rows) + "\n}";
    }
}