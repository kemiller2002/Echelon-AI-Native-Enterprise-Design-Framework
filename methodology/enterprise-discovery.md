---
id: METHOD-EEF-DISCOVERY
title: Enterprise Discovery Method
status: draft
version: 0.1.0
updated: 2026-11-20
---

# Enterprise Discovery Method (v0.1)

**Purpose.** Investigate any organisation or industry well enough to answer the
framework's central question:

> If this organisation did not exist today, how would we build it to accomplish
> the same fundamental outcomes with fewer unnecessary activities, lower costs,
> stronger controls and better results?

Discovery produces an `EnterpriseModel` (see `src/Echelon.Enterprise/Model.fs`).
Every element of the model carries evidence ids, and every piece of evidence
carries a source, an epistemic status and a confidence. The method is designed
to work with **incomplete, contradictory and uncertain information**. It records
what is missing rather than filling gaps silently.

## 1. Principles

1. **Outcomes before processes.** Ask what must be true for stakeholders before
   asking what people do today. A current activity is evidence about the
   enterprise, not a requirement of it.
2. **Constraints are cited, not assumed.** Each constraint names the statute,
   regulation, contract clause or policy that imposes it. "We have always done
   it this way" is recorded as a *policy* constraint and flagged for challenge.
3. **Provenance on everything.** No statement enters the model without a
   `Source`. Values chosen only to make the model run are tagged `Illustrative`
   and can never be presented as facts (validation rule EEF006).
4. **Status is never upgraded by the framework.** A vendor claim stays a claim
   until independently verified (`Evidence.isVerified`).
5. **Contradictions are preserved.** Conflicting evidence is linked with
   `Contradicts` and carried into every report (EEF010). The framework does not
   pick a winner.
6. **Absence is a finding.** "No sponsor publishes reviewer minutes" is recorded
   as verified evidence of an information gap, and it is not treated as a zero.

## 2. Discovery dimensions and standard research questions

Each dimension maps to model elements. Ask the questions in roughly this order.
Later answers often revise earlier ones.

| # | Dimension | Standard research questions | Model element(s) |
|---|---|---|---|
| D1 | Stakeholder needs | Who must be better off, and in what observable way? Who can stop the enterprise operating? | `Stakeholder` |
| D2 | Business outcomes | What must be true when the work is done? What is the minimum information that proves it? How is it measured? | `Outcome.RequiredInformation`, `Measures` |
| D3 | Economic structure | How does revenue arise? What is the cost per unit of outcome? What scales with volume and what is fixed? | `EconomicAssumptions` |
| D4 | Regulatory and contractual constraints | Which rules apply, and what exactly do they oblige? Who is liable? Which obligations need a named human? | `Constraint`, `Obligation` |
| D5 | Capabilities | What must the enterprise be able to do, whoever does it? Which capability is the system of record for each datum? | `Capability.Owns` |
| D6 | Current workflows | What happens, in what order, by whom, with what inputs and outputs, and how long does each step take and wait? | `Workflow`, `Activity` |
| D7 | Technology dependencies | Which systems perform or mediate each activity? Where is data re-keyed? | `Performer.Software`, `Transfer` activities |
| D8 | Data dependencies | What information does each step consume and produce? Where is the authoritative copy? | `DataItem`, `Inputs`/`Outputs` |
| D9 | Decision rights | Who decides what, on what information, under what authority? What may be delegated? | `Decision`, `HumanAuthorization` |
| D10 | Risk and failure modes | What goes wrong, how often, at what cost? What propagates? | `ExceptionRate`, EDF packet |
| D11 | Customer experience | Where do stakeholders wait, repeat themselves or lack status? | `Delay`, `Notify` activities |
| D12 | Competitive structure | Who else delivers this outcome, how, and at what price? | case-study research notes |
| D13 | Operating costs | Loaded labour rates, fixed platform costs, error losses | `Role.LoadedCostPerHour`, economics |
| D14 | Structural inefficiencies | Handoffs, queues, re-keying, duplicate checks, rework, reports nobody consumes | dispositions (derived) |
| D15 | Commercial opportunities | Which inefficiencies are common across the industry and costly enough to sell against? | `commercial/` |

## 3. Evidence schema

Implemented as `Evidence` in `src/Echelon.Enterprise/Evidence.fs`, exported to
the IR (`schemas/eef-analysis-0.schema.json`) and to echelon-knowledge-store
records (`eef export-eks`).

| Field | Meaning |
|---|---|
| `id` | Stable, never reused, e.g. `EV-DAF-005` |
| `statement` | One verifiable proposition |
| `status` | `fact` · `claim` · `assumption` · `hypothesis` · `recommendation` |
| `confidence` | `very-low` … `very-high`. Qualitative, aligned with EKS; no numeric scores |
| `sources[]` | `{citation, locator, kind}`, with kind ∈ primary, secondary, vendor-claim, interview, observation, computed, illustrative |
| `contradicts[]` | Ids of conflicting evidence |

**Derived basis** (reported for every quantity): `verified` > `reported` >
`assumed` > `illustrative` > `unsupported`. A quantity's basis is the *weakest*
of its evidence.

## 4. Interview guide (semi-structured, 45–60 minutes)

Use for operators, managers, compliance and finance staff. Record answers as
`Interview` sources. Never record personal data beyond role and date.

1. **Context.** What is your role? Which outcomes are you accountable for?
2. **Walk-through.** Take one recent, ordinary transaction from trigger to
   completion. What did you receive? From whom? What did you do? Whom did you
   pass it to?
3. **Time.** How long did your hands-on work take? How long did it wait before
   you picked it up, and why?
4. **Information.** What did you need to know to do it? Where did you find it?
   Did you re-enter anything that already existed elsewhere?
5. **Decisions.** What did you decide? On what basis? Could you write the rule
   down? When do you escalate?
6. **Controls.** Which rule or policy requires this step? What would happen if
   it were skipped? Who checks your work?
7. **Exceptions.** What fraction of cases are not ordinary? What are the top
   three kinds? What does each cost you?
8. **Errors.** What went wrong in the last year? How was it caught?
9. **Counterfactual.** If you could remove one step, which would it be, and
   what would break?
10. **Evidence.** Can you show me the system screen, report or policy you just
    described? *(Observation beats recollection.)*

Close by asking who would disagree with your account. Interview that person.

## 5. Analytical procedure

1. **Frame** the scope: one outcome, one trigger, one boundary.
2. **Collect** primary sources first (statute, filings, policy documents), then
   secondary, then interviews and observation. Log every source.
3. **Model outcomes and constraints** before workflows. Express each obligation
   as a typed `Obligation`.
4. **Model the current workflow** as activities with typed nature, performer,
   inputs, outputs and the constraints they satisfy. Estimate effort, delay and
   exception rate as three-point estimates with evidence.
5. **Validate** (`eef validate`). Resolve errors; read every warning. EEF011
   ("constraint satisfied by no activity") means either a model gap or a real
   compliance gap. Find out which.
6. **Mark gaps.** Any estimate resting only on illustrative or assumed
   evidence goes to the research backlog with the measurement that would
   replace it.
7. **Analyse** (`eef analyse`): reinvention, economics, architecture, lenses,
   simulation.
8. **Challenge** with the Clarity brief and EDF packet. Do not treat the
   framework's output as a conclusion until a reviewer has worked through the
   load-bearing assumptions.

## 6. Working with incomplete or contradictory information

| Situation | Treatment |
|---|---|
| No data | Three-point estimate with `Illustrative` source; flagged in sensitivity tables |
| Vendor claim only | `Claim` status, `VendorClaim` source; cannot support a fact |
| Two sources disagree | Two evidence records linked by `Contradicts`; both cited; carried to reports |
| Aggregate only (e.g. Form 990) | `Computed` source with the derivation path; the denominator stated in the statement |
| Policy may be stale | Statement includes the policy date; confidence capped at medium |

## 7. Outputs

- A validated `EnterpriseModel` (typed F#; see ADR 0001)
- `generated/analysis.json` (IR) and `generated/analysis.md` (report)
- `knowledge/evidence/*.md` (EKS records)
- A research backlog of the measurements that would most change the conclusion
  (the sensitivity table ranks them)

## Limitations

- Validated on one case study (DAF). Healthcare and insurance applications are
  pending (`progress/research-gaps-and-next-priorities.md`).
- The interview guide has not been used in a real engagement.
