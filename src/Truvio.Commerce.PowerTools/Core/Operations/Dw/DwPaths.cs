using Dynamicweb.Configuration;
using Dynamicweb.Core;

namespace Truvio.Commerce.PowerTools.Core.Operations.Dw;

/// <summary>
/// The handful of DW file locations the Operations tools read. All of them are derived the
/// same way DW derives them, so a renamed files folder or a relocated root still resolves.
/// </summary>
internal static class DwPaths
{
    /// <summary>
    /// DW's own files-folder name, from
    /// <c>/Globalsettings/System/Filesystem/FilesFolderName</c> — the same lookup
    /// <c>Dynamicweb.Scheduling.Task.GetFilesFolderName()</c> performs, defaulting to "Files".
    /// </summary>
    public static string FilesFolderName
    {
        get
        {
            try
            {
                var configured = SystemConfiguration.Instance.GetValue("/Globalsettings/System/Filesystem/FilesFolderName");
                return string.IsNullOrEmpty(configured) ? "Files" : configured;
            }
            catch
            {
                return "Files";
            }
        }
    }

    /// <summary>Physical path of a DW-relative path, or empty when it cannot be mapped.</summary>
    public static string Map(string relativePath)
    {
        try
        {
            return SystemInformation.MapPath(relativePath);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>Root of the file logs: <c>/Files/System/Log</c> (DW's <c>LogPathPrefix</c>).</summary>
    public const string LogRootRelative = "/Files/System/Log";

    /// <summary>Diagnostics counterpart, the second default log-retention location.</summary>
    public const string DiagnosticsRootRelative = "/Files/System/Diagnostics";

    /// <summary>Where the scheduler writes its logs (<c>TaskService.GetLastExecutionsLogs</c>).</summary>
    public const string ScheduledTaskLogRelative = "/Files/System/Log/ScheduledTasks";

    /// <summary>Where data-integration runs write their logs (<c>Job.LogFileRelativePath</c>).</summary>
    public const string DataIntegrationLogRelative = "/Files/System/Log/DataIntegration";

    /// <summary>
    /// Where activity (job) definitions live — <c>Job.JobFolder</c> is
    /// <c>MapPath("/Files/{filesFolder}/Integration/jobs/")</c>.
    /// </summary>
    public static string ActivityFolderRelative => $"/Files/{FilesFolderName}/Integration/jobs";

    /// <summary>Where XML-defined scheduled tasks live (<c>Task.XmlFolder</c>).</summary>
    public static string TaskXmlFolderRelative => $"/Files/{FilesFolderName}/Integration/ScheduledTasks";

    /// <summary>The main settings file, whose timestamp tells when the install was last reconfigured.</summary>
    public const string GlobalSettingsRelative = "/Files/GlobalSettings.config";
}
