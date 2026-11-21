namespace Echelon.Enterprise

/// Model validation. The F# type system guarantees reference *kinds*; this
/// module checks what types cannot: that references resolve, that evidence
/// standing is honest, and that information flows are possible.
module Validation =

    type Severity =
        | Error
        | Warning
        | Advisory

    type Diagnostic = {
        Code: string
        Severity: Severity
        Subject: string
        Message: string
    }

    let private diag code severity subject message = {
        Code = code
        Severity = severity
        Subject = subject
        Message = message
    }

    let severityLabel =
        function
        | Error -> "error"
        | Warning -> "warning"
        | Advisory -> "advisory"

    let private duplicates (kind: string) (ids: string list) =
        ids
        |> List.countBy id
        |> List.filter (fun (_, n) -> n > 1)
        |> List.map (fun (v, n) -> diag "EEF001" Error v $"Duplicate {kind} id ({n} occurrences).")

    let private unresolved (known: Set<string>) (subject: string) (kind: string) (refs: string list) =
        refs
        |> List.filter (known.Contains >> not)
        |> List.map (fun r -> diag "EEF002" Error subject $"Unresolved {kind} reference '{r}'.")

    let private checkEstimate subject (name: string) (e: Estimate<'u>) = [
        if not (Estimate.isOrdered e) then
            yield diag "EEF003" Error subject $"Estimate '{name}' is not ordered low <= expected <= high."
        if e.Evidence.IsEmpty then
            yield diag "EEF004" Warning subject $"Estimate '{name}' cites no evidence."
    ]

    let private checkRate subject name (e: Estimate<1>) = [
        yield! checkEstimate subject name e
        if e.Low < 0.0 || e.High > 1.0 then
            yield diag "EEF013" Error subject $"Rate '{name}' must lie within [0, 1]."
    ]

    let private checkEvidenceRecord (known: Set<string>) (e: Evidence) =
        let subject = e.Id.Value

        [
            if e.Sources.IsEmpty && e.Status <> Hypothesis && e.Status <> Recommendation then
                yield diag "EEF012" Error subject "Evidence cites no source."
            if e.Status = Fact && not (Evidence.isVerified e) then
                yield
                    diag
                        "EEF005"
                        Error
                        subject
                        "Marked as fact but no primary, secondary or observational source supports it."
            if (e.Status = Fact || e.Status = Claim) && Evidence.isIllustrative e then
                yield diag "EEF006" Error subject "Illustrative values cannot be presented as facts or claims."
            yield! unresolved known subject "evidence" (e.Contradicts |> List.map _.Value)
        ]

    /// Information that is available to an activity: workflow inputs plus the
    /// outputs of every earlier activity.
    let private availabilityChecks (w: Workflow) =
        w.Activities
        |> List.fold
            (fun (available: Set<DataId>, ds) a ->
                let missing = a.Inputs |> List.filter (available.Contains >> not)

                let ds' =
                    missing
                    |> List.map (fun d ->
                        diag
                            "EEF008"
                            Error
                            a.Id.Value
                            $"Input '{d.Value}' is neither a workflow input nor produced by an earlier activity.")

                (Set.union available (Set.ofList a.Outputs), ds @ ds'))
            (Set.ofList w.Inputs, [])

    let private outcomeCoverage (m: EnterpriseModel) (w: Workflow) (available: Set<DataId>) =
        EnterpriseModel.tryOutcome m w.Outcome
        |> Option.map (fun o ->
            o.RequiredInformation
            |> List.filter (available.Contains >> not)
            |> List.map (fun d ->
                diag "EEF009" Error w.Id.Value $"Outcome requires '{d.Value}' but the workflow never produces it."))
        |> Option.defaultValue []

    let private obligationCoverage (m: EnterpriseModel) (w: Workflow) =
        let satisfied = w.Activities |> List.collect _.Satisfies |> Set.ofList

        w.Governing
        |> List.filter (satisfied.Contains >> not)
        |> List.choose (EnterpriseModel.tryConstraint m)
        |> List.filter (fun c -> not c.Obligations.IsEmpty)
        |> List.map (fun c ->
            diag
                "EEF011"
                Warning
                w.Id.Value
                $"Governing constraint '{c.Id.Value}' is satisfied by no current activity (model gap or compliance gap).")

    let private ownershipChecks (m: EnterpriseModel) =
        let owners =
            m.Capabilities
            |> List.collect (fun c -> c.Owns |> List.map (fun d -> d, c.Id))
            |> List.groupBy fst

        [
            for d, cs in owners do
                if cs.Length > 1 then
                    yield
                        diag
                            "EEF007"
                            Error
                            d.Value
                            "Data item is owned by more than one capability (no single system of record)."
            let owned = owners |> List.map fst |> Set.ofList

            for d in m.Data do
                if not (owned.Contains d.Id) then
                    yield diag "EEF014" Warning d.Id.Value "Data item has no owning capability."
        ]

    let private contradictionAdvisories (m: EnterpriseModel) =
        m.Evidence
        |> List.collect (fun e ->
            e.Contradicts
            |> List.map (fun c ->
                diag
                    "EEF010"
                    Advisory
                    e.Id.Value
                    $"Contradicts '{c.Value}'. Preserved; not resolved by the framework."))

    /// Validates a model. An empty error list means the model may be analysed.
    let validate (m: EnterpriseModel) : Diagnostic list =
        let ids f xs = xs |> List.map f |> Set.ofList
        let evidence = ids (fun (e: Evidence) -> e.Id.Value) m.Evidence
        let data = ids (fun (d: DataItem) -> d.Id.Value) m.Data
        let roles = ids (fun (r: Role) -> r.Id.Value) m.Roles
        let outcomes = ids (fun (o: Outcome) -> o.Id.Value) m.Outcomes
        let stakeholders = ids (fun (s: Stakeholder) -> s.Id.Value) m.Stakeholders
        let constraints = ids (fun (c: Constraint) -> c.Id.Value) m.Constraints
        let decisions = ids (fun (d: Decision) -> d.Id.Value) m.Decisions

        let evRefs subject (refs: EvidenceId list) =
            unresolved evidence subject "evidence" (refs |> List.map _.Value)

        let dataRefs subject (refs: DataId list) =
            unresolved data subject "data" (refs |> List.map _.Value)

        let activities = m.Workflows |> List.collect _.Activities

        [
            yield! duplicates "evidence" (m.Evidence |> List.map _.Id.Value)
            yield! duplicates "data" (m.Data |> List.map _.Id.Value)
            yield! duplicates "role" (m.Roles |> List.map _.Id.Value)
            yield! duplicates "outcome" (m.Outcomes |> List.map _.Id.Value)
            yield! duplicates "constraint" (m.Constraints |> List.map _.Id.Value)
            yield! duplicates "capability" (m.Capabilities |> List.map _.Id.Value)
            yield! duplicates "decision" (m.Decisions |> List.map _.Id.Value)
            yield! duplicates "workflow" (m.Workflows |> List.map _.Id.Value)
            yield! duplicates "activity" (activities |> List.map _.Id.Value)

            for e in m.Evidence do
                yield! checkEvidenceRecord evidence e

            for o in m.Outcomes do
                yield! unresolved stakeholders o.Id.Value "stakeholder" (o.For |> List.map _.Value)
                yield! dataRefs o.Id.Value o.RequiredInformation
                yield! evRefs o.Id.Value o.Evidence

            for c in m.Constraints do
                yield! evRefs c.Id.Value c.Evidence

                for ob in c.Obligations do
                    match ob with
                    | HumanAuthorization r -> yield! unresolved roles c.Id.Value "role" [ r.Value ]
                    | RetainRecord d
                    | RequiresInformation d -> yield! dataRefs c.Id.Value [ d ]
                    | IndependentCheck
                    | NoAiClearance -> ()

            for c in m.Capabilities do
                yield! unresolved outcomes c.Id.Value "outcome" (c.Supports |> List.map _.Value)
                yield! dataRefs c.Id.Value c.Owns

            for d in m.Decisions do
                yield! dataRefs d.Id.Value (d.Output :: d.Inputs)
                yield! evRefs d.Id.Value d.Evidence

            for r in m.Roles do
                yield! checkEstimate r.Id.Value "loaded cost per hour" r.LoadedCostPerHour

            yield! ownershipChecks m

            for w in m.Workflows do
                let subject = w.Id.Value
                yield! unresolved outcomes subject "outcome" [ w.Outcome.Value ]
                yield! unresolved constraints subject "constraint" (w.Governing |> List.map _.Value)
                yield! unresolved roles subject "role" [ w.Owner.Value ]
                yield! dataRefs subject w.Inputs
                yield! evRefs subject w.Evidence

                for a in w.Activities do
                    let s = a.Id.Value
                    yield! dataRefs s (a.Inputs @ a.Outputs)
                    yield! unresolved constraints s "constraint" (a.Satisfies |> List.map _.Value)
                    yield! evRefs s a.Evidence

                    match a.Nature with
                    | Decide d -> yield! unresolved decisions s "decision" [ d.Value ]
                    | _ -> ()

                    match a.Performer with
                    | Human r -> yield! unresolved roles s "role" [ r.Value ]
                    | Software _
                    | ExternalParty _ -> ()

                    for c in a.Satisfies |> List.choose (EnterpriseModel.tryConstraint m) do
                        for ob in c.Obligations do
                            match ob with
                            | RequiresInformation d when not (a.Inputs @ a.Outputs |> List.contains d) ->
                                yield
                                    diag
                                        "EEF015"
                                        Warning
                                        s
                                        $"Claims to satisfy '{c.Id.Value}' without using required information '{d.Value}'."
                            | _ -> ()

                    yield! checkEstimate s "effort" a.Effort
                    yield! checkEstimate s "delay" a.Delay
                    yield! checkRate s "exception rate" a.ExceptionRate

                let available, flowDiagnostics = availabilityChecks w
                yield! flowDiagnostics
                yield! outcomeCoverage m w available
                yield! obligationCoverage m w

            let e = m.Economics
            yield! checkEstimate "economics" "annual volume" e.AnnualVolume
            yield! checkRate "economics" "assisted effort factor" e.AssistedEffortFactor
            yield! checkRate "economics" "agent escalation rate" e.AgentEscalationRate
            yield! checkEstimate "economics" "escalation effort" e.EscalationEffort
            yield! checkEstimate "economics" "inference cost per agent step" e.InferenceCostPerAgentStep
            yield! checkEstimate "economics" "inference cost per assisted step" e.InferenceCostPerAssistedStep
            yield! checkEstimate "economics" "deterministic cost per step" e.DeterministicCostPerStep
            yield! checkEstimate "economics" "current fixed annual" e.CurrentFixedAnnual
            yield! checkEstimate "economics" "redesign fixed annual" e.RedesignFixedAnnual
            yield! checkEstimate "economics" "redesign one-time cost" e.RedesignOneTimeCost
            yield! checkRate "economics" "current error rate" e.CurrentErrorRate
            yield! checkRate "economics" "redesign error rate" e.RedesignErrorRate
            yield! checkEstimate "economics" "loss per error" e.LossPerError

            yield! contradictionAdvisories m
        ]

    let errors (ds: Diagnostic list) =
        ds |> List.filter (fun d -> d.Severity = Error)

    /// Railway entry point: a model is only analysed when it has no errors.
    let check (m: EnterpriseModel) : Result<EnterpriseModel * Diagnostic list, Diagnostic list> =
        let ds = validate m

        match errors ds with
        | [] -> Result.Ok(m, ds)
        | _ -> Result.Error ds
