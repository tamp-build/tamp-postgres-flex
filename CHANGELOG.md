# Changelog

All notable changes to **Tamp.PostgresFlex** are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-05-13

### Added

- Initial release. Lifecycle (`Start`, `Stop`, `Restart`), introspection (`Show`, `List`),
  firewall-rule CRUD (`FirewallRuleCreate`, `FirewallRuleDelete`, `FirewallRuleList`),
  server-parameter operations (`ParameterSet`, `ParameterShow`), admin-password rotation
  (`UpdateAdminPassword`). Filed under TAM-177.

- `UpdateAdminPassword.Password` is `Secret`-typed and registered with the runner's
  redaction table.

- `List` uses subscription / resource-group scope rather than server scope (no `--name`
  required), since enumeration across servers is the typical use case.

- `FirewallRuleDelete.Yes` defaults to `true` so build scripts don't hang on the CLI's
  confirmation prompt.

### Notes

- Driven by Strata's adoption-wave gap list 2026-05-13. P2 priority, shipping in the
  same wave as `Tamp.AzureAppService` since both wrap thin `az` subsets.
