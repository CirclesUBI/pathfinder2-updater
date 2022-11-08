FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ENV DOTNET_EnableDiagnostics=0
WORKDIR /src
COPY . .
RUN cd CirclesUBI.PathfinderUpdater.Updater && dotnet restore "CirclesUBI.PathfinderUpdater.Updater.csproj"
RUN cd CirclesUBI.PathfinderUpdater.Updater && dotnet build "CirclesUBI.PathfinderUpdater.Updater.csproj" -c Release -o /app/build

FROM build AS publish
ENV DOTNET_EnableDiagnostics=0
RUN cd CirclesUBI.PathfinderUpdater.Updater && dotnet publish "CirclesUBI.PathfinderUpdater.Updater.csproj" -c Release -o /app/publish

FROM base AS final
LABEL org.opencontainers.image.source=https://github.com/circlesland/pathfinder2-updater
ENV DOTNET_EnableDiagnostics=0

WORKDIR /app
COPY --from=publish /app/publish .
RUN chmod +x ./CirclesUBI.PathfinderUpdater.Updater
ENTRYPOINT ["./CirclesUBI.PathfinderUpdater.Updater"]
