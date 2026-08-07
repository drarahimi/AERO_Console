using System.IO;
using Avl.Core.Model;
using Avl.Core.RunCaseNs;
using Avl.Core.Solver;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>Round-trip tests for the .run writer (RUNSAV port): a written file must
/// parse back through RunCaseFile with constraints and parameters preserved.</summary>
public class RunCaseSaveFetchTests
{
    private static Geometry LoadAllegro() =>
        AvlInputParser.ParseAvlGeometry(TestPaths.ReadRun("allegro.avl"), new ParseOptions
        {
            ResolveAirfoilFile = name =>
            {
                var p = Path.Combine(TestPaths.RunsDir, name);
                return File.Exists(p) ? File.ReadAllText(p) : null;
            },
        });

    private static TrimConstraint C(int fv, ConstraintKind k, double v) => new() { FreeVar = fv, Kind = k, Value = v };

    [Fact]
    public void WriteThenParse_PreservesConstraintsAndParams()
    {
        var geo = LoadAllegro(); // controls: elevator (D1), rudder (D2)
        var rc = new RunCase
        {
            Index = 1,
            Title = " trimmed case ",
            Constraints = new List<TrimConstraint>
            {
                C(0, ConstraintKind.CL, 0.5),   // alpha -> CL = 0.5
                C(1, ConstraintKind.Beta, 0),
                C(2, ConstraintKind.RotX, 0),
                C(3, ConstraintKind.RotY, 0),
                C(4, ConstraintKind.RotZ, 0),
                C(5, ConstraintKind.Cm, 0),     // elevator -> Cm = 0
                C(6, ConstraintKind.Control, 0),
            },
            Params = new RunCaseParams { Mach = 0.12, XCg = 3.3, Mass = 2.5 },
        };

        string text = RunCaseWriter.Write(geo, new[] { rc });
        var back = RunCaseFile.ParseRunCaseFile(text, geo);

        Assert.Single(back);
        var b = back[0];
        Assert.Equal("trimmed case", b.Title);

        // Constraint kinds/values survive the round trip.
        Assert.Equal(ConstraintKind.CL, b.Constraints.First(c => c.FreeVar == 0).Kind);
        Assert.Equal(0.5, b.Constraints.First(c => c.FreeVar == 0).Value, 5);
        Assert.Equal(ConstraintKind.Cm, b.Constraints.First(c => c.FreeVar == 5).Kind);
        Assert.Equal(ConstraintKind.Control, b.Constraints.First(c => c.FreeVar == 6).Kind);

        // Parameters survive.
        Assert.Equal(0.12, b.Params.Mach!.Value, 5);
        Assert.Equal(3.3, b.Params.XCg!.Value, 5);
        Assert.Equal(2.5, b.Params.Mass!.Value, 5);
    }

    [Fact]
    public void Header_MatchesRunsavShape()
    {
        var geo = LoadAllegro();
        var rc = new RunCase { Index = 1, Title = " -unnamed- ", Constraints = new(), Params = new RunCaseParams() };
        // fill default constraints (alpha..rotz + 2 controls)
        var kinds = new[] { ConstraintKind.Alpha, ConstraintKind.Beta, ConstraintKind.RotX, ConstraintKind.RotY, ConstraintKind.RotZ };
        for (int i = 0; i < 5; i++) rc.Constraints.Add(C(i, kinds[i], 0));
        rc.Constraints.Add(C(5, ConstraintKind.Control, 0));
        rc.Constraints.Add(C(6, ConstraintKind.Control, 0));

        var lines = RunCaseWriter.Write(geo, new[] { rc }).Split('\n');
        Assert.Contains(lines, l => l.StartsWith(" ---------------------------------------------"));
        Assert.Contains(lines, l => l.Contains("Run case") && l.Contains("1:"));
        Assert.Contains(lines, l => l.Contains("alpha") && l.Contains("->"));
        Assert.Contains(lines, l => l.TrimStart().StartsWith("Mach"));
        Assert.Contains(lines, l => l.TrimStart().StartsWith("X_cg") && l.Contains("Lunit"));
    }
}
