# Build context is the repository root (see docker-compose.yml) — Directory.Build.props
# and Directory.Packages.props live there and are picked up automatically by MSBuild's
# directory-walk-up, the same way `dotnet build` on a dev machine finds them.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY backend/HMS.sln backend/HMS.sln
COPY backend/src backend/src

# Restoring/publishing just the API project (not the full .sln) skips the test projects —
# smaller build context, and this image only ever needs to run the API.
RUN dotnet restore backend/src/HMS.Api/HMS.Api.csproj
RUN dotnet publish backend/src/HMS.Api/HMS.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

# curl backs the docker-compose healthcheck against /health — the aspnet base image has
# no HTTP client tool otherwise. Installed before switching to the non-root "app" user
# below, since apt-get needs root.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# The aspnet base image ships a built-in non-root "app" user for exactly this purpose.
USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "HMS.Api.dll"]
