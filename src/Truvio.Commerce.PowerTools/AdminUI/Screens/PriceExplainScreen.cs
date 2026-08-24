using System.Net;
using Dynamicweb.CoreUI;
using System.Text;
using Dynamicweb.CoreUI.Actions;
using Dynamicweb.CoreUI.Actions.Implementations;
using Dynamicweb.CoreUI.Displays.Information;
using Dynamicweb.CoreUI.Displays.Widgets;
using Dynamicweb.CoreUI.Layout;
using Dynamicweb.CoreUI.Screens;
using Truvio.Commerce.PowerTools.AdminUI.Models;
using Truvio.Commerce.PowerTools.AdminUI.Queries;
using Truvio.Commerce.PowerTools.Core.Commerce.Dw;
using Icon = Dynamicweb.CoreUI.Icons.Icon;
using Truvio.Commerce.PowerTools.Core.Settings.Dw;

namespace Truvio.Commerce.PowerTools.AdminUI.Screens;

/// <summary>
/// Step 3 of the Price Explainer: the report, as an overview screen — an info bar with the
/// verdicts, then one full-width section per topic (context, visibility, price matrix,
/// discounts). The list grid is deliberately not used: it splits the width evenly between
/// columns and clips long text, and explanations are long text. Context switches
/// (currency, shop, quantity, date) live as dedicated toolbar buttons next to the Actions
/// menu — see <see cref="PriceExplainToolbarInjector"/> — and re-navigate with the adjusted
/// query, so every state is a shareable URL.
/// </summary>
public sealed class PriceExplainScreen : OverviewScreenBase<PriceExplainModel>
{
    private PriceExplainQuery Q => Query as PriceExplainQuery ?? new PriceExplainQuery();

    protected override string GetScreenName() =>
        Model is null || string.IsNullOrEmpty(Model.Title) ? "Price Explainer" : $"Price Explainer: {Model.Title}";

    protected override void BuildOverviewScreen()
    {
        var model = Model;
        if (model is null)
            return;

        if (!string.IsNullOrEmpty(model.Error))
        {
            AddComponent(new Alert { Value = model.Error, Icon = Icon.ExclamationTriangle }, "Explanation failed", Group.GroupWidth.Col_12);
            return;
        }

        SetInfobar(new InfoBar
        {
            Icon = Icon.Tag,
            Information = new Dictionary<string, CardInfo.InfoValue>
            {
                ["Account"] = new(model.AccountName),
                ["Product"] = new(model.ProductName),
                ["Sees it"] = new(new Badge { Value = model.Visible ? "Yes" : "No", BadgeType = model.Visible ? BadgeType.Success : BadgeType.Danger }),
                ["Pays"] = new(model.FinalPrice)
            }
        });

        foreach (var section in model.Rows.Where(r => r.IsHeader).Select(r => r.Section))
        {
            var rows = model.Rows.Where(r => !r.IsHeader && r.Section == section).ToList();
            AddComponent(new HtmlBlock { Value = RenderTable(rows) }, section, Group.GroupWidth.Col_12);
        }
    }

    private static string RenderTable(IReadOnlyList<ExplainRowModel> rows)
    {
        var sb = new StringBuilder();
        // The group renders its heading with ~1.5rem inset but the component flush to the edge.
        sb.Append("<div style=\"padding:0 1.5rem .75rem 1.5rem\">");
        sb.Append("<table style=\"width:100%;border-collapse:collapse;font-size:inherit\">");
        foreach (var r in rows)
        {
            var badge = string.IsNullOrEmpty(r.Verdict)
                ? string.Empty
                : $"<span style=\"display:inline-block;padding:1px 8px;border-radius:10px;font-size:0.85em;white-space:nowrap;{BadgeStyle(r.VerdictKind)}\">{E(r.Verdict)}</span>";
            var detail = (r.Value, r.Why) switch
            {
                ("", "") => string.Empty,
                (_, "") => $"<strong>{E(r.Value)}</strong>",
                ("", _) => E(r.Why),
                _ => $"<strong>{E(r.Value)}</strong> <span style=\"opacity:.8\">&mdash; {E(r.Why)}</span>"
            };
            sb.Append("<tr style=\"border-bottom:1px solid rgba(0,0,0,.06)\">")
              .Append($"<td style=\"padding:6px 8px 6px 0;vertical-align:top;white-space:nowrap;width:1%\">{E(r.Item)}</td>")
              .Append($"<td style=\"padding:6px 8px;vertical-align:top;white-space:nowrap;width:1%\">{badge}</td>")
              .Append($"<td style=\"padding:6px 0 6px 8px;vertical-align:top;white-space:normal\">{detail}</td>")
              .Append("</tr>");
        }
        sb.Append("</table></div>");
        return sb.ToString();
    }

    private static string BadgeStyle(string kind) => kind switch
    {
        "win" or "ok" => "background:#d4edda;color:#155724",
        "match" => "background:#d1ecf1;color:#0c5460",
        "reject" or "hidden" => "background:#f8d7da;color:#721c24",
        "warn" => "background:#fff3cd;color:#856404",
        "info" => "background:#e2e3f3;color:#383d6b",
        _ => "background:#e9ecef;color:#495057"
    };

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

    protected override IEnumerable<ActionGroup>? GetScreenActions()
    {
        var q = Q;
        var groups = new List<ActionGroup>
        {
            new()
            {
                Nodes =
                [
                    new ActionNode
                    {
                        Name = "Select another product",
                        Icon = Icon.Tag,
                        NodeAction = NavigateScreenAction.To<ProductPickScreen>()
                            .With(new ProductPickQuery { AccountKey = q.AccountKey })
                    },
                    new ActionNode
                    {
                        Name = "Select another account",
                        Icon = Icon.UserCircle,
                        NodeAction = NavigateScreenAction.To<ExplainerAccountListScreen>()
                            .With(new ExplainerAccountListQuery())
                    }
                ]
            }
        };

        return groups;
    }

    private static ActionNode DateAction(PriceExplainQuery q, string name, string date) => new()
    {
        Name = name,
        Icon = Icon.CalendarAlt,
        NodeAction = Navigate(q, x => x.Date = date)
    };

    private static NavigateScreenAction Navigate(PriceExplainQuery q, Action<PriceExplainQuery> change)
    {
        var next = new PriceExplainQuery
        {
            AccountKey = q.AccountKey,
            ProductId = q.ProductId,
            VariantId = q.VariantId,
            LanguageId = q.LanguageId,
            CurrencyCode = q.CurrencyCode,
            CountryCode = q.CountryCode,
            ShopId = q.ShopId,
            Quantity = q.Quantity,
            Date = q.Date
        };
        change(next);
        return NavigateScreenAction.To<PriceExplainScreen>().With(next);
    }

    private static IReadOnlyList<T> SafeList<T>(Func<IReadOnlyList<T>> source)
    {
        try
        {
            return source();
        }
        catch
        {
            return [];
        }
    }
}


/// <summary>
/// Puts the report's context switches in the top bar next to the Actions menu — one button
/// per dimension, labelled with the value in effect (like the Visual Editor's device
/// selector, one hop instead of scanning a long mixed menu). An injector because
/// <c>OverviewScreenBase</c> keeps its <c>ScreenLayout</c> private: <c>AddInManager</c>
/// discovers <c>ScreenInjector&lt;T&gt;</c> subclasses and <c>OnAfter</c> hands over the
/// built layout, whose <c>AddAction</c> is the bar (<c>ContextActionGroups</c> is the
/// Actions dropdown).
/// </summary>
public sealed class PriceExplainToolbarInjector : ScreenInjector<PriceExplainScreen>
{
    public override void OnAfter(PriceExplainScreen screen, UiComponentBase content)
    {
        if (content is not ScreenLayout layout)
            return;

        if (screen.Query is not PriceExplainQuery q || string.IsNullOrEmpty(q.ProductId))
            return;

        var currencies = SafeList(DwCommerceExplainer.Currencies);
        if (currencies.Count > 1)
        {
            ToolbarSwitch.Add(layout, string.IsNullOrEmpty(q.CurrencyCode) ? "Currency" : q.CurrencyCode, Icon.Coins,
                currencies.Select(c => ToolbarSwitch.Option($"{c.Code}{(c.Name != c.Code ? $" - {c.Name}" : "")}",
                    active: c.Code == q.CurrencyCode, Navigate(q, x => x.CurrencyCode = c.Code))));
        }

        var shops = SafeList(DwCommerceExplainer.Shops);
        if (shops.Count > 1)
        {
            var current = shops.FirstOrDefault(shop => shop.Id == q.ShopId);
            ToolbarSwitch.Add(layout, string.IsNullOrEmpty(current.Name) ? "Shop" : current.Name, Icon.ShoppingCart,
                shops.Select(shop => ToolbarSwitch.Option(shop.Name, active: shop.Id == q.ShopId, Navigate(q, x => x.ShopId = shop.Id))));
        }

        var settings = DwPowerToolsSettings.Current;

        ToolbarSwitch.Add(layout, $"Qty {q.Quantity:0.##}", Icon.Cube,
            settings.Quantities().Select(qty => ToolbarSwitch.Option($"{qty:0.##}", active: Math.Abs(qty - q.Quantity) < 0.0001,
                Navigate(q, x => x.Quantity = qty))));

        var today = DateTime.Today;
        var dateOptions = new List<ActionNode> { ToolbarSwitch.Option("Now", active: string.IsNullOrEmpty(q.Date), Navigate(q, x => x.Date = string.Empty)) };
        dateOptions.AddRange(settings.DateOffsets().Select(days =>
        {
            var date = today.AddDays(days).ToString("yyyy-MM-dd");
            return ToolbarSwitch.Option($"+{days} days ({date})", active: q.Date == date, Navigate(q, x => x.Date = date));
        }));
        ToolbarSwitch.Add(layout, string.IsNullOrEmpty(q.Date) ? "Now" : q.Date, Icon.CalendarAlt, dateOptions);
    }

    private static NavigateScreenAction Navigate(PriceExplainQuery q, Action<PriceExplainQuery> change)
    {
        var next = new PriceExplainQuery
        {
            AccountKey = q.AccountKey,
            ProductId = q.ProductId,
            VariantId = q.VariantId,
            LanguageId = q.LanguageId,
            CurrencyCode = q.CurrencyCode,
            CountryCode = q.CountryCode,
            ShopId = q.ShopId,
            Quantity = q.Quantity,
            Date = q.Date
        };
        change(next);
        return NavigateScreenAction.To<PriceExplainScreen>().With(next);
    }

    private static IReadOnlyList<T> SafeList<T>(Func<IReadOnlyList<T>> source)
    {
        try
        {
            return source();
        }
        catch
        {
            return [];
        }
    }
}
