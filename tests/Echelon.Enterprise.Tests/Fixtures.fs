/// Minimal, industry-neutral building blocks for tests.
module Echelon.Enterprise.Tests.Fixtures

open Echelon.Enterprise

let ev = EvidenceId "EV-T-001"

let evidence = {
    Id = ev
    Statement = "Test assumption."
    Status = Assumption
    Confidence = Low
    Sources = [
        {
            Citation = "test"
            Locator = None
            Kind = Illustrative
            Retrieved = None
        }
    ]
    Contradicts = []
}

let est lo ex hi : Estimate<'u> = Estimate.create lo ex hi [ ev ]

let mins (m: float) : Estimate<minute> =
    Estimate.exactly (m * 1.0<minute>) [ ev ]

let noRate: Estimate<1> = Estimate.exactly 0.0 [ ev ]

let clerk = RoleId "R-CLERK"
let reviewer = RoleId "R-REVIEWER"
let approver = RoleId "R-APPROVER"

let routine = {
    RuleSpecifiable = true
    InputStructure = Structured
    Judgment = Negligible
    Reversibility = Reversible
    FinancialExposure = Minor
    ExplainabilityRequired = false
    ClearsRisk = false
}

let judgment = {
    RuleSpecifiable = false
    InputStructure = Unstructured
    Judgment = Moderate
    Reversibility = Reversible
    FinancialExposure = Minor
    ExplainabilityRequired = false
    ClearsRisk = false
}

let act id nature performer inputs outputs satisfies = {
    Id = ActivityId id
    Name = id
    Nature = nature
    Performer = performer
    Inputs = inputs |> List.map DataId
    Outputs = outputs |> List.map DataId
    Satisfies = satisfies |> List.map ConstraintId
    Effort = mins 10.0
    Delay = mins 60.0
    ExceptionRate = noRate
    Profile = routine
    Evidence = [ ev ]
}

let constraintWith id obligations = {
    Id = ConstraintId id
    Kind = Policy
    Statement = id
    Authority = "test policy"
    Obligations = obligations
    Evidence = [ ev ]
}

let economics = {
    AnnualVolume = est 1000.0<txn> 10000.0<txn> 20000.0<txn>
    AssistedEffortFactor = est 0.3 0.5 0.7
    AgentEscalationRate = est 0.05 0.1 0.2
    EscalationEffort = est 2.0<minute> 5.0<minute> 10.0<minute>
    InferenceCostPerAgentStep = est 0.01<usd> 0.05<usd> 0.2<usd>
    InferenceCostPerAssistedStep = est 0.01<usd> 0.05<usd> 0.2<usd>
    DeterministicCostPerStep = est 0.001<usd> 0.01<usd> 0.02<usd>
    CurrentFixedAnnual = est 10000.0<usd> 20000.0<usd> 30000.0<usd>
    RedesignFixedAnnual = est 20000.0<usd> 30000.0<usd> 40000.0<usd>
    RedesignOneTimeCost = est 30000.0<usd> 60000.0<usd> 90000.0<usd>
    AmortizationYears = 3.0
    CurrentErrorRate = est 0.0 0.001 0.002
    RedesignErrorRate = est 0.0 0.001 0.002
    LossPerError = est 100.0<usd> 500.0<usd> 1000.0<usd>
}

let roles =
    [ clerk; reviewer; approver ]
    |> List.map (fun r -> {
        Id = r
        Name = r.Value
        Kind = InternalRole
        LoadedCostPerHour = est 50.0<usd / hour> 60.0<usd / hour> 70.0<usd / hour>
    })

/// A model around one workflow. Data items are inferred from the activities.
let modelOf (constraints: Constraint list) (required: string list) (inputs: string list) (activities: Activity list) =
    let dataIds =
        (required @ inputs |> List.map DataId)
        @ (activities |> List.collect (fun a -> a.Inputs @ a.Outputs))
        |> List.distinct

    {
        Name = "test"
        Industry = "test"
        Scope = "test"
        Stakeholders = [
            {
                Id = StakeholderId "SH-T"
                Name = "customer"
                Needs = []
            }
        ]
        Outcomes = [
            {
                Id = OutcomeId "OUT-T"
                Statement = "outcome"
                For = [ StakeholderId "SH-T" ]
                RequiredInformation = required |> List.map DataId
                Measures = []
                Evidence = [ ev ]
            }
        ]
        Constraints = constraints
        Capabilities = [
            {
                Id = CapabilityId "CAP-T"
                Name = "Core"
                Domain = "test"
                Purpose = "test"
                Supports = [ OutcomeId "OUT-T" ]
                Owns = dataIds
            }
        ]
        Data =
            dataIds
            |> List.map (fun d -> {
                Id = d
                Name = d.Value
                Classification = InternalData
            })
        Roles = roles
        Decisions = []
        Workflows = [
            {
                Id = WorkflowId "WF-T"
                Name = "test workflow"
                Outcome = OutcomeId "OUT-T"
                Trigger = "request"
                Inputs = inputs |> List.map DataId
                Governing = constraints |> List.map _.Id
                Owner = clerk
                Activities = activities
                Completion = []
                Evidence = [ ev ]
            }
        ]
        Evidence = [ evidence ]
        Economics = economics
    }

let workflow (m: EnterpriseModel) = List.head m.Workflows

let redesign (m: EnterpriseModel) = Reinvention.reinvent m (workflow m)

let dispositionOf (r: Redesign) id =
    r.Dispositions
    |> List.find (fun (a, _) -> a.Id = ActivityId id)
    |> snd
    |> Reinvention.dispositionLabel

let stepOf (r: Redesign) id =
    r.Steps |> List.find (fun s -> s.Activity.Id = ActivityId id)

let executorOf (r: Redesign) id =
    (stepOf r id).Assignment |> Option.map _.Executor

let codes (m: EnterpriseModel) =
    Validation.validate m |> List.map _.Code |> Set.ofList
