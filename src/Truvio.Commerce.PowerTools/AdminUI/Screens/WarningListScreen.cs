using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Icons;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>Install-wide permission misconfiguration findings.</summary>
public sealed class WarningListScreen : ListScreenBase<FindingModel>
{
    protected override string GetScreenName() => "Content Access Warnings";

    protected override IEnumerable<ActionGroup>? GetScreenActions() =>
    [
        new()
        {
            Nodes =
            [
                new ActionNode
                {
                    Name = "PowerTools settings",
                    Icon = Icon.Cog,
                    NodeAction = NavigateScreenAction.To<PowerToolsSettingsScreen>().With(new PowerToolsSettingsQuery())
                }
            ]
        }
    ];

    protected override IEnumerable<ListViewMapping> GetViewMappings()
    {
        var columns = new List<Dynamicweb.CoreUI.Data.ModelMapping> { CreateMapping(m => m.Severity) };
        if (DwPowerToolsSettings.Current.ShowRuleIds)
            columns.Add(CreateMapping(m => m.RuleId));
        columns.Add(CreateMapping(m => m.Entity));
        columns.Add(CreateMapping(m => m.Title));
        columns.Add(CreateMapping(m => m.Detail));

        return [new RowViewMapping { Columns = columns }];
    }

    // The trailing "N findings hidden by settings" row has no severity: no badge for it.
    protected override Cell? GetCell(string propertyName, FindingModel model) =>
        propertyName == nameof(FindingModel.Severity) && !string.IsNullOrEmpty(model.Severity)
            ? Badges.Severity(model.Severity)
            : null;
}
