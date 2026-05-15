# Tamp.PostgresFlex

> Typed wrappers for Azure Database for PostgreSQL Flexible Server admin: lifecycle, firewall rules, server parameters, admin-password rotation.

| Package | Status |
|---|---|
| `Tamp.PostgresFlex` | 0.1.0 (initial) |

## Install

```bash
dotnet add package Tamp.PostgresFlex
```

Multi-targets net8 / net9 / net10. Requires `az` CLI on PATH.

## Quick start — stop / restart for maintenance

```csharp
using Tamp;
using Tamp.PostgresFlex;

class Build : TampBuild
{
    public static int Main(string[] args) => Execute<Build>(args);

    [FromPath("az")] readonly Tool Az = null!;

    Target StopForMaintenance => _ => _.Executes(() => PostgresFlex.Stop(Az, s => s
        .SetResourceGroup("rg-strata-dev")
        .SetName("strata-postgres-dev")));

    Target ResumeAfterMaintenance => _ => _.Executes(() => PostgresFlex.Start(Az, s => s
        .SetResourceGroup("rg-strata-dev")
        .SetName("strata-postgres-dev")));
}
```

## Verb surface

| Tamp method | az command | Notes |
|---|---|---|
| `PostgresFlex.Start(...)` | `az postgres flexible-server start` | |
| `PostgresFlex.Stop(...)` | `az postgres flexible-server stop` | Saves Azure compute spend during nights/weekends in dev. |
| `PostgresFlex.Restart(...)` | `az postgres flexible-server restart` | Required after some parameter updates. |
| `PostgresFlex.Show(...)` | `az postgres flexible-server show` | |
| `PostgresFlex.List(...)` | `az postgres flexible-server list` | Subscription / resource-group scope; no `--name`. |
| `PostgresFlex.FirewallRuleCreate(...)` | `az postgres flexible-server firewall-rule create` | `EndIp` defaults to `StartIp`. |
| `PostgresFlex.FirewallRuleDelete(...)` | `az postgres flexible-server firewall-rule delete` | Adds `--yes` by default for non-interactive build scripts. |
| `PostgresFlex.FirewallRuleList(...)` | `az postgres flexible-server firewall-rule list` | |
| `PostgresFlex.ParameterSet(...)` | `az postgres flexible-server parameter set` | Followed by `Restart` if the parameter requires it. |
| `PostgresFlex.ParameterShow(...)` | `az postgres flexible-server parameter show` | |
| `PostgresFlex.UpdateAdminPassword(...)` | `az postgres flexible-server update --admin-password` | Password is `Secret`-typed and redacted from logs. |

Every server-scope verb requires `ResourceGroup` + `Name` (validated at plan-build time, not runtime). `Subscription` is optional when the CLI default is correct.

## Auth

Inherits from the `az` CLI session. Use [`Tamp.AzureCli.V2`](https://github.com/tamp-build/tamp-azure-cli)'s `Login` verb earlier in the target graph if you need explicit credentials per-build.

## Releasing

Releases follow the [Tamp dogfood pattern](MAINTAINERS.md): bump `<Version>` in `Directory.Build.props`, tag `v<X.Y.Z>`, GitHub Actions runs `dotnet tamp Ci` then `dotnet tamp Push`.

## Settings authoring style

Examples above use the fluent `Set*`-chain shape. Every wrapper verb also accepts a `new XxxSettings { ... }` object-init form — both produce identical `CommandPlan`s. The fluent shape stays canonical in docs and the `tamp init` template; opt into object-init scaffolding via `tamp init --settings-style=init`.

See [Build Script Authoring → Two authoring styles](https://github.com/tamp-build/tamp/wiki/Build-Script-Authoring#two-authoring-styles-for-wrapper-calls-120) on the wiki for the side-by-side comparison.

## License

MIT. See [LICENSE](LICENSE).
