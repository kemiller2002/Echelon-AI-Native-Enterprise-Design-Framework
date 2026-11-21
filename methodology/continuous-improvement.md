---
id: METHOD-EEF-IMPROVEMENT
title: Continuous Improvement Method
status: draft
version: 0.1.0
updated: 2026-11-20
---

# Continuous Improvement Method (v0.1, specification only)

**Status: designed, not implemented.** No code in this repository yet observes
operational data. This document defines the loop so the implemented engines
can plug into it.

```
Observe → Diagnose → Hypothesize → Simulate → Test → Approve → Deploy → Verify
```

| Stage | Input | Framework component | Output |
|---|---|---|---|
| Observe | Operational event logs (the event catalog defines what is emitted) | *(to build)* event-log ingestion | Measured effort, delay, exception and escalation rates |
| Diagnose | Measured rates vs. the model's estimates | Validation + EDF packet | Discrepancies, bottlenecks, propagation paths |
| Hypothesize | Discrepancies | Reinvention engine on the updated model | Candidate redesigns, each disposition a hypothesis |
| Simulate | Candidate model | Simulation (digital twin v0) | Throughput, cycle time, utilisation |
| Test | Candidate | Economics sensitivity, Clarity brief, architecture invariants; Dokimos for code-level verification (*to integrate*) | Pass or fail against the stated thresholds |
| Approve | Test results | **Human approval** (mandatory for any material change) | Recorded decision with owner |
| Deploy | Approved change | Roadmap phases and gates | Change in production |
| Verify | Post-deployment observations | Observe again | Confirmed or refuted hypothesis |

## Signals the loop must detect

Redundant activities, recurring exceptions, unnecessary human intervention,
poorly designed interfaces (re-keying, i.e. Transfer activities), excessive
processing time, inefficient API interactions, weak controls (EEF011 and
control-coverage gaps), unexpected costs, and organisational bottlenecks
(simulation utilisation).

## Whole-workflow elimination

The reinvention engine already returns `workflow-redundant` when an outcome's
required information is available at the trigger and the workflow carries no
governing control. In the loop, this check re-runs whenever another workflow
begins producing the same information.

## Rules

- No material operational change without independent testing and human
  approval.
- Agents may propose hypotheses but may not approve, deploy or widen their own
  permissions.
- Measured values replace estimates only with an `Observation` source.
  Estimates are never silently overwritten.
