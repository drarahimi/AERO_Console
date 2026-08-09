using Xfoil.Core.Geometry;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Parity tests for the aread.f port (airfoil coordinate file reader).</summary>
public class AirfoilFileTests
{
    [Fact]
    public void PlainFile_NoHeader_ParsedAsType1()
    {
        string dat = "1.0 0.0\n0.5 0.06\n0.0 0.0\n0.5 -0.06\n1.0 0.0\n";
        var r = AirfoilFile.Read(dat);
        Assert.True(r.Ok);
        Assert.Equal(1, r.Type);
        Assert.Equal(5, r.N);
        Assert.Equal(1.0, r.X[0], 12);
        Assert.Equal(0.06, r.Y[1], 12);
        Assert.Equal("", r.Name);
    }

    [Fact]
    public void LabeledFile_WithNameHeader_ParsedAsType2()
    {
        string dat = "NACA 0012\n1.0 0.0\n0.0 0.0\n1.0 0.0\n";
        var r = AirfoilFile.Read(dat);
        Assert.True(r.Ok);
        Assert.Equal(2, r.Type);
        Assert.Equal("NACA 0012", r.Name);
        Assert.Equal(3, r.N);
    }

    [Fact]
    public void CommentLines_AreSkipped()
    {
        string dat = "# a comment\nMy Airfoil\n! another comment\n1.0 0.0\n0.0 0.0\n1.0 0.0\n";
        var r = AirfoilFile.Read(dat);
        Assert.True(r.Ok);
        Assert.Equal("My Airfoil", r.Name);
        Assert.Equal(3, r.N);
    }

    [Fact]
    public void Terminator_999_StopsElement()
    {
        string dat = "1.0 0.0\n0.0 0.0\n1.0 0.0\n999.0 999.0\n2.0 2.0\n";
        var r = AirfoilFile.Read(dat);
        Assert.True(r.Ok);
        Assert.Equal(3, r.N); // points after 999/999 belong to the next element
    }

    [Fact]
    public void Empty_ReturnsError()
    {
        Assert.False(AirfoilFile.Read("").Ok);
    }
}
