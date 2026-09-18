using Dynamicweb.CoreUI.Data;
using Dynamicweb.CoreUI.Data.DynamicFields;

namespace Truvio.Commerce.PowerTools.Features.QueryTester.Models;

/// <summary>The "Why 'X'?" panel for one document (slide-over).</summary>
public sealed class QueryWhyModel : DataViewModelBase
{
    public string Heading { get; set; } = string.Empty;

    public string Html { get; set; } = string.Empty;
}
