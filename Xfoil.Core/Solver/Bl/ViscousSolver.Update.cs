// Port of xbl.f UPDATE and the small velocity-mapping routines QVFUE/GAMQV. UPDATE adds
// the Newton deltas (from BlSolve) to the BL variables, recomputes the edge velocity from
// the updated mass defect, drives CL to consistency (the global variable AC), applies
// under-relaxation, and reports the rms change. Simplified for fixed-alpha/fixed-Re.

namespace Xfoil.Core.Solver.Bl;

public sealed partial class ViscousSolver
{
    // Port of UPDATE. Returns the rms BL change.
    private double Update()
    {
        int n = _n;
        double gm1 = _env.Gm1bl;
        double hstinv = gm1 * (_mach / Qinf) * (_mach / Qinf) / (1.0 + 0.5 * gm1 * _mach * _mach);

        // new Ue distribution from updated mass defect (no under-relaxation yet)
        var unew = new double[2, _maxbl];
        var qnew = new double[n];
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 2; ibl <= _nbl[is1]; ibl++)
            {
                int i = _ipan[is1, ibl];
                double dui = 0.0;
                for (int js = 0; js < 2; js++)
                    for (int jbl = 2; jbl <= _nbl[js]; jbl++)
                    {
                        int j = _ipan[js, jbl], jv = _isys[js, jbl];
                        double ueM = -_vti[is1, ibl] * _vti[js, jbl] * _dij[i, j];
                        dui += ueM * (_mass[js, jbl] + _vdel[2, 0, jv]);
                    }
                unew[is1, ibl] = _uinv[is1, ibl] + dui;
            }
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 2; ibl <= _iblte[is1]; ibl++)
                qnew[_ipan[is1, ibl]] = _vti[is1, ibl] * unew[is1, ibl];

        // new CL from the new surface speed (Mach 0: Cp = 1 - q^2)
        double sa = Math.Sin(_alfa), ca = Math.Cos(_alfa);
        double beta = Math.Sqrt(1.0 - _mach * _mach);
        double bfac = 0.5 * _mach * _mach / (1.0 + beta);
        double clnew = 0.0;
        double cginc = 1.0 - (qnew[0] / Qinf) * (qnew[0] / Qinf);
        double cpg1 = cginc / (beta + bfac * cginc);
        for (int i = 0; i < n; i++)
        {
            int ip = (i == n - 1) ? 0 : i + 1;
            cginc = 1.0 - (qnew[ip] / Qinf) * (qnew[ip] / Qinf);
            double cpg2 = cginc / (beta + bfac * cginc);
            double dx = (_g.X[ip] - _g.X[i]) * ca + (_g.Y[ip] - _g.Y[i]) * sa;
            double ag = 0.5 * (cpg2 + cpg1);
            clnew += dx * ag;
            cpg1 = cpg2;
        }

        double dac = clnew - _cl; // MATYP=1: CL_AC = CL_MS = 0
        double rlx = 1.0;
        const double dclmax = 0.5, dclmin = -0.5;
        if (rlx * dac > dclmax) rlx = dclmax / dac;
        if (rlx * dac < dclmin) rlx = dclmin / dac;

        const double dhi = 1.5, dlo = -0.5;
        double rmsbl = 0.0;
        double rmxbl = 0.0; _imxbl = 0; _ismxbl = 0; _vmxbl = '?';
        // first pass: accumulate rms and set under-relaxation
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 2; ibl <= _nbl[is1]; ibl++)
            {
                int iv = _isys[is1, ibl];
                double dctau = _vdel[0, 0, iv];
                double dthet = _vdel[1, 0, iv];
                double dmass = _vdel[2, 0, iv];
                double duedg = unew[is1, ibl] - _uedg[is1, ibl];
                double ddstr = (dmass - _dstr[is1, ibl] * duedg) / _uedg[is1, ibl];
                double dn1 = ibl < _itran[is1] ? dctau / 10.0 : dctau / _ctau[is1, ibl];
                double dn2 = dthet / _thet[is1, ibl];
                double dn3 = ddstr / _dstr[is1, ibl];
                double dn4 = Math.Abs(duedg) / 0.25;
                rmsbl += dn1 * dn1 + dn2 * dn2 + dn3 * dn3 + dn4 * dn4;
                if (Math.Abs(dn1) > Math.Abs(rmxbl)) { rmxbl = dn1; _vmxbl = ibl < _itran[is1] ? 'n' : 'C'; _imxbl = ibl; _ismxbl = is1; }
                if (Math.Abs(dn2) > Math.Abs(rmxbl)) { rmxbl = dn2; _vmxbl = 'T'; _imxbl = ibl; _ismxbl = is1; }
                if (Math.Abs(dn3) > Math.Abs(rmxbl)) { rmxbl = dn3; _vmxbl = 'D'; _imxbl = ibl; _ismxbl = is1; }
                if (Math.Abs(dn4) > Math.Abs(rmxbl)) { rmxbl = dn4; _vmxbl = 'U'; _imxbl = ibl; _ismxbl = is1; }
                if (rlx * dn1 > dhi) rlx = dhi / dn1;
                if (rlx * dn1 < dlo) rlx = dlo / dn1;
                if (rlx * dn2 > dhi) rlx = dhi / dn2;
                if (rlx * dn2 < dlo) rlx = dlo / dn2;
                if (rlx * dn3 > dhi) rlx = dhi / dn3;
                if (rlx * dn3 < dlo) rlx = dlo / dn3;
                if (rlx * dn4 > dhi) rlx = dhi / dn4;
                if (rlx * dn4 < dlo) rlx = dlo / dn4;
            }
        rmsbl = Math.Sqrt(rmsbl / (4.0 * (_nbl[0] + _nbl[1])));
        _lastRmx = rmxbl;
        _lastRlx = rlx;

        _cl += rlx * dac;

        // second pass: apply under-relaxed changes
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 2; ibl <= _nbl[is1]; ibl++)
            {
                int iv = _isys[is1, ibl];
                double dctau = _vdel[0, 0, iv];
                double dthet = _vdel[1, 0, iv];
                double dmass = _vdel[2, 0, iv];
                double duedg = unew[is1, ibl] - _uedg[is1, ibl];
                double ddstr = (dmass - _dstr[is1, ibl] * duedg) / _uedg[is1, ibl];
                _ctau[is1, ibl] += rlx * dctau;
                _thet[is1, ibl] += rlx * dthet;
                _dstr[is1, ibl] += rlx * ddstr;
                _uedg[is1, ibl] += rlx * duedg;

                double dswaki = ibl > _iblte[is1] ? _wgap[ibl - _iblte[is1] - 1] : 0.0;
                if (ibl >= _itran[is1]) _ctau[is1, ibl] = Math.Min(_ctau[is1, ibl], 0.25);
                double hklim = ibl <= _iblte[is1] ? 1.02 : 1.00005;
                double msq = _uedg[is1, ibl] * _uedg[is1, ibl] * hstinv / (gm1 * (1.0 - 0.5 * _uedg[is1, ibl] * _uedg[is1, ibl] * hstinv));
                double dsw = _dstr[is1, ibl] - dswaki;
                Dslim(ref dsw, _thet[is1, ibl], msq, hklim);
                _dstr[is1, ibl] = dsw + dswaki;
                _mass[is1, ibl] = _dstr[is1, ibl] * _uedg[is1, ibl];
            }

        // equate upper wake to lower wake
        for (int kbl = 1; kbl <= _nbl[1] - _iblte[1]; kbl++)
        {
            _ctau[0, _iblte[0] + kbl] = _ctau[1, _iblte[1] + kbl];
            _thet[0, _iblte[0] + kbl] = _thet[1, _iblte[1] + kbl];
            _dstr[0, _iblte[0] + kbl] = _dstr[1, _iblte[1] + kbl];
            _uedg[0, _iblte[0] + kbl] = _uedg[1, _iblte[1] + kbl];
        }
        return rmsbl;
    }

    private int _imxbl, _ismxbl;
    private char _vmxbl;
    private double _lastRmx;
    /// <summary>Max BL change (value, ibl, side, variable) from the last UPDATE (diagnostic).</summary>
    public (double rmax, int ibl, int iside, char var) MaxChange => (_lastRmx, _imxbl, _ismxbl, _vmxbl);

    // Port of QVFUE: panel viscous tangential velocity from viscous Ue.
    private void Qvfue()
    {
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 2; ibl <= _nbl[is1]; ibl++)
                _qvis[_ipan[is1, ibl]] = _vti[is1, ibl] * _uedg[is1, ibl];
    }

    // Port of GAMQV: airfoil vorticity from the viscous surface speed.
    private void Gamqv()
    {
        for (int i = 0; i < _n; i++) _gam[i] = _qvis[i];
    }
}
