using System.Collections.Concurrent;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// Bridges a slide-over selection into the next screen. The picked id reaches the server as
/// a command value (that is the selector's client contract), and a command result cannot put
/// it into the follow-up URL — so the toolbar button embeds one render-time token in BOTH the
/// pick command and the fixed navigate-on-success URL; the command stores the picked id under
/// the token and the target query reads it back. DW's own product screens use the same
/// cache-key pattern. Entries are tiny and kept (a browser reload of the target URL must
/// still resolve); the dictionary is per process.
/// </summary>
internal static class PickStore
{
    private static readonly ConcurrentDictionary<string, string> Picks = new(StringComparer.Ordinal);

    public static string Get(string token) =>
        !string.IsNullOrEmpty(token) && Picks.TryGetValue(token, out var value) ? value : string.Empty;

    public static void Set(string token, string value)
    {
        if (string.IsNullOrEmpty(token))
            return;

        // A runaway process would otherwise accumulate forever; 1000 picks is far beyond any session.
        if (Picks.Count > 1000)
            Picks.Clear();

        Picks[token] = value ?? string.Empty;
    }
}
