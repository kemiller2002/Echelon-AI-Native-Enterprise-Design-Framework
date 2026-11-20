module Echelon.Enterprise.Tests.ArchitectureTests

open Xunit
open Echelon.Enterprise
open Echelon.Enterprise.Tests.Fixtures

let private screened () =
    let benefit = constraintWith "C-BENEFIT" [ NoAiClearance ]
    let auth = constraintWith "C-AUTH" [ HumanAuthorization approver ]

    modelOf [ benefit; auth ] [ "D-DECISION" ] [ "D-REQUEST" ] [
        {
            act "A1" Verify (Human reviewer) [ "D-REQUEST" ] [ "D-SCREEN" ] [ "C-BENEFIT" ] with
                Profile = { judgment with ClearsRisk = true }
        }
        {
            act "A2" Produce (Human clerk) [ "D-REQUEST" ] [ "D-DRAFT" ] [] with
                Profile = judgment
        }
        act "A3" (Decide(DecisionId "DEC-T")) (Human approver) [ "D-SCREEN"; "D-DRAFT" ] [ "D-DECISION" ] [ "C-AUTH" ]
    ]

let private spec () =
    let m = screened ()
    Architecture.generate m (redesign m)

[<Fact>]
let ``the generated architecture satisfies its governance invariants`` () = Assert.Empty((spec ()).Violations)

[<Fact>]
let ``a screened step yields a flag-raising operation for the agent and a clearance operation for the human`` () =
    let s = spec ()
    let agentTools = s.Agents |> List.collect _.Tools
    Assert.Contains("API-A1-SCREEN", agentTools)
    Assert.DoesNotContain("API-A1", agentTools)
    Assert.Contains(s.Grants, fun g -> g.Api = "API-A1" && g.Principal = RolePrincipal reviewer && g.Right = Approve)

[<Fact>]
let ``no agent holds an operation reserved to human authority`` () =
    let s = spec ()

    let reserved =
        s.Apis
        |> List.filter (fun a -> a.RequiresHumanAuthority.IsSome)
        |> List.map _.Id
        |> Set.ofList

    Assert.NotEmpty reserved
    Assert.All(s.Agents, fun a -> Assert.True(a.Tools |> List.forall (reserved.Contains >> not)))

[<Fact>]
let ``only the named role may approve a human-authority operation`` () =
    let s = spec ()
    let grants = s.Grants |> List.filter (fun g -> g.Api = "API-A3")

    Assert.Equal<Grant list>(
        [
            {
                Principal = RolePrincipal approver
                Api = "API-A3"
                Right = Approve
            }
        ],
        grants
    )

[<Fact>]
let ``every operation traces to an outcome, current activities and evidence`` () =
    let s = spec ()
    Assert.Equal(s.Apis.Length, s.Trace.Length)

    Assert.All(
        s.Trace,
        fun t ->
            Assert.Equal(OutcomeId "OUT-T", t.Outcome)
            Assert.NotEmpty t.Activities
            Assert.NotEmpty t.Evidence
    )

[<Fact>]
let ``events connect producers to the consumers that need their data`` () =
    let s = spec ()
    let screen = s.Events |> List.find (fun e -> e.Data = DataId "D-SCREEN")
    Assert.Equal<ActivityId list>([ ActivityId "A3" ], screen.Consumers)

[<Fact>]
let ``an uncovered control obligation is reported as a violation`` () =
    let m = screened ()
    let orphan = constraintWith "C-ORPHAN" [ IndependentCheck ]
    let w = workflow m

    let m' = {
        m with
            Constraints = m.Constraints @ [ orphan ]
            Workflows = [
                {
                    w with
                        Governing = w.Governing @ [ orphan.Id ]
                }
            ]
    }

    let s = Architecture.generate m' (redesign m')
    Assert.Contains(s.Violations, fun v -> v.Contains "C-ORPHAN")

[<Fact>]
let ``agents are rolled out last, behind an evaluation gate`` () =
    let s = spec ()
    let last = List.last s.Roadmap
    Assert.Contains(ActivityId "A1", last.Steps)
    Assert.Contains("evaluation", last.Gate)
