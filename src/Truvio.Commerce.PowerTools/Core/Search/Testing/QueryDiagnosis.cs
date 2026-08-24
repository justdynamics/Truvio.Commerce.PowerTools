using System.Globalization;

namespace Truvio.Commerce.PowerTools.Core.Search.Testing;

/// <summary>
/// The pure half of the Query tester: given the query spec, its index schema and the values
/// for one run, decide what happens to every clause and what the user should change. No
/// Dynamicweb types, so every rule here is unit-testable.
/// <para>
/// The verdicts mirror <c>Helpers.ParseQueryExpressionInternal</c> exactly, in its own order:
/// a disabled binary expression returns null first; then the field is looked up in
/// <c>index.Schema.Fields</c> and a miss throws <c>ArgumentException</c>; only then is the
/// right-hand value resolved, and <c>value == null &amp;&amp; op != IsEmpty</c> drops the
/// clause. A group keeps only its non-null children and is itself null when none survive,
/// and <c>LuceneIndexProvider</c> ends with
/// <c>ParseQueryExpression(...) ?? new MatchAllDocsQuery()</c>.
/// </para>
/// </summary>
public static class QueryDiagnosis
{
    /// <summary>Walks the expression tree and resolves every node for this run.</summary>
    public static IReadOnlyList<ClauseTrace> Trace(RunInputs inputs)
    {
        var traces = new List<ClauseTrace>();
        Walk(inputs, inputs.Query.Expression, null, 0, traces);

        if (traces.Count == 0)
        {
            traces.Add(new ClauseTrace(
                "1", 0, false, "(no expression)", string.Empty, string.Empty, string.Empty,
                ValueOrigin.None, string.Empty, ClauseVerdict.MatchesEverything,
                "The query has no expression at all, so the provider falls back to a match-all query."));
        }

        return traces;
    }

    /// <summary>True when no clause survives, so the provider returns the whole index.</summary>
    public static bool Collapses(IReadOnlyList<ClauseTrace> traces) =>
        traces.All(t => t.IsGroup || t.Verdict != ClauseVerdict.Active) &&
        traces.All(t => t.Verdict != ClauseVerdict.Throws);

    /// <summary>True when the query cannot run at all — a clause field is missing from the schema.</summary>
    public static bool Throws(IReadOnlyList<ClauseTrace> traces) =>
        traces.Any(t => t.Verdict == ClauseVerdict.Throws);

    // ---- suggestions ---------------------------------------------------------------------

    /// <summary>What to change, judged from the query and the values alone.</summary>
    public static IReadOnlyList<Suggestion> Suggest(RunInputs inputs, IReadOnlyList<ClauseTrace> traces)
    {
        var suggestions = new List<Suggestion>();
        var query = inputs.Query;

        foreach (var trace in traces.Where(t => t.Verdict == ClauseVerdict.Throws))
        {
            suggestions.Add(Suggestion.Fix(
                $"Field '{trace.Field}' is not in the index schema",
                $"Clause {trace.Path} ({trace.Label}) makes the provider throw ArgumentException, so the whole query fails. " +
                "Correct the field name in the query editor, or add the field to the index schema and rebuild."));
        }

        foreach (var trace in traces.Where(t => t.Verdict == ClauseVerdict.UnknownField))
        {
            suggestions.Add(Suggestion.Fix(
                $"Field '{trace.Field}' is not in the index schema",
                $"Clause {trace.Path} ({trace.Label}) is dropped with only a warning in the system log, so it never constrains anything — " +
                $"{DropEffect(traces, trace)} Correct the field name, or add the field to the index schema and rebuild. " +
                "On Dynamicweb 10.19 and older the same clause throws and takes the whole query down."));
        }

        foreach (var trace in traces.Where(t => t.Verdict == ClauseVerdict.Dropped && t.Origin == ValueOrigin.MissingParameter))
        {
            var effect = DropEffect(traces, trace);
            suggestions.Add(Suggestion.Fix(
                $"Parameter '{trace.ParameterName}' has no value, so clause {trace.Path} vanished",
                $"{trace.Label} is removed before the index sees it — {effect} " +
                $"Give '{trace.ParameterName}' a default value in the query editor, or make sure every caller supplies it."));
        }

        foreach (var trace in traces.Where(t => t.Verdict == ClauseVerdict.Dropped && t.Origin is ValueOrigin.Macro or ValueOrigin.Code))
        {
            var effect = DropEffect(traces, trace);
            suggestions.Add(Suggestion.Warn(
                $"Clause {trace.Path} vanished because its {(trace.Origin == ValueOrigin.Macro ? "macro" : "code provider")} returned nothing",
                $"{trace.Label} resolved to an empty value in this context — {effect} " +
                "A macro that only resolves on the frontend (session, cart, favourites) always drops here."));
        }

        foreach (var trace in traces.Where(t => t.Verdict == ClauseVerdict.Disabled))
        {
            suggestions.Add(Suggestion.Info(
                $"Clause {trace.Path} is disabled",
                $"{trace.Label} is switched off in the query editor and never reaches the index. Remove it or enable it."));
        }

        if (Collapses(traces))
        {
            suggestions.Add(Suggestion.Fix(
                "This run returns EVERY document in the index",
                "Nothing survives of the expression, and LuceneIndexProvider falls back to a match-all query. " +
                "Supply values, or give the parameters default values so at least one clause always constrains the result."));
        }

        foreach (var trace in traces.Where(t => !t.IsGroup && t.Verdict == ClauseVerdict.Active))
        {
            var field = inputs.Index?.Field(trace.Field);
            if (field is null)
                continue;

            if (!field.Indexed)
            {
                suggestions.Add(Suggestion.Fix(
                    $"Field '{trace.Field}' is stored but not indexed",
                    $"Clause {trace.Path} can never match: the index writer only writes searchable terms for indexed fields. " +
                    "Tick 'Indexed' on the field in the index schema and rebuild."));
                continue;
            }

            if (field.Analyzed && IsWholeValueOperator(trace.Operator))
            {
                suggestions.Add(Suggestion.Warn(
                    $"'{trace.Operator}' against the analyzed field '{trace.Field}'",
                    $"An analyzed field stores analyzer tokens, not the whole value, so {trace.Operator} only matches a single token — " +
                    $"'{trace.ResolvedValue}' will not match unless it is exactly one token in lower case. " +
                    "Use Contains, compare against a non-analyzed sibling field, or turn 'Analyzed' off and rebuild."));
            }
        }

        foreach (var supplied in inputs.Values.Keys)
        {
            var declared = query.Parameter(supplied);
            var used = query.Clauses().Any(c =>
                c.ValueKind == ClauseValueKind.Parameter &&
                string.Equals(c.ParameterName, supplied, StringComparison.OrdinalIgnoreCase));
            var facetUse = query.Parameters.Any(p => string.Equals(p.Name, supplied, StringComparison.OrdinalIgnoreCase));

            if (declared is null && !used)
            {
                suggestions.Add(Suggestion.Info(
                    $"'{supplied}' is not a parameter of this query",
                    "The value is passed to the provider but no clause and no declaration reads it, so it changes nothing."));
            }
            else if (declared is null)
            {
                suggestions.Add(Suggestion.Warn(
                    $"Clause parameter '{supplied}' is not declared by the query",
                    "It works here only because you supplied it — with nothing supplied the clause silently disappears. " +
                    "Declare the parameter (with a default) in the query editor."));
            }
            else if (!used && facetUse)
            {
                suggestions.Add(Suggestion.Info(
                    $"No clause reads parameter '{supplied}'",
                    "It is declared but only a facet group can be using it; on this query alone the value has no effect."));
            }
        }

        if (inputs.Index is { } index)
        {
            switch (index.Health)
            {
                case IndexHealth.NeverBuilt:
                    suggestions.Add(Suggestion.Fix(
                        "The source index has never been built",
                        $"{index.Key}: {index.HealthDetail} Build it before trusting any result on this screen."));
                    break;
                case IndexHealth.Failed:
                    suggestions.Add(Suggestion.Fix(
                        "The last build of the source index failed",
                        $"{index.Key}: {index.HealthDetail} The documents you see are whatever survived the previous build."));
                    break;
                case IndexHealth.Stale:
                    suggestions.Add(Suggestion.Warn(
                        "The source index is stale",
                        $"{index.Key}: {index.HealthDetail} Anything changed since then is missing or out of date."));
                    break;
            }
        }
        else
        {
            suggestions.Add(Suggestion.Fix(
                "The query's source index does not exist",
                $"'{query.SourceKey}' is not a repository index, so the query can never run."));
        }

        return Order(suggestions);
    }

    /// <summary>What the measured per-clause hit counts say.</summary>
    /// <summary>One place a supplied search value occurs in a document's stored fields.</summary>
    public sealed record TermHit(string Term, string Field, string Before, string Match, string After);

    /// <summary>
    /// Search-highlighting for one result row: where do the run's values actually occur in
    /// this document? Scans the STORED field values (the match itself happened on their
    /// analyzed counterparts, but the stored siblings hold the same text), so it costs no
    /// extra query. Catch-all fields (freetext, *_Search) are skipped when a real field also
    /// carries the term — "EcoTouch appears in the Description" beats "freetext matched".
    /// A multi-word value is also tried word by word, mirroring the wildcard-per-word
    /// queries the platform generates.
    /// </summary>
    public static IReadOnlyList<TermHit> TermHits(
        IReadOnlyDictionary<string, string> documentFields,
        IEnumerable<string> values,
        int maxHits = 4)
    {
        var terms = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .SelectMany(v => v.Trim().Contains(' ') ? new[] { v.Trim() }.Concat(v.Split(' ', StringSplitOptions.RemoveEmptyEntries)) : [v.Trim()])
            .Where(t => t.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (terms.Count == 0)
            return [];

        var hits = new List<TermHit>();
        foreach (var term in terms)
        {
            var found = new List<TermHit>();
            foreach (var (field, text) in documentFields)
            {
                if (string.IsNullOrEmpty(text))
                    continue;

                var index = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    continue;

                var start = Math.Max(0, index - 35);
                var end = Math.Min(text.Length, index + term.Length + 35);
                found.Add(new TermHit(
                    term,
                    field,
                    (start > 0 ? "…" : string.Empty) + text[start..index],
                    text.Substring(index, term.Length),
                    text[(index + term.Length)..end] + (end < text.Length ? "…" : string.Empty)));
            }

            // A hit in a real field makes the catch-all copies of the same text pure noise.
            var real = found.Where(h => !IsCatchAll(h.Field)).ToList();
            hits.AddRange((real.Count > 0 ? real : found).Take(2));
        }

        return hits.Take(maxHits).ToList();
    }

    private static bool IsCatchAll(string field) =>
        field.Equals("freetext", StringComparison.OrdinalIgnoreCase) ||
        field.Equals("keywords", StringComparison.OrdinalIgnoreCase) ||
        field.EndsWith("_Search", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<Suggestion> SuggestFromImpact(
        int totalHits,
        int? defaultsOnlyHits,
        IReadOnlyList<ClauseImpact> impacts)
    {
        var suggestions = new List<Suggestion>();

        foreach (var impact in impacts.Where(i => i.KillsResult))
        {
            suggestions.Add(Suggestion.Fix(
                $"Clause {impact.Path} matches no document at all",
                $"{impact.Label} on its own returns 0 hits, so it kills every result it is ANDed with. " +
                "Check the value, the field and the analyzer before looking anywhere else."));
        }

        if (totalHits == 0)
        {
            foreach (var impact in impacts.Where(i => i.WithoutClause > 0 && !i.KillsResult))
            {
                suggestions.Add(Suggestion.Fix(
                    $"Clause {impact.Path} is why this run returns nothing",
                    $"Removing {impact.Label} lifts the result from 0 to {impact.WithoutClause.GetValueOrDefault().ToString("N0", CultureInfo.InvariantCulture)} hits."));
            }
        }

        foreach (var impact in impacts.Where(i => i.WithoutClause.HasValue && i.WithoutClause.Value == totalHits))
        {
            suggestions.Add(Suggestion.Info(
                $"Clause {impact.Path} changes nothing for these values",
                $"{impact.Label} narrows the result by 0 documents — either it is redundant with another clause, or its value matches everything."));
        }

        if (defaultsOnlyHits.HasValue && defaultsOnlyHits.Value == totalHits && impacts.Count > 0)
        {
            suggestions.Add(Suggestion.Info(
                "The values you supplied did not narrow the result",
                $"The query returns {totalHits.ToString("N0", CultureInfo.InvariantCulture)} hits with and without them — check the clause trace for dropped clauses."));
        }

        return Order(suggestions);
    }

    /// <summary>Why one specific document is missing, judged from its own field values.</summary>
    public static IReadOnlyList<Suggestion> SuggestFromExpectation(
        IReadOnlyList<ClauseTrace> traces,
        string expectedKey,
        bool foundInIndex,
        IReadOnlyList<ExpectationCheck> checks,
        IReadOnlyDictionary<string, string> documentFields)
    {
        var suggestions = new List<Suggestion>();

        if (!foundInIndex)
        {
            suggestions.Add(Suggestion.Fix(
                $"'{expectedKey}' is not in the index at all",
                "No clause can bring it back. Either the builder skipped it (inactive, wrong language, outside the builder's scope) " +
                "or the index has not been rebuilt since it was created."));
            return Order(suggestions);
        }

        foreach (var check in checks.Where(c => !c.Passes))
        {
            var value = check.DocumentValue;
            suggestions.Add(Suggestion.Fix(
                $"'{expectedKey}' fails clause {check.Path}",
                $"{check.Label} — the document has {check.Field} = '{Shorten(value)}'. {check.Note}".Trim()));

            var sibling = documentFields.FirstOrDefault(f =>
                !string.Equals(f.Key, check.Field, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(f.Value) &&
                MatchesExpectedValue(f.Value, ExpectedValue(traces, check.Path)));

            if (!string.IsNullOrEmpty(sibling.Key))
            {
                suggestions.Add(Suggestion.Warn(
                    $"The value you are filtering on lives in '{sibling.Key}', not '{check.Field}'",
                    $"On this document {sibling.Key} = '{Shorten(sibling.Value)}'. Point clause {check.Path} at that field instead."));
            }
        }

        if (checks.Count > 0 && checks.All(c => c.Passes))
        {
            suggestions.Add(Suggestion.Info(
                $"'{expectedKey}' passes every active clause",
                "It should be in the result. If you do not see it, the cause is the result window (Take/Skip), the sort order, " +
                "or a facet selection applied on top of the query."));
        }

        return Order(suggestions);
    }

    // ---- internals -----------------------------------------------------------------------

    private static void Walk(
        RunInputs inputs,
        QueryNodeSpec? node,
        QueryGroupSpec? parent,
        int depth,
        List<ClauseTrace> traces)
    {
        switch (node)
        {
            case null:
                return;

            case QueryGroupSpec group:
            {
                var label = (group.Negate ? "NOT " : string.Empty) +
                            $"{(group.IsAnd ? "All of" : "Any of")} ({group.Children.Count})";
                traces.Add(new ClauseTrace(
                    group.Path, depth, true, label, string.Empty, group.Operator, string.Empty,
                    ValueOrigin.None, string.Empty, ClauseVerdict.Active,
                    group.IsAnd
                        ? "Children become Lucene MUST clauses; a child that disappears widens the result."
                        : "Children become Lucene SHOULD clauses; a child that disappears narrows the result."));

                foreach (var child in group.Children)
                    Walk(inputs, child, group, depth + 1, traces);
                return;
            }

            case QueryFullTextSpec fullText:
            {
                var fields = fullText.Fields.Count == 0 ? "every field in the schema" : string.Join(", ", fullText.Fields);
                var blank = string.IsNullOrWhiteSpace(fullText.SearchText);
                traces.Add(new ClauseTrace(
                    fullText.Path, depth, false, $"Free text '{fullText.SearchText}' over {fields}",
                    fields, "FullText", string.Empty, ValueOrigin.FullText, fullText.SearchText,
                    blank ? ClauseVerdict.Dropped : ClauseVerdict.Active,
                    blank
                        ? "The search text is empty, so MultiFieldQueryParser has nothing to parse."
                        : "Parsed by MultiFieldQueryParser with the index analyzer."));
                return;
            }

            case QueryClauseSpec clause:
            {
                traces.Add(Resolve(inputs, clause, parent, depth));
                return;
            }
        }
    }

    private static ClauseTrace Resolve(RunInputs inputs, QueryClauseSpec clause, QueryGroupSpec? parent, int depth)
    {
        var label = Describe(clause);
        var isEmptyOperator = string.Equals(clause.Operator, "IsEmpty", StringComparison.OrdinalIgnoreCase);

        // 1. A disabled binary expression returns null before anything else is looked at.
        if (clause.Disabled)
        {
            return new ClauseTrace(
                clause.Path, depth, false, label, clause.FieldName, clause.Operator, clause.ParameterName,
                ValueOrigin.None, string.Empty, ClauseVerdict.Disabled,
                $"Disabled in the query editor. {LuceneSemantics.DropEffect(parent)}.");
        }

        // 2. The field lookup happens next, before any value is resolved. What a miss costs
        //    depends on the platform version — see RunInputs.DropsUnknownField.
        if (inputs.Index is not null && inputs.Index.Field(clause.FieldName) is null)
        {
            return inputs.DropsUnknownField
                ? new ClauseTrace(
                    clause.Path, depth, false, label, clause.FieldName, clause.Operator, clause.ParameterName,
                    ValueOrigin.None, string.Empty, ClauseVerdict.UnknownField,
                    $"'{clause.FieldName}' is not in the schema of {inputs.Index.Key}. This platform logs a warning and removes the clause, " +
                    $"so the query still runs: {LuceneSemantics.DropEffect(parent)}.")
                : new ClauseTrace(
                    clause.Path, depth, false, label, clause.FieldName, clause.Operator, clause.ParameterName,
                    ValueOrigin.None, string.Empty, ClauseVerdict.Throws,
                    $"'{clause.FieldName}' is not in the schema of {inputs.Index.Key}; the provider throws ArgumentException and the whole query fails.");
        }

        // 3. Only now is the right-hand value resolved.
        var (origin, value) = ResolveValue(inputs, clause);
        var missing = string.IsNullOrEmpty(value);

        if (missing && !isEmptyOperator)
        {
            var reason = origin switch
            {
                ValueOrigin.MissingParameter =>
                    $"Parameter '{clause.ParameterName}' carries no default and no value was supplied, so it never enters the parameter dictionary.",
                ValueOrigin.Macro => "The macro resolved to nothing in this context.",
                ValueOrigin.Code => "The code provider returned nothing.",
                _ => "The value resolved to null."
            };

            return new ClauseTrace(
                clause.Path, depth, false, label, clause.FieldName, clause.Operator, clause.ParameterName,
                origin, string.Empty, ClauseVerdict.Dropped,
                $"{reason} The clause is removed silently: {LuceneSemantics.DropEffect(parent)}.");
        }

        var explanation = origin switch
        {
            ValueOrigin.SuppliedValue => "Uses the value you supplied for this run.",
            ValueOrigin.ParameterDefault => $"Uses the declared default of '{clause.ParameterName}'.",
            ValueOrigin.UndeclaredParameter =>
                $"'{clause.ParameterName}' is not declared by the query — it only has a value because you supplied one.",
            ValueOrigin.Macro => "Value produced by a macro lookup at run time.",
            ValueOrigin.Code => "Value produced by a code provider at run time.",
            _ => "Constant written into the query."
        };

        if (isEmptyOperator)
        {
            explanation += " IsEmpty is the one operator the provider keeps even without a value: " +
                           "it becomes 'match all documents MUST_NOT have any term in this field'.";
        }

        return new ClauseTrace(
            clause.Path, depth, false, label, clause.FieldName, clause.Operator, clause.ParameterName,
            origin, value, ClauseVerdict.Active, explanation);
    }

    private static (ValueOrigin Origin, string Value) ResolveValue(RunInputs inputs, QueryClauseSpec clause)
    {
        switch (clause.ValueKind)
        {
            case ClauseValueKind.Parameter:
            {
                var declared = inputs.Query.Parameter(clause.ParameterName);
                if (inputs.Values.TryGetValue(clause.ParameterName, out var supplied) && !string.IsNullOrEmpty(supplied))
                    return (declared is null ? ValueOrigin.UndeclaredParameter : ValueOrigin.SuppliedValue, supplied);

                if (declared is not null && declared.HasDefault)
                    return (ValueOrigin.ParameterDefault, declared.DefaultValue);

                return (ValueOrigin.MissingParameter, string.Empty);
            }

            case ClauseValueKind.Macro:
                return (ValueOrigin.Macro, Runtime(inputs, clause.Path, clause.Value));

            case ClauseValueKind.Code:
                return (ValueOrigin.Code, Runtime(inputs, clause.Path, string.Empty));

            case ClauseValueKind.Term:
                return (ValueOrigin.Term, clause.Value);

            default:
                return (ValueOrigin.Constant, clause.Value);
        }
    }

    /// <summary>
    /// Macro and code expressions can only be resolved by the live adapter; when it has not
    /// run (unit tests, or an index that never loaded) fall back to the literal text so the
    /// clause is not reported as dropped on no evidence.
    /// </summary>
    private static string Runtime(RunInputs inputs, string path, string fallback) =>
        inputs.RuntimeValues.TryGetValue(path, out var value) ? value : fallback;

    private static string DropEffect(IReadOnlyList<ClauseTrace> traces, ClauseTrace trace)
    {
        var parent = traces.FirstOrDefault(t => t.IsGroup && IsParentPath(t.Path, trace.Path));
        return parent is null || string.Equals(parent.Operator, "And", StringComparison.OrdinalIgnoreCase)
            ? "the constraint disappears and the query returns MORE documents than intended."
            : "the alternative disappears and the query returns FEWER documents than intended.";
    }

    private static bool IsParentPath(string parent, string child)
    {
        var dot = child.LastIndexOf('.');
        return dot > 0 && string.Equals(child[..dot], parent, StringComparison.Ordinal);
    }

    private static string Describe(QueryClauseSpec clause)
    {
        var right = clause.ValueKind switch
        {
            ClauseValueKind.Parameter => "@" + clause.ParameterName,
            ClauseValueKind.Macro => $"macro({clause.Value})",
            ClauseValueKind.Code => "code(...)",
            _ => string.IsNullOrEmpty(clause.Value) ? "''" : clause.Value
        };

        return $"{clause.FieldName} {clause.Operator} {right}";
    }

    /// <summary>Operators that compare the whole value rather than a substring.</summary>
    private static bool IsWholeValueOperator(string op) =>
        op is "Equal" or "MatchAny" or "MatchAll" or "In";

    private static string ExpectedValue(IReadOnlyList<ClauseTrace> traces, string path) =>
        traces.FirstOrDefault(t => t.Path == path)?.ResolvedValue ?? string.Empty;

    private static bool MatchesExpectedValue(string candidate, string expected) =>
        !string.IsNullOrEmpty(expected) &&
        candidate.Contains(expected, StringComparison.OrdinalIgnoreCase);

    internal static string Shorten(string? value, int max = 120)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= max ? value : value[..max] + "...";
    }

    private static IReadOnlyList<Suggestion> Order(List<Suggestion> suggestions) =>
        suggestions
            .GroupBy(s => s.Title, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(s => s.Rank)
            .ToList();
}
