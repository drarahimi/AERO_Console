using Avl.Core.Model;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>Port of packages/avl-core/test/geometry/allegro.test.ts.</summary>
public class GeometryTests
{
    [Fact]
    public void Allegro_SurfaceStripVortexCounts_MatchReference()
    {
        var geo = AvlInputParser.ParseAvlGeometry(TestPaths.ReadRun("allegro.avl"));

        // Reference (avl3.51-32.exe): 5 Solid surfaces, 64 Strips, 410 Vortices
        Assert.Equal(5, geo.Surfaces.Count);
        Assert.Equal(64, geo.Strips.Count);
        Assert.Equal(410, geo.Vortices.Count);
    }

    [Fact]
    public void Allegro_ReferenceQuantities_Match()
    {
        var geo = AvlInputParser.ParseAvlGeometry(TestPaths.ReadRun("allegro.avl"));

        Assert.Equal(530.0, geo.Sref);
        Assert.Equal(6.6, geo.Cref);
        Assert.Equal(78.6, geo.Bref);
        Assert.Equal(new double[] { 3.25, 0.0, 0.5 }, geo.Xyzref0);
        Assert.Equal(0.0, geo.Mach0);
    }

    [Fact]
    public void B737_CountsAndReferenceQuantities_Match()
    {
        var geo = AvlInputParser.ParseAvlGeometry(TestPaths.ReadRun("b737.avl"));

        Assert.Equal(11, geo.Surfaces.Count);
        Assert.Equal(137, geo.Strips.Count);
        Assert.Equal(1505, geo.Vortices.Count);
        Assert.Equal(1260.0, geo.Sref);
        Assert.Equal(11.0, geo.Cref);
        Assert.Equal(113.0, geo.Bref);
    }
}
