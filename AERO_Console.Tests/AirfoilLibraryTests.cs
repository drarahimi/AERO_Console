using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Xunit;

namespace AERO_Console.Tests
{
    public class AirfoilLibraryTests
    {
        [Fact]
        public void Presets_ContainsBenchmarkAirfoils()
        {
            Assert.NotEmpty(AirfoilLibrary.Presets);
            Assert.True(AirfoilLibrary.Presets.Count >= 10);

            foreach (var p in AirfoilLibrary.Presets)
            {
                Assert.False(string.IsNullOrWhiteSpace(p.Name));
                Assert.False(string.IsNullOrWhiteSpace(p.Category));
                Assert.False(string.IsNullOrWhiteSpace(p.CodeOrFilename));
                Assert.False(string.IsNullOrWhiteSpace(p.Description));
            }
        }

        [Theory]
        [InlineData("0012", true)]
        [InlineData("2412", true)]
        [InlineData("4412", true)]
        [InlineData("23012", true)]
        [InlineData("clarky", false)]
        [InlineData("rae2822", false)]
        [InlineData("", false)]
        public void IsNacaCode_IdentifiesStandardNaca(string code, bool expected)
        {
            Assert.Equal(expected, AirfoilLibrary.IsNacaCode(code));
        }

        [Fact]
        public void MaterializePresetFile_CreatesValidDatFile()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "airfoil_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                string clarkyPath = AirfoilLibrary.MaterializePresetFile("clarky", tempDir);
                Assert.True(File.Exists(clarkyPath));
                string[] lines = File.ReadAllLines(clarkyPath);
                Assert.True(lines.Length > 10);
                Assert.Contains("Clark-Y", lines[0]);

                string s1223Path = AirfoilLibrary.MaterializePresetFile("s1223", tempDir);
                Assert.True(File.Exists(s1223Path));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ThinAirfoilTheory_SymmetricAirfoil_HasNearZeroAlpha0()
        {
            // Create a symmetric 12% thickness profile
            var coords = new List<PointF>();
            int n = 30;
            // Upper surface (TE -> LE)
            for (int i = n; i >= 0; i--)
            {
                float x = (float)i / n;
                float y = 0.12f * 5f * (0.2969f * (float)Math.Sqrt(x) - 0.1260f * x - 0.3516f * x * x + 0.2843f * x * x * x - 0.1015f * x * x * x * x);
                coords.Add(new PointF(x, y));
            }
            // Lower surface (LE -> TE)
            for (int i = 1; i <= n; i++)
            {
                float x = (float)i / n;
                float y = -0.12f * 5f * (0.2969f * (float)Math.Sqrt(x) - 0.1260f * x - 0.3516f * x * x + 0.2843f * x * x * x - 0.1015f * x * x * x * x);
                coords.Add(new PointF(x, y));
            }

            var result = AirfoilLibrary.ComputeThinAirfoilTheory(coords);
            Assert.InRange(result.AlphaZeroDeg, -0.2, 0.2);
            Assert.InRange(result.MaxCamberPercent, 0.0, 0.1);
            Assert.InRange(result.LiftCurveSlopePerDeg, 0.108, 0.111);
        }

        [Fact]
        public void ThinAirfoilTheory_ClarkY_HasNegativeAlpha0()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "airfoil_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                string path = AirfoilLibrary.MaterializePresetFile("clarky", tempDir);
                string[] lines = File.ReadAllLines(path);
                var coords = new List<PointF>();
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x)
                                         && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                    {
                        coords.Add(new PointF(x, y));
                    }
                }

                var result = AirfoilLibrary.ComputeThinAirfoilTheory(coords);
                // Clark-Y has noticeable positive camber, so alpha_0 must be negative (approx -3 deg)
                Assert.True(result.AlphaZeroDeg < -1.0, $"Expected negative alpha_0 for Clark-Y, got {result.AlphaZeroDeg}");
                Assert.True(result.MaxCamberPercent > 1.5, $"Expected significant camber for Clark-Y, got {result.MaxCamberPercent}");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void DeflectFlap_RotatesAftPointsDownward()
        {
            var coords = new List<PointF>
            {
                new PointF(1.0f, 0.0f),
                new PointF(0.5f, 0.05f),
                new PointF(0.0f, 0.0f),
                new PointF(0.5f, -0.05f),
                new PointF(1.0f, 0.0f)
            };

            // Deflect 10 degrees down at xHinge = 0.75
            var flapped = AirfoilLibrary.DeflectFlap(coords, 0.75, 10.0);
            Assert.Equal(coords.Count, flapped.Count);

            // Points ahead of hinge station (x <= 0.75) must be untouched
            Assert.Equal(coords[1].X, flapped[1].X, 4);
            Assert.Equal(coords[1].Y, flapped[1].Y, 4);
            Assert.Equal(coords[2].X, flapped[2].X, 4);
            Assert.Equal(coords[2].Y, flapped[2].Y, 4);

            // Trailing edge (x = 1.0) must be rotated downward (lower Y)
            Assert.True(flapped[0].Y < coords[0].Y, $"Expected TE Y to move down: {flapped[0].Y} < {coords[0].Y}");
        }
    }
}
