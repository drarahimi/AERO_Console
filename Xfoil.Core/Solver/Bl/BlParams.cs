// Port of BLPAR.INC constants (set in xbl.f BLPINI). These tune the turbulent
// closure: shear-lag constant, G-beta locus, wall term, dissipation-length ratio,
// and the transition-region constants. Defaults match XFOIL 6.99.

namespace Xfoil.Core.Solver.Bl;

public sealed record BlParams
{
    public double Sccon { get; init; } = 5.6;   // shear coefficient lag constant
    public double Gacon { get; init; } = 6.70;  // G-beta locus constant
    public double Gbcon { get; init; } = 0.75;  // G-beta locus constant
    public double Gccon { get; init; } = 18.0;  // wall term
    public double Dlcon { get; init; } = 0.9;   // wall/wake dissipation length ratio
    public double Ctrcon { get; init; } = 1.8;  // transition-region constant
    public double Ctrcex { get; init; } = 3.3;  // transition-region exponent
    public double Duxcon { get; init; } = 1.0;

    /// <summary>Ctau weighting coefficient, CTCON = 0.5/(GACON^2 * GBCON).</summary>
    public double Ctcon => 0.5 / (Gacon * Gacon * Gbcon);
}
