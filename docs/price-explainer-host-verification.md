# Price Explainer isolated-host verification

Use disposable hosts only. This is a verification recipe, not evidence that the hosts passed.
Build the three packages with `.github/scripts/build-line.ps1`; install Base on 10.8.4,
Discount on 10.28.17, and Groups on 10.30.6 or 10.30.8. Check AddInManager TypeLoadErrors,
open PowerTools > Commerce > Price Explainer, and inspect GeneralLog errors after rendering.
Keep screenshots of the complete Price and Price steps sections, with the host version visible.

## Shared fixture

Create one currency with exchange rate 1, one VAT country at 25%, one shop, a frontend user,
and a product with default price 200, default unit PCS, and no assortment restriction. Store
prices excluding VAT. Use classic discounts. Give every row the same product, currency,
user, and unit. Leave validity open. The three amount rows are:

| Row label | Minimum quantity | Amount | Priority |
|---|---:|---:|---:|
| A | 1 | 100 | 0 |
| B | 10 | 80 | 0 |
| C | 50 | 60 | 0 |

Create a standard 5% order-line campaign scoped to that product and user, without quantity,
cart, voucher, usage, extender or order-field conditions. Set selection to accumulate.

On Base, expect found amounts 100/80/60, campaign savings 5/4/3 and final units 95/76/57
at quantities 1/10/50. Found discount percentage must say unavailable. Inclusive final units
are 118.75/95/71.25. On the newer lines add one zero-amount price row with discount 15%,
quantity 1, allowing order-line discounts. Expect found amounts 100/80/60, found savings
15/12/9, campaigns 5/4/3, and final units 80/64/48; inclusive finals are 100/80/60.
Line totals excluding VAT at 1/10/50 are 80/640/2400 on those newer lines.

Add the product to a real frontend cart at each quantity for the same account/context and
compare unit and line amounts in both VAT views. A differing quantity-1 result should be
called out when the explainer is at quantity 10 or 50. Preserve the cart evidence.

## Rule differences

On Discount and Groups, add row D: amount 110, priority 10, own percentage 5, quantity 1.
With the 15% zero-amount row still present, D must win over cheaper lower-priority rows.
On 10.28.17 its own 5% wins over fallback: found 110, found savings 5.50, campaign 5.50,
final 99 excluding VAT. On 10.30.6/8 the higher fallback 15% wins: found savings 16.50,
campaign 5.50, final 88 excluding VAT. Other positive rows must say Outranked.

Set D to disallow order-line discounts: found percentage/savings and campaign savings must
be zero, with final 110. Reset it. Add an equal-priority equal-net row and check the tie
warning; do not assume a particular row order independent of DW's FindPrices result.

## Groups and configuration

On Groups, move the zero-amount 15% row to a product discount group shared by the product
and target the user's customer discount group. It must still apply without a product id.
Switch user/group and verify rejection counts and expandable evidence. Add a customer price
group amount row with no percentage. Verify its target and source. Invalid group rows
(amount on a product/customer discount group, or percentage on a customer price group)
must be rejected if such rows are imported; do not weaken admin validation to create them.

## Campaign and boundary checks

- Add a fixed-amount campaign; compare accumulate/highest/lowest. Only selected campaigns
  may be marked Applied. Lower candidates must explain the selection setting.
- Set a per-currency maximum and toggle RoundDiscountPerQuantity; compare with the cart.
- Test amount-includes-VAT both ways and a converted currency, including rounding.
- Use an extender/amount-from-field discount if available. Its value must be flagged as
  computed by DW at quantity 1; do not claim cart parity for it without measuring.
- Enable AdjustmentsFeature on a newer-floor host. Campaign/final values must say Not
  explained. Restore classic discounts after the check.
- Install a test custom PriceProvider returning distinct values at 1/10/50. No matrix row
  may say Wins, Outranked, Shadowed or Discount line. Source must not claim a row winner.
- Install the Base package on a newer host. Verify no winner claim and no fabricated
  found discount/final price. Use an unlisted host release to check the same rule gate.
- Verify toolbar context switches and a copied URL reproduce the selected context.

## Scale and release evidence

On an isolated database, create 322,000 synthetic prices across customers and a large scoped
campaign set using the host's supported import path. Include many rejected rows for the
selected product, not merely unrelated products. Record cold/warm request duration, query
counts and memory, with a target of under two seconds for the warm explanation. Repeat at
quantity breaks. Verify counts cover all candidates while each list/sample obeys the row cap.
The unit test with 322,000 in-memory rows is not this database timing proof.

After explicit publication authorization, published test versions are needed to prove the
Available apps list picks the expected line on each host. Local packages and dependency
inspection cannot prove the marketplace choice. Keep this separate from the build checks.
