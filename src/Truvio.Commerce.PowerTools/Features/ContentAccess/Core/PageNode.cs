namespace Truvio.Commerce.PowerTools.Features.ContentAccess.Core;

/// <summary>Minimal page-tree node the evaluator walks. Mirrors Dynamicweb.Content.Page.</summary>
public sealed record PageNode(int Id, int ParentPageId, int AreaId, string Name, int Sort, bool Active, bool Hidden);
