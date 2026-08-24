using Dynamicweb.Ecommerce;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Ecommerce.Products.CompletionRules;

namespace Truvio.Commerce.PowerTools.Core.Pim.Dw;

/// <summary>
/// <see cref="IPimQualitySource"/> backed by the live DW runtime. Strictly read-only: product
/// enumeration and completeness through DW's own services, asset and image state through the
/// product services. Nothing here writes, deletes, or recalculates a score DW owns.
/// <para>
/// Every read is wrapped: one unreadable part of the catalog degrades that section to empty
/// rather than failing the screen — the convention the Operations source already follows.
/// </para>
/// <para>
/// Deliberately does <b>not</b> reference <c>CompletenessFeature</c>: that flag gates DW's own
/// v2 completeness UI and does not exist at the 10.8 floor. The scores below are computed the
/// same way whether the flag is on or off, so PowerTools reports completeness even on installs
/// where DW's own completeness column is switched off.
/// </para>
/// </summary>
public sealed class DwPimSource : IPimQualitySource
{
    /// <summary>Products whose variant, asset and image state is inspected in one catalog-wide pass.</summary>
    public const int CatalogPassCap = 500;

    /// <summary>Above this many potential combinations the source reports the number only.</summary>
    private const ulong EnumerationCeiling = Rules.VariantGapRule.LargeCombinationCount;

    public IReadOnlyList<(string Id, string Name)> GetGroups() =>
        Safe(() => Services.ProductGroups.GetGroups(DefaultLanguage())
            .Select(g => (g.Id, Name: string.IsNullOrEmpty(g.Name) ? g.Id : g.Name))
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList());

    public IReadOnlyList<(string Id, string Name)> GetLanguages() =>
        Safe(() =>
        {
            var defaultId = DefaultLanguage();
            return Services.Languages.GetLanguages()
                .Select(l => (l.LanguageId, Name: string.IsNullOrEmpty(l.Name) ? l.LanguageId : l.Name))
                // Default first: every language comparison is measured against it.
                .OrderByDescending(l => string.Equals(l.LanguageId, defaultId, StringComparison.OrdinalIgnoreCase))
                .ThenBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        });

    public (IReadOnlyList<ProductQuality> Products, int TotalCount) GetProductQuality(PimScope scope)
    {
        try
        {
            var languageId = Language(scope);
            var search = Search(scope, languageId);
            if (search.Products.Count == 0)
                return ([], search.TotalCount);

            var options = OptionsFor(languageId, [languageId]);
            var scores = Scores(search.Products.Select(p => p.Id), options);

            var products = search.Products
                .Select(product => Quality(product, languageId, scores, options))
                .OrderBy(p => p.Score)
                .ThenBy(p => p.Number, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return (products, search.TotalCount);
        }
        catch
        {
            return ([], 0);
        }
    }

    public ProductQuality? GetProductDetail(string productId, string languageId)
    {
        try
        {
            if (string.IsNullOrEmpty(productId))
                return null;

            var language = string.IsNullOrEmpty(languageId) ? DefaultLanguage() : languageId;
            var product = Services.Products.GetProductById(productId, string.Empty, language);
            if (product is null)
                return null;

            var languages = GetLanguages().Select(l => l.Id).ToList();
            var options = OptionsFor(language, languages);
            var scores = Scores([productId], options);
            var master = Quality(product, language, scores, options);

            // Per-language scores for the family: the language matrix on the detail screen.
            var perLanguage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var pair in Services.CompletionRules.CalculateProductCompletenessForLanguages(productId, options, languages))
                    perLanguage[pair.Key] = pair.Value?.Value ?? 0;
            }
            catch
            {
                // A per-language failure leaves the matrix empty; the family score still renders.
            }

            var variants = Safe(() => Services.Products.GetProductsAndVariantsByProduct(product)
                .Where(p => !string.IsNullOrEmpty(p.VariantId))
                .Select(p => Quality(p, language, scores, options))
                .OrderBy(p => p.VariantId, StringComparer.OrdinalIgnoreCase)
                .ToList());

            return master with { Variants = variants, ScorePerLanguage = perLanguage };
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<RuleUsage> GetRules() =>
        Safe(() =>
        {
            var rules = Services.CompletionRules.GetAll().ToList();
            if (rules.Count == 0)
                return [];

            // Bulk overload: one round-trip for every rule's assignments.
            var usages = new Dictionary<int, List<string>>();
            try
            {
                foreach (var pair in Services.CompletionRules.GetUsages(rules))
                    usages[pair.Key.Id] = pair.Value?.Select(Describe).ToList() ?? [];
            }
            catch
            {
                // No usage data: every rule renders with unknown assignments rather than
                // being falsely reported as dead.
                return rules.Select(r => Usage(r, ["(assignments could not be read)"])).ToList();
            }

            return rules
                .Select(r => Usage(r, usages.TryGetValue(r.Id, out var u) ? u : []))
                .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        });

    public IReadOnlyList<VariantGap> GetVariantGaps(PimScope scope) =>
        Safe(() =>
        {
            var languageId = Language(scope);
            var gaps = new List<VariantGap>();

            foreach (var product in CatalogPass(scope, languageId))
            {
                // Variants themselves have no combination space of their own.
                if (!string.IsNullOrEmpty(product.VariantId))
                    continue;

                var potential = Try(() => Services.Variants.PotentialVariantCount(product.Id));
                if (potential is not > 1)
                    continue;

                var existing = Try(() => Services.VariantCombinations.GetVariantCombinations(product.Id))?.ToList() ?? [];
                if ((ulong)existing.Count >= potential.Value)
                    continue;

                // Enumerate examples only for a combination space small enough to be a real
                // data gap; anything larger is reported as a number.
                var examples = new List<string>();
                if (potential.Value <= EnumerationCeiling)
                {
                    var groups = Try(() => Services.VariantGroups.GetVariantGroupsByProductId(product.Id));
                    var all = groups is null
                        ? null
                        : Try(() => Services.VariantCombinations.GetAllPossibleVariantIds(groups, languageId));

                    if (all is not null)
                    {
                        var have = existing.Select(c => c.VariantId).ToHashSet(StringComparer.OrdinalIgnoreCase);
                        examples = all.Where(id => !have.Contains(id)).Take(5).ToList();
                    }
                }

                gaps.Add(new VariantGap(
                    product.Id,
                    product.Number ?? string.Empty,
                    product.Name ?? string.Empty,
                    potential.Value,
                    existing.Count,
                    examples));
            }

            return gaps;
        });

    public IReadOnlyList<DuplicateAsset> GetDuplicateAssets(PimScope scope) =>
        Safe(() =>
        {
            var languageId = Language(scope);
            var products = CatalogPass(scope, languageId);
            if (products.Count == 0)
                return [];

            var byId = products.GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
            var keys = products.Select(p => new ProductKey(p)).ToList();

            var details = Try(() => Services.Details.GetDetailsBulk(keys, null, false));
            if (details is null)
                return [];

            var duplicates = new List<DuplicateAsset>();
            foreach (var entry in details)
            {
                var rows = entry.Value;
                if (rows is null || rows.Count < 2)
                    continue;

                foreach (var group in rows
                             .Where(d => !string.IsNullOrEmpty(d.Value))
                             .GroupBy(d => d.Value, StringComparer.OrdinalIgnoreCase)
                             .Where(g => g.Count() > 1))
                {
                    var productId = group.First().ProductId;
                    if (!byId.TryGetValue(productId, out var product))
                        continue;

                    duplicates.Add(new DuplicateAsset(
                        productId,
                        product.Number ?? string.Empty,
                        product.Name ?? string.Empty,
                        group.Key,
                        group.Count()));
                }
            }

            return duplicates;
        });

    public IReadOnlyList<BrokenImage> GetBrokenImages(PimScope scope) =>
        Safe(() =>
        {
            var languageId = Language(scope);
            var broken = new List<BrokenImage>();

            foreach (var product in CatalogPass(scope, languageId))
            {
                // GetImagePath resolves patterns and the group/shop default; an empty result
                // is "no image", which is a completeness question, not a broken link.
                var path = Try(() => Services.ProductImages.GetImagePath(product));
                if (string.IsNullOrEmpty(path))
                    continue;

                var exists = Try(() => (bool?)Services.ProductImages.FileExists(path));
                if (exists is false)
                {
                    broken.Add(new BrokenImage(
                        product.Id,
                        product.Number ?? string.Empty,
                        product.Name ?? string.Empty,
                        path));
                }
            }

            return broken;
        });

    public IReadOnlyList<CategoryUsage> GetCategories() =>
        Safe(() =>
        {
            var usages = Try(() => Services.ProductCategories.GetCategoriesUsages())
                         ?? new Dictionary<string, int>();

            return Services.ProductCategories.GetCategories()
                .Select(c => new CategoryUsage(
                    c.Id,
                    string.IsNullOrEmpty(c.GetName(DefaultLanguage())) ? c.Id : c.GetName(DefaultLanguage()),
                    usages.TryGetValue(c.Id, out var count) ? count : 0))
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        });

    public IReadOnlyList<WorkflowUsage> GetWorkflows() =>
        Safe(() =>
        {
            // The extensions hang off a WorkflowService instance. DW marks manual construction
            // obsolete in favour of its container, but the resolver is not part of the public
            // surface at the 10.8 floor this package targets — and the ctor still works. The
            // whole read is inside Safe(), so a future removal degrades the section to empty
            // rather than breaking the screen.
#pragma warning disable CS0618 // Type or member is obsolete
            var service = new Dynamicweb.Security.Workflows.WorkflowService();
#pragma warning restore CS0618
            var byGroups = Try(() => Dynamicweb.Ecommerce.Workflows.WorkflowServiceExtensions
                .GetWorkflowsInUseByGroups(service)?.ToList()) ?? [];
            var byProducts = Try(() => Dynamicweb.Ecommerce.Workflows.WorkflowServiceExtensions
                .GetWorkflowsInUseByProducts(service)?.ToList()) ?? [];

            var names = byGroups.Concat(byProducts)
                .Select(w => w?.Name ?? string.Empty)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

            var groupNames = byGroups.Select(w => w?.Name).Where(n => !string.IsNullOrEmpty(n)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var productNames = byProducts.Select(w => w?.Name).Where(n => !string.IsNullOrEmpty(n)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            return names
                .Select(n => new WorkflowUsage(n, groupNames.Contains(n), productNames.Contains(n)))
                .ToList();
        });

    public PimSnapshot Snapshot(PimScope scope, bool includeCatalogWide = true)
    {
        var (products, total) = GetProductQuality(scope);
        var languages = GetLanguages().Select(l => l.Id).ToList();

        return new PimSnapshot(
            products,
            GetRules(),
            includeCatalogWide ? GetVariantGaps(scope) : [],
            includeCatalogWide ? GetDuplicateAssets(scope) : [],
            includeCatalogWide ? GetBrokenImages(scope) : [],
            includeCatalogWide ? GetCategories() : [],
            scope,
            total)
        {
            Workflows = includeCatalogWide ? GetWorkflows() : [],
            Languages = languages
        };
    }

    // ---- Internals -----------------------------------------------------------------------------

    private static string DefaultLanguage() => Services.Languages.GetDefaultLanguageId();

    private static string Language(PimScope scope) =>
        string.IsNullOrEmpty(scope.LanguageId) ? DefaultLanguage() : scope.LanguageId;

    /// <summary>The scoped, capped product page every screen starts from.</summary>
    private static (IList<Product> Products, int TotalCount) Search(PimScope scope, string languageId)
    {
        var filter = new ProductSearchFilter
        {
            SearchValue = scope.Search ?? string.Empty,
            LanguageIds = [languageId],
            PageNumber = 1,
            PageSize = scope.EffectiveCap,
            IncludeOrphanedProducts = true,
            // Family rows: variants are unfolded on the detail screen, not listed here.
            VariantFilter = ProductSearchFilter.VariantStateFilter.Masters
        };

        if (scope.HasGroup)
            filter.GroupIds = [scope.GroupId];

        var result = Services.Products.GetProductsBySearch(filter);
        return (result.Products, result.TotalCount);
    }

    /// <summary>
    /// The catalog-wide passes (variants, assets, images) share one enumeration, capped
    /// independently of the score cap — these reads are cheaper per product than scoring.
    /// </summary>
    private static IReadOnlyList<Product> CatalogPass(PimScope scope, string languageId)
    {
        var filter = new ProductSearchFilter
        {
            SearchValue = scope.Search ?? string.Empty,
            LanguageIds = [languageId],
            PageNumber = 1,
            PageSize = Math.Max(scope.EffectiveCap, CatalogPassCap),
            IncludeOrphanedProducts = true,
            VariantFilter = ProductSearchFilter.VariantStateFilter.Masters
        };

        if (scope.HasGroup)
            filter.GroupIds = [scope.GroupId];

        return Services.Products.GetProductsBySearch(filter).Products.ToList();
    }

    private static CompletenessOptions OptionsFor(string defaultLanguageId, IEnumerable<string> languageIds) => new()
    {
        DefaultLanguageId = defaultLanguageId,
        LanguagesIds = languageIds.ToList(),
        Rules = Try(() => Services.CompletionRules.GetAll()?.ToList()) ?? []
    };

    private static IDictionary<string, CompletnessResult> Scores(IEnumerable<string> productIds, CompletenessOptions options) =>
        Try(() => Services.CompletionRules.CalculateProductCompletenessForMultipleFamilies(productIds.ToList(), options))
        ?? new Dictionary<string, CompletnessResult>();

    /// <summary>One product's row: DW's score plus the fields DW reports as missing AND in scope.</summary>
    private static ProductQuality Quality(
        Product product,
        string languageId,
        IDictionary<string, CompletnessResult> scores,
        CompletenessOptions options)
    {
        scores.TryGetValue(product.Id, out var result);

        var missing = new List<string>();
        var worstRule = string.Empty;
        var worstMissingCount = 0;

        if (result is not null)
        {
            foreach (var rule in options.Rules ?? [])
            {
                // A rule that excludes variants says nothing about a variant row.
                if (rule.ExcludeVariants && !string.IsNullOrEmpty(product.VariantId))
                    continue;

                var missingHere = 0;
                foreach (var field in Fields(rule))
                {
                    // Scope check first: an excluded field is not missing data, and reporting
                    // it makes every product look broken.
                    var excluded = Try(() => (bool?)result.ProductValueExcludedFromCalculations(
                        product.Id, product.VariantId ?? string.Empty, languageId, field));
                    if (excluded is not false)
                        continue;

                    var has = Try(() => (bool?)result.HasFieldValue(
                        product.Id, product.VariantId ?? string.Empty, languageId, field));
                    if (has is false)
                    {
                        missing.Add(field.SystemName);
                        missingHere++;
                    }
                }

                if (missingHere > worstMissingCount)
                {
                    worstMissingCount = missingHere;
                    worstRule = rule.Name ?? string.Empty;
                }
            }
        }

        return new ProductQuality(
            product.Id,
            product.VariantId ?? string.Empty,
            languageId,
            product.Number ?? string.Empty,
            product.Name ?? string.Empty,
            result?.Value ?? 0,
            worstRule,
            missing.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    /// <summary>
    /// The rule's fields as <see cref="ProductField"/> instances, which is what
    /// <c>HasFieldValue</c> needs.
    /// <para>
    /// Resolved from <c>GetAllEditableProductFields()</c> (public, keyed by system name)
    /// rather than through <c>CompletionRule.Fields</c>: that property is obsolete in favour
    /// of the system names, and DW's own resolver for them is internal. This is the same
    /// lookup DW performs, through the public surface.
    /// </para>
    /// </summary>
    private static IEnumerable<ProductField> Fields(CompletionRule rule) =>
        Try(() =>
        {
            var all = ProductField.GetAllEditableProductFields();
            return (rule.FieldSystemNames ?? [])
                .Select(name => all.TryGetValue(name, out var field) ? field : null)
                .Where(field => field is not null)
                .Select(field => field!)
                .ToList();
        }) ?? [];

    private static RuleUsage Usage(CompletionRule rule, IReadOnlyList<string> usages) => new(
        rule.Id,
        string.IsNullOrEmpty(rule.Name) ? $"Rule {rule.Id}" : rule.Name,
        Try(() => rule.FieldSystemNames?.ToList()) ?? [],
        rule.ExcludeVariants,
        usages);

    private static string Describe(CompletionSettingsSource source) =>
        string.IsNullOrEmpty(source.ParentName)
            ? $"{source.Type} '{source.Name}'"
            : $"{source.Type} '{source.Name}' ({source.ParentName})";

    /// <summary>A section that cannot be read degrades to empty, never to a broken screen.</summary>
    private static IReadOnlyList<T> Safe<T>(Func<IReadOnlyList<T>> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return [];
        }
    }

    private static T? Try<T>(Func<T?> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return default;
        }
    }
}
