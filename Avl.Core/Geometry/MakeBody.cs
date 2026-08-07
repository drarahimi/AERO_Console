// Port of amake.f SUBROUTINE MAKEBODY (and BDUPL): panels a body's r(x) profile
// into NVB source+doublet line nodes with radii, and computes its enclosed volume,
// wetted area, and axial length.
//
// Ported directly from avl3.52 Fortran (no TypeScript predecessor).

namespace Avl.Core.Model;

public static class MakeBody
{
    private const double PI = 3.14159265;

    /// <summary>Build a Body from its raw shape profile (xbod x-stations, ybod centerline,
    /// tbod diameter -- from GETCAM(...,lnorm:false)), the node count/spacing, and the
    /// body's scale/translate.</summary>
    public static Body Make(string title, int nvb, double bspace, double[] scale, double[] translate,
        IReadOnlyList<double> xbod, IReadOnlyList<double> ybod, IReadOnlyList<double> tbod)
    {
        int nbod = xbod.Count;
        var fspace = Spacing.Spacer(nvb, bspace);
        var xpt = new double[nvb];
        for (int i = 0; i < nvb; i++) xpt[i] = fspace[i];
        xpt[0] = 0.0;
        xpt[nvb - 1] = 1.0;

        var body = new Body { Title = title, ImageSign = 1 };
        for (int ivb = 0; ivb < nvb; ivb++)
        {
            double xvb = xbod[0] + (xbod[nbod - 1] - xbod[0]) * xpt[ivb];
            double yvb = Akima.Compute(xbod, ybod, nbod, xvb).Yy;
            double tvb = Akima.Compute(xbod, tbod, nbod, xvb).Yy;
            body.Nodes.Add(new double[] { translate[0] + scale[0] * xvb, translate[1], translate[2] + scale[2] * yvb });
            body.Radii.Add(Math.Sqrt(scale[1] * scale[2]) * 0.5 * tvb);
        }

        // Volume, wetted surface, axial length from the truncated-cone segments.
        double volb = 0, srfb = 0;
        double xbmn = body.Nodes[0][0], xbmx = xbmn;
        for (int ivb = 0; ivb < nvb - 1; ivb++)
        {
            double x0 = body.Nodes[ivb][0], x1 = body.Nodes[ivb + 1][0];
            double dx = Math.Abs(x1 - x0);
            double r0 = body.Radii[ivb], r1 = body.Radii[ivb + 1];
            double dvol = PI * dx * (r0 * r0 + r0 * r1 + r1 * r1) / 3.0;
            double ds = Math.Sqrt((r0 - r1) * (r0 - r1) + dx * dx);
            double dsrf = PI * ds * (r0 + r1);
            volb += dvol;
            srfb += dsrf;
            xbmn = Math.Min(xbmn, Math.Min(x0, x1));
            xbmx = Math.Max(xbmx, Math.Max(x0, x1));
        }
        body.Volume = volb;
        body.Surface = srfb;
        body.Length = xbmx - xbmn;
        return body;
    }

    /// <summary>Port of BDUPL: mirror image of a body about y = yMirror.</summary>
    public static Body Duplicate(Body src, double yMirror)
    {
        double yOff = 2.0 * yMirror;
        var dup = new Body
        {
            Title = $"{src.Title} (YDUP)",
            Volume = src.Volume,
            Surface = src.Surface,
            Length = src.Length,
            ImageSign = -src.ImageSign,
        };
        foreach (var n in src.Nodes) dup.Nodes.Add(new double[] { n[0], -n[1] + yOff, n[2] });
        foreach (var r in src.Radii) dup.Radii.Add(r);
        return dup;
    }
}
