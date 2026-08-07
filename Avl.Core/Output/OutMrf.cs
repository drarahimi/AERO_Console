// Port of aoutmrf.f: the machine-readable ("MRF") output format. Once the OPER
// "MRF" command is typed, the display commands (FT/FN/FS/FE/HM/VM + CNC) emit
// full-precision ES23.15 machine-readable blocks instead of the human-readable
// tables. All values reuse the same solved data as the human formatters.
//
// Ported directly from avl3.52 Fortran (no TypeScript predecessor).

using Avl.Core.Model;
using Avl.Core.Solver;
using static Avl.Core.Output.FortranFormat;

namespace Avl.Core.Output;

public static class OutMrf
{
    private const double DTR = Math.PI / 180.0;
    private const double DIR = -1.0; // GETSA(LNASA_SA=.TRUE.)
    private const string SATYPE = "Standard axis orientation,  X fwd, Z down";

    private static string E(double v) => FortranES(v, 23, 15);
    private static string I6(int v) => FortranI(v, 6);

    /// <summary>MRFTOT (FILEID='TOT'): total-forces machine-readable block.</summary>
    public static string MrfTot(Geometry geo, CaseResult result, double[]? xyzref = null, string title = " -unnamed-")
    {
        xyzref ??= geo.Xyzref0;
        var t = result.Totals;
        var tr = result.Trefftz;
        double ca = Math.Cos(result.Alfa), sa = Math.Sin(result.Alfa);
        var wrot = result.Wrot;

        double rxB = wrot[0] * geo.Bref / 2.0, ryB = wrot[1] * geo.Cref / 2.0, rzB = wrot[2] * geo.Bref / 2.0;
        double rxS = (wrot[0] * ca + wrot[2] * sa) * geo.Bref / 2.0;
        double rzS = (wrot[2] * ca - wrot[0] * sa) * geo.Bref / 2.0;
        double crsax = t.CmTot[0] * ca + t.CmTot[2] * sa;
        double cnsax = t.CmTot[2] * ca - t.CmTot[0] * sa;
        double cditot = t.CdTot - t.CdvTot;

        var l = new List<string>();
        l.Add("TOT");
        l.Add("VERSION 1.0");
        l.Add("Vortex Lattice Output -- Total Forces");
        l.Add(OutTotal.Slice(geo.Title, 60).PadRight(60));
        l.Add(I6(geo.Surfaces.Count) + "      | # surfaces");
        l.Add(I6(geo.Strips.Count) + "      | # strips");
        l.Add(I6(geo.Vortices.Count) + "      | # vortices");
        // Y/Z symmetry lines omitted for iYsym/iZsym == 0 (current fixtures).
        l.Add(E(geo.Sref) + E(geo.Cref) + E(geo.Bref) + "      | Sref, Cref, Bref");
        l.Add(E(xyzref[0]) + E(xyzref[1]) + E(xyzref[2]) + "      | Xref, Yref, Zref");
        l.Add(SATYPE.PadRight(50));
        l.Add(title.PadRight(40));
        l.Add(E(result.Alfa / DTR) + E(DIR * rxB) + E(DIR * rxS) + "      | Alpha, pb/2V, p'b/2V");
        l.Add(E(result.Beta / DTR) + E(ryB) + "      | Beta, qc/2V");
        l.Add(E(result.Mach) + E(DIR * rzB) + E(DIR * rzS) + "      | Mach, rb/2V, r'b/2V");
        l.Add(E(DIR * t.CfTot[0]) + E(DIR * t.CmTot[0]) + E(DIR * crsax) + "      | CXtot, Cltot, Cl'tot");
        l.Add(E(t.CfTot[1]) + E(t.CmTot[1]) + "      | CYtot, Cmtot");
        l.Add(E(DIR * t.CfTot[2]) + E(DIR * t.CmTot[2]) + E(DIR * cnsax) + "      | CZtot, Cntot, Cn'tot");
        l.Add(E(t.ClTot) + "      | CLtot");
        l.Add(E(t.CdTot) + "      | CDtot");
        l.Add(E(t.CdvTot) + E(cditot) + "      | CDvis, CDind");
        l.Add(E(tr.Clff) + E(tr.Cdff) + E(tr.Cyff) + E(tr.Spanef) + "      | Trefftz Plane: CLff, CDff, CYff, e");
        l.Add("CONTROL");
        l.Add(I6(geo.ControlNames.Count));
        for (int k = 0; k < geo.ControlNames.Count; k++)
        {
            double d = k < result.Delcon.Length ? result.Delcon[k] : 0;
            l.Add(E(d) + "  " + geo.ControlNames[k].PadRight(16));
        }
        l.Add("DESIGN");
        l.Add(I6(geo.DesignNames.Count));
        for (int k = 0; k < geo.DesignNames.Count; k++)
            l.Add(E(0) + "  " + geo.DesignNames[k].PadRight(16));

        return string.Join("\n", l);
    }

    private static double[] Cross(double[] a, double[] b) => new double[]
    {
        a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0],
    };

    // Per-surface derived quantities shared by MRFSURF/MRFSTRP/MRFELE (mirrors OutSurface).
    private static (double ssurf, double cave, double clLsrf, double cdLsrf, List<int> js) SurfaceLocal(Geometry geo, CaseResult result, int s)
    {
        var js = new List<int>();
        double area = 0, wtot = 0, enY = 0, enZ = 0;
        for (int j = 0; j < geo.Strips.Count; j++)
        {
            if (geo.Strips[j].SurfaceIndex != s) continue;
            js.Add(j);
            double sr = geo.Strips[j].Chord * geo.Strips[j].Wstrip;
            area += sr; wtot += geo.Strips[j].Wstrip;
            enY += sr * result.StripAxes[j].Ensy;
            enZ += sr * result.StripAxes[j].Ensz;
        }
        double cave = wtot == 0 ? 0 : area / wtot;
        double enaveY = area == 0 ? 0 : enY / area, enaveZ = area == 0 ? 0 : enZ / area;
        double enMag = Math.Sqrt(enaveY * enaveY + enaveZ * enaveZ);
        if (enMag == 0) enaveZ = 1.0; else { enaveY /= enMag; enaveZ /= enMag; }
        var spn = new double[] { 0, enaveZ, -enaveY };
        var udrag = result.Vinf;
        var ulift = Cross(udrag, spn);
        double ulMag = Math.Sqrt(ulift[0] * ulift[0] + ulift[1] * ulift[1] + ulift[2] * ulift[2]);
        ulift = ulMag == 0 ? new double[] { 0, 0, 1 } : new double[] { ulift[0] / ulMag, ulift[1] / ulMag, ulift[2] / ulMag };
        var bs = result.Totals.BySurface[s];
        var cf = area == 0 ? new double[] { 0, 0, 0 } : new double[] { bs.CfSurf[0] * geo.Sref / area, bs.CfSurf[1] * geo.Sref / area, bs.CfSurf[2] * geo.Sref / area };
        double clL = ulift[0] * cf[0] + ulift[1] * cf[1] + ulift[2] * cf[2];
        double cdL = udrag[0] * cf[0] + udrag[1] * cf[1] + udrag[2] * cf[2];
        return (area, cave, clL, cdL, js);
    }

    /// <summary>MRFSURF (FN under MRF mode): per-surface forces.</summary>
    public static string MrfSurf(Geometry geo, CaseResult result)
    {
        var l = new List<string>();
        l.Add("SURF");
        l.Add("VERSION 1.0");
        l.Add(SATYPE.PadRight(50));
        l.Add(E(geo.Sref) + E(geo.Cref) + E(geo.Bref) + "      | Sref, Cref, Bref");
        l.Add(E(geo.Xyzref0[0]) + E(geo.Xyzref0[1]) + E(geo.Xyzref0[2]) + "      | Xref, Yref, Zref");
        l.Add(I6(geo.Surfaces.Count) + "      | # surfaces");
        for (int n = 0; n < geo.Surfaces.Count; n++)
        {
            var (ssurf, cave, clL, cdL, _) = SurfaceLocal(geo, result, n);
            var bs = result.Totals.BySurface[n];
            l.Add("SURFACE");
            l.Add(geo.Surfaces[n].Title.Trim());
            l.Add(FortranI(n + 1, 3) + " " + E(ssurf) + E(bs.ClSurf) + E(bs.CdSurf) + E(bs.CmSurf[1]) +
                  E(bs.CfSurf[1]) + E(DIR * bs.CmSurf[2]) + E(DIR * bs.CmSurf[0]) + E(bs.CdSurf) + E(0) +
                  " | Surface Forces (referred to Sref,Cref,Bref, moments in body axes about Xref,Yref,Zref) : n Area CL CD Cm CY Cn Cl CDi CDv");
            l.Add(FortranI(n + 1, 3) + " " + E(ssurf) + E(cave) + E(clL) + E(cdL) + E(0) +
                  " | Surface Forces (referred to Ssurf, Cave about root LE on hinge axis) : n Ssurf Cave cl cd cdv");
        }
        return string.Join("\n", l);
    }

    /// <summary>MRFSTRP (FS under MRF mode): per-surface + per-strip forces.</summary>
    public static string MrfStrp(Geometry geo, CaseResult result)
    {
        var t = result.Totals;
        var l = new List<string>();
        l.Add("STRP");
        l.Add("VERSION 1.0");
        l.Add(SATYPE.PadRight(50));
        l.Add(E(geo.Sref) + E(geo.Cref) + E(geo.Bref) + "      | Sref, Cref, Bref");
        l.Add(E(geo.Xyzref0[0]) + E(geo.Xyzref0[1]) + E(geo.Xyzref0[2]) + "      | Xref, Yref, Zref");
        l.Add("Surface and Strip Forces by surface (referred to Sref,Cref,Bref about Xref,Yref,Zref)");
        l.Add(I6(geo.Surfaces.Count) + "      | surfaces");
        for (int n = 0; n < geo.Surfaces.Count; n++)
        {
            var (ssurf, cave, clL, cdL, js) = SurfaceLocal(geo, result, n);
            if (js.Count == 0) continue;
            var bs = t.BySurface[n];
            l.Add("SURFACE");
            l.Add(geo.Surfaces[n].Title);
            l.Add(FortranI(n + 1, 4) + " " + FortranI(geo.Surfaces[n].Nvc, 4) + " " + FortranI(js.Count, 4) + " " + FortranI(js[0] + 1, 4) + "    | Surface #, # Chordwise, # Spanwise, First strip");
            l.Add(E(ssurf) + E(cave) + "   | Surface area Ssurf, Ave. chord Cave");
            l.Add(E(bs.ClSurf) + E(DIR * bs.CmSurf[0]) + E(bs.CfSurf[1]) + E(bs.CmSurf[1]) + E(bs.CdSurf) + E(DIR * bs.CmSurf[2]) + E(bs.CdSurf) + E(0) +
                  "   | CLsurf, Clsurf, CYsurf, Cmsurf, CDsurf, Cnsurf, CDisurf, CDvsurf; Forces referred to Sref, Cref, Bref about Xref, Yref, Zref");
            l.Add(E(clL) + E(cdL) + "   | CL_srf CD_srf; Forces referred to Ssurf, Cave");
            l.Add("Strip Forces referred to Strip Area, Chord");
            l.Add("j, Xle, Yle, Zle, Chord, Area, c_cl, ai, cl_perp, cl, cd, cdv, cm_c/4, cm_LE, C.P.x/c");
            foreach (var j in js)
            {
                var strip = geo.Strips[j];
                var bst = t.ByStrip[j];
                double astrp = strip.Wstrip * strip.Chord;
                double xcp = bst.ClLstrp != 0 ? 0.25 - bst.Cmc4Lstrp / bst.ClLstrp : 999.0;
                l.Add(FortranI(j + 1, 4) + E(strip.Rle[0]) + E(strip.Rle[1]) + E(strip.Rle[2]) + E(strip.Chord) + E(astrp) +
                      E(bst.Cnc) + E(result.Trefftz.Dwwake[j]) + E(bst.CltLstrp) + E(bst.ClLstrp) + E(bst.CdLstrp) + E(0) +
                      E(bst.Cmc4Lstrp) + E(bst.CmleLstrp) + E(xcp));
            }
        }
        return string.Join("\n", l);
    }

    /// <summary>MRFHINGE (HM under MRF mode): per-control hinge moments.</summary>
    public static string MrfHinge(Geometry geo, Totals totals)
    {
        var l = new List<string>();
        l.Add("HINGE");
        l.Add("VERSION 1.0");
        l.Add(SATYPE.PadRight(50));
        l.Add(E(geo.Sref) + E(geo.Cref) + "      | Sref, Cref");
        l.Add(FortranI(geo.ControlNames.Count, 4) + "  | # controls");
        for (int n = 0; n < geo.ControlNames.Count; n++)
            l.Add(E(totals.Chinge[n]) + "  " + geo.ControlNames[n].PadRight(16) +
                  "  | Control Hinge Moments (referred to Sref, Cref) : Chinge, Control");
        return string.Join("\n", l);
    }

    /// <summary>MRFELE (FE under MRF mode): per-strip + per-vortex-element detail.</summary>
    public static string MrfEle(Geometry geo, CaseResult result)
    {
        var t = result.Totals;
        var l = new List<string>();
        l.Add("ELE");
        l.Add("VERSION 1.0");
        l.Add(SATYPE.PadRight(50));
        l.Add(E(geo.Sref) + E(geo.Cref) + E(geo.Bref) + "      | Sref, Cref, Bref");
        l.Add(E(geo.Xyzref0[0]) + E(geo.Xyzref0[1]) + E(geo.Xyzref0[2]) + "      | Xref, Yref, Zref");
        l.Add("Vortex Strengths (by surface, by strip)");
        l.Add(I6(geo.Surfaces.Count) + "      | # surfaces");
        for (int n = 0; n < geo.Surfaces.Count; n++)
        {
            var (ssurf, cave, clL, cdL, js) = SurfaceLocal(geo, result, n);
            if (js.Count == 0) continue;
            var bs = t.BySurface[n];
            l.Add("SURFACE");
            l.Add(geo.Surfaces[n].Title);
            l.Add(FortranI(n + 1, 4) + " " + FortranI(geo.Surfaces[n].Nvc, 4) + " " + FortranI(js.Count, 4) + " " + FortranI(js[0] + 1, 4) + "    | Surface #, # Chordwise, # Spanwise, First strip");
            l.Add(E(ssurf) + E(cave) + "   | Surface area, Ave. chord");
            l.Add(E(bs.ClSurf) + E(DIR * bs.CmSurf[0]) + E(bs.CfSurf[1]) + E(bs.CmSurf[1]) + E(bs.CdSurf) + E(DIR * bs.CmSurf[2]) + E(bs.CdSurf) + E(0) +
                  "   | CLsurf, Clsurf, CYsurf, Cmsurf, CDsurf, Cnsurf, CDisurf, CDvsurf");
            l.Add(E(clL) + E(cdL) + "   | CL_srf CD_srf; Forces referred to Ssurf, Cave about hinge axis thru LE");
            foreach (var j in js)
            {
                var strip = geo.Strips[j];
                var bst = t.ByStrip[j];
                double astrp = strip.Wstrip * strip.Chord;
                double dihed = -Math.Atan2(result.StripAxes[j].Ensy, result.StripAxes[j].Ensz) / DTR;
                l.Add("STRIP");
                l.Add(FortranI(j + 1, 4) + FortranI(geo.Surfaces[n].Nvc, 4) + FortranI(strip.FirstVortex + 1, 4) + "  | Strip #, # Chordwise, First Vortex");
                l.Add(E(strip.Rle[0]) + E(strip.Chord) + E(strip.Ainc / DTR) + E(strip.Rle[1]) + E(strip.Wstrip) + E(astrp) + E(strip.Rle[2]) + E(dihed) +
                      "  | Xle, Ave. Chord, Incidence (deg), Yle, Strip Width, Strip Area, Zle, Strip Dihed (deg)");
                l.Add(E(bst.ClLstrp) + E(bst.CdLstrp) + E(0) + E(bst.CnLstrp) + E(bst.CaLstrp) + E(bst.Cnc) + E(result.Trefftz.Dwwake[j]) + E(bst.CmleLstrp) + E(bst.Cmc4Lstrp) +
                      "  | cl, cd, cdv, cn, ca, cnc, wake dnwsh, cmLE, cm c/4");
                for (int ii = 0; ii < strip.Nvc; ii++)
                {
                    int i = strip.FirstVortex + ii;
                    var v = geo.Vortices[i];
                    double xm = 0.5 * (v.Rv1[0] + v.Rv2[0]);
                    double ym = 0.5 * (v.Rv1[1] + v.Rv2[1]);
                    double zm = 0.5 * (v.Rv1[2] + v.Rv2[2]);
                    l.Add(FortranI(i + 1, 4) + E(xm) + E(ym) + E(zm) + E(v.Dx) + E(v.Slopec) + E(t.Dcp[i]) + "  | I, X, Y, Z, DX, Slope, dCp");
                }
            }
        }
        return string.Join("\n", l);
    }

    /// <summary>MRFCNC: per-strip spanwise loading file.</summary>
    public static string MrfCnc(Geometry geo, CaseResult result)
    {
        var t = result.Totals;
        var l = new List<string>();
        l.Add("CNC");
        l.Add("VERSION 1.0");
        l.Add("Strip Loadings:  XM, YM, ZM, CNCM, CLM, CHM, DYM, ASM");
        l.Add(FortranI(geo.Strips.Count, 4) + "   | # strips");
        for (int j = 0; j < geo.Strips.Count; j++)
        {
            var strip = geo.Strips[j];
            var v = geo.Vortices[strip.FirstVortex];
            double xm = 0.5 * (v.Rv1[0] + v.Rv2[0]);
            double ym = 0.5 * (v.Rv1[1] + v.Rv2[1]);
            double zm = 0.5 * (v.Rv1[2] + v.Rv2[2]);
            double asm = strip.Wstrip * strip.Chord;
            l.Add(E(xm) + E(ym) + E(zm) + E(t.ByStrip[j].Cnc) + E(t.ByStrip[j].ClLstrp) + E(strip.Chord) + E(strip.Wstrip) + E(asm));
        }
        return string.Join("\n", l);
    }

    /// <summary>MRFVM: machine-readable strip shear/bending (same integration as OutVm).</summary>
    public static string MrfVm(Geometry geo, CaseResult result)
    {
        var t = result.Totals;
        double sref = geo.Sref, bref = geo.Bref;
        var l = new List<string>();
        l.Add("VM");
        l.Add("VERSION 1.0");
        l.Add("Shear/q and Bending Moment/q vs Y");
        l.Add(OutTotal.Slice(geo.Title, 60).PadRight(60));
        l.Add(E(result.Mach) + E(result.Alfa / DTR) + E(t.ClTot) + E(result.Beta / DTR) + E(sref) + E(bref) + "  | Mach, alpha, CLtot, beta, Sref, Bref");
        l.Add(I6(geo.Surfaces.Count) + "      | # surfaces");

        for (int n = 0; n < geo.Surfaces.Count; n++)
        {
            var js = new List<int>();
            for (int j = 0; j < geo.Strips.Count; j++) if (geo.Strips[j].SurfaceIndex == n) js.Add(j);
            if (js.Count == 0) continue;
            int m = js.Count;
            var surf = geo.Surfaces[n];

            l.Add("SURFACE");
            l.Add(surf.Title.PadRight(40));
            l.Add(FortranI(n + 1, 4) + " " + FortranI(m, 4) + "    | Surface #, # strips");

            double ymin = 1e10, ymax = -1e10;
            foreach (var j in js)
            {
                var s = geo.Strips[j];
                ymin = Math.Min(ymin, Math.Min(s.Rle1[1], s.Rle2[1]));
                ymax = Math.Max(ymax, Math.Max(s.Rle1[1], s.Rle2[1]));
            }

            var ystrp = new double[m + 1];
            var vArr = new double[m + 1];
            var bmArr = new double[m + 1];
            double cnclst = 0, bmlst = 0, wlst = 0, vlst = 0, dy = 0;
            for (int i = m - 1; i >= 0; i--)
            {
                int jj = i + 1;
                var s = geo.Strips[js[i]];
                double cnc = t.ByStrip[js[i]].Cnc;
                dy = 0.5 * (s.Wstrip + wlst);
                ystrp[jj] = s.Rle[1];
                vArr[jj] = vlst + 0.5 * (cnc + cnclst) * dy;
                bmArr[jj] = bmlst + 0.5 * (vArr[jj] + vlst) * dy;
                vlst = vArr[jj]; bmlst = bmArr[jj]; cnclst = cnc; wlst = s.Wstrip;
            }
            double vroot = vlst + cnclst * 0.5 * dy;
            double bmroot = bmlst + 0.5 * (vroot + vlst) * 0.5 * dy;
            double yroot, ytip;
            if (surf.ImageSign >= 0) { yroot = geo.Strips[js[0]].Rle1[1]; ytip = geo.Strips[js[m - 1]].Rle2[1]; }
            else { yroot = geo.Strips[js[0]].Rle2[1]; ytip = geo.Strips[js[m - 1]].Rle1[1]; }
            double dir = (ymin + ymax < 0.0) ? -1.0 : 1.0;

            l.Add(E(2.0 * ymin / bref) + E(2.0 * ymax / bref) + "  | 2Ymin/Bref, 2Ymax/Bref");
            l.Add(E(2.0 * yroot / bref) + E(vroot / sref) + E(dir * bmroot / sref / bref) + "  | 2Y/Bref, Vz/(q*Sref), Mx/(q*Bref*Sref) : root");
            for (int jj = 1; jj <= m; jj++)
                l.Add(E(2.0 * ystrp[jj] / bref) + E(vArr[jj] / sref) + E(dir * bmArr[jj] / sref / bref) + "  | 2Y/Bref, Vz/(q*Sref), Mx/(q*Bref*Sref)");
            l.Add(E(2.0 * ytip / bref) + E(0.0 / sref) + E(dir * 0.0 / sref / bref) + "  | 2Y/Bref, Vz/(q*Sref), Mx/(q*Bref*Sref) : tip");
        }
        return string.Join("\n", l);
    }
}
