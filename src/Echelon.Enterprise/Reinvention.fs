namespace Echelon.Enterprise

/// What the reinvention engine proposes for one current activity. Every
/// disposition other than Retained is a hypothesis to be validated.
type Disposition =
    | Retained of reason: string
    | Eliminated of reason: string
    | MergedInto of ActivityId * reason: string
    /// A handoff, internal queue or status message replaced by event-driven
    /// coordination over a shared system of record.
    | ReplacedByEvent of reason: string

/// A step of the redesigned workflow.
type Step = {
    Activity: Activity
    /// None for steps performed by an external party outside the enterprise's control.
    Assignment: Assignment option
    DependsOn: ActivityId list
    /// Parallel stage: steps in the same stage have no dependency on each other.
    Stage: int
    /// Current activities folded into this step.
    Absorbed: ActivityId list
}

type StructuralMetrics = {
    Steps: int
    /// Points where a human must pick up work produced by a different party.
    HumanHandoffs: int
    /// Steps needing a human on every transaction.
    HumanTouches: int
    /// Sequential stages (critical path length in steps).
    Stages: int
    /// Expected queue delay carried by the structure, in minutes.
    ExpectedDelay: float<minute>
}

type Classification =
    /// The outcome is already satisfiable without this workflow.
    | WorkflowRedundant
    | Reinvention
    | PartialReinvention
    /// Same structure, different executors.
    | SuperficialAutomation
    | NoChange

type Redesign = {
    Workflow: Workflow
    NecessaryData: DataId list
    Dispositions: (Activity * Disposition) list
    Steps: Step list
    Before: StructuralMetrics
    After: StructuralMetrics
    /// Share of current activities structurally changed (eliminated, merged,
    /// replaced or parallelised); distinguishes reinvention from automation.
    StructuralChange: float
    Classification: Classification
}

/// Workflow Reinvention Engine (v0). See methodology/workflow-reinvention.md.
module Reinvention =

    /// Below this structural-change share a redesign is "partial". A heuristic
    /// threshold, recorded as a hypothesis (HY-EEF-003) rather than a finding.
    let partialThreshold = 0.25

    let dispositionLabel =
        function
        | Retained _ -> "retained"
        | Eliminated _ -> "eliminated"
        | MergedInto _ -> "merged"
        | ReplacedByEvent _ -> "replaced-by-event"

    let dispositionReason =
        function
        | Retained r
        | Eliminated r
        | ReplacedByEvent r -> r
        | MergedInto(target, r) -> $"{r} (into {target.Value})"

    let classificationLabel =
        function
        | WorkflowRedundant -> "workflow-redundant"
        | Reinvention -> "reinvention"
        | PartialReinvention -> "partial-reinvention"
        | SuperficialAutomation -> "superficial-automation"
        | NoChange -> "no-change"

    let private intersects (xs: 'a list) (s: Set<'a>) = xs |> List.exists s.Contains

    let private constraintData (cs: Constraint list) =
        cs
        |> List.collect _.Obligations
        |> List.choose (function
            | RetainRecord d
            | RequiresInformation d -> Some d
            | _ -> None)

    /// Backward closure from what the outcome and governing constraints need
    /// to every piece of information required to produce it.
    let necessaryData (m: EnterpriseModel) (w: Workflow) : Set<DataId> =
        let governing = w.Governing |> Set.ofList

        let seed =
            (EnterpriseModel.tryOutcome m w.Outcome
             |> Option.map _.RequiredInformation
             |> Option.defaultValue [])
            @ constraintData (EnterpriseModel.governingConstraints m w)
            |> Set.ofList

        let required (needed: Set<DataId>) (a: Activity) =
            intersects a.Outputs needed || intersects a.Satisfies governing

        let rec fix (needed: Set<DataId>) =
            let next =
                w.Activities
                |> List.filter (required needed)
                |> List.collect _.Inputs
                |> Set.ofList
                |> Set.union needed

            if next = needed then needed else fix next

        fix seed

    let private hasIndependence (m: EnterpriseModel) (a: Activity) =
        Responsibility.obligationsFor m a |> List.contains IndependentCheck

    let private duplicateOf (m: EnterpriseModel) (retained: Activity list) (a: Activity) =
        // A merge must not lose a control: the target must satisfy everything this activity does.
        let coversControls (r: Activity) =
            Set.isSubset (Set.ofList a.Satisfies) (Set.ofList r.Satisfies)

        let sameOutputs (r: Activity) =
            not a.Outputs.IsEmpty
            && Set.ofList r.Outputs = Set.ofList a.Outputs
            && coversControls r

        let sameControl (r: Activity) =
            a.Nature = Verify
            && r.Nature = Verify
            && not a.Satisfies.IsEmpty
            && Set.ofList r.Satisfies = Set.ofList a.Satisfies
            && not (hasIndependence m a)

        retained |> List.tryFind (fun r -> sameOutputs r || sameControl r)

    let private dispose
        (m: EnterpriseModel)
        (w: Workflow)
        (needed: Set<DataId>)
        (retained: Activity list)
        (later: Activity list)
        (a: Activity)
        =
        let governing = w.Governing |> Set.ofList
        let required = intersects a.Outputs needed || intersects a.Satisfies governing
        let carriesControl = intersects a.Satisfies governing

        // Information this activity alone supplies to the outcome or a later step.
        let soleProducer =
            let elsewhere =
                Set.ofList w.Inputs + (retained @ later |> List.collect _.Outputs |> Set.ofList)

            a.Outputs
            |> List.exists (fun o -> needed.Contains o && not (elsewhere.Contains o))

        match a.Nature, a.Performer with
        | (Transfer | Wait | Notify | Rework), _ when carriesControl ->
            Retained "carries a governing control; the control must be redesigned explicitly, not removed"
        | Rework, _ when soleProducer -> Retained "sole producer of required information"
        | Wait, ExternalParty _ -> Retained "external latency is a constraint outside the enterprise's control"
        | Wait, _ -> ReplacedByEvent "internal queue removed: the next step is triggered by an event"
        | Transfer, ExternalParty _ -> Retained "external interface: information must cross the enterprise boundary"
        | Transfer, _ ->
            ReplacedByEvent "handoff removed: shared system of record and an event replace re-keying and forwarding"
        | Notify, _ when required -> Retained "the notification itself is required information"
        | Notify, _ -> ReplacedByEvent "status published as an event; interested parties subscribe"
        | Rework, _ -> Eliminated "rework prevented at source by validation at capture (hypothesis)"
        | (Produce | Decide _ | Verify), _ when not required ->
            Eliminated "produces no information required by the outcome, a decision or a governing constraint"
        | (Produce | Decide _ | Verify), _ ->
            match duplicateOf m retained a with
            | Some r -> MergedInto(r.Id, "duplicates information or a control already established")
            | None -> Retained "required by the outcome, a decision or a governing constraint"

    let private isRetained =
        function
        | Retained _ -> true
        | _ -> false

    let private partyOf =
        function
        | Human r -> $"human:{r.Value}"
        | Software s -> $"software:{s}"
        | ExternalParty p -> $"external:{p}"

    let private stepParty (s: Step) =
        match s.Assignment with
        | Some a ->
            match Responsibility.executorRole a.Executor with
            | Some r when Responsibility.involvesHumanEveryTime a.Executor -> $"human:{r.Value}"
            | _ -> Responsibility.executorLabel a.Executor
        | None -> partyOf s.Activity.Performer

    let private stepNeedsHuman (s: Step) =
        s.Assignment
        |> Option.map (fun a -> Responsibility.involvesHumanEveryTime a.Executor)
        |> Option.defaultValue false

    let private beforeMetrics (w: Workflow) =
        let parties = w.Activities |> List.map (fun a -> a.Performer)

        let handoffs =
            parties
            |> List.pairwise
            |> List.filter (fun (a, b) ->
                partyOf a <> partyOf b
                && (match b with
                    | Human _ -> true
                    | _ -> false))
            |> List.length

        {
            Steps = w.Activities.Length
            HumanHandoffs = handoffs
            HumanTouches =
                parties
                |> List.filter (function
                    | Human _ -> true
                    | _ -> false)
                |> List.length
            Stages = w.Activities.Length
            ExpectedDelay = w.Activities |> List.sumBy (fun a -> a.Delay.Expected)
        }

    let private afterMetrics (steps: Step list) =
        let byId = steps |> List.map (fun s -> s.Activity.Id, s) |> Map.ofList

        let handoffs =
            steps
            |> List.filter stepNeedsHuman
            |> List.sumBy (fun s ->
                s.DependsOn
                |> List.choose byId.TryFind
                |> List.filter (fun d -> stepParty d <> stepParty s)
                |> List.length)

        {
            Steps = steps.Length
            HumanHandoffs = handoffs
            HumanTouches = steps |> List.filter stepNeedsHuman |> List.length
            Stages = steps |> List.map _.Stage |> List.fold max 0
            ExpectedDelay = steps |> List.sumBy (fun s -> s.Activity.Delay.Expected)
        }

    /// Builds redesigned steps from dispositions. Data dependencies, not the
    /// current activity order, determine sequencing; replaced handoffs pass
    /// their inputs' providers through to whoever consumes their outputs.
    let private buildSteps (m: EnterpriseModel) (w: Workflow) (dispositions: (Activity * Disposition) list) =
        let providers (env: Map<DataId, ActivityId list>) (inputs: DataId list) =
            inputs
            |> List.collect (fun d -> env.TryFind d |> Option.defaultValue [])
            |> List.distinct

        let absorbedInto target =
            dispositions
            |> List.choose (fun (a, d) ->
                match d with
                | MergedInto(t, _) when t = target -> Some a.Id
                | _ -> None)

        // A retained wait carries no data, so it is anchored in sequence: it
        // follows the previous retained step and gates the next one.
        let folder
            (env: Map<DataId, ActivityId list>, steps: Step list, pendingWait: ActivityId option)
            (a: Activity, d: Disposition)
            =
            let dataDeps = providers env a.Inputs

            let deps =
                match a.Nature, pendingWait with
                | Wait, _ ->
                    steps
                    |> List.tryLast
                    |> Option.map (fun s -> [ s.Activity.Id ])
                    |> Option.defaultValue []
                | _, Some waitId -> dataDeps @ [ waitId ] |> List.distinct
                | _, None -> dataDeps

            let setOutputs (ids: ActivityId list) e =
                a.Outputs |> List.fold (fun acc o -> Map.add o ids acc) e

            match d with
            | Retained _ ->
                let stageOf id =
                    steps
                    |> List.tryFind (fun s -> s.Activity.Id = id)
                    |> Option.map _.Stage
                    |> Option.defaultValue 0

                let step = {
                    Activity = a
                    Assignment =
                        match a.Performer with
                        | ExternalParty _ -> None
                        | _ -> Some(Responsibility.assign (Responsibility.contextFor m w a))
                    DependsOn = deps
                    Stage = 1 + (deps |> List.map stageOf |> List.fold max 0)
                    Absorbed = absorbedInto a.Id
                }

                let pending = if a.Nature = Wait then Some a.Id else None
                (setOutputs [ a.Id ] env, steps @ [ step ], pending)
            | MergedInto(target, _) -> (setOutputs [ target ] env, steps, pendingWait)
            | Eliminated _
            | ReplacedByEvent _ ->
                // Pass through only information that is not already available;
                // a removed copy or correction must not create new dependencies.
                let env' =
                    a.Outputs
                    |> List.filter (fun o -> not (env.ContainsKey o || w.Inputs |> List.contains o))
                    |> List.fold (fun acc o -> Map.add o dataDeps acc) env

                (env', steps, pendingWait)

        dispositions
        |> List.fold folder (Map.empty, [], None)
        |> fun (_, steps, _) -> steps

    /// The outcome is already met by information available when the workflow
    /// starts, and no governing obligation requires authorisation or a check.
    let isRedundant (m: EnterpriseModel) (w: Workflow) =
        let available = Set.ofList w.Inputs

        let governing = Set.ofList w.Governing

        let requiresAction =
            EnterpriseModel.governingConstraints m w
            |> List.collect _.Obligations
            |> List.exists (function
                | HumanAuthorization _
                | IndependentCheck -> true
                | _ -> false)
            // A workflow that carries a governing control is not redundant: the
            // control would have to be re-homed first.
            || w.Activities |> List.exists (fun a -> intersects a.Satisfies governing)

        match EnterpriseModel.tryOutcome m w.Outcome with
        | Some o -> not requiresAction && o.RequiredInformation |> List.forall available.Contains
        | None -> false

    let private executorChanged (s: Step) =
        match s.Activity.Performer, s.Assignment |> Option.map _.Executor with
        | _, None -> false
        | Software _, Some DeterministicSoftware -> false
        | Human r, Some(AuthorizedHuman r') -> r <> r'
        | _ -> true

    let classify (redundant: bool) (structural: float) (steps: Step list) =
        if redundant then
            WorkflowRedundant
        elif structural = 0.0 then
            if steps |> List.exists executorChanged then
                SuperficialAutomation
            else
                NoChange
        elif structural < partialThreshold then
            PartialReinvention
        else
            Reinvention

    /// Reinvents one workflow. Pure: the same model always yields the same redesign.
    let reinvent (m: EnterpriseModel) (w: Workflow) : Redesign =
        let needed = necessaryData m w
        let redundant = isRedundant m w

        let dispositions =
            if redundant then
                w.Activities
                |> List.map (fun a ->
                    a, Eliminated "the outcome is already satisfied by information available at the trigger")
            else
                w.Activities
                |> List.indexed
                |> List.fold
                    (fun (kept: Activity list, acc) (i, a) ->
                        let later = w.Activities |> List.skip (i + 1)
                        let d = dispose m w needed kept later a
                        ((if isRetained d then kept @ [ a ] else kept), acc @ [ a, d ]))
                    ([], [])
                |> snd

        let steps = buildSteps m w dispositions
        let before = beforeMetrics w
        let after = afterMetrics steps

        let changed = dispositions |> List.filter (snd >> isRetained >> not) |> List.length
        // Steps that no longer wait on a predecessor they used to follow.
        let parallelised = max 0 (steps.Length - after.Stages)

        let structural =
            if w.Activities.IsEmpty then
                0.0
            else
                float (changed + parallelised) / float w.Activities.Length

        {
            Workflow = w
            NecessaryData = needed |> Set.toList
            Dispositions = dispositions
            Steps = steps
            Before = before
            After = after
            StructuralChange = structural
            Classification = classify redundant structural steps
        }
