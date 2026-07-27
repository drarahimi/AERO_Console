# AERO Console v1.7.0

_Since [v1.6.0](https://github.com/drarahimi/AERO_Console/releases/tag/1.6.0) (commit `786312b`) — 17 commits_

## ✈️ Multi-Engine Support: AVL & XFOIL
- Switch seamlessly between **AVL** and **XFOIL** engines from the main ToolStrip.
- New **Download** menu fetches AVL/XFOIL executables directly (with homepage links, progress bar feedback, and safe zip extraction on Windows) — no more manual installs.
- Buttons, tooltips, and background execution paths adapt to the active engine (AVL: Geometry/Mass/Run files; XFOIL: airfoil `.dat`/NACA codes, polar init, alpha sweep).
- Robuster AVL/XFOIL process handling; stuck plot windows are now cleared by a clean process restart (replacing a stdin workaround), with project files auto-reloaded and the "Close Plot" button always available.
- Integrated console input directly into the log view — command entry no longer needs a separate box, and only the live prompt line is editable.

## 📊 In-App Analysis & Plotting
- Native in-app plotting and export (**PNG/SVG/PDF**) for AVL analyses: Trefftz Plane, Spanwise Loads, Drag Polar, Stability Derivatives, Pressure Distribution, and Eigenvalue Analysis — each with its own tab, controls, and export button.
- New `SvgGraphics` class records vector output (SVG/PDF) alongside on-screen rendering for crisp exports; PDF export now uses proper Latin-1 encoding and Greek symbols.
- Derivatives tab shows a color-coded static-stability summary parsed straight from AVL output.
- Eigenmode analysis now shows mode names and stability tips.
- "Load Test Project" added for quickly demoing all analysis features.
- AVL command-sequence helper buttons added to analysis tabs.

## 🎨 Modern UI Overhaul
- **Dark/light theme toggle** across all custom-drawn views, with the preference persisted and every axis/grid/label color made theme-aware.
- Custom themed dialogs and toast notifications (`AppMessageBox`, `AppToast`) replace all `MessageBox`/`MsgBox` calls for a consistent look.
- Split main ToolStrip for clarity, with a live-status StatusStrip.
- Layouts across analysis tabs (Trefftz, Loads, Polar, Derivatives, Pressure, Dynamics) now use `TableLayoutPanel`, fixing invisible/misaligned docked controls and improving consistency across fonts and DPI settings.
- Floating Add/Undo/Redo/Clear buttons and an Add context menu in the geometry editor; widened, flat, double-buffered project selector; all controls double-buffered to eliminate flicker.

## 🕸️ 3D & Geometry Visualization
- New **"Display" dropdown** for geometry overlays, replacing individual toggle buttons — adds chordlines, control points, axes triad, camberline, normal vectors, bound/trailing legs, loading, and off-body points, each with color swatches.
- 3D view now rotates around the model center with depth-sorted geometry/controls/mass points for correct layering; zoom range/step scale to model size; a rotation-hint ("?") button explains the axes; nudge controls and Greek glyphs added to the angle readout.
- Vortex-lattice mesh overlay with perspective-correct clipping (from v1.7.0's initial groundwork), plus a fix so the "Loading" overlay refreshes correctly after Trefftz Plane runs.
- Docked **properties panel** for direct node editing with tooltips, and a drag-and-drop **structure tree** for reordering/navigating geometry blocks (with correct-nesting enforcement).
- Drag-and-drop file import for `.avl`/`.mass`/`.run`/airfoil/zip files, with guards against accidental overwrites.

## 📝 Editor Improvements
- New **auto-spacing settings** (enable + column width) with UI controls, plus a **Prettify** button to auto-indent and align editor content.
- Reworked syntax highlighting, auto-indentation, and formatting; template inserts preserve undo history.
- Owner-drawn monospace tooltips for editor keywords.
- Autosave toggle, explicit **Save** (`Ctrl+S`), and dirty-state tracking with warnings and confirmation prompts on close/tab-change/reload.
- New **validator** with a project gate to catch and prevent invalid states before running analyses.

## ⚡ Performance & Stability
- Throttled/timer-driven rendering with dirty-flag tracking to cut redundant redraws.
- Cached axis/tick fonts (thread-safe) and buffered, timer-flushed log output to reduce UI-thread contention.
- More robust mass-file parsing (tolerant of whitespace/empty lines); saving/loading and analysis are now blocked when the project name is empty.
- Various bug fixes for sync, layout, and process-handling stability.

## 🧹 Housekeeping
- Added "Package as Zip" and "Package as Standalone Exe" tools for maintainers.
- README overhauled multiple times with real screenshots/GIFs, refreshed badges, and up-to-date feature tables.
- Removed bundled third-party binaries (`avl.exe`, `xfoil.exe`, `pplot.exe`, `pxplot.exe`) and legacy docs from source control — fetched at runtime instead.
- Removed the unused `GeomLib` project and legacy `.cs.old` file.

---
**Full diff**: [`786312b...0879610`](https://github.com/drarahimi/AERO_Console/compare/786312b...0879610)
