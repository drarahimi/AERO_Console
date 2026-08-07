// Port of amake.f SUBROUTINE ENCALC -- computes unit normal vectors at each
// vortex's control point (ENC) and bound-vortex midpoint (ENV), the
// strip-level spanwise-axis vectors (ESS/ENSY/ENSZ) used later by AERO, and
// each control variable's linearized-about-zero-deflection normal-vector
// sensitivity (ENC_D/ENV_D).
//
// Port of packages/avl-core/src/solver/encalc.ts.

using Avl.Core.Model;

namespace Avl.Core.Solver;

public sealed class StripAxes
{
    public double[] Ess = new double[3];
    public double Ensy;
    public double Ensz;
    /// <summary>SAXFR-weighted point along the strip's LE-to-TE vortex line.</summary>
    public double[] Sref = new double[3];
}

public sealed class VortexNormals
{
    public double[] Enc = new double[3];
    public double[] Env = new double[3];
    /// <summary>d(ENC)/d(DELCON(N)) at DELCON=0, index-aligned with Geometry.ControlNames.</summary>
    public double[][] EncD = Array.Empty<double[]>();
    /// <summary>d(ENV)/d(DELCON(N)) at DELCON=0.</summary>
    public double[][] EnvD = Array.Empty<double[]>();
}

public sealed class EncalcResult
{
    public StripAxes[] StripAxes = Array.Empty<StripAxes>();
    public VortexNormals[] VortexNormals = Array.Empty<VortexNormals>();
}

public static class Encalc
{
    private const double SAXFR = 0.25; // x/c location of spanwise axis for Vperp definition
    private const double DTR = Math.PI / 180.0;

    private static double[] Cross(double[] a, double[] b) => new double[]
    {
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0],
    };

    private static double Dot(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];

    public static EncalcResult Compute(Geometry geo)
    {
        var stripAxes = new StripAxes[geo.Strips.Count];
        var vortexNormals = new VortexNormals[geo.Vortices.Count];
        int nControl = geo.ControlNames.Count;

        for (int j = 0; j < geo.Strips.Count; j++)
        {
            var strip = geo.Strips[j];
            var vFirst = geo.Vortices[strip.FirstVortex];
            var vLast = geo.Vortices[strip.FirstVortex + strip.Nvc - 1];

            double dxle = vFirst.Rv2[0] - vFirst.Rv1[0];
            double dyle = vFirst.Rv2[1] - vFirst.Rv1[1];
            double dzle = vFirst.Rv2[2] - vFirst.Rv1[2];
            double dxte = vLast.Rv2[0] - vLast.Rv1[0];
            double dyte = vLast.Rv2[1] - vLast.Rv1[1];
            double dzte = vLast.Rv2[2] - vLast.Rv1[2];

            double dxt = (1.0 - SAXFR) * dxle + SAXFR * dxte;
            double dyt = (1.0 - SAXFR) * dyle + SAXFR * dyte;
            double dzt = (1.0 - SAXFR) * dzle + SAXFR * dzte;

            double dtMag = Math.Sqrt(dxt * dxt + dyt * dyt + dzt * dzt);
            var ess = new double[] { dxt / dtMag, dyt / dtMag, dzt / dtMag };

            double yzMag = Math.Sqrt(dyt * dyt + dzt * dzt);
            double ensy = -dzt / yzMag;
            double ensz = dyt / yzMag;

            var sref = new double[]
            {
                (1.0 - SAXFR) * vFirst.Rv[0] + SAXFR * vLast.Rv[0],
                (1.0 - SAXFR) * vFirst.Rv[1] + SAXFR * vLast.Rv[1],
                (1.0 - SAXFR) * vFirst.Rv[2] + SAXFR * vLast.Rv[2],
            };

            stripAxes[j] = new StripAxes { Ess = ess, Ensy = ensy, Ensz = ensz, Sref = sref };

            var es = new double[] { 0, ensy, ensz };

            for (int ii = 0; ii < strip.Nvc; ii++)
            {
                int i = strip.FirstVortex + ii;
                var v = geo.Vortices[i];

                double dxb = v.Rv2[0] - v.Rv1[0];
                double dyb = v.Rv2[1] - v.Rv1[1];
                double dzb = v.Rv2[2] - v.Rv1[2];
                double emag0 = Math.Sqrt(dxb * dxb + dyb * dyb + dzb * dzb);
                var eb = new double[] { dxb / emag0, dyb / emag0, dzb / emag0 };

                double[] NormalFor(double slope)
                {
                    double ang = strip.Ainc - Math.Atan(slope);
                    double sinc = Math.Sin(ang);
                    double cosc = Math.Cos(ang);
                    var ec = new double[] { cosc, -sinc * es[1], -sinc * es[2] };
                    var ecxb = Cross(ec, eb);
                    double emag = Math.Sqrt(ecxb[0] * ecxb[0] + ecxb[1] * ecxb[1] + ecxb[2] * ecxb[2]);
                    if (emag != 0) return new double[] { ecxb[0] / emag, ecxb[1] / emag, ecxb[2] / emag };
                    return es;
                }

                var enc = NormalFor(v.Slopec);
                var env = NormalFor(v.Slopev);

                // Control-surface rotation sensitivity, linearized about zero deflection.
                var encD = new double[nControl][];
                var envD = new double[nControl][];
                for (int n = 0; n < nControl; n++)
                {
                    double dcontrol = v.Dcontrol[n];
                    if (dcontrol == 0)
                    {
                        encD[n] = new double[] { 0, 0, 0 };
                        envD[n] = new double[] { 0, 0, 0 };
                        continue;
                    }
                    double angDdc = DTR * dcontrol;
                    var hinge = strip.Vhinge[n];

                    double[] RotSensitivity(double[] normal)
                    {
                        double endot = Dot(normal, hinge);
                        var ep = new double[] { normal[0] - endot * hinge[0], normal[1] - endot * hinge[1], normal[2] - endot * hinge[2] };
                        var eq = Cross(hinge, ep);
                        return new double[] { eq[0] * angDdc, eq[1] * angDdc, eq[2] * angDdc };
                    }

                    encD[n] = RotSensitivity(enc);
                    envD[n] = RotSensitivity(env);
                }

                vortexNormals[i] = new VortexNormals { Enc = enc, Env = env, EncD = encD, EnvD = envD };
            }
        }

        return new EncalcResult { StripAxes = stripAxes, VortexNormals = vortexNormals };
    }
}
