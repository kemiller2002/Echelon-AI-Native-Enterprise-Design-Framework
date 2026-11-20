# Progress ledger

Durable record of what has been done, verified and decided. Newest first.
An executor session is disposable; this file and the repository are the
continuity boundary.

## 2026-11-20: Milestone 1 (foundation and DAF vertical slice)

**Branch:** `claude/echelon-enterprise-framework-1fkzcp`

### Acceptance criteria (mission §18)

| # | Criterion | Status | Evidence |
|---|---|---|---|
| 1 | Repository correctly configured | Done | `global.json`, `Directory.Build.props`, solution, `.config/dotnet-tools.json`, CI workflow, `AGENTS.md` |
| 2 | Echelon landscape audited | Done | `research/ecosystem-audit-2026-11.md` |
| 3 | Formal discovery methodology | Done (v0.1, unexercised in a real engagement) | `methodology/enterprise-discovery.md` |
| 4 | F# enterprise domain model compiles | Done | `src/Echelon.Enterprise`; warnings are errors |
| 5 | Evidence and assumptions explicit | Done | `Evidence.fs`; EEF005/EEF006; basis on every economic lever |
| 6 | Workflow reinvention methodology | Done | `methodology/workflow-reinvention.md`, `Reinvention.fs` |
| 7 | Representative DAF workflow modelled | Done (composite; inputs illustrative) | `case-studies/daf/` |
| 8 | Initial architecture specification generated | Done | `case-studies/daf/generated/analysis.md` §Generated architecture |
| 9 | Tests validate the implementation | Done | 87 xUnit tests, including 9 property tests over 300 generated workflows |
| 10 | Research gaps and next priorities documented | Done | `progress/research-gaps-and-next-priorities.md` |

### Verified in this session (commands run and passing)

- `dotnet test Echelon.Enterprise.sln -c Release`: 87 passed, 0 failed
- `dotnet fantomas --check src tests case-studies`: clean
- `eef validate daf`: 0 errors, 1 advisory (preserved contradiction)
- `eef analyse daf`: 0 architecture invariant violations
- `eks validate` (echelon-knowledge-store CLI) over `eef export-eks daf`: 20 records, 0 errors, 0 warnings
- `case-studies/daf/generated/analysis.json` validated against `schemas/eef-analysis-0.schema.json` (jsonschema, draft 2020-12): 0 errors
- echelon-knowledge-store builds and passes its 78 tests on .NET 10.0.404

### Defects found and fixed during this milestone

The property tests and the EKS compiler each caught real defects.

- **Reinvention could drop a control.** A handoff, queue, notification or rework step that carried a governing control was replaced. It is now retained.
- **Merges could drop a control.** An output-equivalence merge could fold away an activity with controls its target lacked. Merges now require control coverage.
- **Rework elimination could break data flow.** It could remove the sole producer of required information. Such steps are now retained.
- **Whole-workflow redundancy could discard controls carried by the workflow.** Redundancy now requires that the workflow carries none.
- **"No automated clearance" was ambiguous.** It became `NoAiClearance`: deterministic rules approved under policy may clear, and AI may not.
- **The simulator processed transactions in sequence**, inflating cycle times about 90×. It is now event-ordered.
- **EKS export failed validation** (missing source titles; non-lifecycle statuses). Fixed and re-validated.

### Decisions

- ADR 0001: typed F# models; JSON IR; evidence exported to EKS
- ADR 0002: Clarity and EDF via labelled input packets; disagreements preserved
- ADR 0003: minimal dependencies; Praxis adoption deferred to the owner
