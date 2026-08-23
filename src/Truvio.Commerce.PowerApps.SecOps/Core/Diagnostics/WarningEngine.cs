using Truvio.Commerce.PowerApps.SecOps.Core.Diagnostics.Rules;
using Truvio.Commerce.PowerApps.SecOps.Core.Permissions;

namespace Truvio.Commerce.PowerApps.SecOps.Core.Diagnostics;

/// <summary>Runs every registered warning rule over one source snapshot.</summary>
public sealed class WarningEngine
{
    private readonly IReadOnlyList<IWarningRule> _rules;

    public WarningEngine() : this(
    [
        new BareGroupGrantRule(),
        new GatedLoginPageRule(),
        new LegacyPermissionColumnRule(),
        new OrphanedGrantRule()
    ])
    {
    }

    public WarningEngine(IReadOnlyList<IWarningRule> rules) => _rules = rules;

    public IReadOnlyList<Finding> Run(IContentSecuritySource source)
    {
        var context = new WarningContext(source);
        return _rules
            .SelectMany(rule => rule.Evaluate(context))
            .OrderBy(f => f.Severity switch
            {
                FindingSeverity.Critical => 0,
                FindingSeverity.Warning => 1,
                _ => 2
            })
            .ThenBy(f => f.RuleId, StringComparer.Ordinal)
            .ThenBy(f => f.EntityDisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
