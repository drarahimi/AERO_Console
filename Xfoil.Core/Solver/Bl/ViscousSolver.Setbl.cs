// Port of xbl.f SETBL: fills the global viscous Newton system (VM/VA/VB/VZ/VDEL) by
// sweeping each BL station, coupling the per-station BL equations (BlInterval) to the
// mass-defect->edge-velocity influence DIJ. Simplified for the fixed-alpha, fixed-Re
// (MATYP=1) case: the second (Re-influence) RHS vanishes.

using Xfoil.Core.Geometry;

namespace Xfoil.Core.Solver.Bl;

public sealed partial class ViscousSolver
{
    // work vectors, length nsys
    private double[] _u1m = null!, _d1m = null!, _u2m = null!, _d2m = null!;
    private double[] _ule1m = null!, _ule2m = null!, _ute1m = null!, _ute2m = null!;

    private void Setbl()
    {
        if (!_blini) { Trace?.Invoke("\n Initializing BL ...\n"); Mrchue(); _blini = true; }
        Mrchdu();

        // save current Ue, recompute Ue = Uinv + DIJ*mass, then swap so USAV holds the
        // recomputed value and UEDG the marched value (mismatch drives the Newton forcing)
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 2; ibl <= _nbl[is1]; ibl++) _usav[is1, ibl] = _uedg[is1, ibl];
        Ueset();
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 2; ibl <= _nbl[is1]; ibl++)
            { double t = _usav[is1, ibl]; _usav[is1, ibl] = _uedg[is1, ibl]; _uedg[is1, ibl] = t; }

        int n = _nsys;
        _u1m = new double[n]; _d1m = new double[n]; _u2m = new double[n]; _d2m = new double[n];
        _ule1m = new double[n]; _ule2m = new double[n]; _ute1m = new double[n]; _ute2m = new double[n];

        int ile1 = _ipan[0, 2], ile2 = _ipan[1, 2];
        int ite1 = _ipan[0, _iblte[0]], ite2 = _ipan[1, _iblte[1]];

        for (int js = 0; js < 2; js++)
            for (int jbl = 2; jbl <= _nbl[js]; jbl++)
            {
                int j = _ipan[js, jbl], jv = _isys[js, jbl];
                _ule1m[jv] = -_vti[0, 2] * _vti[js, jbl] * _dij[ile1, j];
                _ule2m[jv] = -_vti[1, 2] * _vti[js, jbl] * _dij[ile2, j];
                _ute1m[jv] = -_vti[0, _iblte[0]] * _vti[js, jbl] * _dij[ite1, j];
                _ute2m[jv] = -_vti[1, _iblte[1]] * _vti[js, jbl] * _dij[ite2, j];
            }
        // LE Ue mismatch (drives the XI-sensitivity forcing terms)
        _dule1 = _uedg[0, 2] - _usav[0, 2];
        _dule2 = _uedg[1, 2] - _usav[1, 2];

        // clear the Newton system
        Array.Clear(_vm, 0, _vm.Length); Array.Clear(_va, 0, _va.Length);
        Array.Clear(_vb, 0, _vb.Length); Array.Clear(_vdel, 0, _vdel.Length); Array.Clear(_vz, 0, _vz.Length);

        double gm1 = _env.Gm1bl, hstinv = _env.Hstinv;

        for (int is1 = 0; is1 < 2; is1++)
        {
            for (int jv = 0; jv < n; jv++) { _u1m[jv] = 0; _d1m[jv] = 0; }
            double due1 = 0, dds1 = 0;
            _itv.Bule = 1.0;
            Xifset(is1); _itv.Xiforc = _xiforc;
            bool tran = false, turb = false;

            for (int ibl = 2; ibl <= _nbl[is1]; ibl++)
            {
                int iv = _isys[is1, ibl];
                bool simi = ibl == 2;
                bool wake = ibl > _iblte[is1];
                tran = ibl == _itran[is1];
                turb = ibl > _itran[is1];
                int i = _ipan[is1, ibl];

                double xsi = _xssi[is1, ibl];
                double ami = ibl < _itran[is1] ? _ctau[is1, ibl] : 0.0;
                double cti = ibl >= _itran[is1] ? _ctau[is1, ibl] : 0.0;
                double uei = _uedg[is1, ibl];
                double thi = _thet[is1, ibl];
                double mdi = _mass[is1, ibl];
                double dsi = mdi / uei;
                double dswaki = wake ? _wgap[ibl - _iblte[is1] - 1] : 0.0;

                double d2m2 = 1.0 / uei, d2u2 = -dsi / uei;
                for (int js = 0; js < 2; js++)
                    for (int jbl = 2; jbl <= _nbl[js]; jbl++)
                    {
                        int j = _ipan[js, jbl], jv = _isys[js, jbl];
                        _u2m[jv] = -_vti[is1, ibl] * _vti[js, jbl] * _dij[i, j];
                        _d2m[jv] = d2u2 * _u2m[jv];
                    }
                _d2m[iv] += d2m2;
                double due2 = _uedg[is1, ibl] - _usav[is1, ibl];
                double dds2 = d2u2 * due2;

                _itv.Simi = simi; _itv.Wake = wake; _itv.Turb = turb; _itv.Tran = tran;
                _itv.Blprv(xsi, ami, cti, thi, dsi, dswaki, uei);
                BlSys.Blkin(_itv.S2, _env);
                if (tran) { _itv.Trchek2(); ami = _itv.S2.Ampl; }

                if (ibl == _iblte[is1] + 1)
                {
                    double tte = _thet[0, _iblte[0]] + _thet[1, _iblte[1]];
                    double dte = _dstr[0, _iblte[0]] + _dstr[1, _iblte[1]] + _ante;
                    double cte = (_ctau[0, _iblte[0]] * _thet[0, _iblte[0]] + _ctau[1, _iblte[1]] * _thet[1, _iblte[1]]) / tte;
                    _itv.Wake = true; _itv.Turb = true;
                    _itv.Tesys(cte, tte, dte);

                    double dteMte1 = 1.0 / _uedg[0, _iblte[0]], dteUte1 = -_dstr[0, _iblte[0]] / _uedg[0, _iblte[0]];
                    double dteMte2 = 1.0 / _uedg[1, _iblte[1]], dteUte2 = -_dstr[1, _iblte[1]] / _uedg[1, _iblte[1]];
                    int jvte1 = _isys[0, _iblte[0]], jvte2 = _isys[1, _iblte[1]];
                    for (int jv = 0; jv < n; jv++) _d1m[jv] = dteUte1 * _ute1m[jv] + dteUte2 * _ute2m[jv];
                    _d1m[jvte1] += dteMte1; _d1m[jvte2] += dteMte2;
                    due1 = 0.0;
                    dds1 = dteUte1 * (_uedg[0, _iblte[0]] - _usav[0, _iblte[0]])
                         + dteUte2 * (_uedg[1, _iblte[1]] - _usav[1, _iblte[1]]);
                }
                else
                {
                    _itv.Blsys();
                }

                double xiUle1 = is1 == 0 ? _sstGo : -_sstGo;
                double xiUle2 = is1 == 0 ? -_sstGp : _sstGp;

                for (int k = 0; k < 3; k++)
                {
                    double vs1d = _itv.Vs1[k, 2], vs1u = _itv.Vs1[k, 3], vs1x = _itv.Vs1[k, 4];
                    double vs2d = _itv.Vs2[k, 2], vs2u = _itv.Vs2[k, 3], vs2x = _itv.Vs2[k, 4];
                    double vsx = _itv.Vsx[k];
                    double xsum = vs1x + vs2x + vsx;
                    for (int jv = 0; jv < n; jv++)
                        _vm[k, jv, iv] = vs1d * _d1m[jv] + vs1u * _u1m[jv] + vs2d * _d2m[jv] + vs2u * _u2m[jv]
                            + xsum * (xiUle1 * _ule1m[jv] + xiUle2 * _ule2m[jv]);
                    _vb[k, 0, iv] = _itv.Vs1[k, 0];
                    _vb[k, 1, iv] = _itv.Vs1[k, 1];
                    _va[k, 0, iv] = _itv.Vs2[k, 0];
                    _va[k, 1, iv] = _itv.Vs2[k, 1];
                    _vdel[k, 1, iv] = 0.0; // MATYP=1: no Re-influence RHS
                    _vdel[k, 0, iv] = _itv.Vsrez[k]
                        + (vs1u * due1 + vs1d * dds1) + (vs2u * due2 + vs2d * dds2)
                        + xsum * (xiUle1 * _dule1 + xiUle2 * _dule2);
                }

                if (ibl == _iblte[is1] + 1)
                {
                    double tte = _thet[0, _iblte[0]] + _thet[1, _iblte[1]];
                    double cteCte1 = _thet[0, _iblte[0]] / tte, cteCte2 = _thet[1, _iblte[1]] / tte;
                    double cteTte1 = (_ctau[0, _iblte[0]] - CteVal(tte)) / tte, cteTte2 = (_ctau[1, _iblte[1]] - CteVal(tte)) / tte;
                    for (int k = 0; k < 3; k++)
                    {
                        _vz[k, 0] = _itv.Vs1[k, 0] * cteCte1;
                        _vz[k, 1] = _itv.Vs1[k, 0] * cteTte1 + _itv.Vs1[k, 1] * 1.0;
                        _vb[k, 0, iv] = _itv.Vs1[k, 0] * cteCte2;
                        _vb[k, 1, iv] = _itv.Vs1[k, 0] * cteTte2 + _itv.Vs1[k, 1] * 1.0;
                    }
                }

                if (tran)
                {
                    _itran[is1] = ibl; _tforce[is1] = _itv.Trforc; _xssitr[is1] = _itv.Xt;
                    double str = is1 == 0 ? _sst - _itv.Xt : _sst + _itv.Xt;
                    double chx = _xte - _xle, chy = _yte - _yle, chsq = chx * chx + chy * chy;
                    double xtr = Spline.Seval(str, _g.X, _g.Xp, _g.S, _n);
                    double ytr = Spline.Seval(str, _g.Y, _g.Yp, _g.S, _n);
                    _xoctr[is1] = ((xtr - _xle) * chx + (ytr - _yle) * chy) / chsq;
                }

                if (ibl == _iblte[is1]) { _itv.Turb = true; _itv.Wake = true; BlSys.Blvar(_itv.S2, _env, _p, 3); Blmid3(); }

                for (int jv = 0; jv < n; jv++) { _u1m[jv] = _u2m[jv]; _d1m[jv] = _d2m[jv]; }
                due1 = due2; dds1 = dds2;
                _itv.S1.CopyFrom(_itv.S2);
            }
        }
    }

    private double _dule1, _dule2;

    // CTE value used for the wake-start Ctau coefficients.
    private double CteVal(double tte) => (_ctau[0, _iblte[0]] * _thet[0, _iblte[0]] + _ctau[1, _iblte[1]] * _thet[1, _iblte[1]]) / tte;

    private void Blmid3() => _itv.Blmid(3);

    private void Ueset()
    {
        for (int is1 = 0; is1 < 2; is1++)
            for (int ibl = 2; ibl <= _nbl[is1]; ibl++)
            {
                int i = _ipan[is1, ibl];
                double dui = 0.0;
                for (int js = 0; js < 2; js++)
                    for (int jbl = 2; jbl <= _nbl[js]; jbl++)
                    {
                        int j = _ipan[js, jbl];
                        dui += -_vti[is1, ibl] * _vti[js, jbl] * _dij[i, j] * _mass[js, jbl];
                    }
                _uedg[is1, ibl] = _uinv[is1, ibl] + dui;
            }
    }

    // Solve the assembled Newton system with the block solver.
    private void SolveNewton()
    {
        int ivte1 = _isys[0, _iblte[0]];
        int ivz = _isys[1, _iblte[1] + 1];
        double sn = _sg[_n - 1] - _sg[0];
        double vacc1 = 0.01, vacc2 = 0.01 * 2.0 / sn, vacc3 = 0.01 * 2.0 / sn;
        new BlSolve(_nsys, _va, _vb, _vz, _vm, _vdel, ivte1, ivz, vacc1, vacc2, vacc3).Solve();
    }
}
