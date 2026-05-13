namespace Tamp.PostgresFlex;

/// <summary>
/// Common knobs shared by every <c>az postgres flexible-server</c> verb. Subscription /
/// resource group / server name triple every command needs, plus JSON-output toggle.
/// </summary>
public abstract class PostgresFlexSettingsBase
{
    /// <summary>Working directory for the spawned <c>az</c> process.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Per-invocation environment variables.</summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    /// <summary>Subscription id / name (<c>--subscription</c>). Optional when CLI default is correct.</summary>
    public string? Subscription { get; set; }

    /// <summary>Resource group (<c>--resource-group</c>). Required.</summary>
    public string? ResourceGroup { get; set; }

    /// <summary>Flexible Server name (<c>--name</c>). Required.</summary>
    public string? Name { get; set; }

    /// <summary>Emit machine-readable JSON (<c>--output json</c>). Default true.</summary>
    public bool JsonOutput { get; set; } = true;

    /// <summary>Subclasses produce the per-verb argument list AFTER the common <c>postgres flexible-server</c> prefix.</summary>
    protected abstract IEnumerable<string> BuildVerbArguments();

    /// <summary>Subclasses extending the secret list (e.g. password updates).</summary>
    protected virtual IEnumerable<Secret> CollectSecrets() => Array.Empty<Secret>();

    /// <summary>Whether this verb writes <c>--resource-group</c> + <c>--name</c>. Most do; override to false for verbs that don't (e.g. <c>list</c>).</summary>
    protected virtual bool RequiresServerScope => true;

    internal CommandPlan ToCommandPlan(Tool tool)
    {
        if (RequiresServerScope)
        {
            if (string.IsNullOrEmpty(ResourceGroup))
                throw new InvalidOperationException("ResourceGroup is required (set via SetResourceGroup).");
            if (string.IsNullOrEmpty(Name))
                throw new InvalidOperationException("Name is required (set via SetName).");
        }

        var args = new List<string> { "postgres", "flexible-server" };
        args.AddRange(BuildVerbArguments());
        if (RequiresServerScope)
        {
            args.Add("--resource-group"); args.Add(ResourceGroup!);
            args.Add("--name"); args.Add(Name!);
        }
        if (!string.IsNullOrEmpty(Subscription)) { args.Add("--subscription"); args.Add(Subscription!); }
        if (JsonOutput) { args.Add("--output"); args.Add("json"); }

        return new CommandPlan
        {
            Executable = tool.Executable.Value,
            Arguments = args,
            Environment = new Dictionary<string, string>(EnvironmentVariables),
            WorkingDirectory = WorkingDirectory ?? tool.WorkingDirectory,
            Secrets = CollectSecrets().ToList(),
        };
    }
}

/// <summary>Fluent setters for the common knobs.</summary>
public static class PostgresFlexSettingsBaseExtensions
{
    public static T SetWorkingDirectory<T>(this T s, string? cwd) where T : PostgresFlexSettingsBase { s.WorkingDirectory = cwd; return s; }
    public static T SetSubscription<T>(this T s, string? sub) where T : PostgresFlexSettingsBase { s.Subscription = sub; return s; }
    public static T SetResourceGroup<T>(this T s, string rg) where T : PostgresFlexSettingsBase { s.ResourceGroup = rg; return s; }
    public static T SetName<T>(this T s, string name) where T : PostgresFlexSettingsBase { s.Name = name; return s; }
    public static T SetJsonOutput<T>(this T s, bool v = true) where T : PostgresFlexSettingsBase { s.JsonOutput = v; return s; }
    public static T SetEnvironmentVariable<T>(this T s, string name, string value) where T : PostgresFlexSettingsBase { s.EnvironmentVariables[name] = value; return s; }
}

/// <summary>Settings for <c>az postgres flexible-server start</c> / <c>stop</c> / <c>restart</c>.</summary>
public sealed class LifecycleSettings : PostgresFlexSettingsBase
{
    internal enum Verb { Start, Stop, Restart }
    private readonly Verb _verb;
    internal LifecycleSettings(Verb verb) { _verb = verb; }

    protected override IEnumerable<string> BuildVerbArguments()
        => new[] { _verb switch { Verb.Start => "start", Verb.Stop => "stop", Verb.Restart => "restart", _ => throw new InvalidOperationException("Unknown verb.") } };
}

/// <summary>Settings for <c>az postgres flexible-server show</c>.</summary>
public sealed class ShowSettings : PostgresFlexSettingsBase
{
    protected override IEnumerable<string> BuildVerbArguments() { yield return "show"; }
}

/// <summary>Settings for <c>az postgres flexible-server list</c>. Operates at subscription / rg scope, not server scope.</summary>
public sealed class ListSettings : PostgresFlexSettingsBase
{
    protected override bool RequiresServerScope => false;
    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "list";
        if (!string.IsNullOrEmpty(ResourceGroup)) { yield return "--resource-group"; yield return ResourceGroup!; }
    }
}

/// <summary>Settings for <c>az postgres flexible-server firewall-rule create</c>.</summary>
public sealed class FirewallRuleCreateSettings : PostgresFlexSettingsBase
{
    /// <summary>Rule name. Required.</summary>
    public string? RuleName { get; set; }
    /// <summary>Starting IP. Required.</summary>
    public string? StartIp { get; set; }
    /// <summary>Ending IP. Defaults to StartIp when omitted.</summary>
    public string? EndIp { get; set; }

    public FirewallRuleCreateSettings SetRuleName(string ruleName) { RuleName = ruleName; return this; }
    public FirewallRuleCreateSettings SetStartIp(string ip) { StartIp = ip; return this; }
    public FirewallRuleCreateSettings SetEndIp(string? ip) { EndIp = ip; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(RuleName)) throw new InvalidOperationException("RuleName is required.");
        if (string.IsNullOrEmpty(StartIp)) throw new InvalidOperationException("StartIp is required.");
        yield return "firewall-rule";
        yield return "create";
        yield return "--rule-name"; yield return RuleName!;
        yield return "--start-ip-address"; yield return StartIp!;
        yield return "--end-ip-address"; yield return EndIp ?? StartIp!;
    }
}

/// <summary>Settings for <c>az postgres flexible-server firewall-rule delete</c>.</summary>
public sealed class FirewallRuleDeleteSettings : PostgresFlexSettingsBase
{
    public string? RuleName { get; set; }
    /// <summary>Skip the confirmation prompt (<c>--yes</c>). Default true — this verb is non-interactive in build scripts.</summary>
    public bool Yes { get; set; } = true;

    public FirewallRuleDeleteSettings SetRuleName(string ruleName) { RuleName = ruleName; return this; }
    public FirewallRuleDeleteSettings SetYes(bool v = true) { Yes = v; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(RuleName)) throw new InvalidOperationException("RuleName is required.");
        yield return "firewall-rule";
        yield return "delete";
        yield return "--rule-name"; yield return RuleName!;
        if (Yes) yield return "--yes";
    }
}

/// <summary>Settings for <c>az postgres flexible-server firewall-rule list</c>.</summary>
public sealed class FirewallRuleListSettings : PostgresFlexSettingsBase
{
    protected override IEnumerable<string> BuildVerbArguments() { yield return "firewall-rule"; yield return "list"; }
}

/// <summary>Settings for <c>az postgres flexible-server parameter set</c>.</summary>
public sealed class ParameterSetSettings : PostgresFlexSettingsBase
{
    /// <summary>Parameter name (e.g. <c>max_connections</c>). Required.</summary>
    public string? Parameter { get; set; }
    /// <summary>Value to set. Required.</summary>
    public string? Value { get; set; }
    /// <summary>Source (e.g. <c>user-override</c>, <c>system-default</c>).</summary>
    public string? Source { get; set; }

    public ParameterSetSettings SetParameter(string p) { Parameter = p; return this; }
    public ParameterSetSettings SetValue(string v) { Value = v; return this; }
    public ParameterSetSettings SetSource(string? s) { Source = s; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Parameter)) throw new InvalidOperationException("Parameter is required.");
        if (string.IsNullOrEmpty(Value)) throw new InvalidOperationException("Value is required.");
        yield return "parameter";
        yield return "set";
        yield return "--name"; yield return Parameter!;
        yield return "--value"; yield return Value!;
        if (!string.IsNullOrEmpty(Source)) { yield return "--source"; yield return Source!; }
    }
}

/// <summary>Settings for <c>az postgres flexible-server parameter show</c>.</summary>
public sealed class ParameterShowSettings : PostgresFlexSettingsBase
{
    public string? Parameter { get; set; }
    public ParameterShowSettings SetParameter(string p) { Parameter = p; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Parameter)) throw new InvalidOperationException("Parameter is required.");
        yield return "parameter";
        yield return "show";
        yield return "--name"; yield return Parameter!;
    }
}

/// <summary>Settings for <c>az postgres flexible-server update --admin-password</c>.</summary>
public sealed class UpdateAdminPasswordSettings : PostgresFlexSettingsBase
{
    /// <summary>New admin password. Required. Tracked as <see cref="Secret"/> so it gets redacted.</summary>
    public Secret? Password { get; set; }

    public UpdateAdminPasswordSettings SetPassword(Secret password) { Password = password; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (Password is null) throw new InvalidOperationException("Password is required.");
        yield return "update";
        yield return "--admin-password";
        yield return Password.Reveal();
    }

    protected override IEnumerable<Secret> CollectSecrets()
    {
        if (Password is not null) yield return Password;
    }
}
