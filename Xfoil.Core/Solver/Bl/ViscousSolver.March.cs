// Port of xbl.f MRCHUE (direct-mode BL initialization march) and MRCHDU (mixed-mode
// march that establishes transition), plus XIFSET (forced-transition location), DSLIM
// (displacement-thickness limiter) and the small 4x4 GAUSS solve MRCHUE/MRCHDU use per
// station. These march the boundary layer downstream, station by station, solving a
// 4x4 Newton system for (dCtau/dAmpl, dTheta, dDstar, dUe) at each one.

namespace Xfoil.Core.Solver.Bl;

public sealed partial class ViscousSolver
{
    private double _xiforc;

    // Port of XIFSET (simplified: forced transition only via XSTRIP; default off).
    private void Xifset(int is1)
    {
        if (_xstrip[is1] >= 1.0) { _xiforc = _xssi[is1, _iblte[is1]]; return; }
        _xiforc = _xssi[is1, _iblte[is1]]; // (forced-trip x/c not used in the standard case)
    }

    // Port of DSLIM: limit Dstar so Hk does not fall below hklim.
    private static void Dslim(ref double dstr, double thet, double msq, double hklim)
    {
        double h = dstr / thet;
        BlClosure.Hkin(h, msq, out double hk, out double hk_h, out _);
        double dh = Math.Max(0.0, hklim - hk) / hk_h;
        dstr += dh * thet;
    }

    private static void Gauss4(double[,] z, double[] r)
    {
        const int nn = 4;
        for (int np = 0; np < nn - 1; np++)
        {
            int nx = np;
            for (int k = np + 1; k < nn; k++) if (Math.Abs(z[k, np]) > Math.Abs(z[nx, np])) nx = k;
            double pivot = 1.0 / z[nx, np];
            z[nx, np] = z[np, np];
            for (int l = np + 1; l < nn; l++) { double t = z[nx, l] * pivot; z[nx, l] = z[np, l]; z[np, l] = t; }
            { double t = r[nx] * pivot; r[nx] = r[np]; r[np] = t; }
            for (int k = np + 1; k < nn; k++)
            {
                double ztmp = z[k, np];
                for (int l = np + 1; l < nn; l++) z[k, l] -= ztmp * z[np, l];
                r[k] -= ztmp * r[np];
            }
        }
        r[nn - 1] /= z[nn - 1, nn - 1];
        for (int np = nn - 2; np >= 0; np--)
            for (int k = np + 1; k < nn; k++) r[np] -= z[np, k] * r[k];
    }

    // Copies VS2 rows 0..3, cols 0..3 into a fresh 4x4 (dropping the dXi column).
    private double[,] Vs2As4x4()
    {
        var a = new double[4, 4];
        for (int k = 0; k < 4; k++) for (int l = 0; l < 4; l++) a[k, l] = _itv.Vs2[k, l];
        return a;
    }

    private void StoreStation(int is1, int ibl, double ami, double cti, double thi, double dsi, double uei)
    {
        _ctau[is1, ibl] = ibl < _itran[is1] ? ami : cti;
        _thet[is1, ibl] = thi;
        _dstr[is1, ibl] = dsi;
        _uedg[is1, ibl] = uei;
        _mass[is1, ibl] = dsi * uei;
    }

    // Port of MRCHUE.
    private void Mrchue()
    {
        const double hlmax = 3.8, htmax = 2.5;
        double gm1 = _env.Gm1bl, hstinv = _env.Hstinv;

        for (int is1 = 0; is1 < 2; is1++)
        {
            Xifset(is1);
            _itv.Xiforc = _xiforc;

            // similarity (Thwaites) initialization at ibl = 2
            double xsi = _xssi[is1, 2], uei = _uedg[is1, 2];
            double bule = 1.0;
            double ucon = uei / Math.Pow(xsi, bule);
            double tsq = 0.45 / (ucon * (5.0 * bule + 1.0) * _env.Reybl) * Math.Pow(xsi, 1.0 - bule);
            double thi = Math.Sqrt(tsq), dsi = 2.2 * thi, ami = 0.0, cti = 0.03;

            bool tran = false, turb = false;
            _itran[is1] = _iblte[is1];
            _itv.Bule = 1.0;

            for (int ibl = 2; ibl <= _nbl[is1]; ibl++)
            {
                int ibm = ibl - 1;
                bool simi = ibl == 2;
                bool wake = ibl > _iblte[is1];
                xsi = _xssi[is1, ibl];
                uei = _uedg[is1, ibl];
                double dswaki = wake ? _wgap[ibl - _iblte[is1] - 1] : 0.0;
                bool direct = true;
                double htarg = 0.0;

                int itbl;
                double dmax = 0;
                for (itbl = 1; itbl <= 25; itbl++)
                {
                    _itv.Simi = simi; _itv.Wake = wake; _itv.Turb = turb; _itv.Tran = false;
                    _itv.Blprv(xsi, ami, cti, thi, dsi, dswaki, uei);
                    BlSys.Blkin(_itv.S2, _env);

                    if (!simi && !turb)
                    {
                        _itv.Trchek2();
                        tran = _itv.Tran;
                        ami = _itv.S2.Ampl;
                        if (tran) { _itran[is1] = ibl; if (cti <= 0.0) { cti = 0.03; _itv.S2.S = cti; } }
                        else _itran[is1] = ibl + 2;
                    }

                    if (ibl == _iblte[is1] + 1)
                    {
                        double tte = _thet[0, _iblte[0]] + _thet[1, _iblte[1]];
                        double dte = _dstr[0, _iblte[0]] + _dstr[1, _iblte[1]] + _ante;
                        double cte = (_ctau[0, _iblte[0]] * _thet[0, _iblte[0]] + _ctau[1, _iblte[1]] * _thet[1, _iblte[1]]) / tte;
                        _itv.Tran = false; _itv.Wake = true; _itv.Turb = true;
                        _itv.Tesys(cte, tte, dte);
                    }
                    else
                    {
                        _itv.Tran = tran;
                        _itv.Blsys();
                    }

                    var s2 = _itv.S2;
                    bool switched = false;
                    if (direct)
                    {
                        var a = Vs2As4x4();
                        a[3, 0] = 0; a[3, 1] = 0; a[3, 2] = 0; a[3, 3] = 1.0;
                        var r = new[] { _itv.Vsrez[0], _itv.Vsrez[1], _itv.Vsrez[2], 0.0 };
                        Gauss4(a, r);

                        dmax = Math.Max(Math.Abs(r[1] / thi), Math.Abs(r[2] / dsi));
                        dmax = ibl < _itran[is1] ? Math.Max(dmax, Math.Abs(r[0] / 10.0)) : Math.Max(dmax, Math.Abs(r[0] / cti));
                        double rlx = 1.0;
                        if (dmax > 0.3) rlx = 0.3 / dmax;

                        if (ibl != _iblte[is1] + 1)
                        {
                            double msq = uei * uei * hstinv / (gm1 * (1.0 - 0.5 * uei * uei * hstinv));
                            double htest = (dsi + rlx * r[2]) / (thi + rlx * r[1]);
                            BlClosure.Hkin(htest, msq, out double hktest, out _, out _);
                            double hmax = ibl < _itran[is1] ? hlmax : htmax;
                            direct = hktest < hmax;
                        }

                        if (direct)
                        {
                            if (ibl >= _itran[is1]) cti += rlx * r[0];
                            thi += rlx * r[1];
                            dsi += rlx * r[2];
                        }
                        else
                        {
                            // switch to inverse mode and retry on the next iteration
                            htarg = InverseHtarg(is1, ibl, wake);
                            switched = true;
                        }
                    }
                    else
                    {
                        var a = Vs2As4x4();
                        a[3, 0] = 0; a[3, 1] = s2.Hk_T; a[3, 2] = s2.Hk_D; a[3, 3] = s2.Hk_U;
                        var r = new[] { _itv.Vsrez[0], _itv.Vsrez[1], _itv.Vsrez[2], htarg - s2.Hk };
                        Gauss4(a, r);
                        dmax = Math.Max(Math.Max(Math.Abs(r[1] / thi), Math.Abs(r[2] / dsi)), Math.Abs(r[3] / uei));
                        if (ibl >= _itran[is1]) dmax = Math.Max(dmax, Math.Abs(r[0] / cti));
                        double rlx = 1.0;
                        if (dmax > 0.3) rlx = 0.3 / dmax;
                        if (ibl >= _itran[is1]) cti += rlx * r[0];
                        thi += rlx * r[1];
                        dsi += rlx * r[2];
                        uei += rlx * r[3];
                    }

                    if (switched) continue; // redo with inverse mode

                    if (ibl >= _itran[is1]) { cti = Math.Min(cti, 0.30); cti = Math.Max(cti, 1e-7); }
                    double hklim = ibl <= _iblte[is1] ? 1.02 : 1.00005;
                    double msq2 = uei * uei * hstinv / (gm1 * (1.0 - 0.5 * uei * uei * hstinv));
                    double dsw = dsi - dswaki;
                    Dslim(ref dsw, thi, msq2, hklim);
                    dsi = dsw + dswaki;

                    if (dmax <= 1.0e-5) break;
                }

                StoreStation(is1, ibl, ami, cti, thi, dsi, uei);

                // set "1" <- "2" for the next station
                _itv.Blprv(xsi, ami, cti, thi, dsi, dswaki, uei);
                BlSys.Blkin(_itv.S2, _env);
                _itv.S1.CopyFrom(_itv.S2);

                if (tran || ibl == _iblte[is1]) { turb = true; _tforce[is1] = false; _xssitr[is1] = _itv.Xt; }
                tran = false;

                if (ibl == _iblte[is1])
                {
                    thi = _thet[0, _iblte[0]] + _thet[1, _iblte[1]];
                    dsi = _dstr[0, _iblte[0]] + _dstr[1, _iblte[1]] + _ante;
                }
            }
        }
    }

    private double InverseHtarg(int is1, int ibl, bool wake)
    {
        var a = _itv.S1; var b = _itv.S2;
        double htarg;
        double hmax = ibl < _itran[is1] ? 3.8 : 2.5;
        if (ibl < _itran[is1]) htarg = a.Hk + 0.03 * (b.X - a.X) / a.T;
        else if (ibl == _itran[is1]) htarg = a.Hk + (0.03 * (_itv.Xt - a.X) - 0.15 * (b.X - _itv.Xt)) / a.T;
        else if (wake)
        {
            double konst = 0.03 * (b.X - a.X) / a.T;
            double hk2 = a.Hk;
            for (int q = 0; q < 3; q++)
                hk2 -= (hk2 + konst * Math.Pow(hk2 - 1.0, 3) - a.Hk) / (1.0 + 3.0 * konst * Math.Pow(hk2 - 1.0, 2));
            htarg = hk2;
        }
        else htarg = a.Hk - 0.15 * (b.X - a.X) / a.T;
        htarg = wake ? Math.Max(htarg, 1.01) : Math.Max(htarg, hmax);
        return htarg;
    }
}
