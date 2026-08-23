# Concepts

## What the Content Access Viewer answers

"Which content does this account see?" — asked by business users who gate pages, grid
rows, or paragraphs by group to personalise the experience (member areas, B2B portals,
per-role dashboard tiles on a shared page).

## The resolution model

DW10 resolves render-time content permissions from the permission entity store (rows per
page / grid row / paragraph, owned by a frontend role or a user group):

1. **Identities.** An account resolves through a set of identities: the built-in frontend
   roles plus, for users and groups, the group memberships (including ancestor groups).
   Per-user rows are ignored at render time.
2. **Contribution per identity.** Each identity contributes its explicit row on the
   entity if one exists, otherwise its role default — the frontend roles default to Read;
   groups have no default.
3. **Highest wins.** The highest contribution decides the level. Read or higher renders;
   None (or nothing) hides.
4. **Page inheritance.** A page without rows of its own inherits from the nearest ancestor
   page carrying rows; without any, only role defaults apply.
5. **Children follow the page.** A grid row or paragraph without rows of its own follows
   the page outcome. With rows, it resolves independently — but nothing renders on a page
   the account cannot see.
6. **Admin bypass.** Angel, built-in admin, and Administrator-type users bypass every
   check. The viewer badges them instead of evaluating.

## Why "deny + grant" is the working gate

Because of the role defaults plus highest-wins, granting a group Read does nothing on its
own — every visitor already resolves to Read through the broad roles. Gating requires the
pair on the same entity:

- `Authenticated users (frontend) -> None` (and `Anonymous users (frontend) -> None`)
- `<your group> -> Read` (or higher)

The Warnings screen (rule SECOPS-W1) finds every place where only half of that pair exists.

## What denial looks like

- **Page denied to an anonymous visitor** → 302 to the first page carrying the
  UserAuthentication app (rule SECOPS-W2 verifies that page is reachable).
- **Page denied to a signed-in visitor** → the page drops from navigation and direct URLs.
- **Grid row / paragraph denied** → renders empty, no redirect — "blank page", not
  "please sign in".
