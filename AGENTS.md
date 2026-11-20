# Agent contract: Echelon AI-Native Enterprise Design Framework

## Purpose

A repeatable, evidence-driven system for discovering how an enterprise should
operate, designing an AI-native alternative, testing whether it is superior,
and generating the architecture to implement it. Industry-independent: sector
facts live only in `case-studies/`.

## Rules

1. **F# only** for implementation (`src/`, `tests/`, `case-studies/`). Functional F#: immutable data, pure functions, effects only in `src/Echelon.Enterprise.Cli`.
2. **Evidence or nothing.** Every model element cites evidence ids. Illustrative values are tagged `Illustrative` and never presented as facts. Never fabricate sources, results or approvals.
3. **Preserve contradictions and disagreements.** Do not resolve them in code or prose.
4. **Every implementation change has tests.** CI fails on an empty or skipped run. Invariants that must hold for any model belong in `PropertyTests.fs`.
5. **Do not weaken governance invariants** (controls never dropped; human authority preserved; AI never clears under `NoAiClearance`; agents never hold human-authority operations) to make a case study pass.
6. **Read-only dependencies.** Do not modify other Echelon repositories.
7. **No new infrastructure or runtime dependencies** without an ADR in `docs/decisions/`.
8. **Branches.** `main` is protected. Work on a branch, open a pull request, never force-push.
9. **Durable state.** Update `progress/LEDGER.md` with what was verified, and the commands that verified it.

## Verify before you push

```bash
dotnet tool restore
dotnet fantomas --check src tests case-studies
dotnet test Echelon.Enterprise.sln -c Release
dotnet run --project src/Echelon.Enterprise.Cli -c Release -- analyse all   # regenerate case-study outputs
```

Generated files under `case-studies/*/generated/` and `case-studies/*/knowledge/` are committed and must be regenerated, never hand-edited.
