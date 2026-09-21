namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

/// <summary>
/// Mirrors DW's DefaultPriceProvider: the twelve PriceFilters of PriceService.FindPrices decide
/// which matrix rows apply, then the CHEAPEST applicable row wins (MinBy on the comparable
/// amount) — Priority is not consulted. No applicable row means the product's default price.
/// </summary>
public static class PriceRowEvaluator
{
    public const string AnyVariant = "Any";

    public static PriceMatrixVerdict Evaluate(IEnumerable<PriceRowSpec> rows, PriceLookupContext context)
    {
        var verdicts = rows.Select(r => Evaluate(r, context)).ToList();

        var matching = verdicts.Where(v => v.Matches).ToList();
        PriceRowVerdict? winner = null;
        var hasTie = false;

        if (matching.Count > 0)
        {
            // MinBy returns the first minimum in enumeration order — same as DW.
            winner = matching.MinBy(v => v.ComparableAmount)!;
            winner.IsWinner = true;
            hasTie = matching.Count(v => v.ComparableAmount == winner.ComparableAmount) > 1;

            foreach (var loser in matching.Where(v => !ReferenceEquals(v, winner)))
                loser.ShadowedBy = loser.ComparableAmount - winner.ComparableAmount;
        }

        return new PriceMatrixVerdict
        {
            Rows = verdicts,
            Winner = winner,
            HasTie = hasTie
        };
    }

    public static PriceRowVerdict Evaluate(PriceRowSpec row, PriceLookupContext ctx)
    {
        var failed = new List<string>();
        var satisfied = new List<string>();

        void Check(bool restricted, bool ok, string restriction, string failure)
        {
            if (!restricted)
                return;
            if (ok)
                satisfied.Add(restriction);
            else
                failed.Add(failure);
        }

        // IsInformativePriceFilter — the explainer always looks at real (non-informative) prices.
        Check(row.IsInformative, false, "", "Informative price (not used for selling)");

        // VariantPriceFilter
        var variantRestricted = !string.IsNullOrEmpty(row.VariantId)
            && !Eq(row.VariantId, AnyVariant);
        Check(variantRestricted,
            Eq(row.VariantId, ctx.VariantId) || Eq(row.VariantId, ctx.VirtualVariantId),
            $"variant {row.VariantId}",
            $"Variant {row.VariantId} ≠ {Display(ctx.VariantId, "no variant")}");

        // UnitPriceFilter
        Check(!string.IsNullOrEmpty(row.UnitId), Eq(row.UnitId, ctx.UnitId),
            $"unit {row.UnitId}", $"Unit {row.UnitId} ≠ {Display(ctx.UnitId, "default unit")}");

        // StockLocationPriceFilter
        Check(row.StockLocationId != 0, row.StockLocationId == ctx.StockLocationId,
            $"stock location {row.StockLocationId}",
            $"Stock location {row.StockLocationId} ≠ {ctx.StockLocationId}");

        // QuantityPriceFilter: a quantity threshold applies; "Any"-variant rows may use the
        // quantity summed across all variants of the product.
        if (row.Quantity != 0 && row.Quantity > ctx.Quantity)
        {
            if (Eq(row.VariantId, AnyVariant) && row.Quantity <= ctx.QuantityAllVariants)
                satisfied.Add($"qty ≥ {row.Quantity:0.##} (across variants)");
            else
                failed.Add($"Needs quantity ≥ {row.Quantity:0.##} (context: {ctx.Quantity:0.##})");
        }
        else if (row.Quantity != 0)
        {
            satisfied.Add($"qty ≥ {row.Quantity:0.##}");
        }

        // LanguagePriceFilter
        Check(!string.IsNullOrEmpty(row.LanguageId), Eq(row.LanguageId, ctx.LanguageId),
            $"language {row.LanguageId}", $"Language {row.LanguageId} ≠ {ctx.LanguageId}");

        // OrderTimePriceFilter
        var fromOk = row.ValidFrom is null || row.ValidFrom.Value <= ctx.Time;
        var toOk = row.ValidTo is null || row.ValidTo.Value >= ctx.Time;
        Check(row.ValidFrom is not null, fromOk, $"valid from {row.ValidFrom:yyyy-MM-dd}",
            $"Not valid before {row.ValidFrom:yyyy-MM-dd HH:mm}");
        Check(row.ValidTo is not null, toOk, $"valid to {row.ValidTo:yyyy-MM-dd}",
            $"Expired {row.ValidTo:yyyy-MM-dd HH:mm}");

        // CurrencyPriceFilter
        Check(!string.IsNullOrEmpty(row.CurrencyCode), Eq(row.CurrencyCode, ctx.CurrencyCode),
            $"currency {row.CurrencyCode}", $"Currency {row.CurrencyCode} ≠ {ctx.CurrencyCode}");

        // CountryPriceFilter
        Check(!string.IsNullOrEmpty(row.CountryCode), Eq(row.CountryCode, ctx.CountryCode),
            $"country {row.CountryCode}", $"Country {row.CountryCode} ≠ {Display(ctx.CountryCode, "none")}");

        // ShopPriceFilter
        Check(!string.IsNullOrEmpty(row.ShopId), Eq(row.ShopId, ctx.ShopId),
            $"shop {row.ShopId}", $"Shop {row.ShopId} ≠ {Display(ctx.ShopId, "none")}");

        // CustomerPriceFilter: UserId takes precedence over UserCustomerNumber.
        if (!string.IsNullOrEmpty(row.UserId))
        {
            Check(true, ctx.UserId is int uid && Eq(row.UserId, uid.ToString()),
                $"user {row.UserId}", $"Only for user id {row.UserId}");
        }
        else if (!string.IsNullOrEmpty(row.UserCustomerNumber))
        {
            Check(true, Eq(row.UserCustomerNumber, ctx.UserCustomerNumber),
                $"customer no. {row.UserCustomerNumber}",
                $"Only for customer number {row.UserCustomerNumber}");
        }

        // CustomerGroupPriceFilter: UserGroupId takes precedence over the legacy CustomerGroupId.
        if (!string.IsNullOrWhiteSpace(row.UserGroupId))
        {
            var ok = int.TryParse(row.UserGroupId, out var gid) && ctx.UserGroupIds.Contains(gid);
            Check(true, ok, $"group {row.UserGroupId}", $"Only for members of group {row.UserGroupId}");
        }
        else if (!string.IsNullOrWhiteSpace(row.CustomerGroupId))
        {
            Check(true, ctx.UserGroupCustomerNumbers.Contains(row.CustomerGroupId),
                $"customer group {row.CustomerGroupId}",
                $"Only for groups with customer number {row.CustomerGroupId}");
        }

        var comparable = double.NaN;
        if (failed.Count == 0)
        {
            comparable = row.Amount;
            if (row.IsWithVat && !ctx.PricesInDatabaseIncludeVat && ctx.VatPercent > 0)
                comparable = row.Amount / (1 + ctx.VatPercent / 100d);
        }

        return new PriceRowVerdict
        {
            Row = row,
            FailedChecks = failed,
            SatisfiedRestrictions = satisfied,
            ComparableAmount = comparable
        };
    }

    private static bool Eq(string? a, string? b) =>
        string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    private static string Display(string? value, string whenEmpty) =>
        string.IsNullOrEmpty(value) ? whenEmpty : value;
}
