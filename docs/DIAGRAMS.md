# System diagrams (Mermaid)

## Entire player / incident loop

```mermaid
flowchart TD
  J[Join self-hosted world] --> M[Load membership & qualifications]
  M --> S[Report for duty / standby]
  S --> D[Server creates wildfire incident]
  D --> C[Commander dispatches units]
  C --> A[Air crew and aircraft]
  C --> G[Ground fire crews]
  A --> AD[Scoop / release water]
  G --> GD[Pump / hose / containment]
  AD --> F[Authoritative fire simulation]
  GD --> F
  F --> K{Contained?}
  K -->|No| C
  K -->|Yes| R[After Action Report]
  R --> S
```

## Authoritative world and VFX

```mermaid
flowchart TD
  IN[Client controls and requests] --> VAL[Dedicated server validation]
  W[Server wind field] --> FIRE[Server fire grid / burn phases]
  W --> DROP[Server water trajectory parcels]
  VAL --> DROP
  VAL --> FIRE
  DROP --> FIRE
  FIRE --> S[Versioned chunk snapshots + deltas]
  W --> S
  S --> C1[Client A]
  S --> C2[Client B]
  C1 --> FX1[Local flame/smoke/sparks/VFX]
  C2 --> FX2[Local flame/smoke/sparks/VFX]
  FIRE --> DB[World persistence checkpoints]
```

## Aircraft resource lifecycle

```mermaid
stateDiagram-v2
  [*] --> Standby
  Standby --> Assigned
  Assigned --> Preflight
  Preflight --> Taxi
  Taxi --> Flight
  Flight --> Scooping
  Scooping --> Approach
  Approach --> Drop
  Drop --> Approach: more water available
  Drop --> Scooping: refill at sea
  Drop --> Return: completed or reserve fuel
  Flight --> Return: cancel / unsafe weather
  Return --> Maintenance
  Maintenance --> Standby
```

## Operations and server geography

```mermaid
flowchart LR
  subgraph WorldServer[One headless dedicated server]
    CMD[Regional command + dispatch]
    Z[Zadar/Zemunik sector]
    S[Split/Omis sector]
    L[Ljubuski sector]
    COM[Global incident summaries]
    CMD --> COM
    Z --> COM
    S --> COM
    L --> COM
  end
  PA[Player in Omiš] --> S
  PA --> COM
  PB[Pilot across sectors] --> Z
  PB --> S
  PC[Commander] --> COM
```

## Database and event processing

See `DATABASE_SCHEMA.md` for detailed ER diagram. High-frequency simulation remains in memory; persistence uses atomic snapshots + append-only meaningful incident events.
