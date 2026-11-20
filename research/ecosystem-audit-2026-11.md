---
id: RP-EEF-2026-001
title: Echelon ecosystem and DAF research audit
status: accepted
updated: 2026-11-20
---

# Echelon ecosystem and DAF research audit (2026-11-20)

Read-only audit of the repositories this framework depends on. All
repositories were shallow-cloned at their default branch on 2026-11-20. No
dependent repository was modified. "Verified" means the auditor ran it in this
session; everything else is read from the repository's own files.

## Summary

| Repository | What it is | Maturity | Integration used here | Verified |
|---|---|---|---|---|
| **echelon-knowledge-store** | F# knowledge compiler: Markdown + YAML front matter → validated IR (27 diagnostic codes) | Research (0.0.0, `compilerVersion 0.1.0-research`); real code, 78 xUnit tests | `eef export-eks` writes EKS evidence records, validated by `eks validate` | **Yes**: built, 78/78 tests pass; `eks validate` on the DAF export: 20 records, 0 errors, 0 warnings |
| **clarity-framework** (Clarity) | Methodology for making claims, evidence, assumptions and confidence explicit; decision posture model | Markdown only; F# harness is experiment-analysis code (net8.0). Own research: "no more accurate", recommends "Clarity Lean" | `Lenses.clarityBrief` prepares Lean inputs (decision, evidence check, rival, load-bearing assumptions, act-or-wait). **Does not perform a Clarity review** | Read only |
| **echelon-diagnostic-framework** (EDF) | Diagnostic grammar v0.3: outcome, manifestations, origin network, propagation, control points | Methodology; no schema for an analysis; own research reports no measurable advantage (four nulls) | `Lenses.edfPacket` maps model facts onto the grammar; structural propagation and single points of failure. **Does not perform an EDF diagnosis** | Read only |
| **praxis** | Repository operating system (work protocol, records, telemetry); v3.25.0; ~104k LOC F# | Mature; CLI and .NET tool | Not installed here (see ADR 0003 and the gap register) | Read only |
| **dokimos** | Evidence-based code-quality measurement; CLI tool v0.11.1 | Operational CLI | Planned for code-level verification in the improvement loop | Read only |
| **aegis** | Exception-to-`Result` fault capture; NuGet 2.0.0 (net8.0) | Operational library | Not needed: the core is pure and total; the CLI has three I/O calls | Read only |
| **limen** | WASM browser-kernel boundary; npm 0.9.1, NuGet packages | Operational | Candidate runtime for Product C; not used | Read only |
| **forma** | Design system (HTML/CSS tokens) | Operational (npm 1.0.2) | Candidate UI for a future report viewer; not used | Read only |
| **folio** | Print/PDF web components | Operational (npm 0.4.2) | Candidate for rendered reports; not used | Read only |
| **echelon-engineering-system** | Static documentation site (F#) | Operational | Conventions only | Read only |

## Echelon engineering conventions adopted

From echelon-knowledge-store `AGENTS.md` and Praxis governance:

- F# only for implementation; functional style (immutable data, pure functions,
  effects at the edges)
- every implementation change has tests; CI fails an empty or skipped test run
- xUnit 2.9.2 / Microsoft.NET.Test.Sdk 17.11.1 (the common Echelon versions)
- Fantomas 8.1.1 formatting check; `TreatWarningsAsErrors`; deterministic builds
- .NET 10 SDK (`global.json` 10.0.400, `rollForward: latestFeature`), net10.0
- `main` protected; work on `claude/*` branches; PRs; never force-push
- measured vs. hypothesised claims distinguished; no fabricated results
- no new infrastructure without recorded evidence of need

## Findings that shaped the design

1. **Knowledge-store's executive report recommends keeping Markdown canonical
   and compiling it**, not inventing a new format. Enterprise *models* are
   structured data with cross-references and units, which F# types check
   better than front matter (ADR 0001). Enterprise *evidence* is exported in the
   EKS format so the EKS compiler can validate it.
2. **Clarity and EDF are not executable**, and neither has shown measured
   accuracy gains. The framework therefore defines *interfaces* (a Clarity Lean
   brief and an EDF packet) and labels each with what it does not do (ADR 0002).
   Their outputs are kept separate from the economics verdict, and conflicts are
   reported as disagreements.
3. **EDF is a diagnostic grammar, not a failure-mode or resilience-scoring
   method.** The mission's description of EDF ("failure analysis, resilience,
   dependencies") overstates what exists. Structural propagation and
   single-point-of-failure analysis are implemented here and attributed to this
   framework, not to EDF.
4. **Target-framework drift**: Aegis, Dokimos, Limen and the Clarity harness
   are net8.0; EKS and Praxis are net10.0. This repository targets net10.0.

## daf-landscape (Case Study A source)

- 410 documents, 764 CSVs and 2,557 validation rows (`research/mission-2026-11-14/FINAL-STATUS.md:13-21`), with explicit labels: [V] validated, [F] first-party, [C] computed, [I] inference, [S] scenario.
- **Verified layer:** statutes (§4966, §4967, §4958), sponsor grant policies, published timings, fees, payment rails, and audited flows.
- **Not available anywhere:** reviewer minutes, exception rates, turnaround distributions or error costs (report l.220, l.224).
- **Reusable scaffolding:**
  - normalised 8-stage grant lifecycle (`batch-2a…:36-43`)
  - proposed grant state machine (`batch-2c…:15-16`)
  - T0–T8 measurement clocks
  - 20 synthetic adjudication cases G01–G20
  - "AI may add a flag, never clear one" (report l.232)
- **Documented conflict, preserved in the model as EV-DAF-007:** approval cadence is weekly in one programme circular, daily in another, and "instant" per Ren.

## Not verified

- Whether NuGet.org hosts the Aegis and Limen packages
- Conditor, Ordo and clarity-service (not audited)
- Builds of any repository other than echelon-knowledge-store
