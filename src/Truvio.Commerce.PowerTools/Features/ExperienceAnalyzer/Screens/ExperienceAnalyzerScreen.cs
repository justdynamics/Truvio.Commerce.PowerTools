using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.ContentAccess.Queries;
using Truvio.Commerce.PowerTools.Features.ContentAccess.Screens;
using Truvio.Commerce.PowerTools.Features.ExperienceAnalyzer.Models;
using Truvio.Commerce.PowerTools.Features.ExperienceAnalyzer.Queries;

namespace Truvio.Commerce.PowerTools.Features.ExperienceAnalyzer.Screens;

/// <summary>
/// What stands out about one account's content experience — and how it differs from another
/// account's. The question it answers is the one asked after the permissions are configured:
/// "the Lumber role should see its dashboard and the Roofing role theirs — did that land?"
/// <para>
/// It reports ACCESS, not rendering: no page is fetched or previewed, so a template that hides
/// content for its own reasons is out of scope. Overview screen, not a list — every row carries
/// an explanation, and the list grid clips long text.
/// </para>
/// </summary>
public sealed class ExperienceAnalyzerScreen : OverviewScreenBase<ExperienceAnalyzerModel>
{
    protected override string GetScreenName() =>
        Model is null || string.IsNullOrEmpty(Model.Title) ? "Experience Analyzer" : Model.Title;

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "Analysis failed", Group.GroupWidth.Col_12);
            return;
        }

        var information = new Dictionary<string, CardInfo.InfoValue>
        {
            ["Account"] = new(model.AccountName)
        };

        if (model.Comparing)
            information["Compared with"] = new(model.CompareName);

        information["Website"] = new(model.Scope);
        information["Sees"] = new(model.VisibleA);

        if (!string.IsNullOrEmpty(model.VisibleB))
            information[model.Comparing ? "They see" : "Public sees"] = new(model.VisibleB);

        information["Differences"] = new(new Badge
        {
            Value = model.Identical ? "None" : model.DifferenceCount.ToString(),
            BadgeType = model.Identical ? BadgeType.Success : BadgeType.Warning
        });

        SetInfobar(new InfoBar { Icon = Icon.Balance, Information = information });

        foreach (var section in model.Sections)
            AddComponent(new HtmlBlock { Value = section.Html }, section.Heading, Group.GroupWidth.Col_12);
    }

    protected override IEnumerable<ActionGroup>? GetScreenActions()
    {
        var q = Query as ExperienceAnalyzerQuery ?? new ExperienceAnalyzerQuery();
        var (accountKey, _) = q.EffectiveKeys();

        return
        [
            new ActionGroup
            {
                Nodes =
                [
                    new ActionNode
                    {
                        Name = "Open in Content Access Viewer",
                        Icon = Icon.Shield,
                        NodeAction = NavigateScreenAction.To<AccessOverviewScreen>()
                            .With(new AccessOverviewQuery { AccountKey = accountKey, AreaId = q.AreaId })
                    }
                ]
            }
        ];
    }
}
