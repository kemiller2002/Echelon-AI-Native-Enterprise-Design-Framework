namespace Echelon.Enterprise

open System.Text.Json
open System.Text.Json.Nodes

/// The machine-readable intermediate representation (schema
/// `eef.analysis/0`, see schemas/eef-analysis-0.schema.json). Explicit
/// encoders keep the wire format independent of F# type layout.
module Json =

    let schema = "eef.analysis/0"

    type private Node = JsonNode | null

    let private str (s: string) : Node = JsonValue.Create s

    let private num (f: float) : Node =
        JsonValue.Create(System.Math.Round(f, 4))

    let private int' (i: int) : Node = JsonValue.Create i
    let private bool' (b: bool) : Node = JsonValue.Create b

    let private obj (fields: (string * Node) list) : Node =
        let o = JsonObject()
        fields |> List.iter (fun (k, v) -> o[k] <- v)
        o

    let private arr (xs: Node list) : Node =
        let a = JsonArray()
        xs |> List.iter a.Add
        a

    let private strs (xs: string list) = xs |> List.map str |> arr

    let private opt (f: 'a -> Node) (x: 'a option) : Node =
        match x with
        | Some v -> f v
        | None -> null

    let private estimate (e: Estimate<'u>) =
        obj [
            "low", num (float e.Low)
            "expected", num (float e.Expected)
            "high", num (float e.High)
            "evidence", strs (e.Evidence |> List.map _.Value)
        ]

    let private evidence (e: Evidence) =
        obj [
            "id", str e.Id.Value
            "statement", str e.Statement
            "status", str (EpistemicStatus.label e.Status)
            "confidence", str (Confidence.label e.Confidence)
            "sources",
            e.Sources
            |> List.map (fun s ->
                obj [
                    "citation", str s.Citation
                    "locator", opt str s.Locator
                    "kind", str (SourceKind.label s.Kind)
                ])
            |> arr
            "contradicts", strs (e.Contradicts |> List.map _.Value)
        ]

    let private diagnostic (d: Validation.Diagnostic) =
        obj [
            "code", str d.Code
            "severity", str (Validation.severityLabel d.Severity)
            "subject", str d.Subject
            "message", str d.Message
        ]

    let private executor (a: Assignment option) =
        match a with
        | None -> obj [ "kind", str "external-party" ]
        | Some a ->
            obj [
                "kind", str (Responsibility.executorLabel a.Executor)
                "role", opt (fun (r: RoleId) -> str r.Value) (Responsibility.executorRole a.Executor)
                "decidingRule", str a.DecidingRule
                "trace", strs a.Trace
            ]

    let private metrics (s: StructuralMetrics) =
        obj [
            "steps", int' s.Steps
            "humanHandoffs", int' s.HumanHandoffs
            "humanTouches", int' s.HumanTouches
            "stages", int' s.Stages
            "expectedDelayMinutes", num (float s.ExpectedDelay)
        ]

    let private redesign (r: Redesign) =
        obj [
            "workflow", str r.Workflow.Id.Value
            "classification", str (Reinvention.classificationLabel r.Classification)
            "structuralChange", num r.StructuralChange
            "necessaryData", strs (r.NecessaryData |> List.map _.Value)
            "dispositions",
            r.Dispositions
            |> List.map (fun (a, d) ->
                obj [
                    "activity", str a.Id.Value
                    "disposition", str (Reinvention.dispositionLabel d)
                    "reason", str (Reinvention.dispositionReason d)
                ])
            |> arr
            "steps",
            r.Steps
            |> List.map (fun s ->
                obj [
                    "id", str s.Activity.Id.Value
                    "name", str s.Activity.Name
                    "stage", int' s.Stage
                    "dependsOn", strs (s.DependsOn |> List.map _.Value)
                    "absorbed", strs (s.Absorbed |> List.map _.Value)
                    "executor", executor s.Assignment
                ])
            |> arr
            "before", metrics r.Before
            "after", metrics r.After
        ]

    let private principal =
        function
        | RolePrincipal r -> $"role:{r.Value}"
        | AgentPrincipal a -> $"agent:{a.Value}"
        | ServicePrincipal s -> $"service:{s}"

    let private architecture (a: ArchitectureSpec) =
        obj [
            "workflow", str a.Workflow.Value
            "capabilities", strs (a.Capabilities |> List.map _.Id.Value)
            "apis",
            a.Apis
            |> List.map (fun api ->
                obj [
                    "id", str api.Id
                    "name", str api.Name
                    "capability", opt (fun (c: CapabilityId) -> str c.Value) api.Capability
                    "inputs", strs (api.Inputs |> List.map _.Value)
                    "outputs", strs (api.Outputs |> List.map _.Value)
                    "permission", str api.Permission
                    "step", str api.Step.Value
                    "requiresHumanAuthority", opt (fun (r: RoleId) -> str r.Value) api.RequiresHumanAuthority
                    "forbidsAiClearance", bool' api.ForbidsAiClearance
                ])
            |> arr
            "events",
            a.Events
            |> List.map (fun e ->
                obj [
                    "name", str e.Name
                    "data", str e.Data.Value
                    "producer", str e.Producer.Value
                    "consumers", strs (e.Consumers |> List.map _.Value)
                ])
            |> arr
            "agents",
            a.Agents
            |> List.map (fun g ->
                obj [
                    "id", str g.Id.Value
                    "name", str g.Name
                    "tools", strs g.Tools
                    "reads", strs (g.Reads |> List.map _.Value)
                    "escalatesTo", strs (g.EscalatesTo |> List.map _.Value)
                    "boundaries", strs g.Boundaries
                    "evaluationCriteria", strs g.EvaluationCriteria
                ])
            |> arr
            "grants",
            a.Grants
            |> List.map (fun g ->
                obj [
                    "principal", str (principal g.Principal)
                    "api", str g.Api
                    "right",
                    str (
                        match g.Right with
                        | Execute -> "execute"
                        | Approve -> "approve"
                    )
                ])
            |> arr
            "controls",
            a.Controls
            |> List.map (fun c ->
                obj [
                    "constraint", str c.Constraint.Value
                    "obligation", str c.Obligation
                    "coveredBy", strs (c.CoveredBy |> List.map _.Value)
                    "auditEvidence", strs (c.AuditEvidence |> List.map _.Value)
                ])
            |> arr
            "trace",
            a.Trace
            |> List.map (fun t ->
                obj [
                    "element", str t.Element
                    "step", str t.Step.Value
                    "activities", strs (t.Activities |> List.map _.Value)
                    "constraints", strs (t.Constraints |> List.map _.Value)
                    "outcome", str t.Outcome.Value
                    "evidence", strs (t.Evidence |> List.map _.Value)
                ])
            |> arr
            "roadmap",
            a.Roadmap
            |> List.map (fun p ->
                obj [
                    "phase", int' p.Phase
                    "name", str p.Name
                    "steps", strs (p.Steps |> List.map _.Value)
                    "gate", str p.Gate
                ])
            |> arr
            "violations", strs a.Violations
        ]

    let private lens (l: LensReport) =
        obj [
            "lens", str l.Lens
            "method", str l.Method
            "stance", str (Lenses.stanceLabel l.Stance)
            "recommendation", str l.Recommendation
            "findings",
            l.Findings
            |> List.map (fun f ->
                obj [
                    "subject", str f.Subject
                    "kind", str f.Severity
                    "statement", str f.Statement
                ])
            |> arr
            "limitations", strs l.Limitations
        ]

    let private cost (c: CostBreakdown) =
        obj [
            "laborPerTxn", num (float c.LaborPerTxn)
            "machinePerTxn", num (float c.MachinePerTxn)
            "riskLossPerTxn", num (float c.RiskLossPerTxn)
            "fixedAnnual", num (float c.FixedAnnual)
            "annualTotal", num (float c.AnnualTotal)
            "annualVolume", num (float c.AnnualVolume)
        ]

    let private scenario (s: Scenario) =
        obj [
            "name", str s.Name
            "current", cost s.Current
            "redesign", cost s.Redesign
            "annualSavings", num (float s.AnnualSavings)
        ]

    let private economics (e: EconomicComparison) =
        obj [
            "verdict", str (Economics.verdictLabel e.Verdict)
            "expected", scenario e.Expected
            "adverse", scenario e.Adverse
            "favourable", scenario e.Favourable
            "breakEvenVolume", opt (fun (v: float<txn>) -> num (float v)) e.BreakEvenVolume
            "sensitivities",
            e.Sensitivities
            |> List.map (fun s ->
                obj [
                    "lever", str s.Lever
                    "savingsAtLow", num (float s.SavingsAtLow)
                    "savingsAtHigh", num (float s.SavingsAtHigh)
                    "flipsConclusion", bool' s.FlipsConclusion
                    "basis", str (Evidence.basisLabel s.Basis)
                ])
            |> arr
        ]

    let private simulation (s: SimulationResult) =
        obj [
            "label", str s.Label
            "transactions", int' s.Transactions
            "meanCycleMinutes", num (float s.MeanCycle)
            "p50CycleMinutes", num (float s.P50Cycle)
            "p90CycleMinutes", num (float s.P90Cycle)
            "humanMinutesPerTxn", num (float s.HumanMinutesPerTxn)
            "utilization",
            s.Utilization
            |> List.map (fun (r, u) -> obj [ "role", str r.Value; "utilization", num u ])
            |> arr
        ]

    let encode (report: AnalysisReport) : Node =
        obj [
            "schema", str schema
            "model", str report.Model.Name
            "industry", str report.Model.Industry
            "scope", str report.Model.Scope
            "diagnostics", report.Diagnostics |> List.map diagnostic |> arr
            "evidence", report.Model.Evidence |> List.map evidence |> arr
            "economics", economics report.Economics
            "workflows",
            report.Workflows
            |> List.map (fun w ->
                obj [
                    "redesign", redesign w.Redesign
                    "architecture", architecture w.Architecture
                    "lenses", arr [ lens w.Clarity; lens w.Edf ]
                    "disagreements",
                    w.Disagreements
                    |> List.map (fun d ->
                        obj [
                            "topic", str d.Topic
                            "positions",
                            d.Positions
                            |> List.map (fun (who, pos) -> obj [ "source", str who; "position", str pos ])
                            |> arr
                        ])
                    |> arr
                    "simulation", arr [ simulation w.CurrentSimulation; simulation w.RedesignSimulation ]
                ])
            |> arr
        ]

    let serialize (report: AnalysisReport) =
        match encode report with
        | null -> "null"
        | node -> node.ToJsonString(JsonSerializerOptions(WriteIndented = true))
