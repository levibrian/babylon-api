# Startup Feature

## Overview

Contains the health check endpoint and the root DI registration that wires up all feature modules.

## Components

```
Features/Startup/
├── Controllers/
│   └── HealthController.cs                 # GET /health - returns 200 OK
└── Extensions/
    └── ServiceCollectionExtensions.cs       # Root RegisterFeatures() method
```

## RegisterFeatures()

This is the entry point for all feature DI registration. Called from `Program.cs` via `builder.Services.RegisterFeatures()`. It:

1. Registers shared repositories (all `I*Repository` / `*Repository` pairs as Scoped).
2. Calls each feature's own `ServiceCollectionExtensions` to register feature-specific services.
3. Registers infrastructure services (Yahoo Finance HTTP clients, etc.).
4. Binds configuration option classes (`IOptions<T>` pattern).

## Health Check

- Route: `GET /health`
- Returns: `200 OK` with simple string response
- Used by Fly.io for readiness/liveness probes (configured in `fly.api.toml`)
