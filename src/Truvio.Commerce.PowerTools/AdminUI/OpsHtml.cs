using System.Net;
using System.Text;
using Truvio.Commerce.PowerTools.AdminUI.Models;

namespace Truvio.Commerce.PowerTools.AdminUI;

/// <summary>
/// Renders the Operations report tables. Report screens use <c>HtmlBlock</c> rather than the
/// list grid on purpose: the grid splits its width evenly between columns and clips long text,
/// and every one of these tables has one narrow label column and one long explanation column.
/// </summary>
internal static class OpsHtml
{
    /// <summary>A label / badge / value+explanation table, matching the Price Explainer report.</summary>
    public static string Table(IReadOnlyList<OpsRowModel> rows)
    {
        if (rows.Count == 0)
            return Note("Nothing to show.");

        var sb = new StringBuilder();
        // The group renders its heading with ~1.5rem inset but the component flush to the edge.
        sb.Append("<div style=\"padding:0 1.5rem .75rem 1.5rem\">");
        sb.Append("<table style=\"width:100%;border-collapse:collapse;font-size:inherit\">");
        foreach (var row in rows)
        {
            var badge = string.IsNullOrEmpty(row.Verdict)
                ? string.Empty
                : $"<span style=\"display:inline-block;padding:1px 8px;border-radius:10px;font-size:0.85em;white-space:nowrap;{BadgeStyle(row.VerdictKind)}\">{E(row.Verdict)}</span>";

            var detail = (row.Value, row.Why) switch
            {
                ("", "") => string.Empty,
                (_, "") => $"<strong>{E(row.Value)}</strong>",
                ("", _) => E(row.Why),
                _ => $"<strong>{E(row.Value)}</strong> <span style=\"opacity:.8\">&mdash; {E(row.Why)}</span>"
            };

            sb.Append("<tr style=\"border-bottom:1px solid rgba(0,0,0,.06)\">")
              .Append($"<td style=\"padding:6px 8px 6px 0;vertical-align:top;white-space:nowrap;width:1%\">{E(row.Item)}</td>")
              .Append($"<td style=\"padding:6px 8px;vertical-align:top;white-space:nowrap;width:1%\">{badge}</td>")
              .Append($"<td style=\"padding:6px 0 6px 8px;vertical-align:top;white-space:normal\">{detail}</td>")
              .Append("</tr>");
        }
        sb.Append("</table></div>");
        return sb.ToString();
    }

    /// <summary>A fixed-width block for log tails and exception text.</summary>
    public static string Pre(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
            return Note("No log lines were found.");

        var sb = new StringBuilder();
        sb.Append("<div style=\"padding:0 1.5rem .75rem 1.5rem\">");
        sb.Append("<pre style=\"margin:0;padding:.75rem;max-height:26rem;overflow:auto;background:rgba(0,0,0,.04);border-radius:4px;font-size:.85em;white-space:pre-wrap;word-break:break-word\">");
        sb.Append(E(string.Join('\n', lines)));
        sb.Append("</pre></div>");
        return sb.ToString();
    }

    public static string Note(string text) =>
        $"<div style=\"padding:0 1.5rem .75rem 1.5rem;opacity:.75\">{E(text)}</div>";

    private static string BadgeStyle(string kind) => kind switch
    {
        "win" or "ok" => "background:#d4edda;color:#155724",
        "match" => "background:#d1ecf1;color:#0c5460",
        "reject" or "hidden" => "background:#f8d7da;color:#721c24",
        "warn" => "background:#fff3cd;color:#856404",
        "info" => "background:#e2e3f3;color:#383d6b",
        _ => "background:#e9ecef;color:#495057"
    };

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
