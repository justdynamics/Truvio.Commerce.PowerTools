using Dynamicweb.CoreUI.Data;
using Dynamicweb.Extensibility.Settings;
using Keys = Truvio.Commerce.PowerTools.Core.Settings.PowerToolsSettingKeys;
using Defaults = Truvio.Commerce.PowerTools.Core.Settings.PowerToolsSettingKeys.Defaults;

namespace Truvio.Commerce.PowerTools.AdminUI.Models;

/// <summary>
/// The editable face of the suite settings, built the way DW builds its own settings screens:
/// <see cref="SettingsViewModelBase"/> loads every <c>[Settings(path)]</c> property out of
/// GlobalSettings in its constructor (<c>SettingsService.Load</c>), and
/// <c>SettingsService.Persist</c> writes them back — editors, binding and persistence for free.
/// <para>
/// Every path lives in <see cref="Keys"/>, shared with the read-side adapter the tools use.
/// </para>
/// </summary>
public sealed class PowerToolsSettingsModel : SettingsViewModelBase
{
    // ---- Query linter ------------------------------------------------------------------------

    [ConfigurableProperty("Ignored rule ids", "One per line, e.g. IDX-W5. A trailing * matches a prefix (IDX-W1*).")]
    [Settings(Keys.IgnoredRules)]
    public string IgnoredRules { get; set; } = string.Empty;

    [ConfigurableProperty("Ignored query parameters", "Parameter names whose IDX-W1/IDX-W2 findings are expected here, e.g. eq, q.")]
    [Settings(Keys.IgnoredParameters)]
    public string IgnoredParameters { get; set; } = string.Empty;

    [ConfigurableProperty("Ignored queries", "Query names or Repository/Query.query keys, one per line.")]
    [Settings(Keys.IgnoredQueries)]
    public string IgnoredQueries { get; set; } = string.Empty;

    [ConfigurableProperty("Stale index after", "Hours since the newest instance build before an index is reported as stale.")]
    [Settings(Keys.StaleIndexHours, Defaults.StaleIndexHours)]
    public int StaleIndexHours { get; set; } = Defaults.StaleIndexHours;

    [ConfigurableProperty("Document row cap", "Most documents the document browser will ever read in one go.")]
    [Settings(Keys.DocumentRowsPerPage, Defaults.DocumentRowsPerPage)]
    public int DocumentRowsPerPage { get; set; } = Defaults.DocumentRowsPerPage;

    // ---- Operations --------------------------------------------------------------------------

    [ConfigurableProperty("Stale task tolerance", "How many of its own intervals a repeating task may miss before it is reported as stale (OPS-W2).")]
    [Settings(Keys.StaleTaskIntervalMultiplier, Defaults.StaleTaskIntervalMultiplier)]
    public int StaleTaskIntervalMultiplier { get; set; } = Defaults.StaleTaskIntervalMultiplier;

    [ConfigurableProperty("Log folder warning size", "A log folder at or above this size is reported (OPS-W6).")]
    [Settings(Keys.LogFolderWarningMb, Defaults.LogFolderWarningMb)]
    public int LogFolderWarningMb { get; set; } = Defaults.LogFolderWarningMb;

    [ConfigurableProperty("Log folder critical size", "Above this size the OPS-W6 finding is raised to critical.")]
    [Settings(Keys.LogFolderCriticalMb, Defaults.LogFolderCriticalMb)]
    public int LogFolderCriticalMb { get; set; } = Defaults.LogFolderCriticalMb;

    [ConfigurableProperty("Table share warning", "A table holding at least this share of the whole database is reported (OPS-W8).")]
    [Settings(Keys.TableSharePercent, Defaults.TableSharePercent)]
    public int TableSharePercent { get; set; } = Defaults.TableSharePercent;

    [ConfigurableProperty("Recent changes window", "Default number of days the recent-changes screen looks back.")]
    [Settings(Keys.RecentChangesDays, Defaults.RecentChangesDays)]
    public int RecentChangesDays { get; set; } = Defaults.RecentChangesDays;

    [ConfigurableProperty("Run history depth", "Runs listed on a scheduled task's detail screen.")]
    [Settings(Keys.RunHistoryDepth, Defaults.RunHistoryDepth)]
    public int RunHistoryDepth { get; set; } = Defaults.RunHistoryDepth;

    // ---- Price Explainer ----------------------------------------------------------------------

    [ConfigurableProperty("Product picker cap", "Products listed per search in the product picker.")]
    [Settings(Keys.ProductPickCap, Defaults.ProductPickCap)]
    public int ProductPickCap { get; set; } = Defaults.ProductPickCap;

    [ConfigurableProperty("Price row cap", "Price-matrix rows rendered in an explanation before the section is truncated.")]
    [Settings(Keys.PriceRowCap, Defaults.PriceRowCap)]
    public int PriceRowCap { get; set; } = Defaults.PriceRowCap;

    [ConfigurableProperty("Quantity presets", "Comma-separated quantities offered as context switches.")]
    [Settings(Keys.QuantityPresets, Defaults.QuantityPresets)]
    public string QuantityPresets { get; set; } = Defaults.QuantityPresets;

    [ConfigurableProperty("Date presets", "Comma-separated day offsets offered as date switches.")]
    [Settings(Keys.DatePresetDays, Defaults.DatePresetDays)]
    public string DatePresetDays { get; set; } = Defaults.DatePresetDays;

    [ConfigurableProperty("Default currency", "Currency code used when an explanation does not name one. Blank uses the DW default currency.")]
    [Settings(Keys.DefaultCurrencyCode)]
    public string DefaultCurrencyCode { get; set; } = string.Empty;

    // ---- Content Access Viewer -----------------------------------------------------------------

    [ConfigurableProperty("User fetch cap", "Users materialised per request in the account pickers.")]
    [Settings(Keys.UserFetchCap, Defaults.UserFetchCap)]
    public int UserFetchCap { get; set; } = Defaults.UserFetchCap;

    [ConfigurableProperty("Suppressed warning rules", "SECOPS rule ids never shown, one per line. A trailing * matches a prefix.")]
    [Settings(Keys.SuppressedWarningRules)]
    public string SuppressedWarningRules { get; set; } = string.Empty;

    [ConfigurableProperty("Hide administrator accounts", "Administrators bypass every permission check, so their rows explain nothing.")]
    [Settings(Keys.HideAdministrators, Defaults.HideAdministrators)]
    public bool HideAdministrators { get; set; } = Defaults.HideAdministrators;

    // ---- General ---------------------------------------------------------------------------------

    [ConfigurableProperty("Security section", "Show the Security section in the PowerTools area.")]
    [Settings(Keys.SecuritySectionEnabled, Defaults.SectionEnabled)]
    public bool SecuritySectionEnabled { get; set; } = Defaults.SectionEnabled;

    [ConfigurableProperty("Commerce section", "Show the Commerce section in the PowerTools area.")]
    [Settings(Keys.CommerceSectionEnabled, Defaults.SectionEnabled)]
    public bool CommerceSectionEnabled { get; set; } = Defaults.SectionEnabled;

    [ConfigurableProperty("Operations section", "Show the Operations section in the PowerTools area.")]
    [Settings(Keys.OperationsSectionEnabled, Defaults.SectionEnabled)]
    public bool OperationsSectionEnabled { get; set; } = Defaults.SectionEnabled;

    [ConfigurableProperty("Search section", "Show the Search section in the PowerTools area.")]
    [Settings(Keys.SearchSectionEnabled, Defaults.SectionEnabled)]
    public bool SearchSectionEnabled { get; set; } = Defaults.SectionEnabled;

    [ConfigurableProperty("Show rule ids", "Show the rule id column on the finding lists.")]
    [Settings(Keys.ShowRuleIds, Defaults.ShowRuleIds)]
    public bool ShowRuleIds { get; set; } = Defaults.ShowRuleIds;
}
