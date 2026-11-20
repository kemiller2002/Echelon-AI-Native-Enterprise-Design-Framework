---
id: METHOD-EEF-REINVENTION
title: Workflow Reinvention Method
status: draft
version: 0.1.0
updated: 2026-11-20
---

# Workflow Reinvention Method (v0.1)

Implemented in `src/Echelon.Enterprise/Reinvention.fs`. This document is the
specification, and the code and its tests are the executable form.

## The core distinction

**Automation** asks: *how can each existing step be done faster or by a
machine?* **Reinvention** asks: *which steps would exist at all if we started
from the outcome?* The framework measures the difference rather than asserting
it:

- **Structural change** is the share of current activities that are eliminated,
  merged, replaced by events or parallelised.
- **Executor change** means the same step is performed by a different kind of
  executor.

| Classification | Rule |
|---|---|
| `workflow-redundant` | The outcome's required information is available at the trigger, and no governing control is carried by the workflow |
| `reinvention` | structural change ≥ 25% |
| `partial-reinvention` | 0 < structural change < 25% |
| `superficial-automation` | no structural change, but executors change |
| `no-change` | neither |

The 25% threshold is a heuristic and is recorded as hypothesis HY-EEF-003. It
has not been calibrated.

## Procedure (the thirteen steps of the mission, as implemented)

| Step | What happens | Where |
|---|---|---|
| 1. Identify the required outcome | `Outcome` with `RequiredInformation` | model |
| 2. Identify the actual constraints | `Workflow.Governing` → typed `Obligation`s | model |
| 3. Determine the minimum information | **Backward closure** from the outcome's required information and constraint-required data through the producers of each item | `Reinvention.necessaryData` |
| 4. Determine the necessary decisions | Decide activities whose outputs are in the closure, or which carry a governing control | `dispose` |
| 5. Eliminate unnecessary activities | Produce, Decide or Verify activities that produce nothing in the closure and carry no governing control | `dispose` |
| 6. Combine redundant operations | Activities producing the same information, or repeating the same control, are merged, but only if the target satisfies every control the merged activity did, and never under an independence obligation | `duplicateOf` |
| 7. Replace sequential handoffs | Internal `Transfer`, `Wait` and non-required `Notify` activities are replaced by a shared system of record plus events. External latency is retained | `dispose` |
| 8–10. Assign executors | The AI Responsibility Model decides deterministic, AI-assisted, agent-screened, supervised agent, authorised human or independent verification | `Responsibility.assign` |
| 11. Design the new workflow | Steps sequenced by **data dependencies**, not by the current order. Steps with no mutual dependency share a stage (parallel) | `buildSteps` |
| 12. Compare | Steps, human handoffs, human touches, stages, structural queue delay | `StructuralMetrics` |
| 13. Validate | Economics (`Economics.compare`), simulation, Clarity brief, EDF packet, architecture invariants | `Analysis.run` |

## Safety rules (enforced by property tests)

These invariants are checked against 300 randomly generated workflows in
`tests/.../PropertyTests.fs`:

1. **No governing control is ever dropped.** A Transfer, Wait, Notify or Rework
   that itself carries a governing control is retained, because the control
   must be redesigned explicitly and not removed as a side effect.
2. **Human authority survives.** Work under a `HumanAuthorization` obligation
   is always assigned to that role.
3. **AI never clears** a concern under `NoAiClearance`.
4. **Information flow is preserved.** Every input of every redesigned step is a
   workflow input, produced by a retained step, or carried by the shared record
   that replaced a handoff. A rework step that is the sole producer of required
   information is retained.
5. **Ordering is acyclic.** Every dependency is in an earlier stage.
6. **Determinism.** The same model always produces the same redesign.

## Workflow specification output

Each redesigned step carries the elements the mission requires:

| Required element | Representation |
|---|---|
| Event triggers | Event catalog: one `<Data>Recorded` event per produced datum, with producer and consumers |
| State transitions | Stage order and `DependsOn` edges; events mark transitions |
| API calls | One operation per internal step (`API-<step>`), plus `-SCREEN` operations for agent screening |
| Agent responsibilities | Agent catalog with closed tool allowlists |
| Controls | Control-coverage table: obligation → covering steps → audit evidence |
| Exceptions | Activity exception rates. Escalation paths are given by the executor (escalation role or clearance role) |
| Completion criteria | `Workflow.Completion` and the outcome's required information |

## Every disposition is a hypothesis

The engine's reasons ("handoff removed: shared system of record and an event
replace re-keying") are design hypotheses, not findings. The Clarity brief
lists every non-retained disposition as a hypothesis to validate before
implementation.

## Known limitations

- Activities are atomic. The engine cannot split one activity into "screen"
  and "clear" parts. The agent-screened executor approximates that split
  (`methodology/ai-responsibility-model.md`).
- Conditional applicability (for example, "only grants over $500k") is
  represented by averaging effort across all transactions, not by branching.
- Retained waits are sequenced between neighbouring steps, a positional
  heuristic.
- Elimination is judged per workflow. Cross-workflow reuse (for example,
  Schedule I compiled from the grant record) is asserted in the reason text,
  not yet verified against another modelled workflow.
