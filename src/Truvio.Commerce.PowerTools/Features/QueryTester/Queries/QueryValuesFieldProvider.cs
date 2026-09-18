using System.Globalization;
using Dynamicweb.CoreUI.Data.DynamicFields;
using Dynamicweb.CoreUI.Editors;
using Dynamicweb.CoreUI.Editors.Inputs;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Queries;

/// <summary>
/// Renders every parameter as a plain text input. A <see cref="FieldGroup"/> demands a
/// provider because DW's dynamic-field pipeline asks it for each field's editor; this one has
/// no persistence side (the OK command reads the posted values itself), so SaveChanges is a
/// no-op.
/// </summary>
internal sealed class QueryValuesFieldProvider : FieldEditorProviderBase
{
    private readonly List<FieldGroup> groups = [];
    private readonly FieldGroupCollection collection = new();

    public override FieldGroupCollection Collection => collection;

    public void AddGroup(string name, IEnumerable<Field> fields)
    {
        groups.Add(new FieldGroup(this)
        {
            Name = name,
            SystemName = string.Empty,
            Fields = fields.ToList()
        });
        collection.Groups = groups;
    }

    protected override EditorBase? GetEditor(Field field) => new Text
    {
        Name = field.SystemName,
        Label = field.Name,
        Hint = field.Hint,
        Value = Convert.ToString(field.Value, CultureInfo.InvariantCulture),
        Readonly = field.Readonly
    };

    public override object? SaveChanges(FieldGroupCollection fieldGroupCollection) => null;
}
