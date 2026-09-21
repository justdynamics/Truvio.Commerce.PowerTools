using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Dw;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Models;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;
using Truvio.Commerce.PowerTools.Shared.AdminUI;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Queries;

/// <summary>
/// The context slide-over for the Price Explainer: one titled section of toggle chips per
/// dimension (currency, shop, quantity, date). Picks are staged client-side — a chip click
/// only marks the chip — and a single Apply navigates the report with everything at once.
/// A panel rather than a form dialog — intercepting dialog values has been unreliable, and
/// rather than the old flat split-button menu, whose group titles barely rendered.
/// <para>
/// Mechanics: chips carry <c>data-dim</c>/<c>data-value</c> and an inline <c>onclick</c>
/// (inline handlers run even when the client inserts the HTML via innerHTML, where a
/// <c>&lt;script&gt;</c> tag would not). Apply's own <c>onclick</c> runs in the target phase,
/// BEFORE DW's document-level delegated handler (bubble phase): it composes the staged picks
/// into a fresh <c>NavigateScreen</c> <c>data-dw-action</c> on itself, which the delegated
/// handler then executes.
/// </para>
/// </summary>
public sealed class PriceContextQuery : DataQueryModelBase<PriceContextModel>
{
    public string AccountKey { get; set; } = string.Empty;

    public string ProductId { get; set; } = string.Empty;

    public string VariantId { get; set; } = string.Empty;

    public string LanguageId { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string ShopId { get; set; } = string.Empty;

    public double Quantity { get; set; } = 1;

    /// <summary>ISO date (yyyy-MM-dd); empty = now.</summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>Selected-chip look; the accent follows the admin's primary purple.</summary>
    private const string ChipOn = "background:#4e2ea6;color:#fff;border-color:#4e2ea6";

    private const string ChipOff = "background:transparent;color:inherit;border-color:rgba(128,128,128,.45)";

    private const string ChipBase =
        "border:1px solid;border-radius:16px;padding:3px 12px;font-size:.9em;cursor:pointer;line-height:1.5;";

    /// <summary>Deselect the chip's siblings (same data-dim), select the chip.</summary>
    private const string ChipJs =
        "var p=this.closest('[data-panel]');" +
        "p.querySelectorAll('[data-dim='+this.dataset.dim+']').forEach(function(b){b.dataset.on='';b.style.cssText='" + ChipBase + ChipOff + "';});" +
        "this.dataset.on='1';this.style.cssText='" + ChipBase + ChipOn + "';";

    /// <summary>
    /// Compose the staged picks into a NavigateScreen action on the Apply link itself; DW's
    /// delegated handler (bubble phase, so it runs after this) then executes it.
    /// </summary>
    private const string ApplyJs =
        "var q=JSON.parse(this.dataset.query);" +
        "this.closest('[data-panel]').querySelectorAll('[data-dim][data-on=\\\"1\\\"]').forEach(function(b){" +
        "q[b.dataset.dim]=b.dataset.dim==='Quantity'?parseFloat(b.dataset.value):b.dataset.value;});" +
        "q.Type='PriceExplain';q.QueryContext={screenTypeName:'PriceExplain'};" +
        "this.setAttribute('data-dw-action',JSON.stringify({name:'NavigateScreen',parameters:{ScreenTypeName:'PriceExplain',Query:q,ForceReload:true,NavigateByPost:false}}));";

    public override PriceContextModel? GetModel()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<div data-panel=\"1\" style=\"padding:0 1.5rem .75rem 1.5rem\">");

        var currencies = Safe(DwCommerceExplainer.Currencies);
        if (currencies.Count > 1)
        {
            Chips("Currency", "CurrencyCode", currencies.Select(c =>
                ($"{c.Code}{(c.Name != c.Code ? $" - {c.Name}" : "")}", c.Code, c.Code == CurrencyCode)));
        }

        var shops = Safe(DwCommerceExplainer.Shops);
        if (shops.Count > 1)
            Chips("Shop / channel", "ShopId", shops.Select(s => (s.Name, s.Id, s.Id == ShopId)));

        var settings = DwPowerToolsSettings.Current;

        Chips("Quantity", "Quantity", settings.Quantities().Select(qty =>
            ($"{qty:0.##}", qty.ToString("0.####", CultureInfo.InvariantCulture), Math.Abs(qty - Quantity) < 0.0001)));

        var today = DateTime.Today;
        Chips("Date", "Date",
            new[] { ("Now", string.Empty, string.IsNullOrEmpty(Date)) }
                .Concat(settings.DateOffsets().Select(days =>
                {
                    var date = today.AddDays(days).ToString("yyyy-MM-dd");
                    return ($"+{days} days ({date})", date, Date == date);
                })));

        // Apply: one navigation for everything staged above. The href is the current-state
        // fallback; the onclick swaps in the staged query before the delegated handler runs.
        sb.Append("<div style=\"margin-top:20px\">");
        sb.Append($"<a href=\"{SearchTables.E(Href())}\" data-query=\"{SearchTables.E(BaseQueryJson())}\" " +
                  $"onclick=\"{SearchTables.E(ApplyJs)}\" " +
                  "style=\"display:inline-block;padding:6px 20px;border-radius:6px;background:#4e2ea6;color:#fff;" +
                  "text-decoration:none;cursor:pointer;font-weight:600\">Apply</a>");
        sb.Append("</div>");

        sb.Append("</div>");
        return new PriceContextModel { Heading = "Context", Html = sb.ToString() };

        void Chips(string title, string dim, IEnumerable<(string Label, string Value, bool Active)> options)
        {
            sb.Append("<div style=\"margin:16px 0 6px;font-weight:600;font-size:.78em;" +
                      $"letter-spacing:.05em;text-transform:uppercase;opacity:.65\">{SearchTables.E(title)}</div>");
            sb.Append("<div style=\"display:flex;flex-wrap:wrap;gap:6px\">");

            foreach (var (label, value, active) in options)
            {
                sb.Append($"<button type=\"button\" data-dim=\"{dim}\" data-value=\"{SearchTables.E(value)}\" " +
                          $"data-on=\"{(active ? "1" : "")}\" onclick=\"{SearchTables.E(ChipJs)}\" " +
                          $"style=\"{ChipBase}{(active ? ChipOn : ChipOff)}\">{SearchTables.E(label)}</button>");
            }

            sb.Append("</div>");
        }
    }

    /// <summary>The full-navigation fallback URL for the current (unstaged) state.</summary>
    private string Href() =>
        "/Admin/UI/PowerTools/PriceExplain" +
        $"?AccountKey={Uri.EscapeDataString(AccountKey)}&ProductId={Uri.EscapeDataString(ProductId)}" +
        $"&VariantId={Uri.EscapeDataString(VariantId)}&LanguageId={Uri.EscapeDataString(LanguageId)}" +
        $"&CurrencyCode={Uri.EscapeDataString(CurrencyCode)}&CountryCode={Uri.EscapeDataString(CountryCode)}" +
        $"&ShopId={Uri.EscapeDataString(ShopId)}&Quantity={Quantity.ToString("0.####", CultureInfo.InvariantCulture)}" +
        $"&Date={Uri.EscapeDataString(Date)}" +
        "&Type=PriceExplain&QueryContext=Dynamicweb.CoreUI.Data.DataQueryContext";

    /// <summary>The current query values, for the Apply handler to overlay staged picks on.</summary>
    private string BaseQueryJson() =>
        System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["AccountKey"] = AccountKey,
            ["ProductId"] = ProductId,
            ["VariantId"] = VariantId,
            ["LanguageId"] = LanguageId,
            ["CurrencyCode"] = CurrencyCode,
            ["CountryCode"] = CountryCode,
            ["ShopId"] = ShopId,
            ["Quantity"] = Quantity,
            ["Date"] = Date
        });

    private static IReadOnlyList<T> Safe<T>(Func<IReadOnlyList<T>> source)
    {
        try
        {
            return source();
        }
        catch
        {
            return [];
        }
    }
}
