namespace Echelon.Enterprise.CaseStudies

open Echelon.Enterprise

/// Registered case studies, keyed by a stable slug used for output folders.
module Catalog =

    let defaultSimulation: SimulationConfig = {
        Seed = 20261120UL
        Transactions = 2000
        // About 40,000 grants a year over ~250 working days of 480 minutes.
        MeanInterArrival = 3.0<minute>
        // Illustrative headcount for the composite sponsor (EV-DAF-013).
        Capacity =
            Map.ofList [
                Daf.associate, 5
                Daf.analyst, 7
                Daf.compliance, 3
                Daf.officer, 1
                Daf.treasury, 4
                Daf.accountant, 2
            ]
        DefaultCapacity = 2
    }

    let all: (string * EnterpriseModel * SimulationConfig) list = [ "daf", Daf.model, defaultSimulation ]
