using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace AERO_Console
{
    [Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
    public partial class frmGeometry : Form
    {

        // Form overrides dispose to clean up the component list.
        [DebuggerNonUserCode()]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && components is not null)
                {
                    components.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // Required by the Windows Form Designer
        private System.ComponentModel.IContainer components;

        // NOTE: The following procedure is required by the Windows Form Designer
        // It can be modified using the Windows Form Designer.  
        // Do not modify it using the code editor.
        [DebuggerStepThrough()]
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frmGeometry));
            _tt = new ToolTip(components);
            StatusStrip1 = new StatusStrip();
            lblCursor = new ToolStripStatusLabel();
            btnEditor = new ToolStripStatusLabel();
            btnEditor.Click += new EventHandler(btnEditor_Click);
            ToolStrip1 = new ToolStrip();
            ToolStripLabel1 = new ToolStripLabel();
            txtName = new ToolStripComboBox();
            txtName.TextChanged += new EventHandler(txtName_TextChanged);
            txtName.Click += new EventHandler(txtName_Click_1);
            txtName.SelectedIndexChanged += new EventHandler(txtName_SelectedIndexChanged);
            ctxAddMenu = new ContextMenuStrip(components);
            btnTabIncrease = new Button();
            btnTabIncrease.Click += new EventHandler(btnTabIncrease_Click);
            btnTabDecrease = new Button();
            btnTabDecrease.Click += new EventHandler(btnTabDecrease_Click);
            btnAdd = new Button();
            btnAdd.Click += new EventHandler(btnAdd_Click);
            AVLTemplateToolStripMenuItem = new ToolStripMenuItem();
            AVLTemplateFullToolStripMenuItem = new ToolStripMenuItem();
            AVLTemplateFullToolStripMenuItem.Click += new EventHandler(AVLTemplateFullToolStripMenuItem_Click);
            AVLTemplateMinimalToolStripMenuItem = new ToolStripMenuItem();
            AVLTemplateMinimalToolStripMenuItem.Click += new EventHandler(AVLTemplateMinimalToolStripMenuItem_Click);
            SurfaceToolStripMenuItem = new ToolStripMenuItem();
            SurfaceFullToolStripMenuItem = new ToolStripMenuItem();
            SurfaceFullToolStripMenuItem.Click += new EventHandler(SurfaceFullToolStripMenuItem_Click);
            SurfaceMinimalToolStripMenuItem = new ToolStripMenuItem();
            SurfaceMinimalToolStripMenuItem.Click += new EventHandler(SurfaceMinimalToolStripMenuItem_Click);
            SectionToolStripMenuItem = new ToolStripMenuItem();
            SectionFullToolStripMenuItem = new ToolStripMenuItem();
            SectionFullToolStripMenuItem.Click += new EventHandler(SectionFullToolStripMenuItem_Click);
            SectionMinimalToolStripMenuItem = new ToolStripMenuItem();
            SectionMinimalToolStripMenuItem.Click += new EventHandler(SectionMinimalToolStripMenuItem_Click);
            ControlToolStripMenuItem = new ToolStripMenuItem();
            ControlFullToolStripMenuItem = new ToolStripMenuItem();
            ControlFullToolStripMenuItem.Click += new EventHandler(ControlFullToolStripMenuItem_Click);
            ControlMinimalToolStripMenuItem = new ToolStripMenuItem();
            ControlMinimalToolStripMenuItem.Click += new EventHandler(ControlMinimalToolStripMenuItem_Click);
            ToolStripSeparator1 = new ToolStripSeparator();
            MassTemplateToolStripMenuItem = new ToolStripMenuItem();
            MassTemplateFullToolStripMenuItem = new ToolStripMenuItem();
            MassTemplateFullToolStripMenuItem.Click += new EventHandler(MassTemplateFullToolStripMenuItem_Click);
            MassTemplateMinimalToolStripMenuItem = new ToolStripMenuItem();
            MassTemplateMinimalToolStripMenuItem.Click += new EventHandler(MassTemplateMinimalToolStripMenuItem_Click);
            ToolStripSeparator2 = new ToolStripSeparator();
            RunTemplateToolStripMenuItem = new ToolStripMenuItem();
            RunTemplateFullToolStripMenuItem = new ToolStripMenuItem();
            RunTemplateFullToolStripMenuItem.Click += new EventHandler(RunTemplateFullToolStripMenuItem_Click);
            RunTemplateMinimalToolStripMenuItem = new ToolStripMenuItem();
            RunTemplateMinimalToolStripMenuItem.Click += new EventHandler(RunTemplateMinimalToolStripMenuItem_Click);
            ToolStripSeparator5 = new ToolStripSeparator();
            SeparatorToolStripMenuItem = new ToolStripMenuItem();
            SeparatorToolStripMenuItem.Click += new EventHandler(SeparatorToolStripMenuItem_Click);
            btnClear = new Button();
            btnClear.Click += new EventHandler(btnClear_Click);
            btnDragMode = new ToolStripButton();
            btnDragMode.Click += new EventHandler(btnDragMode_Click);
            btnUndo = new Button();
            btnUndo.Click += new EventHandler(btnUndo_Click);
            btnRedo = new Button();
            btnRedo.Click += new EventHandler(btnRedo_Click);
            btnPrettify = new Button();
            btnPrettify.Click += new EventHandler(btnPrettify_Click);
            btnValidate = new Button();
            btnValidate.Click += new EventHandler(btnValidate_Click);
            ToolStripSeparator6 = new ToolStripSeparator();
            ToolStripSeparator3 = new ToolStripSeparator();
            btnHelp = new ToolStripDropDownButton();
            btnHelp.Click += new EventHandler(btnHelp_Click_1);
            btnHelp.DropDownItemClicked += new ToolStripItemClickedEventHandler(btnHelp_DropDownItemClicked);
            btnHelpFull = new ToolStripMenuItem();
            btnHelpFull.Click += new EventHandler(btnHelpFull_Click);
            ToolStripSeparator8 = new ToolStripSeparator();
            btnHelpAVL = new ToolStripMenuItem();
            btnHelpAVL.Click += new EventHandler(btnHelpAVL_Click);
            btnHelpMass = new ToolStripMenuItem();
            btnHelpMass.Click += new EventHandler(btnHelpMass_Click);
            btnHelpRun = new ToolStripMenuItem();
            ToolStripSeparator9 = new ToolStripSeparator();
            btnHelpCommands = new ToolStripMenuItem();
            ToolStripSeparator4 = new ToolStripSeparator();
            btn3D = new ToolStripButton();
            btn3D.Click += new EventHandler(btn3D_Click);
            FileSystemWatcher1 = new System.IO.FileSystemWatcher();
            ToolStrip2 = new ToolStrip();
            btnZoomin = new ToolStripButton();
            btnZoomin.Click += new EventHandler(btnZoomin_Click);
            btnZoomout = new ToolStripButton();
            btnZoomout.Click += new EventHandler(btnZoomout_Click);
            btnFitAll = new ToolStripButton();
            btnFitAll.Click += new EventHandler(btnFitAll_Click);
            ToolStripSeparator11 = new ToolStripSeparator();
            btnBasefontplus = new ToolStripButton();
            btnBasefontplus.Click += new EventHandler(btnBasefontplus_Click);
            btnBasefontminus = new ToolStripButton();
            btnBasefontminus.Click += new EventHandler(btnBasefontminus_Click);
            ToolStripSeparator10 = new ToolStripSeparator();
            btnDisplay = new ToolStripButton();
            ToolStripSeparator12 = new ToolStripSeparator();
            btnSpace = new ToolStripButton();
            btnSpace.Click += new EventHandler(btnSpace_Click);
            btnHover = new ToolStripButton();
            btnHover.Click += new EventHandler(btnHover_Click);
            ToolStripSeparator13 = new ToolStripSeparator();
            btnLayers = new ToolStripDropDownButton();
            mnuLayerSection = new ToolStripMenuItem();
            mnuLayerSection.CheckedChanged += new EventHandler(mnuLayerSection_CheckedChanged);
            mnuLayerMass = new ToolStripMenuItem();
            mnuLayerMass.CheckedChanged += new EventHandler(mnuLayerMass_CheckedChanged);
            mnuLayerControl = new ToolStripMenuItem();
            mnuLayerControl.CheckedChanged += new EventHandler(mnuLayerControl_CheckedChanged);
            mnuLayerMesh = new ToolStripMenuItem();
            mnuLayerMesh.CheckedChanged += new EventHandler(mnuLayerMesh_CheckedChanged);
            ToolStripSeparator14 = new ToolStripSeparator();
            mnuLayerChordline = new ToolStripMenuItem();
            mnuLayerChordline.CheckedChanged += new EventHandler(mnuLayerChordline_CheckedChanged);
            mnuLayerControlPoints = new ToolStripMenuItem();
            mnuLayerControlPoints.CheckedChanged += new EventHandler(mnuLayerControlPoints_CheckedChanged);
            mnuLayerAxes = new ToolStripMenuItem();
            mnuLayerAxes.CheckedChanged += new EventHandler(mnuLayerAxes_CheckedChanged);
            ToolStripSeparator15 = new ToolStripSeparator();
            mnuLayerCamberline = new ToolStripMenuItem();
            mnuLayerCamberline.CheckedChanged += new EventHandler(mnuLayerCamberline_CheckedChanged);
            mnuLayerNormalVector = new ToolStripMenuItem();
            mnuLayerNormalVector.CheckedChanged += new EventHandler(mnuLayerNormalVector_CheckedChanged);
            mnuLayerBoundLeg = new ToolStripMenuItem();
            mnuLayerBoundLeg.CheckedChanged += new EventHandler(mnuLayerBoundLeg_CheckedChanged);
            mnuLayerTrailingLegs = new ToolStripMenuItem();
            mnuLayerTrailingLegs.CheckedChanged += new EventHandler(mnuLayerTrailingLegs_CheckedChanged);
            mnuLayerLoading = new ToolStripMenuItem();
            mnuLayerLoading.CheckedChanged += new EventHandler(mnuLayerLoading_CheckedChanged);
            mnuLayerOffBody = new ToolStripMenuItem();
            sc1 = new SplitContainer();
            scup = new SplitContainer();
            tc1 = new TabControl();
            tc1.SelectedIndexChanged += new EventHandler(tc1_SelectedIndexChanged);
            Geometry = new TabPage();
            Mass = new TabPage();
            Run = new TabPage();
            ImageList1 = new ImageList(components);
            p3d = new PictureBox();
            p3d.MouseDown += new MouseEventHandler(p3d_MouseDown);
            p3d.MouseMove += new MouseEventHandler(p3d_MouseMove);
            p3d.MouseUp += new MouseEventHandler(p3d_MouseUp);
            p3d.MouseEnter += new EventHandler(p3d_MouseEnter);
            p3d.MouseWheel += new MouseEventHandler(p3d_MouseWheel);
            pxy = new PictureBox();
            pxy.MouseMove += new MouseEventHandler(p1_MouseMove);
            pxy.MouseDown += new MouseEventHandler(p1_MouseDown);
            pxy.MouseWheel += new MouseEventHandler(p1_MouseWheel);
            pxy.MouseUp += new MouseEventHandler(p1_MouseUp);
            scdown = new SplitContainer();
            pyz = new PictureBox();
            pyz.Click += new EventHandler(pyz_Click);
            pyz.MouseDown += new MouseEventHandler(pyz_MouseDown);
            pyz.MouseMove += new MouseEventHandler(pyz_MouseMove);
            pyz.MouseUp += new MouseEventHandler(pyz_MouseUp);
            pyz.MouseWheel += new MouseEventHandler(pyz_MouseWheel);
            pxz = new PictureBox();
            pxz.MouseDown += new MouseEventHandler(pxz_MouseDown);
            pxz.MouseWheel += new MouseEventHandler(pxz_MouseWheel);
            pxz.MouseMove += new MouseEventHandler(pxz_MouseMove);
            pxz.MouseUp += new MouseEventHandler(pxz_MouseUp);
            StatusStrip1.SuspendLayout();
            ToolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)FileSystemWatcher1).BeginInit();
            ToolStrip2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)sc1).BeginInit();
            sc1.Panel1.SuspendLayout();
            sc1.Panel2.SuspendLayout();
            sc1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)scup).BeginInit();
            scup.Panel1.SuspendLayout();
            scup.Panel2.SuspendLayout();
            scup.SuspendLayout();
            tc1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)p3d).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pxy).BeginInit();
            ((System.ComponentModel.ISupportInitialize)scdown).BeginInit();
            scdown.Panel1.SuspendLayout();
            scdown.Panel2.SuspendLayout();
            scdown.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pyz).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pxz).BeginInit();
            SuspendLayout();
            // 
            // StatusStrip1
            // 
            StatusStrip1.BackColor = Color.WhiteSmoke;
            StatusStrip1.Items.AddRange(new ToolStripItem[] { lblCursor, btnEditor });
            StatusStrip1.Location = new Point(0, 594);
            StatusStrip1.Name = "StatusStrip1";
            StatusStrip1.Size = new Size(979, 22);
            StatusStrip1.TabIndex = 1;
            StatusStrip1.Text = "StatusStrip1";
            // 
            // lblCursor
            // 
            lblCursor.Name = "lblCursor";
            lblCursor.Size = new Size(48, 17);
            lblCursor.Text = "Cursor: ";
            // 
            // btnEditor
            // 
            btnEditor.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnEditor.Image = (Image)resources.GetObject("btnEditor.Image");
            btnEditor.ImageTransparentColor = Color.Magenta;
            btnEditor.Name = "btnEditor";
            btnEditor.Size = new Size(916, 17);
            btnEditor.Spring = true;
            btnEditor.Text = "Editor Help";
            btnEditor.TextAlign = ContentAlignment.MiddleRight;
            btnEditor.TextImageRelation = TextImageRelation.TextBeforeImage;
            // 
            // ToolStrip1
            // 
            ToolStrip1.BackColor = Color.White;
            ToolStrip1.GripStyle = ToolStripGripStyle.Hidden;
            ToolStrip1.Items.AddRange(new ToolStripItem[] { ToolStripLabel1, txtName, ToolStripSeparator6, btnHelp, ToolStripSeparator4 });
            ToolStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            ToolStrip1.Location = new Point(0, 0);
            ToolStrip1.Name = "ToolStrip1";
            ToolStrip1.RenderMode = ToolStripRenderMode.Professional;
            ToolStrip1.Size = new Size(979, 25);
            ToolStrip1.TabIndex = 2;
            ToolStrip1.Text = "ToolStrip1";
            // 
            // ToolStrip2
            // 
            ToolStrip2.BackColor = Color.White;
            ToolStrip2.GripStyle = ToolStripGripStyle.Hidden;
            ToolStrip2.Items.AddRange(new ToolStripItem[] { btnZoomin, btnZoomout, btnFitAll, ToolStripSeparator11, btnBasefontplus, btnBasefontminus, ToolStripSeparator10, btnDisplay, ToolStripSeparator12, btnSpace, btnHover, ToolStripSeparator13, btnLayers, btn3D, btnDragMode });
            ToolStrip2.Location = new Point(0, 25);
            ToolStrip2.Name = "ToolStrip2";
            ToolStrip2.RenderMode = ToolStripRenderMode.Professional;
            ToolStrip2.Size = new Size(979, 25);
            ToolStrip2.TabIndex = 6;
            ToolStrip2.Text = "ToolStrip2";
            // 
            // ToolStripLabel1
            // 
            ToolStripLabel1.ForeColor = Color.DimGray;
            ToolStripLabel1.Name = "ToolStripLabel1";
            ToolStripLabel1.Size = new Size(80, 22);
            ToolStripLabel1.Text = "Project name:";
            // 
            // txtName
            // 
            txtName.AutoToolTip = true;
            txtName.FlatStyle = FlatStyle.Flat;
            txtName.Name = "txtName";
            txtName.Size = new Size(240, 25);
            txtName.ToolTipText = "Name of the project you are working with";
            // 
            // ctxAddMenu
            // 
            ctxAddMenu.Items.AddRange(new ToolStripItem[] { AVLTemplateToolStripMenuItem, SurfaceToolStripMenuItem, SectionToolStripMenuItem, ControlToolStripMenuItem, ToolStripSeparator1, MassTemplateToolStripMenuItem, ToolStripSeparator2, RunTemplateToolStripMenuItem, ToolStripSeparator5, SeparatorToolStripMenuItem });
            ctxAddMenu.Name = "ctxAddMenu";
            ctxAddMenu.Size = new Size(154, 176);
            // 
            // btnTabIncrease
            // 
            btnTabIncrease.Size = new Size(30, 30);
            btnTabIncrease.Location = new Point(274, 8);
            btnTabIncrease.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTabIncrease.Text = "⇥";
            btnTabIncrease.TextAlign = ContentAlignment.MiddleCenter;
            btnTabIncrease.Font = new Font("Segoe UI", 14.0f, FontStyle.Bold);
            btnTabIncrease.FlatStyle = FlatStyle.Flat;
            btnTabIncrease.FlatAppearance.BorderSize = 1;
            btnTabIncrease.FlatAppearance.BorderColor = Color.LightGray;
            btnTabIncrease.BackColor = Color.White;
            btnTabIncrease.Cursor = Cursors.Hand;
            btnTabIncrease.Name = "btnTabIncrease";
            _tt.SetToolTip(btnTabIncrease, "Increase the editor's auto-space column width");
            // 
            // btnTabDecrease
            // 
            btnTabDecrease.Size = new Size(30, 30);
            btnTabDecrease.Location = new Point(240, 8);
            btnTabDecrease.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTabDecrease.Text = "⇤";
            btnTabDecrease.TextAlign = ContentAlignment.MiddleCenter;
            btnTabDecrease.Font = new Font("Segoe UI", 14.0f, FontStyle.Bold);
            btnTabDecrease.FlatStyle = FlatStyle.Flat;
            btnTabDecrease.FlatAppearance.BorderSize = 1;
            btnTabDecrease.FlatAppearance.BorderColor = Color.LightGray;
            btnTabDecrease.BackColor = Color.White;
            btnTabDecrease.Cursor = Cursors.Hand;
            btnTabDecrease.Name = "btnTabDecrease";
            _tt.SetToolTip(btnTabDecrease, "Decrease the editor's auto-space column width");
            // 
            // btnAdd
            // 
            btnAdd.Size = new Size(30, 30);
            btnAdd.Location = new Point(376, 8);
            btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAdd.Text = "➕";
            btnAdd.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.FlatAppearance.BorderSize = 1;
            btnAdd.FlatAppearance.BorderColor = Color.LightGray;
            btnAdd.BackColor = Color.White;
            btnAdd.Cursor = Cursors.Hand;
            btnAdd.Name = "btnAdd";
            _tt.SetToolTip(btnAdd, "Insert a template block (surface, section, control, etc.) at the cursor");
            // 
            // btnPrettify
            // 
            btnPrettify.Size = new Size(30, 30);
            btnPrettify.Location = new Point(342, 8);
            btnPrettify.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPrettify.Text = "✨";
            btnPrettify.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);
            btnPrettify.FlatStyle = FlatStyle.Flat;
            btnPrettify.FlatAppearance.BorderSize = 1;
            btnPrettify.FlatAppearance.BorderColor = Color.LightGray;
            btnPrettify.BackColor = Color.White;
            btnPrettify.Cursor = Cursors.Hand;
            btnPrettify.Name = "btnPrettify";
            _tt.SetToolTip(btnPrettify, "Auto-indent/reformat the current file's text");
            // 
            // btnValidate
            // 
            btnValidate.Size = new Size(30, 30);
            btnValidate.Location = new Point(308, 8);
            btnValidate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnValidate.Text = "🧞";
            btnValidate.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);
            btnValidate.FlatStyle = FlatStyle.Flat;
            btnValidate.FlatAppearance.BorderSize = 1;
            btnValidate.FlatAppearance.BorderColor = Color.LightGray;
            btnValidate.BackColor = Color.White;
            btnValidate.Cursor = Cursors.Hand;
            btnValidate.Name = "btnValidate";
            _tt.SetToolTip(btnValidate, "Check the active file for errors/warnings and highlight them");
            // 
            // AVLTemplateToolStripMenuItem
            // 
            AVLTemplateToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { AVLTemplateFullToolStripMenuItem, AVLTemplateMinimalToolStripMenuItem });
            AVLTemplateToolStripMenuItem.Name = "AVLTemplateToolStripMenuItem";
            AVLTemplateToolStripMenuItem.Size = new Size(153, 22);
            AVLTemplateToolStripMenuItem.Text = "AVL Template (Replace All)";
            AVLTemplateToolStripMenuItem.ToolTipText = "Replaces the entire Geometry tab with a blank starter .avl file.";
            // 
            // AVLTemplateFullToolStripMenuItem
            // 
            AVLTemplateFullToolStripMenuItem.Name = "AVLTemplateFullToolStripMenuItem";
            AVLTemplateFullToolStripMenuItem.Size = new Size(219, 22);
            AVLTemplateFullToolStripMenuItem.Text = "Full (with Explanations)";
            AVLTemplateFullToolStripMenuItem.ToolTipText = "Every field is preceded by a comment explaining what it does and whether it's optional.";
            // 
            // AVLTemplateMinimalToolStripMenuItem
            // 
            AVLTemplateMinimalToolStripMenuItem.Name = "AVLTemplateMinimalToolStripMenuItem";
            AVLTemplateMinimalToolStripMenuItem.Size = new Size(219, 22);
            AVLTemplateMinimalToolStripMenuItem.Text = "Minimal (Fields Only)";
            AVLTemplateMinimalToolStripMenuItem.ToolTipText = "Just the field names and values, matching AVL's own generated file style - no explanatory comments.";
            // 
            // SurfaceToolStripMenuItem
            // 
            SurfaceToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { SurfaceFullToolStripMenuItem, SurfaceMinimalToolStripMenuItem });
            SurfaceToolStripMenuItem.Name = "SurfaceToolStripMenuItem";
            SurfaceToolStripMenuItem.Size = new Size(153, 22);
            SurfaceToolStripMenuItem.Text = "Insert Surface Block";
            SurfaceToolStripMenuItem.ToolTipText = "Inserts a new SURFACE block (name, panel spacing, YDUPLICATE, ANGLE) at the cursor, inside !begingeometry/!endgeometry.";
            // 
            // SurfaceFullToolStripMenuItem
            // 
            SurfaceFullToolStripMenuItem.Name = "SurfaceFullToolStripMenuItem";
            SurfaceFullToolStripMenuItem.Size = new Size(219, 22);
            SurfaceFullToolStripMenuItem.Text = "Full (with Explanations)";
            SurfaceFullToolStripMenuItem.ToolTipText = "Every field is preceded by a comment explaining what it does and whether it's optional.";
            // 
            // SurfaceMinimalToolStripMenuItem
            // 
            SurfaceMinimalToolStripMenuItem.Name = "SurfaceMinimalToolStripMenuItem";
            SurfaceMinimalToolStripMenuItem.Size = new Size(219, 22);
            SurfaceMinimalToolStripMenuItem.Text = "Minimal (Fields Only)";
            SurfaceMinimalToolStripMenuItem.ToolTipText = "Just the field names and values, matching AVL's own generated file style - no explanatory comments.";
            // 
            // SectionToolStripMenuItem
            // 
            SectionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { SectionFullToolStripMenuItem, SectionMinimalToolStripMenuItem });
            SectionToolStripMenuItem.Name = "SectionToolStripMenuItem";
            SectionToolStripMenuItem.Size = new Size(153, 22);
            SectionToolStripMenuItem.Text = "Insert Section Block";
            SectionToolStripMenuItem.ToolTipText = "Inserts a new SECTION block (Xle/Yle/Zle/Chord/Ainc plus a NACA airfoil) at the cursor, inside a SURFACE.";
            // 
            // SectionFullToolStripMenuItem
            // 
            SectionFullToolStripMenuItem.Name = "SectionFullToolStripMenuItem";
            SectionFullToolStripMenuItem.Size = new Size(219, 22);
            SectionFullToolStripMenuItem.Text = "Full (with Explanations)";
            SectionFullToolStripMenuItem.ToolTipText = "Every field is preceded by a comment explaining what it does and whether it's optional.";
            // 
            // SectionMinimalToolStripMenuItem
            // 
            SectionMinimalToolStripMenuItem.Name = "SectionMinimalToolStripMenuItem";
            SectionMinimalToolStripMenuItem.Size = new Size(219, 22);
            SectionMinimalToolStripMenuItem.Text = "Minimal (Fields Only)";
            SectionMinimalToolStripMenuItem.ToolTipText = "Just the field names and values, matching AVL's own generated file style - no explanatory comments.";
            // 
            // ControlToolStripMenuItem
            // 
            ControlToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { ControlFullToolStripMenuItem, ControlMinimalToolStripMenuItem });
            ControlToolStripMenuItem.Name = "ControlToolStripMenuItem";
            ControlToolStripMenuItem.Size = new Size(153, 22);
            ControlToolStripMenuItem.Text = "Insert Control (Hinge) Block";
            ControlToolStripMenuItem.ToolTipText = "Inserts a new CONTROL block (a deflectable hinge, e.g. flap/aileron/elevator) at the cursor, inside a SECTION.";
            // 
            // ControlFullToolStripMenuItem
            // 
            ControlFullToolStripMenuItem.Name = "ControlFullToolStripMenuItem";
            ControlFullToolStripMenuItem.Size = new Size(219, 22);
            ControlFullToolStripMenuItem.Text = "Full (with Explanations)";
            ControlFullToolStripMenuItem.ToolTipText = "Every field is preceded by a comment explaining what it does and whether it's optional.";
            // 
            // ControlMinimalToolStripMenuItem
            // 
            ControlMinimalToolStripMenuItem.Name = "ControlMinimalToolStripMenuItem";
            ControlMinimalToolStripMenuItem.Size = new Size(219, 22);
            ControlMinimalToolStripMenuItem.Text = "Minimal (Fields Only)";
            ControlMinimalToolStripMenuItem.ToolTipText = "Just the field names and values, matching AVL's own generated file style - no explanatory comments.";
            // 
            // ToolStripSeparator1
            // 
            ToolStripSeparator1.Name = "ToolStripSeparator1";
            ToolStripSeparator1.Size = new Size(150, 6);
            // 
            // MassTemplateToolStripMenuItem
            // 
            MassTemplateToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { MassTemplateFullToolStripMenuItem, MassTemplateMinimalToolStripMenuItem });
            MassTemplateToolStripMenuItem.Name = "MassTemplateToolStripMenuItem";
            MassTemplateToolStripMenuItem.Size = new Size(153, 22);
            MassTemplateToolStripMenuItem.Text = "Mass Template (Replace All)";
            MassTemplateToolStripMenuItem.ToolTipText = "Replaces the entire Mass tab with a blank starter .mass file.";
            // 
            // MassTemplateFullToolStripMenuItem
            // 
            MassTemplateFullToolStripMenuItem.Name = "MassTemplateFullToolStripMenuItem";
            MassTemplateFullToolStripMenuItem.Size = new Size(219, 22);
            MassTemplateFullToolStripMenuItem.Text = "Full (with Explanations)";
            MassTemplateFullToolStripMenuItem.ToolTipText = "Every field is preceded by a comment explaining what it does and whether it's optional.";
            // 
            // MassTemplateMinimalToolStripMenuItem
            // 
            MassTemplateMinimalToolStripMenuItem.Name = "MassTemplateMinimalToolStripMenuItem";
            MassTemplateMinimalToolStripMenuItem.Size = new Size(219, 22);
            MassTemplateMinimalToolStripMenuItem.Text = "Minimal (Fields Only)";
            MassTemplateMinimalToolStripMenuItem.ToolTipText = "Just the field names and values, matching AVL's own generated file style - no explanatory comments.";
            // 
            // ToolStripSeparator2
            // 
            ToolStripSeparator2.Name = "ToolStripSeparator2";
            ToolStripSeparator2.Size = new Size(150, 6);
            // 
            // RunTemplateToolStripMenuItem
            // 
            RunTemplateToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { RunTemplateFullToolStripMenuItem, RunTemplateMinimalToolStripMenuItem });
            RunTemplateToolStripMenuItem.Name = "RunTemplateToolStripMenuItem";
            RunTemplateToolStripMenuItem.Size = new Size(153, 22);
            RunTemplateToolStripMenuItem.Text = "Run Template (Replace All)";
            RunTemplateToolStripMenuItem.ToolTipText = "Replaces the entire Run tab with a blank starter .run file.";
            // 
            // RunTemplateFullToolStripMenuItem
            // 
            RunTemplateFullToolStripMenuItem.Name = "RunTemplateFullToolStripMenuItem";
            RunTemplateFullToolStripMenuItem.Size = new Size(219, 22);
            RunTemplateFullToolStripMenuItem.Text = "Full (with Explanations)";
            RunTemplateFullToolStripMenuItem.ToolTipText = "Every constraint line is preceded by a comment explaining the format and the one-constraint-per-target rule.";
            // 
            // RunTemplateMinimalToolStripMenuItem
            // 
            RunTemplateMinimalToolStripMenuItem.Name = "RunTemplateMinimalToolStripMenuItem";
            RunTemplateMinimalToolStripMenuItem.Size = new Size(219, 22);
            RunTemplateMinimalToolStripMenuItem.Text = "Minimal (Fields Only)";
            RunTemplateMinimalToolStripMenuItem.ToolTipText = "Just the field names and values, matching AVL's own generated file style - no explanatory comments.";
            // 
            // ToolStripSeparator5
            // 
            ToolStripSeparator5.Name = "ToolStripSeparator5";
            ToolStripSeparator5.Size = new Size(150, 6);
            // 
            // SeparatorToolStripMenuItem
            // 
            SeparatorToolStripMenuItem.Name = "SeparatorToolStripMenuItem";
            SeparatorToolStripMenuItem.Size = new Size(153, 22);
            SeparatorToolStripMenuItem.Text = "Insert Comment Divider";
            SeparatorToolStripMenuItem.ToolTipText = "Inserts a '#===...' comment line at the cursor - a purely visual divider with no effect on AVL.";
            // 
            // btnClear
            // 
            btnClear.Size = new Size(30, 30);
            btnClear.Location = new Point(478, 8);
            btnClear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClear.Text = "🗑";
            btnClear.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.FlatAppearance.BorderSize = 1;
            btnClear.FlatAppearance.BorderColor = Color.LightGray;
            btnClear.BackColor = Color.White;
            btnClear.Cursor = Cursors.Hand;
            btnClear.Name = "btnClear";
            _tt.SetToolTip(btnClear, "Clear all text in the active tab (Geometry/Mass/Run) - asks for confirmation");
            // 
            // btnDragMode
            // 
            btnDragMode.Alignment = ToolStripItemAlignment.Right;
            btnDragMode.BackColor = Color.FromArgb(220, 220, 220);
            btnDragMode.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnDragMode.Name = "btnDragMode";
            btnDragMode.Size = new Size(100, 22);
            btnDragMode.Text = "Drag Nodes: Off";
            btnDragMode.ToolTipText = "Toggle drag-node mode: click and drag section or mass nodes to reposition them";
            // 
            // ToolStripSeparator6
            // 
            ToolStripSeparator6.Name = "ToolStripSeparator6";
            ToolStripSeparator6.Size = new Size(6, 25);
            // 
            // btnUndo
            // 
            btnUndo.Size = new Size(30, 30);
            btnUndo.Location = new Point(410, 8);
            btnUndo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnUndo.Text = "↶";
            btnUndo.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);
            btnUndo.FlatStyle = FlatStyle.Flat;
            btnUndo.FlatAppearance.BorderSize = 1;
            btnUndo.FlatAppearance.BorderColor = Color.LightGray;
            btnUndo.BackColor = Color.White;
            btnUndo.Cursor = Cursors.Hand;
            btnUndo.Enabled = false;
            btnUndo.Name = "btnUndo";
            _tt.SetToolTip(btnUndo, "Undo the last edit in the active editor tab");
            // 
            // btnRedo
            // 
            btnRedo.Size = new Size(30, 30);
            btnRedo.Location = new Point(444, 8);
            btnRedo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRedo.Text = "↷";
            btnRedo.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);
            btnRedo.FlatStyle = FlatStyle.Flat;
            btnRedo.FlatAppearance.BorderSize = 1;
            btnRedo.FlatAppearance.BorderColor = Color.LightGray;
            btnRedo.BackColor = Color.White;
            btnRedo.Cursor = Cursors.Hand;
            btnRedo.Enabled = false;
            btnRedo.Name = "btnRedo";
            _tt.SetToolTip(btnRedo, "Redo the last undone edit in the active editor tab");
            // 
            // ToolStripSeparator3
            // 
            ToolStripSeparator3.Name = "ToolStripSeparator3";
            ToolStripSeparator3.Size = new Size(6, 25);
            // 
            // btnHelp
            // 
            btnHelp.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnHelp.DropDownItems.AddRange(new ToolStripItem[] { btnHelpFull, ToolStripSeparator8, btnHelpAVL, btnHelpMass, btnHelpRun, ToolStripSeparator9, btnHelpCommands });
            btnHelp.Image = (Image)resources.GetObject("btnHelp.Image");
            btnHelp.ImageTransparentColor = Color.Magenta;
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(25, 22);
            btnHelp.Text = "?";
            // 
            // btnHelpFull
            // 
            btnHelpFull.Name = "btnHelpFull";
            btnHelpFull.Size = new Size(185, 22);
            btnHelpFull.Text = "Full Help Document";
            btnHelpFull.ToolTipText = "Open the complete AVL help/documentation text";
            // 
            // ToolStripSeparator8
            // 
            ToolStripSeparator8.Name = "ToolStripSeparator8";
            ToolStripSeparator8.Size = new Size(182, 6);
            // 
            // btnHelpAVL
            // 
            btnHelpAVL.Name = "btnHelpAVL";
            btnHelpAVL.Size = new Size(185, 22);
            btnHelpAVL.Text = "Geometry File (.avl)";
            btnHelpAVL.ToolTipText = "Open help for the Geometry (.avl) file format";
            // 
            // btnHelpMass
            // 
            btnHelpMass.Name = "btnHelpMass";
            btnHelpMass.Size = new Size(185, 22);
            btnHelpMass.Text = "Mass File (.mass)";
            btnHelpMass.ToolTipText = "Open help for the Mass (.mass) file format";
            // 
            // btnHelpRun
            // 
            btnHelpRun.Name = "btnHelpRun";
            btnHelpRun.Size = new Size(185, 22);
            btnHelpRun.Text = "Run File (.run)";
            btnHelpRun.ToolTipText = "Open help for the Run (.run) case file format";
            // 
            // ToolStripSeparator9
            // 
            ToolStripSeparator9.Name = "ToolStripSeparator9";
            ToolStripSeparator9.Size = new Size(182, 6);
            // 
            // btnHelpCommands
            // 
            btnHelpCommands.Name = "btnHelpCommands";
            btnHelpCommands.Size = new Size(185, 22);
            btnHelpCommands.Text = "Program Commands";
            btnHelpCommands.ToolTipText = "Open help for AVL's program (console) commands";
            // 
            // ToolStripSeparator4
            // 
            ToolStripSeparator4.Name = "ToolStripSeparator4";
            ToolStripSeparator4.Size = new Size(6, 25);
            // 
            // btn3D
            // 
            btn3D.Alignment = ToolStripItemAlignment.Right;
            btn3D.BackColor = Color.Lime;
            btn3D.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btn3D.Image = (Image)resources.GetObject("btn3D.Image");
            btn3D.ImageTransparentColor = Color.Magenta;
            btn3D.Name = "btn3D";
            btn3D.Size = new Size(80, 22);
            btn3D.Text = "3D: Off";
            btn3D.ToolTipText = "Show 3D: Off";
            // 
            // FileSystemWatcher1
            // 
            FileSystemWatcher1.EnableRaisingEvents = true;
            FileSystemWatcher1.SynchronizingObject = this;
            // 
            // btnZoomin
            // 
            btnZoomin.Alignment = ToolStripItemAlignment.Right;
            btnZoomin.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnZoomin.Image = (Image)resources.GetObject("btnZoomin.Image");
            btnZoomin.ImageTransparentColor = Color.Magenta;
            btnZoomin.Name = "btnZoomin";
            btnZoomin.Size = new Size(23, 22);
            btnZoomin.Text = "Zoom in";
            btnZoomin.ToolTipText = "Zoom in on the geometry plot";
            // 
            // btnZoomout
            // 
            btnZoomout.Alignment = ToolStripItemAlignment.Right;
            btnZoomout.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnZoomout.Image = (Image)resources.GetObject("btnZoomout.Image");
            btnZoomout.ImageTransparentColor = Color.Magenta;
            btnZoomout.Name = "btnZoomout";
            btnZoomout.Size = new Size(23, 22);
            btnZoomout.Text = "Zoom out";
            btnZoomout.ToolTipText = "Zoom out on the geometry plot";
            // 
            // btnFitAll
            // 
            btnFitAll.Alignment = ToolStripItemAlignment.Right;
            btnFitAll.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnFitAll.Image = (Image)resources.GetObject("btnFitAll.Image");
            btnFitAll.ImageTransparentColor = Color.Magenta;
            btnFitAll.Name = "btnFitAll";
            btnFitAll.Size = new Size(23, 22);
            btnFitAll.Text = "Fit All";
            btnFitAll.ToolTipText = "Fit all geometry and mass points into the views";
            // 
            // ToolStripSeparator11
            // 
            ToolStripSeparator11.Alignment = ToolStripItemAlignment.Right;
            ToolStripSeparator11.Name = "ToolStripSeparator11";
            ToolStripSeparator11.Size = new Size(6, 25);
            // 
            // btnBasefontplus
            // 
            btnBasefontplus.Alignment = ToolStripItemAlignment.Right;
            btnBasefontplus.ImageTransparentColor = Color.Magenta;
            btnBasefontplus.Name = "btnBasefontplus";
            btnBasefontplus.Size = new Size(43, 22);
            btnBasefontplus.Text = "Font+";
            btnBasefontplus.ToolTipText = "Increase the plot's label font size";
            // 
            // btnBasefontminus
            // 
            btnBasefontminus.Alignment = ToolStripItemAlignment.Right;
            btnBasefontminus.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnBasefontminus.Image = (Image)resources.GetObject("btnBasefontminus.Image");
            btnBasefontminus.ImageTransparentColor = Color.Magenta;
            btnBasefontminus.Name = "btnBasefontminus";
            btnBasefontminus.Size = new Size(40, 22);
            btnBasefontminus.Text = "Font-";
            btnBasefontminus.ToolTipText = "Decrease the plot's label font size";
            // 
            // ToolStripSeparator10
            // 
            ToolStripSeparator10.Alignment = ToolStripItemAlignment.Right;
            ToolStripSeparator10.Name = "ToolStripSeparator10";
            ToolStripSeparator10.Size = new Size(6, 25);
            // 
            // btnDisplay
            // 
            btnDisplay.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnDisplay.ImageTransparentColor = Color.Magenta;
            btnDisplay.Name = "btnDisplay";
            btnDisplay.Size = new Size(102, 22);
            btnDisplay.Text = "Editor Only";
            btnDisplay.Visible = false;
            // 
            // ToolStripSeparator12
            // 
            ToolStripSeparator12.Name = "ToolStripSeparator12";
            ToolStripSeparator12.Size = new Size(6, 25);
            ToolStripSeparator12.Visible = false;
            // 
            // btnSpace
            // 
            btnSpace.BackColor = Color.FromArgb(192, 192, 255);
            btnSpace.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnSpace.Image = (Image)resources.GetObject("btnSpace.Image");
            btnSpace.ImageTransparentColor = Color.Magenta;
            btnSpace.Name = "btnSpace";
            btnSpace.Size = new Size(93, 22);
            btnSpace.Text = "Auto Space: On";
            btnSpace.ToolTipText = "Toggle auto-indent/auto-spacing of the editor text (reformats immediately)";
            // 
            // btnHover
            // 
            btnHover.BackColor = Color.FromArgb(128, 255, 128);
            btnHover.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnHover.Image = (Image)resources.GetObject("btnHover.Image");
            btnHover.ImageTransparentColor = Color.Magenta;
            btnHover.Name = "btnHover";
            btnHover.Size = new Size(118, 22);
            btnHover.Text = "Highlight Hover: On";
            btnHover.ToolTipText = "Toggle highlighting of elements under the mouse in the viewport";
            // 
            // ToolStripSeparator13
            // 
            ToolStripSeparator13.Name = "ToolStripSeparator13";
            ToolStripSeparator13.Size = new Size(6, 25);
            // 
            // btnLayers
            // 
            btnLayers.Alignment = ToolStripItemAlignment.Right;
            btnLayers.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnLayers.DropDownItems.AddRange(new ToolStripItem[] { mnuLayerSection, mnuLayerMass, mnuLayerControl, mnuLayerMesh, ToolStripSeparator14, mnuLayerChordline, mnuLayerControlPoints, mnuLayerAxes, ToolStripSeparator15, mnuLayerCamberline, mnuLayerNormalVector, mnuLayerBoundLeg, mnuLayerTrailingLegs, mnuLayerLoading, mnuLayerOffBody });
            btnLayers.Name = "btnLayers";
            btnLayers.Size = new Size(66, 22);
            btnLayers.Text = "Display";
            btnLayers.ToolTipText = "Toggle geometry plot overlays";
            // 
            // mnuLayerSection
            // 
            mnuLayerSection.CheckOnClick = true;
            mnuLayerSection.Checked = true;
            mnuLayerSection.Name = "mnuLayerSection";
            mnuLayerSection.Size = new Size(230, 22);
            mnuLayerSection.Text = "Section points";
            mnuLayerSection.ToolTipText = "Show the defined section (leading-edge) points";
            // 
            // mnuLayerMass
            // 
            mnuLayerMass.CheckOnClick = true;
            mnuLayerMass.Checked = true;
            mnuLayerMass.Name = "mnuLayerMass";
            mnuLayerMass.Size = new Size(230, 22);
            mnuLayerMass.Text = "Mass distribution";
            mnuLayerMass.ToolTipText = "Overlays the mass distribution. Note that the mass points are scaled based on their mass values";
            // 
            // mnuLayerControl
            // 
            mnuLayerControl.CheckOnClick = true;
            mnuLayerControl.Checked = true;
            mnuLayerControl.Name = "mnuLayerControl";
            mnuLayerControl.Size = new Size(230, 22);
            mnuLayerControl.Text = "Control surfaces";
            mnuLayerControl.ToolTipText = "Highlight control-surface (hinge) markers, e.g. flaps/ailerons/elevators";
            // 
            // mnuLayerMesh
            // 
            mnuLayerMesh.CheckOnClick = true;
            mnuLayerMesh.Name = "mnuLayerMesh";
            mnuLayerMesh.Size = new Size(230, 22);
            mnuLayerMesh.Text = "Vortex-lattice mesh";
            mnuLayerMesh.ToolTipText = "Overlay the AVL vortex-lattice panel mesh (Nchord × Nspan panels per surface)";
            // 
            // ToolStripSeparator14
            // 
            ToolStripSeparator14.Name = "ToolStripSeparator14";
            ToolStripSeparator14.Size = new Size(227, 6);
            // 
            // mnuLayerChordline
            // 
            mnuLayerChordline.CheckOnClick = true;
            mnuLayerChordline.Name = "mnuLayerChordline";
            mnuLayerChordline.Size = new Size(230, 22);
            mnuLayerChordline.Text = "Chordline";
            mnuLayerChordline.ToolTipText = "Draw the leading-edge-to-trailing-edge line at each defined section";
            // 
            // mnuLayerControlPoints
            // 
            mnuLayerControlPoints.CheckOnClick = true;
            mnuLayerControlPoints.Name = "mnuLayerControlPoints";
            mnuLayerControlPoints.Size = new Size(230, 22);
            mnuLayerControlPoints.Text = "Control points";
            mnuLayerControlPoints.ToolTipText = "Approximate vortex-lattice control (collocation) points, one per panel";
            // 
            // mnuLayerAxes
            // 
            mnuLayerAxes.CheckOnClick = true;
            mnuLayerAxes.Name = "mnuLayerAxes";
            mnuLayerAxes.Size = new Size(230, 22);
            mnuLayerAxes.Text = "Axes, xyz ref.";
            mnuLayerAxes.ToolTipText = "Draw an X/Y/Z axis triad at the world origin";
            // 
            // ToolStripSeparator15
            // 
            ToolStripSeparator15.Name = "ToolStripSeparator15";
            ToolStripSeparator15.Size = new Size(227, 6);
            // 
            // mnuLayerCamberline
            // 
            mnuLayerCamberline.CheckOnClick = true;
            mnuLayerCamberline.Name = "mnuLayerCamberline";
            mnuLayerCamberline.Size = new Size(230, 22);
            mnuLayerCamberline.Text = "Camberline";
            mnuLayerCamberline.ToolTipText = "Camber-line shape at each NACA 4-digit section (AIRFOIL/AFILE sections aren't supported yet)";
            // 
            // mnuLayerNormalVector
            // 
            mnuLayerNormalVector.CheckOnClick = true;
            mnuLayerNormalVector.Name = "mnuLayerNormalVector";
            mnuLayerNormalVector.Size = new Size(230, 22);
            mnuLayerNormalVector.Text = "Normal vector";
            mnuLayerNormalVector.ToolTipText = "Approximate outward panel-normal tick at each panel center";
            // 
            // mnuLayerBoundLeg
            // 
            mnuLayerBoundLeg.CheckOnClick = true;
            mnuLayerBoundLeg.Name = "mnuLayerBoundLeg";
            mnuLayerBoundLeg.Size = new Size(230, 22);
            mnuLayerBoundLeg.Text = "Bound leg";
            mnuLayerBoundLeg.ToolTipText = "Approximate vortex-lattice bound leg (1/4-chord) per panel";
            // 
            // mnuLayerTrailingLegs
            // 
            mnuLayerTrailingLegs.CheckOnClick = true;
            mnuLayerTrailingLegs.Name = "mnuLayerTrailingLegs";
            mnuLayerTrailingLegs.Size = new Size(230, 22);
            mnuLayerTrailingLegs.Text = "Trailing legs";
            mnuLayerTrailingLegs.ToolTipText = "Approximate trailing vortex legs shed downstream from each panel";
            // 
            // mnuLayerLoading
            // 
            mnuLayerLoading.CheckOnClick = true;
            mnuLayerLoading.Name = "mnuLayerLoading";
            mnuLayerLoading.Size = new Size(230, 22);
            mnuLayerLoading.Text = "Loading";
            mnuLayerLoading.ToolTipText = "Spanwise c*cl loading curve - requires a Trefftz Plane run first (Analysis menu)";
            // 
            // mnuLayerOffBody
            // 
            mnuLayerOffBody.Enabled = false;
            mnuLayerOffBody.Name = "mnuLayerOffBody";
            mnuLayerOffBody.Size = new Size(230, 22);
            mnuLayerOffBody.Text = "Off-body points";
            mnuLayerOffBody.ToolTipText = "Not yet available - AVL off-body point surveys aren't parsed by this app";
            // 
            // sc1
            // 
            sc1.BackColor = Color.WhiteSmoke;
            sc1.Dock = DockStyle.Fill;
            sc1.Location = new Point(0, 50);
            sc1.Name = "sc1";
            sc1.Orientation = Orientation.Horizontal;
            // 
            // sc1.Panel1
            // 
            sc1.Panel1.Controls.Add(scup);
            sc1.Panel1.Padding = new Padding(5);
            // 
            // sc1.Panel2
            // 
            sc1.Panel2.Controls.Add(scdown);
            sc1.Panel2.Padding = new Padding(5);
            sc1.Size = new Size(979, 544);
            sc1.SplitterDistance = 311;
            sc1.TabIndex = 7;
            // 
            // scup
            // 
            scup.BackColor = Color.WhiteSmoke;
            scup.Dock = DockStyle.Fill;
            scup.Location = new Point(5, 5);
            scup.Name = "scup";
            // 
            // scup.Panel1
            // 
            scup.Panel1.Controls.Add(tc1);
            // 
            // scup.Panel2
            // 
            scup.Panel2.Controls.Add(p3d);
            scup.Panel2.Controls.Add(pxy);
            scup.Size = new Size(969, 301);
            scup.SplitterDistance = 520;
            scup.TabIndex = 0;
            // 
            // tc1
            // 
            tc1.Appearance = TabAppearance.FlatButtons;
            tc1.Controls.Add(Geometry);
            tc1.Controls.Add(Mass);
            tc1.Controls.Add(Run);
            tc1.Dock = DockStyle.Fill;
            tc1.ImageList = ImageList1;
            tc1.Location = new Point(0, 0);
            tc1.Name = "tc1";
            tc1.SelectedIndex = 0;
            tc1.Size = new Size(520, 301);
            tc1.TabIndex = 0;
            // 
            // Geometry
            // 
            Geometry.ImageIndex = 0;
            Geometry.Location = new Point(4, 27);
            Geometry.Name = "Geometry";
            Geometry.Padding = new Padding(3);
            Geometry.Size = new Size(512, 270);
            Geometry.TabIndex = 0;
            Geometry.Text = "Geometry";
            // 
            // Mass
            // 
            Mass.ImageIndex = 1;
            Mass.Location = new Point(4, 27);
            Mass.Name = "Mass";
            Mass.Padding = new Padding(3);
            Mass.Size = new Size(512, 270);
            Mass.TabIndex = 1;
            Mass.Text = "Mass";
            // 
            // Run
            // 
            Run.ImageIndex = 2;
            Run.Location = new Point(4, 27);
            Run.Name = "Run";
            Run.Size = new Size(512, 270);
            Run.TabIndex = 2;
            Run.Text = "Run";
            // 
            // ImageList1
            // 
            ImageList1.ColorDepth = ColorDepth.Depth8Bit;
            ImageList1.ImageStream = (ImageListStreamer)resources.GetObject("ImageList1.ImageStream");
            ImageList1.TransparentColor = Color.Transparent;
            ImageList1.Images.SetKeyName(0, "drafting-compass.png");
            ImageList1.Images.SetKeyName(1, "gym.png");
            ImageList1.Images.SetKeyName(2, "running.png");
            // 
            // p3d
            // 
            p3d.BackColor = Color.White;
            p3d.Location = new Point(194, 80);
            p3d.Name = "p3d";
            p3d.Size = new Size(100, 50);
            p3d.TabIndex = 1;
            p3d.TabStop = false;
            p3d.Visible = false;
            // 
            // pxy
            // 
            pxy.BackColor = Color.White;
            pxy.BorderStyle = BorderStyle.FixedSingle;
            pxy.Dock = DockStyle.Fill;
            pxy.Location = new Point(0, 0);
            pxy.Name = "pxy";
            pxy.Size = new Size(445, 301);
            pxy.TabIndex = 0;
            pxy.TabStop = false;
            // 
            // scdown
            // 
            scdown.BackColor = Color.WhiteSmoke;
            scdown.Dock = DockStyle.Fill;
            scdown.Location = new Point(5, 5);
            scdown.Name = "scdown";
            // 
            // scdown.Panel1
            // 
            scdown.Panel1.Controls.Add(pyz);
            // 
            // scdown.Panel2
            // 
            scdown.Panel2.Controls.Add(pxz);
            scdown.Size = new Size(969, 219);
            scdown.SplitterDistance = 520;
            scdown.TabIndex = 0;
            // 
            // pyz
            // 
            pyz.BackColor = Color.White;
            pyz.BorderStyle = BorderStyle.FixedSingle;
            pyz.Dock = DockStyle.Fill;
            pyz.Location = new Point(0, 0);
            pyz.Name = "pyz";
            pyz.Size = new Size(520, 219);
            pyz.TabIndex = 3;
            pyz.TabStop = false;
            // 
            // pxz
            // 
            pxz.BackColor = Color.White;
            pxz.BorderStyle = BorderStyle.FixedSingle;
            pxz.Dock = DockStyle.Fill;
            pxz.Location = new Point(0, 0);
            pxz.Name = "pxz";
            pxz.Size = new Size(445, 219);
            pxz.TabIndex = 0;
            pxz.TabStop = false;
            // 
            // frmGeometry
            // 
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
            ClientSize = new Size(979, 616);
            Controls.Add(sc1);
            Controls.Add(ToolStrip2);
            Controls.Add(ToolStrip1);
            Controls.Add(StatusStrip1);
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "frmGeometry";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AVL - Designer";
            StatusStrip1.ResumeLayout(false);
            StatusStrip1.PerformLayout();
            ToolStrip1.ResumeLayout(false);
            ToolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)FileSystemWatcher1).EndInit();
            ToolStrip2.ResumeLayout(false);
            ToolStrip2.PerformLayout();
            sc1.Panel1.ResumeLayout(false);
            sc1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)sc1).EndInit();
            sc1.ResumeLayout(false);
            scup.Panel1.ResumeLayout(false);
            scup.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)scup).EndInit();
            scup.ResumeLayout(false);
            tc1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)p3d).EndInit();
            ((System.ComponentModel.ISupportInitialize)pxy).EndInit();
            scdown.Panel1.ResumeLayout(false);
            scdown.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)scdown).EndInit();
            scdown.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pyz).EndInit();
            ((System.ComponentModel.ISupportInitialize)pxz).EndInit();
            Load += new EventHandler(frmGeometry_Load);
            Resize += new EventHandler(frmGeometry_Resize);
            Shown += new EventHandler(frmGeometry_Shown);
            FormClosing += new FormClosingEventHandler(frmGeometry_FormClosing);
            ResumeLayout(false);
            PerformLayout();

        }

        internal ToolTip _tt;
        internal PictureBox pxy;
        internal StatusStrip StatusStrip1;
        internal ToolStripStatusLabel lblCursor;
        internal ToolStrip ToolStrip1;
        internal ContextMenuStrip ctxAddMenu;
        internal Button btnTabIncrease;
        internal Button btnTabDecrease;
        internal Button btnAdd;
        internal ToolStripMenuItem AVLTemplateToolStripMenuItem;
        internal ToolStripMenuItem AVLTemplateFullToolStripMenuItem;
        internal ToolStripMenuItem AVLTemplateMinimalToolStripMenuItem;
        internal ToolStripMenuItem SurfaceToolStripMenuItem;
        internal ToolStripMenuItem SurfaceFullToolStripMenuItem;
        internal ToolStripMenuItem SurfaceMinimalToolStripMenuItem;
        internal ToolStripMenuItem SectionToolStripMenuItem;
        internal ToolStripMenuItem SectionFullToolStripMenuItem;
        internal ToolStripMenuItem SectionMinimalToolStripMenuItem;
        internal ToolStripMenuItem ControlToolStripMenuItem;
        internal ToolStripMenuItem ControlFullToolStripMenuItem;
        internal ToolStripMenuItem ControlMinimalToolStripMenuItem;
        internal ToolStripSeparator ToolStripSeparator1;
        internal ToolStripMenuItem SeparatorToolStripMenuItem;
        internal Button btnClear;
        // Friend WithEvents txt3 As FastColoredTextBoxNS.FastColoredTextBox
        internal ToolStripMenuItem MassTemplateToolStripMenuItem;
        internal ToolStripMenuItem MassTemplateFullToolStripMenuItem;
        internal ToolStripMenuItem MassTemplateMinimalToolStripMenuItem;
        internal ToolStripSeparator ToolStripSeparator2;
        internal ToolStripSeparator ToolStripSeparator3;
        internal ToolStripMenuItem RunTemplateToolStripMenuItem;
        internal ToolStripMenuItem RunTemplateFullToolStripMenuItem;
        internal ToolStripMenuItem RunTemplateMinimalToolStripMenuItem;
        internal ToolStripSeparator ToolStripSeparator5;
        internal PictureBox pxz;
        internal ToolStripStatusLabel btnEditor;
        internal System.IO.FileSystemWatcher FileSystemWatcher1;
        internal PictureBox pyz;

        internal ToolStripButton btnZoomin;
        internal ToolStripButton btnZoomout;
        internal ToolStripButton btnBasefontplus;
        internal ToolStripButton btnBasefontminus;
        internal ToolStripButton btnDisplay;
        internal ToolStripLabel ToolStripLabel1;
        internal ToolStripSeparator ToolStripSeparator4;
        internal ToolStripDropDownButton btnHelp;
        internal ToolStripMenuItem btnHelpFull;
        internal ToolStripSeparator ToolStripSeparator8;
        internal ToolStripMenuItem btnHelpAVL;
        internal ToolStripMenuItem btnHelpMass;
        internal ToolStripMenuItem btnHelpRun;
        internal ToolStripSeparator ToolStripSeparator9;
        internal ToolStripMenuItem btnHelpCommands;
        internal ToolStripComboBox txtName;
        internal ToolStripSeparator ToolStripSeparator11;
        internal ToolStripSeparator ToolStripSeparator10;
        internal ToolStripSeparator ToolStripSeparator12;
        internal ToolStripButton btnSpace;
        internal ToolStripSeparator ToolStripSeparator13;
        internal ToolStripButton btnFitAll;
        internal ToolStripButton btn3D;
        internal ToolStripDropDownButton btnLayers;
        internal ToolStripMenuItem mnuLayerSection;
        internal ToolStripMenuItem mnuLayerMass;
        internal ToolStripMenuItem mnuLayerControl;
        internal ToolStripMenuItem mnuLayerMesh;
        internal ToolStripSeparator ToolStripSeparator14;
        internal ToolStripMenuItem mnuLayerChordline;
        internal ToolStripMenuItem mnuLayerControlPoints;
        internal ToolStripMenuItem mnuLayerAxes;
        internal ToolStripSeparator ToolStripSeparator15;
        internal ToolStripMenuItem mnuLayerCamberline;
        internal ToolStripMenuItem mnuLayerNormalVector;
        internal ToolStripMenuItem mnuLayerBoundLeg;
        internal ToolStripMenuItem mnuLayerTrailingLegs;
        internal ToolStripMenuItem mnuLayerLoading;
        internal ToolStripMenuItem mnuLayerOffBody;
        internal SplitContainer sc1;
        internal SplitContainer scup;
        internal TabControl tc1;
        internal TabPage Geometry;
        internal TabPage Mass;
        internal SplitContainer scdown;
        internal TabPage Run;
        internal ToolStripButton btnHover;
        internal ImageList ImageList1;
        internal PictureBox p3d;
        internal ToolStripButton btnDragMode;
        internal Button btnUndo;
        internal Button btnRedo;
        internal ToolStripSeparator ToolStripSeparator6;
        internal ToolStrip ToolStrip2;
        internal Button btnPrettify;
        internal Button btnValidate;
    }
}