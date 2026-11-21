---
id: ADR-0001
title: Author enterprise models as typed F# values; export evidence to EKS
status: accepted
date: 2026-11-20
---

# ADR 0001: Author enterprise models as typed F# values; export evidence to EKS

## Context

The mission asks for machine-readable, compiler-validated enterprise models,
and says not to depend on agents interpreting Markdown alone. Separately,
echelon-knowledge-store's research recommends keeping Markdown with front
matter canonical for *knowledge records*, compiled and validated.

## Decision

1. Enterprise models (outcomes, constraints, workflows, estimates) are
   authored as **typed F# values** in `case-studies/`. The F# compiler checks
   reference kinds, units and exhaustiveness, and `Validation` checks the rest.
2. The framework's **output** is a versioned JSON IR (`eef.analysis/0`) plus
   a generated Markdown report.
3. **Evidence** is exported as EKS records (`eef export-eks`), so the EKS
   compiler validates provenance and references independently.

## Consequences

- Strong guarantees, and refactoring is safe.
- Authoring requires F#. A JSON *input* decoder for non-F# authors is a
  backlog item.
- Models are data-only: the CLI compiles them; nothing evaluates model content
  at runtime (`dotnet fsi` is not used).
