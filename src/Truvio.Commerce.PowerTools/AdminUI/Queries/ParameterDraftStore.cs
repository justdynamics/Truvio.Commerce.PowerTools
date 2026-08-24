using System.Collections.Concurrent;
using System.Globalization;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// The parameter values the "Set parameters" dialog has saved, per backend user and query.
/// Needed because a command result cannot carry values into the next screen URL: the dialog's
/// OK command writes here, then navigates to the report with <c>UseDraft=true</c>, and the
/// report reads the set back. Every link the report renders carries the resolved values
/// explicitly, so anything copied or clicked from there is frozen and shareable.
/// In-memory and per process on purpose: a draft is scratch state for one person mid-test.
/// </summary>
internal static class ParameterDraftStore
{
    private static readonly ConcurrentDictionary<string, string> Drafts = new(StringComparer.Ordinal);

    public static string Get(string repository, string item) =>
        Drafts.TryGetValue(Key(repository, item), out var value) ? value : string.Empty;

    public static void Set(string repository, string item, string parameters) =>
        Drafts[Key(repository, item)] = parameters ?? string.Empty;

    private static string Key(string repository, string item)
    {
        string user;
        try
        {
            user = Dynamicweb.Security.UserManagement.User.GetCurrentBackendUser()?.ID.ToString(CultureInfo.InvariantCulture) ?? "-";
        }
        catch
        {
            user = "-";
        }

        return $"{user}|{repository}|{item}";
    }
}
