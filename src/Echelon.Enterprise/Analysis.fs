namespace Echelon.Enterprise

type WorkflowAnalysis = {
    Redesign: Redesign
    Architecture: ArchitectureSpec
    Clarity: LensReport
    Edf: LensReport
    Disagreements: Disagreement list
    CurrentSimulation: SimulationResult
    RedesignSimulation: SimulationResult
}

type AnalysisReport = {
    Model: EnterpriseModel
    Diagnostics: Validation.Diagnostic list
    Economics: EconomicComparison
    Workflows: WorkflowAnalysis list
}

/// The end-to-end pipeline: validate → reinvent → price → generate →
/// lens review → simulate. Pure; I/O lives in the CLI.
module Analysis =

    let private analyseWorkflow (m: EnterpriseModel) (economics: EconomicComparison) (sim: SimulationConfig) r =
        let clarity = Lenses.clarityBrief m r economics
        let edf = Lenses.edfPacket m r

        {
            Redesign = r
            Architecture = Architecture.generate m r
            Clarity = clarity
            Edf = edf
            Disagreements = Lenses.disagreements economics clarity edf
            CurrentSimulation = Simulation.run "current" sim (Simulation.ofWorkflow r.Workflow)
            RedesignSimulation = Simulation.run "redesign" sim (Simulation.ofRedesign m.Economics r)
        }

    let run (sim: SimulationConfig) (m: EnterpriseModel) : Result<AnalysisReport, Validation.Diagnostic list> =
        Validation.check m
        |> Result.map (fun (model, diagnostics) ->
            let redesigns = model.Workflows |> List.map (Reinvention.reinvent model)
            let economics = Economics.compare model redesigns

            {
                Model = model
                Diagnostics = diagnostics
                Economics = economics
                Workflows = redesigns |> List.map (analyseWorkflow model economics sim)
            })
