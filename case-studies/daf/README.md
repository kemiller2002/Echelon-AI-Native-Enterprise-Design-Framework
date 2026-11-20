# Case Study A: Donor-advised funds, grant recommendation to disbursement

- **Model:** [`../Echelon.Enterprise.CaseStudies/Daf.fs`](../Echelon.Enterprise.CaseStudies/Daf.fs)
- **Generated report:** [`generated/analysis.md`](generated/analysis.md)
- **IR:** [`generated/analysis.json`](generated/analysis.json)
- **Evidence (EKS records):** [`knowledge/evidence/`](knowledge/evidence/)

Regenerate with `dotnet run --project src/Echelon.Enterprise.Cli -- analyse daf`
and `... export-eks daf`. Pinned by `tests/.../DafCaseStudyTests.fs`.

## What was modelled

A **composite** current workflow for a representative mid-sized sponsor: 22
activities assembled from published policies of Fidelity Charitable, Vanguard
Charitable, NPT, AEF and RCF/Ren in the daf-landscape corpus. It is not any
single sponsor's process. Seven governing constraints are modelled, among them:

- §4966 eligible recipient
- §4967 no prohibited benefit, under "AI may add a flag, never clear one"
- OFAC screening
- sponsor legal control
- officer authority for large grants
- independent payee verification

**Evidence standing.** 20 evidence records: 8 verified, 4 reported, 3 assumed
and 5 illustrative-only. Statutes, policies and published timings are
verified. **All effort, delay, exception, volume and cost inputs are
illustrative**, because no sponsor publishes them (EV-DAF-012).

## Results (modelled, not measured)

| | Current | Redesign |
|---|---|---|
| Steps | 22 | 13 |
| Human handoffs | 9 | 1 |
| Human touches per grant | 18 | 1 (plus flagged and exception cases) |
| Sequential stages | 22 | 10 |
| Simulated mean cycle (hours) | 393 | 276 |
| Human minutes per grant (simulated) | 46.8 | 1.4 |
| Annual cost at expected values | $1.99m | $0.41m |

- **Classification:** reinvention (structural change ≥ 25%).
  - Replaced by events: re-keying (A01, A17), internal queues (A04, A09), forwarding (A07) and the donor status email (A12).
  - Merged: compliance re-verification (A08) into the eligibility check.
  - Eliminated: rework (A03) and the monthly Schedule I spreadsheet (A22).
- **Responsibility:**
  - Eligibility (BMF, Pub 78, OFAC), validation, approval under policy, redemption, payment release, remittance and reconciliation become deterministic. Their exceptions are costed to the accountable human.
  - Purpose and benefit review is agent-screened with analyst clearance.
  - Large grants keep personal officer authorisation.
  - Payee verification is independent.
- **Economics: superior at expected values only.** The combined adverse corner
  reverses the result (−$174k). Break-even is about 4,600 grants a year. No single
  lever reverses it alone.
- **Cycle time:** the reduction is limited (−30%). Custodian settlement,
  charity deposit and reconciliation latency dominate, and none is under the
  sponsor's control.

## What the lenses say (and where they disagree)

- **Clarity Lean brief:** *defer*. Measure effort, exception rates and volume
  before adopting. The conclusion rests on illustrative inputs.
- **EDF packet:** *proceed with safeguards*. The A06 screening agent sits on a
  single point of failure, so fallback and independent monitoring come first.
- Both disagreements with the economics verdict are preserved in the report.

## Plausibility check (not validation)

The modelled current labour cost of about $45 per grant sits inside the range
of observed all-in operating cost per grant (Fidelity $68, DAFgiving360 $55,
Vanguard $134, NPT $419; EV-DAF-008). This shows the illustrative inputs are
not absurd. It does not validate them.

## What would change the conclusion

Ranked by the sensitivity table:

1. **Hands-on effort per activity.** A measured time study at one sponsor
   (daf-landscape backlog Q04/Q-N03).
2. **Volume.** Below about 4,600 grants a year the redesign's fixed costs dominate.
3. **Exception rates** for eligibility (proxy: 7.4–25.1% of recipients not
   on standard lists; EV-DAF-009).

## Generalisation notes (for Phase 4)

These look reusable:

- intake validation
- reference-data eligibility screening
- judgment review with AI screening and human clearance
- authorisation thresholds
- liquidity or funding handoff to an external party
- independent payee verification
- payment and remittance
- reconciliation to a record of control

DAF-specific: §4966/§4967, sponsor legal control, and DAF liquidity via redemption.
