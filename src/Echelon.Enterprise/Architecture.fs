namespace Echelon.Enterprise

type ApiOperation = {
    Id: string
    Name: string
    Capability: CapabilityId option
    Inputs: DataId list
    Outputs: DataId list
    Permission: string
    Step: ActivityId
    /// Set when only this human role may approve through the operation.
    RequiresHumanAuthority: RoleId option
    /// Set when automated or AI callers may raise but not clear through it.
    ForbidsAiClearance: bool
}

type EventContract = {
    Name: string
    Data: DataId
    Producer: ActivityId
    Consumers: ActivityId list
    Capability: CapabilityId option
}

type AgentSpec = {
    Id: AgentId
    Name: string
    Capability: CapabilityId option
    Steps: ActivityId list
    /// Closed allowlist of operations. Agents cannot add to it.
    Tools: string list
    Reads: DataId list
    EscalatesTo: RoleId list
    Boundaries: string list
    EvaluationCriteria: string list
    EvidenceRequirements: string list
}

type Principal =
    | RolePrincipal of RoleId
    | AgentPrincipal of AgentId
    | ServicePrincipal of string

type Right =
    | Execute
    | Approve

type Grant = {
    Principal: Principal
    Api: string
    Right: Right
}

type DecisionRight = {
    Step: ActivityId
    Decision: DecisionId option
    Accountable: RoleId
    Executor: string
    Constraints: ConstraintId list
}

type ControlCoverage = {
    Constraint: ConstraintId
    Obligation: string
    CoveredBy: ActivityId list
    AuditEvidence: DataId list
}

type IntegrationPoint = {
    Step: ActivityId
    Party: string
    Exchanges: DataId list
}

type MonitoringRequirement = {
    Subject: string
    Signal: string
    Threshold: string
}

type RoadmapPhase = {
    Phase: int
    Name: string
    Steps: ActivityId list
    Gate: string
}

type TraceLink = {
    Element: string
    Step: ActivityId
    Activities: ActivityId list
    Constraints: ConstraintId list
    Outcome: OutcomeId
    Evidence: EvidenceId list
}

type ArchitectureSpec = {
    Model: string
    Workflow: WorkflowId
    Capabilities: Capability list
    Apis: ApiOperation list
    Events: EventContract list
    Agents: AgentSpec list
    Grants: Grant list
    DecisionRights: DecisionRight list
    Controls: ControlCoverage list
    Integrations: IntegrationPoint list
    Monitoring: MonitoringRequirement list
    Roadmap: RoadmapPhase list
    Trace: TraceLink list
    /// Governance invariants the generated architecture breaks. Must be empty to proceed.
    Violations: string list
}

/// Enterprise Architecture Generator (v0). Every element it emits is derived
/// from a redesigned step and traces back to activities, constraints, the
/// outcome and evidence. See architecture/overview.md.
module Architecture =

    let private verb =
        function
        | Produce -> "Record"
        | Decide _ -> "Decide"
        | Verify -> "Verify"
        | Transfer -> "Send"
        | Wait -> "Await"
        | Notify -> "Notify"
        | Rework -> "Correct"

    let private slug (s: string) =
        s.ToLowerInvariant()
        |> String.map (fun c -> if System.Char.IsLetterOrDigit c then c else '-')

    let private pascal (s: string) =
        s.Split([| ' '; '-'; '_' |], System.StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun w -> string (System.Char.ToUpperInvariant w[0]) + w.Substring 1)
        |> String.concat ""

    let private internalSteps (r: Redesign) =
        r.Steps |> List.filter (fun s -> s.Assignment.IsSome)

    let private executorOf (s: Step) = s.Assignment |> Option.map _.Executor

    let private apiFor (m: EnterpriseModel) (s: Step) : ApiOperation =
        let a = s.Activity
        let capability = a.Outputs |> List.tryPick (EnterpriseModel.owningCapability m)
        let obligations = Responsibility.obligationsFor m a

        let outputs =
            a.Outputs |> List.map (EnterpriseModel.dataName m) |> String.concat " and "

        let id = $"API-{a.Id.Value}"

        {
            Id = id
            Name = $"{verb a.Nature} {outputs}".Trim()
            Capability = capability |> Option.map _.Id
            Inputs = a.Inputs
            Outputs = a.Outputs
            Permission =
                (capability |> Option.map (fun c -> slug c.Name) |> Option.defaultValue "unowned")
                + "."
                + slug id
            Step = a.Id
            RequiresHumanAuthority =
                obligations
                |> List.tryPick (function
                    | HumanAuthorization r -> Some r
                    | _ -> None)
            ForbidsAiClearance = a.Profile.ClearsRisk && obligations |> List.contains NoAiClearance
        }

    /// For screened steps, a separate flag-raising operation for the agent.
    /// The clearance operation stays with the human.
    let private screenApiFor (api: ApiOperation) : ApiOperation = {
        api with
            Id = api.Id + "-SCREEN"
            Name = $"Raise flags for {api.Name}"
            Outputs = []
            Permission = api.Permission + "-screen"
            RequiresHumanAuthority = None
            ForbidsAiClearance = false
    }

    let private isScreened (s: Step) =
        match executorOf s with
        | Some(AgentScreenedHumanClearance _) -> true
        | _ -> false

    let private allApis (m: EnterpriseModel) (r: Redesign) =
        internalSteps r
        |> List.collect (fun s ->
            let api = apiFor m s
            if isScreened s then [ api; screenApiFor api ] else [ api ])

    let private events (m: EnterpriseModel) (r: Redesign) =
        r.Steps
        |> List.collect (fun s ->
            s.Activity.Outputs
            |> List.map (fun d -> {
                Name = pascal (EnterpriseModel.dataName m d) + "Recorded"
                Data = d
                Producer = s.Activity.Id
                Consumers =
                    r.Steps
                    |> List.filter (fun c ->
                        c.DependsOn |> List.contains s.Activity.Id
                        && c.Activity.Inputs |> List.contains d)
                    |> List.map _.Activity.Id
                Capability = EnterpriseModel.owningCapability m d |> Option.map _.Id
            }))

    let private agents (m: EnterpriseModel) (apis: ApiOperation list) (r: Redesign) =
        internalSteps r
        |> List.choose (fun s ->
            match executorOf s with
            | Some(SupervisedAgent role)
            | Some(AgentScreenedHumanClearance role) -> Some(s, role)
            | _ -> None)
        |> List.groupBy (fun (s, _) -> s.Activity.Outputs |> List.tryPick (EnterpriseModel.owningCapability m))
        |> List.map (fun (capability, steps) ->
            let name =
                capability |> Option.map (fun c -> c.Name) |> Option.defaultValue "Unowned"

            let stepIds = steps |> List.map (fun (s, _) -> s.Activity.Id)

            {
                Id = AgentId $"AGT-{slug name}"
                Name = $"{name} agent"
                Capability = capability |> Option.map _.Id
                Steps = stepIds
                Tools =
                    steps
                    |> List.collect (fun (s, _) ->
                        let own = apis |> List.filter (fun a -> a.Step = s.Activity.Id)

                        if isScreened s then
                            own |> List.filter (fun a -> a.Id.EndsWith "-SCREEN")
                        else
                            own)
                    |> List.map _.Id
                Reads = steps |> List.collect (fun (s, _) -> s.Activity.Inputs) |> List.distinct
                EscalatesTo = steps |> List.map snd |> List.distinct
                Boundaries = [
                    "May invoke only the operations listed as tools; the allowlist is fixed at deployment."
                    "May not grant, modify or request permissions for itself or any principal."
                    "May raise a concern; may not clear a concern raised by a control or a human."
                    "May not approve or authorise any operation reserved to a human role."
                    "Escalates when confidence is below threshold, inputs conflict, or a case matches no known pattern."
                ]
                EvaluationCriteria =
                    stepIds
                    |> List.map (fun id ->
                        $"{id.Value}: accuracy against an independently labelled sample; escalation rate within the assumed range")
                EvidenceRequirements = [
                    "Cite the input records each output relies on."
                    "Record model version, prompt version and policy version for every action."
                ]
            })

    let private grants (apis: ApiOperation list) (agentSpecs: AgentSpec list) (r: Redesign) =
        let apiOf (s: Step) =
            apis
            |> List.find (fun a -> a.Step = s.Activity.Id && not (a.Id.EndsWith "-SCREEN"))

        let screenOf (s: Step) =
            apis |> List.find (fun a -> a.Step = s.Activity.Id && a.Id.EndsWith "-SCREEN")

        let service (api: ApiOperation) =
            ServicePrincipal(
                api.Capability
                |> Option.map (fun c -> $"svc-{slug c.Value}")
                |> Option.defaultValue "svc-unowned"
            )

        internalSteps r
        |> List.collect (fun s ->
            let api = apiOf s

            match executorOf s with
            | Some(AuthorizedHuman role) -> [
                {
                    Principal = RolePrincipal role
                    Api = api.Id
                    Right = Approve
                }
              ]
            | Some(AiAssistedHuman role) -> [
                {
                    Principal = RolePrincipal role
                    Api = api.Id
                    Right = Execute
                }
              ]
            | Some(SupervisedAgent role) ->
                let agent =
                    agentSpecs |> List.find (fun a -> a.Steps |> List.contains s.Activity.Id)

                [
                    {
                        Principal = AgentPrincipal agent.Id
                        Api = api.Id
                        Right = Execute
                    }
                    {
                        Principal = RolePrincipal role
                        Api = api.Id
                        Right = Execute
                    }
                ]
            | Some(AgentScreenedHumanClearance role) ->
                let agent =
                    agentSpecs |> List.find (fun a -> a.Steps |> List.contains s.Activity.Id)

                [
                    {
                        Principal = AgentPrincipal agent.Id
                        Api = (screenOf s).Id
                        Right = Execute
                    }
                    {
                        Principal = RolePrincipal role
                        Api = api.Id
                        Right = Approve
                    }
                ]
            | Some IndependentVerification -> [
                {
                    Principal = ServicePrincipal "svc-independent-verification"
                    Api = api.Id
                    Right = Execute
                }
              ]
            | Some DeterministicSoftware -> [
                {
                    Principal = service api
                    Api = api.Id
                    Right = Execute
                }
              ]
            | None -> [])

    let private obligationLabel =
        function
        | HumanAuthorization r -> $"human authorisation by {r.Value}"
        | IndependentCheck -> "independent check"
        | RetainRecord d -> $"retain record of {d.Value}"
        | RequiresInformation d -> $"consider {d.Value}"
        | NoAiClearance -> "no automated clearance"

    let private controls (m: EnterpriseModel) (r: Redesign) =
        EnterpriseModel.governingConstraints m r.Workflow
        |> List.collect (fun c ->
            let covering =
                r.Steps |> List.filter (fun s -> s.Activity.Satisfies |> List.contains c.Id)

            let produced = r.Steps |> List.collect _.Activity.Outputs |> Set.ofList

            c.Obligations
            |> List.map (fun ob -> {
                Constraint = c.Id
                Obligation = obligationLabel ob
                CoveredBy =
                    match ob with
                    | RetainRecord d
                    | RequiresInformation d ->
                        r.Steps
                        |> List.filter (fun s ->
                            s.Activity.Outputs |> List.contains d || s.Activity.Inputs |> List.contains d)
                        |> List.map _.Activity.Id
                    | _ -> covering |> List.map _.Activity.Id
                AuditEvidence =
                    (covering |> List.collect _.Activity.Outputs)
                    @ (match ob with
                       | RetainRecord d when produced.Contains d || r.Workflow.Inputs |> List.contains d -> [ d ]
                       | _ -> [])
                    |> List.distinct
            }))

    let private violations (apis: ApiOperation list) (agentSpecs: AgentSpec list) (cs: ControlCoverage list) =
        let byId = apis |> List.map (fun a -> a.Id, a) |> Map.ofList

        [
            for agent in agentSpecs do
                for tool in agent.Tools do
                    match byId.TryFind tool with
                    | Some api when api.RequiresHumanAuthority.IsSome ->
                        yield $"{agent.Id.Value} holds {tool}, which is reserved to human authority."
                    | Some api when api.ForbidsAiClearance ->
                        yield $"{agent.Id.Value} holds {tool}, which clears concerns that automation may not clear."
                    | Some _ -> ()
                    | None -> yield $"{agent.Id.Value} holds unknown tool {tool}."
            for c in cs do
                if c.CoveredBy.IsEmpty then
                    yield $"Control gap: {c.Constraint.Value} ({c.Obligation}) is covered by no redesigned step."
            for api in apis do
                if api.Capability.IsNone && not api.Outputs.IsEmpty then
                    yield $"{api.Id} produces data owned by no capability (no system of record)."
        ]

    let private monitoring (m: EnterpriseModel) (agentSpecs: AgentSpec list) (cs: ControlCoverage list) (r: Redesign) =
        let e = m.Economics

        [
            for a in agentSpecs do
                yield {
                    Subject = a.Id.Value
                    Signal = "escalation rate"
                    Threshold =
                        $"alert above {e.AgentEscalationRate.High:P0} (assumed high); review below {e.AgentEscalationRate.Low:P0}"
                }

                yield {
                    Subject = a.Id.Value
                    Signal = "sampled accuracy against independent review"
                    Threshold = "suspend agent on any material error pending review"
                }
            for s in r.Steps do
                if
                    s.Assignment
                    |> Option.exists (fun x -> Responsibility.involvesHumanEveryTime x.Executor)
                then
                    yield {
                        Subject = s.Activity.Id.Value
                        Signal = "queue time to human action"
                        Threshold =
                            if s.Activity.Delay.High > 0.0<minute> then
                                $"alert above {s.Activity.Delay.High} minutes (assumed high)"
                            else
                                "no queue assumed; establish a measured baseline before setting an alert"
                    }
            for c in cs do
                yield {
                    Subject = c.Constraint.Value
                    Signal = $"audit evidence completeness ({c.Obligation})"
                    Threshold = "100% of transactions; any gap is an incident"
                }
        ]

    let private roadmap (r: Redesign) =
        let stepsWhere f =
            internalSteps r
            |> List.filter (fun s -> executorOf s |> Option.exists f)
            |> List.map _.Activity.Id

        [
            {
                Phase = 1
                Name = "System of record, events and deterministic rules"
                Steps =
                    stepsWhere (function
                        | DeterministicSoftware
                        | IndependentVerification -> true
                        | _ -> false)
                Gate =
                    "Replaced handoffs verified in parallel run; deterministic rules pass property and regression tests."
            }
            {
                Phase = 2
                Name = "AI-assisted human work and authorisation support"
                Steps =
                    stepsWhere (function
                        | AiAssistedHuman _
                        | AuthorizedHuman _ -> true
                        | _ -> false)
                Gate = "Assisted effort measured against baseline; no loss of decision quality on sampled cases."
            }
            {
                Phase = 3
                Name = "Supervised and screening agents"
                Steps =
                    stepsWhere (function
                        | SupervisedAgent _
                        | AgentScreenedHumanClearance _ -> true
                        | _ -> false)
                Gate =
                    "Agent evaluation passes on held-out labelled cases; escalation and error rates within assumptions; human approval."
            }
        ]
        |> List.filter (fun p -> not p.Steps.IsEmpty)

    let private trace (m: EnterpriseModel) (apis: ApiOperation list) (r: Redesign) =
        apis
        |> List.map (fun api ->
            let step = r.Steps |> List.find (fun s -> s.Activity.Id = api.Step)


            let constraintEvidence =
                step.Activity.Satisfies
                |> List.choose (EnterpriseModel.tryConstraint m)
                |> List.collect _.Evidence

            {
                Element = api.Id
                Step = api.Step
                Activities = step.Activity.Id :: step.Absorbed
                Constraints = step.Activity.Satisfies
                Outcome = r.Workflow.Outcome
                Evidence = step.Activity.Evidence @ constraintEvidence |> List.distinct
            })

    /// Generates an architecture specification for one redesigned workflow.
    let generate (m: EnterpriseModel) (r: Redesign) : ArchitectureSpec =
        let apis = allApis m r
        let agentSpecs = agents m apis r
        let cs = controls m r

        let usedCapabilities =
            apis
            |> List.choose _.Capability
            |> List.distinct
            |> List.choose (fun id -> m.Capabilities |> List.tryFind (fun c -> c.Id = id))

        {
            Model = m.Name
            Workflow = r.Workflow.Id
            Capabilities = usedCapabilities
            Apis = apis
            Events = events m r
            Agents = agentSpecs
            Grants = grants apis agentSpecs r
            DecisionRights =
                internalSteps r
                |> List.choose (fun s ->
                    match executorOf s, s.Activity.Nature with
                    | Some ex, Decide d ->
                        Some {
                            Step = s.Activity.Id
                            Decision = Some d
                            Accountable =
                                Responsibility.executorRole ex
                                |> Option.defaultValue (Responsibility.accountableFor r.Workflow s.Activity)
                            Executor = Responsibility.executorLabel ex
                            Constraints = s.Activity.Satisfies
                        }
                    | _ -> None)
            Controls = cs
            Integrations =
                r.Steps
                |> List.filter (fun s -> s.Assignment.IsNone)
                |> List.map (fun s -> {
                    Step = s.Activity.Id
                    Party =
                        match s.Activity.Performer with
                        | ExternalParty p -> p
                        | _ -> "unknown"
                    Exchanges = s.Activity.Inputs @ s.Activity.Outputs |> List.distinct
                })
            Monitoring = monitoring m agentSpecs cs r
            Roadmap = roadmap r
            Trace = trace m apis r
            Violations = violations apis agentSpecs cs
        }
