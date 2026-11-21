namespace Echelon.Enterprise

open System

/// Where a piece of evidence came from. Illustrative values are allowed, but
/// they are labelled and can never be treated as facts.
type SourceKind =
    /// Regulator, statute, audited filing, first-party policy document.
    | PrimarySource
    /// Credible secondary analysis or industry dataset.
    | SecondarySource
    /// Claim made by a vendor about its own product; not independently verified.
    | VendorClaim
    | Interview
    | Observation
    /// Output of a model or script in this framework or a dependency.
    | Computed
    /// Placeholder value chosen to make a model runnable. Never evidence of anything.
    | Illustrative

type Source = {
    Citation: string
    /// URL or repository path (e.g. `daf-landscape:research/...md:12`).
    Locator: string option
    Kind: SourceKind
    Retrieved: DateOnly option
}

/// The epistemic standing of a statement. The framework never upgrades a
/// status by itself; it only reports what each conclusion rests on.
type EpistemicStatus =
    /// Verified against a primary or validated secondary source.
    | Fact
    /// Reported by a source but not independently verified.
    | Claim
    /// Taken as true for modelling; not verified.
    | Assumption
    /// A testable proposition about a proposed improvement.
    | Hypothesis
    /// A recommended course of action derived from other statements.
    | Recommendation

type Evidence = {
    Id: EvidenceId
    Statement: string
    Status: EpistemicStatus
    Confidence: Confidence
    Sources: Source list
    /// Evidence this statement conflicts with. Conflicts are preserved,
    /// never silently resolved.
    Contradicts: EvidenceId list
}

[<RequireQualifiedAccess>]
module SourceKind =
    let label =
        function
        | PrimarySource -> "primary"
        | SecondarySource -> "secondary"
        | VendorClaim -> "vendor-claim"
        | Interview -> "interview"
        | Observation -> "observation"
        | Computed -> "computed"
        | Illustrative -> "illustrative"

    let canSupportFact =
        function
        | PrimarySource
        | SecondarySource
        | Observation -> true
        | VendorClaim
        | Interview
        | Computed
        | Illustrative -> false

[<RequireQualifiedAccess>]
module EpistemicStatus =
    let label =
        function
        | Fact -> "fact"
        | Claim -> "claim"
        | Assumption -> "assumption"
        | Hypothesis -> "hypothesis"
        | Recommendation -> "recommendation"

[<RequireQualifiedAccess>]
module Evidence =
    let isIllustrative (e: Evidence) =
        not e.Sources.IsEmpty
        && e.Sources |> List.forall (fun s -> s.Kind = Illustrative)

    /// Verified: a fact backed by at least one source able to support a fact.
    let isVerified (e: Evidence) =
        e.Status = Fact
        && e.Sources |> List.exists (fun s -> SourceKind.canSupportFact s.Kind)

    /// How a quantity's basis is described in reports: the weakest standing of
    /// the evidence behind it.
    type Basis =
        | Verified
        | Reported
        | Assumed
        | IllustrativeOnly
        | Unsupported

    let basisLabel =
        function
        | Verified -> "verified"
        | Reported -> "reported"
        | Assumed -> "assumed"
        | IllustrativeOnly -> "illustrative"
        | Unsupported -> "unsupported"

    let private basisOfOne (e: Evidence) =
        if isIllustrative e then IllustrativeOnly
        elif isVerified e then Verified
        elif e.Status = Claim then Reported
        else Assumed

    let private basisRank =
        function
        | Verified -> 4
        | Reported -> 3
        | Assumed -> 2
        | IllustrativeOnly -> 1
        | Unsupported -> 0

    /// Basis of a set of evidence ids: the weakest member; Unsupported when empty
    /// or when any id cannot be resolved.
    let basis (lookup: EvidenceId -> Evidence option) (ids: EvidenceId list) =
        match ids with
        | [] -> Unsupported
        | _ ->
            ids
            |> List.map (lookup >> Option.map basisOfOne >> Option.defaultValue Unsupported)
            |> List.minBy basisRank
