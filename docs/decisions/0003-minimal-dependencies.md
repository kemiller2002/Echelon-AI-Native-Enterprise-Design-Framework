---
id: ADR-0003
title: Minimal dependencies and no infrastructure in the foundation phase
status: accepted
date: 2026-11-20
---

# ADR 0003: Minimal dependencies

## Decision

- **Runtime dependencies:** FSharp.Core and the .NET BCL (System.Text.Json) only.
- **Test dependencies:** xUnit 2.9.2, matching Echelon repositories. Property
  tests use the framework's own seeded generator instead of FsCheck.
- **Tooling:** Fantomas 8.1.1 (a dotnet local tool, as in echelon-knowledge-store).
- **Not adopted yet:** Praxis installation, Aegis, a database, services. Praxis
  adoption would add its governance scaffolding (`ros.json`, `.ros/`, launchers).
  That is a repository-governance decision for the owner (see the gap register).

## Consequences

- Fast, reproducible builds; no package feed needed beyond nuget.org.
- The repository does not yet use the Praxis work protocol. `progress/` serves
  as the durable ledger in the meantime.
