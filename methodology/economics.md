---
id: METHOD-EEF-ECONOMICS
title: Enterprise Economics Method
status: draft
version: 0.1.0
updated: 2026-11-20
---

# Enterprise Economics Method (v0.1)

Implemented in `src/Echelon.Enterprise/Economics.fs`. **The engine must be
able to conclude that a redesign is economically inferior**, and tests prove
that it does (`EconomicsTests.fs`).

## Cost model (per transaction unless stated)

| Component | Current | Redesign |
|---|---|---|
| Human labour | effort × (1 + exception rate) × loaded rate | assisted/authorised: effort × assisted-effort factor × (1 + exception rate) × rate. Agent-screened: effort × factor × flagged share × rate. Supervised agent: escalation rate × escalation effort × rate. Deterministic: exception share × effort × factor × rate of the accountable role |
| Machine | deterministic cost per software step | inference per agent or assisted step, deterministic cost per step |
| Risk loss | error rate × loss per error | redesign error rate × loss per error |
| Fixed (annual) | current fixed | redesign fixed + one-time cost ÷ amortisation years |

Annual total = variable × volume + fixed. Units of measure (`usd`, `minute`,
`hour`, `txn`) are checked by the F# compiler.

## Uncertainty

Every input is a three-point estimate with evidence. The engine evaluates:

1. **Expected**: all inputs at their expected point.
2. **One-at-a-time sensitivity**: each of 16 levers at low and at high, with
   the others at expected. A lever that reverses the sign of savings is
   **load-bearing**.
3. **Adverse corner**: every lever at whichever end hurts the redesign (the
   direction is found empirically from step 2).
4. **Favourable corner**: the opposite.
5. **Scale curve**: savings at 0.1× to 10× expected volume, plus the
   break-even volume.

## Verdicts

| Verdict | Condition |
|---|---|
| `robustly-superior` | Savings > 0 even at the adverse corner |
| `superior-at-expected` | Savings > 0 at expected values only. The report lists load-bearing levers, or states that only the combined corner reverses it |
| `inferior-at-expected` | Savings ≤ 0 at expected values, but favourable assumptions could reverse it |
| `robustly-inferior` | Savings < 0 even at the favourable corner |

## Benchmarks vs. assumptions

Each sensitivity row shows the evidence **basis** of the lever (verified,
reported, assumed, illustrative or unsupported). A conclusion resting on
illustrative levers is presented as such, and the Clarity brief turns that into
a "defer and measure" stance.

## Limitations

- Correlated uncertainty is not modelled. The corners are extreme and jointly
  improbable, which is deliberately conservative.
- Revenue, customer acquisition and capital requirements are not yet modelled
  (the case studies so far are cost-centre workflows). See the gap register.
- Risk-adjusted economics uses a single loss-per-error estimate, not a loss
  distribution.
