# syntax=docker/dockerfile:1

# Build context is the repo root (see docker-compose.yml) because Directory.Build.props and
# Directory.Packages.props live there and are picked up by MSBuild's automatic upward
# directory search — they must stay at the same relative depth above each .csproj as in the
# real repo layout for that search to find them.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY backend/HMS.sln backend/HMS.sln
COPY backend/src backend/src

WORKDIR /src/backend/src/HMS.Api
# Cache mount persists downloaded packages across builds/retries even on failure — this repo
# has ~15 module projects to restore, worth surviving a flaky connection.
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore HMS.Api.csproj

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish HMS.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Npgsql probes for GSSAPI (Kerberos) support on every new connection; without this lib
# present the probe throws and gets silently retried without it, which otherwise litters
# startup logs with a misleading "libgssapi_krb5.so.2: cannot open shared object file" error
# on every single migration/seed connection.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HMS.Api.dll"]
