# Case studies

| Case | Status | Model | Report |
|---|---|---|---|
| A. Donor-advised funds: grant disbursement | **Modelled (Phase 3)** | [`Echelon.Enterprise.CaseStudies/Daf.fs`](Echelon.Enterprise.CaseStudies/Daf.fs) | [`daf/`](daf/README.md) |
| B. Healthcare administration (scheduling, intake, prior authorisation, claims) | Not started (Phase 4) | — | — |
| C. Insurance claims administration | Not started (Phase 4) | — | — |

Case studies are typed F# values (ADR 0001) registered in
[`Echelon.Enterprise.CaseStudies/Catalog.fs`](Echelon.Enterprise.CaseStudies/Catalog.fs).
Industry-specific facts live only here, never in `src/`.

**Phase 4 protocol.** Each new case is modelled with the same types. Wherever a
case needs a new concept, it is recorded in `progress/generalisation-log.md`
before the core changes, with the case that forced it. That record is how
generalisation failures are documented rather than hidden.
Healthcare and insurance cases must preserve clinical judgment, privacy (HIPAA)
and insurer, adjuster and decision-support authority as typed obligations.
