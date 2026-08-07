using System.IO;
using Avl.Core.Model;
using Avl.Core.Solver;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>Off-body flow-survey velocity vs the bundled reference exe (OB command,
/// single point). Total velocity WOB at (10,5,2) for allegro at alpha=0.</summary>
public class OffBodyTests
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

    [Fact]
    public void OffBodyVelocity_MatchesReference()
    {
        var geo = LoadAllegro();
        var ctx = SolveCase.BuildSolveContext(geo);
        var result = SolveCase.EvaluateCase(ctx, 0, 0, new double[] { 0, 0, 0 }, new double[geo.ControlNames.Count]);

        var wob = OffBody.VelocityAt(ctx, result, new double[] { 10.0, 5.0, 2.0 });

        // Reference avl3.51-32.exe OB survey at (10,5,2): Vx,Vy,Vz
        Assert.Equal(1.015272, wob[0], 5);
        Assert.Equal(-0.002483, wob[1], 5);
        Assert.Equal(-0.054212, wob[2], 5);
    }
}
