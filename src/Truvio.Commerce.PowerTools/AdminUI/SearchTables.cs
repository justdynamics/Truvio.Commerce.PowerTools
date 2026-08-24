using System.Net;
using System.Text;

namespace Truvio.Commerce.PowerTools.AdminUI;

/// <summary>
/// Small HTML table renderer for the search overview screens. The CoreUI list grid gives every
/// column the same width and clips long text, so schema tables, builder settings and document
/// dumps are rendered as HTML blocks inside an overview screen instead.
/// </summary>
internal static class SearchTables
{
    /// <summary>A cell that may carry a coloured pill instead of plain text.</summary>
    internal readonly record struct Pill(string Text, string Kind)
    {
        public static Pill None => new(string.Empty, string.Empty);
    }

    /// <summary>
    /// A cell whose text is allowed to wrap. Every column but the last is <c>nowrap</c> by
    /// default (that keeps narrow key columns readable), which pushes a table wider than its
    /// card when a leading column holds long text — a clause like
    /// <c>ProductCategory|electronic_engine_system|battery_effect In @Battery_Effect</c> does
    /// exactly that. Wrap such a cell instead.
    /// </summary>
    internal readonly record struct Wrap(string Text);

    /// <summary>A cell rendered as a plain link — used for drill-downs out of a report table.</summary>
    internal readonly record struct Link(string Text, string Href);

    /// <summary>Search-highlighting lines: "Field: …before <mark>term</mark> after…".</summary>
    internal readonly record struct Snippets(IReadOnlyList<(string Field, string Before, string Match, string After)> Hits);

    /// <summary>Renders a header row plus body rows; the first column hugs its content.</summary>
    public static string Table(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
    {
        var sb = new StringBuilder();
        // The group renders its heading with ~1.5rem inset but the component flush to the edge.
        sb.Append("<div style=\"padding:0 1.5rem .75rem 1.5rem;overflow-x:auto\">");
        sb.Append("<table style=\"width:100%;border-collapse:collapse;font-size:inherit\">");

        if (headers.Count > 0)
        {
            sb.Append("<thead><tr style=\"border-bottom:1px solid rgba(128,128,128,.35)\">");
            foreach (var header in headers)
                sb.Append($"<th style=\"text-align:left;padding:4px 10px 6px 0;font-weight:600;white-space:nowrap\">{E(header)}</th>");
            sb.Append("</tr></thead>");
        }

        sb.Append("<tbody>");
        var any = false;
        foreach (var row in rows)
        {
            any = true;
            sb.Append("<tr style=\"border-bottom:1px solid rgba(128,128,128,.18)\">");
            for (var i = 0; i < row.Count; i++)
            {
                var last = i == row.Count - 1;
                var style = last || row[i] is Wrap
                    ? (last
                        ? "padding:5px 0 5px 0;vertical-align:top;white-space:normal;word-break:break-word"
                        : "padding:5px 10px 5px 0;vertical-align:top;white-space:normal;word-break:break-word")
                    : "padding:5px 10px 5px 0;vertical-align:top;white-space:nowrap";
                sb.Append($"<td style=\"{style}\">{Render(row[i])}</td>");
            }

            sb.Append("</tr>");
        }

        if (!any)
        {
            sb.Append($"<tr><td colspan=\"{Math.Max(headers.Count, 1)}\" style=\"padding:8px 0;opacity:.7\">Nothing to show.</td></tr>");
        }

        sb.Append("</tbody></table></div>");
        return sb.ToString();
    }

    /// <summary>Item / value list, for one document or one settings block.</summary>
    public static string Pairs(IEnumerable<KeyValuePair<string, string>> pairs) =>
        Table([], pairs.Select(p => new object?[] { p.Key, p.Value }));

    public static string Note(string text) =>
        $"<div style=\"padding:0 1.5rem .75rem 1.5rem;opacity:.85\">{E(text)}</div>";

    private static string Render(object? value) => value switch
    {
        null => string.Empty,
        Pill pill when string.IsNullOrEmpty(pill.Text) => string.Empty,
        Pill pill =>
            $"<span style=\"display:inline-block;padding:1px 8px;border-radius:10px;font-size:0.85em;white-space:nowrap;{Style(pill.Kind)}\">{E(pill.Text)}</span>",
        Wrap wrap => E(wrap.Text),
        Snippets snippets => string.Join("<br>", snippets.Hits.Select(h =>
            $"<span style=\"opacity:.75\">{E(h.Field)}:</span> {E(h.Before)}" +
            $"<mark style=\"background:rgba(255,193,7,.35);color:inherit;padding:0 1px;border-radius:2px\">{E(h.Match)}</mark>" +
            E(h.After))),
        Link link =>
            $"<a href=\"{E(link.Href)}\" style=\"text-decoration:underline;white-space:nowrap\">{E(link.Text)}</a>",
        bool flag => flag ? "Yes" : "No",
        _ => E(value.ToString())
    };

    /// <summary>Same colour language as the Price Explainer report.</summary>
    public static string Style(string kind) => kind switch
    {
        "ok" => "background:#d4edda;color:#155724",
        "info" => "background:#d1ecf1;color:#0c5460",
        "warn" => "background:#fff3cd;color:#856404",
        "bad" => "background:#f8d7da;color:#721c24",
        _ => "background:#e9ecef;color:#495057"
    };

    public static string E(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);
}
