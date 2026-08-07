// Port of amake.f SUBROUTINE MAKESURF (paneling of one surface into strips
// and vortices) and SUBROUTINE SDUPL (mirrored duplicate-image surface).
//
// Scope note: the CPOML nodal-pressure grid (XYN1/XYN2/ZLON*/ZUPN*) is not
// ported -- only used by the CPOM/CPTHK OML-pressure commands, out of scope.
//
// Port of packages/avl-core/src/geometry/makeSurface.ts.

namespace Avl.Core.Model;

public static class MakeSurface
{
    /// <summary>Camberline slope lookup via sgutil.f AKIMA. SASEC already *is* the
    /// camberline-slope function sampled at XASEC stations, so what's needed is
    /// AKIMA's interpolated *value* (.Yy) at xq, not its derivative.</summary>
    private static double AkimaSlope(IReadOnlyList<double> xasec, IReadOnlyList<double> sasec, double xq)
    {
        if (xasec.Count < 2) return 0;
        return Akima.Compute(xasec, sasec, xasec.Count, xq).Yy;
    }

    private static ControlDeclaration? FindControl(Section section, string name)
    {
        return section.Controls.FirstOrDefault(c => c.Name == name);
    }

    private sealed class HingeGeometry
    {
        public double[] Gainda = Array.Empty<double>();
        public double[] Xled = Array.Empty<double>();
        public double[] Xted = Array.Empty<double>();
        public double[][] Vhinge = Array.Empty<double[]>();
        public double[][] Phinge = Array.Empty<double[]>();
        public double[] Vrefl = Array.Empty<double>();
    }

    /// <summary>Port of the per-section-interval control-surface hinge geometry setup
    /// (amake.f:301-310, 393-465).</summary>
    private static HingeGeometry ComputeHingeGeometry(
        Geometry geo,
        Section secL,
        Section secR,
        double[] xyzScale,
        double fc,
        double chordL,
        double chordR,
        double chordC,
        double[] rle)
    {
        int n = geo.ControlNames.Count;
        var gainda = new double[n];
        var xled = new double[n];
        var xted = new double[n];
        var vhinge = new double[n][];
        var phinge = new double[n][];
        var vrefl = new double[n];

        for (int ni = 0; ni < n; ni++)
        {
            string name = geo.ControlNames[ni];
            var ctrlL = FindControl(secL, name);
            var ctrlR = FindControl(secR, name);

            if (ctrlL == null || ctrlR == null)
            {
                vhinge[ni] = new double[] { 0, 0, 0 };
                phinge[ni] = new double[] { 0, 0, 0 };
                continue;
            }

            gainda[ni] = ctrlL.Gain * (1.0 - fc) + ctrlR.Gain * fc;

            double xhd = chordL * ctrlL.XHinge * (1.0 - fc) + chordR * ctrlR.XHinge * fc;
            if (xhd >= 0.0)
            {
                xled[ni] = xhd;
                xted[ni] = chordC;
            }
            else
            {
                xled[ni] = 0.0;
                xted[ni] = -xhd;
            }

            double vhx = ctrlL.HingeVec[0] * xyzScale[0];
            double vhy = ctrlL.HingeVec[1] * xyzScale[1];
            double vhz = ctrlL.HingeVec[2] * xyzScale[2];
            double vsq = vhx * vhx + vhy * vhy + vhz * vhz;
            if (vsq == 0.0)
            {
                vhx = (secR.Xle[0] + Math.Abs(chordR * ctrlR.XHinge) - (secL.Xle[0] + Math.Abs(chordL * ctrlL.XHinge))) * xyzScale[0];
                vhy = (secR.Xle[1] - secL.Xle[1]) * xyzScale[1];
                vhz = (secR.Xle[2] - secL.Xle[2]) * xyzScale[2];
                vsq = vhx * vhx + vhy * vhy + vhz * vhz;
            }
            double vmod = Math.Sqrt(vsq);
            vhinge[ni] = new double[] { vhx / vmod, vhy / vmod, vhz / vmod };
            vrefl[ni] = ctrlL.Refl;

            if (xhd >= 0.0)
            {
                phinge[ni] = new double[] { rle[0] + xhd, rle[1], rle[2] };
            }
            else
            {
                phinge[ni] = new double[] { rle[0] - xhd, rle[1], rle[2] };
            }
        }

        return new HingeGeometry { Gainda = gainda, Xled = xled, Xted = xted, Vhinge = vhinge, Phinge = phinge, Vrefl = vrefl };
    }

    private static double[] Lerp3(double[] a, double[] b, double f) => new double[]
    {
        (1 - f) * a[0] + f * b[0],
        (1 - f) * a[1] + f * b[1],
        (1 - f) * a[2] + f * b[2],
    };

    public static void MakeSurfaceStrips(Geometry geo, int surfIndex)
    {
        var surf = geo.Surfaces[surfIndex];
        int nsec = surf.Sections.Count;
        if (nsec < 2) throw new InvalidOperationException($"Surface '{surf.Title}': need at least 2 sections");

        int nvc = surf.Nvc;

        // ---- Arc-length position of each section along the surface's y-z trace.
        var yzlen = new double[nsec];
        yzlen[0] = 0;
        for (int isec = 1; isec < nsec; isec++)
        {
            double dy = surf.Sections[isec].Xle[1] - surf.Sections[isec - 1].Xle[1];
            double dz = surf.Sections[isec].Xle[2] - surf.Sections[isec - 1].Xle[2];
            yzlen[isec] = yzlen[isec - 1] + Math.Sqrt(dy * dy + dz * dz);
        }

        // ---- Spanwise station points (ypt panel edges, ycp panel centers).
        int nvs;
        var ypt = new List<double>();
        var ycp = new List<double>();
        var iptLoc = new int[nsec];
        // grow-on-demand helpers to mirror the JS sparse-array assignment
        void SetYpt(int i, double v) { while (ypt.Count <= i) ypt.Add(0); ypt[i] = v; }
        void SetYcp(int i, double v) { while (ycp.Count <= i) ycp.Add(0); ycp[i] = v; }

        if (surf.Nvs == 0)
        {
            // Spanwise spacing driven per-section-interval by each SECTION's Nspan/Sspace.
            nvs = 0;
            SetYpt(0, yzlen[0]);
            iptLoc[0] = 0;
            for (int isec = 0; isec < nsec - 1; isec++)
            {
                double dyzlen = yzlen[isec + 1] - yzlen[isec];
                int nvint = surf.Sections[isec].Nspan;
                var fspace = Spacing.Spacer(2 * nvint + 1, surf.Sections[isec].Sspace);
                for (int nn = 1; nn <= nvint; nn++)
                {
                    int ivs = nvs + nn; // 1-based running index, matches Fortran IVS
                    SetYcp(ivs - 1, ypt[nvs] + dyzlen * fspace[2 * nn - 1]);
                    SetYpt(ivs, ypt[nvs] + dyzlen * fspace[2 * nn]);
                }
                iptLoc[isec + 1] = nvs + nvint;
                nvs += nvint;
            }
        }
        else
        {
            // Spanwise spacing driven by the SURFACE-level Nspan/Sspace, fudged to
            // align vortex edges with each interior SECTION.
            nvs = surf.Nvs;
            var fspace = Spacing.Spacer(2 * nvs + 1, surf.Sspace);
            double span = yzlen[nsec - 1] - yzlen[0];
            SetYpt(0, yzlen[0]);
            for (int ivs = 1; ivs <= nvs; ivs++)
            {
                SetYcp(ivs - 1, yzlen[0] + span * fspace[2 * ivs - 1]);
                SetYpt(ivs, yzlen[0] + span * fspace[2 * ivs]);
            }
            int npt = nvs + 1;

            for (int isec = 1; isec < nsec - 1; isec++)
            {
                double yptloc = 1e9;
                iptLoc[isec] = 0;
                for (int ipt = 0; ipt < npt; ipt++)
                {
                    double d = Math.Abs(yzlen[isec] - ypt[ipt]);
                    if (d < yptloc)
                    {
                        yptloc = d;
                        iptLoc[isec] = ipt;
                    }
                }
            }
            iptLoc[0] = 0;
            iptLoc[nsec - 1] = npt - 1;

            for (int isec = 1; isec < nsec - 1; isec++)
            {
                // fudge to align with this section
                {
                    int ipt1 = iptLoc[isec - 1];
                    int ipt2 = iptLoc[isec];
                    if (ipt1 == ipt2) throw new InvalidOperationException($"MAKESURF: cannot adjust spanwise spacing at section {isec + 1}");
                    double ypt1 = ypt[ipt1];
                    double yscale = (yzlen[isec] - yzlen[isec - 1]) / (ypt[ipt2] - ypt1);
                    for (int ipt = ipt1; ipt < ipt2; ipt++) ypt[ipt] = yzlen[isec - 1] + yscale * (ypt[ipt] - ypt1);
                    for (int ivs = ipt1; ivs < ipt2; ivs++) ycp[ivs] = yzlen[isec - 1] + yscale * (ycp[ivs] - ypt1);
                }
                // fudge to align with next section
                {
                    int ipt1 = iptLoc[isec];
                    int ipt2 = iptLoc[isec + 1];
                    if (ipt1 == ipt2) throw new InvalidOperationException($"MAKESURF: cannot adjust spanwise spacing at section {isec + 1}");
                    double ypt1 = ypt[ipt1];
                    double yscale = (ypt[ipt2] - yzlen[isec]) / (ypt[ipt2] - ypt1);
                    for (int ipt = ipt1; ipt < ipt2; ipt++) ypt[ipt] = yzlen[isec] + yscale * (ypt[ipt] - ypt1);
                    for (int ivs = ipt1; ivs < ipt2; ivs++) ycp[ivs] = yzlen[isec] + yscale * (ycp[ivs] - ypt1);
                }
            }
        }

        // ---- Build strips/vortices between each pair of adjacent sections.
        for (int isec = 0; isec < nsec - 1; isec++)
        {
            var secL = surf.Sections[isec];
            var secR = surf.Sections[isec + 1];

            var xyzLeL = new double[]
            {
                surf.Scale[0] * secL.Xle[0] + surf.Translate[0],
                surf.Scale[1] * secL.Xle[1] + surf.Translate[1],
                surf.Scale[2] * secL.Xle[2] + surf.Translate[2],
            };
            var xyzLeR = new double[]
            {
                surf.Scale[0] * secR.Xle[0] + surf.Translate[0],
                surf.Scale[1] * secR.Xle[1] + surf.Translate[1],
                surf.Scale[2] * secR.Xle[2] + surf.Translate[2],
            };

            double width = Math.Sqrt((xyzLeR[1] - xyzLeL[1]) * (xyzLeR[1] - xyzLeL[1]) + (xyzLeR[2] - xyzLeL[2]) * (xyzLeR[2] - xyzLeL[2]));

            double chordL = surf.Scale[0] * secL.Chord;
            double chordR = surf.Scale[0] * secR.Chord;
            double clafL = secL.Claf;
            double clafR = secR.Claf;

            double aincL = secL.Ainc + surf.Angle;
            double aincR = secR.Ainc + surf.Angle;
            double chsinL = chordL * Math.Sin(aincL);
            double chsinR = chordR * Math.Sin(aincR);
            double chcosL = chordL * Math.Cos(aincL);
            double chcosR = chordR * Math.Cos(aincR);

            int iptL = iptLoc[isec];
            int iptR = iptLoc[isec + 1];
            int nspan = iptR - iptL;

            for (int ispan = 1; ispan <= nspan; ispan++)
            {
                int ipt1 = iptL + ispan - 1;
                int ipt2 = iptL + ispan;
                int ivs = iptL + ispan - 1;
                double f1 = (ypt[ipt1] - ypt[iptL]) / (ypt[iptR] - ypt[iptL]);
                double f2 = (ypt[ipt2] - ypt[iptL]) / (ypt[iptR] - ypt[iptL]);
                double fc = (ycp[ivs] - ypt[iptL]) / (ypt[iptR] - ypt[iptL]);

                var rle1 = Lerp3(xyzLeL, xyzLeR, f1);
                var rle2 = Lerp3(xyzLeL, xyzLeR, f2);
                var rle = Lerp3(xyzLeL, xyzLeR, fc);
                double chord1 = (1 - f1) * chordL + f1 * chordR;
                double chord2 = (1 - f2) * chordL + f2 * chordR;
                double chordC = (1 - fc) * chordL + fc * chordR;

                double wstrip = Math.Abs(f2 - f1) * width;
                double tanLE = (xyzLeR[0] - xyzLeL[0]) / width;
                double tanTE = (xyzLeR[0] + chordR - xyzLeL[0] - chordL) / width;

                double chsin = chsinL + fc * (chsinR - chsinL);
                double chcos = chcosL + fc * (chcosR - chcosL);
                double ainc = Math.Atan2(chsin, chcos);

                double clafC = (1 - fc) * (chordL / chordC) * clafL + fc * (chordR / chordC) * clafR;

                var clcd = new double[6];
                for (int l = 0; l < 6; l++) clcd[l] = (1 - fc) * secL.Clcd[l] + fc * secR.Clcd[l];

                var hg = ComputeHingeGeometry(geo, secL, secR, surf.Scale, fc, chordL, chordR, chordC, rle);

                int stripIndex = geo.Strips.Count;
                var strip = new Strip
                {
                    SurfaceIndex = surfIndex,
                    Rle1 = rle1,
                    Rle2 = rle2,
                    Rle = rle,
                    Chord1 = chord1,
                    Chord2 = chord2,
                    Chord = chordC,
                    Wstrip = wstrip,
                    TanLE = tanLE,
                    TanTE = tanTE,
                    Ainc = ainc,
                    Nvc = nvc,
                    FirstVortex = geo.Vortices.Count,
                    Clcd = clcd,
                    Viscous = clcd[3] != 0,
                    Vhinge = hg.Vhinge.ToList(),
                    Phinge = hg.Phinge.ToList(),
                    Vrefl = hg.Vrefl.ToList(),
                };
                geo.Strips.Add(strip);

                var cs = Spacing.Cspacer(nvc, surf.Cspace, clafC);
                var xpt = cs.Xpt; var xvr = cs.Xvr; var xsr = cs.Xsr; var xcp = cs.Xcp;
                int nControl = geo.ControlNames.Count;

                for (int ivc = 0; ivc < nvc; ivc++)
                {
                    double dxoc = xpt[ivc + 1] - xpt[ivc];
                    var dcontrol = new double[nControl];
                    for (int ni = 0; ni < nControl; ni++)
                    {
                        double fracle = (hg.Xled[ni] / chordC - xpt[ivc]) / dxoc;
                        double fracte = (hg.Xted[ni] / chordC - xpt[ivc]) / dxoc;
                        fracle = Math.Min(1.0, Math.Max(0.0, fracle));
                        fracte = Math.Min(1.0, Math.Max(0.0, fracte));
                        dcontrol[ni] = hg.Gainda[ni] * (fracte - fracle);
                    }

                    var vortex = new Vortex
                    {
                        StripIndex = stripIndex,
                        Rv1 = new double[] { rle1[0] + xvr[ivc] * chord1, rle1[1], rle1[2] },
                        Rv2 = new double[] { rle2[0] + xvr[ivc] * chord2, rle2[1], rle2[2] },
                        Rv = new double[] { rle[0] + xvr[ivc] * chordC, rle[1], rle[2] },
                        Rc = new double[] { rle[0] + xcp[ivc] * chordC, rle[1], rle[2] },
                        Rs = new double[] { rle[0] + xsr[ivc] * chordC, rle[1], rle[2] },
                        Slopec =
                            (1 - fc) * (chordL / chordC) * AkimaSlope(secL.Airfoil.Xasec, secL.Airfoil.Sasec, xcp[ivc]) +
                            fc * (chordR / chordC) * AkimaSlope(secR.Airfoil.Xasec, secR.Airfoil.Sasec, xcp[ivc]),
                        Slopev =
                            (1 - fc) * (chordL / chordC) * AkimaSlope(secL.Airfoil.Xasec, secL.Airfoil.Sasec, xvr[ivc]) +
                            fc * (chordR / chordC) * AkimaSlope(secR.Airfoil.Xasec, secR.Airfoil.Sasec, xvr[ivc]),
                        Dx = dxoc * chordC,
                        Chord = chordC,
                        HasWake = !surf.NoWake,
                        Dcontrol = dcontrol.ToList(),
                    };
                    geo.Vortices.Add(vortex);
                }
            }
        }
    }

    /// <summary>Port of amake.f SUBROUTINE SDUPL: mirrors an already-built surface's
    /// strips/vortices about y = yMirror into a new duplicate-image surface.</summary>
    public static void DuplicateSurfaceStrips(Geometry geo, int srcSurfIndex, int dupSurfIndex, double yMirror)
    {
        double yOff = 2.0 * yMirror;
        var srcStrips = geo.Strips.Where(s => s.SurfaceIndex == srcSurfIndex).ToList();

        foreach (var srcStrip in srcStrips)
        {
            double MirrorY(double y) => -y + yOff;

            var dupStrip = new Strip
            {
                SurfaceIndex = dupSurfIndex,
                Rle1 = new double[] { srcStrip.Rle2[0], MirrorY(srcStrip.Rle2[1]), srcStrip.Rle2[2] },
                Rle2 = new double[] { srcStrip.Rle1[0], MirrorY(srcStrip.Rle1[1]), srcStrip.Rle1[2] },
                Rle = new double[] { srcStrip.Rle[0], MirrorY(srcStrip.Rle[1]), srcStrip.Rle[2] },
                Chord1 = srcStrip.Chord2,
                Chord2 = srcStrip.Chord1,
                Chord = srcStrip.Chord,
                Wstrip = srcStrip.Wstrip,
                TanLE = -srcStrip.TanLE,
                TanTE = -srcStrip.TanTE,
                Ainc = srcStrip.Ainc,
                Nvc = srcStrip.Nvc,
                FirstVortex = geo.Vortices.Count,
                Clcd = srcStrip.Clcd,
                Viscous = srcStrip.Viscous,
                Vhinge = srcStrip.Vhinge.Select(v => new double[] { v[0], -v[1], v[2] }).ToList(),
                Phinge = srcStrip.Phinge.Select(p => new double[] { p[0], -p[1] + yOff, p[2] }).ToList(),
                Vrefl = srcStrip.Vrefl,
            };
            int dupStripIndex = geo.Strips.Count;
            geo.Strips.Add(dupStrip);

            for (int k = 0; k < srcStrip.Nvc; k++)
            {
                var v = geo.Vortices[srcStrip.FirstVortex + k];
                var dcontrol = new List<double>();
                for (int ni = 0; ni < v.Dcontrol.Count; ni++) dcontrol.Add(-v.Dcontrol[ni] * srcStrip.Vrefl[ni]);
                var dupV = new Vortex
                {
                    StripIndex = dupStripIndex,
                    Rv1 = new double[] { v.Rv2[0], MirrorY(v.Rv2[1]), v.Rv2[2] },
                    Rv2 = new double[] { v.Rv1[0], MirrorY(v.Rv1[1]), v.Rv1[2] },
                    Rv = new double[] { v.Rv[0], MirrorY(v.Rv[1]), v.Rv[2] },
                    Rc = new double[] { v.Rc[0], MirrorY(v.Rc[1]), v.Rc[2] },
                    Rs = new double[] { v.Rs[0], MirrorY(v.Rs[1]), v.Rs[2] },
                    Slopec = v.Slopec,
                    Slopev = v.Slopev,
                    Dx = v.Dx,
                    Chord = v.Chord,
                    HasWake = v.HasWake,
                    Dcontrol = dcontrol,
                };
                geo.Vortices.Add(dupV);
            }
        }
    }
}
