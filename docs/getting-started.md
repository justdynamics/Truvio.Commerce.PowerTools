# Getting started

## Install into a Dynamicweb 10 host

Pick one:

1. **App store** — DW10 admin → Apps → Available apps → search "SecOps" → install → restart.
2. **Package reference** — in `Dynamicweb.Host.Suite.csproj`:
   ```xml
   <PackageReference Include="Truvio.Commerce.PowerApps.SecOps" Version="0.1.0-beta" />
   ```
3. **Manual (development)** — `dotnet build`, copy
   `src/Truvio.Commerce.PowerApps.SecOps/bin/Debug/net8.0/Truvio.Commerce.PowerApps.SecOps.dll`
   into the host's `bin\` output folder, restart the host.

No configuration is required. All discovery is convention-based (DW's AddInManager picks
up the screens, tree nodes, and permission entities from the assembly).

## Where to find it

DW10 admin → **Content** area → **Tools** section in the tree (directly above the Recycle bin):

- **Security Viewer** — pick an account, inspect its content access, drill into a page.
- **Warnings** — run the misconfiguration rules across the install.

The Tools section is a virtual tree section contributed by the app — it exists only in the
admin UI while the package is installed; there is no content page behind it.

## Verifying a personalisation gate

1. Gate content with the deny+grant pair (broad roles → None, target group → Read).
2. Open Security Viewer → pick the target group → confirm "Sees it: Yes" with the group as winner.
3. Pick a different group (or Authenticated users) → confirm "Sees it: No".
4. Check Warnings — the gate should produce no SECOPS-W1 finding.

Never verify by browsing as an admin account: admins bypass every check (the account list
badges them accordingly).

## Managing who can use the viewer

The app registers the permission entity "Truvio PowerApps SecOps"
(key `truvio-secops-security-viewer`). It is open until explicitly managed; grant or deny
it per user/group through DW's standard permission management. Checks fail closed.
