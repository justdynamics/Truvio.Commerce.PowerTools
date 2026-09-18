namespace Truvio.Commerce.PowerTools.Features.QueryTester.Core;

/// <summary>How one document behaves against one clause — the "why doesn't PROD27 show up" row.</summary>
public sealed record ExpectationCheck(
    string Path,
    string Label,
    string Field,
    string DocumentValue,
    bool Passes,
    string Note);
