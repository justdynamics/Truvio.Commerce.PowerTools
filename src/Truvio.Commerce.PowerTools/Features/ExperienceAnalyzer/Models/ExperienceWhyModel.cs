using Dynamicweb.CoreUI.Data;

namespace Truvio.Commerce.PowerTools.Features.ExperienceAnalyzer.Models;

/// <summary>The "Why?" slide-over for one page: both sides' full explanations.</summary>
public sealed class ExperienceWhyModel : DataViewModelBase
{
    public string Heading { get; set; } = string.Empty;

    public string Html { get; set; } = string.Empty;
}
