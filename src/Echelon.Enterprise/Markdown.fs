namespace Echelon.Enterprise

/// Human-readable rendering of an analysis. Generated, never hand-edited.
module Markdown =

    let private esc (s: string) = s.Replace("|", "\\|")

    let private table (headers: string list) (rows: string list list) = [
        yield "| " + (headers |> String.concat " | ") + " |"
        yield "|" + (headers |> List.map (fun _ -> "---") |> String.concat "|") + "|"
        for r in rows do
            yield "| " + (r |> List.map esc |> String.concat " | ") + " |"
    ]

    let private ids (xs: 'a list) (f: 'a -> string) =
        match xs with
        | [] -> "—"
        | _ -> xs |> List.map f |> String.concat ", "

    let private money (x: float<usd>) = $"{float x:N0}"

    let private executorText (a: Assignment option) =
        match a with
        | None -> "external party"
        | Some a ->
            match Responsibility.executorRole a.Executor with
            | Some r -> $"{Responsibility.executorLabel a.Executor} ({r.Value})"
            | None -> Responsibility.executorLabel a.Executor

    let private evidenceSection (m: EnterpriseModel) = [
        yield "## Evidence register"
        yield ""
        yield
            "Status and basis are reported as recorded; the framework never upgrades them. Illustrative values exist only to make the model executable."
        yield ""
        yield!
            table
                [ "Id"; "Status"; "Basis"; "Confidence"; "Statement"; "Sources" ]
                (m.Evidence
                 |> List.map (fun e -> [
                     e.Id.Value
                     EpistemicStatus.label e.Status
                     Evidence.basisLabel (Evidence.basis (EnterpriseModel.tryEvidence m) [ e.Id ])
                     Confidence.label e.Confidence
                     e.Statement
                     e.Sources
                     |> List.map (fun s ->
                         let loc = s.Locator |> Option.map (fun l -> $" <{l}>") |> Option.defaultValue ""
                         $"[{SourceKind.label s.Kind}] {s.Citation}{loc}")
                     |> String.concat "; "
                 ]))
        yield ""
    ]

    let private redesignSection (w: WorkflowAnalysis) =
        let r = w.Redesign

        [
            yield $"## Workflow: {r.Workflow.Name} (`{r.Workflow.Id.Value}`)"
            yield ""
            yield
                $"**Classification:** {Reinvention.classificationLabel r.Classification} (structural change {r.StructuralChange:P0}). Every non-retained disposition is a hypothesis."
            yield ""
            yield "### Structural comparison"
            yield ""
            yield!
                table [ "Metric"; "Current"; "Redesign" ] [
                    [ "Steps"; string r.Before.Steps; string r.After.Steps ]
                    [
                        "Human handoffs"
                        string r.Before.HumanHandoffs
                        string r.After.HumanHandoffs
                    ]
                    [ "Human touches"; string r.Before.HumanTouches; string r.After.HumanTouches ]
                    [ "Sequential stages"; string r.Before.Stages; string r.After.Stages ]
                    [
                        "Expected queue delay (min)"
                        $"{float r.Before.ExpectedDelay:N0}"
                        $"{float r.After.ExpectedDelay:N0}"
                    ]
                ]
            yield ""
            yield "### Dispositions of current activities"
            yield ""
            yield!
                table
                    [ "Activity"; "Name"; "Disposition"; "Reason" ]
                    (r.Dispositions
                     |> List.map (fun (a, d) -> [
                         a.Id.Value
                         a.Name
                         Reinvention.dispositionLabel d
                         Reinvention.dispositionReason d
                     ]))
            yield ""
            yield "### Redesigned steps and AI responsibility assignments"
            yield ""
            yield!
                table
                    [ "Stage"; "Step"; "Name"; "Executor"; "Depends on"; "Deciding rule" ]
                    (r.Steps
                     |> List.map (fun s -> [
                         string s.Stage
                         s.Activity.Id.Value
                         s.Activity.Name
                         executorText s.Assignment
                         ids s.DependsOn _.Value
                         s.Assignment |> Option.map _.DecidingRule |> Option.defaultValue "—"
                     ]))
            yield ""
        ]

    let private architectureSection (m: EnterpriseModel) (a: ArchitectureSpec) = [
        yield "### Generated architecture"
        yield ""
        yield "#### Capability map"
        yield ""
        yield!
            table
                [ "Capability"; "Domain"; "Supports"; "Owns (system of record)" ]
                (a.Capabilities
                 |> List.map (fun c -> [
                     $"{c.Name} (`{c.Id.Value}`)"
                     c.Domain
                     ids c.Supports _.Value
                     ids c.Owns (EnterpriseModel.dataName m)
                 ]))
        yield ""
        yield "#### API catalog"
        yield ""
        yield!
            table
                [
                    "Operation"
                    "Name"
                    "Capability"
                    "Permission"
                    "Human authority"
                    "No automated clearance"
                ]
                (a.Apis
                 |> List.map (fun api -> [
                     api.Id
                     api.Name
                     api.Capability |> Option.map _.Value |> Option.defaultValue "—"
                     api.Permission
                     api.RequiresHumanAuthority |> Option.map _.Value |> Option.defaultValue "—"
                     (if api.ForbidsAiClearance then "yes" else "—")
                 ]))
        yield ""
        yield "#### Event catalog"
        yield ""
        yield!
            table
                [ "Event"; "Data"; "Producer"; "Consumers" ]
                (a.Events
                 |> List.map (fun e -> [ e.Name; e.Data.Value; e.Producer.Value; ids e.Consumers _.Value ]))
        yield ""
        yield "#### Agent catalog"
        yield ""
        if a.Agents.IsEmpty then
            yield "No supervised agents are proposed."
        for g in a.Agents do
            yield $"**{g.Name}** (`{g.Id.Value}`): tools {ids g.Tools id}; escalates to {ids g.EscalatesTo _.Value}."
            yield ""

            for b in g.Boundaries do
                yield $"- {b}"

            for c in g.EvaluationCriteria do
                yield $"- Evaluation: {c}"

            yield ""
        yield ""
        yield "#### Authorization model"
        yield ""
        yield!
            table
                [ "Principal"; "Operation"; "Right" ]
                (a.Grants
                 |> List.map (fun g -> [
                     (match g.Principal with
                      | RolePrincipal r -> $"role {r.Value}"
                      | AgentPrincipal x -> $"agent {x.Value}"
                      | ServicePrincipal s -> $"service {s}")
                     g.Api
                     (match g.Right with
                      | Execute -> "execute"
                      | Approve -> "approve")
                 ]))
        yield ""
        yield "#### Human decision matrix"
        yield ""
        yield!
            table
                [ "Step"; "Decision"; "Accountable"; "Executor"; "Constraints" ]
                (a.DecisionRights
                 |> List.map (fun d -> [
                     d.Step.Value
                     d.Decision |> Option.map _.Value |> Option.defaultValue "—"
                     d.Accountable.Value
                     d.Executor
                     ids d.Constraints _.Value
                 ]))
        yield ""
        yield "#### Control coverage and audit evidence"
        yield ""
        yield!
            table
                [ "Constraint"; "Obligation"; "Covered by"; "Audit evidence" ]
                (a.Controls
                 |> List.map (fun c -> [
                     c.Constraint.Value
                     c.Obligation
                     ids c.CoveredBy _.Value
                     ids c.AuditEvidence _.Value
                 ]))
        yield ""
        yield "#### Integration points"
        yield ""
        yield!
            table
                [ "Step"; "Party"; "Exchanges" ]
                (a.Integrations
                 |> List.map (fun i -> [ i.Step.Value; i.Party; ids i.Exchanges _.Value ]))
        yield ""
        yield "#### Operational monitoring"
        yield ""
        yield!
            table
                [ "Subject"; "Signal"; "Threshold" ]
                (a.Monitoring |> List.map (fun x -> [ x.Subject; x.Signal; x.Threshold ]))
        yield ""
        yield "#### Implementation roadmap"
        yield ""
        yield!
            table
                [ "Phase"; "Name"; "Steps"; "Gate" ]
                (a.Roadmap
                 |> List.map (fun p -> [ string p.Phase; p.Name; ids p.Steps _.Value; p.Gate ]))
        yield ""
        yield "#### Traceability"
        yield ""
        yield!
            table
                [
                    "Element"
                    "Step"
                    "Current activities"
                    "Constraints"
                    "Outcome"
                    "Evidence"
                ]
                (a.Trace
                 |> List.map (fun t -> [
                     t.Element
                     t.Step.Value
                     ids t.Activities _.Value
                     ids t.Constraints _.Value
                     t.Outcome.Value
                     ids t.Evidence _.Value
                 ]))
        yield ""
        yield "#### Governance invariants"
        yield ""
        match a.Violations with
        | [] ->
            yield
                "All generated-architecture invariants hold (no agent holds a human-authority or clearance operation; every control obligation is covered; every produced datum has a system of record)."
        | vs ->
            for v in vs do
                yield $"- VIOLATION: {v}"
        yield ""
    ]

    let private lensSection (l: LensReport) = [
        yield $"#### {l.Lens}"
        yield ""
        yield $"_Method:_ {l.Method}"
        yield ""
        yield $"**Stance:** {Lenses.stanceLabel l.Stance}. **Recommendation:** {l.Recommendation}"
        yield ""
        for f in l.Findings do
            yield $"- [{f.Severity}] `{f.Subject}`: {f.Statement}"
        yield ""
        yield "_Limitations:_"
        for x in l.Limitations do
            yield $"- {x}"
        yield ""
    ]

    let private simulationRow (s: SimulationResult) = [
        s.Label
        $"{float s.MeanCycle / 60.0:N1}"
        $"{float s.P50Cycle / 60.0:N1}"
        $"{float s.P90Cycle / 60.0:N1}"
        $"{float s.HumanMinutesPerTxn:N1}"
        s.Bottleneck
        |> Option.map (fun (r, u) -> $"{r.Value} ({u:P0})")
        |> Option.defaultValue "—"
    ]

    let private economicsSection (e: EconomicComparison) =
        let s = e.Expected

        [
            yield "## Economics"
            yield ""
            yield $"**Verdict:** {Economics.verdictLabel e.Verdict}"

            match e.Verdict with
            | SuperiorAtExpected xs
            | InferiorAtExpected xs when not xs.IsEmpty ->
                let levers = String.concat ", " xs
                yield $"Conclusion reverses within the range of: {levers}."
            | _ -> ()

            match e.Verdict with
            | SuperiorAtExpected [] when e.Adverse.AnnualSavings < 0.0<usd> ->
                yield
                    "No single assumption reverses the conclusion, but the combined adverse corner does: the redesign is not robustly superior."
            | _ -> ()

            yield ""
            yield!
                table
                    [ "Scenario"; "Current annual (USD)"; "Redesign annual (USD)"; "Savings (USD)" ]
                    ([ e.Expected; e.Adverse; e.Favourable ]
                     |> List.map (fun x -> [
                         x.Name
                         money x.Current.AnnualTotal
                         money x.Redesign.AnnualTotal
                         money x.AnnualSavings
                     ]))
            yield ""
            yield
                $"Per transaction at expected values: current {float s.Current.PerTxn:N2} USD (labour {float s.Current.LaborPerTxn:N2}), redesign {float s.Redesign.PerTxn:N2} USD (labour {float s.Redesign.LaborPerTxn:N2}, machine {float s.Redesign.MachinePerTxn:N2})."
            yield ""

            match e.BreakEvenVolume with
            | Some v -> yield $"Break-even annual volume: {float v:N0} transactions."
            | None -> yield "Break-even volume: never reached (redesign variable cost is not lower)."

            yield ""
            yield "### Sensitivity (one assumption at a time; others at expected)"
            yield ""
            yield!
                table
                    [ "Lever"; "Savings at low"; "Savings at high"; "Flips conclusion"; "Basis" ]
                    (e.Sensitivities
                     |> List.map (fun x -> [
                         x.Lever
                         money x.SavingsAtLow
                         money x.SavingsAtHigh
                         (if x.FlipsConclusion then "YES" else "no")
                         Evidence.basisLabel x.Basis
                     ]))
            yield ""
            yield "### Scale"
            yield ""
            yield!
                table
                    [ "Annual volume"; "Savings (USD)" ]
                    (e.VolumeCurve |> List.map (fun (v, sv) -> [ $"{float v:N0}"; money sv ]))
            yield ""
        ]

    let render (report: AnalysisReport) =
        let m = report.Model

        [
            yield $"# Enterprise analysis: {m.Name}"
            yield ""
            yield
                $"> Generated by the Echelon AI-Native Enterprise Design Framework (`{Json.schema}`). Do not edit by hand."
            yield ""
            yield $"- **Industry:** {m.Industry}"
            yield $"- **Scope:** {m.Scope}"
            yield ""
            yield "## Outcomes"
            yield ""
            for o in m.Outcomes do
                yield
                    $"- `{o.Id.Value}`: {o.Statement} (requires {ids o.RequiredInformation (EnterpriseModel.dataName m)})"
            yield ""
            yield "## Validation diagnostics"
            yield ""
            match report.Diagnostics with
            | [] -> yield "None."
            | ds ->
                for d in ds do
                    yield $"- {d.Code} [{Validation.severityLabel d.Severity}] `{d.Subject}`: {d.Message}"
            yield ""
            yield! economicsSection report.Economics
            for w in report.Workflows do
                yield! redesignSection w
                yield! architectureSection m w.Architecture
                yield "### Simulation (digital twin v0, modelled — not measured)"
                yield ""

                yield!
                    table [
                        "Design"
                        "Mean cycle (h)"
                        "P50 (h)"
                        "P90 (h)"
                        "Human min/txn"
                        "Bottleneck"
                    ] [ simulationRow w.CurrentSimulation; simulationRow w.RedesignSimulation ]

                yield ""
                yield "### Independent analytical lenses"
                yield ""
                yield! lensSection w.Clarity
                yield! lensSection w.Edf
                yield "#### Preserved disagreements"
                yield ""

                match w.Disagreements with
                | [] -> yield "None detected."
                | ds ->
                    for d in ds do
                        yield $"- **{d.Topic}:**"

                        for who, pos in d.Positions do
                            yield $"  - {who}: {pos}"

                yield ""
            yield! evidenceSection m
        ]
        |> String.concat "\n"
