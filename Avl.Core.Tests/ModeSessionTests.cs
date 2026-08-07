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
