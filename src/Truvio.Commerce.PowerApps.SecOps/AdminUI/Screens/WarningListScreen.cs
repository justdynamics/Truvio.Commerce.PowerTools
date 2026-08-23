using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerApps.SecOps.AdminUI.Models;

namespace Truvio.Commerce.PowerApps.SecOps.AdminUI.Screens;

/// <summary>Install-wide permission misconfiguration findings.</summary>
public sealed class WarningListScreen : ListScreenBase<FindingModel>
{
    protected override string GetScreenName() => "Security Viewer - warnings";

    protected override IEnumerable<ListViewMapping> GetViewMappings() =>
    [
        new RowViewMapping
        {
            Columns =
            [
                CreateMapping(m => m.Severity),
                CreateMapping(m => m.RuleId),
                CreateMapping(m => m.Entity),
                CreateMapping(m => m.Title),
                CreateMapping(m => m.Detail)
            ]
        }
    ];

    protected override Cell? GetCell(string propertyName, FindingModel model) =>
        propertyName == nameof(FindingModel.Severity)
            ? SecOpsBadges.Severity(model.Severity)
            : null;
}
