using System.Numerics;
using Avl.Core.Solver;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>Independent validation of the real-general eigenvalue solver (the EISPACK RG
/// stand-in used by MODE) against matrices with known spectra.</summary>
public class EigenTests
{
    private static Complex[] Sorted(Complex[] e) =>
        e.OrderBy(c => c.Real).ThenBy(c => c.Imaginary).ToArray();

    private static void AssertSpectrum(double[] a, int n, Complex[] expected)
    {
        var got = Sorted(Eigen.Eigenvalues(a, n));
        var exp = Sorted(expected);
        Assert.Equal(exp.Length, got.Length);
        for (int i = 0; i < exp.Length; i++)
        {
            Assert.Equal(exp[i].Real, got[i].Real, 6);
            Assert.Equal(exp[i].Imaginary, got[i].Imaginary, 6);
        }
    }

    [Fact]
    public void Diagonal_EigenvaluesAreDiagonal()
    {
        var a = new double[] { -3, 0, 0, 0, 5, 0, 0, 0, 1.5 };
        AssertSpectrum(a, 3, new[] { new Complex(-3, 0), new Complex(5, 0), new Complex(1.5, 0) });
    }

    [Fact]
    public void Rotation2x2_PureImaginaryPair()
    {
        var a = new double[] { 0, -1, 1, 0 }; // eigenvalues +/- i
        AssertSpectrum(a, 2, new[] { new Complex(0, 1), new Complex(0, -1) });
    }

    [Fact]
    public void ComplexPair_OnePlusMinusI()
    {
        var a = new double[] { 1, -1, 1, 1 }; // eigenvalues 1 +/- i
        AssertSpectrum(a, 2, new[] { new Complex(1, 1), new Complex(1, -1) });
    }

    [Fact]
    public void Companion_RealRoots123()
    {
        // Companion matrix of (x-1)(x-2)(x-3) = x^3 - 6x^2 + 11x - 6.
        var a = new double[]
        {
            6, -11, 6,
            1, 0, 0,
            0, 1, 0,
        };
        AssertSpectrum(a, 3, new[] { new Complex(1, 0), new Complex(2, 0), new Complex(3, 0) });
    }

    [Fact]
    public void Mixed_RealAndComplex()
    {
        // Block-diagonal: real eigenvalue -2, and a 2x2 block with eigenvalues -0.5 +/- 2i.
        var a = new double[]
        {
            -2, 0, 0,
            0, -0.5, -2,
            0, 2, -0.5,
        };
        AssertSpectrum(a, 3, new[] { new Complex(-2, 0), new Complex(-0.5, 2), new Complex(-0.5, -2) });
    }
}
