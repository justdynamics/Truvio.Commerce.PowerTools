using Dynamicweb.CoreUI;
using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Lists;
using Dynamicweb.CoreUI.Lists.ViewMappings;
using Dynamicweb.CoreUI.Screens;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Features.PimQuality.Queries;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Screens;

/// <summary>The same scope switches on the catalog overview.</summary>
public sealed class PimQualityToolbarInjector : ScreenInjector<PimQualityScreen>
{
    public override void OnAfter(PimQualityScreen screen, UiComponentBase content)
    {
        if (content is not ScreenLayout layout)
            return;

        var q = screen.Query as PimQualityQuery ?? new PimQualityQuery();

        PimToolbar.AddGroupPicker(layout, q.GroupId, token =>
            NavigateScreenAction.To<PimQualityScreen>()
                .With(new PimQualityQuery { LanguageId = q.LanguageId, GroupPickToken = token }));

        PimToolbar.AddLanguageSwitch(layout, q.LanguageId, languageId =>
            NavigateScreenAction.To<PimQualityScreen>()
                .With(new PimQualityQuery { GroupId = q.GroupId, LanguageId = languageId }));
    }
}
