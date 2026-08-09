// Global viscous-inviscid driver: the port of XFOIL's VISCAL loop plus its supporting
// setup (QWCALC/QISET/STFIND/IBLPAN/XICALC/IBLSYS/UICALC/QDCALC), boundary-layer
// initialization (MRCHUE/MRCHDU), global Newton assembly (SETBL), and update (UPDATE).
// It couples the inviscid panel solution (InviscidSolver) to the boundary-layer
// equations (BlInterval) via the mass-defect influence matrix DIJ, and converges the
// two together with the block solver (BlSolve).
//
// Scope of this port: fixed angle of attack, fixed Reynolds/Mach (MATYP=1), free or
// forced transition. Compressibility reduces out at Mach 0. Indexing mirrors XFOIL:
// BL side arrays are [is, ibl] with is in {0,1} and ibl 1-based (ibl=1 is the stagnation
// dummy, real stations start at 2); global node arrays are 0-based over N airfoil nodes
// followed by NW wake nodes.

using Xfoil.Core.Geometry;

namespace Xfoil.Core.Solver.Bl;

public sealed class ViscousResult
{
    public double Alpha, Cl, Cm, Cd, Cdf, Cdp;
    public bool Converged;
    public int Iterations;
    public double RmsResidual;
    // Per-side transition x/c (top, bottom).
    public double XtrTop, XtrBottom;
}

public sealed partial class ViscousSolver
{
    private const double Qinf = 1.0;
    private const double Pi = Math.PI;

    private readonly PanelAirfoil _g;
    private readonly InviscidSolver _inv;
    private readonly double _reynolds, _mach, _ncrit;

    private int _n, _nw, _nt;
    // Extended node arrays (airfoil 0..N-1, wake N..N+NW-1).
    private double[] _xg = null!, _yg = null!, _sg = null!, _nxg = null!, _nyg = null!, _apg = null!;
    private double[] _gamu1 = null!, _gamu2 = null!, _gam = null!;
    private double[] _qinvu0 = null!, _qinvu1 = null!, _qinv = null!, _qinvA = null!, _qvis = null!;
    private double[,] _dij = null!; // [NT,NT]

    private double _alfa; // radians
    private BlEnv _env = null!;
    private readonly BlParams _p = new();

    // Trailing-edge geometry.
    private double _ante, _dste, _chord, _xle, _yle, _xte, _yte;

    // Stagnation point.
    private int _ist; private double _sst, _sstGo, _sstGp;

    // BL side arrays, [is, ibl].
    private int _maxbl;
    private double[,] _thet = null!, _dstr = null!, _ctau = null!, _uedg = null!, _mass = null!;
    private double[,] _uinv = null!, _uinvA = null!, _xssi = null!, _vti = null!, _usav = null!;
    private int[,] _ipan = null!, _isys = null!;
    private readonly int[] _nbl = new int[2];
    private readonly int[] _iblte = new int[2];
    private readonly int[] _itran = new int[2];
    private readonly double[] _xssitr = new double[2];
    private readonly bool[] _tforce = new bool[2];
    private double[] _wgap = null!; // [NW]
    private double[] _xstrip = { 1.0, 1.0 }; // forced-transition x/c per side (>=1 => none)
    private int _nsys;

    // Newton system.
    private double[,,] _vm = null!, _va = null!, _vb = null!, _vdel = null!;
    private double[,] _vz = null!;

    private readonly BlInterval _itv = new();
    private bool _blini;

    /// <summary>Optional per-iteration trace (iter, rms, cl, cd) for diagnostics.</summary>
    public Action<int, double, double, double>? DebugTrace;

    /// <summary>Optional console-text sink. When set, Solve() emits the same progress
    /// lines the real xfoil.exe prints (unit-vorticity/wake/source setup, transition
    /// locations, and the per-iteration rms / CL / CD block).</summary>
    public Action<string>? Trace;
    private double _lastRlx = 1.0;

    private void EmitIterationTrace(int iter, double rms)
    {
        if (Trace == null) return;
        for (int is1 = 0; is1 < 2; is1++)
        {
            string prefix = _tforce[is1] ? " forced" : "  free ";
            Trace($" Side{is1 + 1,2}{prefix} transition at x/c = {_xoctr[is1],7:F4}{_itran[is1],5}\n");
        }
        var mc = MaxChange; // (rmax, ibl, iside, var)
        string line = $"\n{iter,4}   rms: {FortE104(rms)}   max: {FortE104(mc.rmax)}   {mc.var} at {mc.ibl,4}{mc.iside + 1,3}";
        if (_lastRlx < 1.0) line += $"   RLX:{_lastRlx,6:F3}";
        Trace(line + "\n");
        Trace($"       a ={_alfa * 180.0 / Pi,7:F3}      CL ={_cl,8:F4}\n");
        Trace($"      Cm ={_cm,8:F4}     CD ={_cd,9:F5}   =>   CDf ={_cdf,9:F5}    CDp ={_cd - _cdf,9:F5}\n");
    }

    // Fortran E10.4 format, e.g. 0.5241 -> "0.5241E+00", -4.356 -> "-.4356E+01".
    private static string FortE104(double v)
    {
        if (v == 0.0) return "0.0000E+00";
        int sign = v < 0 ? -1 : 1;
        double a = Math.Abs(v);
        int exp = (int)Math.Floor(Math.Log10(a)) + 1; // a/10^exp in [0.1,1)
        int mi = (int)Math.Round(a / Math.Pow(10.0, exp) * 10000.0);
        if (mi >= 10000) { mi /= 10; exp++; }
        if (mi < 1000) { mi *= 10; exp--; }
        string es = (exp < 0 ? "-" : "+") + Math.Abs(exp).ToString("D2");
        string body = "0." + mi.ToString("D4") + "E" + es;
        return sign < 0 ? "-" + body.Substring(1) : body;
    }
    public int Iblte0 => _iblte[0];
    public int Iblte1 => _iblte[1];
    public int Itran0 => _itran[0];
    public int Itran1 => _itran[1];
    public int Nbl0 => _nbl[0];
    public int Nbl1 => _nbl[1];

    public ViscousSolver(PanelAirfoil g, double reynolds, double mach = 0.0, double ncrit = 9.0)
    {
        _g = g;
        _inv = new InviscidSolver(g);
        _reynolds = reynolds; _mach = mach; _ncrit = ncrit;
    }

    /// <summary>Solves the viscous operating point at the given alpha (deg), running up to
    /// maxIter global Newton iterations.</summary>
    public ViscousResult Solve(double alphaDeg, int maxIter = 50)
    {
        _alfa = alphaDeg * Pi / 180.0;
        _env = BlEnv.Create(_mach, _reynolds, Qinf, _ncrit);
        _itv.Env = _env; _itv.P = _p;

        SetupGeometryAndInviscid();
        Trace?.Invoke(" Calculating unit vorticity distributions ...\n");
        Trace?.Invoke(" Calculating wake trajectory ...\n");
        Xywake();
        Qwcalc();
        Qiset();
        Stfind();
        Iblpan();
        Xicalc();
        Iblsys();
        Uicalc();
        Trace?.Invoke(" Calculating source influence matrix ...\n");
        Qdcalc();

        AllocateNewton();

        // initial Ue = inviscid Ue
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 1; ibl <= _nbl[is1]; ibl++)
                _uedg[is1, ibl] = _uinv[is1, ibl];
        _blini = false;

        Trace?.Invoke("\n Solving BL system ...\n");
        var res = new ViscousResult { Alpha = alphaDeg };
        double rms = 0;
        int iter;
        for (iter = 1; iter <= maxIter; iter++)
        {
            Setbl();       // MRCHUE (first) + MRCHDU + assemble Newton system
            SolveNewton(); // BlSolve on the assembled system
            rms = Update(); // apply deltas, compute rms change and new CL
            Qvfue();
            Gamqv();
            Stmove();
            ClCdCalc();
            if (DebugTrace != null) DebugTrace(iter, rms, _cl, _cd);
            EmitIterationTrace(iter, rms);
            if (rms < 1.0e-4) { res.Converged = true; break; }
        }
        res.Iterations = iter;
        res.RmsResidual = rms;
        res.Cl = _cl; res.Cm = _cm; res.Cd = _cd; res.Cdf = _cdf; res.Cdp = _cd - _cdf;
        res.XtrTop = _xoctr[0]; res.XtrBottom = _xoctr[1];
        return res;
    }

    // ---- geometry + inviscid setup -------------------------------------------------

    private void SetupGeometryAndInviscid()
    {
        _n = _g.N;
        _gamu1 = _inv.Gamu1; _gamu2 = _inv.Gamu2;
        _chord = _g.Chord; _xle = _g.Xle; _yle = _g.Yle; _xte = _g.Xte; _yte = _g.Yte;
        _ante = _g.Ante; _dste = _g.Dste;

        double cosa = Math.Cos(_alfa), sina = Math.Sin(_alfa);
        _gam = new double[_n];
        for (int i = 0; i < _n; i++) _gam[i] = cosa * _gamu1[i] + sina * _gamu2[i];
    }

    private WakeGeometry _wake = null!;

    private void Xywake()
    {
        _wake = Wake.XyWake(_g, _inv, _alfa * 180.0 / Pi);
        _nw = _wake.Nw;
        _nt = _n + _nw;
        _xg = new double[_nt]; _yg = new double[_nt]; _sg = new double[_nt];
        _nxg = new double[_nt]; _nyg = new double[_nt]; _apg = new double[_nt];
        for (int i = 0; i < _n; i++)
        { _xg[i] = _g.X[i]; _yg[i] = _g.Y[i]; _sg[i] = _g.S[i]; _nxg[i] = _g.Nx[i]; _nyg[i] = _g.Ny[i]; _apg[i] = _g.Apanel[i]; }
        for (int k = 0; k < _nw; k++)
        {
            int i = _n + k;
            _xg[i] = _wake.X[k]; _yg[i] = _wake.Y[k]; _sg[i] = _wake.S[k];
            _nxg[i] = _wake.Nx[k]; _nyg[i] = _wake.Ny[k]; _apg[i] = _wake.Apanel[k];
        }
        _maxbl = _nt + 4;
    }

    // Port of QWCALC: inviscid wake tangential velocities for alpha = 0 and 90.
    private void Qwcalc()
    {
        _qinvu0 = new double[_nt];
        _qinvu1 = new double[_nt];
        for (int i = 0; i < _n; i++) { _qinvu0[i] = _gamu1[i]; _qinvu1[i] = _gamu2[i]; }
        // first wake point == TE
        _qinvu0[_n] = _qinvu0[_n - 1];
        _qinvu1[_n] = _qinvu1[_n - 1];
        for (int i = _n + 1; i < _nt; i++)
        {
            _qinvu0[i] = Psi.StreamFn(_g, _gamu1, 1.0, 0.0, Qinf, 0, _xg[i], _yg[i], _nxg[i], _nyg[i]).psiNi;
            _qinvu1[i] = Psi.StreamFn(_g, _gamu2, 0.0, 1.0, Qinf, 0, _xg[i], _yg[i], _nxg[i], _nyg[i]).psiNi;
        }
    }

    // Port of QISET: combine unit distributions for the current alpha.
    private void Qiset()
    {
        double cosa = Math.Cos(_alfa), sina = Math.Sin(_alfa);
        _qinv = new double[_nt]; _qinvA = new double[_nt]; _qvis = new double[_nt];
        for (int i = 0; i < _nt; i++)
        {
            _qinv[i] = cosa * _qinvu0[i] + sina * _qinvu1[i];
            _qinvA[i] = -sina * _qinvu0[i] + cosa * _qinvu1[i];
        }
    }

    // Port of STFIND: stagnation point where GAM changes sign.
    private void Stfind()
    {
        int i;
        for (i = 0; i < _n - 1; i++)
            if (_gam[i] >= 0.0 && _gam[i + 1] < 0.0) break;
        if (i >= _n - 1) i = _n / 2;
        _ist = i;
        double dgam = _gam[i + 1] - _gam[i];
        double ds = _sg[i + 1] - _sg[i];
        _sst = _gam[i] < -_gam[i + 1] ? _sg[i] - ds * (_gam[i] / dgam) : _sg[i + 1] - ds * (_gam[i + 1] / dgam);
        if (_sst <= _sg[i]) _sst = _sg[i] + 1.0e-7;
        if (_sst >= _sg[i + 1]) _sst = _sg[i + 1] - 1.0e-7;
        _sstGo = (_sst - _sg[i + 1]) / dgam;
        _sstGp = (_sg[i] - _sst) / dgam;
    }

    // Port of IBLPAN: BL station -> panel index pointers.
    private void Iblpan()
    {
        _ipan = new int[2, _maxbl];
        _vti = new double[2, _maxbl];

        // top surface (is=0): from stagnation panel back to the LE (node 0)
        int ibl = 1;
        for (int i = _ist; i >= 0; i--) { ibl++; _ipan[0, ibl] = i; _vti[0, ibl] = 1.0; }
        _iblte[0] = ibl; _nbl[0] = ibl;

        // bottom surface (is=1)
        ibl = 1;
        for (int i = _ist + 1; i < _n; i++) { ibl++; _ipan[1, ibl] = i; _vti[1, ibl] = -1.0; }
        _iblte[1] = ibl;
        for (int iw = 1; iw <= _nw; iw++)
        {
            int i = _n + iw - 1; // global wake node (0-based): wake node iw-1
            ibl = _iblte[1] + iw;
            _ipan[1, ibl] = i; _vti[1, ibl] = -1.0;
        }
        _nbl[1] = _iblte[1] + _nw;

        // upper-wake pointers mirror the lower wake (for plotting/output only -- NBL(0)
        // stays at IBLTE(0); the wake is solved only on the lower side).
        for (int iw = 1; iw <= _nw; iw++)
        {
            _ipan[0, _iblte[0] + iw] = _ipan[1, _iblte[1] + iw];
            _vti[0, _iblte[0] + iw] = 1.0;
        }
    }

    // Port of XICALC: BL arc length on each side + wake, and the TE-gap wake array WGAP.
    private void Xicalc()
    {
        _xssi = new double[2, _maxbl];
        _wgap = new double[_nw];
        double xeps = 1.0e-7 * (_sg[_n - 1] - _sg[0]);

        // top side
        _xssi[0, 1] = 0.0;
        for (int ibl = 2; ibl <= _iblte[0]; ibl++)
        {
            int i = _ipan[0, ibl];
            _xssi[0, ibl] = Math.Max(_sst - _sg[i], xeps);
        }
        // bottom side
        _xssi[1, 1] = 0.0;
        for (int ibl = 2; ibl <= _iblte[1]; ibl++)
        {
            int i = _ipan[1, ibl];
            _xssi[1, ibl] = Math.Max(_sg[i] - _sst, xeps);
        }
        // wake arc length (continues from the lower TE)
        _xssi[1, _iblte[1] + 1] = _xssi[1, _iblte[1]];
        for (int ibl = _iblte[1] + 2; ibl <= _nbl[1]; ibl++)
        {
            int i = _ipan[1, ibl];
            _xssi[1, ibl] = _xssi[1, ibl - 1] + Math.Sqrt((_xg[i] - _xg[i - 1]) * (_xg[i] - _xg[i - 1]) + (_yg[i] - _yg[i - 1]) * (_yg[i] - _yg[i - 1]));
        }
        // upper-side wake arc length mirrors the lower
        for (int iw = 1; iw <= _nw; iw++)
            _xssi[0, _iblte[0] + iw] = _xssi[1, _iblte[1] + iw];

        // TE-gap (wake) thickness array
        const double telrat = 2.50;
        double crosp = (_g.Xp[0] * _g.Yp[_n - 1] - _g.Yp[0] * _g.Xp[_n - 1])
            / Math.Sqrt((_g.Xp[0] * _g.Xp[0] + _g.Yp[0] * _g.Yp[0]) * (_g.Xp[_n - 1] * _g.Xp[_n - 1] + _g.Yp[_n - 1] * _g.Yp[_n - 1]));
        double dwdxte = crosp / Math.Sqrt(1.0 - crosp * crosp);
        dwdxte = Math.Max(dwdxte, -3.0 / telrat);
        dwdxte = Math.Min(dwdxte, 3.0 / telrat);
        double aa = 3.0 + telrat * dwdxte;
        double bb = -2.0 - telrat * dwdxte;
        if (_g.Sharp)
            for (int iw = 0; iw < _nw; iw++) _wgap[iw] = 0.0;
        else
            for (int iw = 1; iw <= _nw; iw++)
            {
                int ibl = _iblte[1] + iw;
                double zn = 1.0 - (_xssi[1, ibl] - _xssi[1, _iblte[1]]) / (telrat * _ante);
                _wgap[iw - 1] = zn >= 0.0 ? _ante * (aa + bb * zn) * zn * zn : 0.0;
            }
    }

    // Port of IBLSYS: BL station -> Newton system row (0-based).
    private void Iblsys()
    {
        _isys = new int[2, _maxbl];
        int iv = 0;
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 2; ibl <= _nbl[is1]; ibl++)
                _isys[is1, ibl] = iv++;
        _nsys = iv;
    }

    // Port of UICALC: inviscid edge velocity on each BL station.
    private void Uicalc()
    {
        _uinv = new double[2, _maxbl];
        _uinvA = new double[2, _maxbl];
        for (int is1 = 0; is1 < 2; is1++)
        {
            _uinv[is1, 1] = 0.0; _uinvA[is1, 1] = 0.0;
            for (int ibl = 2; ibl <= _nbl[is1]; ibl++)
            {
                int i = _ipan[is1, ibl];
                _uinv[is1, ibl] = _vti[is1, ibl] * _qinv[i];
                _uinvA[is1, ibl] = _vti[is1, ibl] * _qinvA[i];
            }
        }
    }

    private void AllocateNewton()
    {
        _vm = new double[3, _nsys, _nsys];
        _va = new double[3, 2, _nsys];
        _vb = new double[3, 2, _nsys];
        _vdel = new double[3, 2, _nsys];
        _vz = new double[3, 2];
        _thet = new double[2, _maxbl]; _dstr = new double[2, _maxbl]; _ctau = new double[2, _maxbl];
        _uedg = new double[2, _maxbl]; _mass = new double[2, _maxbl]; _usav = new double[2, _maxbl];
    }

    private double _cl, _cm, _cd, _cdf;
    private readonly double[] _xoctr = new double[2];

    /// <summary>Surface pressure coefficient at every airfoil node (viscous Cp from the
    /// converged edge speed), in node order. Call after Solve().</summary>
    public IReadOnlyList<(double x, double y, double cp)> SurfaceCp()
    {
        double beta = Math.Sqrt(1.0 - _mach * _mach);
        double bfac = 0.5 * _mach * _mach / (1.0 + beta);
        var list = new List<(double, double, double)>(_n);
        for (int i = 0; i < _n; i++)
        {
            double q = _gam[i];
            double cpinc = 1.0 - (q / Qinf) * (q / Qinf);
            list.Add((_g.X[i], _g.Y[i], cpinc / (beta + bfac * cpinc)));
        }
        return list;
    }

    /// <summary>Boundary-layer profile at every BL station: both airfoil surfaces AND the
    /// wake (which trails ~1 chord past the TE, so x runs from 0 to ~2). Each point carries
    /// its x/y so the caller can split top/bottom by y-sign exactly like XFOIL's BL dump.
    /// Call after Solve().</summary>
    public List<BlStationPoint> BoundaryLayer()
    {
        var list = new List<BlStationPoint>();
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 2; ibl <= _iblte[is1]; ibl++)
                list.Add(BlPointAt(is1, ibl, wake: false));
        // wake is carried on the lower side only (nodes past the TE)
        for (int ibl = _iblte[1] + 1; ibl <= _nbl[1]; ibl++)
            list.Add(BlPointAt(1, ibl, wake: true));
        return list;
    }

    private BlStationPoint BlPointAt(int is1, int ibl, bool wake)
    {
        int i = _ipan[is1, ibl];
        double x = _xg[i], y = _yg[i];
        double uei = _uedg[is1, ibl], dsi = _dstr[is1, ibl], thi = _thet[is1, ibl];
        bool turb = wake || ibl >= _itran[is1];

        // recompute the secondary BL variables at this converged station.
        int ityp = wake ? 3 : (ibl < _itran[is1] ? 1 : 2);
        double ami = ibl < _itran[is1] ? _ctau[is1, ibl] : 0.0;
        double cti = ibl >= _itran[is1] ? _ctau[is1, ibl] : 0.0;
        double dswaki = wake ? _wgap[ibl - _iblte[is1] - 1] : 0.0;
        _itv.Simi = ibl == 2; _itv.Wake = wake; _itv.Turb = ibl > _itran[is1]; _itv.Tran = false;
        _itv.Blprv(_xssi[is1, ibl], ami, cti, thi, dsi, dswaki, uei);
        BlSys.Blkin(_itv.S2, _env);
        BlSys.Blvar(_itv.S2, _env, _p, ityp);
        var s2 = _itv.S2;

        double cf = wake ? 0.0 : s2.Cf;
        // dissipation coefficient CD = 0.5 * (2 CD/H*) * H* = 0.5 * Di * Hs
        double cd = 0.5 * s2.Di * s2.Hs;
        // amplification factor n (grows 0->Ncrit through the laminar region, then held at
        // Ncrit once transitioned) and the sqrt-shear-stress coefficient (turbulent only).
        double nAmp = turb ? _env.Amcrit : _ctau[is1, ibl];
        double ctau = turb ? _ctau[is1, ibl] : 0.0;

        return new BlStationPoint(x, y, uei, dsi, thi, cf, dsi / thi, nAmp, ctau, cd);
    }
}

/// <summary>One boundary-layer station's plottable quantities. N is the amplification
/// factor (laminar) held at Ncrit after transition; Ctau is the sqrt shear-stress
/// coefficient (turbulent); Cd is the dissipation coefficient.</summary>
public sealed record BlStationPoint(double X, double Y, double Ue, double Dstar, double Theta, double Cf, double H,
    double N, double Ctau, double Cd);
