using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Permissions.Dw;
using Truvio.Commerce.PowerTools.Core.Principals.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Queries;

/// <summary>
/// The personalisation drilldown for one page and one account: the page verdict first, then
/// what each grid row and paragraph resolves to.
/// </summary>
public sealed class PageAudienceQuery : DataQueryListBase<AudienceItemModel, AudienceItemModel, DataListViewModel<AudienceItemModel>>
{
    public string AccountKey { get; set; } = string.Empty;

    public int PageId { get; set; }

    protected override IEnumerable<AudienceItemModel>? GetListItems()
    {
        var account = new DwAccountCatalog().Resolve(AccountKey);
        if (account is null || PageId <= 0)
            return [];

        var source = new DwContentSecuritySource();
        var evaluator = new EffectiveAccessEvaluator(source);

        var page = Dynamicweb.Content.Services.Pages.GetPage(PageId);
        if (page is null)
            return [];

        var pagesById = source.GetPages(page.AreaId).ToDictionary(p => p.Id);
        var pageAccess = evaluator.EvaluatePage(account, PageId, pagesById);
        var ownerName = AccessOverviewQuery.OwnerNameResolver();

        var items = new List<AudienceItemModel>
        {
            new()
            {
                ItemType = "Page",
                Name = page.GetDisplayName(),
                Visible = pageAccess.GrantsRead ? "Yes" : "No (page is denied; nothing below renders)",
                Level = pageAccess.LevelName,
                Reason = AccessOverviewQuery.Explain(account, pageAccess, evaluator, pagesById, ownerName),
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
                Reason = access.Origin == AccessOrigin.ExplicitHere
                    ? AccessExplanation.Explain(account, access, evaluator.GetExplicitGridRowRows(row.Id), null, ownerName)
                    : AccessOverviewQuery.Explain(account, access, evaluator, pagesById, ownerName),
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
                Reason = access.Origin == AccessOrigin.ExplicitHere
                    ? AccessExplanation.Explain(account, access, evaluator.GetExplicitParagraphRows(paragraph.Id), null, ownerName)
                    : AccessOverviewQuery.Explain(account, access, evaluator, pagesById, ownerName),
                VisibleState = pageAccess.GrantsRead && access.GrantsRead,
                LevelValue = access.Level,
                OriginKind = access.Origin.ToString()
            });
        }

        return items;
    }

    private static string Verdict(Core.Permissions.EffectiveAccess pageAccess, Core.Permissions.EffectiveAccess access)
    {
        if (!pageAccess.GrantsRead)
            return "No (page is denied)";
        return access.GrantsRead ? "Yes" : "No (renders empty)";
    }

    protected override IEnumerable<AudienceItemModel> MapModels(IEnumerable<AudienceItemModel> items) => items;

    protected override DataListViewModel<AudienceItemModel> MakeListModel() => new();
}
