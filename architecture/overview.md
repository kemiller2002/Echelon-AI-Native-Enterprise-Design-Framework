---
id: ARCH-EEF-OVERVIEW
title: Framework architecture
status: draft
updated: 2026-11-20
---

# Framework architecture (v0)

A **modular monolith** in F#: one library with one module per engine, a
case-study library, a CLI and a test project. No services, databases,
containers or infrastructure-as-code. The core performs no I/O.

```
case-studies/  (typed F# models)
      │
      ▼
src/Echelon.Enterprise  ──────────────────────────────────────────────────
  Primitives   ids (one type per reference kind), units of measure, estimates
  Evidence     sources, epistemic status, basis
  Model        outcomes, constraints/obligations, capabilities, data, roles,
               decisions, activities, workflows, economic assumptions
  Validation   EEF001–EEF015 diagnostics          (compiler-assisted layer)
  Responsibility  executor rules R1–R7             (AI Responsibility Model)
  Reinvention  closure, dispositions, steps        (Workflow Reinvention Engine)
  Economics    scenarios, sensitivity, verdicts    (Economics Engine)
  Architecture capability map, APIs, events,       (Architecture Generator)
               agents, grants, decision rights,
               controls, monitoring, roadmap, trace
  Lenses       Clarity Lean brief, EDF packet,     (analytical interfaces)
               disagreements
  Simulation   seeded discrete-event twin          (Digital Twin v0)
  Analysis     validate → reinvent → price → generate → lenses → simulate
  Json         eef.analysis/0 IR
  Markdown     human-readable report
  Knowledge    EKS evidence records
──────────────────────────────────────────────────────────────────────────
      │
      ▼
src/Echelon.Enterprise.Cli (eef)  — the only effects: args, files, exit codes
```

## Where the compiler helps (and where it does not)

| Guarantee | Mechanism |
|---|---|
| A capability id cannot be used where an outcome id is expected | Single-case union per reference kind |
| Minutes cannot be added to dollars; hourly rates convert explicitly | F# units of measure |
| Every executor case is handled in economics, simulation and architecture | Exhaustive pattern matching (`TreatWarningsAsErrors`) |
| References resolve; facts have fact-capable sources; inputs are available | Runtime validation (EEF diagnostics) |
| Controls survive redesign; agents never hold human authority | Property tests over 300 generated workflows |

## Generator invariants

`Architecture.generate` reports a **violation** (and the CLI exits 4) when:

- an agent holds an operation reserved to human authority;
- an agent holds a clearance operation under `NoAiClearance`;
- a governing control obligation is covered by no redesigned step;
- an operation produces data that no capability owns (no system of record).

## Traceability

Every generated operation traces through the following chain:

redesigned step → absorbed current activities → satisfied constraints → workflow outcome → evidence ids

The trace appears in both the IR and the report.

## WASM

The core library is pure and has no dependencies beyond FSharp.Core and
System.Text.Json, so it is a candidate for WASM (for example, an in-browser
model editor on Limen). This has not been attempted yet, and no claim of WASM
compatibility is made.
