# Research gaps and next development priorities

## Decisions needing the repository owner

1. **Praxis adoption.** Installing Praxis (`praxis init`) adds the standard
   Echelon governance scaffolding (ros.json, .ros/, launchers, AGENTS/BOOTSTRAP
   templates) and its work protocol. Not done without authorisation (ADR 0003).
2. **Licence.** Other Echelon repositories use PolyForm Noncommercial 1.0.0. No
   licence has been added here, because licensing is the owner's call.
3. **Branch protection** on `main` (Echelon convention) is a GitHub setting this
   session cannot change.

## Evidence gaps (largest effect on conclusions first)

| Gap | Effect | How to close |
|---|---|---|
| DAF hands-on effort per activity | The economics verdict reverses at the adverse corner | Time-and-motion study at one sponsor (daf-landscape Q04/Q-N03) |
| DAF exception rates by activity | Drives human cost of deterministic and screened steps | Sample of 500 recommendations with outcomes; reuse G01–G20 as labelled cases |
| Representative volume and headcount | Break-even about 4,600 grants a year; utilisation | Form 990 grant counts by sponsor size band (in daf-landscape data) |
| Assisted-effort factor for AI-supported review | Load-bearing in R4–R6 steps | Controlled trial on G01–G20 cases |
| Regulator acceptance of "agent screens, deterministic rules clear" | Governance viability of R4a | Legal review; IRS guidance search |

## Framework gaps (engineering)

| Priority | Gap | Phase |
|---|---|---|
| P1 | Healthcare administration case study (prior authorisation slice) | 4 |
| P1 | Insurance claims case study (FNOL to payment slice) | 4 |
| P1 | `progress/generalisation-log.md`: record every core change a new case forces | 4 |
| P2 | Activity splitting: let the engine split clearance activities into screen and clear parts instead of relying on the executor approximation | 4 |
| P2 | Conditional applicability (for example, grants over $500k) as branches, not averaged effort | 4 |
| P2 | Simulation: working-hours calendar, transaction types, failure injection | 5 |
| P2 | Revenue, acquisition and capital in the economics engine (needed for non-cost-centre workflows) | 5 |
| P3 | OpenAPI and AsyncAPI emission from the API and event catalogs | 6 |
| P3 | JSON *input* decoder so non-F# authors can supply models | 6 |
| P3 | Cross-workflow analysis (Schedule I derived from the grant record; shared capabilities) | 6 |
| P3 | Dokimos verification obligations from generated architectures | 6 |
| P3 | Continuous improvement: event-log ingestion to replace estimates with observations | later |
| P3 | WASM build of the core (Limen) for an in-browser model editor | later |
| P4 | Commercial evaluation of products A, B and C (`commercial/`) | 7 |

## Not claimed

- Universal applicability: one case study so far.
- That Clarity or EDF reviews were performed. Only their inputs are prepared.
- Any measured performance of the redesigned DAF workflow.
