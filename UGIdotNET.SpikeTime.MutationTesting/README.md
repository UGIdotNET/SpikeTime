# UGIdotNET SpikeTime - Mutation Testing

A .NET 10 minimal API with Aspire orchestration, demonstrating mutation testing with [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/).

The API manages a Todo list backed by SQL Server and is covered by integration tests that use [Testcontainers](https://dotnet.testcontainers.org/) to spin up an isolated SQL Server container per test run.

## Prerequisites

| Requirement | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 or later |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or compatible runtime) | running |
| [.NET Aspire workload](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling) | 13.4 or later |
| [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/aspire-sdk-tooling#aspire-cli) *(optional)* | latest |

> **Docker** is required both for the Aspire-managed SQL Server container (development) and for the Testcontainers-based SQL Server instance used by the test suite.

### Install the Aspire workload

```bash
dotnet workload install aspire
```

### Install the Aspire CLI *(optional)*

The Aspire CLI is a standalone tool that provides a richer command-line experience for starting, stopping, and inspecting Aspire applications without needing Visual Studio or the full `dotnet run` flow.

```bash
dotnet tool install --global aspire
```

To update an existing installation:

```bash
aspire update --self
```

Verify the installation:

```bash
aspire doctor
```

## Running the application

The application is orchestrated by .NET Aspire. The AppHost starts:

- A **SQL Server** container with a persistent data volume.
- The **Migrations** worker, which applies EF Core migrations automatically.
- The **Web API** (`/api/todos`), which waits for the database to be ready.
- An optional **DbGate** UI (explicit start required from the Aspire dashboard).

### Option A — `dotnet run`

```bash
dotnet run --project UGIdotNET.SpikeTime.MutationTesting.AppHost
```

### Option B — Aspire CLI

From the repository root:

```bash
aspire run
```

---

Once running, the Aspire dashboard URL is printed to the console. Open it to inspect resource health, logs, and traces. The Web API OpenAPI document is available at `/openapi/v1.json` (development only).

## Running the tests

The test project uses `WebApplicationFactory` and Testcontainers — Docker must be running.

```bash
dotnet test UGIdotNET.SpikeTime.MutationTesting.Test
```

## Mutation testing with Stryker.NET

### Install Stryker.NET

Stryker.NET is distributed as a .NET global tool:

```bash
dotnet tool install --global dotnet-stryker
```

To update an existing installation:

```bash
dotnet tool update --global dotnet-stryker
```

Verify the installation:

```bash
dotnet stryker --version
```

### Run Stryker.NET

A `stryker-config.json` file at the root of the repository pre-configures the run (mutation level, thresholds, reporters, files to mutate, etc.).

Run mutation testing from the repository root:

```bash
dotnet stryker
```

Stryker will:
1. Build the solution.
2. Run the test suite once to establish a baseline.
3. Generate mutants and re-run tests for each one.
4. Produce an **HTML report** inside `StrykerOutput/<timestamp>/reports/`.

Open the HTML report in a browser to explore surviving and killed mutants.

### Configuration highlights (`stryker-config.json`)

| Setting | Value | Description |
|---|---|---|
| `mutation-level` | `Standard` | Set of mutators applied |
| `concurrency` | `8` | Parallel test runners |
| `coverage-analysis` | `perTest` | Only re-run tests that cover a mutant |
| `thresholds.high` | `80` | Score shown in green |
| `thresholds.low` | `60` | Score shown in yellow; below breaks the build when `break > 0` |
| `mutate` | excludes `Migrations` and `ServiceDefaults` | Only application code is mutated |
| `reporters` | `dots`, `Html` | Console progress + HTML report |
