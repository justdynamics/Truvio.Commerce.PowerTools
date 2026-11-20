# Price Explainer

Price Explainer is read-only. The context is a user or anonymous visitor, product/variant,
language, currency, country, shop, quantity and date. Toolbar changes preserve these as URL state.
Only the default unit and stock location 0 are explained.

## Package lines

| Line | DW minimum | Target | Package version for this release | Capabilities |
|---|---|---|---|---|
| Base | 10.8.4 | net8.0 | 0.13.10-beta | DW amounts at quantity breaks, standard applicable-row overview, rejection summaries, scoped campaign evidence. Found discount percentages and allow flags are unavailable. |
| Discount | 10.25.5 | net8.0 | 0.13.11-beta | Adds found discount percentage and amount, allow flags, priority/net selection and four-value steps. |
| Groups | 10.30.6 | net10.0 | 0.13.12-beta | Adds customer price/discount groups, product discount groups and the newer fallback rule. |

All use one package ID and source. The patch number reserves the last digit for the line:
Base is `x.y.(10n+0)`, Discount is `x.y.(10n+1)`, Groups is `x.y.(10n+2)`, where `n` is the
patch release counter. The Base version in the project must have a patch divisible by ten.
The next patch release uses `0.13.20`, `0.13.21` and `0.13.22`; the same mapping preserves
prerelease suffixes and works for stable releases. Higher floors sort above lower ones
within a release, and later releases sort above all three earlier lines.

The release tag always names the Base version, currently `v0.13.10-beta`. Four-part package
versions are not supported: the DW add-in folder parser can absorb a numeric component into
the package name, causing DLL discovery to fail even though installation reports success.
The build validates the intended identity before compilation, and the packed-manifest guard
requires the folder parser to return exactly the package ID and version from the nuspec.
It also checks the packed version equals the requested line version. CI runs positive mapping
and negative packed-version checks using `.github/scripts/test-package-version.ps1`.

Lower-floor packages can run on newer hosts, but they
cannot read APIs absent from their build floor. The screen identifies that limitation.

## Verified winner rules

The runtime `Dynamicweb.Ecommerce` assembly version selects the rule. Release boundaries
between inspected packages are deliberately not guessed. Unlisted host versions have DW
amounts and applicable rows, but no winner claim.

| Inspected release | Rule |
|---|---|
| 10.8.4 | Lowest comparable amount, including zero. |
| 10.17.5 | Lowest net; own positive percentage, otherwise the highest percentage on matching zero-amount rows. No priority tier. This algorithm is unit-tested; no package line targets this floor. |
| 10.25.5, 10.28.1, 10.28.17, 10.29.8 | Same percentage rule; if any positive price candidate has positive priority, only the highest priority competes. |
| 10.30.6, 10.30.8 | Product discount-group candidates, valid configurations and group filters; fallback percentage over all matching rows; higher of own and fallback percentage; highest positive priority, then lowest net. |

Amounts are normalized for stored VAT before comparison. Discount-capable rules exclude
nonpositive amounts from price competition; zero-amount rows can supply the fallback discount.
A row disallowing order-line discounts has effective percentage zero. No positive candidate
leaves the product default price with the applicable fallback percentage. Ties keep DW's
matching-set order and are flagged.

`FindPrices` decides matches. The mirror explains why other candidates fail. The Groups line
reads only rows whose product or product discount group matches a parameterized SQL query;
it does not call the all-prices API. The other lines use the product-scoped service lookup.
Rows matching this account at a higher quantity remain visible as step evidence. Rejected
counts use one primary reason per row; expanded details retain all failed checks.

Any installed custom price provider disables winner, outranked and discount-line labels.
Rows then say Applies, without guessing a price-versus-discount convention. Provider prices
still appear at each discovered quantity break. The explainer cannot discover extra break
quantities known only to a custom provider or reproduce template-specific pricing logic.

## Amounts and limits

Found price and returned percentage come from the same `PriceManager.FindPrice` selection.
The returned percentage's saving is separate from campaigns. The standard product-page
quantity-1 result is compared with the chosen quantity and a difference is reported.
The Base line cannot read percentages or allow flags, so final price is unavailable on
newer hosts; at the verified 10.8.4 host the percentage is known to be absent.

Campaign validity is supplied by `DiscountInfoCollection` (enumeration only, never its obsolete
`TotalPrice`). Classic percentage amounts are evaluated on the chosen quantity's unit price.
The internal `DiscountProvider` class is not callable from an add-in, despite its public
`CalculateDiscountPrice` method. A small percentage calculation mirrors its inspected code,
using public `PriceInfo`/`PriceCalculated` operations, the currency decimal count,
`RoundDiscountPerQuantity` and per-currency maximum limits. Fixed amounts use DW's calculation
because they do not depend on quantity. Extender and amount-from-field discounts keep DW's
own quantity-1 amount and are marked. Accumulate/highest/lowest selection is then applied.
All discounts use the same found price, rather than compounding on one another.

This is not a whole-cart simulation. Quantity-required campaigns, vouchers, order totals,
order fields and usage restrictions are not included in the product-context total and are
identified in the evidence. Shipping/payment choices, real cart state and discount extenders
need host verification. No cart or order is created or saved. When AdjustmentsFeature is
active the campaign and final figures say Not explained. If discount enumeration fails,
final price is unavailable rather than silently treated as no discounts.

The row cap applies to the applicable-price list, raw quantity list, discount list, and each
expanded rejection sample. All rejected counts and applied amount calculations precede
rendering caps. Consecutive price steps with identical figures and source render once, at
their first quantity. Every break is still calculated. Large numbers of distinct
breaks therefore require correspondingly many provider calls. DW may populate its own caches
when its service methods are first called; a bounded explainer query does not promise a cold
DW cache is cheap.

## Verification

Run `.github/scripts/build-line.ps1` for Base, Discount and Groups. It rebuilds before testing
and packing, then verifies the packed add-in identity/version, assembly references, framework
and dependency floor.
Pure tests cover rule differences, priority, fallback percentages, allow flags, ties, VAT,
authoritative matches, group targets, unknown/custom mode, 322,000 synthetic candidates,
campaign selection, rendering caps, HTML encoding and context preservation.

See the [host results, remaining gaps and repeatable recipe](price-explainer-host-verification.md).
Host/cart and app-list checks are separate from local tests and package verification.
