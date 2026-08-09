using Xfoil.Core.Geometry;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Parity tests for the naca.f port (NACA4/NACA5 buffer generation).</summary>
public class NacaTests
{
    [Fact]
    public void Naca0012_PointCountAndName_MatchXfoil()
    {
        var af = Naca.Naca4(12);
        // NB = 2*NSIDE - 1 with NSIDE = IQX/3 = 166  ->  331 buffer points.
        Assert.Equal(331, af.Xb.Length);
        Assert.Equal(331, af.Yb.Length);
        Assert.Equal("NACA 0012", af.Name);
    }

    [Fact]
    public void Naca0012_LoopStartsAndEndsAtTrailingEdge()
    {
        var af = Naca.Naca4(12);
        // Loop runs TE -> LE -> TE, so first and last x are 1.0 (the TE).
        Assert.Equal(1.0, af.Xb[0], 12);
        Assert.Equal(1.0, af.Xb[^1], 12);
    }

    [Fact]
    public void Naca0012_LeadingEdgeAtOrigin()
    {
        var af = Naca.Naca4(12);
        // The LE node sits at the middle of the loop (index NSIDE-1) at (0, 0).
        int le = Naca.DefaultNSide - 1;
        Assert.Equal(0.0, af.Xb[le], 12);
        Assert.Equal(0.0, af.Yb[le], 12);
    }

    [Fact]
    public void Naca0012_IsSymmetric_MaxHalfThicknessNearTwelvePercent()
    {
        var af = Naca.Naca4(12);
        double maxY = af.Yb.Max();
        double minY = af.Yb.Min();
        // Symmetric section: upper and lower extents mirror each other.
        Assert.Equal(maxY, -minY, 6);
        // Half-thickness peak for a 12% section (open-TE XFOIL coefficients) ~0.066.
        Assert.InRange(maxY, 0.058, 0.070);
    }

    [Fact]
    public void Naca2412_HasPositiveCamber()
    {
        var symmetric = Naca.Naca4(12);
        var cambered = Naca.Naca4(2412);
        Assert.Equal("NACA 2412", cambered.Name);
        // Net upward camber: mean y is ~0 for 0012 but clearly positive for 2412.
        Assert.True(Math.Abs(symmetric.Yb.Average()) < 1e-9);
        Assert.True(cambered.Yb.Average() > 0.005);
    }

    [Fact]
    public void Naca23012_FiveDigit_Recognized()
    {
        var af = Naca.Naca5(23012);
        Assert.NotNull(af);
        Assert.Equal("NACA 23012", af!.Name);
        Assert.Equal(331, af.Xb.Length);
    }

    [Fact]
    public void Naca_UnrecognizedDesignation_ReturnsNull()
    {
        // Above 25099 no mean-line family is defined (matches xfoil.f ITYPE=0).
        Assert.Null(Naca.Generate(99999));
        // A 5-digit code outside the 210..250 families is also rejected.
        Assert.Null(Naca.Naca5(21112));
    }
}
