// Port of xblsys.f BLPRV, BLSYS and TESYS: the per-station glue that sets the "2"
// primary variables from the marching/Newton unknowns (BLPRV), assembles the interval
// system by dispatching to BLVAR/BLMID/BLDIF/TRDIF and converting to incompressible Uei
// (BLSYS), and builds the dummy TE->wake continuity system (TESYS).

namespace Xfoil.Core.Solver.Bl;

public sealed partial class BlInterval
{
    /// <summary>Port of BLPRV: set station "2" primary variables from the parameter list,
    /// including the compressible edge speed U2 from the incompressible Uei via the
    /// Karman-Tsien parameter TKBL.</summary>
    public void Blprv(double xsi, double ami, double cti, double thi, double dsi, double dswaki, double uei)
    {
        var b = S2;
        b.X = xsi; b.Ampl = ami; b.S = cti; b.T = thi; b.D = dsi - dswaki; b.Dw = dswaki;

        double tkbl = Env.Tkbl, qinf = Env.Qinfbl, tkblMs = Env.TkblMs;
        double uq2 = (uei / qinf) * (uei / qinf);
        b.U = uei * (1.0 - tkbl) / (1.0 - tkbl * uq2);
        b.U_Uei = (1.0 + tkbl * (2.0 * b.U * uei / (qinf * qinf) - 1.0)) / (1.0 - tkbl * uq2);
        b.U_Ms = (b.U * uq2 - uei) * tkblMs / (1.0 - tkbl * uq2);
    }

    /// <summary>Port of BLSYS: assemble the current interval's Newton system and convert
    /// the edge-velocity derivatives from compressible Uec to incompressible Uei. Uses the
    /// SIMI/TRAN/TURB/WAKE flags set by the caller.</summary>
    public void Blsys()
    {
        int ityp = Wake ? 3 : ((Turb || Tran) ? 2 : 1);
        BlSys.Blvar(S2, Env, P, ityp);
        Blmid(ityp);

        if (Simi) S1.CopyFrom(S2); // "1" == "2" at the similarity station

        if (Tran) Trdif();
        else if (Simi) Bldif(0);
        else if (!Turb) Bldif(1);
        else if (Wake) Bldif(3);
        else Bldif(2);

        if (Simi)
            for (int k = 0; k < 4; k++)
                for (int l = 0; l < 5; l++) { Vs2[k, l] = Vs1[k, l] + Vs2[k, l]; Vs1[k, l] = 0.0; }

        // convert residual derivatives wrt compressible U into incompressible Uei + Mach
        for (int k = 0; k < 4; k++)
        {
            double resU1 = Vs1[k, 3];
            double resU2 = Vs2[k, 3];
            double resMs = Vsm[k];
            Vs1[k, 3] = resU1 * S1.U_Uei;
            Vs2[k, 3] = resU2 * S2.U_Uei;
            Vsm[k] = resU1 * S1.U_Ms + resU2 * S2.U_Ms + resMs;
        }
    }

    /// <summary>Port of TESYS: dummy BL system tying the airfoil TE to the first wake
    /// station (continuity of Ctau, Theta, Dstar+gap).</summary>
    public void Tesys(double cte, double tte, double dte)
    {
        ClearSystemPublic();
        BlSys.Blvar(S2, Env, P, 3);
        Vs1[0, 0] = -1.0; Vs2[0, 0] = 1.0; Vsrez[0] = cte - S2.S;
        Vs1[1, 1] = -1.0; Vs2[1, 1] = 1.0; Vsrez[1] = tte - S2.T;
        Vs1[2, 2] = -1.0; Vs2[2, 2] = 1.0; Vsrez[2] = dte - S2.D - S2.Dw;
    }

    private void ClearSystemPublic()
    {
        for (int k = 0; k < 4; k++)
        {
            Vsrez[k] = 0; Vsm[k] = 0; Vsr[k] = 0; Vsx[k] = 0;
            for (int l = 0; l < 5; l++) { Vs1[k, l] = 0; Vs2[k, l] = 0; }
        }
    }
}
