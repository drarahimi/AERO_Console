// Design-variable changes (the OPER "DE" command). A design variable perturbs
// the incidence of every section that declares it: d(ainc)/d(DELDES_n) = gain*DTR
// (amake.f: CHSIN_G/CHCOS_G/AINC_G are all incidence sensitivities -- design
// variables in this AVL version only rotate section incidence).
//
// AVL applies this as a linearized GAM_G correction to the circulation. This port
// instead re-panels the geometry with the perturbed section incidences and
// re-solves -- physically exact, and identical to AVL's linearization to displayed
// precision for the small changes DE is used with.
//
// Ported directly from avl3.52 Fortran (no TypeScript predecessor).

namespace Avl.Core.Model;

public static class DesignPerturbation
{
    private const double DTR = Math.PI / 180.0;

    private static Section CloneSection(Section s) => new()
    {
        Xle = s.Xle,
        Chord = s.Chord,
        Ainc = s.Ainc,
        Nspan = s.Nspan,
        Sspace = s.Sspace,
        Clcd = s.Clcd,
        Claf = s.Claf,
        Airfoil = s.Airfoil,   // not mutated by paneling -- safe to share
        Controls = s.Controls,
        Designs = s.Designs,
    };

    private static Surface CloneSurface(Surface s) => new()
    {
        Title = s.Title,
        Nvc = s.Nvc,
        Cspace = s.Cspace,
        Nvs = s.Nvs,
        Sspace = s.Sspace,
        YDuplicate = s.YDuplicate,
        Angle = s.Angle,
        Translate = s.Translate,
        Scale = s.Scale,
        NoWake = s.NoWake,
        NoAlbe = s.NoAlbe,
        NoLoad = s.NoLoad,
        Component = s.Component,
        Sections = s.Sections.Select(CloneSection).ToList(),
        IsDuplicateImage = s.IsDuplicateImage,
        ImageSign = s.ImageSign,
        DuplicateOf = s.DuplicateOf,
        YMirror = s.YMirror,
    };

    /// <summary>Returns a copy of <paramref name="geo"/> with each design variable's
    /// incidence perturbation applied and strips/vortices rebuilt. If all deltas are
    /// zero (or none declared), returns the original geometry unchanged.</summary>
    public static Geometry Apply(Geometry geo, IReadOnlyList<double> deldes)
    {
        if (geo.DesignNames.Count == 0 || deldes == null || deldes.All(d => d == 0)) return geo;

        var g = new Geometry
        {
            Title = geo.Title,
            Mach0 = geo.Mach0,
            IYsym = geo.IYsym,
            IZsym = geo.IZsym,
            ZSym = geo.ZSym,
            Sref = geo.Sref,
            Cref = geo.Cref,
            Bref = geo.Bref,
            Xyzref0 = geo.Xyzref0,
            Cdoref0 = geo.Cdoref0,
            Surfaces = geo.Surfaces.Select(CloneSurface).ToList(),
            ControlNames = geo.ControlNames,
            DesignNames = geo.DesignNames,
        };

        // Perturb section incidences: ainc += gain * DELDES * DTR per matching design.
        foreach (var surf in g.Surfaces)
        {
            foreach (var sec in surf.Sections)
            {
                foreach (var d in sec.Designs)
                {
                    int idx = g.DesignNames.IndexOf(d.Name);
                    if (idx < 0 || idx >= deldes.Count) continue;
                    sec.Ainc += d.Gain * deldes[idx] * DTR;
                }
            }
        }

        // Rebuild paneling (mirrors AvlInputParser.BuildAllStrips).
        for (int i = 0; i < g.Surfaces.Count; i++)
        {
            var surf = g.Surfaces[i];
            if (surf.IsDuplicateImage && surf.DuplicateOf != null)
                MakeSurface.DuplicateSurfaceStrips(g, surf.DuplicateOf.Value, i, surf.YMirror!.Value);
            else
                MakeSurface.MakeSurfaceStrips(g, i);
        }

        return g;
    }
}
