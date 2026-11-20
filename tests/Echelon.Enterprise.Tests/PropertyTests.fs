/// Property tests over randomly generated workflows, using the framework's
/// own seeded generator so failures are reproducible from the seed alone.
module Echelon.Enterprise.Tests.PropertyTests

open Xunit
open Echelon.Enterprise
open Echelon.Enterprise.Tests.Fixtures

type private Gen<'a> = uint64 -> 'a * uint64

let private pick (xs: 'a list) : Gen<'a> =
    fun s ->
        let u, s = Simulation.next s
        xs[min (xs.Length - 1) (int (u * float xs.Length))], s

let private chance p : Gen<bool> =
    fun s ->
        let u, s = Simulation.next s
        u < p, s

let private constraints = [
    constraintWith "C-AUTH" [ HumanAuthorization approver ]
    constraintWith "C-SOD" [ IndependentCheck ]
    constraintWith "C-NAC" [ NoAiClearance ]
    constraintWith "C-REC" [ RequiresInformation(DataId "D-IN0") ]
]

let private profile: Gen<TaskProfile> =
    fun s ->
        let spec, s = chance 0.5 s
        let structure, s = pick [ Structured; SemiStructured; Unstructured ] s
        let judgment, s = pick [ Negligible; Minor; Moderate; Major ] s
        let rev, s = pick [ Reversible; CostlyToReverse; Irreversible ] s
        let exposure, s = pick [ Negligible; Minor; Moderate; Major ] s
        let explain, s = chance 0.5 s
        let clears, s = chance 0.3 s

        {
            RuleSpecifiable = spec
            InputStructure = structure
            Judgment = judgment
            Reversibility = rev
            FinancialExposure = exposure
            ExplainabilityRequired = explain
            ClearsRisk = clears
        },
        s

/// A random but well-formed model: inputs are always available when used.
let private generate (seed: uint64) =
    let n, s = pick [ 3..10 ] seed

    let activities, _, _ =
        [ 1..n ]
        |> List.fold
            (fun (acc, available: string list, s) i ->
                let nature, s =
                    pick [ Produce; Verify; Transfer; Wait; Notify; Rework; Decide(DecisionId "DEC-G") ] s

                let performer, s =
                    pick [ Human clerk; Human reviewer; Software "sys"; ExternalParty "partner" ] s

                let input, s = pick available s
                let second, s = pick available s
                let controlled, s = chance 0.3 s
                let c, s = pick constraints s
                let p, s = profile s
                let id = $"A{i}"

                let needsIn0 = controlled && c.Id = ConstraintId "C-REC"

                let inputs, outputs =
                    match nature with
                    | Wait when needsIn0 -> [ "D-IN0" ], []
                    | Wait -> [], []
                    | Transfer -> [ input ], [ input ]
                    | _ -> List.distinct [ input; second ], [ $"D-{id}" ]

                let inputs =
                    if needsIn0 then
                        List.distinct ("D-IN0" :: inputs)
                    else
                        inputs

                let a = {
                    act id nature performer inputs outputs (if controlled then [ c.Id.Value ] else []) with
                        Profile = p
                }

                (acc @ [ a ], available @ outputs, s))
            ([], [ "D-IN0"; "D-IN1" ], s)

    let produced = activities |> List.collect _.Outputs |> List.map _.Value
    let required = produced |> List.tryLast |> Option.defaultValue "D-IN0"
    let used = activities |> List.collect _.Satisfies |> Set.ofList
    let governing = constraints |> List.filter (fun c -> used.Contains c.Id)
    let m = modelOf governing [ required ] [ "D-IN0"; "D-IN1" ] activities

    {
        m with
            Decisions = [
                {
                    Id = DecisionId "DEC-G"
                    Question = "generated"
                    Inputs = []
                    Output = DataId "D-IN0"
                    Evidence = [ ev ]
                }
            ]
    }

let private seeds = [ 1UL .. 300UL ] |> List.map (fun i -> i * 7919UL)

let private validModels =
    seeds
    |> List.map (fun seed -> seed, generate seed)
    |> List.filter (fun (_, m) -> Validation.errors (Validation.validate m) |> List.isEmpty)

let private forAll (property: EnterpriseModel -> Redesign -> bool) =
    let failures =
        validModels
        |> List.filter (fun (_, m) -> not (property m (redesign m)))
        |> List.map fst

    Assert.True(failures.IsEmpty, $"property failed for seeds %A{failures}")

[<Fact>]
let ``the generator produces mostly valid models`` () =
    Assert.True(validModels.Length >= 250, $"only {validModels.Length} of 300 generated models were valid")

[<Fact>]
let ``reinvention never drops a governing control`` () =
    forAll (fun m r ->
        let before = (workflow m).Activities |> List.collect _.Satisfies |> Set.ofList
        let after = r.Steps |> List.collect _.Activity.Satisfies |> Set.ofList
        Set.isSubset (Set.intersect before (Set.ofList (workflow m).Governing)) after)

[<Fact>]
let ``work under a human-authority obligation always stays with that human`` () =
    forAll (fun m r ->
        r.Steps
        |> List.forall (fun s ->
            let obligations = Responsibility.obligationsFor m s.Activity

            match
                obligations
                |> List.tryPick (function
                    | HumanAuthorization role -> Some role
                    | _ -> None),
                s.Assignment
            with
            | Some role, Some a -> a.Executor = AuthorizedHuman role
            | _ -> true))

[<Fact>]
let ``AI never clears a concern it is forbidden to clear`` () =
    forAll (fun m r ->
        r.Steps
        |> List.forall (fun s ->
            let forbidden =
                s.Activity.Profile.ClearsRisk
                && Responsibility.obligationsFor m s.Activity |> List.contains NoAiClearance

            match forbidden, s.Assignment |> Option.map _.Executor with
            | true, Some(SupervisedAgent _) -> false
            | _ -> true))

[<Fact>]
let ``redesigned steps are ordered: every dependency is an earlier stage`` () =
    forAll (fun _ r ->
        let stage = r.Steps |> List.map (fun s -> s.Activity.Id, s.Stage) |> Map.ofList

        r.Steps
        |> List.forall (fun s -> s.DependsOn |> List.forall (fun d -> stage.ContainsKey d && stage[d] < s.Stage)))

[<Fact>]
let ``every input of a redesigned step remains available`` () =
    forAll (fun m r ->
        let w = workflow m

        let available =
            Set.ofList w.Inputs
            + (r.Steps |> List.collect _.Activity.Outputs |> Set.ofList)
            + (r.Dispositions
               |> List.filter (fun (a, d) ->
                   a.Nature = Transfer && Reinvention.dispositionLabel d = "replaced-by-event")
               |> List.collect (fun (a, _) -> a.Outputs)
               |> Set.ofList)

        r.Steps
        |> List.forall (fun s -> s.Activity.Inputs |> List.forall available.Contains))

[<Fact>]
let ``reinvention is deterministic`` () = forAll (fun m r -> redesign m = r)

[<Fact>]
let ``generated architectures never violate governance invariants`` () =
    forAll (fun m r -> (Architecture.generate m r).Violations.IsEmpty)

[<Fact>]
let ``adverse economics never beat expected economics`` () =
    forAll (fun m r ->
        let e = Economics.compare m [ r ]

        e.Adverse.AnnualSavings <= e.Expected.AnnualSavings + 0.01<usd>
        && e.Expected.AnnualSavings <= e.Favourable.AnnualSavings + 0.01<usd>)
