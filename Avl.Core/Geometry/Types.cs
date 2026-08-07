// Data model for parsed .avl geometry, loosely mirroring AVL.INC's SURF_*/STRP_*/VRTX_*
// COMMON blocks but grouped into plain objects/arrays instead of flat parallel arrays.
//
// Port of packages/avl-core/src/geometry/types.ts. The TypeScript [number,number,number]
// tuples are represented here as double[] (length 3) to keep reference/index semantics
// identical to the original, since the solver mutates and indexes these directly.

namespace Avl.Core.Model;

public sealed class ControlDeclaration
{
    public string Name = "";
    public double Gain;
    public double XHinge;
    public double[] HingeVec = new double[3];
    public double Refl;
}

public sealed class DesignDeclaration
{
    public string Name = "";
    public double Gain;
}

/// <summary>Camberline data for one airfoil, x/c-normalized, from NACA/AIRFOIL/AFIL.</summary>
public sealed class AirfoilData
{
    /// <summary>x/c stations, ascending, normalized 0..1.</summary>
    public List<double> Xasec = new();
    /// <summary>Camberline slope dz/dx at each xasec station.</summary>
    public List<double> Sasec = new();
    /// <summary>Thickness (t/c) at each xasec station.</summary>
    public List<double> Tasec = new();
}

public sealed class Section
{
    public double[] Xle = new double[3];
    public double Chord;
    /// <summary>Incidence angle, radians.</summary>
    public double Ainc;
    public int Nspan;
    public double Sspace;
    /// <summary>[CLneg, CDneg, CLmid, CDmid, CLpos, CDpos] parabolic drag polar, all zero if unset.</summary>
    public double[] Clcd = new double[6];
    public double Claf;
    public AirfoilData Airfoil = new();
    public List<ControlDeclaration> Controls = new();
    public List<DesignDeclaration> Designs = new();
}

public sealed class Surface
{
    public string Title = "";
    public int Nvc;
    public double Cspace;
    /// <summary>0 means "derive spanwise spacing per-section from each SECTION's Nspan/Sspace".</summary>
    public int Nvs;
    public double Sspace;
    public double? YDuplicate;
    public double Angle;
    public double[] Translate = new double[3];
    public double[] Scale = new double[3];
    public bool NoWake;
    public bool NoAlbe;
    public bool NoLoad;
    /// <summary>Component/index grouping (defaults to surface's own 1-based index).</summary>
    public int Component;
    public List<Section> Sections = new();
    /// <summary>true for a mirror-image surface synthesized by YDUPLICATE (SDUPL).</summary>
    public bool IsDuplicateImage;
    /// <summary>+1 for normally-defined surfaces, -1 for duplicate images (IMAGS).</summary>
    public int ImageSign;
    /// <summary>For a duplicate-image surface: 0-based index of the source surface.</summary>
    public int? DuplicateOf;
    /// <summary>For a duplicate-image surface: the y=yMirror mirror plane.</summary>
    public double? YMirror;
}

public sealed class Strip
{
    public int SurfaceIndex; // 0-based index into Geometry.Surfaces
    /// <summary>Leading-edge points of the two strip edges and the strip centerline.</summary>
    public double[] Rle1 = new double[3];
    public double[] Rle2 = new double[3];
    public double[] Rle = new double[3];
    public double Chord1;
    public double Chord2;
    public double Chord;
    /// <summary>Strip width (spanwise arc-length extent).</summary>
    public double Wstrip;
    public double TanLE;
    public double TanTE;
    public double Ainc;
    /// <summary>Number of chordwise vortices in this strip.</summary>
    public int Nvc;
    /// <summary>Index of first vortex belonging to this strip, 0-based into Geometry.Vortices.</summary>
    public int FirstVortex;
    /// <summary>Parabolic-drag-polar data interpolated to this strip.</summary>
    public double[] Clcd = new double[6];
    public bool Viscous;
    /// <summary>Per-control-variable hinge unit vector (index-aligned with Geometry.ControlNames); zero vector if this control doesn't affect this strip.</summary>
    public List<double[]> Vhinge = new();
    /// <summary>Per-control-variable hinge-axis point on the strip's chordline.</summary>
    public List<double[]> Phinge = new();
    /// <summary>Per-control-variable reflection sign (VREFL: -1 mirrors deflection sense on a duplicate-image strip).</summary>
    public List<double> Vrefl = new();
}

public sealed class Vortex
{
    public int StripIndex; // 0-based index into Geometry.Strips
    /// <summary>Bound-vortex leg endpoints (quarter-chord-ish locations from chordwise spacing).</summary>
    public double[] Rv1 = new double[3];
    public double[] Rv2 = new double[3];
    public double[] Rv = new double[3]; // strip-centerline bound-vortex point
    public double[] Rc = new double[3]; // control point (strip-centerline)
    public double[] Rs = new double[3]; // sense point (strip-centerline)
    public double Slopec; // camberline slope at control point
    public double Slopev; // camberline slope at vortex point
    public double Dx; // chordwise panel width (dimensional)
    public double Chord;
    public bool HasWake;
    /// <summary>Per-control-variable gain*(fraction of this panel aft of the hinge line), i.e. DCONTROL -- 0 for controls that don't touch this panel.</summary>
    public List<double> Dcontrol = new();
}

/// <summary>A slender body (fuselage/nacelle) modeled as a line of source+doublet
/// segments, from a BODY block (amake.f MAKEBODY). Nodes are ordered along the axis.</summary>
public sealed class Body
{
    public string Title = "";
    /// <summary>Line-singularity node positions [x,y,z], length NL.</summary>
    public List<double[]> Nodes = new();
    /// <summary>Body radius at each node, length NL.</summary>
    public List<double> Radii = new();
    /// <summary>Enclosed volume, wetted surface area, and axial length (VOLBDY/SRFBDY/ELBDY).</summary>
    public double Volume;
    public double Surface;
    public double Length;
    /// <summary>+1 for a normally-defined body, -1 for a YDUPLICATE image.</summary>
    public int ImageSign = 1;
}

public sealed class Geometry
{
    public string Title = "";
    public double Mach0;
    public int IYsym;
    public int IZsym;
    public double ZSym;
    public double Sref;
    public double Cref;
    public double Bref;
    public double[] Xyzref0 = new double[3];
    public double Cdoref0;
    public List<Surface> Surfaces = new();
    public List<Strip> Strips = new();
    public List<Vortex> Vortices = new();
    public List<Body> Bodies = new();
    public List<string> ControlNames = new();
    public List<string> DesignNames = new();
}
