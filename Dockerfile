FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY TacticalSync.csproj TacticalSync/
RUN dotnet restore TacticalSync/TacticalSync.csproj

# Copy source code and build
COPY . TacticalSync/
WORKDIR /src/TacticalSync
RUN dotnet build TacticalSync.csproj -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish TacticalSync.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime - Distroless for security
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final

# Security: Run as non-root user
USER 10001:10001
WORKDIR /app

# Copy published application
COPY --from=publish --chown=10001:10001 /app/publish .

# Environment variables for FIPS compliance
ENV FIPS_ENABLED=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV COMPlus_EnableFIPS=1

# Labels for metadata
LABEL type="Idan Take home asssesment Part 2"
LABEL author="Idan Tz"
LABEL maintainer="Idan Assessment"
LABEL description="Tactical Edge Synchronization - Offline-First Intelligence Reporting"
LABEL security.fips="enabled"
LABEL security.scan-required="true"

# Run the application
ENTRYPOINT ["dotnet", "TacticalSync.dll"]