namespace Echelon.Enterprise

/// One statement produced by an analytical lens.
type Finding = {
    Subject: string
    Statement: string
    Severity: string
}

/// A lens's position on adopting the redesign, in a form that can be compared.
type Stance =
    | Proceed
    | ProceedWithSafeguards
    | Defer
    | Reject

/// The output of one independent analytical method. Lenses never see each
/// other's findings; disagreement between them is reported, not reconciled.
type LensReport = {
    Lens: string
    /// What this module actually does, in contrast to what the named method does.
    Method: string
    Findings: Finding list
    Stance: Stance
    Recommendation: string
    Limitations: string list
}

type Disagreement = {
    Topic: string
    Positions: (string * string) list
}

/// Interfaces to Clarity and EDF.
///
/// Both are, as of the 2026-11 audit, Markdown methodologies with no
/// executable API (clarity-framework, echelon-diagnostic-framework v0.3), and
/// their own research reports no measured accuracy gain. This module therefore
/// prepares structured inputs in each method's vocabulary (a Clarity Lean brief
/// and an EDF diagnostic packet) for a human or agent reviewer. It does not
/// perform, and must not be described as performing, a Clarity or EDF review.
module Lenses =

    let private finding subject severity statement = {
        Subject = subject
        Statement = statement
        Severity = severity
    }

    let private weakBasis =
        function
        | Evidence.Assumed
        | Evidence.IllustrativeOnly
        | Evidence.Unsupported -> true
        | Evidence.Verified
        | Evidence.Reported -> false

    /// Inputs for a Clarity Lean review (decision-first brief, evidence check,
    /// strongest rival, load-bearing assumptions, act-or-wait, reconsider-if).
    let clarityBrief (m: EnterpriseModel) (r: Redesign) (e: EconomicComparison) : LensReport =
        let loadBearing = e.Sensitivities |> List.filter _.FlipsConclusion
        let weakLoadBearing = loadBearing |> List.filter (fun s -> weakBasis s.Basis)

        let basisCounts =
            m.Evidence
            |> List.countBy (fun ev -> Evidence.basis (EnterpriseModel.tryEvidence m) [ ev.Id ])
            |> List.map (fun (b, n) -> $"{Evidence.basisLabel b}: {n}")
            |> String.concat ", "

        let contradictions =
            m.Evidence
            |> List.collect (fun ev ->
                ev.Contradicts
                |> List.map (fun c ->
                    finding
                        ev.Id.Value
                        "evidence-conflict"
                        $"Conflicts with {c.Value}; unresolved and carried forward."))

        let hypotheses =
            r.Dispositions
            |> List.filter (fun (_, d) -> Reinvention.dispositionLabel d <> "retained")
            |> List.map (fun (a, d) ->
                finding a.Id.Value "hypothesis" $"{Reinvention.dispositionLabel d}: {Reinvention.dispositionReason d}")

        let names (ws: Sensitivity list) =
            ws |> List.map _.Lever |> String.concat ", "

        let stance, actOrWait =
            match e.Verdict, weakLoadBearing with
            | RobustlySuperior, [] -> Proceed, "Act: pilot the redesign; robust to every modelled assumption."
            | RobustlySuperior, ws ->
                ProceedWithSafeguards,
                $"Act cautiously: robust at modelled ranges, but ranges for {names ws} are not evidenced."
            | SuperiorAtExpected _, [] when e.Adverse.AnnualSavings < 0.0<usd> ->
                Defer,
                "Wait: superior at expected values only; the combined adverse corner reverses it and the inputs are largely unverified. Measure effort, exception rates and volume first."
            | SuperiorAtExpected _, [] ->
                ProceedWithSafeguards, "Act on a reversible pilot with reconsider-if thresholds."
            | SuperiorAtExpected _, ws ->
                Defer, $"Wait: measure {names ws} first; the conclusion depends on unverified assumptions."
            | InferiorAtExpected _, _ ->
                Reject, "Do not adopt at expected values; reconsider if the listed levers move."
            | RobustlyInferior, _ -> Reject, "Do not adopt: inferior even at favourable assumptions."

        {
            Lens = "clarity-lean-brief"
            Method = "Prepares Clarity Lean inputs from the model. Does not perform a Clarity review; a reviewer must."
            Findings = [
                yield
                    finding
                        r.Workflow.Id.Value
                        "decision"
                        $"Decision: adopt the redesigned '{r.Workflow.Name}' ({Reinvention.classificationLabel r.Classification})?"
                yield finding "evidence" "evidence-check" $"Evidence basis across the model: {basisCounts}."
                yield
                    finding
                        "rival"
                        "strongest-rival"
                        "Rival: keep the current structure and automate in place (superficial automation); compare before adopting."
                for s in loadBearing do
                    yield
                        finding
                            s.Lever
                            "load-bearing-assumption"
                            $"Reverses the conclusion within its range (basis: {Evidence.basisLabel s.Basis}); reconsider if it moves toward the adverse end (savings {s.SavingsAtLow:N0} / {s.SavingsAtHigh:N0} USD)."
                yield! contradictions
                yield! hypotheses
            ]
            Stance = stance
            Recommendation = actOrWait
            Limitations = [
                "Clarity has no machine-readable model or API; this brief follows its documented Lean structure."
                "Clarity's own 2026 research does not support claims that it improves decision accuracy."
            ]
        }

    /// Steps whose failure stops every later stage: each later step
    /// transitively depends on it.
    let singlePointsOfFailure (r: Redesign) =
        let deps = r.Steps |> List.map (fun s -> s.Activity.Id, s.DependsOn) |> Map.ofList

        let rec ancestors (id: ActivityId) : Set<ActivityId> =
            deps.TryFind id
            |> Option.defaultValue []
            |> List.fold (fun acc d -> Set.add d acc |> Set.union (ancestors d)) Set.empty

        let finalStage = r.Steps |> List.map _.Stage |> List.fold max 0
        let terminal = r.Steps |> List.filter (fun s -> s.Stage = finalStage)

        r.Steps
        |> List.filter (fun s ->
            s.Stage < finalStage
            && terminal
               |> List.forall (fun t -> (ancestors t.Activity.Id).Contains s.Activity.Id))

    /// Downstream fan-out of each step: how many steps a failure would propagate to.
    let propagation (r: Redesign) =
        let rec dependents (id: ActivityId) : Set<ActivityId> =
            r.Steps
            |> List.filter (fun s -> s.DependsOn |> List.contains id)
            |> List.fold (fun acc s -> Set.add s.Activity.Id acc |> Set.union (dependents s.Activity.Id)) Set.empty

        r.Steps |> List.map (fun s -> s, (dependents s.Activity.Id).Count)

    /// Inputs for an EDF-style diagnosis: outcome, manifestations, origin
    /// network, propagation, control points and unknowns.
    let edfPacket (m: EnterpriseModel) (r: Redesign) : LensReport =
        let spofs = singlePointsOfFailure r

        let origins =
            r.Dispositions
            |> List.choose (fun (a, d) ->
                match a.Nature, d with
                | (Transfer | Wait), ReplacedByEvent _ -> Some(a, "organizational: handoff or queue between parties")
                | Rework, _ -> Some(a, "informational: defects created upstream")
                | _, Eliminated _ -> Some(a, "organizational: activity without a required output")
                | _, MergedInto _ -> Some(a, "organizational: duplicated control or information")
                | _ -> None)

        let unknowns =
            m.Evidence
            |> List.filter (fun ev -> weakBasis (Evidence.basis (EnterpriseModel.tryEvidence m) [ ev.Id ]))
            |> List.map (fun ev -> finding ev.Id.Value "unknown" $"{EpistemicStatus.label ev.Status}: {ev.Statement}")

        let agentSpof =
            spofs
            |> List.filter (fun s ->
                match s.Assignment |> Option.map _.Executor with
                | Some(SupervisedAgent _)
                | Some(AgentScreenedHumanClearance _) -> true
                | _ -> false)

        {
            Lens = "edf-diagnostic-packet"
            Method =
                "Prepares EDF v0.3 inputs (manifestations, origins, propagation, control points). Does not perform an EDF diagnosis."
            Findings = [
                yield
                    finding
                        r.Workflow.Id.Value
                        "manifestation"
                        $"Current structure: {r.Before.Steps} activities, {r.Before.HumanHandoffs} human handoffs, {r.Before.ExpectedDelay:N0} expected queue minutes."
                for a, origin in origins do
                    yield finding a.Id.Value "origin" origin
                for s, n in propagation r |> List.sortByDescending snd do
                    if n > 0 then
                        yield
                            finding s.Activity.Id.Value "propagation" $"A failure propagates to {n} downstream step(s)."
                for s in spofs do
                    yield
                        finding
                            s.Activity.Id.Value
                            "control-point"
                            "Single point of failure: every terminal step depends on it; candidate control point."
                yield! unknowns
            ]
            Stance =
                match agentSpof with
                | [] -> Proceed
                | _ -> ProceedWithSafeguards
            Recommendation =
                match agentSpof with
                | [] -> "No agent sits on a single point of failure."
                | xs ->
                    let ids = xs |> List.map _.Activity.Id.Value |> String.concat ", "
                    $"Add fallback and independent monitoring before deploying agents at {ids}; they are single points of failure."
            Limitations = [
                "EDF v0.3 has no schema or executable form; this packet maps model facts onto its stated grammar."
                "EDF's comparative research reports no measurable advantage over generic analysis."
                "Propagation is structural (data dependencies), not a probabilistic failure model."
            ]
        }

    /// Where lenses and the economics engine point in different directions.
    /// Reported side by side; the framework does not choose a winner.
    let stanceLabel =
        function
        | Proceed -> "proceed"
        | ProceedWithSafeguards -> "proceed-with-safeguards"
        | Defer -> "defer"
        | Reject -> "reject"

    let disagreements (economics: EconomicComparison) (clarity: LensReport) (edf: LensReport) =
        let econPosition = Economics.verdictLabel economics.Verdict

        let economicsFavours =
            match economics.Verdict with
            | RobustlySuperior
            | SuperiorAtExpected _ -> true
            | InferiorAtExpected _
            | RobustlyInferior -> false

        let holdsBack lens =
            match lens.Stance with
            | Defer
            | Reject -> true
            | Proceed
            | ProceedWithSafeguards -> false

        [
            if economicsFavours && holdsBack clarity then
                yield {
                    Topic = "adoption"
                    Positions = [ "economics", econPosition; clarity.Lens, clarity.Recommendation ]
                }
            if economicsFavours && edf.Stance <> Proceed then
                yield {
                    Topic = "deployment risk"
                    Positions = [ "economics", econPosition; edf.Lens, edf.Recommendation ]
                }
        ]
