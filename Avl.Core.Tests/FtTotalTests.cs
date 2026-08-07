using System.IO;
using Avl.Core.Model;
using Avl.Core.Output;
using Avl.Core.Solver;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>Port of packages/avl-core/test/solver/ft-total.test.ts -- the FT
/// total-forces block must byte-match the reference avl.exe transcript.</summary>
public class FtTotalTests
{
    private static Geometry Load(string avlFile) =>
        AvlInputParser.ParseAvlGeometry(TestPaths.ReadRun(avlFile), new ParseOptions
        {
            ResolveAirfoilFile = name =>
            {
                var p = Path.Combine(TestPaths.RunsDir, name);
                return File.Exists(p) ? File.ReadAllText(p) : null;
            },
        });

    [Theory]
    [InlineData("allegro.avl", "allegro-ft-alpha0.txt")]
    [InlineData("b737.avl", "b737-ft-alpha0.txt")]
    public void FtTotalForcesBlock_MatchesReference(string avlFile, string goldenFile)
    {
        var geo = Load(avlFile);
        var result = SolveCase.Solve(geo, new CaseInputs { AlfaDeg = 0, BetaDeg = 0, Wrot = new double[] { 0, 0, 0 } });
        var actualLines = GoldenText.Normalize(OutTotal.FormatOutTotal(geo, result).Split("\n"));

        var goldenBlock = GoldenText.ExtractFirstOutTotalBlock(TestPaths.ReadGolden(goldenFile));

        Assert.Equal(goldenBlock.Length, actualLines.Length);
        for (int i = 0; i < goldenBlock.Length; i++)
        {
            Assert.Equal(
                GoldenText.NormalizeNegativeZero(goldenBlock[i]),
                GoldenText.NormalizeNegativeZero(actualLines[i]));
        }
    }
}
