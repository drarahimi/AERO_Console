using AeroPlot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Text;
using System.Windows.Forms;

namespace AERO_Console
{
    // Detached-window support for the AVL dynamic-modes root-locus plot (MODE menu "N"). Like
    // Trefftz it's a static 2D plot, so the pop-out gets render + PNG/SVG/PDF export + resize,
    // but no rotate/zoom/fit. Data comes straight from what the native session computed for
    // MODE 'N' (eigenvalues + eigenvectors), so nothing is re-run.
    public partial class frmGeometry
    {
        private frmPlotWindow _plotModesWindow;

        /// <summary>Native-AVL console (MODE menu "N"): draw the root-locus from the eigenvalues /
        /// eigenvectors the session just computed. No re-analysis -- avl.exe pops its own root-locus
        /// window here; this shows the modern one.</summary>
        public void ShowConsoleModesPlot(IReadOnlyList<Complex> evals, IReadOnlyList<Complex[]> evecs)
        {
            EnsureFormInitialized();
            try
            {
                _lastEigenvalues = BuildEigenValues(evals, evecs);
                ClassifyModes(_lastEigenvalues);
                RenderModesPlot(); // refresh the docked pModes tab too (self-guards if not built)
            }
            catch
            {
            }
            OpenModesWindow();
        }

        // Maps the session's raw eigenvalues + eigenvectors (state order JEU..JEPS = u,w,q,the,
        // v,p,r,phi,x,y,z,psi) onto EigenValue objects the root-locus renderer + ClassifyModes use.
        private static List<EigenValue> BuildEigenValues(IReadOnlyList<Complex> evals, IReadOnlyList<Complex[]> evecs)
        {
            var list = new List<EigenValue>();
            if (evals is null)
                return list;
            for (int i = 0; i < evals.Count; i++)
            {
                Complex[] v = (evecs is not null && i < evecs.Count) ? evecs[i] : null;
                double Mag(int idx) => (v is not null && idx < v.Length) ? v[idx].Magnitude : 0.0;
                list.Add(new EigenValue
                {
                    RunCase = 1,
                    Real = evals[i].Real,
                    Imag = evals[i].Imaginary,
                    MagU = Mag(0),
                    MagW = Mag(1),
                    MagQ = Mag(2),
                    MagTheta = Mag(3),
                    MagV = Mag(4),
                    MagP = Mag(5),
                    MagR = Mag(6),
                    MagPhi = Mag(7),
                    MagPsi = Mag(11),
                });
            }
            return list;
        }

        /// <summary>Opens (or focuses) the detached, resizable root-locus window.</summary>
        public void OpenModesWindow()
        {
            if (_plotModesWindow is not null && !_plotModesWindow.IsDisposed)
            {
                _plotModesWindow.Focus();
                _plotModesWindow.RequestRender();
                return;
            }

            _plotModesWindow = new frmPlotWindow(new DelegatePlotSource(
                "AVL Dynamic Modes - Root Locus",
                render: (w, h, view) => PopoutRender(() => BuildModesBitmap(w, h, view, false, out _, out _)),
                export: ExportModes,
                setTheme: SetPopoutTheme, initialDark: IsDarkTheme, setFontScale: SetPopoutFontScale))
            {
                Icon = this.Icon,
            };
            _plotModesWindow.FormClosed += (_, __) => _plotModesWindow = null;
            _plotModesWindow.Show(this);
        }

        /// <summary>Export handler for the detached root-locus window's "Export ▾" menu.</summary>
        public void ExportModes(string format, int width, int height)
        {
            Bitmap bmp = null;
            _popoutRenderActive = true; // render the export at the pop-out's chosen font size
            try
            {
                bmp = BuildModesBitmap(width, height, AeroPlot.PlotView.Identity, true, out var svg, out var pdf);

                if (format != "PNG" && string.IsNullOrEmpty(svg))
                {
                    AppMessageBox.Show("The view has not finished rendering. Please try again in a moment.",
                        "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var sfd = new SaveFileDialog { Title = $"Export Root Locus as {format}" };
                string projName = (projectName ?? "").Trim();
                if (string.IsNullOrEmpty(projName) || projName.Contains("Enter AVL Project") || projName.Contains("Enter NACA"))
                {
                    sfd.FileName = "Root_Locus";
                }
                else
                {
                    foreach (var c in Path.GetInvalidFileNameChars())
                        projName = projName.Replace(c, '_');
                    sfd.FileName = $"{projName}_Root_Locus";
                }
                switch (format)
                {
                    case "PNG": sfd.Filter = "PNG Image (*.png)|*.png"; sfd.DefaultExt = "png"; break;
                    case "SVG": sfd.Filter = "SVG Image (*.svg)|*.svg"; sfd.DefaultExt = "svg"; break;
                    case "PDF": sfd.Filter = "PDF Document (*.pdf)|*.pdf"; sfd.DefaultExt = "pdf"; break;
                }

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    switch (format)
                    {
                        case "PNG":
                            using (var copy = new Bitmap(bmp))
                                copy.Save(sfd.FileName, ImageFormat.Png);
                            break;
                        case "SVG":
                            File.WriteAllText(sfd.FileName, svg, Encoding.UTF8);
                            break;
                        case "PDF":
                            WriteVectorPdf(pdf, width, height, sfd.FileName);
                            break;
                    }
                    AppToast.ShowExported($"{format} exported to " + Path.GetFileName(sfd.FileName), sfd.FileName);
                }
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error exporting file: " + ex.Message, "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _popoutRenderActive = false;
                bmp?.Dispose();
            }
        }
    }
}
