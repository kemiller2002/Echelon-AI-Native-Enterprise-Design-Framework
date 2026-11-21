namespace Echelon.Enterprise

/// Who or what executes an activity in a redesigned workflow.
type Executor =
    /// Fully specified rules in ordinary software. Preferred wherever it suffices.
    | DeterministicSoftware
    /// A human does the work with AI preparation; the human remains the actor.
    | AiAssistedHuman of RoleId
    /// An agent acts within a fixed tool allowlist and escalates to a human role.
    | SupervisedAgent of escalation: RoleId
    /// An agent screens every transaction and may only raise flags; flagged
    /// transactions go to the named human role, who alone may clear them.
    | AgentScreenedHumanClearance of RoleId
    /// A named accountable human decides; AI may prepare but not decide.
    | AuthorizedHuman of RoleId
    /// A check performed independently of whoever prepared the work.
    | IndependentVerification

/// The result of assigning an executor, with the reasoning that produced it.
type Assignment = {
    Executor: Executor
    /// The rule that decided the assignment.
    DecidingRule: string
    /// Every rule evaluated, in order, with its outcome.
    Trace: string list
}

/// AI Responsibility Model (v0). An ordered rule list; the first rule that
/// applies decides. Rules encode hypotheses documented in
/// methodology/ai-responsibility-model.md and are deliberately conservative:
/// hard governance obligations come first and cannot be outweighed by cost.
module Responsibility =

    type Context = {
        Activity: Activity
        Obligations: Obligation list
        /// The human role accountable for the activity's result.
        Accountable: RoleId
    }

    type private Rule = {
        Name: string
        Applies: Context -> bool
        Assign: Context -> Executor
    }

    let executorLabel =
        function
        | DeterministicSoftware -> "deterministic-software"
        | AiAssistedHuman _ -> "ai-assisted-human"
        | SupervisedAgent _ -> "supervised-agent"
        | AgentScreenedHumanClearance _ -> "agent-screen-human-clearance"
        | AuthorizedHuman _ -> "authorized-human"
        | IndependentVerification -> "independent-verification"

    let executorRole =
        function
        | AiAssistedHuman r
        | SupervisedAgent r
        | AgentScreenedHumanClearance r
        | AuthorizedHuman r -> Some r
        | DeterministicSoftware
        | IndependentVerification -> None

    /// True when a human must act on every transaction.
    let involvesHumanEveryTime =
        function
        | AiAssistedHuman _
        | AuthorizedHuman _ -> true
        | DeterministicSoftware
        | SupervisedAgent _
        | AgentScreenedHumanClearance _
        | IndependentVerification -> false

    let private humanAuthority ctx =
        ctx.Obligations
        |> List.tryPick (function
            | HumanAuthorization r -> Some r
            | _ -> None)

    let private has obligation ctx =
        ctx.Obligations |> List.contains obligation

    let private p ctx = ctx.Activity.Profile

    let private rules: Rule list = [
        {
            Name = "R1 human-authority: a governing obligation names an accountable human role"
            Applies = humanAuthority >> Option.isSome
            Assign = fun ctx -> AuthorizedHuman(humanAuthority ctx |> Option.get)
        }
        {
            Name = "R2 independence: the obligation requires a check independent of the preparer"
            Applies = fun ctx -> has IndependentCheck ctx && ctx.Activity.Nature = Verify
            Assign = fun _ -> IndependentVerification
        }
        {
            Name = "R3 deterministic: rule-specifiable over structured inputs"
            Applies = fun ctx -> (p ctx).RuleSpecifiable && (p ctx).InputStructure = Structured
            Assign = fun _ -> DeterministicSoftware
        }
        {
            Name =
                "R4a no-automated-clearance (bounded): agent may raise flags on reversible, sub-major work; a human clears them"
            Applies =
                fun ctx ->
                    (p ctx).ClearsRisk
                    && has NoAiClearance ctx
                    && (p ctx).Reversibility <> Irreversible
                    && not (Level.atLeast Major (p ctx).FinancialExposure)
            Assign = fun ctx -> AgentScreenedHumanClearance ctx.Accountable
        }
        {
            Name = "R4b no-automated-clearance (unbounded): clearing a concern stays with a human on every case"
            Applies = fun ctx -> (p ctx).ClearsRisk && has NoAiClearance ctx
            Assign = fun ctx -> AiAssistedHuman ctx.Accountable
        }
        {
            Name = "R5 consequence: irreversible or major financial exposure keeps a human actor"
            Applies =
                fun ctx ->
                    (p ctx).Reversibility = Irreversible
                    || Level.atLeast Major (p ctx).FinancialExposure
            Assign = fun ctx -> AiAssistedHuman ctx.Accountable
        }
        {
            Name = "R6 accountable-judgment: moderate+ judgment, explainability required and moderate+ exposure"
            Applies =
                fun ctx ->
                    Level.atLeast Moderate (p ctx).Judgment
                    && (p ctx).ExplainabilityRequired
                    && Level.atLeast Moderate (p ctx).FinancialExposure
            Assign = fun ctx -> AiAssistedHuman ctx.Accountable
        }
        {
            Name = "R7 supervised-agent: remaining judgment over reversible, bounded-exposure work"
            Applies = fun _ -> true
            Assign = fun ctx -> SupervisedAgent ctx.Accountable
        }
    ]

    /// Assigns an executor. Pure and total: R7 always applies.
    let assign (ctx: Context) : Assignment =
        let evaluated = rules |> List.map (fun r -> r, r.Applies ctx)
        let decidingRule, _ = evaluated |> List.find snd

        let trace =
            evaluated
            |> List.takeWhile (fun (r, _) -> r.Name <> decidingRule.Name)
            |> List.map (fun (r, _) -> $"not applicable: {r.Name}")
            |> fun skipped -> skipped @ [ $"applied: {decidingRule.Name}" ]

        {
            Executor = decidingRule.Assign ctx
            DecidingRule = decidingRule.Name
            Trace = trace
        }

    /// Obligations that apply to an activity: those of the constraints it satisfies.
    let obligationsFor (m: EnterpriseModel) (a: Activity) =
        a.Satisfies
        |> List.choose (EnterpriseModel.tryConstraint m)
        |> List.collect _.Obligations

    let accountableFor (w: Workflow) (a: Activity) =
        match a.Performer with
        | Human r -> r
        | Software _
        | ExternalParty _ -> w.Owner

    let contextFor (m: EnterpriseModel) (w: Workflow) (a: Activity) = {
        Activity = a
        Obligations = obligationsFor m a
        Accountable = accountableFor w a
    }
