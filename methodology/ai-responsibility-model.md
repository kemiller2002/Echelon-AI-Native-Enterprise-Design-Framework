---
id: METHOD-EEF-RESPONSIBILITY
title: AI Responsibility Model
status: draft
version: 0.1.0
updated: 2026-11-20
---

# AI Responsibility Model (v0.1)

Implemented in `src/Echelon.Enterprise/Responsibility.fs`. The model decides,
for each redesigned activity, **who or what executes it**. It does not assume
that AI beats deterministic software or human judgment. Deterministic software
is preferred wherever rules suffice, and governance obligations outrank cost.

## Executors

| Executor | Meaning |
|---|---|
| `deterministic-software` | Fully specified rules. Exceptions route to the accountable human, and their cost is counted |
| `ai-assisted-human` | A human acts on every case with AI preparation |
| `agent-screen-human-clearance` | An agent screens every case and may only **raise** flags. A named human alone **clears** flagged cases |
| `supervised-agent` | An agent acts within a closed tool allowlist and escalates to a human role |
| `authorized-human` | A named accountable human decides. AI may prepare but not decide |
| `independent-verification` | A check independent of whoever prepared the work |

## Factors evaluated

Accuracy and judgment (`Judgment`, `InputStructure`, `RuleSpecifiable`),
reversibility, financial exposure, legal requirements (constraint obligations),
explainability, whether the activity *clears* a risk, and, through the
economics engine, cost. Uncertainty enters through the escalation and exception
estimates.

## Rules (ordered; the first that applies decides; all evaluated rules are traced)

| Rule | Condition | Executor | Rationale |
|---|---|---|---|
| R1 | A governing obligation names a human role | `authorized-human` | Legal and fiduciary authority cannot be delegated to software |
| R2 | Verify activity under an independence obligation | `independent-verification` | Segregation of duties |
| R3 | Rule-specifiable over structured inputs | `deterministic-software` | Cheapest, auditable and predictable. Rules are approved by humans under policy |
| R4a | Clears a risk under `NoAiClearance`; reversible; exposure below major | `agent-screen-human-clearance` | "AI may add a flag, never clear one" (daf-landscape report l.232) |
| R4b | Clears a risk under `NoAiClearance`, otherwise | `ai-assisted-human` | Unbounded consequence keeps a human on every case |
| R5 | Irreversible, or major financial exposure | `ai-assisted-human` | Consequence |
| R6 | Moderate+ judgment, explainability required, moderate+ exposure | `ai-assisted-human` | Accountable judgment |
| R7 | Otherwise | `supervised-agent` | Bounded, reversible judgment work |

R3 deliberately precedes R5. A rule-specifiable payment release, run after
every control has passed, is better done by audited deterministic code than by
a human re-keying it. The *decision* to pay was made by the controls, and the
human authority (R1) where one applies.

## Agent governance (enforced in the generated architecture)

Every agent in the agent catalog has:

- a **closed tool allowlist**, fixed at deployment, with no operation to grant
  or modify permissions (no agent can expand its own authority);
- **no** operation reserved to human authority (checked: violation if present);
- **no** clearance operation for a step under `NoAiClearance`. Screening
  agents receive only the separate `-SCREEN` operation (checked);
- escalation roles, evaluation criteria (accuracy against an independently
  labelled sample, escalation rate within the assumed range) and evidence
  requirements (cite inputs; record model, prompt and policy versions);
- monitoring thresholds derived from the economic assumptions, with suspension
  on any material error.

Agents are rolled out in the **last** roadmap phase, behind an evaluation gate
and human approval.

## Open questions

- Whether "no flag raised" by a screening agent amounts to clearance by
  omission. In the DAF case the deterministic eligibility check carries the
  clearance, and the agent only adds flags. Whether regulators accept that
  reading is unverified (Clarity brief: load-bearing).
- Calibration of the rule order against real incident data.
