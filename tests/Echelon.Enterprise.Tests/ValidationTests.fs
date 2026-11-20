module Echelon.Enterprise.Tests.ValidationTests

open Xunit
open Echelon.Enterprise
open Echelon.Enterprise.Tests.Fixtures

let private simple () =
    modelOf [] [ "D-OUT" ] [ "D-IN" ] [ act "A1" Produce (Human clerk) [ "D-IN" ] [ "D-OUT" ] [] ]

[<Fact>]
let ``a well-formed model has no errors`` () =
    Assert.Empty(Validation.errors (Validation.validate (simple ())))

[<Fact>]
let ``unresolved references are errors`` () =
    let m = simple ()
    let w = workflow m

    let broken = {
        m with
            Workflows = [
                {
                    w with
                        Governing = [ ConstraintId "C-MISSING" ]
                }
            ]
    }

    Assert.Contains("EEF002", codes broken)

[<Fact>]
let ``duplicate identifiers are errors`` () =
    let m = simple ()

    Assert.Contains(
        "EEF001",
        codes {
            m with
                Evidence = m.Evidence @ m.Evidence
        }
    )

[<Fact>]
let ``a fact without a fact-capable source is an error`` () =
    let m = simple ()
    let e = { evidence with Status = Fact }
    Assert.Contains("EEF005", codes { m with Evidence = [ e ] })

[<Fact>]
let ``illustrative values cannot be presented as claims`` () =
    let m = simple ()
    let e = { evidence with Status = Claim }
    Assert.Contains("EEF006", codes { m with Evidence = [ e ] })

[<Fact>]
let ``an input that is never available is an error`` () =
    let m =
        modelOf [] [ "D-OUT" ] [] [ act "A1" Produce (Human clerk) [ "D-GHOST" ] [ "D-OUT" ] [] ]

    Assert.Contains("EEF008", codes m)

[<Fact>]
let ``an outcome whose information is never produced is an error`` () =
    let m =
        modelOf [] [ "D-NEVER" ] [ "D-IN" ] [ act "A1" Produce (Human clerk) [ "D-IN" ] [ "D-OUT" ] [] ]

    Assert.Contains("EEF009", codes m)

[<Fact>]
let ``an unordered estimate is an error and an unsupported one a warning`` () =
    let m = simple ()
    let w = workflow m
    let a = List.head w.Activities

    let bad = {
        a with
            Effort = Estimate.create 10.0<minute> 5.0<minute> 20.0<minute> []
    }

    let ds =
        Validation.validate {
            m with
                Workflows = [ { w with Activities = [ bad ] } ]
        }

    Assert.Contains(ds, fun d -> d.Code = "EEF003" && d.Severity = Validation.Error)
    Assert.Contains(ds, fun d -> d.Code = "EEF004" && d.Severity = Validation.Warning)

[<Fact>]
let ``rates outside zero to one are errors`` () =
    let m = simple ()
    let w = workflow m
    let a = List.head w.Activities

    let bad = {
        a with
            ExceptionRate = est 0.5 1.0 1.5
    }

    Assert.Contains(
        "EEF013",
        codes {
            m with
                Workflows = [ { w with Activities = [ bad ] } ]
        }
    )

[<Fact>]
let ``contradictions are preserved as advisories, not errors`` () =
    let m = simple ()

    let other = {
        evidence with
            Id = EvidenceId "EV-T-002"
    }

    let conflicting = {
        evidence with
            Contradicts = [ other.Id ]
    }

    let ds =
        Validation.validate {
            m with
                Evidence = [ conflicting; other ]
        }

    Assert.Contains(ds, fun d -> d.Code = "EEF010" && d.Severity = Validation.Advisory)
    Assert.Empty(Validation.errors ds)

[<Fact>]
let ``data owned by two capabilities has no single system of record`` () =
    let m = simple ()

    let dup = {
        List.head m.Capabilities with
            Id = CapabilityId "CAP-OTHER"
    }

    Assert.Contains(
        "EEF007",
        codes {
            m with
                Capabilities = m.Capabilities @ [ dup ]
        }
    )

[<Fact>]
let ``check refuses a model with errors`` () =
    let m = modelOf [] [ "D-NEVER" ] [] []

    match Validation.check m with
    | Ok _ -> failwith "expected errors"
    | Error ds -> Assert.NotEmpty ds
