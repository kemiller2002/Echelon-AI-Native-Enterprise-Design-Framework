---
id: ADR-0002
title: Interface to Clarity and EDF through labelled input packets; preserve disagreement
status: accepted
date: 2026-11-20
---

# ADR 0002: Interface to Clarity and EDF through labelled input packets

## Context

The mission says to use Clarity and EDF as independent analytical components
without recreating them. The audit found that both are Markdown methodologies
with no executable API, and that each repository's own research reports no
measured accuracy advantage.

## Decision

- `Lenses.clarityBrief` produces the inputs for a Clarity Lean review: the
  decision, an evidence-basis check, the strongest rival, load-bearing
  assumptions from the sensitivity analysis, conflicting evidence, hypotheses,
  and an act-or-wait stance with reconsider-if conditions.
- `Lenses.edfPacket` produces the inputs for an EDF diagnosis: manifestation,
  origins, structural propagation, single points of failure as candidate
  control points, and unknowns.
- Each `LensReport` states in `Method` what it does **not** do, and lists its
  `Limitations`.
- Lenses never read each other's output. `Lenses.disagreements` reports where
  the economics verdict and a lens stance diverge, and does not resolve the
  conflict.

## Consequences

- No false claim of integration. When Clarity or EDF gain executable forms,
  the packets become their inputs.
- Interpretation still needs a reviewer.
