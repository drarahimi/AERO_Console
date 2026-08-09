// Port of xpanel.f QDCALC (source-panel influence matrix DIJ) and PSWLIN (wake
// source-panel influence). DIJ(i,j) is the tangential velocity induced at node i by a
// unit mass-defect source at node j, accounting for the airfoil vorticity that responds
// to keep the flow tangent -- the dense coupling that ties every BL station's edge
// velocity to the mass defect everywhere.

namespace Xfoil.Core.Solver.Bl;

public sealed partial class ViscousSolver
{
    private void Qdcalc()
    {
        int n = _n, m = n + 1;
        _dij = new double[_nt, _nt];

        // ---- airfoil sources: back-substitute each BIJ column through factored AIJ
        var bij = _inv.Bij; // [m][n], bij[i][j] = -dPsi/dSig_j at node i
        for (int j = 0; j < n; j++)
        {
            var col = new double[m];
            for (int i = 0; i < m; i++) col[i] = bij[i][j];
            _inv.SolveAij(col);
            for (int i = 0; i < n; i++) _dij[i, j] = col[i];
        }

        // ---- wake sources on airfoil nodes
        var dzdmW = new double[_nt];
        var dqdmW = new double[_nt];
        var bijw = new double[m][];
        for (int i = 0; i < m; i++) bijw[i] = new double[_nw];
        for (int i = 0; i < n; i++)
        {
            Pswlin(i + 1, _xg[i], _yg[i], _nxg[i], _nyg[i], dzdmW, dqdmW);
            for (int k = 0; k < _nw; k++) bijw[i][k] = -dzdmW[n + k];
        }
        if (_g.Sharp) for (int k = 0; k < _nw; k++) bijw[n - 1][k] = 0.0;
        for (int k = 0; k < _nw; k++)
        {
            var col = new double[m];
            for (int i = 0; i < m; i++) col[i] = bijw[i][k];
            _inv.SolveAij(col);
            for (int i = 0; i < n; i++) _dij[i, n + k] = col[i];
            for (int i = 0; i < m; i++) bijw[i][k] = col[i]; // keep for the wake-velocity assembly
        }

        // ---- wake velocities
        var cij = new double[_nw][];
        for (int iw = 0; iw < _nw; iw++) cij[iw] = new double[n];
        var dzdg = new double[n]; var dzdm = new double[n]; var dqdg = new double[n]; var dqdm = new double[n];
        var zero = new double[n];
        double cosa = Math.Cos(_alfa), sina = Math.Sin(_alfa);
        for (int i = n; i < _nt; i++)
        {
            int iw = i - n;
            // airfoil vorticity + source contribution at the wake node
            Psi.PsilinFull(_g, zero, zero, cosa, sina, Qinf, i + 1, _xg[i], _yg[i], _nxg[i], _nyg[i], dzdg, dzdm, dqdg, dqdm);
            for (int j = 0; j < n; j++) { cij[iw][j] = dqdg[j]; _dij[i, j] = dqdm[j]; }
            // wake source contribution
            Pswlin(i + 1, _xg[i], _yg[i], _nxg[i], _nyg[i], dzdmW, dqdmW);
            for (int k = 0; k < _nw; k++) _dij[i, n + k] = dqdmW[n + k];
        }

        // ---- add the effect of all sources acting through the airfoil vorticity
        for (int i = n; i < _nt; i++)
        {
            int iw = i - n;
            for (int j = 0; j < n; j++)
            {
                double sum = 0.0;
                for (int kk = 0; kk < n; kk++) sum += cij[iw][kk] * _dij[kk, j];
                _dij[i, j] += sum;
            }
            for (int k = 0; k < _nw; k++)
            {
                double sum = 0.0;
                for (int kk = 0; kk < n; kk++) sum += cij[iw][kk] * bijw[kk][k];
                _dij[i, n + k] += sum;
            }
        }

        // first wake point takes the TE velocity
        for (int j = 0; j < _nt; j++) _dij[n, j] = _dij[n - 1, j];
    }

    // Port of PSWLIN: dPsi/dSig (dzdm) and dQtan/dSig (dqdm) at (xi,yi) due to the wake
    // source panels, filling indices n..nt-1. io is the 1-based node index.
    private void Pswlin(int io, double xi, double yi, double nxi, double nyi, double[] dzdm, double[] dqdm)
    {
        int n = _n;
        double qopi = 0.25 / Pi;
        for (int j = n; j < _nt; j++) { dzdm[j] = 0.0; dqdm[j] = 0.0; }

        for (int jo = n; jo <= _nt - 2; jo++) // Fortran N+1..N+NW-1
        {
            int jp = jo + 1;
            int jm = (jo == n) ? jo : jo - 1;
            int jq = (jo == _nt - 2) ? jp : jp + 1;

            double dso = Math.Sqrt((_xg[jo] - _xg[jp]) * (_xg[jo] - _xg[jp]) + (_yg[jo] - _yg[jp]) * (_yg[jo] - _yg[jp]));
            double dsio = 1.0 / dso;
            double apan = _apg[jo];

            double rx1 = xi - _xg[jo], ry1 = yi - _yg[jo], rx2 = xi - _xg[jp], ry2 = yi - _yg[jp];
            double sx = (_xg[jp] - _xg[jo]) * dsio, sy = (_yg[jp] - _yg[jo]) * dsio;
            double x1 = sx * rx1 + sy * ry1, x2 = sx * rx2 + sy * ry2, yy = sx * ry1 - sy * rx1;
            double rs1 = rx1 * rx1 + ry1 * ry1, rs2 = rx2 * rx2 + ry2 * ry2;

            double sgn = (io >= n + 1 && io <= _nt) ? 1.0 : (yy < 0.0 ? -1.0 : 1.0);
            int jo1 = jo + 1, jp1 = jp + 1;
            double g1, t1, g2, t2;
            if (io != jo1 && rs1 > 0.0) { g1 = Math.Log(rs1); t1 = Math.Atan2(sgn * x1, sgn * yy) - (0.5 - 0.5 * sgn) * Pi; } else { g1 = 0.0; t1 = 0.0; }
            if (io != jp1 && rs2 > 0.0) { g2 = Math.Log(rs2); t2 = Math.Atan2(sgn * x2, sgn * yy) - (0.5 - 0.5 * sgn) * Pi; } else { g2 = 0.0; t2 = 0.0; }

            double x1i = sx * nxi + sy * nyi, x2i = sx * nxi + sy * nyi, yyi = sx * nyi - sy * nxi;

            double x0 = 0.5 * (x1 + x2);
            double rs0 = x0 * x0 + yy * yy;
            double g0 = Math.Log(rs0);
            double t0 = Math.Atan2(sgn * x0, sgn * yy) - (0.5 - 0.5 * sgn) * Pi;

            // 1-0 half panel
            double dxinv = 1.0 / (x1 - x0);
            double psum = x0 * (t0 - apan) - x1 * (t1 - apan) + 0.5 * yy * (g1 - g0);
            double pdif = ((x1 + x0) * psum + rs1 * (t1 - apan) - rs0 * (t0 - apan) + (x0 - x1) * yy) * dxinv;
            double psx1 = -(t1 - apan), psx0 = t0 - apan, psyy = 0.5 * (g1 - g0);
            double pdx1 = ((x1 + x0) * psx1 + psum + 2.0 * x1 * (t1 - apan) - pdif) * dxinv;
            double pdx0 = ((x1 + x0) * psx0 + psum - 2.0 * x0 * (t0 - apan) + pdif) * dxinv;
            double pdyy = ((x1 + x0) * psyy + 2.0 * (x0 - x1 + yy * (t1 - t0))) * dxinv;
            double dsm = Math.Sqrt((_xg[jp] - _xg[jm]) * (_xg[jp] - _xg[jm]) + (_yg[jp] - _yg[jm]) * (_yg[jp] - _yg[jm]));
            double dsim = 1.0 / dsm;
            dzdm[jm] += qopi * (-psum * dsim + pdif * dsim);
            dzdm[jo] += qopi * (-psum * dsio - pdif * dsio);
            dzdm[jp] += qopi * (psum * (dsio + dsim) + pdif * (dsio - dsim));
            double psni = psx1 * x1i + psx0 * (x1i + x2i) * 0.5 + psyy * yyi;
            double pdni = pdx1 * x1i + pdx0 * (x1i + x2i) * 0.5 + pdyy * yyi;
            dqdm[jm] += qopi * (-psni * dsim + pdni * dsim);
            dqdm[jo] += qopi * (-psni * dsio - pdni * dsio);
            dqdm[jp] += qopi * (psni * (dsio + dsim) + pdni * (dsio - dsim));

            // 0-2 half panel
            dxinv = 1.0 / (x0 - x2);
            psum = x2 * (t2 - apan) - x0 * (t0 - apan) + 0.5 * yy * (g0 - g2);
            pdif = ((x0 + x2) * psum + rs0 * (t0 - apan) - rs2 * (t2 - apan) + (x2 - x0) * yy) * dxinv;
            psx0 = -(t0 - apan); double psx2 = t2 - apan; psyy = 0.5 * (g0 - g2);
            pdx0 = ((x0 + x2) * psx0 + psum + 2.0 * x0 * (t0 - apan) - pdif) * dxinv;
            double pdx2 = ((x0 + x2) * psx2 + psum - 2.0 * x2 * (t2 - apan) + pdif) * dxinv;
            pdyy = ((x0 + x2) * psyy + 2.0 * (x2 - x0 + yy * (t0 - t2))) * dxinv;
            double dsp = Math.Sqrt((_xg[jq] - _xg[jo]) * (_xg[jq] - _xg[jo]) + (_yg[jq] - _yg[jo]) * (_yg[jq] - _yg[jo]));
            double dsip = 1.0 / dsp;
            dzdm[jo] += qopi * (-psum * (dsip + dsio) - pdif * (dsip - dsio));
            dzdm[jp] += qopi * (psum * dsio - pdif * dsio);
            dzdm[jq] += qopi * (psum * dsip + pdif * dsip);
            psni = psx0 * (x1i + x2i) * 0.5 + psx2 * x2i + psyy * yyi;
            pdni = pdx0 * (x1i + x2i) * 0.5 + pdx2 * x2i + pdyy * yyi;
            dqdm[jo] += qopi * (-psni * (dsip + dsio) - pdni * (dsip - dsio));
            dqdm[jp] += qopi * (psni * dsio - pdni * dsio);
            dqdm[jq] += qopi * (psni * dsip + pdni * dsip);
        }
    }
}
