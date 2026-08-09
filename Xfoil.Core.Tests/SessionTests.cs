using Xfoil.Core.Cli;
using Xfoil.Core.Engine;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>End-to-end tests driving the session/engine the way the WinForms host will:
/// feed console command lines, read text out.</summary>
public class SessionTests
{
    private sealed class NullFs : IVirtualFileSystem
    {
        public string? ReadFile(string name) => null;
    }

    [Fact]
    public void NacaOperAlfa_ProducesInviscidPoint()
    {
        var s = new XfoilSession(new NullFs());
        s.Start();
        s.Feed("NACA 0012");
        Assert.NotNull(s.Panel);
        Assert.Equal(160, s.Panel!.N);

        s.Feed("OPER");
        var outText = s.Feed("ALFA 5");

        Assert.NotNull(s.LastPoint);
        Assert.Equal(0.6034, s.LastPoint!.Cl, 3);
        Assert.Contains("CL", outText); // console echoes the result block
    }

    [Fact]
    public void NacaOperViscAlfa_ProducesViscousPoint()
    {
        var s = new XfoilSession(new NullFs());
        s.Start();
        s.Feed("NACA 0012");
        s.Feed("OPER");
        s.Feed("VISC 1000000");
        var outText = s.Feed("ALFA 5");

        Assert.NotNull(s.LastViscous);
        Assert.True(s.LastViscous!.Converged);
        Assert.Equal(0.5571, s.LastViscous!.Cl, 3);
        Assert.Equal(0.00848, s.LastViscous!.Cd, 4);
        Assert.Contains("CDf", outText);
    }

    [Fact]
    public void Engine_BannerAndPointFlow()
    {
        var captured = new System.Text.StringBuilder();
        var engine = new NativeXfoilEngine(new NullFs());
        engine.Output += t => captured.Append(t);
        engine.Start();
        Assert.Contains("XFOIL", captured.ToString());

        engine.Send("NACA 0012");
        engine.Send("OPER");
        engine.Send("ALFA 5");
        Assert.NotNull(engine.Session!.LastPoint);
        Assert.Equal(0.6034, engine.Session!.LastPoint!.Cl, 3);

        // Blank line leaves OPER; QUIT stops the engine.
        engine.Send("");
        engine.Send("QUIT");
        Assert.False(engine.IsRunning);
    }
}
