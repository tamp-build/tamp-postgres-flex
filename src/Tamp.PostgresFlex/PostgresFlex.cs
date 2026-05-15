namespace Tamp.PostgresFlex;

/// <summary>
/// Typed wrappers for <c>az postgres flexible-server</c>. Targets the admin / lifecycle
/// surface adopters reach for in build + maintenance scripts: stop / start / restart,
/// firewall-rule CRUD, parameter updates, admin-password rotation.
/// </summary>
/// <remarks>
/// <code>
/// [FromPath("az")] readonly Tool Az = null!;
///
/// Target StopForMaintenance => _ => _.Executes(() => PostgresFlex.Stop(Az, s => s
///     .SetResourceGroup("rg-strata-dev")
///     .SetName("strata-postgres-dev")));
/// </code>
/// </remarks>
public static class PostgresFlex
{
    /// <summary><c>az postgres flexible-server start</c>.</summary>
    public static CommandPlan Start(Tool tool, Action<LifecycleSettings> configure)
        => BuildLifecycle(tool, LifecycleSettings.Verb.Start, configure);

    /// <summary><c>az postgres flexible-server stop</c>.</summary>
    public static CommandPlan Stop(Tool tool, Action<LifecycleSettings> configure)
        => BuildLifecycle(tool, LifecycleSettings.Verb.Stop, configure);

    /// <summary><c>az postgres flexible-server restart</c>.</summary>
    public static CommandPlan Restart(Tool tool, Action<LifecycleSettings> configure)
        => BuildLifecycle(tool, LifecycleSettings.Verb.Restart, configure);

    /// <summary><c>az postgres flexible-server show</c>.</summary>
    public static CommandPlan Show(Tool tool, Action<ShowSettings> configure)
        => Build<ShowSettings>(tool, configure);

    /// <summary><c>az postgres flexible-server list</c>. Operates at subscription / rg scope.</summary>
    public static CommandPlan List(Tool tool, Action<ListSettings>? configure = null)
        => Build<ListSettings>(tool, configure);

    /// <summary><c>az postgres flexible-server firewall-rule create</c>.</summary>
    public static CommandPlan FirewallRuleCreate(Tool tool, Action<FirewallRuleCreateSettings> configure)
        => Build<FirewallRuleCreateSettings>(tool, configure);

    /// <summary><c>az postgres flexible-server firewall-rule delete</c>.</summary>
    public static CommandPlan FirewallRuleDelete(Tool tool, Action<FirewallRuleDeleteSettings> configure)
        => Build<FirewallRuleDeleteSettings>(tool, configure);

    /// <summary><c>az postgres flexible-server firewall-rule list</c>.</summary>
    public static CommandPlan FirewallRuleList(Tool tool, Action<FirewallRuleListSettings> configure)
        => Build<FirewallRuleListSettings>(tool, configure);

    /// <summary><c>az postgres flexible-server parameter set --name &lt;p&gt; --value &lt;v&gt;</c>.</summary>
    public static CommandPlan ParameterSet(Tool tool, Action<ParameterSetSettings> configure)
        => Build<ParameterSetSettings>(tool, configure);

    /// <summary><c>az postgres flexible-server parameter show --name &lt;p&gt;</c>.</summary>
    public static CommandPlan ParameterShow(Tool tool, Action<ParameterShowSettings> configure)
        => Build<ParameterShowSettings>(tool, configure);

    /// <summary><c>az postgres flexible-server update --admin-password &lt;secret&gt;</c>.</summary>
    public static CommandPlan UpdateAdminPassword(Tool tool, Action<UpdateAdminPasswordSettings> configure)
        => Build<UpdateAdminPasswordSettings>(tool, configure);

    // ---- Object-init overloads (TAM-161) ----
    // Parallel surface to the fluent verbs above. Both styles produce identical
    // CommandPlans; fluent stays canonical in docs and `tamp init` templates.
    //
    // Lifecycle verbs (Start / Stop / Restart) intentionally stay fluent-only:
    // their shared LifecycleSettings carries an internal verb selector — users
    // can't construct it object-init style without choosing a verb, and the
    // current fluent shape already pins the verb at the call site.
    public static CommandPlan Show(Tool tool, ShowSettings settings) => Plan(tool, settings);
    public static CommandPlan List(Tool tool, ListSettings settings) => Plan(tool, settings);
    public static CommandPlan FirewallRuleCreate(Tool tool, FirewallRuleCreateSettings settings) => Plan(tool, settings);
    public static CommandPlan FirewallRuleDelete(Tool tool, FirewallRuleDeleteSettings settings) => Plan(tool, settings);
    public static CommandPlan FirewallRuleList(Tool tool, FirewallRuleListSettings settings) => Plan(tool, settings);
    public static CommandPlan ParameterSet(Tool tool, ParameterSetSettings settings) => Plan(tool, settings);
    public static CommandPlan ParameterShow(Tool tool, ParameterShowSettings settings) => Plan(tool, settings);
    public static CommandPlan UpdateAdminPassword(Tool tool, UpdateAdminPasswordSettings settings) => Plan(tool, settings);

    private static CommandPlan Build<T>(Tool tool, Action<T>? configure) where T : PostgresFlexSettingsBase, new()
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        var s = new T();
        configure?.Invoke(s);
        return s.ToCommandPlan(tool);
    }

    private static CommandPlan BuildLifecycle(Tool tool, LifecycleSettings.Verb verb, Action<LifecycleSettings> configure)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new LifecycleSettings(verb);
        configure(s);
        return s.ToCommandPlan(tool);
    }

    private static CommandPlan Plan<T>(Tool tool, T settings) where T : PostgresFlexSettingsBase
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }
}
