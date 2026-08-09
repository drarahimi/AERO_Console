using System;
using System.Text;
using System.Windows.Forms;
using Avl.Core.Engine;

namespace AERO_Console
{
    // Dedicated, ISOLATED native-AVL engine for the Geometry Designer's tab analyses
    // (Trefftz / Loads / element-forces / stability / point analyses). Previously these
    // ran on frmMain's shared console engine via its EngineSendLine/EngineFlush bridge and
    // scraped frmMain.txtLog for the transcript -- which reset the live session's state
    // (load/oper/x) and dumped their command traffic into the user's console log. Running
    // them on this private in-process AvlSession keeps the main console completely untouched,
    // mirroring how frmXfoilAnalysis runs its own solvers independently of the XFOIL console.
    public partial class frmGeometry
    {
        // One private engine per Designer, created lazily on first analysis and reused.
        private readonly AvlAnalysisBridge _avl = new AvlAnalysisBridge();

        /// <summary>Wraps a private <see cref="NativeAvlEngine"/> behind the same tiny surface
        /// (EngineAlive / EngineSendLine / EngineFlush / loadConsole / LogText) the analysis code
        /// used to call on frmMain, so those call sites only had to swap the receiver. The engine's
        /// console output is accumulated into a growing buffer that the analyses read with the same
        /// "length-before, substring-after" delta approach they used against frmMain.txtLog.</summary>
        private sealed class AvlAnalysisBridge
        {
            private readonly object _sync = new object();
            private readonly StringBuilder _log = new StringBuilder();
            private NativeAvlEngine _engine;

            /// <summary>Whether the private analysis engine is ready -- and, because it is created
            /// lazily on demand, this STARTS it if it isn't running yet, then reports the result.
            /// The analysis code uses `if (!EngineAlive) return;` as a pre-flight guard; unlike the
            /// old shared frmMain console (which the user had already started), this private engine
            /// has no separate "start" step, so the guard must bring it up rather than bail out.</summary>
            public bool EngineAlive
            {
                get
                {
                    try
                    {
                        if (_engine is null || !_engine.IsRunning)
                            Restart();
                    }
                    catch
                    {
                        return false;
                    }
                    return _engine is not null && _engine.IsRunning;
                }
            }

            /// <summary>Recovery hook (named to match frmMain's) -- (re)creates the private engine.
            /// Analyses call this if EngineAlive is false; it never touches the main console.</summary>
            public void loadConsole() => Restart();

            /// <summary>Sends one command line to the private engine (starting it if needed).</summary>
            public void EngineSendLine(string command = "")
            {
                if (_engine is null || !_engine.IsRunning)
                    Restart();
                _engine.Send(command);
            }

            /// <summary>No-op: the native engine processes each Send synchronously (parity with
            /// frmMain.EngineFlush, which only flushes stdin for the external avl.exe path).</summary>
            public void EngineFlush()
            {
            }

            /// <summary>The full accumulated console transcript so far. Analyses snapshot its length
            /// before sending, then substring from that offset after -- exactly as they did with the
            /// old frmMain.txtLog.Text scrape (the buffer is intentionally never cleared).</summary>
            public string LogText
            {
                get { lock (_sync) return _log.ToString(); }
            }

            private void Restart()
            {
                if (_engine is not null)
                    _engine.Output -= OnOutput;
                // Same working directory as the main console so relative "load x.avl" and the
                // temp-file writes the analyses issue resolve identically.
                _engine = new NativeAvlEngine(Application.StartupPath);
                _engine.Output += OnOutput;
                _engine.Start();
            }

            private void OnOutput(string s)
            {
                lock (_sync)
                    _log.Append(s);
            }
        }
    }
}
