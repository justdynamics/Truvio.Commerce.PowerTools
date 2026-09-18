using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Data.DynamicFields;
using Truvio.Commerce.PowerTools.Features.QueryTester.Queries;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Models;

/// <summary>
/// The "Set parameters" dialog: one dynamic field per declared parameter.
/// <see cref="IModelWithDynamicFields"/> is what makes the round-trip work: when the OK
/// command is posted, DW's model builder merges standard properties (Repository, Item) first,
/// then calls <see cref="FillDynamicFields"/> to rebuild the field set server-side, and only
/// then copies the posted values into those fields — without it the posted values are
/// silently dropped.
/// </summary>
public sealed class QueryValuesModel : DataViewModelBase, IModelWithDynamicFields
{
    public string QueryName { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public string Item { get; set; } = string.Empty;

    public FieldGroupCollection Fields { get; set; } = new();

    public void FillDynamicFields()
    {
        if (Fields.Groups.Any())
            return;

        Fields = Queries.QueryValuesQuery.BuildFields(Repository, Item, string.Empty);
    }
}
