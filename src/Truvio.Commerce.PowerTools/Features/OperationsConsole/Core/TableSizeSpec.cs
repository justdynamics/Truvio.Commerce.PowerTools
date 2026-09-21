namespace Truvio.Commerce.PowerTools.Features.OperationsConsole.Core;

/// <summary>Size of one database table.</summary>
/// <param name="Name">Table name.</param>
/// <param name="RowCount">Rows in the heap/clustered index.</param>
/// <param name="Bytes">Reserved bytes across all its indexes.</param>
public sealed record TableSizeSpec(string Name, long RowCount, long Bytes);
