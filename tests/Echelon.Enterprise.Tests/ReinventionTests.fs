module Echelon.Enterprise.Tests.ReinventionTests

open Xunit
open Echelon.Enterprise
open Echelon.Enterprise.Tests.Fixtures

/// intake (clerk) → email handoff → queue → review (reviewer) → status email → approve
let private wasteful () =
    let control = constraintWith "C-APPROVAL" [ HumanAuthorization approver ]

    modelOf [ control ] [ "D-DECISION" ] [ "D-REQUEST" ] [
        act "A1" Produce (Human clerk) [ "D-REQUEST" ] [ "D-CASE" ] []
        act "A2" Transfer (Human clerk) [ "D-CASE" ] [ "D-CASE" ] []
        act "A3" Wait (Human reviewer) [] [] []
        {
            act "A4" Produce (Human reviewer) [ "D-CASE" ] [ "D-ASSESSMENT" ] [] with
                Profile = judgment
        }
        act "A5" Notify (Software "mail") [ "D-ASSESSMENT" ] [ "D-STATUS" ] []
        act "A6" Produce (Human clerk) [ "D-CASE" ] [ "D-SPREADSHEET" ] []
        act "A7" (Decide(DecisionId "DEC-T")) (Human approver) [ "D-ASSESSMENT" ] [ "D-DECISION" ] [ "C-APPROVAL" ]
    ]

[<Fact>]
let ``handoffs and internal queues are replaced by events`` () =
    let r = redesign (wasteful ())
    Assert.Equal("replaced-by-event", dispositionOf r "A2")
    Assert.Equal("replaced-by-event", dispositionOf r "A3")

[<Fact>]
let ``notifications that are not required information become events`` () =
    Assert.Equal("replaced-by-event", dispositionOf (redesign (wasteful ())) "A5")

[<Fact>]
let ``work that produces nothing required is eliminated`` () =
    Assert.Equal("eliminated", dispositionOf (redesign (wasteful ())) "A6")

[<Fact>]
let ``work that the outcome needs is retained`` () =
    let r = redesign (wasteful ())

    for id in [ "A1"; "A4"; "A7" ] do
        Assert.Equal("retained", dispositionOf r id)

[<Fact>]
let ``necessary data is the backward closure from the outcome`` () =
    let needed =
        Reinvention.necessaryData (wasteful ()) (workflow (wasteful ()))
        |> Set.map _.Value

    Assert.Equal<Set<string>>(set [ "D-DECISION"; "D-ASSESSMENT"; "D-CASE"; "D-REQUEST" ], needed)

[<Fact>]
let ``human authority survives redesign`` () =
    Assert.Equal(Some(AuthorizedHuman approver), executorOf (redesign (wasteful ())) "A7")

[<Fact>]
let ``a structural redesign is classified as reinvention`` () =
    let r = redesign (wasteful ())
    Assert.Equal(Reinvention, r.Classification)
    Assert.True(r.After.HumanHandoffs < r.Before.HumanHandoffs)

[<Fact>]
let ``duplicate checks of the same control are merged`` () =
    let c = constraintWith "C-CHECK" [ RequiresInformation(DataId "D-IN") ]

    let m =
        modelOf [ c ] [ "D-OUT" ] [ "D-IN" ] [
            act "A1" Verify (Human clerk) [ "D-IN" ] [ "D-CHECK1" ] [ "C-CHECK" ]
            act "A2" Verify (Human reviewer) [ "D-IN" ] [ "D-CHECK2" ] [ "C-CHECK" ]
            act "A3" Produce (Human clerk) [ "D-CHECK1"; "D-CHECK2" ] [ "D-OUT" ] []
        ]

    let r = redesign m
    Assert.Equal("merged", dispositionOf r "A2")
    Assert.Contains(ActivityId "A2", (stepOf r "A1").Absorbed)

[<Fact>]
let ``checks under an independence obligation are not merged`` () =
    let c = constraintWith "C-SOD" [ IndependentCheck ]

    let m =
        modelOf [ c ] [ "D-OUT" ] [ "D-IN" ] [
            act "A1" Verify (Human clerk) [ "D-IN" ] [ "D-CHECK1" ] [ "C-SOD" ]
            act "A2" Verify (Human reviewer) [ "D-IN" ] [ "D-CHECK2" ] [ "C-SOD" ]
            act "A3" Produce (Human clerk) [ "D-CHECK1"; "D-CHECK2" ] [ "D-OUT" ] []
        ]

    Assert.Equal("retained", dispositionOf (redesign m) "A2")

[<Fact>]
let ``external latency is retained`` () =
    let m =
        modelOf [] [ "D-OUT" ] [ "D-IN" ] [
            act "A1" Produce (Human clerk) [ "D-IN" ] [ "D-MID" ] []
            act "A2" Wait (ExternalParty "bank") [] [] []
            act "A3" Produce (Human clerk) [ "D-MID" ] [ "D-OUT" ] []
        ]

    let r = redesign m
    Assert.Equal("retained", dispositionOf r "A2")
    // The retained wait is sequenced between its neighbours.
    Assert.Contains(ActivityId "A1", (stepOf r "A2").DependsOn)
    Assert.Contains(ActivityId "A2", (stepOf r "A3").DependsOn)

[<Fact>]
let ``independent work is parallelised by data dependencies`` () =
    let m =
        modelOf [] [ "D-OUT" ] [ "D-IN" ] [
            act "A1" Produce (Human clerk) [ "D-IN" ] [ "D-X" ] []
            act "A2" Produce (Human clerk) [ "D-IN" ] [ "D-Y" ] []
            act "A3" Produce (Human clerk) [ "D-X"; "D-Y" ] [ "D-OUT" ] []
        ]

    let r = redesign m
    Assert.Equal(1, (stepOf r "A1").Stage)
    Assert.Equal(1, (stepOf r "A2").Stage)
    Assert.Equal(2, r.After.Stages)

[<Fact>]
let ``same structure with new executors is superficial automation`` () =
    let m =
        modelOf [] [ "D-OUT" ] [ "D-IN" ] [ act "A1" Produce (Human clerk) [ "D-IN" ] [ "D-OUT" ] [] ]

    Assert.Equal(SuperficialAutomation, (redesign m).Classification)

[<Fact>]
let ``software that stays deterministic is no change`` () =
    let m =
        modelOf [] [ "D-OUT" ] [ "D-IN" ] [ act "A1" Produce (Software "erp") [ "D-IN" ] [ "D-OUT" ] [] ]

    Assert.Equal(NoChange, (redesign m).Classification)

[<Fact>]
let ``a workflow whose outcome is already available is redundant as a whole`` () =
    let m =
        modelOf [] [ "D-IN" ] [ "D-IN" ] [
            act "A1" Transfer (Human clerk) [ "D-IN" ] [ "D-IN" ] []
            act "A2" Verify (Human reviewer) [ "D-IN" ] [ "D-CHECKED" ] []
        ]

    let r = redesign m
    Assert.Equal(WorkflowRedundant, r.Classification)
    Assert.Empty r.Steps

[<Fact>]
let ``a governing human authorisation prevents whole-workflow elimination`` () =
    let c = constraintWith "C-AUTH" [ HumanAuthorization approver ]

    let m =
        modelOf [ c ] [ "D-IN" ] [ "D-IN" ] [
            act "A1" (Decide(DecisionId "DEC-T")) (Human approver) [ "D-IN" ] [ "D-OK" ] [ "C-AUTH" ]
        ]

    Assert.NotEqual(WorkflowRedundant, (redesign m).Classification)
