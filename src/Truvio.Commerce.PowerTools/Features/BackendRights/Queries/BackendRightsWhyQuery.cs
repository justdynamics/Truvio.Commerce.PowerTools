using System.Globalization;
using Dynamicweb.CoreUI.Data;
using Truvio.Commerce.PowerTools.Features.BackendRights.Core;
using Truvio.Commerce.PowerTools.Features.BackendRights.Dw;
using Truvio.Commerce.PowerTools.Features.BackendRights.Models;
using Truvio.Commerce.PowerTools.Shared.AdminUI;
using Truvio.Commerce.PowerTools.Shared.Principals;

namespace Truvio.Commerce.PowerTools.Features.BackendRights.Queries;

/// <summary>The "Why?" panel for one row of the report.</summary>
public sealed class BackendRightsWhyQuery : DataQueryModelBase<BackendRightsWhyModel>
{
    public string AccountKey { get; set; } = string.Empty;

    /// <summary>The <see cref="RightsNodeSpec.Id"/> to explain.</summary>
    public string NodeId { get; set; } = string.Empty;

    public override BackendRightsWhyModel? GetModel()
    {
        var parsed = SecurityAccount.ParseKey(AccountKey);
        if (parsed is null || parsed.Value.Kind != SecurityAccountKind.User
            || !int.TryParse(parsed.Value.Id, out var userId) || string.IsNullOrEmpty(NodeId))
        {
            return new BackendRightsWhyModel { Heading = "Why?", Html = SearchTables.Note("Nothing selected.") };
        }

        try
        {
            var snapshot = new DwRightsSource().Build(userId);
            var verdict = RightsEvaluator.Evaluate(snapshot)
                .FirstOrDefault(v => string.Equals(v.Node.Id, NodeId, StringComparison.OrdinalIgnoreCase));

            if (verdict is null)
                return new BackendRightsWhyModel { Heading = "Why?", Html = SearchTables.Note("That area is no longer installed.") };

            return new BackendRightsWhyModel
            {
                Heading = RightsExplanation.Headline(verdict),
                Html = BackendRightsReport.WhyHtml(snapshot, verdict)
            };
        }
        catch (Exception ex)
        {
            return new BackendRightsWhyModel { Heading = "Why?", Html = SearchTables.Note(ex.Message) };
        }
    }
}
