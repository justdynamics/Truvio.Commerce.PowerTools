using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Dw;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Models;
using Truvio.Commerce.PowerTools.Features.Settings.Core;
using Truvio.Commerce.PowerTools.Features.Settings.Dw;
using Truvio.Commerce.PowerTools.Shared.AdminUI;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Queries;

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

    /// <summary>Resolves a toolbar account pick — see <see cref="PickStore"/>.</summary>
    public string AccountPickToken { get; set; } = string.Empty;

    /// <summary>Resolves a toolbar product pick (<c>product~variant~language</c>).</summary>
    public string ProductPickToken { get; set; } = string.Empty;

    /// <summary>A pick token wins over whatever the URL carried for that dimension.</summary>
    private void ResolvePicks()
    {
        if (!string.IsNullOrEmpty(AccountPickToken) && PickStore.Get(AccountPickToken) is { Length: > 0 } account)
            AccountKey = account;

        if (!string.IsNullOrEmpty(ProductPickToken) && PickStore.Get(ProductPickToken) is { Length: > 0 } picked)
        {
            var parts = picked.Split(Selectors.ExplainerProductSelectorProvider.IdSeparator);
            ProductId = parts[0];
            VariantId = parts.Length > 1 ? parts[1] : string.Empty;
            if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
                LanguageId = parts[2];
        }
    }

    // Methods, not properties: CoreUI serialises every public property into the screen URL.
    public int? GetUserId() =>
        int.TryParse(AccountKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;

    public DateTime? GetTime() =>
        DateTime.TryParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    /// <summary>The currency asked for, or the PowerTools override, or (blank) the DW default.</summary>
    public string EffectiveCurrencyCode() =>
        string.IsNullOrWhiteSpace(CurrencyCode)
            ? DwPowerToolsSettings.Current.DefaultCurrencyCode
            : CurrencyCode;

    public ExplainRequest ToRequest() => new()
    {
        UserId = GetUserId(),
        ProductId = ProductId,
        VariantId = VariantId,
        LanguageId = LanguageId,
        CurrencyCode = EffectiveCurrencyCode(),
        CountryCode = CountryCode,
        ShopId = ShopId,
        Quantity = Quantity <= 0 ? 1 : Quantity,
        Time = GetTime()
    };

    public override PriceExplainModel? GetModel()
    {
        ResolvePicks();

        if (string.IsNullOrEmpty(ProductId))
            return new PriceExplainModel { Title = "Price Explainer", Error = "No product selected" };

        try
        {
            var report = new DwCommerceExplainer().Explain(ToRequest());
            return new PriceExplainModel
            {
                Title = $"{ProductId}{(string.IsNullOrEmpty(VariantId) ? "" : $" / {VariantId}")} · {(GetUserId() is int id ? $"user {id}" : "anonymous")}",
                AccountName = report.Context.FirstOrDefault(c => c.Label == "Account").Value ?? string.Empty,
                ProductName = report.Context.FirstOrDefault(c => c.Label == "Product").Value ?? string.Empty,
                Visible = report.Visibility.Visible,
                VisibilitySummary = report.Visibility.Summary,
                PriceBeforeDiscount = report.DwPriceBeforeDiscount,
                PriceSource = report.DwPriceSource,
                FoundDiscount = report.FoundDiscount,
                Steps = report.Steps,
                DiscountTotal = report.DwDiscountTotal,
                AppliedDiscountCount = report.Discounts.Count(d => d.AppliedByDw),
                FinalPrice = report.DwFinalPrice,
                Rows = ToRows(report, PowerToolsSettings.Positive(DwPowerToolsSettings.Current.PriceRowCap, DefaultPriceRowCap))
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

    /// <summary>Price-matrix rows rendered before the section truncates; overridable in settings.</summary>
    internal const int DefaultPriceRowCap = 100;

    internal static List<ExplainRowModel> ToRows(ExplainReport report, int priceRowCap = DefaultPriceRowCap)
    {
        var rowCap = priceRowCap > 0 ? priceRowCap : DefaultPriceRowCap;
        var rows = new List<ExplainRowModel>();

        // ---- Result first: the answer, then the reasoning. ----------------------------------
        Header("Price");
        Add("Price", "Found price", report.DwPriceSource, "info", report.DwPriceBeforeDiscount,
            report.PriceMatrix.Winner is { } winner
                ? $"Winning matrix row {winner.Row.Id}{Restrictions(winner)}"
                : "DW's returned price; no matrix winner is claimed");
        Add("Price", "Found discount", "Price-line %", "info", report.FoundDiscount, report.FoundDiscountReason);
        Add("Price", "Campaign discounts", "Selected", "info", report.DwDiscountTotal, report.DiscountSelectionBehavior);
        Add("Price", "Final unit price", "Pays", "win", report.DwFinalPrice, "Found price minus price-line discount and campaign savings; see limitations below");
        Add("Price", "Line total", "Chosen quantity", "info", report.LineTotal, report.AllowFlags);
        Header("Price steps");
        Header("Result");
        Add("Result", "Sees the product", report.Visibility.Visible ? "Yes" : "No",
            report.Visibility.Visible ? "ok" : "hidden", string.Empty, report.Visibility.Summary);

        if (report.Conversion is { } conv)
        {
            // Transparency first: the implied unit rate makes a nonsense factor obvious even
            // when no rule fires.
            Add("Result", "Currency conversion", conv.Broken ? "Broken" : "Applied", conv.Broken ? "reject" : "info",
                conv.ImpliedRateText, conv.FormulaText);
            foreach (var f in conv.Findings)
                Add("Result", "Currency", f.Severity == FindingSeverity.Critical ? "Broken" : "Check",
                    f.Severity == FindingSeverity.Critical ? "reject" : "warn", string.Empty,
                    $"{f.Title} — {f.Detail}");
        }

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
        var matrixRows = report.PriceMatrix.Rows
            .Where(r => r.Matches || r.NeedsHigherQuantity)
            .OrderByDescending(r => r.IsWinner).ThenBy(r => r.Row.TargetKind).ThenBy(r => r.Row.Quantity).ToList();
        foreach (var v in matrixRows.Take(rowCap)) AddPriceRow(v, rows);
        if (matrixRows.Count > rowCap)
            Add("Price matrix", "More applicable rows", "Truncated", "warn", $"{matrixRows.Count - rowCap}",
                "Raise the price row cap in PowerTools settings to see more rows");
        foreach (var group in report.PriceMatrix.Rows.Where(r => !r.Matches && !r.NeedsHigherQuantity).GroupBy(r => r.RejectionReason))
        {
            var detail = new List<ExplainRowModel>();
            foreach (var v in group.Take(rowCap)) AddPriceRow(v, detail);
            rows.Add(new ExplainRowModel
            {
                Section = "Price matrix", Item = group.Key, Verdict = "Rejected", VerdictKind = "reject",
                Value = $"{group.Count()} rows", Why = $"Open for evidence (up to {rowCap} rows)", DetailRows = detail
            });
        }
        Header("DW raw quantity list");
        foreach (var (qty, price) in report.QuantityPrices.Take(rowCap))
            Add("DW raw quantity list", $"Quantity break >= {qty}", "Raw", "info", price,
                "Storefront quantity list: may contain duplicate breaks; does not select the winning net price");
        if (report.QuantityPrices.Count > rowCap)
            Add("DW raw quantity list", "More rows", "Truncated", "warn", $"{report.QuantityPrices.Count - rowCap}", "Raise the price row cap to see more");

        // ---- Discounts ------------------------------------------------------------------------
        Header("Discounts");
        if (report.Discounts.Count == 0)
            Add("Discounts", "Product discounts", "none", "", string.Empty, "No in-scope discounts are listed; see excluded counts below");
        foreach (var d in report.Discounts.Take(rowCap))
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
                why = !string.IsNullOrEmpty(d.CalculationNote) ? d.CalculationNote : s.NeedsOrder ? $"Needs cart: {s.NeedsOrderReason}" : $"Base conditions pass but DW did not select it — product scope ({s.ProductScope}){(s.OnlyApplyToNonDiscountedItems ? ", 'only non-discounted items'" : "")} or an extender excluded this product";
            }
            Add("Discounts", item, verdict, kind, value, why + (string.IsNullOrEmpty(d.CalculationNote) || why.Contains(d.CalculationNote) ? "" : $"; {d.CalculationNote}"));
        }

        if (report.ApplicableDiscountCount > rowCap)
            Add("Discounts", "More in-scope discounts", "Truncated", "warn", $"{report.ApplicableDiscountCount - rowCap}", "Raise the price row cap to see more");
        foreach (var (reason, count) in report.DiscountRejectedCounts)
            rows.Add(new ExplainRowModel
            {
                Section = "Discounts", Item = reason, Verdict = "Excluded", VerdictKind = "reject",
                Value = $"{count} discounts", Why = $"Open for evidence (up to {rowCap} rows)",
                DetailRows = report.DiscountRejectedSamples.TryGetValue(reason, out var sample)
                    ? sample.Take(rowCap).Select(d => new ExplainRowModel
                    {
                        Item = $"{d.Verdict.Discount.Name} (#{d.Verdict.Discount.Id})",
                        Verdict = "Excluded", VerdictKind = "reject", Value = d.Verdict.Discount.TypeDescription,
                        Why = $"{d.CalculationNote}; {d.Verdict.Discount.ProductScope}; {string.Join("; ", d.Verdict.FailedChecks)}"
                    }).ToList() : []
            });

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

        void AddPriceRow(PriceRowVerdict v, List<ExplainRowModel> target)
        {
            var row = v.Row;
            var verdict = v.IsWinner ? "Wins" : v.IsOutranked ? "Outranked" : v.IsDiscountLine ? "Discount line"
                : v.Matches ? report.PriceMatrix.Rule == PriceSelectionRule.Unknown ? "Applies" : "Shadowed"
                : v.NeedsHigherQuantity ? "Higher quantity" : "Rejected";
            var why = !v.Matches ? string.Join("; ", v.FailedChecks)
                : $"{row.TargetKind}; priority {row.Priority}" +
                    (report.DiscountDetailsAvailable ? $"; own {row.DiscountPercentage:0.##}%; allow order: {row.AllowOrderDiscounts}; allow line: {row.AllowOrderLineDiscounts}" +
                        (report.PriceMatrix.Rule != PriceSelectionRule.Unknown ? $"; effective {v.EffectiveDiscountPercentage:0.##}%; comparable net {v.NetAmount:0.00}" : "") : "; discount % unavailable in Base line") + Restrictions(v);
            target.Add(new ExplainRowModel
            {
                Section = "Price matrix", Item = $"Row {row.Id} (qty >= {row.Quantity:0.##})",
                Verdict = verdict, VerdictKind = v.IsWinner ? "win" : v.Matches ? "match" : "reject",
                Value = $"{row.Amount.ToString("0.00", CultureInfo.InvariantCulture)} {row.CurrencyCode}{(row.IsWithVat ? " incl. VAT" : "")}", Why = why
            });
        }

        static string Restrictions(PriceRowVerdict v) =>
            v.SatisfiedRestrictions.Count == 0
                ? " (unrestricted row)"
                : $" (restricted to {string.Join(", ", v.SatisfiedRestrictions)})";
    }
}
