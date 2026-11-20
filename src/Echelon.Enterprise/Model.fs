namespace Echelon.Enterprise

/// A party whose needs the enterprise exists to serve or must answer to.
type Stakeholder = {
    Id: StakeholderId
    Name: string
    Needs: string list
}

/// A required business outcome. Workflows are judged against outcomes, never
/// against the activities they happen to perform today.
type Outcome = {
    Id: OutcomeId
    Statement: string
    For: StakeholderId list
    /// The minimum information that must exist for the outcome to be achieved.
    RequiredInformation: DataId list
    Measures: string list
    Evidence: EvidenceId list
}

type ConstraintKind =
    | Regulatory
    | Contractual
    | Fiduciary
    | Policy
    | Technical
    | Financial

/// What a constraint obliges the enterprise to do. Obligations are what the
/// reinvention engine may never design away.
type Obligation =
    /// A named accountable human role must authorise. Cannot be delegated to software or agents.
    | HumanAuthorization of RoleId
    /// Must be checked by a party independent of whoever prepared the work.
    | IndependentCheck
    /// A record of this information must be retained as audit evidence.
    | RetainRecord of DataId
    /// The obligation cannot be met without this information.
    | RequiresInformation of DataId
    /// AI executors (agents, models) may raise a concern but never clear one.
    /// Deterministic rules approved under policy, and humans, may clear.
    | NoAiClearance

type Constraint = {
    Id: ConstraintId
    Kind: ConstraintKind
    Statement: string
    /// Statute, regulation, contract clause or policy that imposes it.
    Authority: string
    Obligations: Obligation list
    Evidence: EvidenceId list
}

type DataClassification =
    | PublicData
    | InternalData
    | ConfidentialData
    | RestrictedData

type DataItem = {
    Id: DataId
    Name: string
    Classification: DataClassification
}

/// A stable business capability: what the enterprise must be able to do,
/// independent of who or what does it. Capabilities own data.
type Capability = {
    Id: CapabilityId
    Name: string
    Domain: string
    Purpose: string
    Supports: OutcomeId list
    Owns: DataId list
}

type RoleKind =
    | InternalRole
    | ExternalRole
    | CustomerRole

type Role = {
    Id: RoleId
    Name: string
    Kind: RoleKind
    LoadedCostPerHour: Estimate<usd / hour>
}

type Decision = {
    Id: DecisionId
    Question: string
    /// Minimum information required to decide.
    Inputs: DataId list
    Output: DataId
    Evidence: EvidenceId list
}

/// Who performs an activity today.
type Performer =
    | Human of RoleId
    | Software of system: string
    | ExternalParty of party: string

/// The nature of an activity. Transfer, Wait, Notify and Rework are the
/// typical carriers of structural waste; the engine treats them differently.
type ActivityNature =
    /// Creates or transforms information.
    | Produce
    | Decide of DecisionId
    /// A control check.
    | Verify
    /// Moves information between parties or systems (handoff, re-keying, email).
    | Transfer
    /// Queue or batch delay with no work done.
    | Wait
    /// Tells a party about status.
    | Notify
    /// Corrects earlier work.
    | Rework

type InputStructure =
    | Structured
    | SemiStructured
    | Unstructured

type Reversibility =
    | Reversible
    | CostlyToReverse
    | Irreversible

/// The facts the AI responsibility model scores an activity on.
type TaskProfile = {
    /// Can the activity be fully specified as deterministic rules?
    RuleSpecifiable: bool
    InputStructure: InputStructure
    Judgment: Level
    Reversibility: Reversibility
    FinancialExposure: Level
    ExplainabilityRequired: bool
    /// Does the activity clear (resolve in favour of proceeding) a risk or concern?
    ClearsRisk: bool
}

type Activity = {
    Id: ActivityId
    Name: string
    Nature: ActivityNature
    Performer: Performer
    Inputs: DataId list
    Outputs: DataId list
    Satisfies: ConstraintId list
    /// Hands-on effort per transaction.
    Effort: Estimate<minute>
    /// Elapsed waiting before the activity starts.
    Delay: Estimate<minute>
    /// Share of transactions needing exception handling at this activity.
    ExceptionRate: Estimate<1>
    Profile: TaskProfile
    Evidence: EvidenceId list
}

/// A workflow as it operates today. Activities are listed in their current order.
type Workflow = {
    Id: WorkflowId
    Name: string
    Outcome: OutcomeId
    Trigger: string
    /// Information available when the workflow is triggered.
    Inputs: DataId list
    Governing: ConstraintId list
    /// Accountable owner; the escalation target where no other role is implied.
    Owner: RoleId
    Activities: Activity list
    Completion: string list
    Evidence: EvidenceId list
}

/// Volume and cost assumptions for comparing current and redesigned operation.
type EconomicAssumptions = {
    AnnualVolume: Estimate<txn>
    /// Fraction of human effort that remains when a human is AI-assisted.
    AssistedEffortFactor: Estimate<1>
    /// Share of agent-executed transactions escalated to a human.
    AgentEscalationRate: Estimate<1>
    /// Human minutes spent per escalation.
    EscalationEffort: Estimate<minute>
    InferenceCostPerAgentStep: Estimate<usd>
    InferenceCostPerAssistedStep: Estimate<usd>
    DeterministicCostPerStep: Estimate<usd>
    CurrentFixedAnnual: Estimate<usd>
    RedesignFixedAnnual: Estimate<usd>
    RedesignOneTimeCost: Estimate<usd>
    AmortizationYears: float
    /// Probability per transaction of an undetected material error.
    CurrentErrorRate: Estimate<1>
    RedesignErrorRate: Estimate<1>
    LossPerError: Estimate<usd>
}

/// A complete, industry-independent enterprise model for one scope.
type EnterpriseModel = {
    Name: string
    Industry: string
    Scope: string
    Stakeholders: Stakeholder list
    Outcomes: Outcome list
    Constraints: Constraint list
    Capabilities: Capability list
    Data: DataItem list
    Roles: Role list
    Decisions: Decision list
    Workflows: Workflow list
    Evidence: Evidence list
    Economics: EconomicAssumptions
}

[<RequireQualifiedAccess>]
module EnterpriseModel =
    let tryEvidence (m: EnterpriseModel) id =
        m.Evidence |> List.tryFind (fun e -> e.Id = id)

    let tryConstraint (m: EnterpriseModel) id =
        m.Constraints |> List.tryFind (fun c -> c.Id = id)

    let tryOutcome (m: EnterpriseModel) id =
        m.Outcomes |> List.tryFind (fun o -> o.Id = id)

    let tryRole (m: EnterpriseModel) id =
        m.Roles |> List.tryFind (fun r -> r.Id = id)

    let tryData (m: EnterpriseModel) id =
        m.Data |> List.tryFind (fun d -> d.Id = id)

    let dataName (m: EnterpriseModel) (id: DataId) =
        tryData m id |> Option.map (fun d -> d.Name) |> Option.defaultValue id.Value

    let owningCapability (m: EnterpriseModel) (id: DataId) =
        m.Capabilities |> List.tryFind (fun c -> c.Owns |> List.contains id)

    let governingConstraints (m: EnterpriseModel) (w: Workflow) =
        w.Governing |> List.choose (tryConstraint m)
