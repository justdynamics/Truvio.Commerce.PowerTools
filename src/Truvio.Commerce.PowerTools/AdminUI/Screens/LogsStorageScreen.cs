using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Icon = Dynamicweb.CoreUI.Icons.Icon;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// What is growing on this install: log folders, the biggest tables, the retention settings
/// that decide whether either shrinks, and the findings that follow.
/// </summary>
public sealed class LogsStorageScreen : OverviewScreenBase<LogsStorageModel>
{
    protected override string GetScreenName() => "Logs & storage";

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "Storage report failed", Group.GroupWidth.Col_12);
            return;
        }

        SetInfobar(new InfoBar
        {
            Icon = Icon.Database,
            Information = new Dictionary<string, CardInfo.InfoValue>
            {
                ["Log files"] = new(model.LogTotal),
                ["Database"] = new(model.DatabaseTotal),
                ["Log purging"] = new(new Badge
                {
                    Value = model.RetentionSummary,
                    BadgeType = model.RetentionEnabled ? BadgeType.Success : BadgeType.Warning
                }),
                ["Findings"] = new(model.FindingCount.ToString())
            }
        });

        AddComponent(new HtmlBlock { Value = OpsHtml.Table(model.Findings) }, "Findings", Group.GroupWidth.Col_12);
        AddComponent(new HtmlBlock { Value = OpsHtml.Table(model.Folders) }, "Log folders by size", Group.GroupWidth.Col_12);
        AddComponent(new HtmlBlock { Value = OpsHtml.Table(model.Tables) }, "Largest database tables", Group.GroupWidth.Col_12);
        AddComponent(new HtmlBlock { Value = OpsHtml.Table(model.Retention) }, "Retention settings", Group.GroupWidth.Col_12);
    }
}
