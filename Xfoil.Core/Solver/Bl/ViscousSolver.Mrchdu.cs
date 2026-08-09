// Port of xbl.f MRCHDU: marches the BLs and wake in mixed mode using the current Ue and
// Hk, following a quasi-normal to the Ue-Hk characteristic so the Goldstein separation
// singularity is avoided, while continuously checking transition onset.

namespace Xfoil.Core.Solver.Bl;

public sealed partial class ViscousSolver
{
    private void Mrchdu()
    {
        const double deps = 5.0e-6, senswt = 1000.0;
        double gm1 = _env.Gm1bl, hstinv = _env.Hstinv;

        for (int is1 = 0; is1 < 2; is1++)
        {
            Xifset(is1);
            _itv.Xiforc = _xiforc;
            _itv.Bule = 1.0;

            int itrold = _itran[is1];
            bool tran = false, turb = false;
            _itran[is1] = _iblte[is1];
            double sens = 0.0, sennew = 0.0;

            for (int ibl = 2; ibl <= _nbl[is1]; ibl++)
            {
                bool simi = ibl == 2;
                bool wake = ibl > _iblte[is1];
                double xsi = _xssi[is1, ibl];
                double uei = _uedg[is1, ibl];
                double thi = _thet[is1, ibl];
                double dsi = _dstr[is1, ibl];
                double ami, cti;
                if (ibl < itrold) { ami = _ctau[is1, ibl]; cti = 0.03; }
                else { cti = _ctau[is1, ibl]; if (cti <= 0.0) cti = 0.03; ami = 0.0; }

                double dswaki = wake ? _wgap[ibl - _iblte[is1] - 1] : 0.0;
                if (ibl <= _iblte[is1]) dsi = Math.Max(dsi - dswaki, 1.02000 * thi) + dswaki;
                else dsi = Math.Max(dsi - dswaki, 1.00005 * thi) + dswaki;

                double dmax = 0;
                for (int itbl = 1; itbl <= 25; itbl++)
                {
                    _itv.Simi = simi; _itv.Wake = wake; _itv.Turb = turb; _itv.Tran = false;
                    _itv.Blprv(xsi, ami, cti, thi, dsi, dswaki, uei);
                    BlSys.Blkin(_itv.S2, _env);

                    if (!simi && !turb)
                    {
                        _itv.Trchek2();
                        tran = _itv.Tran;
                        ami = _itv.S2.Ampl;
                        _itran[is1] = tran ? ibl : ibl + 2;
                    }

                    if (ibl == _iblte[is1] + 1)
                    {
                        double tte = _thet[0, _iblte[0]] + _thet[1, _iblte[1]];
                        double dte = _dstr[0, _iblte[0]] + _dstr[1, _iblte[1]] + _ante;
                        double cte = (_ctau[0, _iblte[0]] * _thet[0, _iblte[0]] + _ctau[1, _iblte[1]] * _thet[1, _iblte[1]]) / tte;
                        _itv.Tran = false; _itv.Wake = true; _itv.Turb = true;
                        _itv.Tesys(cte, tte, dte);
                    }
                    else { _itv.Tran = tran; _itv.Blsys(); }

                    var s2 = _itv.S2;
                    double ueref, hkref;
                    if (itbl == 1)
                    {
                        ueref = s2.U; hkref = s2.Hk;
                        _ueref[is1] = ueref; _hkref[is1] = hkref;
                        if (ibl < _itran[is1] && ibl >= itrold)
                        {
                            double uem = _uedg[is1, ibl - 1], dsm = _dstr[is1, ibl - 1], thm = _thet[is1, ibl - 1];
                            double msq = uem * uem * hstinv / (gm1 * (1.0 - 0.5 * uem * uem * hstinv));
                            BlClosure.Hkin(dsm / thm, msq, out hkref, out _, out _);
                            _hkref[is1] = hkref;
                        }
                        if (ibl < itrold)
                        {
                            if (tran) _ctau[is1, ibl] = 0.03;
                            if (turb) _ctau[is1, ibl] = _ctau[is1, ibl - 1];
                            if (tran || turb) { cti = _ctau[is1, ibl]; s2.S = cti; }
                        }
                    }
                    ueref = _ueref[is1]; hkref = _hkref[is1];

                    if (simi || ibl == _iblte[is1] + 1)
                    {
                        var a = Vs2As4x4();
                        a[3, 0] = 0; a[3, 1] = 0; a[3, 2] = 0; a[3, 3] = s2.U_Uei;
                        var r = new[] { _itv.Vsrez[0], _itv.Vsrez[1], _itv.Vsrez[2], ueref - s2.U };
                        Gauss4(a, r);
                        dmax = ApplyMrchduStep(is1, ibl, r, ref ami, ref cti, ref thi, ref dsi, ref uei);
                    }
                    else
                    {
                        // Ue-Hk characteristic slope
                        var vtmp = Vs2As4x4();
                        var vz = new[] { _itv.Vsrez[0], _itv.Vsrez[1], _itv.Vsrez[2], 1.0 };
                        vtmp[3, 0] = 0; vtmp[3, 1] = s2.Hk_T; vtmp[3, 2] = s2.Hk_D; vtmp[3, 3] = s2.Hk_U * s2.U_Uei;
                        Gauss4(vtmp, vz);
                        sennew = senswt * vz[3] * hkref / ueref;
                        if (itbl <= 5) sens = sennew;
                        else if (itbl <= 15) sens = 0.5 * (sens + sennew);

                        var a = Vs2As4x4();
                        a[3, 0] = 0;
                        a[3, 1] = s2.Hk_T * hkref;
                        a[3, 2] = s2.Hk_D * hkref;
                        a[3, 3] = (s2.Hk_U * hkref + sens / ueref) * s2.U_Uei;
                        var r = new[] { _itv.Vsrez[0], _itv.Vsrez[1], _itv.Vsrez[2],
                            -(hkref * hkref) * (s2.Hk / hkref - 1.0) - sens * (s2.U / ueref - 1.0) };
                        Gauss4(a, r);
                        dmax = ApplyMrchduStep(is1, ibl, r, ref ami, ref cti, ref thi, ref dsi, ref uei);
                    }

                    if (ibl >= _itran[is1]) { cti = Math.Min(cti, 0.30); cti = Math.Max(cti, 1e-7); }
                    double hklim = ibl <= _iblte[is1] ? 1.02 : 1.00005;
                    double msq2 = uei * uei * hstinv / (gm1 * (1.0 - 0.5 * uei * uei * hstinv));
                    double dsw = dsi - dswaki;
                    Dslim(ref dsw, thi, msq2, hklim);
                    dsi = dsw + dswaki;

                    if (dmax <= deps) break;
                }

                sens = sennew;
                StoreStation(is1, ibl, ami, cti, thi, dsi, uei);
                _itv.Blprv(xsi, ami, cti, thi, dsi, dswaki, uei);
                BlSys.Blkin(_itv.S2, _env);
                _itv.S1.CopyFrom(_itv.S2);

                if (tran || ibl == _iblte[is1])
                {
                    turb = true;
                    _tforce[is1] = false;
                    _xssitr[is1] = _itv.Xt;
                }
                tran = false;
            }
        }
    }

    private readonly double[] _ueref = new double[2];
    private readonly double[] _hkref = new double[2];

    private double ApplyMrchduStep(int is1, int ibl, double[] r, ref double ami, ref double cti, ref double thi, ref double dsi, ref double uei)
    {
        double dmax = Math.Max(Math.Max(Math.Abs(r[1] / thi), Math.Abs(r[2] / dsi)), Math.Abs(r[3] / uei));
        if (ibl >= _itran[is1]) dmax = Math.Max(dmax, Math.Abs(r[0] / (10.0 * cti)));
        double rlx = 1.0;
        if (dmax > 0.3) rlx = 0.3 / dmax;
        if (ibl < _itran[is1]) ami += rlx * r[0];
        if (ibl >= _itran[is1]) cti += rlx * r[0];
        thi += rlx * r[1];
        dsi += rlx * r[2];
        uei += rlx * r[3];
        return dmax;
    }
}
