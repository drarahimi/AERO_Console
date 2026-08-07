// Port of amass.f SUBROUTINE MASGET: parses a .mass file into total mass,
// center of gravity, and inertia tensor.
//
// Port of packages/avl-core/src/massfile/massFileParser.ts.

using System.Globalization;
using System.Text.RegularExpressions;
using Avl.Core.Model;

namespace Avl.Core.MassFile;

public sealed class MassProperties
{
    public double Mass;
    public double[] Cg = new double[3];
    /// <summary>Inertia tensor about the CG, [[Ixx,-Ixy,-Ixz],[-Ixy,Iyy,-Iyz],[-Ixz,-Iyz,Izz]].</summary>
    public double[][] Inertia = { new double[3], new double[3], new double[3] };
    public double G;
    public double Rho;
    public double UnitL;
    public double UnitM;
    public double UnitT;
}

public static class MassFileParser
{
    public static MassProperties ParseMassFile(string contents)
    {
        var fac = new double[10];
        for (int i = 0; i < 10; i++) fac[i] = 1.0;
        var add = new double[10];
        double unitL = 1, unitM = 1, unitT = 1;
        double g = 1, rho = 1;

        double sumM = 0, sumMx = 0, sumMy = 0, sumMz = 0;
        double sumMxx = 0, sumMyy = 0, sumMzz = 0, sumMxy = 0, sumMxz = 0, sumMyz = 0;
        double sumIxx = 0, sumIyy = 0, sumIzz = 0, sumIxy = 0, sumIxz = 0, sumIyz = 0;

        var lines = Regex.Split(contents, "\r\n|\r|\n");
        foreach (var rawLine in lines)
        {
            if (rawLine.Length == 0) continue;
            if (rawLine[0] == '#' || rawLine[0] == '!') continue;

            if (rawLine[0] == '*')
            {
                var vals = FortranText.GetFlt(rawLine.Substring(1), 10);
                for (int k = 0; k < vals.Count; k++) fac[k] = vals[k];
                continue;
            }
            if (rawLine[0] == '+')
            {
                var vals = FortranText.GetFlt(rawLine.Substring(1), 10);
                for (int k = 0; k < vals.Count; k++) add[k] = vals[k];
                continue;
            }

            int keq = rawLine.IndexOf('=');
            if (keq > 1)
            {
                string key = rawLine.Substring(0, keq);
                string rest = rawLine.Substring(keq + 1).Trim();
                string numTok = Regex.Split(rest, @"\s+")[0];
                double val = double.TryParse(numTok, NumberStyles.Float, CultureInfo.InvariantCulture, out var pv) ? pv : 0;
                if (key.Contains("Lunit")) { unitL = val; continue; }
                if (key.Contains("Munit")) { unitM = val; continue; }
                if (key.Contains("Tunit")) { unitT = val; continue; }
                if (key.Contains("g")) { g = val; continue; }
                if (key.Contains("rho")) { rho = val; continue; }
            }

            // data row: mass x y z Ixx Iyy Izz [Ixy Ixz Iyz]
            List<double> rinp;
            try
            {
                rinp = FortranText.GetFlt(rawLine, 10);
            }
            catch
            {
                continue; // matches MASGET's "bad data line ... ignored"
            }
            if (rinp.Count == 0) continue;
            double Get(int i) => i < rinp.Count ? rinp[i] : 0;
            double mi = fac[0] * Get(0) + add[0];
            double xi = fac[1] * Get(1) + add[1];
            double yi = fac[2] * Get(2) + add[2];
            double zi = fac[3] * Get(3) + add[3];
            double ixxi = fac[4] * Get(4) + add[4];
            double iyyi = fac[5] * Get(5) + add[5];
            double izzi = fac[6] * Get(6) + add[6];
            double ixyi = fac[7] * Get(7) + add[7];
            double ixzi = fac[8] * Get(8) + add[8];
            double iyzi = fac[9] * Get(9) + add[9];

            sumM += mi;
            sumMx += mi * xi;
            sumMy += mi * yi;
            sumMz += mi * zi;
            sumMxx += mi * xi * xi;
            sumMyy += mi * yi * yi;
            sumMzz += mi * zi * zi;
            sumMxy += mi * xi * yi;
            sumMxz += mi * xi * zi;
            sumMyz += mi * yi * zi;

            sumIxx += ixxi;
            sumIyy += iyyi;
            sumIzz += izzi;
            sumIxy += ixyi;
            sumIxz += ixzi;
            sumIyz += iyzi;
        }

        double xcg = sumM == 0 ? 0 : sumMx / sumM;
        double ycg = sumM == 0 ? 0 : sumMy / sumM;
        double zcg = sumM == 0 ? 0 : sumMz / sumM;

        double ixx = sumIxx + (sumMyy + sumMzz) - sumM * (ycg * ycg + zcg * zcg);
        double iyy = sumIyy + (sumMzz + sumMxx) - sumM * (zcg * zcg + xcg * xcg);
        double izz = sumIzz + (sumMxx + sumMyy) - sumM * (xcg * xcg + ycg * ycg);
        double ixy = sumIxy + sumMxy - sumM * xcg * ycg;
        double ixz = sumIxz + sumMxz - sumM * xcg * zcg;
        double iyz = sumIyz + sumMyz - sumM * ycg * zcg;

        double ul2 = unitL * unitL;
        return new MassProperties
        {
            Mass = sumM * unitM,
            Cg = new double[] { xcg * unitL, ycg * unitL, zcg * unitL },
            Inertia = new double[][]
            {
                new double[] { ixx * unitM * ul2, -ixy * unitM * ul2, -ixz * unitM * ul2 },
                new double[] { -ixy * unitM * ul2, iyy * unitM * ul2, -iyz * unitM * ul2 },
                new double[] { -ixz * unitM * ul2, -iyz * unitM * ul2, izz * unitM * ul2 },
            },
            G = g,
            Rho = rho,
            UnitL = unitL,
            UnitM = unitM,
            UnitT = unitT,
        };
    }
}
