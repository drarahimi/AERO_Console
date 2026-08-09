// Skeleton port of xfoil.f's top-level menu loop (PROGRAM XFOIL), as a synchronous
// line-in / text-out session object -- the XFOIL analogue of Avl.Core's AvlSession,
// suitable for driving from a terminal-emulator UI in place of the xfoil.exe child
// process.
//
// SCOPE (this is the foundation slice): the geometry front end is ported and live --
// NACA buffer generation (naca.f) and airfoil file loading (aread.f), splined via
// spline.f. The numeric solver (PANGEN paneling, the inviscid vortex/source panel
// solve in xpanel.f, and the viscous OPER path in xoper.f/xbl.f) is NOT yet ported;
// commands that need it report that plainly rather than pretending to compute. As
// those modules land, this loop grows the corresponding branches (OPER, etc.).

using Xfoil.Core.Geometry;
using Xfoil.Core.Solver;

namespace Xfoil.Core.Cli;

/// <summary>Host-supplied file access for LOAD (airfoil .dat) and the SAVE side used
/// once polar/output writing is ported. Mirrors Avl.Core's IVirtualFileSystem.</summary>
public interface IVirtualFileSystem
{
    /// <summary>Returns file contents, or null if the file doesn't exist.</summary>
    string? ReadFile(string name);

    /// <summary>Writes command output to the named file. Default no-op so read-only
    /// hosts (e.g. tests) don't have to implement it.</summary>
    void WriteFile(string name, string contents) { }
}

/// <summary>OPER plot commands xfoil.exe answers with a graphics window; the native session
/// recognizes them and raises <see cref="XfoilSession.PlotRequested"/> so the host draws its own
/// modern plot.</summary>
public enum XfoilPlotKind
{
    Cp,       // CPX/CPV/CPWR -- Cp vs x
    Bl,       // VPLO/BLC/BLWT -- boundary-layer variables vs x
    Geometry, // airfoil geometry
}

public sealed class XfoilSession
{
    private const string TOP_BANNER = """
 ===================================================
  XFOIL Version 6.99
  Copyright (C) 2000   Mark Drela, Harold Youngren

  This software comes with ABSOLUTELY NO WARRANTY,
    subject to the GNU General Public License.

  Caveat computor
 ===================================================
""";

    // The top-level command menu XFOIL prints on startup (xfoil.f). Only the commands
    // this port actually implements do anything; the rest are shown for fidelity.
    private const string TOP_MENU = """
   QUIT    Exit program

  .OPER    Direct operating point(s)
  .MDES    Complex mapping design routine
  .QDES    Surface speed design routine
  .GDES    Geometry design routine

   SAVE f  Write airfoil to labeled coordinate file
   PSAV f  Write airfoil to plain coordinate file
   ISAV f  Write airfoil to ISES coordinate file
   MSAV f  Write airfoil to MSES coordinate file
   REVE    Reverse written-airfoil node ordering

   LOAD f  Read buffer airfoil from coordinate file
   NACA i  Set NACA 4,5-digit airfoil and buffer airfoil
   INTE    Set buffer airfoil by interpolating two airfoils
   NORM    Buffer airfoil normalization toggle
   XYCM rr Change CM reference location, currently  0.25000 0.00000

   BEND    Display structural properties of current airfoil

   PCOP    Set current-airfoil panel nodes directly from buffer airfoil points
   PANE    Set current-airfoil panel nodes ( 160 ) based on curvature
  .PPAR    Show/change paneling

  .PLOP    Plotting options

   WDEF f  Write  current-settings file
   RDEF f  Reread current-settings file
   NAME s  Specify new airfoil name
   NINC    Increment name version number

   Z       Zoom    | (available in all menus)
   U       Unzoom  |
""";

    private readonly IVirtualFileSystem _fs;

    // Current buffer airfoil (XB/YB in XFOIL terms) and its name. Set by NACA/LOAD.
    private double[]? _xb;
    private double[]? _yb;
    private string _name = "";

    // Paneling parameters and the paneled "current airfoil" produced by PANGEN.
    private PanelingParams _panPrm = new();
    private PanelAirfoil? _panel;

    // OPER (inviscid) state: the solver (rebuilt when geometry changes), current Mach,
    // menu mode, and the last computed operating point.
    private enum Mode { Top, Oper, Vplo }
    private Mode _mode = Mode.Top;
    private InviscidSolver? _solver;
    private double _mach;
    private InviscidPoint? _lastPoint;

    // Viscous state (VISC re / N ncrit toggle the viscous OPER path).
    private bool _viscous;
    private double _reynolds;
    private double _ncrit = 9.0;
    private Solver.Bl.ViscousResult? _lastViscous;

    /// <summary>The last inviscid operating point solved (OPER, ALFA), or null.</summary>
    public InviscidPoint? LastPoint => _lastPoint;

    /// <summary>The last viscous operating point solved (OPER, VISC, ALFA), or null.</summary>
    public Solver.Bl.ViscousResult? LastViscous => _lastViscous;

    /// <summary>Raised when the user issues an OPER plot command (CPX/VPLO/...) that xfoil.exe
    /// answers with a graphics window. The host draws its own modern plot from the Last* data
    /// below. See <see cref="XfoilPlotKind"/>.</summary>
    public event Action<XfoilPlotKind>? PlotRequested;

    // Plot data captured on the most recent ALFA solve, so the host can draw Cp/BL from exactly
    // what the console computed (no re-run). Cp = surface (x, cp); BL = boundary-layer stations
    // (viscous only, else null); Info = the point summary shown in the plot header.
    private System.Collections.Generic.List<(double x, double cp, double? cpi)>? _lastCp;
    private System.Collections.Generic.List<(double x, double y)>? _lastAirfoil;
    private System.Collections.Generic.IReadOnlyList<Solver.Bl.BlStationPoint>? _lastBl;
    private (double alpha, double cl, double cm, double cd, double re, double ncrit, double xtrTop, double xtrBot, double mach)? _lastPointInfo;

    public System.Collections.Generic.IReadOnlyList<(double x, double cp, double? cpi)>? LastCp => _lastCp;
    /// <summary>Airfoil surface nodes (x, y) of the panelled geometry for the most recent solve, so
    /// the host can draw the airfoil shape band under the Cp plot (with the BL/wake overlay), exactly
    /// like the docked point analysis / xfoil.exe's CPX view.</summary>
    public System.Collections.Generic.IReadOnlyList<(double x, double y)>? LastAirfoil => _lastAirfoil;
    public System.Collections.Generic.IReadOnlyList<Solver.Bl.BlStationPoint>? LastBoundaryLayer => _lastBl;
    public (double alpha, double cl, double cm, double cd, double re, double ncrit, double xtrTop, double xtrBot, double mach)? LastPointInfo => _lastPointInfo;

    /// <summary>Which boundary-layer variable the user last chose in the VPLO menu (0-based,
    /// matching the host's BL-quantity list: 0=Hk,1=Ue,2=Cf,3=Dstar/Theta top,4=bottom,5=Re_theta,
    /// 6=logRe_theta,7=N,8=Ctau,9=Cd). The host renders this quantity when it draws the BL plot.</summary>
    public int LastBlQuantity { get; private set; }

    public bool Quit { get; private set; }

    public XfoilSession(IVirtualFileSystem fs) => _fs = fs;

    /// <summary>The current buffer airfoil coordinates, or null if none loaded yet.
    /// Exposed so a host can render the loaded geometry.</summary>
    public (double[] x, double[] y)? BufferAirfoil => _xb == null ? null : (_xb, _yb!);
    public string AirfoilName => _name;

    /// <summary>The paneled current airfoil (PANGEN output), or null if none yet.
    /// This is the geometry the panel solver will run on.</summary>
    public PanelAirfoil? Panel => _panel;

    /// <summary>Startup banner + top menu + initial prompt, matching xfoil.exe on launch.</summary>
    public string Start() => $"\n  Copyright (C) 2026 Dr. Afshin Rahimi - Native Port\n{TOP_BANNER}\n\n File  xfoil.def  not found\n\n{TOP_MENU}\n{TopPrompt()}";

    private static string TopPrompt() => "\n XFOIL   c>  ";

    public string Feed(string line)
    {
        if (_mode == Mode.Vplo) return HandleVplo(line);
        if (_mode == Mode.Oper) return HandleOper(line);

        var (cmd, arg) = SplitCommand(line);
        string @out = "";
        switch (cmd)
        {
            case "":
                break;
            case "?":
                // Show the top-level command list, matching xfoil.exe's "?" at "XFOIL c>".
                return "\n" + TOP_MENU + "\n" + TopPrompt();
            case "QUIT":
            case "Q":
                Quit = true;
                return "";
            case "NACA":
            {
                if (!int.TryParse(arg.Trim(), out int ides) || ides <= 0)
                    return "\n Enter NACA 4 or 5-digit airfoil designation\n" + TopPrompt();
                var af = Naca.Generate(ides);
                if (af == null)
                    return "\n This designation not implemented.\n" + TopPrompt();
                SetBuffer(af.Xb, af.Yb, af.Name);
                @out += ThickCamberReport();
                @out += $"\n Buffer airfoil set using{_xb!.Length,4} points\n";
                @out += Repanel();
                break;
            }
            case "LOAD":
            {
                if (arg.Length == 0) return " Enter filename: ";
                var content = _fs.ReadFile(arg);
                if (content == null) { @out += $"\n File OPEN error.  Nonexistent file:  {arg}\n"; break; }
                var res = AirfoilFile.Read(content);
                if (!res.Ok) { @out += "\n File READ error.  Unrecognizable file format\n *** LOAD NOT COMPLETED ***\n"; break; }
                SetBuffer(res.X, res.Y, res.Name.Length > 0 ? res.Name : arg);
                @out += $"\n Number of input coordinate points:{res.N,4}\n";
                @out += $" {_name}\n";
                @out += ThickCamberReport();
                @out += Repanel();
                break;
            }
            case "PANE":
                @out += Repanel();
                break;
            case "OPER":
                if (_panel == null) { @out += "\n ***  No airfoil available  ***\n"; break; }
                _solver ??= new InviscidSolver(_panel);
                _mode = Mode.Oper;
                return OperPrompt();
            case "GDES":
            case "MDES":
            case "QDES":
                @out += $"\n * {cmd} is not yet available in the native XFOIL port.\n"
                      + "   (geometry, paneling, and the inviscid solve are ported; viscous is in progress)\n";
                break;
            default:
                @out += $" {cmd} command not recognized.  Type a \"?\" for list\n";
                break;
        }
        return @out + TopPrompt();
    }

    private void SetBuffer(double[] xb, double[] yb, string name)
    {
        _xb = xb;
        _yb = yb;
        _name = name;
    }

    // Runs PANGEN on the current buffer airfoil, mirroring xfoil.f (NACA/LOAD panel
    // automatically) and reporting the TE type as the real console does. Invalidates
    // the cached solver since the geometry changed.
    private string Repanel()
    {
        if (_xb == null) return "";
        _panel = Paneling.Pangen(_xb, _yb!, _name, _panPrm);
        _solver = null;
        _lastPoint = null;
        if (_panel == null) return "\n PANGEN: Buffer airfoil not available.\n";
        string te = _panel.Sharp
            ? "\n Sharp trailing edge\n"
            : $"\n Blunt trailing edge.  Gap ={_panel.Dste,9:F5}\n";
        return te + PanelParamsReport();
    }

    // Port of xgeom.f GEOPAR's thickness/camber print (WRITE(*,1000)), emitted by NACA/LOAD.
    private string ThickCamberReport()
    {
        var (thick, xthick, cambr, xcambr) = Xgeom.ThicknessCamber(_xb!, _yb!);
        return $"\n Max thickness = {thick,12:F6}  at x = {xthick,7:F3}\n"
             + $" Max camber    = {cambr,12:F6}  at x = {xcambr,7:F3}\n";
    }

    // Port of xfoil.f PANGEN's paneling-parameters block (WRITE(*,1100), when SHOPAR).
    private string PanelParamsReport()
    {
        var p = _panPrm;
        return "\n Paneling parameters used...\n"
             + $"   Number of panel nodes      {p.Npan,4}\n"
             + $"   Panel bunching parameter   {p.CvPar,6:F3}\n"
             + $"   TE/LE panel density ratio  {p.CteRat,6:F3}\n"
             + $"   Refined-area/LE panel density ratio   {p.CtrRat,6:F3}\n"
             + $"   Top    side refined area x/c limits {p.XsRef1,6:F3}{p.XsRef2,6:F3}\n"
             + $"   Bottom side refined area x/c limits {p.XpRef1,6:F3}{p.XpRef2,6:F3}\n";
    }

    private string OperPrompt() => _viscous ? "\n.OPERv   c>  " : "\n.OPERi   c>  ";

    // The OPER command menu xfoil.exe prints when you type "?" at ".OPERi/.OPERv c>" (xoper.f).
    // Only the ported commands actually do anything; the rest are shown for fidelity, exactly
    // like TOP_MENU.
    private const string OPER_MENU = """
   <cr>     Return to Top Level
   Visc r   Toggle Inviscid/Viscous  (Visc <Re> also sets Reynolds number)
   INVI     Set inviscid
   Re   r   Change Reynolds number
   Mach r   Change Mach number
   N    r   Change critical amplification ratio  Ncrit
  .VPAR    Change BL parameter(s)
   ITER i   Change viscous-solution iteration limit

   Alfa r   Prescribe alpha
   CL   r   Prescribe CL
   ASeq rrr Prescribe a sequence of alphas
   CSeq rrr Prescribe a sequence of CLs

   Pacc i   Toggle polar accumulation
   PGET f   Read  polar from save file
   PWRT f   Write polar to  save file
   PSUM     Show summary of stored polars

   CPX      Plot Cp vs x
   CPV      Plot airfoil with Cp vectors
   VPLO     Plot boundary-layer variables vs x

   Z        Zoom    | (available in all menus)
   U        Unzoom  |
""";

    // XFOIL OPER commands that open interactive graphics (no meaning in a text console).
    private static readonly HashSet<string> OperPlotCommands = new()
    {
        "VPLO", "CPV", "CPX", "CPWR", "PPLO", "PLOT", "HARD", "ANNO", "SIZE", "BLC", "BLWT", "GRID",
    };

    // OPER submenu: the ported subset (inviscid ALFA/CL sweep points + MACH). A blank
    // line returns to the top menu, matching XFOIL.
    private string HandleOper(string line)
    {
        var (cmd, arg) = SplitCommand(line);
        switch (cmd)
        {
            case "":
                _mode = Mode.Top;
                return TopPrompt();
            case "?":
                // Show the OPER command list, matching xfoil.exe's "?" at ".OPERi/.OPERv c>".
                return "\n" + OPER_MENU + "\n" + OperPrompt();
            case "ALFA":
            {
                if (!TryParseDouble(arg, out double a))
                    return "\n Enter angle of attack (deg)\n" + OperPrompt();
                if (_viscous)
                {
                    var sb = new System.Text.StringBuilder();
                    var vs = new Solver.Bl.ViscousSolver(_panel!, _reynolds, _mach, _ncrit) { Trace = s => sb.Append(s) };
                    _lastViscous = vs.Solve(a, 80);
                    CaptureViscousPlotData(vs, a);
                    return sb.ToString() + OperPrompt();
                }
                _lastPoint = _solver!.SolveAlpha(a, _mach);
                CaptureInviscidPlotData(a);
                return FormatPoint(_lastPoint) + OperPrompt();
            }
            case "MACH":
                if (TryParseDouble(arg, out double m)) _mach = m;
                return $"\n M  ={_mach,10:F4}\n" + OperPrompt();
            case "VISC":
                if (TryParseDouble(arg, out double re)) { _reynolds = re; _viscous = re > 0; }
                // MRSHOW(.TRUE.,.TRUE.): blank line, then Mach and Re.
                return $"\n M  ={_mach,10:F4}\n Re ={(int)_reynolds,10}\n" + OperPrompt();
            case "INVI":
                _viscous = false;
                return $"\n M  ={_mach,10:F4}\n" + OperPrompt();
            case "N": // Ncrit (VPAR N in XFOIL)
                if (TryParseDouble(arg, out double nc)) _ncrit = nc;
                return $"\n Ncrit = {_ncrit:F2}\n" + OperPrompt();
            default:
                if (OperPlotCommands.Contains(cmd))
                    return HandleOperPlotCommand(cmd);
                return $"\n * {cmd} is not available in the native OPER console. Ported: ALFA, MACH, VISC, INVI, N.\n" + OperPrompt();
        }
    }

    // xfoil.exe opens a graphics window for these; we have none, so recognize the command and
    // signal the host to draw its own modern plot from the last-computed data.
    private string HandleOperPlotCommand(string cmd)
    {
        if (cmd == "CPX" || cmd == "CPV" || cmd == "CPWR")
        {
            if (_lastCp == null)
                return "\n * No solution to plot.  Compute a point first (e.g. ALFA 2).\n" + OperPrompt();
            PlotRequested?.Invoke(XfoilPlotKind.Cp);
            return OperPrompt();
        }
        if (cmd == "VPLO" || cmd == "BLC" || cmd == "BLWT")
        {
            if (_lastBl == null)
                return "\n * No viscous solution.  Set VISC <Re>, then compute a point first.\n" + OperPrompt();
            // xfoil.exe's VPLO opens a submenu to choose which BL variable to plot. Do the same.
            _mode = Mode.Vplo;
            return VploMenu() + VploPrompt();
        }
        return $"\n * {cmd} is an interactive plot command - not available in the text console.\n"
             + "   Use CPX (Cp plot) or VPLO (boundary-layer plot) after computing a point.\n" + OperPrompt();
    }

    // The BL-variable choices, keyed the way xfoil.exe's VPLOT menu keys them, mapped to the host's
    // 0-based BL-quantity list. Order here is display order; the index is the host quantity.
    private static readonly (string key, int quantity, string label)[] VploVars =
    {
        ("H",   0, "kinematic shape parameter  Hk"),
        ("UE",  1, "edge velocity  Ue/Vinf"),
        ("CF",  2, "skin-friction coefficient  Cf"),
        ("DT",  3, "displacement & momentum thickness (top)  d*, theta"),
        ("DB",  4, "displacement & momentum thickness (bottom)  d*, theta"),
        ("RT",  5, "momentum-thickness Reynolds number  Re_theta"),
        ("RTL", 6, "log10( Re_theta )"),
        ("N",   7, "amplification ratio  n"),
        ("CT",  8, "max shear-stress coefficient  Ctau"),
        ("CD",  9, "dissipation coefficient  Cd"),
    };

    private static string VploMenu()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("\n ----- BL variable to plot -----\n");
        foreach (var (key, _, label) in VploVars)
            sb.Append($"   {key,-4}{label}\n");
        return sb.ToString();
    }

    private string VploPrompt() => "\n.VPLO   c>  ";

    // VPLO submenu: pick a BL variable (plot it via the host) or blank to return to OPER.
    private string HandleVplo(string line)
    {
        var (cmd, _) = SplitCommand(line);
        if (cmd.Length == 0)
        {
            _mode = Mode.Oper;
            return OperPrompt();
        }
        if (cmd == "?")
            return VploMenu() + VploPrompt();
        foreach (var (key, quantity, _) in VploVars)
        {
            if (cmd == key)
            {
                LastBlQuantity = quantity;
                PlotRequested?.Invoke(XfoilPlotKind.Bl);
                return VploPrompt(); // stay in the menu so more variables can be plotted
            }
        }
        return $"\n * {cmd} not recognized - pick a BL variable (\"?\" for the list) or <return> to exit.\n" + VploPrompt();
    }

    // The airfoil shape band under the Cp plot needs the panel node (x, y); capture it alongside
    // the Cp so the console CPX pop-out can draw the shape + BL/wake, not just the Cp curve.
    private void CaptureAirfoil()
    {
        _lastAirfoil = null;
        if (_panel is null) return;
        var xs = _panel.X;
        var ys = _panel.Y;
        int n = System.Math.Min(xs.Length, ys.Length);
        var af = new System.Collections.Generic.List<(double, double)>(n);
        for (int i = 0; i < n; i++)
            af.Add((xs[i], ys[i]));
        _lastAirfoil = af;
    }

    private void CaptureViscousPlotData(Solver.Bl.ViscousSolver vs, double alpha)
    {
        var cp = new System.Collections.Generic.List<(double, double, double?)>();
        foreach (var c in vs.SurfaceCp())
            cp.Add((c.x, c.cpv, c.cpi));
        _lastCp = cp;
        CaptureAirfoil();
        _lastBl = vs.BoundaryLayer();
        var r = _lastViscous!;
        _lastPointInfo = (alpha, r.Cl, r.Cm, r.Cd, _reynolds, _ncrit, r.XtrTop, r.XtrBottom, _mach);
    }

    private void CaptureInviscidPlotData(double alpha)
    {
        var cp = new System.Collections.Generic.List<(double, double, double?)>();
        var xs = _panel!.X;
        var cps = _lastPoint!.Cp;
        int n = System.Math.Min(xs.Length, cps.Length);
        for (int i = 0; i < n; i++)
            cp.Add((xs[i], cps[i], (double?)null));   // inviscid run: no separate reference curve
        _lastCp = cp;
        CaptureAirfoil();
        _lastBl = null;
        var p = _lastPoint!;
        _lastPointInfo = (alpha, p.Cl, p.Cm, p.Cdp, 0.0, 0.0, 0.0, 0.0, _mach);
    }

    private static string FormatPoint(InviscidPoint p) =>
        $"\n a ={p.Alpha,8:F3}     CL ={p.Cl,9:F4}\n"
        + $" Cm ={p.Cm,9:F4}     CD ={p.Cdp,10:F5}\n";

    private static string FormatViscous(Solver.Bl.ViscousResult r) =>
        $"\n a ={r.Alpha,8:F3}     CL ={r.Cl,9:F4}\n"
        + $" Cm ={r.Cm,9:F4}     CD ={r.Cd,10:F5}   =>   CDf ={r.Cdf,9:F5}    CDp ={r.Cdp,9:F5}\n"
        + $" Xtr: top ={r.XtrTop,7:F4}   bot ={r.XtrBottom,7:F4}"
        + (r.Converged ? "\n" : "   ** not converged **\n");

    private static bool TryParseDouble(string s, out double v) =>
        double.TryParse(s.Trim(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out v);

    private static (string cmd, string arg) SplitCommand(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.Length == 0) return ("", "");
        int sp = 0;
        while (sp < trimmed.Length && !char.IsWhiteSpace(trimmed[sp])) sp++;
        string cmd = trimmed.Substring(0, sp).ToUpperInvariant();
        string arg = sp < trimmed.Length ? trimmed.Substring(sp).Trim() : "";
        return (cmd, arg);
    }
}
