namespace Echelon.Enterprise

/// Export of evidence as echelon-knowledge-store (EKS) records: Markdown with
/// YAML front matter in an `evidence/` directory, so `eks validate` can check
/// identifiers, references and provenance. Pure: returns (relative path, text).
module Knowledge =

    let private yamlString (s: string) =
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""

    /// EKS lifecycle status. The epistemic status is carried separately in
    /// `evidence_type`, so nothing is lost by the mapping.
    let private status (e: Evidence) =
        match e.Status with
        | Fact -> "accepted"
        | Claim
        | Recommendation -> "review"
        | Assumption
        | Hypothesis -> "draft"

    let record (e: Evidence) =
        let primary = e.Sources |> List.tryPick _.Locator

        let lines = [
            yield "---"
            yield $"id: {e.Id.Value}"
            yield $"title: {yamlString e.Statement}"
            yield $"status: {status e}"
            yield $"confidence: {Confidence.label e.Confidence}"
            yield $"evidence_type: {EpistemicStatus.label e.Status}"

            match primary with
            | Some l -> yield $"source_uri: {yamlString l}"
            | None -> ()

            match e.Sources with
            | first :: _ -> yield $"source_title: {yamlString first.Citation}"
            | [] -> ()

            if not e.Contradicts.IsEmpty then
                yield "contradicts:"

                for c in e.Contradicts do
                    yield $"  - {c.Value}"

            yield "---"
            yield ""
            yield $"# {e.Id.Value}"
            yield ""
            yield e.Statement
            yield ""
            yield "## Sources"
            yield ""

            for s in e.Sources do
                let loc = s.Locator |> Option.map (fun l -> $" — {l}") |> Option.defaultValue ""
                yield $"- [{SourceKind.label s.Kind}] {s.Citation}{loc}"
        ]

        $"evidence/{e.Id.Value}.md", (lines |> String.concat "\n") + "\n"

    let export (m: EnterpriseModel) = m.Evidence |> List.map record
