module Echelon.Enterprise.Cli.Program

open System.IO
open Echelon.Enterprise
open Echelon.Enterprise.CaseStudies

// The only module with effects: argument parsing, file output and exit codes.
// Exit codes: 0 ok, 2 usage, 3 validation errors, 4 architecture invariant violations.

let private usage =
    """eef — Echelon AI-Native Enterprise Design Framework

Usage:
  eef list
  eef validate <case|all>
  eef analyse  <case|all> [--out DIR]      writes DIR/<case>/analysis.json and analysis.md
  eef export-eks <case|all> [--out DIR]    writes EKS evidence records to DIR/<case>/knowledge/

Default DIR is case-studies."""

let private selected name =
    Catalog.all |> List.filter (fun (slug, _, _) -> name = "all" || slug = name)

let private printDiagnostics (ds: Validation.Diagnostic list) =
    ds
    |> List.iter (fun d -> printfn $"  {d.Code} [{Validation.severityLabel d.Severity}] {d.Subject}: {d.Message}")

let private write (path: string) (text: string) =
    match Path.GetDirectoryName path with
    | null -> ()
    | parent -> Directory.CreateDirectory parent |> ignore

    File.WriteAllText(path, text)
    printfn $"  wrote {path}"

let private outDir args =
    match args |> List.skipWhile ((<>) "--out") with
    | _ :: dir :: _ -> dir
    | _ -> "case-studies"

let private validate cases =
    cases
    |> List.map (fun (slug, model, _) ->
        let ds = Validation.validate model
        let errors = Validation.errors ds
        printfn $"{slug}: {errors.Length} error(s), {ds.Length - errors.Length} other diagnostic(s)"
        printDiagnostics ds
        if errors.IsEmpty then 0 else 3)
    |> List.fold max 0

let private analyse dir cases =
    cases
    |> List.map (fun (slug, model, sim) ->
        printfn $"{slug}:"

        match Analysis.run sim model with
        | Error ds ->
            printDiagnostics ds
            3
        | Ok report ->
            write (Path.Combine(dir, slug, "generated", "analysis.json")) (Json.serialize report)
            write (Path.Combine(dir, slug, "generated", "analysis.md")) (Markdown.render report + "\n")

            let violations = report.Workflows |> List.collect _.Architecture.Violations
            violations |> List.iter (printfn "  VIOLATION: %s")

            report.Workflows
            |> List.iter (fun w ->
                printfn
                    $"  {w.Redesign.Workflow.Id.Value}: {Reinvention.classificationLabel w.Redesign.Classification}, economics {Economics.verdictLabel report.Economics.Verdict}")

            if violations.IsEmpty then 0 else 4)
    |> List.fold max 0

let private exportEks dir cases =
    cases
    |> List.iter (fun (slug, model, _) ->
        Knowledge.export model
        |> List.iter (fun (rel, text) -> write (Path.Combine(dir, slug, "knowledge", rel)) text))

    0

[<EntryPoint>]
let main argv =
    match List.ofArray argv with
    | [ "list" ] ->
        Catalog.all |> List.iter (fun (slug, m, _) -> printfn $"{slug}\t{m.Name}")
        0
    | "validate" :: name :: _ -> validate (selected name)
    | "analyse" :: name :: rest -> analyse (outDir rest) (selected name)
    | "export-eks" :: name :: rest -> exportEks (outDir rest) (selected name)
    | _ ->
        eprintfn "%s" usage
        2
