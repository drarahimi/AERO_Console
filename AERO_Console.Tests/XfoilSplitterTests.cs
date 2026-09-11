using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace AERO_Console.Tests
{
    public class XfoilSplitterTests
    {
        [Fact]
        public void XfoilSplitterSettings_DefaultValues_AreSensible()
        {
            var settings = new XfoilSplitterSettings();
            Assert.Null(settings.Version);
            Assert.Equal(180, settings.TopAndBottomDistance);
            Assert.Equal(530, settings.ParamsCardsDistance);
            Assert.Equal(120, settings.PlotsAndLogFromBottom);
            Assert.Equal(170, settings.PolarAndRunsFromRight);
            Assert.False(settings.LogCollapsed);
            Assert.Equal(120, settings.SavedLogHeight);
        }

        [Fact]
        public void XfoilSplitterSettings_SerializationRoundTrip_PreservesValues()
        {
            var original = new XfoilSplitterSettings
            {
                Version = 2,
                TopAndBottomDistance = 185,
                ParamsCardsDistance = 540,
                PlotsAndLogFromBottom = 125,
                PolarAndRunsFromRight = 175,
                LogCollapsed = true,
                SavedLogHeight = 135
            };

            string json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = true });
            var deserialized = JsonSerializer.Deserialize<XfoilSplitterSettings>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.Version);
            Assert.Equal(185, deserialized.TopAndBottomDistance);
            Assert.Equal(540, deserialized.ParamsCardsDistance);
            Assert.Equal(125, deserialized.PlotsAndLogFromBottom);
            Assert.Equal(175, deserialized.PolarAndRunsFromRight);
            Assert.True(deserialized.LogCollapsed);
            Assert.Equal(135, deserialized.SavedLogHeight);
        }

        private static void ResetSettingsFile()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appData, "AERO_Console", "xfoil_splitters.json");
            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch { }
        }

        [Fact]
        public void FrmXfoilAnalysis_Splitters_AreProperlyInitialized()
        {
            ResetSettingsFile();
            // Execute on STA thread for WinForms safety
            var thread = new Thread(() =>
            {
                using var form = new frmXfoilAnalysis();
                form.Size = new System.Drawing.Size(1100, 750);
                form.CreateControl();

                // 1. Top vs Bottom Splitter
                var topBottom = form.SplitTopAndBottom;
                Assert.NotNull(topBottom);
                Assert.Equal(Orientation.Horizontal, topBottom.Orientation);
                Assert.Equal(FixedPanel.Panel1, topBottom.FixedPanel);
                Assert.Equal(4, topBottom.SplitterWidth);
                Assert.True(topBottom.Panel1.Controls.Count > 0);
                Assert.True(topBottom.Panel2.Controls.Count > 0);

                // 2. Card 1 vs Card 2 Splitter
                var paramsCards = form.SplitParamsCards;
                Assert.NotNull(paramsCards);
                Assert.Equal(Orientation.Vertical, paramsCards.Orientation);
                Assert.Equal(4, paramsCards.SplitterWidth);
                Assert.True(paramsCards.Panel1.Controls.Count > 0);
                Assert.True(paramsCards.Panel2.Controls.Count > 0);

                // 3. Plots vs Console Log Splitter
                var plotsLog = form.SplitPlotsAndLog;
                Assert.NotNull(plotsLog);
                Assert.Equal(Orientation.Horizontal, plotsLog.Orientation);
                Assert.Equal(FixedPanel.Panel2, plotsLog.FixedPanel);
                Assert.Equal(4, plotsLog.SplitterWidth);
                Assert.True(plotsLog.Panel1.Controls.Count > 0);
                Assert.True(plotsLog.Panel2.Controls.Count > 0);

                // 4. Polar Subplots vs Runs Sidebar Splitter
                var polarRuns = form.SplitPolarAndRuns;
                Assert.NotNull(polarRuns);
                Assert.Equal(Orientation.Vertical, polarRuns.Orientation);
                Assert.Equal(FixedPanel.Panel2, polarRuns.FixedPanel);
                Assert.Equal(4, polarRuns.SplitterWidth);
                Assert.True(polarRuns.Panel1.Controls.Count > 0);
                Assert.True(polarRuns.Panel2.Controls.Count > 0);

                // 5. Toggle Log Button
                var btnToggle = form.BtnToggleLog;
                Assert.NotNull(btnToggle);
                Assert.False(form.IsLogCollapsed);
                Assert.Equal("▾ Hide Log", btnToggle.Text);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void FrmXfoilAnalysis_ToggleLogCollapse_TogglesStateAndText()
        {
            ResetSettingsFile();
            var thread = new Thread(() =>
            {
                using var form = new frmXfoilAnalysis();
                form.Size = new System.Drawing.Size(1100, 750);
                form.CreateControl();

                Assert.False(form.IsLogCollapsed);
                Assert.Equal("▾ Hide Log", form.BtnToggleLog.Text);

                // Toggle to collapse
                form.ToggleLogCollapseForTest();
                Assert.True(form.IsLogCollapsed);
                Assert.Equal("▴ Show Log", form.BtnToggleLog.Text);

                // Toggle to expand
                form.ToggleLogCollapseForTest();
                Assert.False(form.IsLogCollapsed);
                Assert.Equal("▾ Hide Log", form.BtnToggleLog.Text);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void FrmXfoilAnalysis_Persistence_SavesAndLoadsJson()
        {
            ResetSettingsFile();
            var thread = new Thread(() =>
            {
                using var form = new frmXfoilAnalysis();
                form.Size = new System.Drawing.Size(1100, 750);
                form.CreateControl();

                // Trigger Save
                form.SaveSplitterSettingsForTest();

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string filePath = Path.Combine(appData, "AERO_Console", "xfoil_splitters.json");
                Assert.True(File.Exists(filePath));

                string json = File.ReadAllText(filePath);
                var loaded = JsonSerializer.Deserialize<XfoilSplitterSettings>(json);
                Assert.NotNull(loaded);
                Assert.True(loaded.TopAndBottomDistance > 0);
                Assert.True(loaded.ParamsCardsDistance > 0);
                Assert.True(loaded.PlotsAndLogFromBottom >= 38);
                Assert.True(loaded.PolarAndRunsFromRight >= 100);

                // Trigger Load/Apply
                form.ApplySplitterSettingsForTest();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void FrmXfoilAnalysis_Migration_UpgradesVersion1Ratios()
        {
            ResetSettingsFile();
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "AERO_Console");
            Directory.CreateDirectory(folder);
            string filePath = Path.Combine(folder, "xfoil_splitters.json");

            // Write an unversioned (v1) legacy settings file with old bad ratios
            string legacyJson = "{\"TopAndBottomDistance\":140,\"ParamsCardsDistance\":480,\"PlotsAndLogFromBottom\":200,\"PolarAndRunsFromRight\":200,\"LogCollapsed\":false,\"SavedLogHeight\":200}";
            File.WriteAllText(filePath, legacyJson);

            var thread = new Thread(() =>
            {
                using var form = new frmXfoilAnalysis();
                form.Size = new System.Drawing.Size(1100, 750);
                form.CreateControl();

                form.ApplySplitterSettingsForTest();
                form.SaveSplitterSettingsForTest();

                string json = File.ReadAllText(filePath);
                var upgraded = JsonSerializer.Deserialize<XfoilSplitterSettings>(json);
                Assert.NotNull(upgraded);
                Assert.Equal(2, upgraded.Version);
                Assert.Equal(180, upgraded.TopAndBottomDistance);
                Assert.Equal(530, upgraded.ParamsCardsDistance);
                Assert.Equal(120, upgraded.PlotsAndLogFromBottom);
                Assert.Equal(170, upgraded.PolarAndRunsFromRight);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
