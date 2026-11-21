namespace Echelon.Enterprise

type CostBreakdown = {
    LaborPerTxn: float<usd / txn>
    MachinePerTxn: float<usd / txn>
    RiskLossPerTxn: float<usd / txn>
    FixedAnnual: float<usd>
    AnnualVolume: float<txn>
} with

    member this.VariablePerTxn =
        this.LaborPerTxn + this.MachinePerTxn + this.RiskLossPerTxn

    member this.AnnualTotal = this.VariablePerTxn * this.AnnualVolume + this.FixedAnnual

    member this.PerTxn =
        if this.AnnualVolume > 0.0<txn> then
            this.AnnualTotal / this.AnnualVolume
        else
            0.0<usd / txn>

type Scenario = {
    Name: string
    Current: CostBreakdown
    Redesign: CostBreakdown
} with

    /// Positive when the redesign is cheaper.
    member this.AnnualSavings = this.Current.AnnualTotal - this.Redesign.AnnualTotal

type Sensitivity = {
    Lever: string
    SavingsAtLow: float<usd>
    SavingsAtHigh: float<usd>
    /// Does either end reverse the sign of expected savings?
    FlipsConclusion: bool
    Basis: Evidence.Basis
}

type EconomicVerdict =
    /// Cheaper even with every assumption at its adverse end.
    | RobustlySuperior
    /// Cheaper at expected values; the listed levers can reverse that.
    | SuperiorAtExpected of sensitiveTo: string list
    /// More expensive at expected values; the listed levers could reverse that.
    | InferiorAtExpected of sensitiveTo: string list
    /// More expensive even with every assumption at its favourable end.
    | RobustlyInferior

type EconomicComparison = {
    Expected: Scenario
    Adverse: Scenario
    Favourable: Scenario
    Sensitivities: Sensitivity list
    VolumeCurve: (float<txn> * float<usd>) list
    BreakEvenVolume: float<txn> option
    Verdict: EconomicVerdict
}

/// Enterprise Economics Engine (v0). See methodology/economics.md.
module Economics =

    /// Levers whose points can vary. Names are stable keys used in reports.
    let levers = [
        "volume"
        "labor-rates"
        "effort"
        "exception-rates"
        "assisted-effort-factor"
        "agent-escalation-rate"
        "escalation-effort"
        "inference-cost-agent"
        "inference-cost-assisted"
        "deterministic-cost"
        "current-fixed"
        "redesign-fixed"
        "redesign-one-time"
        "current-error-rate"
        "redesign-error-rate"
        "loss-per-error"
    ]

    let verdictLabel =
        function
        | RobustlySuperior -> "robustly-superior"
        | SuperiorAtExpected _ -> "superior-at-expected"
        | InferiorAtExpected _ -> "inferior-at-expected"
        | RobustlyInferior -> "robustly-inferior"

    let private rateOf (m: EnterpriseModel) (choose: string -> Point) (r: RoleId) : float<usd / minute> =
        EnterpriseModel.tryRole m r
        |> Option.map (fun role -> Estimate.at (choose "labor-rates") role.LoadedCostPerHour / Units.minutesPerHour)
        |> Option.defaultValue 0.0<usd / minute>

    let private perTxn (x: float<'u>) : float<'u / txn> = x / 1.0<txn>

    let private laborCost m choose (minutes: float<minute>) role = perTxn (minutes * rateOf m choose role)

    /// Human cost of performing an activity on every transaction, plus its exceptions.
    let private humanCost m choose (a: Activity) (factor: float) role =
        let effort = Estimate.at (choose "effort") a.Effort
        let exceptions = Estimate.at (choose "exception-rates") a.ExceptionRate
        laborCost m choose (effort * factor * (1.0 + exceptions)) role

    /// Human cost of handling only the exception share of an activity.
    let private exceptionCost m choose (a: Activity) (factor: float) role =
        let effort = Estimate.at (choose "effort") a.Effort
        let share = Estimate.at (choose "exception-rates") a.ExceptionRate
        laborCost m choose (effort * factor * share) role

    let current (m: EnterpriseModel) (choose: string -> Point) (ws: Workflow list) : CostBreakdown =
        let e = m.Economics

        let det =
            perTxn (Estimate.at (choose "deterministic-cost") e.DeterministicCostPerStep)

        let labor, machine =
            ws
            |> List.collect _.Activities
            |> List.fold
                (fun (l, mc) a ->
                    match a.Performer with
                    | Human r -> (l + humanCost m choose a 1.0 r, mc)
                    | Software _ -> (l, mc + det)
                    | ExternalParty _ -> (l, mc))
                (0.0<usd / txn>, 0.0<usd / txn>)

        {
            LaborPerTxn = labor
            MachinePerTxn = machine
            RiskLossPerTxn =
                perTxn (
                    Estimate.at (choose "current-error-rate") e.CurrentErrorRate
                    * Estimate.at (choose "loss-per-error") e.LossPerError
                )
            FixedAnnual = Estimate.at (choose "current-fixed") e.CurrentFixedAnnual
            AnnualVolume = Estimate.at (choose "volume") e.AnnualVolume
        }

    let redesigned (m: EnterpriseModel) (choose: string -> Point) (rs: Redesign list) : CostBreakdown =
        let e = m.Economics
        let at name est = Estimate.at (choose name) est
        let det = perTxn (at "deterministic-cost" e.DeterministicCostPerStep)
        let assist = at "assisted-effort-factor" e.AssistedEffortFactor

        let assistedInference =
            perTxn (at "inference-cost-assisted" e.InferenceCostPerAssistedStep)

        // Rules still need a human for the exceptions they cannot decide.
        let exceptionHandling (s: Step) (w: Workflow) =
            exceptionCost m choose s.Activity assist (Responsibility.accountableFor w s.Activity)

        let stepCost (w: Workflow) (s: Step) =
            match s.Assignment |> Option.map _.Executor with
            | None -> (0.0<usd / txn>, 0.0<usd / txn>)
            | Some DeterministicSoftware
            | Some IndependentVerification -> (exceptionHandling s w, det)
            | Some(AgentScreenedHumanClearance r) ->
                (exceptionCost m choose s.Activity assist r,
                 perTxn (at "inference-cost-agent" e.InferenceCostPerAgentStep))
            | Some(AiAssistedHuman r)
            | Some(AuthorizedHuman r) -> (humanCost m choose s.Activity assist r, assistedInference)
            | Some(SupervisedAgent r) ->
                let escalation =
                    at "agent-escalation-rate" e.AgentEscalationRate
                    * at "escalation-effort" e.EscalationEffort
                    * rateOf m choose r

                (perTxn escalation, perTxn (at "inference-cost-agent" e.InferenceCostPerAgentStep))

        let labor, machine =
            rs
            |> List.collect (fun r -> r.Steps |> List.map (stepCost r.Workflow))
            |> List.fold (fun (l, mc) (l', mc') -> (l + l', mc + mc')) (0.0<usd / txn>, 0.0<usd / txn>)

        let amortised =
            if e.AmortizationYears > 0.0 then
                at "redesign-one-time" e.RedesignOneTimeCost / e.AmortizationYears
            else
                at "redesign-one-time" e.RedesignOneTimeCost

        {
            LaborPerTxn = labor
            MachinePerTxn = machine
            RiskLossPerTxn =
                perTxn (
                    at "redesign-error-rate" e.RedesignErrorRate
                    * at "loss-per-error" e.LossPerError
                )
            FixedAnnual = at "redesign-fixed" e.RedesignFixedAnnual + amortised
            AnnualVolume = at "volume" e.AnnualVolume
        }

    let scenario m rs name (choose: string -> Point) = {
        Name = name
        Current = current m choose (rs |> List.map _.Workflow)
        Redesign = redesigned m choose rs
    }

    let private leverEvidence (m: EnterpriseModel) (rs: Redesign list) lever =
        let e = m.Economics
        let activities = rs |> List.collect (fun r -> r.Workflow.Activities)

        (match lever with
         | "volume" -> e.AnnualVolume.Evidence
         | "labor-rates" -> m.Roles |> List.collect _.LoadedCostPerHour.Evidence
         | "effort" -> activities |> List.collect _.Effort.Evidence
         | "exception-rates" -> activities |> List.collect _.ExceptionRate.Evidence
         | "assisted-effort-factor" -> e.AssistedEffortFactor.Evidence
         | "agent-escalation-rate" -> e.AgentEscalationRate.Evidence
         | "escalation-effort" -> e.EscalationEffort.Evidence
         | "inference-cost-agent" -> e.InferenceCostPerAgentStep.Evidence
         | "inference-cost-assisted" -> e.InferenceCostPerAssistedStep.Evidence
         | "deterministic-cost" -> e.DeterministicCostPerStep.Evidence
         | "current-fixed" -> e.CurrentFixedAnnual.Evidence
         | "redesign-fixed" -> e.RedesignFixedAnnual.Evidence
         | "redesign-one-time" -> e.RedesignOneTimeCost.Evidence
         | "current-error-rate" -> e.CurrentErrorRate.Evidence
         | "redesign-error-rate" -> e.RedesignErrorRate.Evidence
         | "loss-per-error" -> e.LossPerError.Evidence
         | _ -> [])
        |> List.distinct

    let private only lever point =
        fun name -> if name = lever then point else ExpectedPoint

    /// Break-even annual volume: where the redesign's extra fixed cost is
    /// repaid by lower variable cost. None when it is never reached.
    let breakEven (s: Scenario) =
        let fixedGap = s.Redesign.FixedAnnual - s.Current.FixedAnnual
        let variableGap = s.Current.VariablePerTxn - s.Redesign.VariablePerTxn

        if fixedGap <= 0.0<usd> && variableGap >= 0.0<usd / txn> then
            Some 0.0<txn>
        elif variableGap > 0.0<usd / txn> then
            Some(fixedGap / variableGap)
        else
            None

    let compare (m: EnterpriseModel) (rs: Redesign list) : EconomicComparison =
        let expected = scenario m rs "expected" (fun _ -> ExpectedPoint)
        let baseline = expected.AnnualSavings

        let sensitivities =
            levers
            |> List.map (fun lever ->
                let low = (scenario m rs lever (only lever LowPoint)).AnnualSavings
                let high = (scenario m rs lever (only lever HighPoint)).AnnualSavings
                let signOf (x: float<usd>) = sign (float x)

                {
                    Lever = lever
                    SavingsAtLow = low
                    SavingsAtHigh = high
                    FlipsConclusion = signOf low <> signOf baseline || signOf high <> signOf baseline
                    Basis = Evidence.basis (EnterpriseModel.tryEvidence m) (leverEvidence m rs lever)
                })

        let corner pickWorse =
            let points =
                sensitivities
                |> List.map (fun s ->
                    s.Lever,
                    (if pickWorse = (s.SavingsAtLow <= s.SavingsAtHigh) then
                         LowPoint
                     else
                         HighPoint))
                |> Map.ofList

            fun name -> points.TryFind name |> Option.defaultValue ExpectedPoint

        let adverse = scenario m rs "adverse" (corner true)
        let favourable = scenario m rs "favourable" (corner false)
        let flipping = sensitivities |> List.filter _.FlipsConclusion |> List.map _.Lever

        let verdict =
            if adverse.AnnualSavings > 0.0<usd> then
                RobustlySuperior
            elif favourable.AnnualSavings < 0.0<usd> then
                RobustlyInferior
            elif baseline > 0.0<usd> then
                SuperiorAtExpected flipping
            else
                InferiorAtExpected flipping

        let volumeCurve =
            [ 0.1; 0.25; 0.5; 1.0; 2.0; 5.0; 10.0 ]
            |> List.map (fun k ->
                let v = expected.Current.AnnualVolume * k

                let s = {
                    expected with
                        Current = {
                            expected.Current with
                                AnnualVolume = v
                        }
                        Redesign = {
                            expected.Redesign with
                                AnnualVolume = v
                        }
                }

                v, s.AnnualSavings)

        {
            Expected = expected
            Adverse = adverse
            Favourable = favourable
            Sensitivities = sensitivities
            VolumeCurve = volumeCurve
            BreakEvenVolume = breakEven expected
            Verdict = verdict
        }
