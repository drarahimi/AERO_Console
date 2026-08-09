// One boundary-layer station's state: the primary variables plus every secondary
// variable and its sensitivities, mirroring the VAR1/VAR2 common blocks in XBL.INC.
// XFOIL names these HK2, HK2_U2, ... for the "2" station; here the trailing "2" is
// dropped so the same type serves either station (station 1 is a copy of station 2
// shifted upstream). Sensitivity suffixes map as: _U2->_U, _T2->_T, _D2->_D, _S2->_S,
// _MS->_Ms (d/dMach^2), _RE->_Re (d/dReynolds).

namespace Xfoil.Core.Solver.Bl;

public sealed class BlStation
{
    // Primary variables: streamwise coord, edge speed, momentum thickness, displacement
    // thickness, sqrt(Ctau) shear coefficient, and amplification ratio.
    public double X, U, T, D, S, Ampl;
    public double U_Uei, U_Ms, Dw;

    // Shape parameter H = D/T
    public double H, H_T, H_D;
    // Edge Mach^2
    public double M, M_U, M_Ms;
    // Edge density
    public double R, R_U, R_Ms;
    // Viscosity (kinematic, normalized)
    public double V, V_U, V_Ms, V_Re;
    // Kinematic shape parameter
    public double Hk, Hk_U, Hk_T, Hk_D, Hk_Ms;
    // KE-thickness shape parameter
    public double Hs, Hs_U, Hs_T, Hs_D, Hs_Ms, Hs_Re;
    // Density shape parameter
    public double Hc, Hc_U, Hc_T, Hc_D, Hc_Ms;
    // Momentum-thickness Reynolds number
    public double Rt, Rt_U, Rt_T, Rt_Ms, Rt_Re;
    // Skin friction
    public double Cf, Cf_U, Cf_T, Cf_D, Cf_Ms, Cf_Re;
    // Dissipation function 2 CD/H*
    public double Di, Di_U, Di_T, Di_D, Di_S, Di_Ms, Di_Re;
    // Normalized slip velocity Us
    public double Us, Us_U, Us_T, Us_D, Us_Ms, Us_Re;
    // Equilibrium shear coefficient (Ctau)eq^1/2
    public double Cq, Cq_U, Cq_T, Cq_D, Cq_Ms, Cq_Re;
    // BL thickness Delta
    public double De, De_U, De_T, De_D, De_Ms;

    /// <summary>Deep copy (all fields are value-type doubles). Mirrors saving the
    /// COM1/COM2 array in XFOIL's TRCHEK2/TRDIF.</summary>
    public BlStation Clone() => (BlStation)MemberwiseClone();

    /// <summary>Restore every field from a snapshot (mirrors COM2 = C2SAV).</summary>
    public void CopyFrom(BlStation o)
    {
        X = o.X; U = o.U; T = o.T; D = o.D; S = o.S; Ampl = o.Ampl;
        U_Uei = o.U_Uei; U_Ms = o.U_Ms; Dw = o.Dw;
        H = o.H; H_T = o.H_T; H_D = o.H_D;
        M = o.M; M_U = o.M_U; M_Ms = o.M_Ms;
        R = o.R; R_U = o.R_U; R_Ms = o.R_Ms;
        V = o.V; V_U = o.V_U; V_Ms = o.V_Ms; V_Re = o.V_Re;
        Hk = o.Hk; Hk_U = o.Hk_U; Hk_T = o.Hk_T; Hk_D = o.Hk_D; Hk_Ms = o.Hk_Ms;
        Hs = o.Hs; Hs_U = o.Hs_U; Hs_T = o.Hs_T; Hs_D = o.Hs_D; Hs_Ms = o.Hs_Ms; Hs_Re = o.Hs_Re;
        Hc = o.Hc; Hc_U = o.Hc_U; Hc_T = o.Hc_T; Hc_D = o.Hc_D; Hc_Ms = o.Hc_Ms;
        Rt = o.Rt; Rt_U = o.Rt_U; Rt_T = o.Rt_T; Rt_Ms = o.Rt_Ms; Rt_Re = o.Rt_Re;
        Cf = o.Cf; Cf_U = o.Cf_U; Cf_T = o.Cf_T; Cf_D = o.Cf_D; Cf_Ms = o.Cf_Ms; Cf_Re = o.Cf_Re;
        Di = o.Di; Di_U = o.Di_U; Di_T = o.Di_T; Di_D = o.Di_D; Di_S = o.Di_S; Di_Ms = o.Di_Ms; Di_Re = o.Di_Re;
        Us = o.Us; Us_U = o.Us_U; Us_T = o.Us_T; Us_D = o.Us_D; Us_Ms = o.Us_Ms; Us_Re = o.Us_Re;
        Cq = o.Cq; Cq_U = o.Cq_U; Cq_T = o.Cq_T; Cq_D = o.Cq_D; Cq_Ms = o.Cq_Ms; Cq_Re = o.Cq_Re;
        De = o.De; De_U = o.De_U; De_T = o.De_T; De_D = o.De_D; De_Ms = o.De_Ms;
    }
}
