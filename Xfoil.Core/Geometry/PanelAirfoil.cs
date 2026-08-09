// Result and parameter types for the PANGEN paneling port.

namespace Xfoil.Core.Geometry;

/// <summary>Paneling parameters, mirroring the XFOIL common-block values set in
/// xfoil.f's INIT and editable via the PANE/GETPAN menu. Defaults match XFOIL 6.99.</summary>
public sealed record PanelingParams
{
    /// <summary>Number of panel nodes (XFOIL NPAN, default 160).</summary>
    public int Npan { get; init; } = 160;
    /// <summary>Panel bunching parameter (CVPAR, default 1.0).</summary>
    public double CvPar { get; init; } = 1.0;
    /// <summary>TE/LE panel density ratio (CTERAT, default 0.15).</summary>
    public double CteRat { get; init; } = 0.15;
    /// <summary>Refined-area/LE panel density ratio (CTRRAT, default 0.2).</summary>
    public double CtrRat { get; init; } = 0.2;
    /// <summary>Top-side refined-area x/c limits (XSREF1/XSREF2, default 1/1 = off).</summary>
    public double XsRef1 { get; init; } = 1.0;
    public double XsRef2 { get; init; } = 1.0;
    /// <summary>Bottom-side refined-area x/c limits (XPREF1/XPREF2, default 1/1 = off).</summary>
    public double XpRef1 { get; init; } = 1.0;
    public double XpRef2 { get; init; } = 1.0;
}

/// <summary>The paneled "current airfoil" produced by PANGEN: node coordinates plus
/// the derived leading/trailing-edge geometry, node normals, and panel angles the
/// panel solver consumes. Arrays are 0-based, length N.</summary>
public sealed class PanelAirfoil
{
    public required string Name { get; init; }
    public required int N { get; init; }
    public required double[] X { get; init; }
    public required double[] Y { get; init; }
    public required double[] S { get; init; }   // arc length
    public required double[] Xp { get; init; }   // dX/dS
    public required double[] Yp { get; init; }   // dY/dS

    public required double Sle { get; init; }    // LE spline parameter
    public required double Xle { get; init; }
    public required double Yle { get; init; }
    public required double Xte { get; init; }
    public required double Yte { get; init; }
    public required double Chord { get; init; }

    /// <summary>Node normal-vector components (NCALC: NX, NY).</summary>
    public required double[] Nx { get; init; }
    public required double[] Ny { get; init; }
    /// <summary>Panel angles (APCALC: APANEL), length N (index N-1 is the TE panel).</summary>
    public required double[] Apanel { get; init; }

    // Trailing-edge geometry (TECALC), independent of the flow solution.
    public required bool Sharp { get; init; }
    public required double Ante { get; init; }   // normal projected TE gap area
    public required double Aste { get; init; }   // streamwise projected TE gap area
    public required double Dste { get; init; }   // total TE gap
    public required double Scs { get; init; }
    public required double Sds { get; init; }
}
