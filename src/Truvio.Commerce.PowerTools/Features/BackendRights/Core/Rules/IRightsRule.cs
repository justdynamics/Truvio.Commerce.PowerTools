using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Core.Rules;

/// <summary>
/// A rule over one <see cref="RightsSnapshot"/>. Deliberately its own contract rather than
/// <c>IWarningRule</c>: that one is built over <c>WarningContext</c>/<c>IContentSecuritySource</c>,
/// which knows about pages and paragraphs and nothing about the admin's own gates. The findings
/// carry the same <see cref="Finding"/> shape, so the Content Access Warnings screen lists them
/// beside the SECOPS-W rules.
/// </summary>
public interface IRightsRule
{
    string RuleId { get; }

    IEnumerable<Finding> Evaluate(RightsSnapshot snapshot);
}
