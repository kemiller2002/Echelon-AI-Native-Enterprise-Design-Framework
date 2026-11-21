namespace Echelon.Enterprise

/// Units of measure. Costs and durations are checked by the compiler, so a
/// cost per hour cannot be added to a duration in minutes by mistake.
[<Measure>]
type usd

[<Measure>]
type minute

[<Measure>]
type hour

[<Measure>]
type txn

[<RequireQualifiedAccess>]
module Units =
    let minutesPerHour = 60.0<minute / hour>
    let toHours (m: float<minute>) : float<hour> = m / minutesPerHour

// Strongly typed identifiers. A capability id cannot be passed where an
// outcome id is expected; the compiler enforces the reference kind.

type OutcomeId =
    | OutcomeId of string

    member this.Value = let (OutcomeId v) = this in v

type StakeholderId =
    | StakeholderId of string

    member this.Value = let (StakeholderId v) = this in v

type CapabilityId =
    | CapabilityId of string

    member this.Value = let (CapabilityId v) = this in v

type ConstraintId =
    | ConstraintId of string

    member this.Value = let (ConstraintId v) = this in v

type EvidenceId =
    | EvidenceId of string

    member this.Value = let (EvidenceId v) = this in v

type ActivityId =
    | ActivityId of string

    member this.Value = let (ActivityId v) = this in v

type DecisionId =
    | DecisionId of string

    member this.Value = let (DecisionId v) = this in v

type DataId =
    | DataId of string

    member this.Value = let (DataId v) = this in v

type RoleId =
    | RoleId of string

    member this.Value = let (RoleId v) = this in v

type WorkflowId =
    | WorkflowId of string

    member this.Value = let (WorkflowId v) = this in v

type AgentId =
    | AgentId of string

    member this.Value = let (AgentId v) = this in v

/// Qualitative confidence, aligned with echelon-knowledge-store's scale. No
/// numeric score is used because nothing calibrates one.
type Confidence =
    | VeryLow
    | Low
    | Medium
    | High
    | VeryHigh

[<RequireQualifiedAccess>]
module Confidence =
    let rank =
        function
        | VeryLow -> 0
        | Low -> 1
        | Medium -> 2
        | High -> 3
        | VeryHigh -> 4

    let label =
        function
        | VeryLow -> "very-low"
        | Low -> "low"
        | Medium -> "medium"
        | High -> "high"
        | VeryHigh -> "very-high"

    /// The weakest link: a conclusion is no more certain than its least certain input.
    let weakest (cs: Confidence list) =
        cs |> List.sortBy rank |> List.tryHead |> Option.defaultValue VeryLow

/// A three-point estimate with the evidence it rests on. Every quantity used
/// in economics or simulation is an estimate, never a bare number.
type Estimate<[<Measure>] 'u> = {
    Low: float<'u>
    Expected: float<'u>
    High: float<'u>
    Evidence: EvidenceId list
}

/// Which point of an estimate to read.
type Point =
    | LowPoint
    | ExpectedPoint
    | HighPoint

[<RequireQualifiedAccess>]
module Estimate =
    let create (low: float<'u>) (expected: float<'u>) (high: float<'u>) evidence : Estimate<'u> = {
        Low = low
        Expected = expected
        High = high
        Evidence = evidence
    }

    let exactly (v: float<'u>) evidence : Estimate<'u> = create v v v evidence

    let zero<[<Measure>] 'u> : Estimate<'u> =
        exactly (LanguagePrimitives.FloatWithMeasure<'u> 0.0) []

    let at point (e: Estimate<'u>) =
        match point with
        | LowPoint -> e.Low
        | ExpectedPoint -> e.Expected
        | HighPoint -> e.High

    let isOrdered (e: Estimate<'u>) =
        e.Low <= e.Expected && e.Expected <= e.High

    let map (f: float<'u> -> float<'v>) (e: Estimate<'u>) : Estimate<'v> = {
        Low = f e.Low
        Expected = f e.Expected
        High = f e.High
        Evidence = e.Evidence
    }

/// Ordinal level used by the responsibility model.
type Level =
    | Negligible
    | Minor
    | Moderate
    | Major

[<RequireQualifiedAccess>]
module Level =
    let rank =
        function
        | Negligible -> 0
        | Minor -> 1
        | Moderate -> 2
        | Major -> 3

    let atLeast threshold level = rank level >= rank threshold
