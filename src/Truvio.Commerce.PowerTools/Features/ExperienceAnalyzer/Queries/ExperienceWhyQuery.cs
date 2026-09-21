using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.ContentAccess.Core;
using Truvio.Commerce.PowerTools.Features.ContentAccess.Dw;
using Truvio.Commerce.PowerTools.Features.ContentAccess.Queries;
using Truvio.Commerce.PowerTools.Features.ExperienceAnalyzer.Models;
using Truvio.Commerce.PowerTools.Shared.AdminUI;
using Truvio.Commerce.PowerTools.Shared.Principals;

namespace Truvio.Commerce.PowerTools.Features.ExperienceAnalyzer.Queries;

/// <summary>
/// The "Why?" slide-over for one page of the Experience Analyzer: both sides' full gate
/// explanations, with the page audit one further slide-over away. Everything the report row
/// used to spell out inline lives here instead.
/// </summary>
public sealed class ExperienceWhyQuery : DataQueryModelBase<ExperienceWhyModel>
{
    public string AccountKey { get; set; } = string.Empty;

    /// <summary>Empty = the anonymous baseline, mirroring the analyzer.</summary>
    public string CompareKey { get; set; } = string.Empty;

    public int PageId { get; set; }

    public override ExperienceWhyModel? GetModel()
    {
        try
        {
            var catalog = new DwAccountCatalog();
            var account = catalog.Resolve(AccountKey);
            var other = catalog.Resolve(string.IsNullOrEmpty(CompareKey) ? $"role:{SecurityAccount.AnonymousRole}" : CompareKey);
            var page = PageId > 0 ? Dynamicweb.Content.Services.Pages.GetPage(PageId) : null;

            if (account is null || other is null || page is null)
                return new ExperienceWhyModel { Heading = "Why?", Html = SearchTables.Note("The page or account no longer exists.") };

            var source = new DwContentSecuritySource();
            var evaluator = new EffectiveAccessEvaluator(source);
            var pagesById = source.GetPages(page.AreaId).ToDictionary(p => p.Id);
            var ownerName = AccessOverviewQuery.OwnerNameResolver();

            (string Name, string Key, bool Sees, string Why) Side(SecurityAccount who)
            {
                var access = evaluator.EvaluatePage(who, PageId, pagesById);
                return (who.DisplayName, who.Key, access.GrantsRead,
                    AccessOverviewQuery.Explain(who, access, evaluator, pagesById, ownerName));
            }

            var pathLine = pagesById.TryGetValue(PageId, out var node)
                ? ExperienceAnalyzerQuery.Path(node, pagesById)
                : page.GetDisplayName();
            var areaName = source.GetAreas().FirstOrDefault(a => a.Id == page.AreaId)?.Name;
            if (!string.IsNullOrEmpty(areaName))
                pathLine = $"{areaName}: {pathLine}";

            return new ExperienceWhyModel
            {
                Heading = page.GetDisplayName(),
                Html = ExperienceReport.WhyHtml(pathLine, Side(account), Side(other), PageId)
            };
        }
        catch (Exception ex)
        {
            return new ExperienceWhyModel { Heading = "Why?", Html = SearchTables.Note(ex.Message) };
        }
    }
}
