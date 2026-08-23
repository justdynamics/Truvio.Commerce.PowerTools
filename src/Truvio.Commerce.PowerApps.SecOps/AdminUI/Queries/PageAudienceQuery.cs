using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerApps.SecOps.AdminUI.Models;
using Truvio.Commerce.PowerApps.SecOps.Core.Permissions;
using Truvio.Commerce.PowerApps.SecOps.Core.Permissions.Dw;
using Truvio.Commerce.PowerApps.SecOps.Core.Principals.Dw;

namespace Truvio.Commerce.PowerApps.SecOps.AdminUI.Queries;

/// <summary>
/// The personalisation drilldown for one page and one account: the page verdict first, then
/// what each grid row and paragraph resolves to.
/// </summary>
public sealed class PageAudienceQuery : DataQueryModelBase<DataListViewModel<AudienceItemModel>>
{
    public string AccountKey { get; set; } = string.Empty;

    public int PageId { get; set; }

    public override DataListViewModel<AudienceItemModel>? GetModel()
    {
        var account = new DwAccountCatalog().Resolve(AccountKey);
        if (account is null || PageId <= 0)
            return new DataListViewModel<AudienceItemModel>();

        var source = new DwContentSecuritySource();
        var evaluator = new EffectiveAccessEvaluator(source);

        var page = Dynamicweb.Content.Services.Pages.GetPage(PageId);
        if (page is null)
            return new DataListViewModel<AudienceItemModel>();

        var pagesById = source.GetPages(page.AreaId).ToDictionary(p => p.Id);
        var pageAccess = evaluator.EvaluatePage(account, PageId, pagesById);

        var items = new List<AudienceItemModel>
        {
            new()
            {
                ItemType = "Page",
                Name = page.GetDisplayName(),
                Visible = pageAccess.GrantsRead ? "Yes" : "No (page is denied; nothing below renders)",
                Level = pageAccess.LevelName,
                Reason = AccessOverviewQuery.Describe(pageAccess, pagesById),
                VisibleState = pageAccess.GrantsRead,
                LevelValue = pageAccess.Level,
                OriginKind = pageAccess.Origin.ToString()
            }
        };

        foreach (var row in source.GetGridRows(PageId))
        {
            var access = evaluator.EvaluateGridRow(account, row.Id, pageAccess);
            items.Add(new AudienceItemModel
            {
                ItemType = "Grid row",
                Name = row.Name,
                Visible = Verdict(pageAccess, access),
                Level = access.LevelName,
                Reason = AccessOverviewQuery.Describe(access, pagesById),
                VisibleState = pageAccess.GrantsRead && access.GrantsRead,
                LevelValue = access.Level,
                OriginKind = access.Origin.ToString()
            });
        }

        foreach (var paragraph in source.GetParagraphs(PageId))
        {
            var access = evaluator.EvaluateParagraph(account, paragraph.Id, pageAccess);
            items.Add(new AudienceItemModel
            {
                ItemType = "Paragraph",
                Name = paragraph.Name,
                Visible = Verdict(pageAccess, access),
                Level = access.LevelName,
                Reason = AccessOverviewQuery.Describe(access, pagesById),
                VisibleState = pageAccess.GrantsRead && access.GrantsRead,
                LevelValue = access.Level,
                OriginKind = access.Origin.ToString()
            });
        }

        return new DataListViewModel<AudienceItemModel>
        {
            Data = items,
            TotalCount = items.Count
        };
    }

    private static string Verdict(Core.Permissions.EffectiveAccess pageAccess, Core.Permissions.EffectiveAccess access)
    {
        if (!pageAccess.GrantsRead)
            return "No (page is denied)";
        return access.GrantsRead ? "Yes" : "No (renders empty)";
    }
}
