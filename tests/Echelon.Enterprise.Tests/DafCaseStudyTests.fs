/// Vertical-slice acceptance tests for Case Study A. These pin the
/// framework's conclusions about the composite DAF workflow; a change that
/// moves them must be deliberate and explained.
module Echelon.Enterprise.Tests.DafCaseStudyTests

open Xunit
open Echelon.Enterprise
open Echelon.Enterprise.CaseStudies

let private report =
    lazy
        (match Analysis.run (Catalog.all |> List.head |> fun (_, _, s) -> s) Daf.model with
         | Ok r -> r
         | Error ds -> failwithf "DAF model invalid: %A" ds)

let private wf () = report.Value.Workflows |> List.head

let private executor id =
    (wf ()).Redesign.Steps
    |> List.find (fun s -> s.Activity.Id = ActivityId id)
    |> fun s -> s.Assignment |> Option.map _.Executor

let private disposition id =
    (wf ()).Redesign.Dispositions
    |> List.find (fun (a, _) -> a.Id = ActivityId id)
    |> snd
    |> Reinvention.dispositionLabel

[<Fact>]
let ``the DAF model validates without errors`` () =
    Assert.Empty(Validation.errors (Validation.validate Daf.model))

[<Fact>]
let ``the conflicting approval-cadence evidence is preserved`` () =
    Assert.Contains(report.Value.Diagnostics, fun d -> d.Code = "EEF010" && d.Subject = "EV-DAF-007")

[<Fact>]
let ``the redesign is a reinvention, not superficial automation`` () =
    Assert.Equal(Reinvention, (wf ()).Redesign.Classification)

[<Fact>]
let ``re-keying, internal queues and status emails are removed`` () =
    for id in [ "A01"; "A04"; "A07"; "A09"; "A12"; "A17" ] do
        Assert.Equal("replaced-by-event", disposition id)

[<Fact>]
let ``compliance re-verification is merged into the eligibility check`` () =
    Assert.Equal("merged", disposition "A08")

[<Fact>]
let ``external settlement and deposit latency is retained`` () =
    Assert.Equal("retained", disposition "A14")
    Assert.Equal("retained", disposition "A20")

[<Fact>]
let ``AI may add a flag but never clear one on purpose and benefit review`` () =
    Assert.Equal(Some(AgentScreenedHumanClearance Daf.analyst), executor "A06")

[<Fact>]
let ``large grants keep personal officer authorisation`` () =
    Assert.Equal(Some(AuthorizedHuman Daf.officer), executor "A11")

[<Fact>]
let ``payee verification stays independent of the preparer`` () =
    Assert.Equal(Some IndependentVerification, executor "A16")

[<Fact>]
let ``the generated architecture holds its governance invariants`` () =
    Assert.Empty((wf ()).Architecture.Violations)

[<Fact>]
let ``economics favour the redesign only at expected values`` () =
    // Inputs are illustrative; the framework must not claim robustness.
    match report.Value.Economics.Verdict with
    | SuperiorAtExpected _ -> Assert.True(report.Value.Economics.Adverse.AnnualSavings < 0.0<usd>)
    | other -> failwithf "unexpected verdict %A" other

[<Fact>]
let ``the clarity brief defers and the disagreement with economics is preserved`` () =
    let w = wf ()
    Assert.Equal(Defer, w.Clarity.Stance)
    Assert.Contains(w.Disagreements, fun d -> d.Topic = "adoption")

[<Fact>]
let ``external latency limits the cycle-time gain`` () =
    let w = wf ()
    Assert.True(w.RedesignSimulation.MeanCycle < w.CurrentSimulation.MeanCycle)
    // More than half of the current cycle remains: the waits are outside the sponsor's control.
    Assert.True(w.RedesignSimulation.MeanCycle > 0.5 * w.CurrentSimulation.MeanCycle)
