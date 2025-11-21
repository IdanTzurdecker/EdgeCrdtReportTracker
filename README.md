## How to Run

### Prerequisites

- .NET 9.0 SDK or later

### Local Execution

```bash
# Navigate to project directory
cd TacticalSync

# Build and run
dotnet build
dotnet run
```

### Docker Execution

#### Build Image

```bash
docker build -t tactical-sync:latest -f Dockerfile .
```

#### Run Container

```bash
docker run --rm tactical-sync:latest
```
