using Dynamicweb.Configuration;
using Dynamicweb.Ecommerce;
using Dynamicweb.Ecommerce.International;
using Dynamicweb.Ecommerce.Orders.Discounts;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Ecommerce.Shops;
using Dynamicweb.Security.UserManagement;
using Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;
using Truvio.Commerce.PowerTools.Shared.Currency;

namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Dw;

/// <summary>
/// Builds the <see cref="ExplainReport"/> against the DW runtime: reads the raw price matrix,
/// assortments and discounts into evaluator specs (the "why"), and runs DW's own
/// PriceManager / DiscountInfoCollection for the same context (the "what") so both are shown
/// side by side. Read-only.
/// </summary>
public sealed class DwCommerceExplainer
{
#if DW_HAS_PRICE_DISCOUNT_PERCENTAGE
    private static readonly bool HasDiscountApi = true;
#else
    private static readonly bool HasDiscountApi = false;
#endif
#if DW_HAS_PRICE_GROUPS
    private static readonly bool HasGroupApi = true;
#else
    private static readonly bool HasGroupApi = false;
#endif

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

        var currency = ResolveCurrency(request.CurrencyCode, user)
            ?? throw new InvalidOperationException("No currency is configured for this context.");
        var country = ResolveCountry(request.CountryCode, user, languageId)
            ?? throw new InvalidOperationException("No VAT country is configured for this context.");
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
            ProductId = product.Id,
#if DW_HAS_PRICE_GROUPS
            CustomerPriceGroupId = user?.CustomerPriceGroupId ?? string.Empty,
            CustomerDiscountGroupId = user?.CustomerDiscountGroupId ?? string.Empty,
            ProductDiscountGroupId = product.DiscountGroupId ?? string.Empty,
#endif
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

        var rows = DwPriceRows.Read(product);
        var providers = InstalledPriceProviders();
        var hostVersion = typeof(PriceManager).Assembly.GetName().Version ?? new Version(0, 0, 0);
        var rule = PriceRuleVersions.Resolve(hostVersion, HasDiscountApi, HasGroupApi);
        if (providers.Count > 0) rule = PriceSelectionRule.Unknown;
        var matches = Services.Prices.FindPrices(priceContext,
            new PriceProductSelection(product, product.DefaultUnitId, 0, quantity, 0), false).ToList();
        // DW enumeration order decides ties. Retain it when joining candidate evidence.
        var order = matches.Select((p, i) => (p.Id, i)).ToDictionary(p => p.Id, p => p.i);
        rows = rows.OrderBy(r => order.GetValueOrDefault(r.Id, int.MaxValue)).ToList();
        var matrix = PriceRowEvaluator.Evaluate(rows, lookup, rule, order.Keys.ToHashSet());
        warnings.AddRange(LegacyCustomerGroupWarnings(rows).Take(10));
        if (matrix.HasTie) warnings.Add("Matching rows tie at the winning net price and priority; DW's row order decides the winner.");
        if (rule == PriceSelectionRule.Unknown)
            warnings.Add($"No winning row is claimed: host {hostVersion} has an unverified selection rule, this package lacks its APIs, or a custom provider is installed. Rows show DW's standard matching set only.");
        if (!HasDiscountApi)
            warnings.Add("This Base package cannot read found discount percentages or allow flags. Install the Discount or Groups line on a compatible host.");
        if (!HasGroupApi && hostVersion >= new Version(10, 30))
            warnings.Add("This package cannot read price-group candidates. Install the Groups line for complete row evidence.");

        // ---- DW's own price -----------------------------------------------------------------
        // Not PriceManager.GetPrice: that overload builds a PriceProductSelection without a
        // quantity (so DW prices at quantity 1, and price providers see Quantity = 1) and caches
        // the result under a key that ignores quantity. FindPrice with an explicit selection runs
        // the same provider chain for the requested quantity (issue #1).
        var dwPrice = FindDwPrice(priceContext, product, quantity);
        if (dwPrice.PriceSource == PriceSource.CustomProvider && rule != PriceSelectionRule.Unknown)
        {
            rule = PriceSelectionRule.Unknown;
            matrix = PriceRowEvaluator.Evaluate(rows, lookup, rule, order.Keys.ToHashSet());
        }
        var exclusive = providers.Where(p => p.Exclusive).Select(p => p.Name).ToList();
        var dwSource = dwPrice.PriceSource switch
        {
            PriceSource.PriceMatrix => "Price matrix",
            PriceSource.ProductDefault => "Product default price",
            PriceSource.CustomProvider => "Custom price provider",
            null when dwPrice.Price == 0 && exclusive.Count > 0 => "No price",
            _ => "Unknown"
        };

        if (dwPrice.PriceSource is null && dwPrice.Price == 0 && exclusive.Count > 0)
            warnings.Add($"SELLS AT ZERO: the exclusive price provider {string.Join(", ", exclusive)} returned no price for this context, and DW does not fall back to the product default price when an exclusive provider returns nothing (PriceManager.FindPrice) — the storefront shows {Format(dwPrice)}");
        else if (dwPrice.PriceSource == PriceSource.CustomProvider || exclusive.Count > 0)
            warnings.Add($"A custom price provider is installed ({string.Join(", ", providers.Select(p => p.Name + (p.Exclusive ? " - exclusive" : "")))}) — the rows below show DW's standard matching set, which the provider {(exclusive.Count > 0 ? "replaces" : "may override")}");
        else if (rule != PriceSelectionRule.Unknown && dwPrice.PriceSource == PriceSource.PriceMatrix && matrix.Winner is null)
            warnings.Add("DW reports a price-matrix price but the built-in filters match no row — a price subscriber or provider is intervening");
        else if (dwPrice.PriceSource == PriceSource.ProductDefault && matrix.Winner is not null)
            warnings.Add("DW fell back to the product's default price although a matrix row matches — check for a custom provider or notification subscriber");

        // ---- Currency conversion sanity ------------------------------------------------------
        var conversion = currency is null ? null : SafeConversion(currency.Code);

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

        var selection = SystemConfiguration.Instance.GetValue("/Globalsettings/Ecom/Order/Discounts/Selection");
        if (string.IsNullOrEmpty(selection)) selection = "acc";
        var selectionText = selection.ToLowerInvariant() switch
        {
            "high" => "Apply only the highest discount",
            "low" => "Apply only the lowest discount",
            _ => "Accumulate all applicable discounts"
        };
        var campaignsAvailable = true;
#if DW_HAS_PRICE_DISCOUNT_PERCENTAGE
#pragma warning disable DWEX100001 // Only detecting the unsupported adjustments experience.
        if (Dynamicweb.Core.Feature.IsActive<Dynamicweb.Ecommerce.Orders.Adjustments.AdjustmentsFeature>())
        {
            campaignsAvailable = false;
            warnings.Add("New Discount Experience is active: campaign discounts and the final price are not explained.");
        }
#endif
#pragma warning restore DWEX100001
        var infos = new List<DiscountInfo>();
        if (campaignsAvailable)
        {
            try
            {
                infos = new DiscountInfoCollection(priceContext, product, product.DefaultUnitId, 0)
                    .Where(i => !ToSpec(i.Discount, languageId).NeedsOrder).ToList();
            }
            catch (Exception ex)
            {
                campaignsAvailable = false;
                warnings.Add($"DW discount calculation failed: {ex.Message}");
            }
        }
        warnings.Add("Campaign amounts use the chosen quantity's unit price. Cart-dependent conditions (quantity requirements, vouchers, order totals, usage and order fields) need a real cart and are not included. Storefront template customizations are outside this explanation.");
        var calculated = DwCampaignPrices.Calculate(priceContext, product, dwPrice, infos, selection,
            hostVersion == new Version(10, 8, 4, 0));
        var complete = campaignsAvailable && calculated.Percentage is not null;
        if (calculated.QuantityOneAmounts.Count > 0)
            warnings.Add("Some campaign amounts come from DW at quantity 1 (extender or amount-from-field). They are marked in Discounts; final amounts need cart verification.");

        var validDiscountIds = infos.Select(i => i.Discount.ID).ToHashSet();
        var discountBlockNote = string.Empty;
#if DW_HAS_PRICE_DISCOUNT_PERCENTAGE
        if (!dwPrice.AllowOrderLineDiscounts) discountBlockNote = "The found price disallows order-line discounts";
#endif
        var scopedIds = Services.Discounts.GetDiscounts(product, false).Select(d => d.ID).ToHashSet();
        foreach (var group in product.Groups)
            scopedIds.UnionWith(Services.Discounts.GetDiscounts(group, false).Select(d => d.ID));
        var configuredCap = Features.Settings.Dw.DwPowerToolsSettings.Current.PriceRowCap;
        var rowCap = configuredCap > 0 ? configuredCap : 100;
        var discounts = new List<DiscountExplanation>();
        var rejectedDiscounts = new Dictionary<string, int>();
        var discountSamples = new Dictionary<string, List<DiscountExplanation>>();
        void RejectDiscount(string reason, Discount d, DiscountVerdict? verdict = null)
        {
            rejectedDiscounts[reason] = rejectedDiscounts.GetValueOrDefault(reason) + 1;
            if (!discountSamples.TryGetValue(reason, out var sample)) discountSamples[reason] = sample = [];
            if (sample.Count < rowCap) sample.Add(new DiscountExplanation
            {
                Verdict = verdict ?? DiscountCandidateEvaluator.Evaluate(ToSpec(d, languageId), discountLookup),
                CalculationNote = reason
            });
        }
        var applicableDiscountCount = 0;
        foreach (var d in Services.Discounts.GetActiveOrderLineDiscounts())
        {
            var appliedByDw = calculated.Applied.ContainsKey(d.ID);
            var inScope = appliedByDw || scopedIds.Contains(d.ID) || !d.HasProductRestrictions;
            if (!inScope)
            {
                RejectDiscount("Other product scope", d);
                continue;
            }
            var verdict = DiscountCandidateEvaluator.Evaluate(ToSpec(d, languageId), discountLookup);
            if (!appliedByDw && !verdict.PassesBaseChecks)
            {
                var reason = verdict.RejectionReason;
                RejectDiscount(reason, d, verdict);
                continue;
            }
            applicableDiscountCount++;
            // Retain only the bounded evidence; all applied amounts have already been summed.
            if (discounts.Count >= rowCap && !appliedByDw) continue;
            discounts.Add(new DiscountExplanation
            {
                Verdict = verdict,
                AppliedByDw = appliedByDw,
                Amount = calculated.Applied.TryGetValue(d.ID, out var amount) ? Format(amount) : string.Empty,
                CalculationNote = !campaignsAvailable ? "Campaign calculation is not explained for this context"
                    : !string.IsNullOrEmpty(discountBlockNote) ? discountBlockNote
                    : validDiscountIds.Contains(d.ID) && !appliedByDw ? $"DW-valid, but not selected by {selectionText}"
                    : calculated.QuantityOneAmounts.Contains(d.ID) ? "Computed by DW at quantity 1" : ""
            });
        }
        discounts = discounts.OrderByDescending(d => d.AppliedByDw).ThenBy(d => d.Verdict.Discount.Priority).Take(rowCap).ToList();

        var breaks = Services.Prices.FindQuantityPrices(priceContext, product, false)
            .Where(p => string.IsNullOrEmpty(p.UnitId) || p.UnitId == product.DefaultUnitId)
            .Where(p => p.StockLocationId == 0)
            .Select(p => p.Quantity).Where(q => q > 0).Append(1).Distinct().Order().ToList();
        var steps = new List<PriceStep>();
        foreach (var q in breaks)
        {
            var price = q == quantity ? dwPrice : FindDwPrice(priceContext, product, q);
            var values = q == quantity ? calculated : DwCampaignPrices.Calculate(priceContext, product, price, infos, selection,
                hostVersion == new Version(10, 8, 4, 0));
            var stepMatches = Services.Prices.FindPrices(priceContext,
                new PriceProductSelection(product, product.DefaultUnitId, 0, q, 0), false).ToList();
            var stepOrder = stepMatches.Select((p, i) => (p.Id, i)).ToDictionary(p => p.Id, p => p.i);
            var stepMatrix = PriceRowEvaluator.Evaluate(rows.OrderBy(r => stepOrder.GetValueOrDefault(r.Id, int.MaxValue)),
                lookup with { Quantity = q }, rule, stepOrder.Keys.ToHashSet());
            var source = stepMatrix.Winner is { } w ? $"{w.Row.TargetKind} (row {w.Row.Id})"
                : providers.Count > 0 ? "Provider price; winner not claimed"
                : rule == PriceSelectionRule.Unknown ? "DW price; winner not claimed" : "Product default price";
            steps.Add(new PriceStep(q, Format(price), DiscountText(values), campaignsAvailable ? Format(values.Campaigns) : "Not explained",
                complete ? Format(values.Final) : "Not explained", source));
        }
        if (quantity != 1 && complete)
        {
            var pagePrice = FindDwPrice(priceContext, product, 1);
            var page = DwCampaignPrices.Calculate(priceContext, product, pagePrice, infos, selection, hostVersion == new Version(10, 8, 4, 0));
            if (Math.Abs(page.Final.PriceWithVAT - calculated.Final.PriceWithVAT) > 0.005)
                warnings.Add($"The standard product-page quantity-1 final price is {Format(page.Final)}; it differs from the chosen quantity's unit price.");
        }
        if (rule == PriceSelectionRule.Unknown && dwPrice.PriceSource == PriceSource.PriceMatrix && providers.Count == 0)
        {
            var agrees = matrix.Rows.Where(v => v.Matches).Any(v =>
            {
                var amount = PriceCalculated.Create(priceContext, new PriceRaw(v.ComparableAmount,
                    string.IsNullOrEmpty(v.Row.CurrencyCode) ? Services.Currencies.GetDefaultCurrency()
                        : Services.Currencies.GetCurrency(v.Row.CurrencyCode)), product);
                amount.Calculate();
                return Math.Abs(amount.PriceWithVAT - dwPrice.PriceWithVAT) <= 0.005;
            });
            if (!agrees) warnings.Add("DW's matrix amount matches none of the available comparable rows. The candidate evidence may be incomplete, or a subscriber/provider changed the price.");
        }
        if (matrix.Winner is { } mirror && providers.Count == 0)
        {
            var comparison = PriceCalculated.Create(priceContext,
                new PriceRaw(mirror.ComparableAmount, string.IsNullOrEmpty(mirror.Row.CurrencyCode)
                    ? Services.Currencies.GetDefaultCurrency() : Services.Currencies.GetCurrency(mirror.Row.CurrencyCode)), product);
            comparison.Calculate();
            if (Math.Abs(comparison.PriceWithVAT - dwPrice.PriceWithVAT) > 0.005)
                warnings.Add("DW and the mirror disagree on the winning amount. Check providers, subscribers and currency conversion.");
        }
        var flags = "Unavailable in Base line";
#if DW_HAS_PRICE_DISCOUNT_PERCENTAGE
        flags = $"Allow order discounts: {dwPrice.AllowOrderDiscounts}; allow order-line discounts: {dwPrice.AllowOrderLineDiscounts}";
#endif
        var fallbackSources = matrix.Rows.Where(v => v.Matches && v.Row.DiscountPercentage == matrix.FallbackDiscountPercentage
            && (rule == PriceSelectionRule.GroupsPriorityThenLowestNet || v.ComparableAmount == 0)).Select(v => v.Row.Id).ToList();
        var discountReason = !HasDiscountApi ? "Found discount percentages are unavailable in the Base line."
            : rule == PriceSelectionRule.Unknown ? "Returned by the price provider; row provenance is not claimed."
            : $"{rule}; fallback {matrix.FallbackDiscountPercentage:0.##}% from matching rows {string.Join(", ", fallbackSources.Take(5))}{(fallbackSources.Count > 5 ? " (more sources omitted)" : "")}. " +
                (matrix.Winner is { } winning ? $"Row {winning.Row.Id}: own {winning.Row.DiscountPercentage:0.##}%, effective {winning.EffectiveDiscountPercentage:0.##}%." : "Product default price fallback.");

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
            DwDiscountTotal = campaignsAvailable ? Format(calculated.Campaigns) : "Not explained",
            DwFinalPrice = complete ? Format(calculated.Final) : "Not explained",
            DiscountDetailsAvailable = HasDiscountApi,
            FoundDiscount = DiscountText(calculated),
            FoundDiscountReason = discountReason,
            LineTotal = complete ? Format(calculated.Final.Multiply(quantity)) : "Not explained",
            AllowFlags = flags,
            Steps = steps,
            DiscountRejectedCounts = rejectedDiscounts,
            DiscountRejectedSamples = discountSamples,
            ApplicableDiscountCount = applicableDiscountCount,
            QuantityPrices = quantityPrices,
            Warnings = warnings,
            Conversion = conversion
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
            var rowIds = string.Join(", ", rows.Where(r => string.Equals(r.CustomerGroupId, value, StringComparison.OrdinalIgnoreCase)).Take(10).Select(r => r.Id));
            yield return groupIds.Contains(value)
                ? $"Price row(s) {rowIds} use the legacy customer-group column with value '{value}' — that column is matched against a group's customer number, not its id, and no group has customer number '{value}'. If group {value} was intended, set the row's user group instead; as it stands the row applies to nobody"
                : $"Price row(s) {rowIds} target customer group '{value}', but no user group carries that customer number — the row applies to nobody";
        }
    }

    /// <summary>Custom PriceProvider add-ins DW would consult (DefaultPriceProvider excluded), with their exclusivity.</summary>
    private static IReadOnlyList<(string Name, bool Exclusive)> InstalledPriceProviders()
    {
        try
        {
            return Dynamicweb.Extensibility.AddIns.AddInManager.GetTypes<PriceProvider>()
                .Where(t => t != typeof(DefaultPriceProvider))
                .Select(t =>
                {
                    var exclusive = false;
                    try { exclusive = Dynamicweb.Extensibility.AddIns.AddInManager.GetInstance<PriceProvider>(t)?.HandlePricesExclusively ?? false; } catch { }
                    return (t.Name, exclusive);
                })
                .ToList();
        }
        catch
        {
            return [("Provider discovery unavailable", false)];
        }
    }

    /// <summary>
    /// DW's price for the requested quantity, with the price source preserved. Mirrors the
    /// private PriceManager.CalculatePrice: a provider may return a finished PriceInfo
    /// (IPriceInfoProvider) or a PriceRaw that still needs VAT and rounding applied.
    /// </summary>
    private static PriceInfo FindDwPrice(PriceContext priceContext, Product product, double quantity)
    {
        var selection = new PriceProductSelection(product, product.DefaultUnitId, 0, quantity, 0);
        var found = PriceManager.FindPrice(priceContext, selection, isInformative: false);
        switch (found)
        {
            case PriceInfo info:
                return info;
            case PriceRaw raw:
                var calculated = PriceCalculated.Create(priceContext, raw, product);
                calculated.IsInformative = false;
                calculated.Calculate();
                return calculated;
            default:
                return new PriceInfo(priceContext.Currency);
        }
    }

    private static string DiscountText(DwPriceCalculation value) => HasDiscountApi && value.Percentage is double percentage
        ? $"{percentage:0.##}% / {Format(value.FoundDiscount)}" : "Unavailable in Base line";

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
            Name = d.GetName(languageId) is { Length: > 0 } n ? n : d.Name ?? string.Empty,
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

    /// <summary>The conversion row must never take the whole explanation down with it.</summary>
    private static ConversionExplanation? SafeConversion(string contextCurrencyCode)
    {
        try
        {
            return CurrencyGuard.ExplainConversion(DwCurrencyReader.GetCurrencies(), contextCurrencyCode);
        }
        catch
        {
            return null;
        }
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
        Services.Shops.GetShops().Select(s => (s.Id, s.Name ?? s.Id)).ToList();
}
