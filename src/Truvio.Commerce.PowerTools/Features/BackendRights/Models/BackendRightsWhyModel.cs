using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Models;

/// <summary>The "Why?" slide-over for one area, section or node.</summary>
public sealed class BackendRightsWhyModel : DataViewModelBase
{
    public string Heading { get; set; } = string.Empty;

    public string Html { get; set; } = string.Empty;
}
