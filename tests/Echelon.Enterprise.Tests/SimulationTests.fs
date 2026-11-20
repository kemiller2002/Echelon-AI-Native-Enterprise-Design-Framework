module Echelon.Enterprise.Tests.SimulationTests

open Xunit
open Echelon.Enterprise
open Echelon.Enterprise.Tests.Fixtures

let private config seed = {
    Seed = seed
    Transactions = 200
    MeanInterArrival = 30.0<minute>
    Capacity = Map.empty
    DefaultCapacity = 2
}

let private tasks =
    let m =
        modelOf [] [ "D-OUT" ] [ "D-IN" ] [
            {
                act "A1" Produce (Human clerk) [ "D-IN" ] [ "D-MID" ] [] with
                    Effort = est 5.0<minute> 10.0<minute> 20.0<minute>
            }
            {
                act "A2" Produce (Human reviewer) [ "D-MID" ] [ "D-OUT" ] [] with
                    Effort = est 5.0<minute> 10.0<minute> 20.0<minute>
            }
        ]

    Simulation.ofWorkflow (workflow m)

[<Fact>]
let ``the same seed gives the same result`` () =
    Assert.Equal(Simulation.run "a" (config 7UL) tasks, Simulation.run "a" (config 7UL) tasks)

[<Fact>]
let ``a different seed gives a different sample`` () =
    Assert.NotEqual(
        (Simulation.run "a" (config 7UL) tasks).MeanCycle,
        (Simulation.run "a" (config 8UL) tasks).MeanCycle
    )

[<Fact>]
let ``the generator stays within the unit interval`` () =
    let rec draws n state acc =
        if n = 0 then
            acc
        else
            let u, s = Simulation.next state
            draws (n - 1) s (u :: acc)

    Assert.All(draws 10000 42UL [], fun u -> Assert.InRange(u, 0.0, 0.9999999999))

[<Fact>]
let ``triangular samples stay within the estimate`` () =
    let e = est 2.0<minute> 5.0<minute> 11.0<minute>

    for u in [ 0.0; 0.1; 0.33; 0.5; 0.9; 0.999 ] do
        Assert.InRange(float (Simulation.triangular e u), 2.0, 11.0)

[<Fact>]
let ``cycle time is at least the service and delay on the critical path`` () =
    let r = Simulation.run "a" (config 1UL) tasks
    // Each task has a 60 minute delay and at least 5 minutes of service.
    Assert.True(r.P50Cycle >= 130.0<minute>)

[<Fact>]
let ``scarce capacity creates a bottleneck and longer cycles`` () =
    let busy = {
        config 3UL with
            MeanInterArrival = 6.0<minute>
            DefaultCapacity = 1
    }

    let ample = { busy with DefaultCapacity = 10 }
    let scarce = Simulation.run "scarce" busy tasks
    let plenty = Simulation.run "ample" ample tasks
    Assert.True(scarce.MeanCycle > plenty.MeanCycle)
    Assert.True(snd scarce.Bottleneck.Value > snd plenty.Bottleneck.Value)
