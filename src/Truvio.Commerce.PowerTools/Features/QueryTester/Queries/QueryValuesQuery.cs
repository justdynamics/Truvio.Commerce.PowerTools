using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Data.DynamicFields;
using Dynamicweb.CoreUI.Editors;
using Dynamicweb.CoreUI.Editors.Inputs;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Core;
using Truvio.Commerce.PowerTools.Features.IndexInspector.Queries;
using Truvio.Commerce.PowerTools.Features.QueryTester.Core;
using Truvio.Commerce.PowerTools.Features.QueryTester.Models;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Queries;

/// <summary>
/// Builds the "Set parameters" dialog: one editable field per declared parameter (plus the
/// tester's own <c>#expect</c> setting), pre-filled from <see cref="Parameters"/>. A prompt
/// screen posts its edited model back to the OK command, so this is a real form — no toolbar
/// search box tricks. The field set differs per query, hence dynamic fields rather than a
/// fixed model shape.
/// </summary>
public sealed class QueryValuesQuery : DataQueryModelBase<QueryValuesModel>
{
    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    /// <summary>The values to pre-fill, as <c>name=value;name2=value2</c>.</summary>
    public string Parameters { get; set; } = string.Empty;

    public override QueryValuesModel? GetModel()
    {
        var model = new QueryValuesModel { Repository = Repository, Item = Item };
        if (string.IsNullOrEmpty(Repository) || string.IsNullOrEmpty(Item))
            return model;

        try
        {
            model.QueryName = SearchQueryHelpers.Catalog().Query(Repository, Item)?.Name ?? string.Empty;
        }
        catch
        {
            return model;
        }

        model.Fields = BuildFields(Repository, Item, Parameters);
        return model;
    }

    /// <summary>Also called by <see cref="QueryValuesModel.FillDynamicFields"/> when the OK command's posted model is rebuilt.</summary>
    internal static FieldGroupCollection BuildFields(string repository, string item, string parameters)
    {
        var fields = new FieldGroupCollection();
        if (string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(item))
            return fields;

        QuerySpec? query;
        try
        {
            query = SearchQueryHelpers.Catalog().Query(repository, item);
        }
        catch
        {
            return fields;
        }

        if (query is null)
            return fields;

        var provider = new QueryValuesFieldProvider();
        var values = ParameterValues.Parse(parameters);

        var parameterFields = new List<Field>();
        foreach (var parameter in query.Parameters)
        {
            values.TryGetValue(parameter.Name, out var supplied);
            var used = query.Clauses().Any(c =>
                c.ValueKind == ClauseValueKind.Parameter &&
                string.Equals(c.ParameterName, parameter.Name, StringComparison.OrdinalIgnoreCase));

            parameterFields.Add(new Field(parameter)
            {
                Name = parameter.Name,
                SystemName = parameter.Name,
                TypeName = "System.String",
                Value = supplied ?? string.Empty,
                DefaultValue = parameter.HasDefault ? parameter.DefaultValue : string.Empty,
                Hint = Hint(parameter, used)
            });
        }

        var expect = ParameterValues.Reserved(parameters, ParameterValues.ExpectKeyName);
        var expectField = new Field(query)
        {
            Name = "Expected document (#expect)",
            SystemName = ParameterValues.ExpectKeyName,
            TypeName = "System.String",
            Value = expect,
            Hint = "The document key the \"Why not X?\" section explains, e.g. a product ID."
        };

        provider.AddGroup("Parameter values", parameterFields);
        provider.AddGroup("Tester settings", [expectField]);
        return provider.Collection;
    }

    private static string Hint(QueryParameterSpec parameter, bool used)
    {
        var type = IndexFieldSpec.ShortenType(parameter.TypeName);
        var tail = !used
            ? "No clause reads this parameter - it can only drive a facet."
            : parameter.HasDefault
                ? $"Blank runs the clause with the declared default ({parameter.DefaultValue})."
                : "Blank makes its clause DISAPPEAR - nothing constrains that field.";
        return $"{type}. {tail}";
    }
}
