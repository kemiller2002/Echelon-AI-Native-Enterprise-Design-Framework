# Simulations (digital twin v0)

`src/Echelon.Enterprise/Simulation.fs` is a minimal, pure, seeded
discrete-event simulator:

- arrivals (exponential)
- tasks with triangular service and delay drawn from model estimates
- capacity-limited human role pools, served FIFO in order of readiness
- escalations and exception handling routed to human roles

It reports mean, P50 and P90 cycle time, human minutes per transaction, and
role utilisation with the bottleneck.

**Modelled, not measured.** Every input is an estimate from the model, and
results inherit its evidence basis.

**Known limits:**

- a single calendar clock (no working hours)
- no breakdowns or failure injection
- no branching by transaction type
- no preemption

Failure-propagation analysis is structural (`Lenses.propagation`), not simulated.

Experiments comparing alternative designs will be recorded here.
