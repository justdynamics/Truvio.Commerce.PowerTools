using System.Globalization;
using Shipped = Truvio.Commerce.PowerTools.Features.Settings.Core.PowerToolsSettingKeys.Defaults;
using Truvio.Commerce.PowerTools.Shared.Diagnostics;

namespace Truvio.Commerce.PowerTools.Features.Settings.Core;

/// <summary>What a screen shows after suppression, and how much it is not showing.</summary>
public sealed record FindingFilter(IReadOnlyList<Finding> Visible, int HiddenCount)
{
    /// <summary>The line every finding screen renders when something was hidden.</summary>
    public string HiddenNotice() =>
        $"{HiddenCount} finding{(HiddenCount == 1 ? "" : "s")} hidden by settings";
}
