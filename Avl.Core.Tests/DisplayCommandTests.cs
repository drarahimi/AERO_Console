using System.IO;
using Avl.Core.Model;
using Avl.Core.Output;
using Avl.Core.Solver;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>Byte-parity of the FN/FS/FSB/FE/HM/ST/SM/SB display commands against the
/// reference avl.exe transcripts (allegro, alpha=0). Ports the corresponding
/// packages/avl-core/test/solver/*.test.ts.</summary>
public class DisplayCommandTests
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

    private static CaseResult SolveAlpha0(Geometry geo) =>
        SolveCase.Solve(geo, new CaseInputs { AlfaDeg = 0, BetaDeg = 0, Wrot = new double[] { 0, 0, 0 } });

    private static void AssertBlock(string[] golden, string[] actual, Func<string, string> norm)
    {
        Assert.Equal(golden.Length, actual.Length);
        for (int i = 0; i < golden.Length; i++)
            Assert.Equal(norm(golden[i]), norm(actual[i]));
    }

    [Fact]
    public void Fn_SurfaceForces_MatchesReference()
    {
        var geo = LoadAllegro();
        var r = SolveAlpha0(geo);
        var actual = GoldenText.Normalize(OutSurface.FormatOutSurface(geo, r).Split("\n"));
        var golden = GoldenText.ExtractHeaderToDashes(TestPaths.ReadGolden("allegro-fn-alpha0.txt"), "Surface Forces (referred to Sref");
        AssertBlock(golden, actual, GoldenText.NormalizeNegativeZero);
    }

    [Fact]
    public void Fs_StripForces_MatchesReference()
    {
        var geo = LoadAllegro();
        var r = SolveAlpha0(geo);
        var actual = GoldenText.Normalize(OutStrip.FormatOutStrip(geo, r).Split("\n"));
        var golden = GoldenText.ExtractHeaderToDashes(TestPaths.ReadGolden("allegro-fs-alpha0.txt"), "Surface and Strip Forces by surface");
        AssertBlock(golden, actual, s => GoldenText.NormalizeIllConditionedCp(GoldenText.NormalizeNegativeZero(s)));
    }

    [Fact]
    public void Fsb_StripForcesBody_MatchesReference()
    {
        var geo = LoadAllegro();
        var r = SolveAlpha0(geo);
        var actual = GoldenText.Normalize(OutStrip.FormatOutStripBody(geo, r).Split("\n"));
        var golden = GoldenText.ExtractHeaderToDashes(TestPaths.ReadGolden("allegro-fsb-alpha0.txt"), "Surface and Strip Forces by surface");
        AssertBlock(golden, actual, s => GoldenText.NormalizeNearZero(GoldenText.NormalizeNegativeZero(s)));
    }

    [Fact]
    public void Fe_ElementForces_MatchesReference()
    {
        var geo = LoadAllegro();
        var r = SolveAlpha0(geo);
        var actual = GoldenText.Normalize(OutElement.FormatOutElement(geo, r).Split("\n"));
        var golden = GoldenText.ExtractHeaderToDashes(TestPaths.ReadGolden("allegro-fe-alpha0.txt"), "Vortex Strengths (by surface, by strip)");
        AssertBlock(golden, actual, GoldenText.NormalizeNegativeZero);
    }

    [Fact]
    public void Hm_HingeMoments_MatchesReference()
    {
        var geo = LoadAllegro();
        var r = SolveAlpha0(geo);
        var actual = GoldenText.Normalize(OutHinge.FormatOutHinge(geo, r.Totals, r.Delcon).Split("\n"));
        var golden = GoldenText.ExtractHeaderToSep(TestPaths.ReadGolden("allegro-hm-alpha0.txt"), "Control Hinge Moments");
        AssertBlock(golden, actual, GoldenText.NormalizeNearZero);
    }

    [Fact]
    public void St_StabilityDerivs_MatchesReference()
    {
        var geo = LoadAllegro();
        var ctx = SolveCase.BuildSolveContext(geo);
        var r = SolveCase.EvaluateCase(ctx, 0, 0, new double[] { 0, 0, 0 }, new double[geo.ControlNames.Count]);
        var derivs = StabDerivsCalc.ComputeStabDerivs(geo, ctx, r.Alfa, r.Beta, r.Wrot, r.Delcon);
        var actual = GoldenText.Normalize(OutStability.FormatStabilityDerivs(geo, r, derivs).Split("\n"));
        var golden = GoldenText.ExtractStabilityBlock(TestPaths.ReadGolden("allegro-st-alpha0.txt"));
        AssertBlock(golden, actual, GoldenText.NormalizeNegativeZero);
    }

    [Fact]
    public void Sm_StabilityDerivsBody_MatchesReference()
    {
        var geo = LoadAllegro();
        var ctx = SolveCase.BuildSolveContext(geo);
        var r = SolveCase.EvaluateCase(ctx, 0, 0, new double[] { 0, 0, 0 }, new double[geo.ControlNames.Count]);
        var derivs = StabDerivsCalc.ComputeStabDerivs(geo, ctx, r.Alfa, r.Beta, r.Wrot, r.Delcon, MomentAxis.Body);
        var actual = GoldenText.Normalize(OutStability.FormatStabilityDerivsBody(geo, r, derivs).Split("\n"));
        var golden = GoldenText.ExtractStabilityBlock(TestPaths.ReadGolden("allegro-sm-alpha0.txt"));
        AssertBlock(golden, actual, GoldenText.NormalizeNegativeZero);
    }

    [Fact]
    public void Sb_BodyAxisDerivs_MatchesReference()
    {
        var geo = LoadAllegro();
        var ctx = SolveCase.BuildSolveContext(geo);
        var r = SolveCase.EvaluateCase(ctx, 0, 0, new double[] { 0, 0, 0 }, new double[geo.ControlNames.Count]);
        var derivs = BodyDerivsCalc.ComputeBodyAxisDerivs(geo, ctx, r.Alfa, r.Vinf, r.Wrot, r.Delcon);
        var actual = GoldenText.Normalize(OutStability.FormatStabilityDerivsGeom(geo, r, derivs).Split("\n"));
        var golden = GoldenText.ExtractSbBlock(TestPaths.ReadGolden("allegro-sb-alpha0.txt"));
        AssertBlock(golden, actual, GoldenText.NormalizeNegativeZero);
    }
}
