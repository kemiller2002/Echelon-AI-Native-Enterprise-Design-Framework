module Echelon.Enterprise.Tests.ResponsibilityTests

open Xunit
open Echelon.Enterprise
open Echelon.Enterprise.Tests.Fixtures

let private assign nature profile obligations =
    let a = {
        act "A1" nature (Human clerk) [] [] [] with
            Profile = profile
    }

    Responsibility.assign {
        Activity = a
        Obligations = obligations
        Accountable = clerk
    }

[<Fact>]
let ``R1 human authority outranks everything, including deterministic rules`` () =
    let r = assign Produce routine [ HumanAuthorization approver ]
    Assert.Equal(AuthorizedHuman approver, r.Executor)
    Assert.StartsWith("R1", r.DecidingRule)

[<Fact>]
let ``R2 an independence obligation on a check yields independent verification`` () =
    Assert.Equal(IndependentVerification, (assign Verify routine [ IndependentCheck ]).Executor)

[<Fact>]
let ``R3 rule-specifiable structured work is deterministic software`` () =
    Assert.Equal(DeterministicSoftware, (assign Produce routine []).Executor)

[<Fact>]
let ``R4a bounded clearance work is screened by an agent and cleared by a human`` () =
    let p = { judgment with ClearsRisk = true }
    Assert.Equal(AgentScreenedHumanClearance clerk, (assign Verify p [ NoAiClearance ]).Executor)

[<Fact>]
let ``R4b irreversible clearance work keeps a human on every case`` () =
    let p = {
        judgment with
            ClearsRisk = true
            Reversibility = Irreversible
    }

    Assert.Equal(AiAssistedHuman clerk, (assign Verify p [ NoAiClearance ]).Executor)

[<Fact>]
let ``R5 major exposure keeps a human actor`` () =
    let p = {
        judgment with
            FinancialExposure = Major
    }

    Assert.Equal(AiAssistedHuman clerk, (assign Produce p []).Executor)

[<Fact>]
let ``R6 accountable judgment keeps a human actor`` () =
    let p = {
        judgment with
            ExplainabilityRequired = true
            FinancialExposure = Moderate
    }

    Assert.Equal(AiAssistedHuman clerk, (assign Produce p []).Executor)

[<Fact>]
let ``R7 bounded judgment work goes to a supervised agent escalating to the accountable role`` () =
    Assert.Equal(SupervisedAgent clerk, (assign Produce judgment []).Executor)

[<Fact>]
let ``the trace records every rule evaluated up to the deciding one`` () =
    let r = assign Produce judgment []
    Assert.Equal(8, r.Trace.Length)
    Assert.StartsWith("applied: R7", List.last r.Trace)

[<Fact>]
let ``external and software performers are accountable to the workflow owner`` () =
    let m =
        modelOf [] [ "D-OUT" ] [ "D-IN" ] [ act "A1" Produce (Software "erp") [ "D-IN" ] [ "D-OUT" ] [] ]

    let w = workflow m
    Assert.Equal(w.Owner, Responsibility.accountableFor w (List.head w.Activities))
