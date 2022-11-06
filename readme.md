# pathfinder2-updater
Run with:
```
sudo docker run --rm ghcr.io/circlesland/pathfinder2-updater:0.0.1 \  
"Server={indexer-host};Port=5432;Database=indexer;User ID=readonly_user;Password={indexer-pwd};Command Timeout=240" \  
wss://index.circles.land \  
/path/to/capacity_graph.db \  
http://localhost:1234
```
where the parameters are:  
[0]: indexer db connection string (see: https://github.com/circlesland/blockchain-indexer)  
[1]: indexer websocket endpoint   
[2]: filesystem path where the initial capacity graph dump should be stored (pathfinder2 needs read access)  
[3]: http json-rpc endpoint of pathfinder2 (see: https://github.com/chriseth/pathfinder2)