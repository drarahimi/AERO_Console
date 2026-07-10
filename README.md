# AERO Console

[![Latest Release](https://img.shields.io/github/v/release/drarahimi/AERO_Console?label=Latest%20Version&style=flat-square)](https://github.com/drarahimi/AERO_Console/releases/latest)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows)](https://github.com/drarahimi/AERO_Console/releases)
[![Language](https://img.shields.io/github/languages/top/drarahimi/AERO_Console?style=flat-square)](https://github.com/drarahimi/AERO_Console)
[![YouTube](https://img.shields.io/badge/Tutorials-YouTube-FF0000?style=flat-square&logo=youtube)](https://youtube.com/playlist?list=PLxcxum3Kgj_ZdFPVwpr5niSOyrRWJFYSp)
[![Sponsor](https://img.shields.io/badge/Sponsor-Ko--fi-F16061?style=flat-square&logo=ko-fi)](https://ko-fi.com/arahimi)

**AERO Console** is a modern graphical user interface (GUI) for Mark Drela's renowned [Athena Vortex Lattice (AVL)](https://web.mit.edu/drela/Public/web/avl/) and [XFoil](https://web.mit.edu/drela/Public/web/xfoil/) aerodynamic analysis tools.

Originally developed for the **Aerodynamics and Performance course (MECH-4671)** at the [University of Windsor](https://www.uwindsor.ca/) by [Dr. Afshin Rahimi](https://www.arahimi.ca/), this tool bridges the gap between powerful aerodynamic calculation engines and user-friendly design environments.

<p align="center">
  <img src="Images/main-window.png" alt="AERO Console Main Window" width="800">
</p>

---

## 📖 Table of Contents
- [Background](#-background)
- [Key Features](#-key-features)
- [Installation](#-installation)
- [Important Workflow & File Structure](#-important-workflow--file-structure)
- [Tutorials & Support](#-tutorials--support)
- [Credits](#-credits)

---

## 💡 Background

AVL and XFoil are industry-standard, powerful, and free tools for aerodynamic analysis. However, their reliance on legacy DOS-command interfaces creates a steep learning curve for students and modern engineers. 

**AERO Console solves this by providing:**
* A visual design environment to replace command-line inputs.
* Real-time syntax highlighting for AVL files.
* Instant visual feedback on geometry and mass distributions.
* Streamlined workflow for design iteration.

---

## 🚀 Key Features

### 🖥️ Interface & Editor
| Feature | Description |
| :--- | :--- |
| **Graphical User Interface** | User-friendly forms and consoles to manage simulation parameters. |
| **Syntax Highlighting** | Color-coded and automatically indented AVL/XFOIL code editor. |
| **Custom Owner-Drawn ToolTips** | Hover over key terms for monospace (`Consolas` 10-11pt) helper explanations in clean white card styling. |
| **Autosave Control** | Toggle auto-saving on/off. When off, a warning indicates dirty states, and prompts confirm closing, tab changes, or project reloads. |
| **Optimized Rendering** | Batched console log updates and cached font/GDI+ resources eliminate textbox/combobox flicker and reduce CPU/fan load during heavy AVL output. |
| **Drag-and-Drop File Import** | Drop `.avl`/`.mass`/`.run`/airfoil files, whole folders, or `.zip` archives onto the drop zone at the bottom of the main window. Archives are extracted and folder structures are flattened automatically, with collision-safe renaming and a runtime-file exclusion list so the app's own `.exe`/`.dll` files can never be overwritten by accident. |

#### 📺 Interface Visual Showcase
<table width="100%">
  <tr>
    <td width="50%" align="center"><b>Interactive Graphical Interface</b><br/><br/><img src="Images/graphical-interface.gif" alt="Graphical Interface Demo" width="100%"/></td>
    <td width="50%" align="center"><b>Syntax Highlighting Editor</b><br/><br/><img src="Images/colorful-editor.gif" alt="Syntax Highlighting Demo" width="100%"/></td>
  </tr>
  <tr>
    <td colspan="2" align="center"><b>Custom Owner-Drawn ToolTips</b><br/><br/><img src="Images/tooltip-help.gif" alt="Custom ToolTips Demo" width="70%"/></td>
  </tr>
</table>

### ✈️ Visualization Tools
| Feature | Description |
| :--- | :--- |
| **3D Navigation** | Rotate, zoom, and pan the 3D view with mouse controls. |
| **Component Overlay** | Toggle visibility for sections, mass distribution, control surfaces, and vortex-lattice panels mesh. |
| **3D Mesh Rendering** | Displays panels mesh overlay in 3D visualization with proper perspective coordinates clipping. |
| **Node Highlighting** | Interactive highlighter for geometry and mass nodes. |
| **ClearType Typography** | High-fidelity text rendering using `ClearTypeGridFit` for smooth grids, labels, and documentation. |
| **Centered Fit All** | Automated scaling with dynamic margins padding to keep models centered with breathing room. |

#### 📺 Visualization Visual Showcase
<table width="100%">
  <tr>
    <td width="50%" align="center"><b>3D Interactive Navigation</b><br/><br/><img src="Images/show-3d.gif" alt="3D Navigation Demo" width="100%"/></td>
    <td width="50%" align="center"><b>Component Overlay Controls</b><br/><br/><img src="Images/show-controls.gif" alt="Component Overlays Demo" width="100%"/></td>
  </tr>
  <tr>
    <td colspan="2" align="center"><b>Geometry & Mass Node Highlighting</b><br/><br/><img src="Images/node-highlighter.gif" alt="Node Highlighting Demo" width="70%"/></td>
  </tr>
</table>

### ⚙️ Multi-Engine Support (AVL & XFOIL)
- **Engine Switching**: Switch seamlessly between AVL and XFOIL engines from the main ToolStrip.
- **Auto-Downloader**: Automatically downloads and unzips `XFOIL6.99.zip` from MIT's official server into the local `appdata/` directory when first activated.
- **Contextual Actions**: Button labels, tooltips, and background execution pathways dynamically morph to support the active tool:
  - **AVL Mode**: Load Geometry (`.avl`), Load Mass (`.mass`), Load Run (`.run`).
  - **XFOIL Mode**: Load Airfoil (`.dat` or NACA codes), Init Polar (`pacc`), Run Alpha (`alfa` interactive prompt).
- **Plot Controller**: A specialized "Close Plot" action dispatches safety escape key sequences and restarts XFOIL automatically if it terminates, ensuring plotting windows close cleanly.

### 📊 Analysis & Plots
All of the plots below are rendered natively by AERO Console rather than opening AVL's own graphics window, so every one supports the same PNG/SVG/PDF export as the geometry views and runs from an in-tab **Run** button — no separate menu needed.

| Feature | Description |
| :--- | :--- |
| **Trefftz Plane Plot** | In-house recreation of AVL's Trefftz Plane window: spanwise circulation/downwash distribution plus the full coefficient readout (CL, CD, e, etc.), pulled directly from AVL rather than screen-scraped. |
| **Loads (Shear & Bending Moment)** | Spanwise shear force and bending moment distribution per surface, from AVL's `VM` command. |
| **Drag Polar** | Runs a configurable alpha sweep and plots the resulting CL–CD polar. |
| **Derivatives** | Stability and control derivatives (`ST`, `SB`, `FN`, `FB`, `HM`), shown as AVL's own formatted text output. |
| **Pressure / Element Forces** | Chordwise pressure distribution per strip, from AVL's `FE` command, with a strip selector. |
| **Dynamics (Eigenmodes)** | Root-locus/eigenvalue plot from AVL's `.MODE` analysis, with automatic mode classification (Phugoid, Short Period, Dutch Roll, Roll Subsidence, Spiral), hover tooltips showing ωₙ/ζ/period/stability, and a **Stability Tips** button explaining how to fix an unstable mode via geometry or mass changes and why it helps. |
| **Load Test Project** | One-click button that generates a ready-to-run 3-surface test aircraft (wing+aileron, tail+elevator, fin+rudder) with a mass breakdown and trimmed run case, sized to exercise every analysis tab above without needing your own project. |

### 🖱️ Interactive Geometry Editing
| Feature | Description |
| :--- | :--- |
| **Properties Panel** | Click any Section, Control, or Mass node in the 2D/3D views to open a side panel of editable fields (Xle/Yle/Zle/Chord/Ainc/Airfoil for sections; Cname/Cgain/Xhinge/HingeVec/SgnDup for controls; Mass/X/Y/Z/Ixx/Iyy/Izz for mass points). Edits write straight back to the underlying line - the raw text editor remains the source of truth - and it correctly handles mirrored (`YDUPLICATE`) geometry. |
| **Field Help Tooltips** | Every properties-panel field has a "?" hint describing what it does, sourced from AVL's own documentation. |
| **Structure Tree** | A collapsible Surface → Section → Control navigator with drag-and-drop reordering/reassignment (type-aware: Sections only accept drops onto Sections/Surfaces, Controls only onto Controls/Sections), Add/Delete buttons, single-click-to-navigate, and double-click-to-open-properties. |
| **Drag-to-Reposition** | Toggle Drag Mode to directly drag leading/trailing edges and control hinges in any 2D view; the underlying line updates automatically. |
| **Auto-Prettify** | Edits made through the Properties Panel or Structure Tree automatically re-align the file's column formatting, so panel edits and manual edits never leave the file with mismatched spacing. |

---

## 📥 Installation

> [!NOTE]
> **Compatibility:** This software is designed exclusively for **Windows OS**. Mac/Linux users should use a Virtual Machine (VM) or Boot Camp.

1.  **Download:** Get the latest standalone version from the [Releases Page](https://github.com/drarahimi/AERO_Console/releases/latest).
2.  **Location:** Place the `.exe` file in a folder where you have **Read/Write permissions** (e.g., specific project folder, not `C:\Program Files`).
3.  **Run:** Open `AERO_Console.exe`.
    * *Security Warning:* You may see a "Windows protected your PC" popup. Click **More Info** > **Run Anyway**. This occurs because the app is not digitally signed by Microsoft.
4.  **Setup:** The program will automatically unpack necessary dependencies into the same folder.

---

## ⚠️ Important Workflow & File Structure

To ensure the software functions correctly, please adhere to the following file naming and structural rules.

### 1. Reserved Filenames
The program relies on specific temporary filenames in the root directory.

> [!WARNING]
> **Risk of Overwriting:** The program constantly writes to the files listed below. **Do not use these names for your saved projects.**
> Once you are satisfied with a design, **Rename** or **Move** these files immediately to a safe location.

| Filename | Purpose | Location |
| :--- | :--- | :--- |
| `test.avl` | Temporary Geometry File | Root Folder* |
| `test.mas` | Temporary Mass File | Root Folder* |
| `test.run` | Temporary Run File | Root Folder* |

*\*Root Folder is the directory where `AERO_Console.exe` is located.*

### 2. Geometry Hierarchy
When editing `.avl` files, you must follow this specific nesting order to prevent calculation errors or crashes:

```text
+- AVL Template (Root)
   |
   +-- Surface 1
   |    |-- Section A (Root Chord)
   |    |    +-- Control Surface
   |    |
   |    +-- Section B (Tip Chord)
   |         +-- Control Surface
   |
   +-- Surface 2 ...
```

The **Structure Tree** panel (see [Interactive Geometry Editing](#-interactive-geometry-editing) above) enforces this same nesting automatically when adding, deleting, or dragging blocks to reorder them.