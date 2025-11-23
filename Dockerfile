FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ["TacticalSync.sln", "./"]

# Copy project files for restore
COPY ["src/TacticalSync/TacticalSync.csproj", "src/TacticalSync/"]

# Restore dependencies
RUN dotnet restore "src/TacticalSync/TacticalSync.csproj"

# Copy all source code
COPY . .

# Build
WORKDIR "/src/src/TacticalSync"
RUN dotnet build "TacticalSync.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "TacticalSync.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime - Distroless for security
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final

# Security: Run as non-root user
USER 10001:10001
WORKDIR /app

# Copy published application
COPY --from=publish --chown=10001:10001 /app/publish .

# Labels for metadata
LABEL type="Idan Take home asssesment Part 2"
LABEL author="Idan Tz"
LABEL maintainer="Idan Assessment"
LABEL description="Tactical Edge Synchronization - Offline-First Intelligence Reporting"

# Run the application
ENTRYPOINT ["dotnet", "TacticalSync.dll"]