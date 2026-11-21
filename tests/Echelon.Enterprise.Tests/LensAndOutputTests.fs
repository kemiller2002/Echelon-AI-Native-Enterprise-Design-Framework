module Echelon.Enterprise.Tests.LensAndOutputTests

open System.Text.Json
open Xunit
open Echelon.Enterprise
open Echelon.Enterprise.Tests.Fixtures

let private sim = {
    Seed = 1UL
    Transactions = 50
    MeanInterArrival = 30.0<minute>
    Capacity = Map.empty
    DefaultCapacity = 3
}

let private report () =
    let m =
        modelOf [] [ "D-OUT" ] [ "D-IN" ] [
            {
                act "A1" Produce (Human clerk) [ "D-IN" ] [ "D-MID" ] [] with
                    Profile = judgment
            }
            act "A2" Transfer (Human clerk) [ "D-MID" ] [ "D-MID" ] []
            act "A3" Produce (Human reviewer) [ "D-MID" ] [ "D-OUT" ] []
        ]

    match Analysis.run sim m with
    | Ok r -> r
    | Error ds -> failwithf "unexpected diagnostics %A" ds

[<Fact>]
let ``lens reports describe what they do not do`` () =
    let w = (report ()).Workflows |> List.head
    Assert.Contains("Does not perform", w.Clarity.Method)
    Assert.Contains("Does not perform", w.Edf.Method)
    Assert.NotEmpty w.Clarity.Limitations
    Assert.NotEmpty w.Edf.Limitations

[<Fact>]
let ``the clarity brief surfaces every non-retained disposition as a hypothesis`` () =
    let w = (report ()).Workflows |> List.head

    let hypotheses =
        w.Clarity.Findings
        |> List.filter (fun f -> f.Severity = "hypothesis")
        |> List.map _.Subject

    Assert.Contains("A2", hypotheses)

[<Fact>]
let ``disagreements between economics and lenses are preserved, not resolved`` () =
    let r = report ()
    let w = r.Workflows |> List.head

    let defer = {
        w.Clarity with
            Stance = Defer
            Recommendation = "Wait: test"
    }

    let favourable = {
        r.Economics with
            Verdict = RobustlySuperior
    }

    let ds = Lenses.disagreements favourable defer w.Edf
    let adoption = ds |> List.find (fun d -> d.Topic = "adoption")
    Assert.Equal(2, adoption.Positions.Length)

[<Fact>]
let ``the JSON IR is valid JSON carrying the schema identifier`` () =
    use doc = JsonDocument.Parse(Json.serialize (report ()))
    Assert.Equal(Json.schema, doc.RootElement.GetProperty("schema").GetString())
    Assert.Equal(1, doc.RootElement.GetProperty("workflows").GetArrayLength())

[<Fact>]
let ``the markdown report contains every architecture section`` () =
    let md = Markdown.render (report ())

    for section in
        [
            "Capability map"
            "API catalog"
            "Event catalog"
            "Agent catalog"
            "Authorization model"
            "Human decision matrix"
            "Control coverage"
            "Implementation roadmap"
            "Traceability"
            "Evidence register"
        ] do
        Assert.Contains(section, md)

[<Fact>]
let ``evidence exports as knowledge-store records with front matter`` () =
    let path, text = Knowledge.record evidence
    Assert.Equal("evidence/EV-T-001.md", path)
    Assert.StartsWith("---\nid: EV-T-001\n", text)
    Assert.Contains("confidence: low", text)
