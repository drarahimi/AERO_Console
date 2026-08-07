using System.IO;
using Avl.Core.Model;
using Avl.Core.Output;
using Avl.Core.Solver;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>Port of packages/avl-core/test/solver/trim.test.ts -- Newton trim
/// converges to the reference avl.exe operating point (FT block + iteration count).</summary>
public class TrimTests
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

    private static void AssertTrim(TrimConstraint[] constraints, string goldenFile, int expectedIterations)
    {
        var geo = LoadAllegro();
        var trim = Trim.SolveTrimmedCase(geo, constraints);
        Assert.True(trim.Converged);
        Assert.Equal(expectedIterations, trim.Iterations);

        var actual = GoldenText.Normalize(OutTotal.FormatOutTotal(geo, trim.Result).Split("\n"));
        var golden = GoldenText.ExtractFirstOutTotalBlock(TestPaths.ReadGolden(goldenFile));
        Assert.Equal(golden.Length, actual.Length);
        for (int i = 0; i < golden.Length; i++)
            Assert.Equal(GoldenText.NormalizeNegativeZero(golden[i]), GoldenText.NormalizeNegativeZero(actual[i]));
    }

    private static TrimConstraint C(int fv, ConstraintKind k, double v) => new() { FreeVar = fv, Kind = k, Value = v };

    [Fact]
    public void AlphaToCl05_Trim_MatchesReference() => AssertTrim(new[]
    {
        C(0, ConstraintKind.CL, 0.5),
        C(1, ConstraintKind.Beta, 0),
        C(2, ConstraintKind.RotX, 0),
        C(3, ConstraintKind.RotY, 0),
        C(4, ConstraintKind.RotZ, 0),
        C(5, ConstraintKind.Control, 0),
        C(6, ConstraintKind.Control, 0),
    }, "allegro-ft-cl05.txt", 2);

    [Fact]
    public void DirectElevator10_MatchesReference() => AssertTrim(new[]
    {
        C(0, ConstraintKind.Alpha, 0),
        C(1, ConstraintKind.Beta, 0),
        C(2, ConstraintKind.RotX, 0),
        C(3, ConstraintKind.RotY, 0),
        C(4, ConstraintKind.RotZ, 0),
        C(5, ConstraintKind.Control, 10),
        C(6, ConstraintKind.Control, 0),
    }, "allegro-ft-elev10.txt", 1);

    [Fact]
    public void ElevatorToCm0_PitchTrim_MatchesReference() => AssertTrim(new[]
    {
        C(0, ConstraintKind.Alpha, 0),
        C(1, ConstraintKind.Beta, 0),
        C(2, ConstraintKind.RotX, 0),
        C(3, ConstraintKind.RotY, 0),
        C(4, ConstraintKind.RotZ, 0),
        C(5, ConstraintKind.Cm, 0),
        C(6, ConstraintKind.Control, 0),
    }, "allegro-ft-elevtrim.txt", 3);
}
