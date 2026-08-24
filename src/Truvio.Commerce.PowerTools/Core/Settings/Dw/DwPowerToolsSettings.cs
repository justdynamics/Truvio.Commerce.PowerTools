using Dynamicweb.Configuration;
using Keys = Truvio.Commerce.PowerTools.Core.Settings.PowerToolsSettingKeys;

namespace Truvio.Commerce.PowerTools.Core.Settings.Dw;

/// <summary>
/// Reads the suite's settings out of DW's GlobalSettings.
/// <para>
/// Read-only on purpose: <c>SystemConfiguration.Instance</c> is a process-wide singleton whose
/// providers hold the parsed <c>/Files/*.config</c> documents in a
/// <c>ConcurrentDictionary</c> (verified: <c>Dynamicweb.Configuration.XmlConfigurationProvider.TryGet</c>),
/// so every read here is a dictionary lookup — no caching layer is worth its staleness. The
/// only writer in the suite is the settings save command.
/// </para>
/// <para>
/// Note the deliberate difference from DW's own <c>SettingsService.Load</c>: that helper
/// *writes* the declared default back into the configuration file for any missing key
/// (<c>EnsureDefaultValue</c>). Every tool in the suite reads settings on render, so this
/// reader falls back in memory instead and never touches the file.
/// </para>
/// </summary>
public static class DwPowerToolsSettings
{
    /// <summary>The settings as configured right now, defaults filling in whatever is unset.</summary>
    public static PowerToolsSettings Current
    {
        get
        {
            try
            {
                return Read();
            }
            catch
            {
                // A tool must still work when the configuration cannot be read.
                return PowerToolsSettings.Defaults;
            }
        }
    }

    private static PowerToolsSettings Read()
    {
        var d = PowerToolsSettings.Defaults;

        return new PowerToolsSettings
        {
            IgnoredRules = Str(Keys.IgnoredRules, d.IgnoredRules),
            IgnoredParameters = Str(Keys.IgnoredParameters, d.IgnoredParameters),
            IgnoredQueries = Str(Keys.IgnoredQueries, d.IgnoredQueries),
            StaleIndexHours = Int(Keys.StaleIndexHours, d.StaleIndexHours),
            DocumentRowsPerPage = Int(Keys.DocumentRowsPerPage, d.DocumentRowsPerPage),

            StaleTaskIntervalMultiplier = Int(Keys.StaleTaskIntervalMultiplier, d.StaleTaskIntervalMultiplier),
            LogFolderWarningMb = Int(Keys.LogFolderWarningMb, d.LogFolderWarningMb),
            LogFolderCriticalMb = Int(Keys.LogFolderCriticalMb, d.LogFolderCriticalMb),
            TableSharePercent = Int(Keys.TableSharePercent, d.TableSharePercent),
            RecentChangesDays = Int(Keys.RecentChangesDays, d.RecentChangesDays),
            RunHistoryDepth = Int(Keys.RunHistoryDepth, d.RunHistoryDepth),

            ProductPickCap = Int(Keys.ProductPickCap, d.ProductPickCap),
            PriceRowCap = Int(Keys.PriceRowCap, d.PriceRowCap),
            QuantityPresets = Str(Keys.QuantityPresets, d.QuantityPresets),
            DatePresetDays = Str(Keys.DatePresetDays, d.DatePresetDays),
            DefaultCurrencyCode = Str(Keys.DefaultCurrencyCode, d.DefaultCurrencyCode).Trim(),
            RateDeviationPercent = Int(Keys.RateDeviationPercent, d.RateDeviationPercent),
            LiveRateCheckEnabled = Bool(Keys.LiveRateCheckEnabled, d.LiveRateCheckEnabled),
            LiveRateFeedUrl = Str(Keys.LiveRateFeedUrl, d.LiveRateFeedUrl).Trim(),

            UserFetchCap = Int(Keys.UserFetchCap, d.UserFetchCap),
            SuppressedWarningRules = Str(Keys.SuppressedWarningRules, d.SuppressedWarningRules),
            HideAdministrators = Bool(Keys.HideAdministrators, d.HideAdministrators),

            SecuritySectionEnabled = Bool(Keys.SecuritySectionEnabled, d.SecuritySectionEnabled),
            CommerceSectionEnabled = Bool(Keys.CommerceSectionEnabled, d.CommerceSectionEnabled),
            OperationsSectionEnabled = Bool(Keys.OperationsSectionEnabled, d.OperationsSectionEnabled),
            SearchSectionEnabled = Bool(Keys.SearchSectionEnabled, d.SearchSectionEnabled),
            ShowRuleIds = Bool(Keys.ShowRuleIds, d.ShowRuleIds)
        };
    }

    private static SystemConfiguration Config => SystemConfiguration.Instance;

    private static string Str(string key, string fallback)
    {
        var value = Config.GetValue(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    /// <summary>
    /// A missing or blank key means "unset", not zero — GetInt32 returns 0 for both, so the
    /// raw string decides. A stored 0 or a negative is treated as unset by the callers via
    /// <see cref="PowerToolsSettings.Positive"/>.
    /// </summary>
    private static int Int(string key, int fallback)
    {
        var raw = Config.GetValue(key);
        return string.IsNullOrWhiteSpace(raw) ? fallback : Config.GetInt32(key);
    }

    private static bool Bool(string key, bool fallback)
    {
        var raw = Config.GetValue(key);
        return string.IsNullOrWhiteSpace(raw) ? fallback : Config.GetBoolean(key);
    }
}
