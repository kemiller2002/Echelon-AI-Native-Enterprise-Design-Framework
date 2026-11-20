# Echelon AI-Native Enterprise Design Framework

> If this organisation did not exist today, how would we build it to
> accomplish the same fundamental outcomes with fewer unnecessary activities,
> lower costs, stronger controls and better results?

An industry-independent, evidence-driven framework, implemented in F#, for:

- discovering how an enterprise operates
- reinventing (not merely automating) its workflows
- deciding what deterministic software, AI, agents and accountable humans should each do
- testing whether the redesign is actually better
- generating a traceable architecture for it

**Status:** foundation phase (v0, unreleased). One case study (donor-advised
funds) is modelled end to end. See [`progress/LEDGER.md`](progress/LEDGER.md).

## What it does

```
typed enterprise model ─► validate ─► reinvent ─► assign executors ─► price ─► generate architecture
   (case-studies/)        EEF001–015   closure,     AI Responsibility  scenarios,   APIs, events, agents,
                                       dispositions  Model R1–R7        sensitivity  grants, controls, trace
                                                                         │
                                       Clarity brief · EDF packet · simulation ◄┘   (disagreements preserved)
```

Every proposed improvement is treated as a hypothesis. The framework can and
does conclude that a redesign is inferior, or only superior at expected values.

## Quick start

Requires the .NET 10 SDK.

```bash
dotnet tool restore
dotnet test Echelon.Enterprise.sln -c Release
dotnet run --project src/Echelon.Enterprise.Cli -- list
dotnet run --project src/Echelon.Enterprise.Cli -- validate daf
dotnet run --project src/Echelon.Enterprise.Cli -- analyse daf      # → case-studies/daf/generated/
dotnet run --project src/Echelon.Enterprise.Cli -- export-eks daf   # → case-studies/daf/knowledge/
```

Read the output: [`case-studies/daf/generated/analysis.md`](case-studies/daf/generated/analysis.md).

## Repository map

| Path | Contents |
|---|---|
| [`methodology/`](methodology/) | Discovery, workflow reinvention, AI responsibility, economics and continuous improvement methods |
| [`src/Echelon.Enterprise/`](src/Echelon.Enterprise/) | The framework: model, validation, engines, generator, lenses, simulation, IR |
| [`src/Echelon.Enterprise.Cli/`](src/Echelon.Enterprise.Cli/) | `eef` CLI (the only code with effects) |
| [`case-studies/`](case-studies/) | Typed industry models and their generated outputs |
| [`tests/`](tests/) | xUnit unit, property and case-study acceptance tests |
| [`schemas/`](schemas/) | `eef.analysis/0` JSON Schema for the IR |
| [`architecture/`](architecture/) | Framework architecture and generator invariants |
| [`research/`](research/) | Ecosystem audit and supporting research |
| [`economics/`](economics/), [`simulations/`](simulations/), [`commercial/`](commercial/) | Cross-case studies (to come) |
| [`docs/decisions/`](docs/decisions/) | Architecture decision records |
| [`progress/`](progress/) | Ledger, gap register, generalisation log |

## Relationship to other Echelon projects

| Project | Use |
|---|---|
| echelon-knowledge-store | Evidence exported as EKS records and validated by `eks validate` (verified) |
| Clarity | Clarity Lean review inputs prepared; review not performed by the framework |
| EDF | EDF v0.3 diagnostic inputs prepared; diagnosis not performed by the framework |
| daf-landscape | Source corpus for Case Study A |
| Praxis, Dokimos, Aegis, Limen, Forma, Folio | Audited; integration planned or deferred (see the audit) |

## Contributing

See [`AGENTS.md`](AGENTS.md).
