# ---------------------------------------
# Stage 1: build & publish
# ---------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

# Workdir holds the repo subset passed as build context (./api)
WORKDIR /src

# Minimal NuGet config to avoid host-specific fallback folders
# Only nuget.org as source; no fallback package folders
RUN printf '<configuration>\n  <packageSources>\n    <add key="nuget" value="https://api.nuget.org/v3/index.json" />\n  </packageSources>\n</configuration>\n' > NuGet.config

# Copy project files first to leverage layer caching
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Presentation/Api.csproj src/Presentation/
COPY Directory.Build.props ./

# Restore with explicit config and empty RestoreFallbackFolders
RUN dotnet restore src/Presentation/Api.csproj \
    --configfile ./NuGet.config \
    -p:RestoreFallbackFolders=""

# Copy the full source
COPY . .

# Build
RUN dotnet build src/Presentation/Api.csproj -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Publish trimmed app
RUN dotnet publish src/Presentation/Api.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

# ---------------------------------------
# Stage 2: runtime image
# ---------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# OCI labels
LABEL org.opencontainers.image.title="CollabTask API" \
      org.opencontainers.image.description="ASP.NET Core API for CollabTask" \
      org.opencontainers.image.source="https://github.com/borjaoviedo/collab-task" \
      org.opencontainers.image.licenses="MIT"

# Runtime Environment
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

# Minimal tooling for healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=build /app/publish ./

# Network
EXPOSE 8080

# Container-level healthcheck
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1

# Entrypoint
ENTRYPOINT ["dotnet", "Api.dll"]