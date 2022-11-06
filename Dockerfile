FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ENV DOTNET_EnableDiagnostics=0
WORKDIR /src
COPY . .
RUN cd CirclesLand.PathfinderExport.Updater && dotnet restore "CirclesLand.PathfinderExport.Updater.csproj"
RUN cd CirclesLand.PathfinderExport.Updater && dotnet build "CirclesLand.PathfinderExport.Updater.csproj" -c Release -o /app/build

FROM build AS publish
ENV DOTNET_EnableDiagnostics=0
RUN cd CirclesLand.PathfinderExport.Updater && dotnet publish "CirclesLand.PathfinderExport.Updater.csproj" -c Release -o /app/publish

FROM base AS final
LABEL org.opencontainers.image.source=https://github.com/jaensen/CirclesLand.Pathfinder
ENV DOTNET_EnableDiagnostics=0
ENV INDEXER_RPC_GATEWAY_URL ''
ENV INDEXER_CONNECTION_STRING ''
ENV INDEXER_WEBSOCKET_PORT='8675'
WORKDIR /app
COPY --from=publish /app/publish .
RUN chmod +x ./CirclesLand.PathfinderExport.Updater
ENTRYPOINT ["./CirclesLand.PathfinderExport.Updater"]
