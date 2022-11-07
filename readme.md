# pathfinder2-updater

**First, run the pathfinder:**

```shell
cargo run --release 54389
```

Where the command line parameters are:

[0]: run the optimized release build  
[1]: listen on port 54389

**Then, run the updater (on a different terminal):**  
_replace the values in curly braces_

```shell
docker run \
  --network=host \
  -v "${HOME}/.pathfinder2:/var/pathfinder2/data" \
  --rm \
  ghcr.io/circlesland/pathfinder2-updater:0.0.2 \
  "Server={server};Port={port};Database=indexer;User ID={username};Password={password};Command Timeout=240" \
  ws://localhost:8675 \
  "/var/pathfinder2/data/capacity_graph.db" \
  "${HOME}/.pathfinder2/capacity_graph.db" \
  http://localhost:54389
```

where the command line parameters are:

[0]: use the host network (to reach the previously started pathfinder2)  
[1]: use a volume that's accessible to the pathfinder2  
[2]: delete the container after it's stopped  
[3]: the image to run  
[4]: indexer db connection string (see: https://github.com/circlesland/blockchain-indexer)    
[5]: indexer websocket endpoint (see: https://github.com/circlesland/blockchain-indexer)  
[6]: filesystem path where the initial capacity graph dump should be stored (pathfinder2 needs read access so this has to be a volume)    
[7]: http json-rpc endpoint of pathfinder2 (see: https://github.com/chriseth/pathfinder2)  
