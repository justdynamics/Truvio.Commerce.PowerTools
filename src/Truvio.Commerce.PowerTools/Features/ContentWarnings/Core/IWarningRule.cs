using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.ContentWarnings.Core;

/// <summary>
/// A single misconfiguration detector. Rules are pure over the source snapshot so they can
/// run against an in-memory fake in tests.
/// </summary>
public interface IWarningRule
{
    string RuleId { get; }

    IEnumerable<Finding> Evaluate(WarningContext context);
}
