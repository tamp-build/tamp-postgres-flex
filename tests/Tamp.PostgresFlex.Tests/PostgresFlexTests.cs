using System.Linq;
using Tamp;
using Tamp.PostgresFlex;
using Xunit;

namespace Tamp.PostgresFlex.Tests;

public sealed class PostgresFlexTests
{
    private static Tool FakeTool() => new(AbsolutePath.Create("/fake/az"));

    private static int IndexOf(IReadOnlyList<string> args, string token)
    {
        for (var i = 0; i < args.Count; i++) if (args[i] == token) return i;
        return -1;
    }

    [Theory]
    [InlineData("start")]
    [InlineData("stop")]
    [InlineData("restart")]
    public void Lifecycle_Verbs(string verb)
    {
        var plan = verb switch
        {
            "start" => PostgresFlex.Start(FakeTool(), s => s.SetResourceGroup("rg").SetName("pg")),
            "stop" => PostgresFlex.Stop(FakeTool(), s => s.SetResourceGroup("rg").SetName("pg")),
            "restart" => PostgresFlex.Restart(FakeTool(), s => s.SetResourceGroup("rg").SetName("pg")),
            _ => throw new InvalidOperationException(),
        };
        Assert.Equal(new[] { "postgres", "flexible-server", verb }, plan.Arguments.Take(3));
        Assert.Equal("rg", plan.Arguments[IndexOf(plan.Arguments, "--resource-group") + 1]);
        Assert.Equal("pg", plan.Arguments[IndexOf(plan.Arguments, "--name") + 1]);
    }

    [Fact]
    public void Show_Builds_Command()
    {
        var plan = PostgresFlex.Show(FakeTool(), s => s.SetResourceGroup("rg").SetName("pg"));
        Assert.Equal(new[] { "postgres", "flexible-server", "show" }, plan.Arguments.Take(3));
    }

    [Fact]
    public void List_Operates_At_Subscription_Scope_Without_Server_Name()
    {
        // Note: no SetName — list doesn't require server scope.
        var plan = PostgresFlex.List(FakeTool());
        Assert.Equal(new[] { "postgres", "flexible-server", "list" }, plan.Arguments.Take(3));
        // Must NOT carry --name at the server-scope position
        Assert.DoesNotContain("--name", plan.Arguments);
    }

    [Fact]
    public void List_With_Rg_Filter()
    {
        var plan = PostgresFlex.List(FakeTool(), s => s.SetResourceGroup("rg-strata"));
        Assert.Equal("rg-strata", plan.Arguments[IndexOf(plan.Arguments, "--resource-group") + 1]);
    }

    [Fact]
    public void FirewallRuleCreate_Defaults_EndIp_To_StartIp()
    {
        var plan = PostgresFlex.FirewallRuleCreate(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("pg")
            .SetRuleName("allow-prod-runner")
            .SetStartIp("10.0.0.5"));
        Assert.Equal("allow-prod-runner", plan.Arguments[IndexOf(plan.Arguments, "--rule-name") + 1]);
        Assert.Equal("10.0.0.5", plan.Arguments[IndexOf(plan.Arguments, "--start-ip-address") + 1]);
        Assert.Equal("10.0.0.5", plan.Arguments[IndexOf(plan.Arguments, "--end-ip-address") + 1]);
    }

    [Fact]
    public void FirewallRuleCreate_With_Range()
    {
        var plan = PostgresFlex.FirewallRuleCreate(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("pg")
            .SetRuleName("dev-vpn")
            .SetStartIp("10.0.0.1").SetEndIp("10.0.0.255"));
        Assert.Equal("10.0.0.1", plan.Arguments[IndexOf(plan.Arguments, "--start-ip-address") + 1]);
        Assert.Equal("10.0.0.255", plan.Arguments[IndexOf(plan.Arguments, "--end-ip-address") + 1]);
    }

    [Fact]
    public void FirewallRuleDelete_Adds_Yes_Flag_By_Default()
    {
        var plan = PostgresFlex.FirewallRuleDelete(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("pg").SetRuleName("old"));
        Assert.Contains("--yes", plan.Arguments);
    }

    [Fact]
    public void FirewallRuleList()
    {
        var plan = PostgresFlex.FirewallRuleList(FakeTool(), s => s.SetResourceGroup("rg").SetName("pg"));
        Assert.Equal(new[] { "postgres", "flexible-server", "firewall-rule", "list" }, plan.Arguments.Take(4));
    }

    [Fact]
    public void ParameterSet_Builds_Command()
    {
        var plan = PostgresFlex.ParameterSet(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("pg")
            .SetParameter("max_connections").SetValue("500"));
        Assert.Equal("max_connections", plan.Arguments[IndexOf(plan.Arguments, "--name") + 1]);
        Assert.Equal("500", plan.Arguments[IndexOf(plan.Arguments, "--value") + 1]);
    }

    [Fact]
    public void ParameterShow_Builds_Command()
    {
        var plan = PostgresFlex.ParameterShow(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("pg").SetParameter("shared_buffers"));
        Assert.Equal("shared_buffers", plan.Arguments[IndexOf(plan.Arguments, "--name") + 1]);
    }

    [Fact]
    public void UpdateAdminPassword_Tracks_Password_As_Secret()
    {
        var pwd = new Secret("admin-password", "RotateMe123!");
        var plan = PostgresFlex.UpdateAdminPassword(FakeTool(), s => s
            .SetResourceGroup("rg").SetName("pg").SetPassword(pwd));
        Assert.Equal("RotateMe123!", plan.Arguments[IndexOf(plan.Arguments, "--admin-password") + 1]);
        Assert.Contains(pwd, plan.Secrets);
    }

    [Fact]
    public void Missing_ResourceGroup_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PostgresFlex.Show(FakeTool(), s => s.SetName("pg")).Arguments.ToList());
    }

    [Fact]
    public void JsonOutput_Default_Adds_Output_Json_Flag()
    {
        var plan = PostgresFlex.Show(FakeTool(), s => s.SetResourceGroup("rg").SetName("pg"));
        Assert.Equal("json", plan.Arguments[IndexOf(plan.Arguments, "--output") + 1]);
    }

    [Fact]
    public void Executable_Is_Tool_Path()
    {
        var plan = PostgresFlex.Show(FakeTool(), s => s.SetResourceGroup("rg").SetName("pg"));
        Assert.Equal("/fake/az", plan.Executable);
    }
}
