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

/// <summary>
/// The scope switches for the PIM screens, in the top bar next to Actions: a searchable group
/// picker (a catalog can hold thousands of groups) and a language switch. An injector because
/// the screen bases keep their <c>ScreenLayout</c> private.
/// </summary>
public sealed class PimCompletenessToolbarInjector : ScreenInjector<PimCompletenessScreen>
{
    public override void OnAfter(PimCompletenessScreen screen, UiComponentBase content)
    {
        if (content is not ScreenLayout layout)
            return;

        var q = screen.Query as PimCompletenessQuery ?? new PimCompletenessQuery();

        PimToolbar.AddGroupPicker(layout, q.GroupId, token =>
            NavigateScreenAction.To<PimCompletenessScreen>()
                .With(new PimCompletenessQuery { LanguageId = q.LanguageId, GroupPickToken = token }));

        PimToolbar.AddLanguageSwitch(layout, q.LanguageId, languageId =>
            NavigateScreenAction.To<PimCompletenessScreen>()
                .With(new PimCompletenessQuery { GroupId = q.GroupId, LanguageId = languageId }));
    }
}
