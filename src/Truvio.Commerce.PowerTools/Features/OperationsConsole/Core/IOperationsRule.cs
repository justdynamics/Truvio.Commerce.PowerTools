using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

/// <summary>A rule over one <see cref="OperationsSnapshot"/>. Rule ids stay stable: OPS-W1..</summary>
public interface IOperationsRule
{
    IEnumerable<Finding> Evaluate(OperationsSnapshot snapshot);
}
