using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Avl.Core.Cli;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>End-to-end MODE through the AvlSession CLI (what frmGeometry's dynamic-modes
/// analysis drives): LOAD -> MASS -> MSET -> CASE -> OPER/X -> MODE/N/W, then the written
/// EIGOUT eigenvalue file must contain the reference exe's dynamic modes.</summary>
public class ModeSessionTests
{
    // allegro run case with a real flight condition (v=10 m/s) and geometry-unit CG,
    // matching what AVL would save; velocity is required for MODE.
    private const string ModesRun =
        " ---------------------------------------------\n" +
        " Run case  1:   modes\n\n" +
        " alpha        ->  alpha       =   0.00000    \n" +
        " beta         ->  beta        =   0.00000    \n" +
        " pb/2V        ->  pb/2V       =   0.00000    \n" +
        " qc/2V        ->  qc/2V       =   0.00000    \n" +
        " rb/2V        ->  rb/2V       =   0.00000    \n" +
        " elevator     ->  elevator    =   0.00000    \n" +
        " rudder       ->  rudder      =   0.00000    \n\n" +
        " velocity  =   10.00000    Lunit/Tunit\n" +
        " density   =   1.225000    Munit/Lunit^3\n" +
        " grav.acc. =   9.810000    Lunit/Tunit^2\n" +
        " X_cg      =   3.438000    Lunit\n" +
        " Z_cg      =   0.488300    Lunit\n" +
        " mass      =   0.514000    Munit\n";

    private sealed class ModesFs : IVirtualFileSystem
    {
        public readonly Dictionary<string, string> Written = new();
        public string? ReadFile(string name)
        {
            if (name == "modes.run") return ModesRun;
            var p = Path.Combine(TestPaths.RunsDir, name);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        public void WriteFile(string name, string contents) => Written[name] = contents;
    }

    [Fact]
    public void Session_ModeNW_WritesReferenceEigenvalues()
    {
        var fs = new ModesFs();
        var session = new AvlSession(fs);
        session.Start();
        foreach (var cmd in new[]
                 {
                     "LOAD allegro.avl", "MASS allegro.mass", "MSET 0", "CASE modes.run",
                     "OPER", "X", "", "MODE", "N", "W", "eig.out", "", "QUIT",
                 })
        {
            if (session.Quit) break;
            session.Feed(cmd);
        }

        Assert.True(fs.Written.ContainsKey("eig.out"), "eigenvalue file was written");
        var evs = ParseEig(fs.Written["eig.out"]);

        // Reference exe MODE eigenvalues (the highly-damped roll/short-period + others).
        Assert.Contains(evs, e => Near(e.re, -32.997) && Near(e.im, 0));
        Assert.Contains(evs, e => Near(e.re, -18.56) && Near(Math.Abs(e.im), 7.676));
        Assert.Contains(evs, e => Near(e.re, -2.009) && Near(Math.Abs(e.im), 6.175));
    }

    // Mirrors the "e4" project: an EMPTY .mass file plus a .run that carries its own
    // mass/inertia. avl.exe reads the run-case values and succeeds; the native port must too
    // (it used to build a singular tensor from the empty mass file and hang/fail).
    private const string E4Run =
        " ---------------------------------------------\n" +
        " Run case  1:  -Test-\n\n" +
        " alpha        ->  alpha       =   0.00000    \n" +
        " beta         ->  beta        =   0.00000    \n" +
        " pb/2V        ->  pb/2V       =   0.00000    \n" +
        " qc/2V        ->  qc/2V       =   0.00000    \n" +
        " rb/2V        ->  rb/2V       =   0.00000    \n\n" +
        " Mach      =   0.00000    \n" +
        " velocity  =   1.000000    Lunit/Tunit\n" +
        " density   =   1.000000    Munit/Lunit^3\n" +
        " grav.acc. =   9.800000    Lunit/Tunit^2\n" +
        " X_cg      =   0.250000    Lunit\n" +
        " mass      =   1.000000    Munit\n" +
        " Ixx       =   1.000000    Munit-Lunit^2\n" +
        " Iyy       =   1.000000    Munit-Lunit^2\n" +
        " Izz       =   1.000000    Munit-Lunit^2\n";

    private sealed class E4Fs : IVirtualFileSystem
    {
        public readonly Dictionary<string, string> Written = new();
        public string? ReadFile(string name)
        {
            if (name == "e4.run") return E4Run;
            if (name == "e4.mass") return ""; // empty, like the real project's file
            var p = Path.Combine(TestPaths.RunsDir, name);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        public void WriteFile(string name, string contents) => Written[name] = contents;
    }

    [Fact]
    public void Session_ModeN_EmptyMassFile_RunCaseInertia_ComputesModes()
    {
        var fs = new E4Fs();
        var session = new AvlSession(fs);
        session.Start();
        foreach (var cmd in new[] { "LOAD allegro.avl", "MASS e4.mass", "CASE e4.run", "OPER", "X", "" })
            session.Feed(cmd);
        session.Feed("MODE");
        string modeOut = session.Feed("N");

        // Non-zero run-case mass/inertia => avl.exe computes; native must not report a zero-inertia
        // guard failure or a singular-system failure, and must return eigenvalues.
        Assert.DoesNotContain("Zero Ixx", modeOut);
        Assert.DoesNotContain("not computed", modeOut);
        Assert.DoesNotContain("singular system", modeOut);
        Assert.Contains("  mode 1:", modeOut);
    }

    [Fact]
    public void Session_ModeN_WithoutPriorExec_AutoRunsFlowAndComputesModes()
    {
        // avl.exe's MODE 'N' runs EXEC itself (amode.f), so going straight LOAD/MASS/CASE ->
        // MODE/N (no OPER/X) must NOT print "Execute flow calculation first" -- it should trim
        // automatically and return eigenvalues, and must never hang.
        var fs = new ModesFs();
        var session = new AvlSession(fs);
        session.Start();
        session.Feed("LOAD allegro.avl");
        session.Feed("MASS allegro.mass");
        session.Feed("MSET 0");
        session.Feed("CASE modes.run");
        session.Feed("MODE");
        string modeOut = session.Feed("N");

        Assert.DoesNotContain("Execute flow calculation first", modeOut);
        Assert.Contains("  mode 1:", modeOut); // eigenvalues were produced
    }

    [Fact]
    public void Session_ModeN_OutputMatchesAvlEiglstFormat()
    {
        // The MODE 'N' console block must reproduce amode.f EIGLST's exact layout: the run-case
        // header (RTITLE padded to 40), "  mode J:" lines (1X + ' mode' + I2), and the four
        // eigenvector rows u/v/x, w/p/y, q/r/z, the/phi/psi with the ':' label + F11.4/F11.4/G12.4
        // columns and 6-space gaps. Verified against avl.exe's own output.
        var fs = new ModesFs();
        var session = new AvlSession(fs);
        session.Start();
        foreach (var cmd in new[] { "LOAD allegro.avl", "MASS allegro.mass", "MSET 0", "CASE modes.run", "OPER", "X", "", "MODE" })
            session.Feed(cmd);
        string modeOut = session.Feed("N");

        Assert.Contains("\n Run case  1:  modes", modeOut);            // 3100: 1X 'Run case' I3 ':  ' A
        Assert.Contains("\n  mode 1:", modeOut);                       // 3200: 1X ' mode' I2 ':'
        Assert.Contains("\n  mode 8:", modeOut);                       // allegro yields 8 kept modes
        Assert.Contains(" u  :", modeOut);                             // 3300 row labels
        Assert.Contains("      v  :", modeOut);                        // 6X gap before 2nd column
        Assert.Contains("      x  :", modeOut);                        // 6X gap before 3rd column
        Assert.Contains(" the:", modeOut);
        Assert.Contains(" phi:", modeOut);
        Assert.Contains(" psi:", modeOut);
        Assert.DoesNotContain("  mode:", modeOut);                     // old index-less format is gone
    }

    private static bool Near(double a, double b) => Math.Abs(a - b) < 0.05;

    private static List<(double re, double im)> ParseEig(string content)
    {
        var list = new List<(double, double)>();
        foreach (var line in Regex.Split(content, "\r\n|\r|\n"))
        {
            if (line.Length == 0 || line.TrimStart().StartsWith("#")) continue;
            var t = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (t.Length >= 3 && int.TryParse(t[0], out _)
                && double.TryParse(t[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var re)
                && double.TryParse(t[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var im))
                list.Add((re, im));
        }
        return list;
    }
}
