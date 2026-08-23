using Truvio.Commerce.PowerTools.Core.Operations;

namespace Truvio.Commerce.PowerTools.Tests;

/// <summary>Builders for Operations specs so each test states only what it cares about.</summary>
internal static class OperationsTestData
{
    public static readonly DateTime Now = new(2026, 8, 23, 12, 0, 0, DateTimeKind.Unspecified);

    public static TaskSpec Task(
        int id = 1,
        string name = "Nightly import",
        string addInType = "Dynamicweb.DataIntegration.Integration.JobScheduledTaskAddIn, Dynamicweb.DataIntegration",
        bool enabled = true,
        int intervalMinutes = 60,
        DateTime? lastRun = null,
        bool? lastResult = true,
        string lastException = "",
        string linkedActivityId = "") =>
        new(
            id,
            name,
            addInType,
            enabled,
            intervalMinutes,
            "begin: 01 Jan 2026 00:00, repeat every 60 minutes",
            lastRun,
            lastRun?.AddMinutes(intervalMinutes),
            lastResult,
            lastException,
            linkedActivityId,
            Comment: string.Empty);

    public static ActivitySpec Activity(
        string name = "Import Customers",
        string group = "",
        DateTime? lastRun = null,
        string lastResult = "Completed") =>
        new(
            Id: string.IsNullOrEmpty(group) ? name : $"{group}\\{name}",
            Name: name,
            Group: group,
            Description: "",
            SourceProvider: "Dynamicweb.DataIntegration.Providers.XmlProvider.XmlProvider",
            DestinationProvider: "Dynamicweb.DataIntegration.Providers.UserProvider.UserProvider",
            TableCount: 1,
            MappingCount: 1,
            ColumnMappingCount: 6,
            LastRun: lastRun,
            LastResult: lastResult,
            LastDuration: TimeSpan.FromSeconds(12),
            ModifiedAt: Now.AddDays(-3));

    public static StorageFolderSpec Folder(
        string name = "System/Log/ScheduledTasks",
        long bytes = 1024,
        int fileCount = 3,
        DateTime? oldest = null,
        DateTime? newest = null) =>
        new(name, "/Files/" + name, bytes, fileCount, oldest ?? Now.AddDays(-1), newest ?? Now);

    public static TableSizeSpec Table(string name = "CommandLog", long rows = 1000, long bytes = 1024) =>
        new(name, rows, bytes);

    public static OperationsSnapshot Snapshot(
        IReadOnlyList<TaskSpec>? tasks = null,
        IReadOnlyList<ActivitySpec>? activities = null,
        IReadOnlyList<StorageFolderSpec>? folders = null,
        IReadOnlyList<TableSizeSpec>? tables = null,
        RetentionSpec? retention = null,
        long databaseBytes = 0) =>
        new(
            tasks ?? [],
            activities ?? [],
            folders ?? [],
            tables ?? [],
            retention ?? RetentionSpec.Unknown,
            Now,
            databaseBytes);
}
