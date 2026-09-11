using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace AERO_Console
{
    /// <summary>
    /// Provides curated benchmark airfoil profiles for aerospace coursework,
    /// thin airfoil theory analytical calculations, and geometric flap deflection.
    /// </summary>
    public static class AirfoilLibrary
    {
        public record PresetAirfoil(string Name, string Category, string CodeOrFilename, string Description);

        public static readonly List<PresetAirfoil> Presets = new()
        {
            new PresetAirfoil("NACA 0012", "Symmetric", "0012", "Classic 12% symmetric benchmark - zero camber, used on helicopter rotors, vertical stabilizers, and baseline tests."),
            new PresetAirfoil("NACA 0009", "Symmetric", "0009", "Thin 9% symmetric tailplane section with low profile drag."),
            new PresetAirfoil("NACA 0006", "Symmetric", "0006", "Very thin 6% supersonic/transonic fin section."),
            new PresetAirfoil("NACA 2412", "Cambered", "2412", "Standard light general aviation airfoil (2% camber at 40% chord, 12% thickness) - Cessna 172/182 baseline."),
            new PresetAirfoil("NACA 4412", "Cambered", "4412", "High cambered utility airfoil (4% camber at 40% chord) with high maximum lift."),
            new PresetAirfoil("NACA 6412", "High-Lift", "6412", "Extreme 6% camber high-lift profile for STOL and low-speed flight."),
            new PresetAirfoil("NACA 23012", "5-Digit", "23012", "Classic 5-digit reflexed section with forward camber for near-zero quarter-chord pitching moment (Cm ≈ -0.01)."),
            new PresetAirfoil("Clark-Y", "Trainer", "clarky", "Flat-bottomed classic trainer and propeller blade section - easy to build and forgiving stall."),
            new PresetAirfoil("NACA 63-415", "Laminar Flow", "63-415", "6-series laminar flow section designed with an extensive favorable pressure gradient bucket."),
            new PresetAirfoil("NACA 64-210", "Laminar Flow", "64-210", "Thin 10% laminar section for fast cruise and low drag."),
            new PresetAirfoil("Eppler 387", "Low Reynolds", "e387", "Benchmark sailplane/UAV low Reynolds number profile with gentle transition and low drag."),
            new PresetAirfoil("Selig S1223", "High-Lift UAV", "s1223", "High-lift low-Re profile capable of CL > 2.0 un-flapped, popular in SAE Aero Design competitions."),
            new PresetAirfoil("RAE 2822", "Transonic", "rae2822", "World standard supercritical/transonic benchmark airfoil for shock-boundary layer interaction testing."),
            new PresetAirfoil("Wortmann FX 63-137", "Sailplane", "fx63137", "High-performance glider section designed by Franz Wortmann for low profile drag at high lift.")
        };

        #region Embedded Non-NACA Coordinate Tables

        // Standard Selig-format normalized (x/c = 1.0 -> 0.0 -> 1.0) coordinates
        private static readonly Dictionary<string, (string Title, double[,] Coords)> _embeddedAirfoils = new(StringComparer.OrdinalIgnoreCase)
        {
            ["clarky"] = ("Clark-Y Flat Bottom Airfoil", new double[,]
            {
                { 1.0000, 0.0000 }, { 0.9500, 0.0147 }, { 0.9000, 0.0290 }, { 0.8000, 0.0560 }, { 0.7000, 0.0800 },
                { 0.6000, 0.0990 }, { 0.5000, 0.1130 }, { 0.4000, 0.1190 }, { 0.3000, 0.1170 }, { 0.2000, 0.1030 },
                { 0.1500, 0.0920 }, { 0.1000, 0.0770 }, { 0.0750, 0.0670 }, { 0.0500, 0.0550 }, { 0.0250, 0.0380 },
                { 0.0125, 0.0280 }, { 0.0000, 0.0000 }, { 0.0125, -0.0170 }, { 0.0250, -0.0220 }, { 0.0500, -0.0250 },
                { 0.0750, -0.0250 }, { 0.1000, -0.0240 }, { 0.1500, -0.0210 }, { 0.2000, -0.0160 }, { 0.3000, -0.0060 },
                { 0.4000, 0.0000 }, { 0.5000, 0.0000 }, { 0.6000, 0.0000 }, { 0.7000, 0.0000 }, { 0.8000, 0.0000 },
                { 0.9000, 0.0000 }, { 0.9500, 0.0000 }, { 1.0000, 0.0000 }
            }),
            ["e387"] = ("Eppler 387 Low Re Airfoil", new double[,]
            {
                { 1.0000, 0.0020 }, { 0.9500, 0.0129 }, { 0.9000, 0.0246 }, { 0.8000, 0.0487 }, { 0.7000, 0.0717 },
                { 0.6000, 0.0913 }, { 0.5000, 0.1051 }, { 0.4000, 0.1114 }, { 0.3000, 0.1087 }, { 0.2000, 0.0954 },
                { 0.1500, 0.0847 }, { 0.1000, 0.0706 }, { 0.0500, 0.0507 }, { 0.0250, 0.0361 }, { 0.0100, 0.0232 },
                { 0.0000, 0.0000 }, { 0.0100, -0.0160 }, { 0.0250, -0.0224 }, { 0.0500, -0.0280 }, { 0.1000, -0.0322 },
                { 0.1500, -0.0321 }, { 0.2000, -0.0300 }, { 0.3000, -0.0242 }, { 0.4000, -0.0177 }, { 0.5000, -0.0118 },
                { 0.6000, -0.0069 }, { 0.7000, -0.0033 }, { 0.8000, -0.0009 }, { 0.9000, 0.0002 }, { 0.9500, 0.0003 },
                { 1.0000, -0.0005 }
            }),
            ["s1223"] = ("Selig S1223 High Lift Low Re Airfoil", new double[,]
            {
                { 1.0000, 0.0022 }, { 0.9500, 0.0210 }, { 0.9000, 0.0410 }, { 0.8000, 0.0790 }, { 0.7000, 0.1120 },
                { 0.6000, 0.1370 }, { 0.5000, 0.1510 }, { 0.4000, 0.1520 }, { 0.3000, 0.1410 }, { 0.2000, 0.1170 },
                { 0.1500, 0.1010 }, { 0.1000, 0.0810 }, { 0.0500, 0.0550 }, { 0.0250, 0.0370 }, { 0.0100, 0.0220 },
                { 0.0000, 0.0000 }, { 0.0100, -0.0180 }, { 0.0250, -0.0230 }, { 0.0500, -0.0240 }, { 0.1000, -0.0180 },
                { 0.1500, -0.0080 }, { 0.2000, 0.0020 }, { 0.3000, 0.0180 }, { 0.4000, 0.0280 }, { 0.5000, 0.0330 },
                { 0.6000, 0.0320 }, { 0.7000, 0.0260 }, { 0.8000, 0.0170 }, { 0.9000, 0.0070 }, { 0.9500, 0.0020 },
                { 1.0000, -0.0022 }
            }),
            ["rae2822"] = ("RAE 2822 Supercritical Benchmark", new double[,]
            {
                { 1.0000, 0.0010 }, { 0.9500, 0.0118 }, { 0.9000, 0.0232 }, { 0.8000, 0.0465 }, { 0.7000, 0.0683 },
                { 0.6000, 0.0863 }, { 0.5000, 0.0984 }, { 0.4000, 0.1032 }, { 0.3000, 0.0998 }, { 0.2000, 0.0872 },
                { 0.1500, 0.0772 }, { 0.1000, 0.0638 }, { 0.0500, 0.0451 }, { 0.0250, 0.0314 }, { 0.0100, 0.0195 },
                { 0.0000, 0.0000 }, { 0.0100, -0.0163 }, { 0.0250, -0.0229 }, { 0.0500, -0.0287 }, { 0.1000, -0.0328 },
                { 0.1500, -0.0327 }, { 0.2000, -0.0305 }, { 0.3000, -0.0245 }, { 0.4000, -0.0179 }, { 0.5000, -0.0119 },
                { 0.6000, -0.0072 }, { 0.7000, -0.0039 }, { 0.8000, -0.0021 }, { 0.9000, -0.0012 }, { 0.9500, -0.0008 },
                { 1.0000, -0.0010 }
            }),
            ["fx63137"] = ("Wortmann FX 63-137 High Performance Sailplane", new double[,]
            {
                { 1.0000, 0.0010 }, { 0.9500, 0.0152 }, { 0.9000, 0.0298 }, { 0.8000, 0.0592 }, { 0.7000, 0.0870 },
                { 0.6000, 0.1105 }, { 0.5000, 0.1268 }, { 0.4000, 0.1332 }, { 0.3000, 0.1278 }, { 0.2000, 0.1102 },
                { 0.1500, 0.0967 }, { 0.1000, 0.0792 }, { 0.0500, 0.0552 }, { 0.0250, 0.0381 }, { 0.0100, 0.0235 },
                { 0.0000, 0.0000 }, { 0.0100, -0.0168 }, { 0.0250, -0.0232 }, { 0.0500, -0.0281 }, { 0.1000, -0.0298 },
                { 0.1500, -0.0261 }, { 0.2000, -0.0198 }, { 0.3000, -0.0062 }, { 0.4000, 0.0061 }, { 0.5000, 0.0142 },
                { 0.6000, 0.0168 }, { 0.7000, 0.0143 }, { 0.8000, 0.0089 }, { 0.9000, 0.0031 }, { 0.9500, 0.0009 },
                { 1.0000, -0.0010 }
            })
        };

        #endregion

        /// <summary>
        /// Checks if a preset requires an external coordinate file or is handled by XFOIL's internal NACA generator.
        /// </summary>
        public static bool IsNacaCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            code = code.Trim();
            return (code.Length == 4 || code.Length == 5) && int.TryParse(code, out _);
        }

        /// <summary>
        /// Writes a built-in airfoil coordinate table to a .dat file formatted for XFOIL/AVL.
        /// </summary>
        public static string MaterializePresetFile(string key, string directory)
        {
            if (!_embeddedAirfoils.TryGetValue(key, out var data))
                return "";

            Directory.CreateDirectory(directory);
            string filePath = Path.Combine(directory, $"{key.ToLowerInvariant()}.dat");

            using var sw = new StreamWriter(filePath);
            sw.WriteLine(data.Title);
            int count = data.Coords.GetLength(0);
            for (int i = 0; i < count; i++)
            {
                double x = data.Coords[i, 0];
                double y = data.Coords[i, 1];
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "  {0,9:F6}  {1,9:F6}", x, y));
            }
            return filePath;
        }

        #region Thin Airfoil Theory Analytical Solver

        public record ThinAirfoilTheoryResult(
            double AlphaZeroDeg,
            double CL0,
            double LiftCurveSlopePerDeg,
            double MaxCamberPercent,
            double MaxCamberLocation);

        /// <summary>
        /// Calculates Thin Airfoil Theory analytical zero-lift angle of attack (alpha_0)
        /// and lift curve slope (dCL/da = 2*pi) from discrete airfoil coordinates.
        /// alpha_0 = -1/pi * integral_0^pi (dz_c/dx * (cos(theta) - 1)) dtheta
        /// </summary>
        public static ThinAirfoilTheoryResult ComputeThinAirfoilTheory(IReadOnlyList<PointF> rawCoords)
        {
            const double twoPiPerRad = 2.0 * Math.PI;
            double slopePerDeg = twoPiPerRad * (Math.PI / 180.0); // ~0.10966 / deg

            if (rawCoords == null || rawCoords.Count < 6)
            {
                return new ThinAirfoilTheoryResult(0.0, 0.0, slopePerDeg, 0.0, 0.0);
            }

            // 1. Separate coordinates into upper and lower surfaces at leading edge
            int leIndex = 0;
            float minX = float.MaxValue;
            for (int i = 0; i < rawCoords.Count; i++)
            {
                if (rawCoords[i].X < minX)
                {
                    minX = rawCoords[i].X;
                    leIndex = i;
                }
            }

            var upper = new List<PointF>();
            for (int i = 0; i <= leIndex; i++) upper.Add(rawCoords[i]); // TE -> LE
            upper.Reverse(); // Now LE -> TE

            var lower = new List<PointF>();
            for (int i = leIndex; i < rawCoords.Count; i++) lower.Add(rawCoords[i]); // LE -> TE

            // 2. Numerical integration using transformation x = (1 - cos(theta)) / 2
            const int nStations = 120;
            double dTheta = Math.PI / nStations;
            double integral = 0.0;
            double maxCamber = 0.0;
            double maxCamberX = 0.0;

            double SampleY(List<PointF> surface, double xTarget)
            {
                if (surface.Count == 0) return 0.0;
                if (xTarget <= surface[0].X) return surface[0].Y;
                if (xTarget >= surface[^1].X) return surface[^1].Y;

                for (int i = 0; i < surface.Count - 1; i++)
                {
                    if (surface[i].X <= xTarget && surface[i + 1].X >= xTarget)
                    {
                        double dx = surface[i + 1].X - surface[i].X;
                        if (Math.Abs(dx) < 1e-7) return surface[i].Y;
                        double t = (xTarget - surface[i].X) / dx;
                        return surface[i].Y + t * (surface[i + 1].Y - surface[i].Y);
                    }
                }
                return 0.0;
            }

            double Camber(double x)
            {
                double yu = SampleY(upper, x);
                double yl = SampleY(lower, x);
                return 0.5 * (yu + yl);
            }

            for (int i = 1; i < nStations; i++)
            {
                double theta = i * dTheta;
                double x = 0.5 * (1.0 - Math.Cos(theta));

                double zc = Camber(x);
                if (Math.Abs(zc) > Math.Abs(maxCamber))
                {
                    maxCamber = zc;
                    maxCamberX = x;
                }

                // Numerical derivative dzc/dx
                const double eps = 0.001;
                double x1 = Math.Max(0.0, x - eps);
                double x2 = Math.Min(1.0, x + eps);
                double dzdx = (Camber(x2) - Camber(x1)) / (x2 - x1);

                double integrand = dzdx * (Math.Cos(theta) - 1.0);
                integral += integrand * dTheta;
            }

            double alpha0Rad = -integral / Math.PI;
            double alpha0Deg = alpha0Rad * (180.0 / Math.PI);
            double cl0 = twoPiPerRad * (-alpha0Rad);

            return new ThinAirfoilTheoryResult(
                AlphaZeroDeg: alpha0Deg,
                CL0: cl0,
                LiftCurveSlopePerDeg: slopePerDeg,
                MaxCamberPercent: maxCamber * 100.0,
                MaxCamberLocation: maxCamberX * 100.0
            );
        }

        #endregion

        #region Geometric Trailing-Edge Flap Deflection

        /// <summary>
        /// Deflects a trailing-edge plain flap on discrete airfoil coordinates.
        /// Rotates points aft of xHinge by deltaDeg around (xHinge, yCamber(xHinge)).
        /// </summary>
        public static List<PointF> DeflectFlap(IReadOnlyList<PointF> rawCoords, double xHinge, double deltaDeg)
        {
            if (rawCoords == null || rawCoords.Count == 0)
                return new List<PointF>();

            if (Math.Abs(deltaDeg) < 0.01)
                return new List<PointF>(rawCoords);

            // Find hinge camber location
            double yHinge = 0.0;
            double sumY = 0.0;
            int countNearHinge = 0;
            foreach (var pt in rawCoords)
            {
                if (Math.Abs(pt.X - xHinge) < 0.05)
                {
                    sumY += pt.Y;
                    countNearHinge++;
                }
            }
            if (countNearHinge > 0) yHinge = sumY / countNearHinge;

            double angleRad = -deltaDeg * (Math.PI / 180.0); // positive delta is flap DOWN
            double cosA = Math.Cos(angleRad);
            double sinA = Math.Sin(angleRad);

            var result = new List<PointF>(rawCoords.Count);
            foreach (var pt in rawCoords)
            {
                if (pt.X <= xHinge)
                {
                    result.Add(pt);
                }
                else
                {
                    double dx = pt.X - xHinge;
                    double dy = pt.Y - yHinge;
                    double rx = dx * cosA - dy * sinA + xHinge;
                    double ry = dx * sinA + dy * cosA + yHinge;
                    result.Add(new PointF((float)rx, (float)ry));
                }
            }

            return result;
        }

        #endregion
    }
}
