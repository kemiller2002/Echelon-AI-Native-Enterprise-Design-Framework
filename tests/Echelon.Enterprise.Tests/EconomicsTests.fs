module Echelon.Enterprise.Tests.EconomicsTests

open Xunit
open Echelon.Enterprise
open Echelon.Enterprise.Tests.Fixtures

let private heavyManual () =
    modelOf [] [ "D-OUT" ] [ "D-IN" ] [
        {
            act "A1" Produce (Human clerk) [ "D-IN" ] [ "D-MID" ] [] with
                Effort = est 20.0<minute> 30.0<minute> 40.0<minute>
        }
        act "A2" Transfer (Human clerk) [ "D-MID" ] [ "D-MID" ] []
        {
            act "A3" Produce (Human clerk) [ "D-MID" ] [ "D-OUT" ] [] with
                Effort = est 20.0<minute> 30.0<minute> 40.0<minute>
        }
    ]

[<Fact>]
let ``removing heavy manual work at volume is robustly superior`` () =
    let m = heavyManual ()

    let m = {
        m with
            Economics = {
                m.Economics with
                    AnnualVolume = est 10000.0<txn> 15000.0<txn> 20000.0<txn>
            }
    }

    Assert.Equal(RobustlySuperior, (Economics.compare m [ redesign m ]).Verdict)

[<Fact>]
let ``the same redesign at low volume is not robust: fixed costs dominate`` () =
    let m = heavyManual ()
    Assert.NotEqual(RobustlySuperior, (Economics.compare m [ redesign m ]).Verdict)

[<Fact>]
let ``the engine can conclude a redesign is inferior`` () =
    let m =
        modelOf [] [ "D-OUT" ] [ "D-IN" ] [ act "A1" Produce (Software "erp") [ "D-IN" ] [ "D-OUT" ] [] ]

    let costly = {
        m with
            Economics = {
                m.Economics with
                    RedesignFixedAnnual = est 500000.0<usd> 600000.0<usd> 700000.0<usd>
            }
    }

    Assert.Equal(RobustlyInferior, (Economics.compare costly [ redesign costly ]).Verdict)

[<Fact>]
let ``labour cost uses loaded rates per minute`` () =
    let m =
        modelOf [] [ "D-OUT" ] [ "D-IN" ] [ act "A1" Produce (Human clerk) [ "D-IN" ] [ "D-OUT" ] [] ]

    let c = Economics.current m (fun _ -> ExpectedPoint) m.Workflows
    // 10 minutes at 60 USD/hour = 10 USD.
    Assert.Equal(10.0, float c.LaborPerTxn, 6)

[<Fact>]
let ``break-even volume is the fixed-cost gap over the variable-cost gap`` () =
    let cost labor fixedAnnual = {
        LaborPerTxn = labor
        MachinePerTxn = 0.0<usd / txn>
        RiskLossPerTxn = 0.0<usd / txn>
        FixedAnnual = fixedAnnual
        AnnualVolume = 1000.0<txn>
    }

    let s = {
        Name = "t"
        Current = cost 10.0<usd / txn> 0.0<usd>
        Redesign = cost 2.0<usd / txn> 8000.0<usd>
    }

    Assert.Equal(Some 1000.0<txn>, Economics.breakEven s)

[<Fact>]
let ``break-even is never reached when the redesign costs more per transaction`` () =
    let cost labor = {
        LaborPerTxn = labor
        MachinePerTxn = 0.0<usd / txn>
        RiskLossPerTxn = 0.0<usd / txn>
        FixedAnnual = 100.0<usd>
        AnnualVolume = 1.0<txn>
    }

    Assert.Equal(
        None,
        Economics.breakEven {
            Name = "t"
            Current = cost 1.0<usd / txn>
            Redesign = cost 2.0<usd / txn>
        }
    )

[<Fact>]
let ``sensitivity covers every lever and carries the evidence basis`` () =
    let m = heavyManual ()
    let e = Economics.compare m [ redesign m ]
    Assert.Equal(Economics.levers.Length, e.Sensitivities.Length)
    Assert.All(e.Sensitivities, fun s -> Assert.Equal(Evidence.IllustrativeOnly, s.Basis))

[<Fact>]
let ``adverse savings never exceed expected, nor expected exceed favourable`` () =
    let m = heavyManual ()
    let e = Economics.compare m [ redesign m ]
    Assert.True(e.Adverse.AnnualSavings <= e.Expected.AnnualSavings)
    Assert.True(e.Expected.AnnualSavings <= e.Favourable.AnnualSavings)
