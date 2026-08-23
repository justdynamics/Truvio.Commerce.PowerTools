using Dynamicweb.Configuration;
using Dynamicweb.Ecommerce;
using Dynamicweb.Ecommerce.International;
using Dynamicweb.Ecommerce.Orders.Discounts;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Ecommerce.Shops;
using Dynamicweb.Security.UserManagement;

namespace Truvio.Commerce.PowerTools.Core.Commerce.Dw;

/// <summary>The inputs of one explanation: who, what, and under which commercial context.</summary>
public sealed record ExplainRequest
{
    /// <summary>null = anonymous visitor.</summary>
    public int? UserId { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public string VariantId { get; init; } = string.Empty;
    public string LanguageId { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string ShopId { get; init; } = string.Empty;
    public double Quantity { get; init; } = 1;
    public DateTime? Time { get; init; }
}

/// <summary>
/// Builds the <see cref="ExplainReport"/> against the DW runtime: reads the raw price matrix,
/// assortments and discounts into evaluator specs (the "why"), and runs DW's own
/// PriceManager / DiscountInfoCollection for the same context (the "what") so both are shown
/// side by side. Read-only.
/// </summary>
public sealed class DwCommerceExplainer
{
    public ExplainReport Explain(ExplainRequest request)
    {
        var warnings = new List<string>();

        var languageId = string.IsNullOrEmpty(request.LanguageId)
            ? Services.Languages.GetDefaultLanguageId()
            : request.LanguageId;

        var product = Services.Products.GetProductById(request.ProductId, request.VariantId ?? string.Empty, languageId)
            ?? throw new InvalidOperationException($"Product '{request.ProductId}' (variant '{request.VariantId}', language '{languageId}') was not found.");

        var user = request.UserId is int uid ? UserManagementServices.Users.GetUserById(uid) : null;
        if (request.UserId is not null && user is null)
            warnings.Add($"User id {request.UserId} no longer exists — evaluated as anonymous");

        var currency = ResolveCurrency(request.CurrencyCode, user);
        var country = ResolveCountry(request.CountryCode, user, languageId);
        var shop = ResolveShop(request.ShopId, product);
        var time = request.Time ?? DateTime.Now;
        var quantity = request.Quantity <= 0 ? 1 : request.Quantity;

        var groups = user is null ? [] : UserManagementServices.UserGroups.GetGroupsByUserId(user.ID).ToList();
        var groupIds = groups.Select(g => g.ID).ToHashSet();

        // ---- Context rows -----------------------------------------------------------------
        var context = new List<(string, string)>
        {
            ("Account", user is null ? "Anonymous visitor" : $"{user.Name} (id {user.ID}{(string.IsNullOrEmpty(user.CustomerNumber) ? "" : $", customer no. {user.CustomerNumber}")})"),
            ("Groups", groups.Count == 0 ? (user is null ? "—" : "none") : string.Join(", ", groups.Select(g => $"{g.Name} ({g.ID})"))),
            ("Product", $"{product.Name} — {product.Number} (id {product.Id}{(string.IsNullOrEmpty(product.VariantId) ? "" : $", variant {product.VariantId}")})"),
            ("Language", languageId),
            ("Currency", currency is null ? "none" : $"{currency.Code}"),
            ("Country (VAT)", country is null ? "none" : $"{country.Code2}"),
            ("Shop", shop is null ? "none" : $"{shop.Name} ({shop.Id})"),
            ("Quantity", quantity.ToString("0.##")),
            ("Date", time.ToString("yyyy-MM-dd HH:mm"))
        };

        // ---- Visibility (assortments) --------------------------------------------------------
        var productKey = product.GetProductKey();
        var assortmentSpecs = Services.Assortments.GetAllAssortments()
            .Select(a => new AssortmentSpec
            {
                Id = a.ID,
                Name = a.GetName(languageId) is { Length: > 0 } n ? n : a.ID,
                Active = a.Active,
                AllowAnonymousUsers = a.AllowAnonymousUsers,
                RebuildRequired = a.RebuildRequired,
                ContainsProduct = a.Items.ContainsKey(productKey),
                PermittedUserIds = a.Permissions.Keys.Where(id => !IsGroupId(id)).ToHashSet(),
                PermittedGroupIds = a.Permissions.Keys.Where(IsGroupId).ToHashSet()
            })
            .ToList();

        var useAssortments = SystemConfiguration.Instance.GetBoolean("/Globalsettings/Ecom/Assortments/UseAssortments");
        var visibility = AssortmentEvaluator.Evaluate(
            assortmentSpecs,
            new AssortmentAccount { UserId = user?.ID, GroupIds = groupIds },
            useAssortments);

        // ---- Price matrix ------------------------------------------------------------------
        var priceContext = new PriceContext(currency, country, shop, user, false, time);
        var vatPercent = country is null ? 0 : PriceCalculated.FindVatPercent(country, product, false);

        var lookup = new PriceLookupContext
        {
            UserId = user?.ID,
            UserCustomerNumber = user?.CustomerNumber,
            UserGroupIds = groupIds,
            UserGroupCustomerNumbers = groups.Select(g => g.CustomerNumber).Where(c => !string.IsNullOrEmpty(c)).ToHashSet(StringComparer.OrdinalIgnoreCase),
            CurrencyCode = currency?.Code ?? string.Empty,
            CountryCode = country?.Code2,
            ShopId = shop?.Id,
            LanguageId = languageId,
            VariantId = product.VariantId ?? string.Empty,
            VirtualVariantId = product.VirtualVariantId ?? string.Empty,
            UnitId = product.DefaultUnitId ?? string.Empty,
            StockLocationId = 0,
            Quantity = quantity,
            QuantityAllVariants = 0,
            Time = time,
            VatPercent = vatPercent,
            PricesInDatabaseIncludeVat = PriceHelper.PricesInDatabaseIncludeVat
        };

        var rows = Services.Prices.GetByProductId(product.Id)
            .Select(p => new PriceRowSpec
            {
                Id = p.Id,
                VariantId = p.VariantId ?? string.Empty,
                LanguageId = p.LanguageId ?? string.Empty,
                UnitId = p.UnitId ?? string.Empty,
                CurrencyCode = p.CurrencyCode ?? string.Empty,
                CountryCode = p.CountryCode ?? string.Empty,
                ShopId = p.ShopId ?? string.Empty,
                Quantity = p.Quantity,
                Amount = p.Amount,
                IsWithVat = p.IsWithVat,
                IsInformative = p.IsInformative,
                StockLocationId = p.StockLocationId,
                ValidFrom = p.ValidFrom,
                ValidTo = p.ValidTo,
                UserId = p.UserId ?? string.Empty,
                UserCustomerNumber = p.UserCustomerNumber ?? string.Empty,
                UserGroupId = p.UserGroupId ?? string.Empty,
                CustomerGroupId = p.CustomerGroupId ?? string.Empty
            })
            .ToList();

        var matrix = PriceRowEvaluator.Evaluate(rows, lookup);
        warnings.AddRange(LegacyCustomerGroupWarnings(rows));
        if (matrix.HasTie)
            warnings.Add("Two or more matching price rows share the lowest amount — DW takes whichever the database returns first, so the winning row is order-dependent");

        // ---- DW's own price -----------------------------------------------------------------
        var dwPrice = PriceManager.GetPrice(priceContext, product, product.DefaultUnitId, 0);
        var dwSource = dwPrice.PriceSource switch
        {
            PriceSource.PriceMatrix => "Price matrix",
            PriceSource.ProductDefault => "Product default price",
            PriceSource.CustomProvider => "Custom price provider",
            _ => "Unknown"
        };

        if (dwPrice.PriceSource == PriceSource.CustomProvider)
            warnings.Add("A custom price provider is installed — the matrix explanation below shows DW's built-in selection, which the provider may override");
        else if (dwPrice.PriceSource == PriceSource.PriceMatrix && matrix.Winner is null)
            warnings.Add("DW reports a price-matrix price but the built-in filters match no row — a price subscriber or provider is intervening");
        else if (dwPrice.PriceSource == PriceSource.ProductDefault && matrix.Winner is not null)
            warnings.Add("DW fell back to the product's default price although a matrix row matches — check for a custom provider or notification subscriber");

        var quantityPrices = PriceManager.GetQuantityPrices(priceContext, product)
            .OrderBy(kv => kv.Key.Quantity)
            .Select(kv => (kv.Key.Quantity.ToString("0.##"), Format(kv.Value)))
            .ToList();

        // ---- Discounts ----------------------------------------------------------------------
        var discountLookup = new DiscountLookupContext
        {
            UserId = user?.ID,
            UserCustomerNumber = user?.CustomerNumber,
            UserGroupIds = groupIds,
            CurrencyCode = currency?.Code ?? string.Empty,
            CountryCode = country?.Code2,
            ShopId = shop?.Id,
            LanguageId = languageId,
            Time = time
        };

        var applied = new Dictionary<int, IPriceInfo>();
        IPriceInfo? discountTotal = null;
        try
        {
            var infos = new DiscountInfoCollection(priceContext, product, product.DefaultUnitId, 0);
            foreach (var info in infos)
                applied[info.Discount.ID] = info.GetPrice(priceContext, product.DefaultUnitId, 0);
            discountTotal = infos.TotalPrice;
        }
        catch (Exception ex)
        {
            warnings.Add($"DW discount calculation failed: {ex.Message}");
        }

        var discounts = Services.Discounts.GetActiveOrderLineDiscounts()
            .Select(d => ToSpec(d, languageId))
            .Select(spec => DiscountCandidateEvaluator.Evaluate(spec, discountLookup))
            .Select(v => new DiscountExplanation
            {
                Verdict = v,
                AppliedByDw = applied.ContainsKey(v.Discount.Id),
                Amount = applied.TryGetValue(v.Discount.Id, out var amount) ? Format(amount) : string.Empty
            })
            .OrderByDescending(d => d.AppliedByDw)
            .ThenBy(d => d.Verdict.Discount.Priority)
            .ToList();

        var selection = SystemConfiguration.Instance.GetValue("/Globalsettings/Ecom/Order/Discounts/Selection");
        var selectionText = (string.IsNullOrEmpty(selection) ? "acc" : selection).ToLowerInvariant() switch
        {
            "acc" => "Accumulate all applicable discounts",
            "high" => "Apply only the highest discount",
            "low" => "Apply only the lowest discount",
            var other => other
        };

        var final = discountTotal is null ? dwPrice : dwPrice.Substract(discountTotal);

        return new ExplainReport
        {
            Context = context,
            Visibility = visibility,
            PriceMatrix = matrix,
            DwPriceBeforeDiscount = Format(dwPrice),
            DwPriceSource = dwSource,
            ProductDefaultPrice = currency is null ? product.DefaultPrice.ToString("0.00") : Services.Currencies.Format(Services.Currencies.GetDefaultCurrency(), product.DefaultPrice),
            Discounts = discounts,
            DiscountSelectionBehavior = selectionText,
            DwDiscountTotal = discountTotal is null ? "—" : Format(discountTotal),
            DwFinalPrice = Format(final),
            QuantityPrices = quantityPrices,
            Warnings = warnings
        };

    }

    // AssortmentPermission rows hold user ids AND group ids in the same column (UserID =
    // either); DW resolves them by testing both the user id and the user's group ids, so the
    // same id space is split here by whether a group with that id exists.
    private static bool IsGroupId(int id) => UserManagementServices.UserGroups.GetGroupById(id) is not null;

    /// <summary>
    /// The legacy PriceCustomerGroupId column is matched against a user group's CUSTOMER NUMBER,
    /// not its id. Rows whose value matches no group's customer number but does equal a group
    /// id were almost certainly meant as UserGroupId rows — and never apply to anyone.
    /// </summary>
    private static IEnumerable<string> LegacyCustomerGroupWarnings(IReadOnlyList<PriceRowSpec> rows)
    {
        var suspects = rows
            .Where(r => string.IsNullOrWhiteSpace(r.UserGroupId) && !string.IsNullOrWhiteSpace(r.CustomerGroupId))
            .Select(r => r.CustomerGroupId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (suspects.Count == 0)
            yield break;

        var groups = UserManagementServices.UserGroups.GetGroups().ToList();
        var customerNumbers = groups.Select(g => g.CustomerNumber).Where(c => !string.IsNullOrEmpty(c)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var groupIds = groups.Select(g => g.ID.ToString()).ToHashSet();

        foreach (var value in suspects)
        {
            if (customerNumbers.Contains(value))
                continue;
            var rowIds = string.Join(", ", rows.Where(r => string.Equals(r.CustomerGroupId, value, StringComparison.OrdinalIgnoreCase)).Select(r => r.Id));
            yield return groupIds.Contains(value)
                ? $"Price row(s) {rowIds} use the legacy customer-group column with value '{value}' — that column is matched against a group's customer number, not its id, and no group has customer number '{value}'. If group {value} was intended, set the row's user group instead; as it stands the row applies to nobody"
                : $"Price row(s) {rowIds} target customer group '{value}', but no user group carries that customer number — the row applies to nobody";
        }
    }

    private static string Format(IPriceInfo price) =>
        $"{price.PriceWithoutVATFormatted} excl. VAT / {price.PriceWithVATFormatted} incl.";

    private static DiscountSpec ToSpec(Discount d, string languageId)
    {
        var needsOrder = new List<string>();
        if (d.ProductQuantity > 1) needsOrder.Add($"product quantity ≥ {d.ProductQuantity:0.##} in cart");
        if (d.OrderTotalPrice > 1) needsOrder.Add($"order total {d.OrderTotalPriceCondition} {d.OrderTotalPrice:0.##}");
        if (!string.IsNullOrWhiteSpace(d.OrderFieldName)) needsOrder.Add($"order field '{d.OrderFieldName}' = '{d.OrderFieldValue}'");
        if (d.VoucherListId is int v && v > 0) needsOrder.Add("voucher code");
        if (d.UsePerCustomer > 0) needsOrder.Add($"max {d.UsePerCustomer} use(s) per customer");

        var type = d.DiscountType switch
        {
            DiscountTypes.Amount => $"{d.Amount:0.##} {d.CurrencyCode} off",
            DiscountTypes.Percentage => $"{d.Percentage:0.##}% off",
            _ => d.DiscountType.ToString()
        };
        if (!string.IsNullOrEmpty(d.ExtenderType))
            type += $" (extender: {d.ExtenderType.Split(',')[0].Split('.').Last()})";

        var scope = d.HasProductRestrictions
            ? $"restricted — {DescribeScope(d.ProductsAndGroupsIds)}{(d.ExcludedProductsAndGroupsIds?.Length > 6 ? $", excluding {DescribeScope(d.ExcludedProductsAndGroupsIds)}" : "")}"
            : "all products";

        return new DiscountSpec
        {
            Id = d.ID,
            Name = d.GetName(languageId) is { Length: > 0 } n ? n : d.Name,
            Active = d.Active,
            Priority = d.Priority,
            ValidFrom = d.ValidFrom,
            ValidTo = d.ValidTo,
            CurrencyCode = d.CurrencyCode ?? string.Empty,
            CountryCode = d.CountryCode2 ?? string.Empty,
            ShopId = d.ShopId ?? string.Empty,
            LanguageId = d.LanguageId ?? string.Empty,
            AnonymousUsers = d.AnonymousUsers,
            UserId = d.UserId,
            UserGroupId = d.UserGroupId,
            UserCustomerNumber = d.UserCustomerNumber ?? string.Empty,
            ProductScope = scope,
            NeedsOrder = needsOrder.Count > 0,
            NeedsOrderReason = string.Join("; ", needsOrder),
            TypeDescription = type,
            StopFurtherProcessing = d.StopFurtherProcessing,
            OnlyApplyToNonDiscountedItems = d.OnlyApplyToNonDiscountedItems
        };
    }

    /// <summary>"[p:PROD1][g:GROUP1]..." → "2 product(s), 1 group(s)" with the first few ids.</summary>
    private static string DescribeScope(string? ids)
    {
        if (string.IsNullOrEmpty(ids))
            return "nothing";
        var parts = ids.Split(['[', ']', ','], StringSplitOptions.RemoveEmptyEntries);
        var products = parts.Where(p => p.StartsWith("p:", StringComparison.OrdinalIgnoreCase)).Select(p => p[2..]).ToList();
        var groups = parts.Where(p => p.StartsWith("g:", StringComparison.OrdinalIgnoreCase)).Select(p => p[2..]).ToList();
        var other = parts.Length - products.Count - groups.Count;
        var bits = new List<string>();
        if (products.Count > 0) bits.Add($"{products.Count} product(s): {string.Join(", ", products.Take(3))}{(products.Count > 3 ? ", …" : "")}");
        if (groups.Count > 0) bits.Add($"{groups.Count} group(s): {string.Join(", ", groups.Take(3))}{(groups.Count > 3 ? ", …" : "")}");
        if (other > 0) bits.Add($"{other} other selector(s)");
        return bits.Count == 0 ? ids : string.Join("; ", bits);
    }

    private static Currency? ResolveCurrency(string code, User? user)
    {
        if (!string.IsNullOrEmpty(code) && Services.Currencies.GetCurrency(code) is { } c)
            return c;
        if (user is not null && !string.IsNullOrEmpty(user.Currency) && Services.Currencies.GetCurrency(user.Currency) is { } uc)
            return uc;
        return Services.Currencies.GetDefaultCurrency();
    }

    private static Country? ResolveCountry(string code, User? user, string languageId)
    {
        if (!string.IsNullOrEmpty(code) && Services.Countries.GetCountry(code) is { } c)
            return c;
        if (user is not null && !string.IsNullOrEmpty(user.CountryCode) && Services.Countries.GetCountry(user.CountryCode) is { } uc)
            return uc;
        // Mirrors CountryService.GetDefaultCountry (internal): the language's country, else the first one.
        var language = Services.Languages.GetLanguage(languageId);
        if (language is not null && !string.IsNullOrEmpty(language.CountryCode) && Services.Countries.GetCountry(language.CountryCode) is { } lc)
            return lc;
        return Services.Countries.GetCountries().FirstOrDefault();
    }

    private static Shop? ResolveShop(string shopId, Product product)
    {
        if (!string.IsNullOrEmpty(shopId) && Services.Shops.GetShop(shopId) is { } s)
            return s;
        var group = product.GetDefaultGroup();
        if (group is not null && !string.IsNullOrEmpty(group.ShopId) && Services.Shops.GetShop(group.ShopId) is { } gs)
            return gs;
        return Services.Shops.GetDefaultShop();
    }

    // ---- Pickers -----------------------------------------------------------------------------

    public static IReadOnlyList<(string Code, string Name)> Currencies() =>
        Services.Currencies.GetAllCurrencies().Select(c => (c.Code, c.GetName(Services.Languages.GetDefaultLanguageId()) ?? c.Code)).ToList();

    public static IReadOnlyList<(string Id, string Name)> Shops() =>
        Services.Shops.GetShops().Select(s => (s.Id, s.Name)).ToList();
}
