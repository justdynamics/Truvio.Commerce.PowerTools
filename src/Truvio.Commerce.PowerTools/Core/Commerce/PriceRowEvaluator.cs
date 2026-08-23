namespace Truvio.Commerce.PowerTools.Core.Commerce;

/// <summary>
/// One row of the price matrix (EcomPrices), reduced to the fields DW's price filters read.
/// Empty string / 0 / null means "not restricted" for every dimension.
/// </summary>
public sealed record PriceRowSpec
{
    public string Id { get; init; } = string.Empty;
    public string VariantId { get; init; } = string.Empty;
    public string LanguageId { get; init; } = string.Empty;
    public string UnitId { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string ShopId { get; init; } = string.Empty;
    public double Quantity { get; init; }
    public double Amount { get; init; }
    public bool IsWithVat { get; init; }
    public bool IsInformative { get; init; }
    public long StockLocationId { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string UserCustomerNumber { get; init; } = string.Empty;
    public string UserGroupId { get; init; } = string.Empty;
    /// <summary>Legacy customer-group column: matches a user group by its customer number.</summary>
    public string CustomerGroupId { get; init; } = string.Empty;
}

/// <summary>The selection DW evaluates price rows against (PriceContext + PriceProductSelection).</summary>
public sealed record PriceLookupContext
{
    public int? UserId { get; init; }
    public string? UserCustomerNumber { get; init; }
    public IReadOnlySet<int> UserGroupIds { get; init; } = new HashSet<int>();
    public IReadOnlySet<string> UserGroupCustomerNumbers { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public string CurrencyCode { get; init; } = string.Empty;
    public string? CountryCode { get; init; }
    public string? ShopId { get; init; }
    public string LanguageId { get; init; } = string.Empty;
    public string VariantId { get; init; } = string.Empty;
    public string VirtualVariantId { get; init; } = string.Empty;
    public string UnitId { get; init; } = string.Empty;
    public long StockLocationId { get; init; }
    public double Quantity { get; init; } = 1;
    public double QuantityAllVariants { get; init; }
    public DateTime Time { get; init; } = DateTime.Now;
    /// <summary>VAT percent used to normalise rows stored with VAT when the DB stores prices without VAT.</summary>
    public double VatPercent { get; init; }
    public bool PricesInDatabaseIncludeVat { get; init; }
}

public sealed class PriceRowVerdict
{
    public required PriceRowSpec Row { get; init; }

    /// <summary>True when every DW filter accepts the row for this context.</summary>
    public bool Matches => FailedChecks.Count == 0;

    /// <summary>Human-readable reasons the row was rejected (empty when it matches).</summary>
    public IReadOnlyList<string> FailedChecks { get; init; } = [];

    /// <summary>The restrictions the row carries that the context satisfied ("group 12", "qty >= 10").</summary>
    public IReadOnlyList<string> SatisfiedRestrictions { get; init; } = [];

    /// <summary>Amount DW compares rows by (VAT-normalised); NaN when the row does not match.</summary>
    public double ComparableAmount { get; init; } = double.NaN;

    public bool IsWinner { get; set; }

    /// <summary>Set on matching rows that lose to the winner: how much more expensive they are.</summary>
    public double? ShadowedBy { get; set; }
}

public sealed class PriceMatrixVerdict
{
    public IReadOnlyList<PriceRowVerdict> Rows { get; init; } = [];

    public PriceRowVerdict? Winner { get; init; }

    /// <summary>True when two or more matching rows share the lowest amount: DW picks whichever the DB returns first.</summary>
    public bool HasTie { get; init; }

    public int MatchCount => Rows.Count(r => r.Matches);
}

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
