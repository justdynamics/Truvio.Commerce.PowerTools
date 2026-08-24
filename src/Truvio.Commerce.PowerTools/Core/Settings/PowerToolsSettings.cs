using System.Globalization;
using Truvio.Commerce.PowerTools.Core.Diagnostics;
using Shipped = Truvio.Commerce.PowerTools.Core.Settings.PowerToolsSettingKeys.Defaults;

namespace Truvio.Commerce.PowerTools.Core.Settings;

/// <summary>
/// The suite-wide settings, as a plain immutable value: every tool reads its thresholds, caps
/// and suppressions from here instead of from a hard-coded constant. Pure and DW-free so the
/// parsing and suppression rules are unit-tested without a host.
/// </summary>
public sealed record PowerToolsSettings
{
    /// <summary>The shipped configuration — what every tool behaves like on a fresh install.</summary>
    public static PowerToolsSettings Defaults { get; } = new();

    // ---- Query linter -----------------------------------------------------------------------
    /// <summary>Rule ids never shown, one per line. A trailing '*' matches a prefix (IDX-W1*).</summary>
    public string IgnoredRules { get; init; } = string.Empty;

    /// <summary>Query parameter names whose IDX-W1/IDX-W2 findings are noise here (e.g. "eq, q").</summary>
    public string IgnoredParameters { get; init; } = string.Empty;

    /// <summary>Query names ("ProductSearch") or repository-qualified names, one per line.</summary>
    public string IgnoredQueries { get; init; } = string.Empty;

    /// <summary>Hours after the newest instance build before an index counts as stale.</summary>
    public int StaleIndexHours { get; init; } = Shipped.StaleIndexHours;

    /// <summary>Documents read per page in the document browser.</summary>
    public int DocumentRowsPerPage { get; init; } = Shipped.DocumentRowsPerPage;

    // ---- Operations -------------------------------------------------------------------------
    /// <summary>How many of its own intervals a repeating task may miss before OPS-W2 fires.</summary>
    public int StaleTaskIntervalMultiplier { get; init; } = Shipped.StaleTaskIntervalMultiplier;

    public int LogFolderWarningMb { get; init; } = Shipped.LogFolderWarningMb;

    public int LogFolderCriticalMb { get; init; } = Shipped.LogFolderCriticalMb;

    /// <summary>Share of the whole database, in percent, above which a table is reported (OPS-W8).</summary>
    public int TableSharePercent { get; init; } = Shipped.TableSharePercent;

    public int RecentChangesDays { get; init; } = Shipped.RecentChangesDays;

    /// <summary>Runs listed on a scheduled task's detail screen.</summary>
    public int RunHistoryDepth { get; init; } = Shipped.RunHistoryDepth;

    // ---- Price Explainer --------------------------------------------------------------------
    public int ProductPickCap { get; init; } = Shipped.ProductPickCap;

    /// <summary>Price-matrix rows rendered before the report truncates the section.</summary>
    public int PriceRowCap { get; init; } = Shipped.PriceRowCap;

    public string QuantityPresets { get; init; } = Shipped.QuantityPresets;

    public string DatePresetDays { get; init; } = Shipped.DatePresetDays;

    /// <summary>Currency used when the explanation does not name one; blank = the DW default.</summary>
    public string DefaultCurrencyCode { get; init; } = string.Empty;

    /// <summary>Percent a configured exchange rate may deviate from the evidence before CUR-W2/CUR-W3 fire.</summary>
    public int RateDeviationPercent { get; init; } = Shipped.RateDeviationPercent;

    /// <summary>Compare configured rates against the live reference feed (the suite's only outbound call). Off by default.</summary>
    public bool LiveRateCheckEnabled { get; init; } = Shipped.LiveRateCheckEnabled;

    /// <summary>Override for the reference feed URL; blank = the ECB daily reference rates.</summary>
    public string LiveRateFeedUrl { get; init; } = string.Empty;

    // ---- PIM quality ------------------------------------------------------------------------
    /// <summary>Most products scored per page in the PIM screens.</summary>
    public int PimProductCap { get; init; } = Shipped.PimProductCap;

    /// <summary>Completeness score below which PIM-W1 reports a product.</summary>
    public int PimCompletenessThreshold { get; init; } = Shipped.PimCompletenessThreshold;

    /// <summary>Percent of scanned products missing the same field before PIM-W2 calls it a common gap.</summary>
    public int PimCommonGapPercent { get; init; } = Shipped.PimCommonGapPercent;

    /// <summary>PIM rule ids never shown, one per line. A trailing '*' matches a prefix.</summary>
    public string PimSuppressedRules { get; init; } = string.Empty;

    // ---- Content Access Viewer --------------------------------------------------------------
    public int UserFetchCap { get; init; } = Shipped.UserFetchCap;

    /// <summary>SECOPS rule ids never shown on the Content Access Warnings screen.</summary>
    public string SuppressedWarningRules { get; init; } = string.Empty;

    /// <summary>Administrator accounts bypass every check, so they are noise in the pickers.</summary>
    public bool HideAdministrators { get; init; } = Shipped.HideAdministrators;

    // ---- General ----------------------------------------------------------------------------
    public bool SecuritySectionEnabled { get; init; } = Shipped.SectionEnabled;

    public bool PimSectionEnabled { get; init; } = Shipped.SectionEnabled;

    public bool CommerceSectionEnabled { get; init; } = Shipped.SectionEnabled;

    public bool OperationsSectionEnabled { get; init; } = Shipped.SectionEnabled;

    public bool SearchSectionEnabled { get; init; } = Shipped.SectionEnabled;

    /// <summary>Show the rule id column on finding lists.</summary>
    public bool ShowRuleIds { get; init; } = Shipped.ShowRuleIds;

    // ---- Parsing helpers ---------------------------------------------------------------------

    /// <summary>
    /// Splits a free-text list setting. Editors produce newlines, humans type commas, and the
    /// XML config store may fold whitespace, so every one of those separates entries. Rule ids,
    /// parameter names and numbers never contain whitespace, so this is lossless for them.
    /// </summary>
    public static IReadOnlyList<string> SplitList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        return value
            .Split([',', ';', '\n', '\r', '\t', ' ', '|'], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>A token matches a value literally, or as a prefix when it ends in '*'.</summary>
    internal static bool TokenMatches(string token, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return token.EndsWith('*')
            ? value.StartsWith(token[..^1], StringComparison.OrdinalIgnoreCase)
            : string.Equals(token, value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool AnyMatch(string setting, string? value) =>
        SplitList(setting).Any(token => TokenMatches(token, value));

    public bool IsRuleIgnored(string? ruleId) => AnyMatch(IgnoredRules, ruleId);

    public bool IsParameterIgnored(string? parameterName) => AnyMatch(IgnoredParameters, parameterName);

    public bool IsWarningRuleSuppressed(string? ruleId) => AnyMatch(SuppressedWarningRules, ruleId);

    /// <summary>
    /// True when the finding belongs to a query the admin muted. Search findings carry the
    /// query as "Name (Repository)" in the display name and "Repository/Item" in the key, so a
    /// bare name, the qualified key, or the whole display string all work as a token.
    /// </summary>
    public bool IsQueryIgnored(string? entityKey, string? entityDisplayName)
    {
        foreach (var token in SplitList(IgnoredQueries))
        {
            if (TokenMatches(token, entityKey) || TokenMatches(token, entityDisplayName))
                return true;

            // "Products (Default)" — match on the leading name alone.
            if (entityDisplayName is not null &&
                entityDisplayName.StartsWith(token + " (", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>Quantity switches offered by the Price Explainer, in the order given.</summary>
    public IReadOnlyList<double> Quantities() => Numbers(QuantityPresets, Shipped.QuantityPresets)
        .Where(n => n > 0)
        .ToList();

    /// <summary>Day offsets offered as date switches by the Price Explainer.</summary>
    public IReadOnlyList<int> DateOffsets() => Numbers(DatePresetDays, Shipped.DatePresetDays)
        .Where(n => n > 0)
        .Select(n => (int)n)
        .ToList();

    private static IReadOnlyList<double> Numbers(string value, string fallback)
    {
        var parsed = Parse(value);
        return parsed.Count > 0 || string.IsNullOrEmpty(fallback) ? parsed : Parse(fallback);

        static List<double> Parse(string raw) => SplitList(raw)
            .Select(part => double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : double.NaN)
            .Where(n => !double.IsNaN(n))
            .ToList();
    }

    /// <summary>A positive value, or the shipped default when the stored one is nonsense (0, negative).</summary>
    public static int Positive(int value, int fallback) => value > 0 ? value : fallback;

    // ---- Suppression ---------------------------------------------------------------------------

    /// <summary>
    /// Drops the findings the admin muted, and reports how many went — suppression is never
    /// silent, every screen shows the hidden count.
    /// </summary>
    public FindingFilter FilterSearchFindings(IEnumerable<Finding> findings)
    {
        var all = findings as IReadOnlyList<Finding> ?? findings.ToList();
        var visible = all.Where(f => !IsSuppressedSearchFinding(f)).ToList();
        return new FindingFilter(visible, all.Count - visible.Count);
    }

    /// <summary>Same, for the content-permission (SECOPS-*) findings.</summary>
    public FindingFilter FilterWarningFindings(IEnumerable<Finding> findings)
    {
        var all = findings as IReadOnlyList<Finding> ?? findings.ToList();
        var visible = all.Where(f => !IsWarningRuleSuppressed(f.RuleId)).ToList();
        return new FindingFilter(visible, all.Count - visible.Count);
    }

    public bool IsPimRuleSuppressed(string? ruleId) => AnyMatch(PimSuppressedRules, ruleId);

    /// <summary>Same, for the PIM (PIM-*) findings.</summary>
    public FindingFilter FilterPimFindings(IEnumerable<Finding> findings)
    {
        var all = findings as IReadOnlyList<Finding> ?? findings.ToList();
        var visible = all.Where(f => !IsPimRuleSuppressed(f.RuleId)).ToList();
        return new FindingFilter(visible, all.Count - visible.Count);
    }

    private bool IsSuppressedSearchFinding(Finding finding)
    {
        if (IsRuleIgnored(finding.RuleId))
            return true;

        if (IsQueryIgnored(finding.EntityKey, finding.EntityDisplayName))
            return true;

        // Parameter suppression only applies to findings that name the parameters they are
        // about; a finding with no subject is never dropped by accident.
        var subjects = SplitList(finding.Subject);
        return subjects.Count > 0 && subjects.All(IsParameterIgnored);
    }
}

/// <summary>What a screen shows after suppression, and how much it is not showing.</summary>
public sealed record FindingFilter(IReadOnlyList<Finding> Visible, int HiddenCount)
{
    /// <summary>The line every finding screen renders when something was hidden.</summary>
    public string HiddenNotice() =>
        $"{HiddenCount} finding{(HiddenCount == 1 ? "" : "s")} hidden by settings";
}
