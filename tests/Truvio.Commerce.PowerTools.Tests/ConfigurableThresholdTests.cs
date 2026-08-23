using Truvio.Commerce.PowerTools.Core.Operations;
using Truvio.Commerce.PowerTools.Core.Operations.Rules;
using Truvio.Commerce.PowerTools.Core.Settings;
using Xunit;
using static Truvio.Commerce.PowerTools.Tests.OperationsTestData;

namespace Truvio.Commerce.PowerTools.Tests;

/// <summary>
/// The thresholds the settings screen exposes really do change what the rules report — and a
/// nonsense value falls back to the shipped default instead of disabling the rule.
/// </summary>
public class ConfigurableThresholdTests
{
    private const long Mb = 1024L * 1024L;

    // ---- OPS-W2 stale task -------------------------------------------------------------

    [Fact]
    public void Stale_task_tolerance_widens_the_window()
    {
        // Runs hourly, last ran 3 hours ago: stale at 2x, fine at 4x.
        var snapshot = Snapshot(tasks: [Task(intervalMinutes: 60, lastRun: Now.AddHours(-3))]);

        Assert.Single(new StaleTaskRule(2).Evaluate(snapshot));
        Assert.Empty(new StaleTaskRule(4).Evaluate(snapshot));
    }

    [Fact]
    public void Stale_task_tolerance_falls_back_when_stored_as_zero()
    {
        var snapshot = Snapshot(tasks: [Task(intervalMinutes: 60, lastRun: Now.AddHours(-3))]);

        Assert.Single(new StaleTaskRule(0).Evaluate(snapshot));
    }

    // ---- OPS-W6 log folder size ---------------------------------------------------------

    [Fact]
    public void Log_folder_thresholds_decide_what_is_reported_and_how_loudly()
    {
        var snapshot = Snapshot(folders: [Folder(bytes: 300 * Mb)]);

        Assert.Empty(new LogGrowthRule(500 * Mb, 2048 * Mb).Evaluate(snapshot));

        var finding = Assert.Single(new LogGrowthRule(100 * Mb, 2048 * Mb).Evaluate(snapshot));
        Assert.Equal(LogGrowthRule.SizeId, finding.RuleId);
        Assert.Equal(Core.Diagnostics.FindingSeverity.Warning, finding.Severity);

        var critical = Assert.Single(new LogGrowthRule(100 * Mb, 200 * Mb).Evaluate(snapshot));
        Assert.Equal(Core.Diagnostics.FindingSeverity.Critical, critical.Severity);
    }

    // ---- OPS-W8 table share --------------------------------------------------------------

    [Fact]
    public void Table_share_threshold_decides_when_a_table_is_too_big_a_slice()
    {
        // 30 MB of a 100 MB database = 30%.
        var snapshot = Snapshot(
            tables: [Table("EcomProducts", rows: 10, bytes: 30 * Mb), Table("Other", rows: 10, bytes: 70 * Mb)],
            databaseBytes: 100 * Mb);

        Assert.Contains(new TableBloatRule(0.25).Evaluate(snapshot), Products);
        Assert.DoesNotContain(new TableBloatRule(0.50).Evaluate(snapshot), Products);

        static bool Products(Core.Diagnostics.Finding f) =>
            f.RuleId == TableBloatRule.ShareId && f.EntityKey == "EcomProducts";
    }

    [Fact]
    public void Table_share_threshold_falls_back_for_an_impossible_value()
    {
        var snapshot = Snapshot(
            tables: [Table("EcomProducts", rows: 10, bytes: 30 * Mb)],
            databaseBytes: 100 * Mb);

        // 0 and 500% are both nonsense: the shipped 25% applies.
        Assert.Contains(new TableBloatRule(0).Evaluate(snapshot), f => f.RuleId == TableBloatRule.ShareId);
        Assert.Contains(new TableBloatRule(5).Evaluate(snapshot), f => f.RuleId == TableBloatRule.ShareId);
    }

    // ---- the engine wires settings onto the rules ---------------------------------------------

    [Fact]
    public void The_engine_hands_the_configured_thresholds_to_its_rules()
    {
        var settings = new PowerToolsSettings
        {
            StaleTaskIntervalMultiplier = 4,
            LogFolderWarningMb = 100,
            LogFolderCriticalMb = 200,
            TableSharePercent = 50
        };

        var snapshot = Snapshot(
            tasks: [Task(intervalMinutes: 60, lastRun: Now.AddHours(-3))],
            folders: [Folder(bytes: 300 * Mb)],
            tables: [Table("EcomProducts", rows: 10, bytes: 30 * Mb)],
            databaseBytes: 100 * Mb);

        var findings = new OperationsHealthEngine(settings).Run(snapshot);

        Assert.DoesNotContain(findings, f => f.RuleId == StaleTaskRule.StaleId);
        Assert.Contains(findings, f => f.RuleId == LogGrowthRule.SizeId && f.Severity == Core.Diagnostics.FindingSeverity.Critical);
        Assert.DoesNotContain(findings, f => f.RuleId == TableBloatRule.ShareId);
    }

    [Fact]
    public void Default_settings_reproduce_the_shipped_engine()
    {
        var snapshot = Snapshot(
            tasks: [Task(intervalMinutes: 60, lastRun: Now.AddHours(-3))],
            folders: [Folder(bytes: 600 * Mb)]);

        var shipped = new OperationsHealthEngine().Run(snapshot).Select(f => f.RuleId).ToList();
        var configured = new OperationsHealthEngine(PowerToolsSettings.Defaults).Run(snapshot).Select(f => f.RuleId).ToList();

        Assert.Equal(shipped, configured);
    }
}
