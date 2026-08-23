using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Commerce;
using Truvio.Commerce.PowerTools.Core.Commerce.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// The explanation: account + product (+ currency, country, shop, quantity, date) → what the
/// account sees and pays, and why. Every parameter is a plain property so it round-trips
/// through the screen URL and the "switch currency / quantity" actions.
/// </summary>
public sealed class PriceExplainQuery : DataQueryModelBase<PriceExplainModel>
{
    /// <summary>"anonymous" or a user id.</summary>
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

    // Methods, not properties: CoreUI serialises every public property into the screen URL.
    public int? GetUserId() =>
        int.TryParse(AccountKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;

    public DateTime? GetTime() =>
        DateTime.TryParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    public ExplainRequest ToRequest() => new()
    {
        UserId = GetUserId(),
        ProductId = ProductId,
        VariantId = VariantId,
        LanguageId = LanguageId,
        CurrencyCode = CurrencyCode,
        CountryCode = CountryCode,
        ShopId = ShopId,
        Quantity = Quantity <= 0 ? 1 : Quantity,
        Time = GetTime()
    };

    public override PriceExplainModel? GetModel()
    {
        if (string.IsNullOrEmpty(ProductId))
            return new PriceExplainModel { Title = "Price Explainer", Error = "No product selected" };

        try
        {
            var report = new DwCommerceExplainer().Explain(ToRequest());
            return new PriceExplainModel
            {
                Title = $"{ProductId}{(string.IsNullOrEmpty(VariantId) ? "" : $" / {VariantId}")} for {(GetUserId() is int id ? $"user {id}" : "anonymous")}",
                AccountName = report.Context.FirstOrDefault(c => c.Label == "Account").Value ?? string.Empty,
                ProductName = report.Context.FirstOrDefault(c => c.Label == "Product").Value ?? string.Empty,
                Visible = report.Visibility.Visible,
                VisibilitySummary = report.Visibility.Summary,
                PriceBeforeDiscount = report.DwPriceBeforeDiscount,
                PriceSource = report.DwPriceSource,
                DiscountTotal = report.DwDiscountTotal,
                AppliedDiscountCount = report.Discounts.Count(d => d.AppliedByDw),
                FinalPrice = report.DwFinalPrice,
                Rows = ToRows(report)
            };
        }
        catch (Exception ex)
        {
            return new PriceExplainModel
            {
                Title = $"{ProductId}",
                Error = ex.Message
            };
        }
    }

    internal static List<ExplainRowModel> ToRows(ExplainReport report)
    {
        var rows = new List<ExplainRowModel>();

        // ---- Result first: the answer, then the reasoning. ----------------------------------
        Header("Result");
        Add("Result", "Sees the product", report.Visibility.Visible ? "Yes" : "No",
            report.Visibility.Visible ? "ok" : "hidden", string.Empty, report.Visibility.Summary);
        Add("Result", "Price before discounts", report.DwPriceSource, "info", report.DwPriceBeforeDiscount,
            report.PriceMatrix.Winner is { } winner
                ? $"Winning matrix row {winner.Row.Id}{Restrictions(winner)}"
                : report.PriceMatrix.Rows.Count == 0
                    ? "No price-matrix rows exist for this product"
                    : $"None of the {report.PriceMatrix.Rows.Count} matrix row(s) match this context{OtherCurrenciesHint(report)}");
        Add("Result", "Discounts", report.Discounts.Count(d => d.AppliedByDw) is var n && n > 0 ? $"{n} applied" : "none",
            n > 0 ? "ok" : "", report.DwDiscountTotal, report.DiscountSelectionBehavior);
        Add("Result", "Final unit price", "Pays", "win", report.DwFinalPrice, "DW price before discounts minus the applied product discounts");

        foreach (var warning in report.Warnings)
            Add("Result", "Warning", "Check", "warn", string.Empty, warning);

        // ---- Context --------------------------------------------------------------------------
        Header("Context");
        foreach (var (label, value) in report.Context)
            Add("Context", label, string.Empty, string.Empty, value, string.Empty);

        // ---- Visibility -----------------------------------------------------------------------
        Header("Visibility");
        if (report.Visibility.Rows.Count == 0)
            Add("Visibility", "Assortments", "none", "", string.Empty, "No assortments are defined");
        foreach (var r in report.Visibility.Rows)
        {
            var a = r.Assortment;
            var verdict = r.Grants ? "Grants" : r.AccountHasIt ? "Held" : a.Active ? "Not held" : "Inactive";
            var kind = r.Grants ? "win" : r.AccountHasIt ? "match" : a.Active ? "reject" : "";
            var value = a.ContainsProduct ? "Contains product" : "Product not in it";
            Add("Visibility", $"{a.Name} ({a.Id})", verdict, kind, value, r.Explanation);
        }
        foreach (var warning in report.Visibility.Warnings)
            Add("Visibility", "Warning", "Check", "warn", string.Empty, warning);

        // ---- Price matrix ---------------------------------------------------------------------
        Header("Price matrix");
        Add("Price matrix", "Product default price", "Fallback", "info", report.ProductDefaultPrice,
            "Used only when no matrix row matches");
        foreach (var v in report.PriceMatrix.Rows.OrderByDescending(r => r.IsWinner).ThenByDescending(r => r.Matches).ThenBy(r => r.ComparableAmount))
        {
            var row = v.Row;
            var amount = $"{row.Amount.ToString("0.00", CultureInfo.InvariantCulture)} {row.CurrencyCode}{(row.IsWithVat ? " incl. VAT" : "")}";
            string verdict, kind, why;
            if (v.IsWinner)
            {
                verdict = "Wins"; kind = "win";
                why = $"Cheapest matching row{Restrictions(v)}";
            }
            else if (v.Matches)
            {
                verdict = "Shadowed"; kind = "match";
                why = $"Matches but is {v.ShadowedBy?.ToString("0.00", CultureInfo.InvariantCulture)} more expensive than the winner{Restrictions(v)}";
            }
            else
            {
                verdict = "Rejected"; kind = "reject";
                why = string.Join("; ", v.FailedChecks);
            }
            Add("Price matrix", $"Row {row.Id}", verdict, kind, amount, why);
        }
        foreach (var (qty, price) in report.QuantityPrices)
            Add("Price matrix", $"Quantity break ≥ {qty}", "Tier", "info", price, "DW quantity price for this context");

        // ---- Discounts ------------------------------------------------------------------------
        Header("Discounts");
        if (report.Discounts.Count == 0)
            Add("Discounts", "Product discounts", "none", "", string.Empty, "No active order-line discounts exist");
        foreach (var d in report.Discounts)
        {
            var s = d.Verdict.Discount;
            var item = $"{s.Name} (#{s.Id}, prio {s.Priority})";
            var value = string.IsNullOrEmpty(d.Amount) ? s.TypeDescription : $"{s.TypeDescription} → {d.Amount}";
            string verdict, kind, why;
            if (d.AppliedByDw)
            {
                verdict = "Applied"; kind = "win";
                why = $"Scope: {s.ProductScope}{(d.Verdict.SatisfiedRestrictions.Count > 0 ? $"; matched {string.Join(", ", d.Verdict.SatisfiedRestrictions)}" : "")}{(s.StopFurtherProcessing ? "; stops further discounts" : "")}";
            }
            else if (!d.Verdict.PassesBaseChecks)
            {
                verdict = "Rejected"; kind = "reject";
                why = string.Join("; ", d.Verdict.FailedChecks);
            }
            else
            {
                verdict = "Not applied"; kind = "match";
                why = $"Base conditions pass but DW did not apply it — product scope ({s.ProductScope}){(s.OnlyApplyToNonDiscountedItems ? ", 'only non-discounted items'" : "")} or an extender excluded this product";
            }
            Add("Discounts", item, verdict, kind, value, why);
        }

        return rows;

        void Header(string section) =>
            rows.Add(new ExplainRowModel { IsHeader = true, Section = section, Item = section, Details = $"— {section.ToUpperInvariant()} —" });

        static string Line(string item, string value, string why) =>
            (value, why) switch
            {
                ("", "") => item,
                (_, "") => $"{item}: {value}",
                ("", _) => $"{item} — {why}",
                _ => $"{item}: {value} — {why}"
            };

        void Add(string section, string item, string verdict, string kind, string value, string why) =>
            rows.Add(new ExplainRowModel
            {
                Section = section,
                Item = item,
                Verdict = verdict,
                VerdictKind = kind,
                Value = value,
                Why = why,
                Details = Line(item, value, why)
            });

        static string OtherCurrenciesHint(ExplainReport report)
        {
            var currencies = report.PriceMatrix.Rows
                .Where(r => !r.Matches && r.FailedChecks.Any(f => f.StartsWith("Currency ")))
                .Select(r => r.Row.CurrencyCode)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return currencies.Count == 0
                ? string.Empty
                : $" — rows exist in {string.Join(", ", currencies)}; switch the currency via Actions to compare";
        }

        static string Restrictions(PriceRowVerdict v) =>
            v.SatisfiedRestrictions.Count == 0
                ? " (unrestricted row)"
                : $" (restricted to {string.Join(", ", v.SatisfiedRestrictions)})";
    }
}
