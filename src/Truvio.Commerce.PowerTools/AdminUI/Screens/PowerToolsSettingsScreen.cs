using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Editors;
using Dynamicweb.CoreUI.Editors.Inputs;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Commands;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Security;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// The single settings screen for the whole suite — the one place in PowerTools that writes.
/// Built on DW's own settings pattern (<c>EditScreenBase</c> + a
/// <c>SettingsViewModelBase</c> model + a save command calling <c>SettingsService.Persist</c>),
/// so the editors, the binding and the GlobalSettings persistence are the platform's.
/// One tab per tool family; the settings a tab holds change how that tool behaves everywhere.
/// </summary>
public sealed class PowerToolsSettingsScreen : EditScreenBase<PowerToolsSettingsModel>
{
    private const string LinterTab = "Query linter";
    private const string OperationsTab = "Operations";
    private const string CommerceTab = "Price Explainer";
    private const string SecurityTab = "Content Access";
    private const string GeneralTab = "General";

    protected override string GetScreenName() => "PowerTools settings";

    /// <summary>Null when the user may not edit: CoreUI then renders no Save button at all.</summary>
    protected override CommandBase<PowerToolsSettingsModel>? GetSaveCommand() =>
        PowerToolsAccess.CanEditSettings() ? new PowerToolsSettingsSaveCommand() : null;

    protected override void BuildEditScreen()
    {
        AddComponents(LinterTab, "Suppressions",
        [
            EditorFor(m => m.IgnoredRules),
            EditorFor(m => m.IgnoredParameters),
            EditorFor(m => m.IgnoredQueries)
        ]);

        AddComponents(LinterTab, "Thresholds",
        [
            EditorFor(m => m.StaleIndexHours),
            EditorFor(m => m.DocumentRowsPerPage)
        ]);

        AddComponents(OperationsTab, "Scheduled tasks",
        [
            EditorFor(m => m.StaleTaskIntervalMultiplier),
            EditorFor(m => m.RunHistoryDepth)
        ]);

        AddComponents(OperationsTab, "Logs and storage",
        [
            EditorFor(m => m.LogFolderWarningMb),
            EditorFor(m => m.LogFolderCriticalMb),
            EditorFor(m => m.TableSharePercent)
        ]);

        AddComponents(OperationsTab, "Recent changes",
        [
            EditorFor(m => m.RecentChangesDays)
        ]);

        AddComponents(CommerceTab, "Caps",
        [
            EditorFor(m => m.ProductPickCap),
            EditorFor(m => m.PriceRowCap)
        ]);

        AddComponents(CommerceTab, "Context switches",
        [
            EditorFor(m => m.QuantityPresets),
            EditorFor(m => m.DatePresetDays),
            EditorFor(m => m.DefaultCurrencyCode)
        ]);

        AddComponents(SecurityTab, "Accounts",
        [
            EditorFor(m => m.UserFetchCap),
            EditorFor(m => m.HideAdministrators)
        ]);

        AddComponents(SecurityTab, "Warnings",
        [
            EditorFor(m => m.SuppressedWarningRules)
        ]);

        AddComponents(GeneralTab, "Sections",
        [
            EditorFor(m => m.SecuritySectionEnabled),
            EditorFor(m => m.CommerceSectionEnabled),
            EditorFor(m => m.OperationsSectionEnabled),
            EditorFor(m => m.SearchSectionEnabled)
        ]);

        AddComponents(GeneralTab, "Findings",
        [
            EditorFor(m => m.ShowRuleIds)
        ]);
    }

    protected override EditorBase? GetEditor(string property) => property switch
    {
        nameof(PowerToolsSettingsModel.IgnoredRules) or
        nameof(PowerToolsSettingsModel.IgnoredParameters) or
        nameof(PowerToolsSettingsModel.IgnoredQueries) or
        nameof(PowerToolsSettingsModel.SuppressedWarningRules) => new Textarea { Rows = 4 },

        nameof(PowerToolsSettingsModel.StaleIndexHours) => Num("Hours"),
        nameof(PowerToolsSettingsModel.RecentChangesDays) => Num("Days"),
        nameof(PowerToolsSettingsModel.LogFolderWarningMb) or
        nameof(PowerToolsSettingsModel.LogFolderCriticalMb) => Num("MB"),
        nameof(PowerToolsSettingsModel.TableSharePercent) => Num("%"),
        nameof(PowerToolsSettingsModel.StaleTaskIntervalMultiplier) => Num("× interval"),
        nameof(PowerToolsSettingsModel.RunHistoryDepth) => Num("runs"),
        nameof(PowerToolsSettingsModel.DocumentRowsPerPage) => Num("rows"),
        nameof(PowerToolsSettingsModel.ProductPickCap) => Num("products"),
        nameof(PowerToolsSettingsModel.PriceRowCap) => Num("rows"),
        nameof(PowerToolsSettingsModel.UserFetchCap) => Num("users"),

        _ => null
    };

    private static Number Num(string append) => new() { Append = append, Step = 1 };
}
