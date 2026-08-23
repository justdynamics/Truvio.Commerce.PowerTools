using Truvio.Commerce.PowerTools.Core.Permissions;

namespace Truvio.Commerce.PowerTools.Core.Diagnostics.Rules;

/// <summary>
/// The DW10 runtime resolves render-time permissions from the permission entity store only;
/// the legacy Page.PagePermission / EcomParagraph.ParagraphPermission columns are ignored.
/// A populated legacy value means someone believes content is gated when it is not.
/// </summary>
public sealed class LegacyPermissionColumnRule : IWarningRule
{
    public string RuleId => "SECOPS-W3";

    public IEnumerable<Finding> Evaluate(WarningContext context)
    {
        foreach (var pageId in context.Source.GetPagesWithLegacyPermissionValues())
        {
            var entity = context.DescribeEntity(ContentEntityNames.Page, pageId.ToString());
            yield return new Finding(
                RuleId,
                FindingSeverity.Warning,
                ContentEntityNames.Page,
                pageId.ToString(),
                entity,
                "Legacy page permission value is ignored at runtime",
                $"{entity} has a value in the legacy page permission column. DW10 does not evaluate "
                + "it when rendering — if this page should be gated, set the permission through the "
                + "page's Permissions panel instead.");
        }

        foreach (var paragraphId in context.Source.GetParagraphsWithLegacyPermissionValues())
        {
            var entity = context.DescribeEntity(ContentEntityNames.Paragraph, paragraphId.ToString());
            yield return new Finding(
                RuleId,
                FindingSeverity.Warning,
                ContentEntityNames.Paragraph,
                paragraphId.ToString(),
                entity,
                "Legacy paragraph permission value is ignored at runtime",
                $"{entity} has a value in the legacy paragraph permission column. DW10 does not "
                + "evaluate it when rendering — if this paragraph should be gated, set the permission "
                + "through the paragraph's Permissions panel instead.");
        }
    }
}
