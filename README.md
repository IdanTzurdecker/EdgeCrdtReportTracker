# Tactical Edge Intelligence Sync System

## Overview

This project is my solution to the Forward Deployed Engineer coding challenge. I built an offline-first synchronization system for intelligence reports that can operate in environments with unreliable network connectivity (like military Forward Operating Bases with a spotty SATCOM connection).
This is a scenario based demo PoC.

Checkout the [dev branch](https://github.com/IdanTzurdecker/EdgeCrdtReportTracker/tree/dev) for more recent development including a HTTP server.


---

## How to Run 

Clone Repository

### Prerequisites

- .NET 9.0 SDK or later
- Optional: Docker 

### Local Execution

```bash
# Navigate to project directory
cd TacticalSync/src/TacticalSync

# Build and run
dotnet build
dotnet run
```

### Docker Execution

#### Navigate to Project Directory

```bash
cd TacticalSync
````

#### Build Image

```bash
docker build -t tactical-sync:latest -f Dockerfile .
```

#### Run Container

```bash
docker run --rm tactical-sync:latest
```

The application will run 4 demonstration scenarios showing:
1. A Basic sync between two nodes
2. Network partition, offline updates, and then automatic conflict resolution
3. Three-way concurrent conflicts with consistency at the end
4. An Audit trail integrity test and tamper detection test

#### Run Unit Tests

```bash
## root
cd TacticalSync 

dotnet test --verbosity normal
```


## About TacticalSync


### Tech Stack
- .NET 9.0 (exe)
- Docker
- C#

**Why I chose C# / .NET?**
- Cross-platform 
- Most comfortable for me to work with
- Cloud-native
- Solid standard library
- Memory-safety

# About 
Proof of Concept that simulates distributed intelligence report tracker that works offline-first

## Project structure 
```
TacticalSync/
├── Models/
│   ├── VectorClock.cs          # "Causal" ordering mechanism
│   ├── IntelligenceReport.cs   # Model for SALUTE report data model (used salute format based on research)
│   └── AuditLog.cs              # model for a NIST-compliant audit entries
├── Core/
│   ├── Node.cs                  # Distributed node (FOB/Command Center)
│   ├── ConflictResolver.cs     # Class for Hybrid CRDT merge logic
│   └── NetworkController.cs    # Class for Handling Partition simulation
└── Program.cs                   # Demonstration scenarios
```

## My Synchronization Strategy

I chose to implement a simulation of Ditto's sync mechanism. This does not use Ditto itself, but the concepts.

**Vector Clocks** track causality:
- Each node has a logical clock: `{FOB_Alpha: 3, FOB_Bravo: 2}`
- When a node creates or updates a report, it increments its counter
- When syncing, we compare vector clocks to determine which version is newer in their VC's
- If neither version is newer , we have a conflict

**CRDTs** (Conflict-Free Replicated Data Types):
- The data structures are designed so merging always works
- Equipment field of report will always take the union
  - This means: If soldier A sees a Tank and sodlier B sees a Helicopter, the merged report shows both


### Conflict Resolution Strategy

All elements in Vector_A are less than or equal to Vector_B, and at least one element in V_A is strictly less than Vector_B

** When comparing**

1. **Check Causality**: Use vector clocks to see if one update happened after the other
    - If one version is clearly newer → accept it (no conflict!)

2. **Detect Concurrency**: If vector clocks show concurrent updates (neither is newer)
    - This means both nodes updated while offline

3. **Deterministic Resolution**:
    - Primary tie-breaker: Latest timestamp wins
    - Secondary tie-breaker: Alphabetically higher node ID
    - merge equipment sets (preserve all intelligence)

##  Trade-offs

### CAP Theorem Choice: AP (Availability + Partition Tolerance)

The CAP theorem says you can only have 2 of 3:
- **C**onsistency: Everyone sees the same data immediately
- **A**vailability: System responds even during network failures
- **P**artition Tolerance: System works despite network outages

**I chose AP**

Business value: forward operating bases need to input intelligence regardless of network status. I will assume end users are soldiers
- In tactical environments, assume network partitions will happen (example from the assesment was SATCOM outages)
- We can't block operations waiting for connectivity

**Cons (losing C)**
- We lose Immediate consistency - nodes might have different views temporarily
  - but we get eventual consistency instead - all nodes converge when they sync

**Pros (having AP)**
- FOBs work offline indefinitely
- No single point of failure
- Automatic conflict resolution when connectivity returns


### Eventual Consistency Guarantees

All nodes are guaranteed to converge to the **same final state** because:
1. CRDT merge operations are commutative (order doesn't matter)
2. CRDT merge operations are idempotent (repeat merges will not change result)
3. Conflict resolution is deterministic (all nodes make the same decisions)

**Example**
- If FOB_Alpha syncs with FOB_Bravo, then FOB_Charlie
- Or if FOB_Alpha syncs with FOB_Charlie, then FOB_Bravo
- The final state is the same - sync order doesn't matter

## Security & Compliance Considerations

**Implemented Cryptographic Audit Trail**
- Every operation logged with SHA-256 hash chain
- Tampering is immediately detectable
- Meets NIST SP 800-53 Control AU-9

AuditLog captures: who, what, when, where, outcome, classification
Audit protection | Cryptographic hash chain prevents tampering

Currently simulated:
- Network communication (direct method calls, not HTTP/gRPC)
- Authentication (currently uses node ID)
- Encryption in transit


Production would add:
- Token-based authentication (OIDC/OAuth2)
- End-to-end encryption for classified data

## Scaling to 100+ Edge Locations: 
- Currently, Nodes are fully connected and can communicate with each other when online. Understandably doesnt scale well 
  - edges = n(n-1)/2 for n nodes
- only send necessary reports, which are changed reports (already done)
- Brainstorming and research leads me to Merkle trees for verifying hash at root & Hub-and-Spoke Architecture

## Notes:
- Nodes use in-memory storage
- Sync Triggers are manual
  - Does not Automaticly trigger sync : This demo has manual syncs but would add background timers for automatic