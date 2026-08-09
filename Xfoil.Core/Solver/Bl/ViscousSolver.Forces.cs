// Port of xpanel.f STMOVE (stagnation-point relocation) and xfoil.f CLCALC/CDCALC
// (integrated lift/moment and the Squire-Young + friction drag).

namespace Xfoil.Core.Solver.Bl;

public sealed partial class ViscousSolver
{
    private double[,] _tau = null!;

    // Port of STMOVE: relocate the stagnation point if the sign change in GAM moved to a
    // different panel; otherwise just recompute the arc-length arrays.
    private void Stmove()
    {
        int istold = _ist;
        Stfind();
        if (_ist == istold) { Xicalc(); return; }

        Iblpan();
        Uicalc();
        Xicalc();
        Iblsys();

        if (_ist > istold)
        {
            int idif = _ist - istold;
            _itran[0] += idif; _itran[1] -= idif;
            for (int ibl = _nbl[0]; ibl >= idif + 2; ibl--) ShiftTo(0, ibl, ibl - idif);
            double dudx = _uedg[0, idif + 2] / _xssi[0, idif + 2];
            for (int ibl = idif + 1; ibl >= 2; ibl--) { CopyFromStation(0, ibl, 0, idif + 2); _uedg[0, ibl] = dudx * _xssi[0, ibl]; }
            for (int ibl = 2; ibl <= _nbl[1]; ibl++) ShiftTo(1, ibl, ibl + idif);
        }
        else
        {
            int idif = istold - _ist;
            _itran[0] -= idif; _itran[1] += idif;
            for (int ibl = _nbl[1]; ibl >= idif + 2; ibl--) ShiftTo(1, ibl, ibl - idif);
            double dudx = _uedg[1, idif + 2] / _xssi[1, idif + 2];
            for (int ibl = idif + 1; ibl >= 2; ibl--) { CopyFromStation(1, ibl, 1, idif + 2); _uedg[1, ibl] = dudx * _xssi[1, ibl]; }
            for (int ibl = 2; ibl <= _nbl[0]; ibl++) ShiftTo(0, ibl, ibl + idif);
        }

        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 2; ibl <= _nbl[is1]; ibl++)
                _mass[is1, ibl] = _dstr[is1, ibl] * _uedg[is1, ibl];
    }

    private void ShiftTo(int is1, int ibl, int from)
    {
        _ctau[is1, ibl] = _ctau[is1, from]; _thet[is1, ibl] = _thet[is1, from];
        _dstr[is1, ibl] = _dstr[is1, from]; _uedg[is1, ibl] = _uedg[is1, from];
    }
    private void CopyFromStation(int is1, int ibl, int fis, int fibl)
    {
        _ctau[is1, ibl] = _ctau[fis, fibl]; _thet[is1, ibl] = _thet[fis, fibl]; _dstr[is1, ibl] = _dstr[fis, fibl];
    }

    // Port of CLCALC + CDCALC (viscous), producing CL, CM, CD (Squire-Young) and CDf.
    private void ClCdCalc()
    {
        int n = _n;
        double sa = Math.Sin(_alfa), ca = Math.Cos(_alfa);
        double beta = Math.Sqrt(1.0 - _mach * _mach);
        double bfac = 0.5 * _mach * _mach / (1.0 + beta);
        double xref = 0.25, yref = 0.0;

        _cl = 0; _cm = 0;
        double cginc = 1.0 - (_gam[0] / Qinf) * (_gam[0] / Qinf);
        double cpg1 = cginc / (beta + bfac * cginc);
        for (int i = 0; i < n; i++)
        {
            int ip = (i == n - 1) ? 0 : i + 1;
            cginc = 1.0 - (_gam[ip] / Qinf) * (_gam[ip] / Qinf);
            double cpg2 = cginc / (beta + bfac * cginc);
            double dx = (_g.X[ip] - _g.X[i]) * ca + (_g.Y[ip] - _g.Y[i]) * sa;
            double dy = (_g.Y[ip] - _g.Y[i]) * ca - (_g.X[ip] - _g.X[i]) * sa;
            double dg = cpg2 - cpg1;
            double ax = (0.5 * (_g.X[ip] + _g.X[i]) - xref) * ca + (0.5 * (_g.Y[ip] + _g.Y[i]) - yref) * sa;
            double ay = (0.5 * (_g.Y[ip] + _g.Y[i]) - yref) * ca - (0.5 * (_g.X[ip] + _g.X[i]) - xref) * sa;
            double ag = 0.5 * (cpg2 + cpg1);
            _cl += dx * ag;
            _cm += -dx * (ag * ax + dg * dx / 12.0) - dy * (ag * ay + dg * dy / 12.0);
            cpg1 = cpg2;
        }

        // Squire-Young drag from the wake end
        double thwake = _thet[1, _nbl[1]];
        double urat = _uedg[1, _nbl[1]] / Qinf;
        double tklam = _env.Tkbl;
        double uewake = _uedg[1, _nbl[1]] * (1.0 - tklam) / (1.0 - tklam * urat * urat);
        double shwake = _dstr[1, _nbl[1]] / _thet[1, _nbl[1]];
        _cd = 2.0 * thwake * Math.Pow(uewake / Qinf, 0.5 * (5.0 + shwake));

        // friction drag from wall shear
        ComputeTau();
        _cdf = 0.0;
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 3; ibl <= _iblte[is1]; ibl++)
            {
                int i = _ipan[is1, ibl], im = _ipan[is1, ibl - 1];
                double dx = (_g.X[i] - _g.X[im]) * ca + (_g.Y[i] - _g.Y[im]) * sa;
                _cdf += 0.5 * (_tau[is1, ibl] + _tau[is1, ibl - 1]) * dx * 2.0 / (Qinf * Qinf);
            }
    }

    // Recompute wall shear TAU = 0.5*rho*Ue^2*Cf at each airfoil station from the
    // converged BL variables.
    private void ComputeTau()
    {
        _tau = new double[2, _maxbl];
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 2; ibl <= _iblte[is1]; ibl++)
            {
                double uei = _uedg[is1, ibl];
                double dsi = _dstr[is1, ibl], thi = _thet[is1, ibl];
                double ami = ibl < _itran[is1] ? _ctau[is1, ibl] : 0.0;
                double cti = ibl >= _itran[is1] ? _ctau[is1, ibl] : 0.0;
                _itv.Simi = ibl == 2; _itv.Wake = false; _itv.Turb = ibl > _itran[is1]; _itv.Tran = false;
                _itv.Blprv(_xssi[is1, ibl], ami, cti, thi, dsi, 0.0, uei);
                BlSys.Blkin(_itv.S2, _env);
                BlSys.Blvar(_itv.S2, _env, _p, ibl < _itran[is1] ? 1 : 2);
                _tau[is1, ibl] = 0.5 * _itv.S2.R * _itv.S2.U * _itv.S2.U * _itv.S2.Cf;
            }
    }
}
