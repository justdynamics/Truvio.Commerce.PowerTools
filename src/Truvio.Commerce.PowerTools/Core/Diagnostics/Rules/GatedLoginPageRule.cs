using Truvio.Commerce.PowerTools.Core.Permissions;
using Truvio.Commerce.PowerTools.Core.Principals;

namespace Truvio.Commerce.PowerTools.Core.Diagnostics.Rules;

/// <summary>
/// When a page denies an anonymous visitor, DW 302s them to the first page in the website
/// carrying the UserAuthentication app. If that login page is itself denied to Anonymous
/// (or inactive), the redirect flow dead-ends and gated sections become unreachable for
/// signed-out visitors instead of prompting a sign-in.
/// </summary>
public sealed class GatedLoginPageRule : IWarningRule
{
    public string RuleId => "SECOPS-W2";

    private const string UserAuthenticationModule = "UserAuthentication";

    public IEnumerable<Finding> Evaluate(WarningContext context)
    {
        var evaluator = new EffectiveAccessEvaluator(context.Source);
        var anonymous = new SecurityAccount
        {
            Kind = SecurityAccountKind.Role,
            Id = SecurityAccount.AnonymousRole,
            DisplayName = "Anonymous users (frontend)",
            OwnerIds = [SecurityAccount.AnonymousRole]
        };

        var pagesById = context.PagesById.Value;
        var loginPages = context.ParagraphsById.Value.Values
            .Where(p => string.Equals(p.ModuleSystemName, UserAuthenticationModule, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.PageId)
            .Distinct();

        foreach (var pageId in loginPages)
        {
            if (!pagesById.TryGetValue(pageId, out var page))
                continue;

            var entity = context.DescribeEntity(ContentEntityNames.Page, pageId.ToString());

            if (!page.Active)
            {
                yield return new Finding(
                    RuleId,
                    FindingSeverity.Critical,
                    ContentEntityNames.Page,
                    pageId.ToString(),
                    entity,
                    "Sign-in page is inactive",
                    $"{entity} carries the UserAuthentication app but is not active. Anonymous visitors "
                    + "denied on gated pages are redirected here and will dead-end.");
                continue;
            }

            var access = evaluator.EvaluatePage(anonymous, pageId, pagesById);
            if (!access.GrantsRead)
            {
                yield return new Finding(
                    RuleId,
                    FindingSeverity.Critical,
                    ContentEntityNames.Page,
                    pageId.ToString(),
                    entity,
                    "Sign-in page is gated against anonymous visitors",
                    $"{entity} carries the UserAuthentication app but resolves to "
                    + $"'{access.LevelName}' for Anonymous. Signed-out visitors redirected here "
                    + "cannot reach the sign-in form; remove the Anonymous deny on this page.");
            }
        }
    }
}
