namespace Truvio.Commerce.PowerTools.Core.Settings;

/// <summary>
/// Every GlobalSettings key the suite owns, plus its default. The suite writes nowhere else:
/// all keys live under <c>/Globalsettings/Truvio/PowerTools/</c>, so removing that one node
/// from <c>GlobalSettings.config</c> resets the whole product.
/// <para>
/// The paths are compile-time constants because the settings edit model puts them in
/// <c>[Settings(path, default)]</c> attributes (DW's <c>Dynamicweb.Extensibility.Settings</c>
/// pattern), and the read-side adapter uses the same constants — one definition, two users.
/// </para>
/// </summary>
public static class PowerToolsSettingKeys
{
    public const string Root = "/Globalsettings/Truvio/PowerTools";

    // ---- Query linter / Index & Query Inspector ------------------------------------------
    public const string IgnoredRules = Root + "/Search/IgnoredRules";
    public const string IgnoredParameters = Root + "/Search/IgnoredParameters";
    public const string IgnoredQueries = Root + "/Search/IgnoredQueries";
    public const string StaleIndexHours = Root + "/Search/StaleIndexHours";
    public const string DocumentRowsPerPage = Root + "/Search/DocumentRowsPerPage";

    // ---- Operations ------------------------------------------------------------------------
    public const string StaleTaskIntervalMultiplier = Root + "/Operations/StaleTaskIntervalMultiplier";
    public const string LogFolderWarningMb = Root + "/Operations/LogFolderWarningMb";
    public const string LogFolderCriticalMb = Root + "/Operations/LogFolderCriticalMb";
    public const string TableSharePercent = Root + "/Operations/TableSharePercent";
    public const string RecentChangesDays = Root + "/Operations/RecentChangesDays";
    public const string RunHistoryDepth = Root + "/Operations/RunHistoryDepth";

    // ---- Price Explainer ---------------------------------------------------------------------
    public const string ProductPickCap = Root + "/Commerce/ProductPickCap";
    public const string PriceRowCap = Root + "/Commerce/PriceRowCap";
    public const string QuantityPresets = Root + "/Commerce/QuantityPresets";
    public const string DatePresetDays = Root + "/Commerce/DatePresetDays";
    public const string DefaultCurrencyCode = Root + "/Commerce/DefaultCurrencyCode";

    // ---- Content Access Viewer ----------------------------------------------------------------
    public const string UserFetchCap = Root + "/Security/UserFetchCap";
    public const string SuppressedWarningRules = Root + "/Security/SuppressedWarningRules";
    public const string HideAdministrators = Root + "/Security/HideAdministrators";

    // ---- General --------------------------------------------------------------------------------
    public const string SecuritySectionEnabled = Root + "/General/SecuritySectionEnabled";
    public const string CommerceSectionEnabled = Root + "/General/CommerceSectionEnabled";
    public const string OperationsSectionEnabled = Root + "/General/OperationsSectionEnabled";
    public const string SearchSectionEnabled = Root + "/General/SearchSectionEnabled";
    public const string ShowRuleIds = Root + "/General/ShowRuleIds";

    /// <summary>
    /// The shipped defaults. Constants (not readonly fields) because the settings edit model
    /// needs them inside attributes.
    /// </summary>
    public static class Defaults
    {
        public const int StaleIndexHours = 24;
        public const int DocumentRowsPerPage = 50;

        public const int StaleTaskIntervalMultiplier = 2;
        public const int LogFolderWarningMb = 500;
        public const int LogFolderCriticalMb = 2048;
        public const int TableSharePercent = 25;
        public const int RecentChangesDays = 30;
        public const int RunHistoryDepth = 20;

        public const int ProductPickCap = 200;
        public const int PriceRowCap = 100;
        public const string QuantityPresets = "1,5,10,25,50,100,500";
        public const string DatePresetDays = "7,30,90";

        public const int UserFetchCap = 500;

        public const bool SectionEnabled = true;
        public const bool ShowRuleIds = true;
        public const bool HideAdministrators = false;
    }
}
