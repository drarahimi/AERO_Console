// Port of ainput.f SUBROUTINE INPUT -- the .avl geometry file keyword parser.
//
// Scope note: BODY blocks are not ported yet (no Milestone 1/2 fixture uses
// them). NACA-designator camberlines fall back to a flat placeholder --
// AFIL (external file) and AIRFOIL (inline coordinates) run the real
// GETCAM+AKIMA camber-extraction pipeline via Airfoil.cs.
//
// Port of packages/avl-core/src/geometry/avlInputParser.ts.

namespace Avl.Core.Model;

public sealed class ParseOptions
{
    /// <summary>Resolves an AFIL filename to its file contents, or null if not found
    /// (matching ainput.f's "Airfoil file not found -- using default zero-camber
    /// airfoil" fallback in that case).</summary>
    public Func<string, string?>? ResolveAirfoilFile;
}

public static class AvlInputParser
{
    private const double DTR = Math.PI / 180.0;

    private static AirfoilData FlatAirfoil() => new()
    {
        Xasec = new List<double> { 0, 1 },
        Sasec = new List<double> { 0, 0 },
        Tasec = new List<double> { 0, 0 },
    };

    /// <summary>Port of ainput.f's post-GETCAM loop (AFIL/AIRFOIL branches): resamples
    /// the camberline at NASEC stations within [xfmin,xfmax] via the local AKIMA
    /// interpolator, then normalizes x/c to [0,1] (NRMLIZ).</summary>
    private static AirfoilData AirfoilFromBoundary(List<double> xb, List<double> yb, int nb, double xfmin, double xfmax)
    {
        int nin = Math.Min(50, nb);
        var cam = AirfoilCamber.Getcam(xb, yb, nb, nin);
        int nasec = cam.N;
        var xin = cam.Xc.ToList();
        var yin = cam.Yc.ToList();
        var tin = cam.Tc.ToList();

        var xasec = new double[nasec];
        var sasec = new double[nasec];
        var tasec = new double[nasec];
        for (int i = 1; i <= nasec; i++)
        {
            double xf = xfmin + (xfmax - xfmin) * ((double)(i - 1) / (nasec - 1));
            double xq = xin[0] + xf * (xin[nasec - 1] - xin[0]);
            xasec[i - 1] = xq;
            sasec[i - 1] = Akima.Compute(xin, yin, nasec, xq).Slp;
            tasec[i - 1] = Akima.Compute(xin, tin, nasec, xq).Yy;
        }
        // NRMLIZ
        double dx = xasec[nasec - 1] - xasec[0];
        if (dx == 0) dx = 1.0;
        double x1 = xasec[0];
        for (int i = 0; i < nasec; i++) xasec[i] = (xasec[i] - x1) / dx;

        return new AirfoilData { Xasec = xasec.ToList(), Sasec = sasec.ToList(), Tasec = tasec.ToList() };
    }

    private sealed class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
    }

    public static Geometry ParseAvlGeometry(string text, ParseOptions? options = null)
    {
        options ??= new ParseOptions();
        var it = new LineIterator(text);

        var geo = new Geometry
        {
            Title = "",
            Mach0 = 0,
            IYsym = 0,
            IZsym = 0,
            ZSym = 0,
            Sref = 1,
            Cref = 1,
            Bref = 1,
            Xyzref0 = new double[] { 0, 0, 0 },
            Cdoref0 = 0,
        };

        string RequireLine()
        {
            var l = it.Next();
            if (l == null) throw new ParseException($"Unexpected EOF at line {it.LineNumber}");
            return l;
        }

        geo.Title = RequireLine();
        geo.Mach0 = FortranText.GetFlt(RequireLine(), 1)[0];

        {
            var v = FortranText.GetFlt(RequireLine(), 3);
            double iy = v[0], iz = v[1], zs = v[2];
            geo.IYsym = iy > 0 ? 1 : iy < 0 ? -1 : 0;
            geo.IZsym = iz > 0 ? 1 : iz < 0 ? -1 : 0;
            geo.ZSym = zs;
        }

        {
            var v = FortranText.GetFlt(RequireLine(), 3);
            double sref = v[0], cref = v[1], bref = v[2];
            geo.Sref = sref <= 0 ? 1 : sref;
            geo.Cref = cref <= 0 ? 1 : cref;
            geo.Bref = bref <= 0 ? 1 : bref;
        }

        {
            var v = FortranText.GetFlt(RequireLine(), 3);
            geo.Xyzref0 = new double[] { v[0], v[1], v[2] };
        }

        // Optional CDoref line: try to parse, otherwise treat as first keyword line.
        string? pendingLine = null;
        {
            string line = RequireLine();
            var parsed = FortranText.TryGetFlt(line, 1);
            if (parsed != null && parsed.Count >= 1)
            {
                geo.Cdoref0 = parsed[0];
            }
            else
            {
                geo.Cdoref0 = 0;
                pendingLine = line;
            }
        }

        Surface? curSurface = null;
        Section? curSection = null;
        bool ldupl = false;
        double yDupl = 0;
        BodyBuild? curBody = null;

        void FinishSurface()
        {
            if (curSurface != null)
            {
                int srcIndex = geo.Surfaces.Count;
                geo.Surfaces.Add(curSurface);
                if (ldupl)
                {
                    geo.Surfaces.Add(MakeDuplicatePlaceholder(curSurface, srcIndex, yDupl));
                }
            }
            curSurface = null;
            curSection = null;
        }

        void FinishBody()
        {
            if (curBody != null && curBody.Xbod != null)
            {
                var b = MakeBody.Make(curBody.Title, curBody.Nvb, curBody.Bspace, curBody.Scale, curBody.Translate,
                    curBody.Xbod, curBody.Ybod!, curBody.Tbod!);
                geo.Bodies.Add(b);
                if (curBody.Ydup != null) geo.Bodies.Add(MakeBody.Duplicate(b, curBody.Ydup.Value));
            }
            curBody = null;
        }

        string? NextKeywordLine()
        {
            if (pendingLine != null)
            {
                var l = pendingLine;
                pendingLine = null;
                return l;
            }
            return it.Next();
        }

        string? line2 = NextKeywordLine();
        while (line2 != null)
        {
            string line = line2;
            string keywd = (line.Length >= 4 ? line.Substring(0, 4) : line).ToUpperInvariant();

            if (keywd == "SURF")
            {
                FinishSurface();
                FinishBody();
                string title = RequireLine();
                string spacingLine = RequireLine();
                var nums = FortranText.GetFlt(spacingLine, 4);
                int nvc = (int)(nums[0] + 0.001);
                double cspace = nums[1];
                int nvs = nums.Count >= 4 ? (int)(nums[2] + 0.001) : 0;
                double sspace = nums.Count >= 4 ? nums[3] : 0;
                curSurface = new Surface
                {
                    Title = title,
                    Nvc = nvc,
                    Cspace = cspace,
                    Nvs = nvs,
                    Sspace = sspace,
                    YDuplicate = null,
                    Angle = 0,
                    Translate = new double[] { 0, 0, 0 },
                    Scale = new double[] { 1, 1, 1 },
                    NoWake = false,
                    NoAlbe = false,
                    NoLoad = false,
                    Component = geo.Surfaces.Count + 1,
                    IsDuplicateImage = false,
                    ImageSign = 1,
                };
                ldupl = false;
                curSection = null;
            }
            else if (keywd == "YDUP")
            {
                if (curBody != null)
                {
                    curBody.Ydup = FortranText.GetFlt(RequireLine(), 1)[0];
                }
                else if (curSurface != null)
                {
                    yDupl = FortranText.GetFlt(RequireLine(), 1)[0];
                    ldupl = true;
                    curSurface.YDuplicate = yDupl;
                }
                else
                {
                    RequireLine();
                }
            }
            else if (keywd == "INDE" || keywd == "COMP")
            {
                if (curSurface != null) curSurface.Component = (int)FortranText.GetFlt(RequireLine(), 1)[0];
                else RequireLine();
            }
            else if (keywd == "SCAL")
            {
                var v = FortranText.GetFlt(RequireLine(), 3);
                if (curBody != null) curBody.Scale = new double[] { v[0], v[1], v[2] };
                else if (curSurface != null) curSurface.Scale = new double[] { v[0], v[1], v[2] };
            }
            else if (keywd == "TRAN")
            {
                var v = FortranText.GetFlt(RequireLine(), 3);
                if (curBody != null) curBody.Translate = new double[] { v[0], v[1], v[2] };
                else if (curSurface != null) curSurface.Translate = new double[] { v[0], v[1], v[2] };
            }
            else if (keywd == "ANGL" || keywd == "AINC")
            {
                double a = FortranText.GetFlt(RequireLine(), 1)[0];
                if (curSurface != null) curSurface.Angle = a * DTR;
            }
            else if (keywd == "NOWA")
            {
                if (curSurface != null) curSurface.NoWake = true;
            }
            else if (keywd == "NOAL")
            {
                if (curSurface != null) curSurface.NoAlbe = true;
            }
            else if (keywd == "NOLO")
            {
                if (curSurface != null) curSurface.NoLoad = true;
            }
            else if (keywd == "SECT")
            {
                var nums = FortranText.GetFlt(RequireLine(), 7);
                if (nums.Count < 5) throw new ParseException($"SECTION needs >=5 numbers at line {it.LineNumber}");
                curSection = new Section
                {
                    Xle = new double[] { nums[0], nums[1], nums[2] },
                    Chord = nums[3],
                    Ainc = nums[4] * DTR,
                    Nspan = nums.Count >= 7 ? (int)(nums[5] + 0.001) : 0,
                    Sspace = nums.Count >= 7 ? nums[6] : 0,
                    Clcd = new double[] { 0, 0, 0, 0, 0, 0 },
                    Claf = 1.0,
                    Airfoil = FlatAirfoil(),
                };
                if (curSurface != null) curSurface.Sections.Add(curSection);
            }
            else if (keywd == "NACA")
            {
                var (xfmin, xfmax) = ParseXfLimits(line);
                int ides = (int)FortranText.GetFlt(RequireLine(), 1)[0];
                if (curSection != null) curSection.Airfoil = NacaCamberline(ides, xfmin, xfmax);
            }
            else if (keywd == "AIRF")
            {
                var (xfmin, xfmax) = ParseXfLimits(line);
                var (xb, yb) = ReadAirfoilInline(it);
                if (curSection != null)
                {
                    curSection.Airfoil = xb.Count >= 3 ? AirfoilFromBoundary(xb, yb, xb.Count, xfmin, xfmax) : FlatAirfoil();
                }
            }
            else if (keywd == "AFIL")
            {
                var (xfmin, xfmax) = ParseXfLimits(line);
                string filename = RequireLine().Trim();
                if (curSection != null)
                {
                    curSection.AfileName = filename;
                    string? contents = options.ResolveAirfoilFile?.Invoke(filename);
                    if (contents == null)
                    {
                        curSection.Airfoil = FlatAirfoil(); // matches ainput.f's "Airfoil file not found" fallback
                    }
                    else
                    {
                        var (xb, yb) = ParseAirfoilCoordFile(contents);
                        curSection.Airfoil = xb.Count >= 3 ? AirfoilFromBoundary(xb, yb, xb.Count, xfmin, xfmax) : FlatAirfoil();
                    }
                }
            }
            else if (keywd == "CDCL")
            {
                var nums = FortranText.GetFlt(RequireLine(), 6);
                var cls = new[] { nums[0], nums[2], nums[4] };
                var cds = new[] { nums[1], nums[3], nums[5] };
                int lmax = 0, lmin = 0;
                for (int l = 1; l < 3; l++)
                {
                    if (cls[l] > cls[lmax]) lmax = l;
                    if (cls[l] < cls[lmin]) lmin = l;
                }
                int lmid = 3 - (lmin + lmax); // 0-based analog of Fortran's "6 - (LMIN+LMAX)"
                var clcd = new double[]
                {
                    cls[lmin], cds[lmin], cls[lmid], cds[lmid], cls[lmax], cds[lmax],
                };
                if (curSection != null) curSection.Clcd = clcd;
            }
            else if (keywd == "CLAF")
            {
                double v = FortranText.GetFlt(RequireLine(), 1)[0];
                if (curSection != null) curSection.Claf = v <= 0 || v >= 2 ? 1.0 : v;
            }
            else if (keywd == "CONT")
            {
                string l = RequireLine();
                int spaceIdx = l.IndexOf(' ');
                string name = spaceIdx > 0 ? l.Substring(0, spaceIdx) : l;
                if (!geo.ControlNames.Contains(name)) geo.ControlNames.Add(name);
                var nums = FortranText.GetFlt(spaceIdx > 0 ? l.Substring(spaceIdx + 1) : "", 6);
                var ctrl = new ControlDeclaration
                {
                    Name = name,
                    Gain = nums.Count >= 1 ? nums[0] : 1.0,
                    XHinge = nums.Count >= 2 ? nums[1] : 0.0,
                    HingeVec = nums.Count >= 5 ? new double[] { nums[2], nums[3], nums[4] } : new double[] { 0, 0, 0 },
                    Refl = nums.Count >= 6 ? nums[5] : 1.0,
                };
                if (curSection != null) curSection.Controls.Add(ctrl);
            }
            else if (keywd == "DESI")
            {
                string l = RequireLine();
                int spaceIdx = l.IndexOf(' ');
                string name = spaceIdx > 0 ? l.Substring(0, spaceIdx) : l;
                if (!geo.DesignNames.Contains(name)) geo.DesignNames.Add(name);
                var nums = FortranText.GetFlt(spaceIdx > 0 ? l.Substring(spaceIdx + 1) : "", 1);
                var des = new DesignDeclaration { Name = name, Gain = nums.Count >= 1 ? nums[0] : 1.0 };
                if (curSection != null) curSection.Designs.Add(des);
            }
            else if (keywd == "BODY")
            {
                FinishSurface();
                FinishBody();
                string title = RequireLine();
                var nums = FortranText.GetFlt(RequireLine(), 2);
                curBody = new BodyBuild
                {
                    Title = title,
                    Nvb = (int)(nums[0] + 0.001),
                    Bspace = nums.Count >= 2 ? nums[1] : 0,
                    Scale = new double[] { 1, 1, 1 },
                    Translate = new double[] { 0, 0, 0 },
                };
                curSurface = null;
                curSection = null;
            }
            else if (keywd == "BFIL")
            {
                string filename = RequireLine().Trim();
                if (curBody != null)
                {
                    string? contents = options.ResolveAirfoilFile?.Invoke(filename);
                    if (contents != null)
                    {
                        var (xb, yb) = ParseAirfoilCoordFile(contents);
                        if (xb.Count >= 3)
                        {
                            int nbod = Math.Min(50, xb.Count);
                            var cam = AirfoilCamber.Getcam(xb, yb, xb.Count, nbod, lnorm: false);
                            curBody.Xbod = cam.Xc.ToList();
                            curBody.Ybod = cam.Yc.ToList();
                            curBody.Tbod = cam.Tc.ToList();
                        }
                    }
                }
            }
            else if (keywd == "CORE")
            {
                RequireLine();
            }
            else if (keywd == "EOF ")
            {
                break;
            }
            else
            {
                // unrecognized line: ignored, matching ainput.f's fallthrough behavior
            }

            line2 = NextKeywordLine();
        }
        FinishSurface();
        FinishBody();

        BuildAllStrips(geo);

        return geo;
    }

    /// <summary>Accumulator for a BODY block while its keywords stream in.</summary>
    private sealed class BodyBuild
    {
        public string Title = "";
        public int Nvb;
        public double Bspace;
        public double[] Scale = { 1, 1, 1 };
        public double[] Translate = { 0, 0, 0 };
        public double? Ydup;
        public List<double>? Xbod;
        public List<double>? Ybod;
        public List<double>? Tbod;
    }

    /// <summary>Port of the x/c-limit lookahead used by NACA/AIRFOIL/AFIL/BFIL:
    /// optional "xfmin xfmax" trailing the keyword on the same line.</summary>
    private static (double xfmin, double xfmax) ParseXfLimits(string keywordLine)
    {
        int spaceIdx = keywordLine.IndexOf(' ');
        if (spaceIdx < 0) return (0, 1);
        string rest = keywordLine.Substring(spaceIdx + 1).Trim();
        if (rest.Length == 0) return (0, 1);
        var nums = FortranText.TryGetFlt(rest, 2);
        if (nums == null || nums.Count < 2) return (0, 1);
        return (Math.Max(0, nums[0]), Math.Min(1, nums[1]));
    }

    /// <summary>Port of ainput.f's AIRF branch: reads consecutive "x y" lines until a
    /// non-numeric line is hit, which is pushed back for the outer keyword loop.</summary>
    private static (List<double> xb, List<double> yb) ReadAirfoilInline(LineIterator it)
    {
        var xb = new List<double>();
        var yb = new List<double>();
        for (; ; )
        {
            var line = it.Next();
            if (line == null) break;
            var nums = FortranText.TryGetFlt(line, 2);
            if (nums == null || nums.Count < 2)
            {
                it.PushBack(line);
                break;
            }
            xb.Add(nums[0]);
            yb.Add(nums[1]);
        }
        return (xb, yb);
    }

    /// <summary>Parses a plain airfoil coordinate file (title line, then "x y" pairs).</summary>
    private static (List<double> xb, List<double> yb) ParseAirfoilCoordFile(string contents)
    {
        var lines = System.Text.RegularExpressions.Regex.Split(contents, "\r\n|\r|\n");
        var xb = new List<double>();
        var yb = new List<double>();
        foreach (var raw in lines)
        {
            var nums = FortranText.TryGetFlt(raw, 2);
            if (nums != null && nums.Count == 2)
            {
                xb.Add(nums[0]);
                yb.Add(nums[1]);
            }
        }
        return (xb, yb);
    }

    /// <summary>Port of ainput.f's NACA branch: classic 4-digit thin-airfoil-theory
    /// camberline, sampled directly (no spline needed).</summary>
    private static AirfoilData NacaCamberline(int idesignator, double xfmin, double xfmax)
    {
        int icam = idesignator / 1000;
        int ipos = (idesignator - 1000 * icam) / 100;
        int ithk = idesignator - 1000 * icam - 100 * ipos;
        double c = icam / 100.0;
        double p = ipos / 10.0;
        double t = ithk / 100.0;

        int nasec = 50;
        var xasec = new double[nasec];
        var sasec = new double[nasec];
        var tasec = new double[nasec];
        for (int i = 1; i <= nasec; i++)
        {
            double xf = xfmin + (xfmax - xfmin) * ((double)(i - 1) / (nasec - 1));
            double slp;
            if (p == 0) slp = 0;
            else if (xf < p) slp = (c * 2.0 * (p - xf)) / (p * p);
            else if (xf > p) slp = (c * 2.0 * (p - xf)) / ((1.0 - p) * (1.0 - p));
            else slp = 0;
            double thk = (0.2969 * Math.Sqrt(xf) - 0.126 * xf - 0.3516 * xf * xf + 0.2843 * xf * xf * xf - 0.1015 * xf * xf * xf * xf) * t * 10.0;
            xasec[i - 1] = xf;
            sasec[i - 1] = slp;
            tasec[i - 1] = thk;
        }
        double dx = xasec[nasec - 1] - xasec[0];
        if (dx == 0) dx = 1.0;
        double x1 = xasec[0];
        for (int i = 0; i < nasec; i++) xasec[i] = (xasec[i] - x1) / dx;

        return new AirfoilData { Xasec = xasec.ToList(), Sasec = sasec.ToList(), Tasec = tasec.ToList() };
    }

    /// <summary>Placeholder pushed at the position SDUPL would insert the image surface;
    /// its strips are populated later by DuplicateSurfaceStrips().</summary>
    private static Surface MakeDuplicatePlaceholder(Surface src, int srcIndex, double yDupl) => new()
    {
        Title = $"{src.Title} (YDUP)",
        Nvc = src.Nvc,
        Cspace = src.Cspace,
        Nvs = src.Nvs,
        Sspace = src.Sspace,
        YDuplicate = null,
        Angle = src.Angle,
        Translate = src.Translate,
        Scale = src.Scale,
        NoWake = src.NoWake,
        NoAlbe = src.NoAlbe,
        NoLoad = src.NoLoad,
        Component = src.Component,
        Sections = src.Sections, // shared reference, matching the JS spread
        IsDuplicateImage = true,
        ImageSign = -src.ImageSign,
        DuplicateOf = srcIndex,
        YMirror = yDupl,
    };

    private static void BuildAllStrips(Geometry geo)
    {
        for (int i = 0; i < geo.Surfaces.Count; i++)
        {
            var surf = geo.Surfaces[i];
            if (surf.IsDuplicateImage && surf.DuplicateOf != null)
            {
                MakeSurface.DuplicateSurfaceStrips(geo, surf.DuplicateOf.Value, i, surf.YMirror!.Value);
            }
            else
            {
                MakeSurface.MakeSurfaceStrips(geo, i);
            }
        }
    }
}
