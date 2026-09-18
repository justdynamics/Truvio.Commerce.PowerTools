using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.PimQuality.Core;

/// <summary>A rule over one <see cref="PimSnapshot"/>. Rule ids stay stable: PIM-W1..</summary>
public interface IPimRule
{
    IEnumerable<Finding> Evaluate(PimSnapshot snapshot);
}
