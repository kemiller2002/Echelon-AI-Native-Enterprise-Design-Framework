namespace Echelon.Enterprise

/// A unit of simulated work, derived from either a current activity or a
/// redesigned step. The simulator is indifferent to which.
type SimTask = {
    Id: ActivityId
    /// Human resource pool consumed, if any.
    Role: RoleId option
    Service: Estimate<minute>
    Delay: Estimate<minute>
    /// Probability of an escalation to a human role, with its effort.
    Escalation: (float * RoleId * Estimate<minute>) option
    DependsOn: ActivityId list
}

type SimulationConfig = {
    Seed: uint64
    Transactions: int
    /// Mean minutes between arrivals (exponential inter-arrival times).
    MeanInterArrival: float<minute>
    /// Headcount per human role; roles not listed get DefaultCapacity.
    Capacity: Map<RoleId, int>
    DefaultCapacity: int
}

type SimulationResult = {
    Label: string
    Transactions: int
    MeanCycle: float<minute>
    P50Cycle: float<minute>
    P90Cycle: float<minute>
    HumanMinutesPerTxn: float<minute>
    Utilization: (RoleId * float) list
    Bottleneck: (RoleId * float) option
}

/// Minimal enterprise digital-twin simulator (v0): a pure, seeded
/// discrete-event model of transactions flowing through tasks with
/// capacity-limited human pools. Measured data and modelled assumptions are
/// kept apart in the model; the simulator only consumes estimates.
module Simulation =

    /// SplitMix64: a pure, reproducible generator. Returns a value in [0, 1).
    let next (state: uint64) =
        let s = state + 0x9E3779B97F4A7C15UL
        let z1 = (s ^^^ (s >>> 30)) * 0xBF58476D1CE4E5B9UL
        let z2 = (z1 ^^^ (z1 >>> 27)) * 0x94D049BB133111EBUL
        let z = z2 ^^^ (z2 >>> 31)
        float (z >>> 11) / float (1UL <<< 53), s

    /// Inverse-CDF sample of a triangular distribution over an estimate.
    let triangular (e: Estimate<'u>) (u: float) : float<'u> =
        let a, c, b = float e.Low, float e.Expected, float e.High

        let v =
            if b <= a then
                c
            elif u < (c - a) / (b - a) then
                a + sqrt (u * (b - a) * (c - a))
            else
                b - sqrt ((1.0 - u) * (b - a) * (b - c))

        LanguagePrimitives.FloatWithMeasure v

    let private sample e rng =
        let u, rng' = next rng
        triangular e u, rng'

    let ofWorkflow (w: Workflow) : SimTask list =
        w.Activities
        |> List.pairwise
        |> List.map (fun (prev, a) -> a, [ prev.Id ])
        |> fun rest ->
            (w.Activities |> List.tryHead |> Option.map (fun a -> a, []) |> Option.toList)
            @ rest
        |> List.map (fun (a, deps) -> {
            Id = a.Id
            Role =
                match a.Performer, a.Nature with
                | _, Wait -> None
                | Human r, _ -> Some r
                | _ -> None
            Service = a.Effort
            Delay = a.Delay
            Escalation = None
            DependsOn = deps
        })

    let ofRedesign (e: EconomicAssumptions) (r: Redesign) : SimTask list =
        let scaled (x: Estimate<minute>) = {
            x with
                Low = x.Low * e.AssistedEffortFactor.Low
                Expected = x.Expected * e.AssistedEffortFactor.Expected
                High = x.High * e.AssistedEffortFactor.High
        }

        r.Steps
        |> List.map (fun s ->
            let a = s.Activity

            let role, service, escalation =
                match s.Assignment |> Option.map _.Executor with
                | None -> None, a.Effort, None
                | Some(AiAssistedHuman role)
                | Some(AuthorizedHuman role) -> Some role, scaled a.Effort, None
                | Some(SupervisedAgent role) ->
                    None,
                    Estimate.exactly 1.0<minute> [],
                    Some(e.AgentEscalationRate.Expected, role, e.EscalationEffort)
                | Some(AgentScreenedHumanClearance role) ->
                    None, Estimate.exactly 1.0<minute> [], Some(a.ExceptionRate.Expected, role, scaled a.Effort)
                | Some DeterministicSoftware
                | Some IndependentVerification ->
                    let owner = Responsibility.accountableFor r.Workflow a
                    None, Estimate.zero, Some(a.ExceptionRate.Expected, owner, scaled a.Effort)

            {
                Id = a.Id
                Role = role
                Service = service
                Delay = a.Delay
                Escalation = escalation
                DependsOn = s.DependsOn
            })

    type private Pools = Map<RoleId, float<minute> list>

    let private acquire (cfg: SimulationConfig) (pools: Pools) role (ready: float<minute>) (service: float<minute>) =
        let servers =
            pools.TryFind role
            |> Option.defaultValue (
                List.replicate (cfg.Capacity.TryFind role |> Option.defaultValue cfg.DefaultCapacity) 0.0<minute>
            )

        let free = List.min servers
        let finish = max ready free + service
        let idx = servers |> List.findIndex ((=) free)
        finish, Map.add role (servers |> List.mapi (fun i t -> if i = idx then finish else t)) pools

    let private percentile p (xs: float<minute> list) =
        match List.sort xs with
        | [] -> 0.0<minute>
        | sorted -> sorted[min (sorted.Length - 1) (int (p * float sorted.Length))]

    /// Random draws for one task of one transaction, sampled up front so the
    /// event loop itself is deterministic given the seed.
    type private Draw = {
        Delay: float<minute>
        Service: float<minute>
        Escalates: bool
        EscalationEffort: float<minute>
    }

    type private EventKey = float<minute> * int * int * int // ready time, txn, task index, phase

    let private draws (cfg: SimulationConfig) (tasks: SimTask array) =
        let sampleTask rng (t: SimTask) =
            let delay, rng = sample t.Delay rng
            let service, rng = sample t.Service rng
            let u, rng = next rng

            let escalates, effort, rng =
                match t.Escalation with
                | Some(rate, _, est) ->
                    let e, rng = sample est rng
                    u < rate, e, rng
                | None -> false, 0.0<minute>, rng

            {
                Delay = delay
                Service = service
                Escalates = escalates
                EscalationEffort = effort
            },
            rng

        [ 1 .. cfg.Transactions ]
        |> List.fold
            (fun (rng, clock: float<minute>, acc) _ ->
                let u, rng = next rng
                let arrival = clock + (-cfg.MeanInterArrival * log (1.0 - u))

                let ds, rng =
                    tasks
                    |> Array.fold (fun (xs, r) t -> let d, r = sampleTask r t in (xs @ [ d ], r)) ([], rng)

                (rng, arrival, acc @ [ arrival, Array.ofList ds ]))
            (cfg.Seed, 0.0<minute>, [])
        |> fun (_, _, acc) -> Array.ofList acc

    /// Runs the simulation: an event-ordered, non-preemptive FIFO model in
    /// which work is served in order of readiness. Pure and seeded.
    let run (label: string) (cfg: SimulationConfig) (taskList: SimTask list) : SimulationResult =
        let tasks = Array.ofList taskList
        let index = tasks |> Array.mapi (fun i t -> t.Id, i) |> Map.ofArray
        let depsOf = tasks |> Array.map (fun t -> t.DependsOn |> List.choose index.TryFind)

        let dependents =
            tasks
            |> Array.mapi (fun i _ ->
                depsOf
                |> Array.indexed
                |> Array.filter (fun (_, ds) -> List.contains i ds)
                |> Array.map fst)

        let txns = draws cfg tasks

        let initial: Set<EventKey> =
            txns
            |> Array.indexed
            |> Array.collect (fun (n, (arrival, ds)) ->
                depsOf
                |> Array.indexed
                |> Array.filter (fun (_, deps) -> deps.IsEmpty)
                |> Array.map (fun (i, _) -> (arrival + ds[i].Delay, n, i, 0)))
            |> Set.ofArray

        let addBusy role (m: float<minute>) (b: Map<RoleId, float<minute>>) =
            Map.add role ((b.TryFind role |> Option.defaultValue 0.0<minute>) + m) b

        let rec loop
            (queue: Set<EventKey>)
            pools
            (finishes: Map<int * int, float<minute>>)
            busy
            (human: float<minute>)
            =
            if queue.IsEmpty then
                finishes, busy, human
            else
                let (ready, n, i, phase) as key = queue.MinimumElement
                let queue = queue.Remove key
                let t = tasks[i]
                let d = (snd txns[n])[i]

                match phase with
                | 0 ->
                    let finish, pools, busy, human =
                        match t.Role with
                        | Some role ->
                            let f, p = acquire cfg pools role ready d.Service
                            f, p, addBusy role d.Service busy, human + d.Service
                        | None -> ready + d.Service, pools, busy, human

                    match t.Escalation with
                    | Some _ when d.Escalates -> loop (queue.Add(finish, n, i, 1)) pools finishes busy human
                    | _ -> complete queue pools finishes busy human n i finish
                | _ ->
                    let role =
                        t.Escalation
                        |> Option.map (fun (_, r, _) -> r)
                        |> Option.defaultValue (RoleId "unassigned")

                    let finish, pools = acquire cfg pools role ready d.EscalationEffort

                    complete
                        queue
                        pools
                        finishes
                        (addBusy role d.EscalationEffort busy)
                        (human + d.EscalationEffort)
                        n
                        i
                        finish

        and complete queue pools finishes busy human n i finish =
            let finishes = Map.add (n, i) finish finishes
            let arrival, ds = txns[n]

            let queue =
                dependents[i]
                |> Array.filter (fun j -> depsOf[j] |> List.forall (fun k -> finishes.ContainsKey(n, k)))
                |> Array.fold
                    (fun (q: Set<EventKey>) j ->
                        let ready =
                            depsOf[j] |> List.map (fun k -> finishes[(n, k)]) |> List.fold max arrival

                        q.Add(ready + ds[j].Delay, n, j, 0))
                    queue

            loop queue pools finishes busy human

        let finishes, busy, human = loop initial Map.empty Map.empty Map.empty 0.0<minute>

        let completions =
            txns
            |> Array.mapi (fun n (arrival, _) ->
                let done' =
                    tasks
                    |> Array.mapi (fun i _ -> finishes.TryFind(n, i) |> Option.defaultValue arrival)
                    |> Array.fold max arrival

                arrival, done')
            |> List.ofArray

        let cycles = completions |> List.map (fun (a, c) -> c - a)
        // Utilisation over the arrival window, so long external delays do not dilute it.
        let horizon = completions |> List.map fst |> List.fold max 1.0<minute>

        let utilization =
            busy
            |> Map.toList
            |> List.map (fun (role, b) ->
                let capacity = cfg.Capacity.TryFind role |> Option.defaultValue cfg.DefaultCapacity
                role, float (b / (horizon * float capacity)))
            |> List.sortByDescending snd

        {
            Label = label
            Transactions = cfg.Transactions
            MeanCycle = (if cycles.IsEmpty then 0.0<minute> else List.average cycles)
            P50Cycle = percentile 0.5 cycles
            P90Cycle = percentile 0.9 cycles
            HumanMinutesPerTxn = human / float (max 1 cfg.Transactions)
            Utilization = utilization
            Bottleneck = utilization |> List.tryHead
        }
