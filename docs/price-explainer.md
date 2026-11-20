# Price Explainer

Price Explainer is read-only. The context is a user or anonymous visitor, product/variant,
language, currency, country, shop, quantity and date. Toolbar changes preserve these as URL state.
Only the default unit and stock location 0 are explained.

## Package lines

| Line | DW minimum | Target | Package version for this release | Capabilities |
|---|---|---|---|---|
| Base | 10.8.4 | net8.0 | 0.13.0-beta | DW amounts at quantity breaks, standard applicable-row overview, rejection summaries, scoped campaign evidence. Found discount percentages and allow flags are unavailable. |
| Discount | 10.25.5 | net8.0 | 0.13.0.25-beta | Adds found discount percentage and amount, allow flags, priority/net selection and four-value steps. |
| Groups | 10.30.6 | net10.0 | 0.13.0.30-beta | Adds customer price/discount groups, product discount groups and the newer fallback rule. |

All use one package ID and source. The fourth version component orders higher floors above
lower ones within a release; the next patch release sorts above all three. The app-list
selection behavior still needs proof using published test versions on representative hosts.
A build/pack test is not that proof. Lower-floor packages can run on newer hosts, but they
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
rendering caps. Applicable price steps themselves are not truncated. Large numbers of distinct
breaks therefore require correspondingly many provider calls. DW may populate its own caches
when its service methods are first called; a bounded explainer query does not promise a cold
DW cache is cheap.

## Verification

Run `.github/scripts/build-line.ps1` for Base, Discount and Groups. It rebuilds before testing
and packing, then verifies each packed assembly reference, framework and dependency floor.
Pure tests cover rule differences, priority, fallback percentages, allow flags, ties, VAT,
authoritative matches, group targets, unknown/custom mode, 322,000 synthetic candidates,
campaign selection, rendering caps, HTML encoding and context preservation.

Before release, prove rendering and actual cart parity on isolated 10.8.4, 10.28.17 and 10.30.6+
hosts with the appropriate packages. Include quantities 1/10/50, both VAT views, percentage
and fixed campaigns, caps/rounding, group rows, custom providers and adjustments enabled.
The in-memory volume test does not prove database/provider latency. Measure a cold and warm
322,000-row fixture separately. No live host, publication or marketplace selection is
validated merely by this repository's unit tests.
