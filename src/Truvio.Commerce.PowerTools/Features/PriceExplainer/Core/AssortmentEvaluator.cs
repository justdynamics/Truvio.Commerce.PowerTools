namespace Truvio.Commerce.PowerTools.Features.PriceExplainer.Core;

/// <summary>
/// Mirrors DW's AssortmentService.InternalHasAccessToProduct + GetAssortmentIdsByUser:
/// collect the assortments containing the product; none → accessible; otherwise the account
/// must hold one of them (anonymous: AllowAnonymousUsers; user: user- or group permission on
/// an ACTIVE assortment). Note DW's public HasAccessToProduct short-circuits to true outside
/// the frontend, which is why the rule is mirrored here instead of called.
/// </summary>
public static class AssortmentEvaluator
{
    public static VisibilityVerdict Evaluate(
        IReadOnlyList<AssortmentSpec> assortments,
        AssortmentAccount account,
        bool useAssortmentsSetting)
    {
        var warnings = new List<string>();
        var rows = new List<AssortmentRowVerdict>();

        var anyActive = assortments.Any(a => a.Active);
        if (!useAssortmentsSetting || !anyActive)
        {
            var reason = !useAssortmentsSetting
                ? "Assortments are disabled in Settings > Ecommerce > Assortments"
                : "No active assortment exists";
            return new VisibilityVerdict
            {
                Outcome = VisibilityOutcome.AssortmentsInactive,
                Summary = $"Visible to everyone — {reason}",
                Rows = assortments.Select(a => new AssortmentRowVerdict
                {
                    Assortment = a,
                    AccountHasIt = false,
                    Explanation = a.Active ? "Ignored (assortments disabled)" : "Inactive"
                }).ToList(),
                Warnings = warnings
            };
        }

        var held = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in assortments)
        {
            var (has, why) = Holds(a, account);
            if (has)
                held.Add(a.Id);
            if (a.RebuildRequired && a.ContainsProduct)
                warnings.Add($"Assortment '{a.Name}' is flagged for rebuild — its product list may be stale");
            rows.Add(new AssortmentRowVerdict { Assortment = a, AccountHasIt = has, Explanation = why });
        }

        // DW collects the containing assortments across ALL assortments (active or not).
        var containing = assortments.Where(a => a.ContainsProduct).ToList();
        if (containing.Count == 0)
        {
            warnings.Add("The product is in no assortment: direct links open it, but assortment-filtered product lists (index queries with the assortment criterion) do not show it");
            return new VisibilityVerdict
            {
                Outcome = VisibilityOutcome.InNoAssortment,
                Summary = "Visible on direct access — the product belongs to no assortment",
                Rows = rows,
                Warnings = warnings
            };
        }

        var granting = containing.Where(a => held.Contains(a.Id)).ToList();
        var finalRows = rows.Select(r => new AssortmentRowVerdict
        {
            Assortment = r.Assortment,
            AccountHasIt = r.AccountHasIt,
            Explanation = r.Explanation,
            Grants = granting.Any(g => g.Id == r.Assortment.Id)
        }).ToList();

        if (granting.Count > 0)
        {
            return new VisibilityVerdict
            {
                Outcome = VisibilityOutcome.Visible,
                Summary = $"Visible — via assortment {string.Join(", ", granting.Select(g => $"'{g.Name}'"))}",
                Rows = finalRows,
                Warnings = warnings
            };
        }

        var inactiveOnly = containing.All(a => !a.Active);
        if (inactiveOnly)
            warnings.Add("The product is only in INACTIVE assortments — DW still treats it as assortment-restricted, so nobody sees it until one of them is activated");

        return new VisibilityVerdict
        {
            Outcome = VisibilityOutcome.Hidden,
            Summary = account.IsAnonymous
                ? $"Hidden — the product is in {containing.Count} assortment(s), none of which allows anonymous users"
                : $"Hidden — the product is in {containing.Count} assortment(s), none of which the user or their groups are permitted to",
            Rows = finalRows,
            Warnings = warnings
        };
    }

    private static (bool Has, string Why) Holds(AssortmentSpec a, AssortmentAccount account)
    {
        if (!a.Active)
            return (false, "Inactive — never granted");

        if (account.IsAnonymous)
        {
            return a.AllowAnonymousUsers
                ? (true, "Allows anonymous users")
                : (false, "Does not allow anonymous users");
        }

        if (a.PermittedUserIds.Contains(account.UserId!.Value))
            return (true, "User is permitted directly");

        var viaGroups = a.PermittedGroupIds.Intersect(account.GroupIds).ToList();
        if (viaGroups.Count > 0)
            return (true, $"Permitted via group {string.Join(", ", viaGroups)}");

        return (false, a.PermittedUserIds.Count + a.PermittedGroupIds.Count == 0
            ? "No user or group is permitted"
            : "User is not among the permitted users/groups");
    }
}
