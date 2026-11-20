# Generalisation log

Every change to `src/` forced by a specific case study is recorded here before
it is made. This is how generalisation failures are documented.

| Date | Case | Concept forced | Core change | Generalises? |
|---|---|---|---|---|
| 2026-11-20 | DAF | "AI may add a flag, never clear one" did not fit a binary agent-or-human choice | `AgentScreenedHumanClearance` executor, rule R4a, `-SCREEN` operations | Expected to (prior authorisation, fraud screening). Untested |
| 2026-11-20 | DAF | Deterministic steps still have exceptions needing humans | Exception cost charged to the accountable role in economics and simulation | Yes (industry-neutral) |
| 2026-11-20 | DAF | External settlement waits have no data dependencies | Retained waits sequenced between neighbouring steps | Positional heuristic; revisit with insurance payments |
