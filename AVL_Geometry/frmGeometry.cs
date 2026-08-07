using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using FastColoredTextBoxNS;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace AERO_Console
{

    public partial class frmGeometry
    {
        private string pxySvg = "";
        private string pxyPdf = "";
        private string pxzSvg = "";
        private string pxzPdf = "";
        private string pyzSvg = "";
        private string pyzPdf = "";
        private string p3dSvg = "";
        private string p3dPdf = "";
        private string trefftzSvg = "";
        private string trefftzPdf = "";
        private TabPage Trefftz;
        private PictureBox pTrefftz;
        private System.Windows.Forms.Button btnRunTrefftz = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button btnAvlCommandsTrefftz = new System.Windows.Forms.Button();
        private List<TrefftzSurface> _lastTrefftzSurfaces = null;
        private double _lastTrefftzCref = 1.0d;
        private string _lastTrefftzLog = "";
        private string _lastTrefftzRawFile = "";
        private TrefftzTotals _lastTrefftzTotals = null;

        private string loadsSvg = "";
        private string loadsPdf = "";
        private TabPage Loads;
        private PictureBox pLoads;
        private System.Windows.Forms.Button btnLoads = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button btnAvlCommandsLoads = new System.Windows.Forms.Button();
        private List<VmSurface> _lastVmSurfaces = null;
        private string _lastVmLog = "";

        private string polarSvg = "";
        private string polarPdf = "";
        private TabPage Polar;
        private PictureBox pPolar;
        private System.Windows.Forms.TextBox txtPolarMin = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPolarMax = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPolarStep = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.Button btnRunPolar = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button btnAvlCommandsPolar = new System.Windows.Forms.Button();
        private List<PolarPoint> _lastPolarPoints = null;
        private string _lastPolarLog = "";

        private TabPage Derivatives;
        private System.Windows.Forms.TextBox txtDerivatives = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.Button btnRunDerivatives = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button btnExportDerivatives = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button btnAvlCommandsDerivatives = new System.Windows.Forms.Button();
        private System.Windows.Forms.RichTextBox rtbDerivInsights = new System.Windows.Forms.RichTextBox();
        private string _lastDerivativesText = "";
        private string _lastStRawText = "";

        private string feSvg = "";
        private string fePdf = "";
        private TabPage FE;
        private PictureBox pFE;
        private System.Windows.Forms.Button btnRunFE = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button btnAvlCommandsFE = new System.Windows.Forms.Button();
        private System.Windows.Forms.ComboBox cmbFeStrip = new System.Windows.Forms.ComboBox();
        private List<FeStrip> _lastFeStrips = null;
        private string _lastFeLog = "";

        private string modesSvg = "";
        private string modesPdf = "";
        private TabPage ModesTab;
        private PictureBox pModes;
        private System.Windows.Forms.Button btnRunModes = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button btnModeTips = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button btnModesZoomIn = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button btnModesZoomOut = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button btnModesZoomReset = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button btnAvlCommandsModes = new System.Windows.Forms.Button();
        private List<EigenValue> _lastEigenvalues = null;
        private string _lastModesLog = "";
        private System.Windows.Forms.ToolTip modesTip = new System.Windows.Forms.ToolTip();
        private int _lastModesHoverIndex = -1;

        // Root-locus zoom state. _modesZoom=1.0 shows the full auto-fit range (the
        // original behavior); >1 zooms in, <1 zooms out. _modesPanX/Y are a data-space
        // offset (in Real/Imag units) added to the auto-fit center, so panning survives
        // across re-renders even though the auto-fit bounds themselves are recomputed
        // from the data every time. _modesXMin/XMax/YMin/YMax and _modesPlotX/Y/W/H cache
        // the bounds and pixel rect from the most recent render, so the mouse-wheel
        // handler can map cursor pixels back to data coordinates for zoom-to-cursor.
        private double _modesZoom = 1.0d;
        private double _modesPanX = 0.0d;
        private double _modesPanY = 0.0d;
        private double _modesXMin;
        private double _modesXMax;
        private double _modesYMin;
        private double _modesYMax;
        private float _modesPlotX;
        private float _modesPlotY;
        private float _modesPlotW;
        private float _modesPlotH;
        private bool _modesIsPanning = false;
        private int _modesDragLastX;
        private int _modesDragLastY;

        private ToolStripDropDownButton btnFileMenu = new ToolStripDropDownButton();
        private ToolStripMenuItem btnLoadTestProject = new ToolStripMenuItem();
        private ToolStripMenuItem btnMakeCopy = new ToolStripMenuItem();

        // Properties panel — click a Section/Control node in any view to edit its
        // fields directly, without hand-editing the raw text (which remains the
        // source of truth; this panel just reads/writes the same file+line).
        private System.Windows.Forms.Panel pnlProperties;
        private System.Windows.Forms.Panel pnlSectionFields;
        private System.Windows.Forms.Panel pnlControlFields;
        private System.Windows.Forms.Panel pnlMassFields;
        private System.Windows.Forms.Label lblPropHeader;
        private System.Windows.Forms.Label lblPropAirfoilKind;
        private System.Windows.Forms.TextBox txtPropXle = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropYle = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropZle = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropChord = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropAinc = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropAirfoil = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropCname = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropCgain = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropXhinge = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropHx = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropHy = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropHz = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropSgnDup = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropMassVal = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropMassX = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropMassY = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropMassZ = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropIxx = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropIyy = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.TextBox txtPropIzz = new System.Windows.Forms.TextBox();
        private System.Windows.Forms.Button btnPropApply = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button btnPropClose = new System.Windows.Forms.Button();
        private System.Windows.Forms.ToolTip propHelpTip = new System.Windows.Forms.ToolTip();
        private Node _selectedNode = null;
        private Point mouseDownScreenPos = Point.Empty;

        // Structure tree — Surface/Section/Control block navigator with drag-and-drop
        // reordering/reassignment. Operates on whole text blocks (delimited by this
        // app's own !beginX/!endX markers) rather than single lines.
        private System.Windows.Forms.Panel pnlStructureTree;
        private System.Windows.Forms.TreeView tvStructure;
        private ToolStripButton btnToggleStructureTree = new ToolStripButton();

        // Validation panel — runs FileValidator (FileValidator.vb) against the active
        // Geometry/Mass/Run tab's text, lists findings with fix suggestions, and paints
        // each offending line's background in the editor so problems are easy to spot.
        private System.Windows.Forms.Panel pnlValidation;
        private System.Windows.Forms.ListView lvValidation;
        private System.Windows.Forms.TextBox txtFixHint;
        private System.Windows.Forms.Label lblValidationSummary;

        // ===================== Light/dark theme =====================
        // Applies to every custom-drawn canvas (the pxy/pxz/pyz/p3d geometry
        // views via HeavyRender, and the six analysis-tab plots via SvgGraphics),
        // plus the Derivatives tab's text panes. Persisted in My.Settings.DarkTheme
        // so it survives across sessions. Curve/accent colors (OrangeRed,
        // LimeGreen, DeepSkyBlue, Gold, node-marker Red/Green/Blue, etc.) are
        // deliberately left unchanged between themes - they're vivid enough to
        // read against both a white and a black background. Only background,
        // grid, axis/border, and primary text/label colors actually flip.
        private bool IsDarkTheme = false;
        private ToolStripButton btnToggleTheme = new ToolStripButton();

        private Color ThemeCanvasBackColor
        {
            get
            {
                return IsDarkTheme ? Color.Black : Color.White;
            }
        }

        private Color ThemeAxisColor
        {
            get
            {
                return IsDarkTheme ? Color.White : Color.Black;
            }
        }

        private Color ThemeGridColor
        {
            get
            {
                return IsDarkTheme ? Color.FromArgb(90, 90, 90) : Color.FromArgb(210, 210, 210);
            }
        }

        // For elements that were hardcoded LightGray assuming a black background
        // (reference lines, de-emphasized legend/label text) - LightGray reads
        // fine on black but washes out on white, so it needs a darker equivalent
        // in light theme.
        private Color ThemeMutedColor
        {
            get
            {
                return IsDarkTheme ? Color.LightGray : Color.DimGray;
            }
        }

        // The mesh-overlay pen was DarkSlateGray at partial opacity, tuned for a
        // white background - nearly invisible blended over black.
        private Color ThemeMeshColor
        {
            get
            {
                return IsDarkTheme ? Color.LightGray : Color.DarkSlateGray;
            }
        }

        // Derivatives-tab stability verdict colors. Dark(Green/Red/Orange) read
        // fine on the light rtbDerivInsights background but go near-black-on-black
        // if that pane switches to dark, so dark theme needs brighter equivalents.
        private Color ThemeStableColor
        {
            get
            {
                return IsDarkTheme ? Color.LightGreen : Color.DarkGreen;
            }
        }

        private Color ThemeUnstableColor
        {
            get
            {
                return IsDarkTheme ? Color.Salmon : Color.DarkRed;
            }
        }

        private Color ThemeCautionColor
        {
            get
            {
                return IsDarkTheme ? Color.Orange : Color.DarkOrange;
            }
        }

        // Softer panel background for rtbDerivInsights - matches frmMain's txtLog
        // dark shade rather than pure black, since it's a text pane, not a canvas.
        private Color ThemePanelBackColor
        {
            get
            {
                return IsDarkTheme ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 245, 245);
            }
        }

        public frmGeometry()
        {
            help = rootPath + @"\avl_doc.txt";
            _renderTimer = new System.Windows.Forms.Timer() { Interval = 30 };
            _renderTimer.Tick += _renderTimer_Tick; // VB "Handles _renderTimer.Tick" — not auto-wired by the converter
            InitializeComponent();
        }

        // Re-applies BackColor/ForeColor to the Derivatives tab's text panes and
        // re-renders the insight-line colors, since RefreshThemeColors() only
        // handles the drawn canvases.
        private void RefreshDerivativesTheme()
        {
            if (txtDerivatives is null)
                return;
            txtDerivatives.BackColor = ThemeCanvasBackColor;
            txtDerivatives.ForeColor = ThemeAxisColor;
            rtbDerivInsights.BackColor = ThemePanelBackColor;
            UpdateDerivativesInsights(ParseDerivativesInsight(_lastStRawText));
        }

        // Re-colors the shared/reusable pens+brush in place (pAxis/pGrid/bAxisText
        // are mutable module fields referenced from ~30 call sites in HeavyRender,
        // so updating them here propagates everywhere without touching each site)
        // and flips the geometry-view PictureBoxes' BackColor. The six analysis
        // tabs construct their pens fresh each render (see each Render*Plot/
        // Draw*Subplot function) reading Theme*Color directly, so they don't need
        // anything refreshed here - only re-rendered.
        private void RefreshThemeColors()
        {
            pAxis.Color = ThemeAxisColor;
            pGrid.Color = ThemeGridColor;
            bAxisText.Color = ThemeAxisColor;

            foreach (var pb in new PictureBox[] { pxy, pxz, pyz, p3d, pTrefftz, pLoads, pPolar, pFE, pModes })
            {
                if (pb is not null)
                    pb.BackColor = ThemeCanvasBackColor;
            }
        }

        private double xmax = 10d;
        private double xmin = -10;
        private double ymax = 10d;
        private double ymin = -10;
        private double zmax = 10d;
        private double zmin = -10;
        private int gridnumber = 2;
        private int gridnumbermini = 4;
        private int gridstep;
        private double xoffset = 0d;
        private double yoffset = 0d;
        private double zoffset = 0d;
        private double xdown;
        private double ydown;
        private double zdown;
        private List<Node> points = new List<Node>();
        private double curX;
        private double curY;
        private double curZ;
        private AutocompleteMenu popupMenu;
        private TextStyle blueStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
        private TextStyle greenStyle = new TextStyle(Brushes.Green, null, FontStyle.Bold);
        private TextStyle lightgreenStyle = new TextStyle(Brushes.Green, null, FontStyle.Regular);
        private TextStyle redStyle = new TextStyle(Brushes.Red, null, FontStyle.Italic);
        private TextStyle purpleStyle = new TextStyle(Brushes.Purple, null, FontStyle.Underline);
        private EllipseStyle ellipseStyle1 = new EllipseStyle(Color.Red);
        private EllipseStyle ellipseStyle2 = new EllipseStyle(Color.Blue);
        private EllipseStyle ellipseStyle3 = new EllipseStyle(Color.Cyan);
        private EllipseStyle ellipseStyle4 = new EllipseStyle(Color.Magenta);
        private Pen pAxis = new Pen(Color.Black);
        private Pen pGrid = new Pen(Color.LightGray) { DashStyle = DashStyle.Dash };
        private Pen pDot = new Pen(Color.Red);
        private Brush bPolySurface = Brushes.Aqua;
        private Brush bPolyAilern = Brushes.Yellow;
        private Brush bPolyFlap = Brushes.Gold;
        private Brush bPolyElevator = Brushes.LightGoldenrodYellow;
        private Brush bPolyRudder = Brushes.LightYellow;
        // pAxis's text-label counterpart (HeavyRender draws axis-label/origin text
        // via scattered Brushes.Black calls that can't be made theme-aware by
        // mutating a shared System.Drawing.Brushes.X, since those are read-only) -
        // mutable so RefreshThemeColors() can flip it alongside pAxis.
        private SolidBrush bAxisText = new SolidBrush(Color.Black);
        private int baseFontsize = 3;
        private Font _cachedAxisFont = null;
        private Font _cachedTickFont = null;
        private float _cachedTickFontSize = float.NaN;
        private readonly object _fontCacheLock = new object();
        private bool isHovered = false;
        private ToolStripButton btnAutosave = null;
        private ToolStripButton btnSave = null;
        private ToolStripLabel lblDirtyWarning = null;
        private bool isDirty = false;
        private string lastActiveTabName = "Geometry";
        private string currentToolTipText = "";
        private string currentHoveredWord = "";
        // Distinct from the navy title and black body text, and dark enough to stay readable on the
        // tooltip's white background - used to highlight the hovered term within the body text.
        private readonly SolidBrush ToolTipHighlightBrush = new SolidBrush(Color.FromArgb(196, 62, 0));
        private string rootPath = Application.StartupPath + @"\appdata";
        // Starts blank (not a hardcoded "test") so a fresh window genuinely has no project
        // associated until the user explicitly creates or loads one - see UpdateProjectGate().
        public string projectName = "";
        // The project whose files are actually reflected in txt3 right now - distinct from
        // projectName, which txtName_TextChanged updates on every keystroke as the user types
        // a name into the project combo box (i.e. projectName can already equal a not-yet-loaded
        // name before the switch is committed). Save*() targets this, not projectName, so that
        // saving never writes the outgoing project's in-progress edits into the new project's file.
        // UpdateProjectGate() also gates on this rather than projectName, since it should only
        // unlock once a project has actually finished loading, not while a name is mid-typed.
        private string loadedProjectName = "";
        private System.Windows.Forms.Panel pnlProjectGate;
        private bool updating = false;
        public string help;
        private bool autoSpace = true;
        private int autoSpaceWidth = 12;
        private const int MinAutoSpaceWidth = 2;
        private const int MaxAutoSpaceWidth = 24;
        private bool showMass = true;
        private bool showControl = true;
        private bool showSection = true;
        private bool showMesh = false;
        private bool show3D = false;
        private bool showHover = true;
        // AVL-style plot layer toggles, surfaced via the "Display" dropdown (btnLayers).
        // showChordline/showControlPoints/showAxes have real draw logic (below). The rest
        // (Camberline, Bound leg, Trailing legs, Normal vector, Loading, Off-body) are
        // placeholders in the menu until AVL's solved output is piped into the geometry
        // views - their menu items stay disabled so they read as "not yet available"
        // rather than silently doing nothing when clicked.
        private bool showChordline = false;
        private bool showControlPoints = false;
        private bool showAxesTriad = false;
        private bool showCamberline = false;
        private bool showBoundLeg = false;
        private bool showTrailingLegs = false;
        private bool showNormalVector = false;
        private bool showLoadingOverlay = false;
        private bool showOffBody = false;
        private List<Surface> parsedSurfaces = new List<Surface>();
        // Add these to your variable declarations at the top of frmGeometry
        private float viewAlpha = 0f; // Yaw (Rotation around Y)
        private float viewBeta = 0f;   // Pitch (Rotation around X)
        private float viewGamma = 0f;   // Roll (Rotation around Z)
        private float viewDist = 200f;  // Distance from camera (Zoom)
        private float viewFOV = 500f;   // Field of View scale
                                        // 3D rotation pivot (the model's bounding-box center, recomputed each
                                        // HeavyRender pass) - module-level so DrawMeshForSurface/WorldToScreen's
                                        // "3D" case can rotate around the SAME point HeavyRender uses for
                                        // everything else, instead of the world origin. Without this the mesh
                                        // overlay would visually drift away from the (now re-centered) surfaces
                                        // and grid as soon as the aircraft's geometry isn't centered near (0,0,0).
        private float view3DCenterX = 0f;
        private float view3DCenterY = 0f;
        private float view3DCenterZ = 0f;
        private Point lastMouseLoc;
        private ModernFastColoredTextBox txt3;

        // --- Drag-nodes mode state ---
        private bool isDragMode = false;
        private bool isDragging = false;
        private Node draggingNode = null;
        private double dragStartX = 0d;
        private double dragStartY = 0d;
        private double dragStartZ = 0d;

        // 1. CANCELLATION: Replaces WorkerSupportsCancellation
        private CancellationTokenSource _cts;

        // Render throttle: instead of firing a full async render on every event,
        // callers just set _renderPending = True and the timer fires at most ~30fps.
        private bool _renderPending = false;
        private System.Windows.Forms.Timer _renderTimer;

        // Set to True before calling drawAxes() when SVG/PDF export data is needed.
        // Building the SVG/PDF StringBuilder on every frame is expensive; skip it during normal interaction.
        private bool _captureVectors = false;

        private int frames = 0;
        private DateTime lastFrameTime = DateTime.Now;

        // Assuming you have a PictureBox named p3d, if not, add one to your designer

        public struct Section
        {
            public double Xle;
            public double Yle;
            public double Zle;
            public double Chord;
            public double Ainc;
            public double Nspanwise;
            public double Sspace;
            public int lineNumber;
            public List<ControlSurface> controls;
        }
        public struct Surface
        {
            public string Name;
            public bool yDuplicate;
            public double yDuplicatevalue;
            public double Nchordwise;
            public double Cspace;
            public double Nspanwise;
            public double Sspace;
            public List<Section> sections;
            // SCALE - applied to Xle/Yle/Zle/Chord before TRANSLATE. Must default to 1.0 (not the
            // Double zero-value default) since these are multiplicative - a stray 0.0 would collapse
            // the whole surface to a point. Callers must explicitly set these to 1.0 right after
            // creating a Surface, since VB Structures always zero-init on New.
            public double Xscale;
            public double Yscale;
            public double Zscale;
            // TRANSLATE - added to Xle/Yle/Zle after SCALE. 0.0 default (VB's own zero-init) is correct.
            public double dX;
            public double dY;
            public double dZ;
            // ANGLE - added to every section's Ainc. Ainc itself is documented as not visually
            // rotating the drawn geometry (flow-tangency only), so dAinc is folded into Section.Ainc
            // for data correctness but likewise has no visual effect, consistent with existing behavior.
            public double dAinc;
            // COMPONENT/INDEX - Lcomp groups surfaces into a composite virtual surface. 0 means unset
            // (each surface implicitly its own component, matching AVL's default when omitted).
            public int Lcomp;
            public bool NoWake;
            public bool NoAlbe;
            public bool NoLoad;
        }
        public struct ControlSurface
        {
            public int lineNumber;
            public string Type;
            public double Cgain;
            public double Xhinge;
        }
        public struct Blocks
        {
            public int GeometryBeginBefore;
            public int GeometryBeginAfter;
            public int GeometryEndBefore;
            public int GeometryEndAfter;
            public int SurfaceBeginBefore;
            public int SurfaceBeginAfter;
            public int SurfaceEndBefore;
            public int SurfaceEndAfter;
            public int SectionBeginBefore;
            public int SectionBeginAfter;
            public int SectionEndBefore;
            public int SectionEndAfter;
            public int ControlBeginBefore;
            public int ControlBeginAfter;
            public int ControlEndBefore;
            public int ControlEndAfter;
        }

        private void frmGeometry_Load(object sender, EventArgs e)
        {
            autoSpace = My.MySettingsProperty.Settings.autoSpaceEnabled;
            autoSpaceWidth = Math.Max(MinAutoSpaceWidth, Math.Min(MaxAutoSpaceWidth, My.MySettingsProperty.Settings.autoSpaceWidth));
            btnSpace.Text = autoSpace ? "Auto Space: On" : "Auto Space: Off";
            InitLayerMenuSwatches();

            txt3 = new ModernFastColoredTextBox();
            txt3.Dock = DockStyle.Fill;

            txt3.Font = new Font("Consolas", 12f);
            txt3.Cursor = Cursors.IBeam;

            // Highlights the line the cursor is currently on (Visual improvement)
            txt3.CurrentLineColor = Color.FromArgb(50, 220, 220, 220);

            // Selection color (kept yours, just cleaned up syntax)
            txt3.SelectionColor = Color.FromArgb(60, 0, 0, 255);

            // Better Line Number spacing
            txt3.ReservedCountOfLineNumberChars = 4; // Allows up to 9999 lines without resizing
            txt3.LeftPadding = 5; // Adds breathing room between border and text

            // --- PERFORMANCE SETTINGS ---
            // Only recalculate highlighting for what is visible on screen
            txt3.HighlightingRangeType = HighlightingRangeType.VisibleRange;

            // Delays syntax check slightly to ensure typing stays buttery smooth
            txt3.DelayedEventsInterval = 200;
            txt3.DelayedTextChangedInterval = 200;

            // If your lines are very long, turning this OFF improves performance significantly
            txt3.WordWrap = false;

            // --- BEHAVIOR ---
            txt3.ShowFoldingLines = true;
            txt3.IsReplaceMode = false;
            txt3.AutoScrollMinSize = new Size(0, 0); // Let it auto-calculate
            txt3.BackBrush = null;
            txt3.Paddings = new Padding(0);
            txt3.Zoom = 100;
            txt3.TabIndex = 2;

            // --- AUTOMATION & SYNTAX ---
            txt3.AutoCompleteBrackets = true;

            // Cleaned up Char list using VB Literals
            txt3.AutoCompleteBracketsList = new char[] { '(', ')', '{', '}', '[', ']', '"', '"', '\'', '\'' };

            txt3.AutoIndentCharsPatterns = "";
            txt3.AutoIndent = true;

            // Optional: Set language if known (e.g., CSharp, VB, JSON) for built-in speed
            // txt3.Language = FastColoredTextBoxNS.Language.CSharp

            Geometry.Controls.Add(txt3);

            // Add floating editor action buttons directly on the parent tab page (Geometry)
            // so that they stay stationary and do not scroll with txt3 text content
            Geometry.Controls.Add(btnAdd);
            Geometry.Controls.Add(btnPrettify);
            Geometry.Controls.Add(btnValidate);
            Geometry.Controls.Add(btnUndo);
            Geometry.Controls.Add(btnRedo);
            Geometry.Controls.Add(btnClear);
            Geometry.Controls.Add(btnTabIncrease);
            Geometry.Controls.Add(btnTabDecrease);
            btnAdd.BringToFront();
            btnPrettify.BringToFront();
            btnValidate.BringToFront();
            btnUndo.BringToFront();
            btnRedo.BringToFront();
            btnClear.BringToFront();
            btnTabIncrease.BringToFront();
            btnTabDecrease.BringToFront();

            // Bind tooltips to floating editor buttons
            var floatTooltip = new System.Windows.Forms.ToolTip();
            floatTooltip.SetToolTip(btnAdd, "Add Template or Element");
            floatTooltip.SetToolTip(btnPrettify, "Prettify: auto-indent and column-align the editor content");
            floatTooltip.SetToolTip(btnValidate, "Validate: check this file for AVL format errors and get fix suggestions");
            floatTooltip.SetToolTip(btnUndo, "Undo (Ctrl+Z)");
            floatTooltip.SetToolTip(btnRedo, "Redo (Ctrl+Y)");
            floatTooltip.SetToolTip(btnClear, "Clear Editor Content");
            floatTooltip.SetToolTip(btnTabIncrease, "Increase auto-spacing between columns");
            floatTooltip.SetToolTip(btnTabDecrease, "Decrease auto-spacing between columns");

            txt3.TextChangedDelayed += txt3_TextChangedDelayed;
            txt3.ToolTipNeeded += txt3_ToolTipNeeded;
            txt3.TextChanged += (s, ev) =>
                {
                    UpdateUndoRedoState();
                    ApplySyntaxHighlighting();
                };
            txt3.Leave += (s, ev) => FormatActiveText();
            txt3.AutoIndentNeeded += txt3_AutoIndentNeeded;

            // Customize ToolTip appearance
            txt3.ToolTip.OwnerDraw = true;
            txt3.ToolTip.Popup += ToolTip_Popup;
            txt3.ToolTip.Draw += ToolTip_Draw;

            // Inject dirty warning label
            lblDirtyWarning = new ToolStripLabel(" * Unsaved Changes * ");
            lblDirtyWarning.ForeColor = Color.Red;
            lblDirtyWarning.Font = new Font(lblDirtyWarning.Font, FontStyle.Bold);
            lblDirtyWarning.Visible = false;
            lblDirtyWarning.Name = "lblDirtyWarning";

            // Inject Save button
            btnSave = new ToolStripButton("Save");
            btnSave.Name = "btnSave";
            btnSave.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnSave.BackColor = Color.FromArgb(220, 220, 220);
            btnSave.ToolTipText = "Save the active file to disk (Ctrl+S)";
            btnSave.Visible = !My.MySettingsProperty.Settings.autoSave;
            btnSave.Click += btnSave_Click;

            // Inject Autosave toggle button
            btnAutosave = new ToolStripButton();
            btnAutosave.Name = "btnAutosave";
            btnAutosave.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnAutosave.Text = My.MySettingsProperty.Settings.autoSave ? "Autosave: On" : "Autosave: Off";
            btnAutosave.BackColor = My.MySettingsProperty.Settings.autoSave ? Color.FromArgb(192, 255, 192) : Color.FromArgb(255, 192, 192);
            btnAutosave.ToolTipText = "Toggle auto-save of AVL, mass, and run files";
            btnAutosave.Click += btnAutosave_Click;

            int hoverIndex = ToolStrip1.Items.IndexOf(btnHover);
            if (hoverIndex >= 0)
            {
                ToolStrip1.Items.Insert(hoverIndex + 1, new ToolStripSeparator());
                ToolStrip1.Items.Insert(hoverIndex + 2, lblDirtyWarning);
                ToolStrip1.Items.Insert(hoverIndex + 3, btnSave);
                ToolStrip1.Items.Insert(hoverIndex + 4, btnAutosave);
            }
            else
            {
                ToolStrip1.Items.Add(new ToolStripSeparator());
                ToolStrip1.Items.Add(lblDirtyWarning);
                ToolStrip1.Items.Add(btnSave);
                ToolStrip1.Items.Add(btnAutosave);
            }

            // Bind Ctrl+S shortcut to save active file
            txt3.KeyDown += (s, ev) => { if (ev.Control && ev.KeyCode == Keys.S) { ev.SuppressKeyPress = true; if (btnSave.Visible) { btnSave_Click(null, null); } } };


            // popupMenu = New FastColoredTextBoxNS.AutocompleteMenu(txt3)
            // popupMenu.MinFragmentLength = 2
            var keyWords = new List<string>();
            keyWords.AddRange("section|control|Mach|IYsym|IZsym|Zsym|Sref|Cref|Bref|Xref|Yref|Zref|Nchordwise|Cspace|Nspanwise|Sspace|Xle|Yle|Zle|Chord|Ainc|Nspanwise|Sspace|Cname|Cgain|Xhinge|HingeVec|SgnDup|YDUPLICATE|ANGLE".Split('|'));
            // popupMenu.Items.SetAutocompleteItems(keyWords)
            // tlp1.Dock = DockStyle.Fill
            sc1.Dock = DockStyle.Fill;
            tc1.Dock = DockStyle.Fill;

            pxy.Dock = DockStyle.Fill;
            pxz.Dock = DockStyle.Fill;
            pyz.Dock = DockStyle.Fill;
            txt3.Dock = DockStyle.Fill;
            frmMain.SetAllControlsFont(Controls, frmMain.systemFont);

            IsDarkTheme = My.MySettingsProperty.Settings.DarkTheme;
            RefreshThemeColors();

            drawAxes();


            // Initialize warning label dynamically
            var lblWarning = new ToolStripLabel();
            lblWarning.Name = "lblWarning";
            lblWarning.ForeColor = Color.OrangeRed;
            lblWarning.Font = new Font(ToolStrip1.Font, FontStyle.Bold);
            lblWarning.Text = "⚠️ Warning: Project name is empty. Saving, autosaving, and analysis are disabled!";
            ToolStrip1.Items.Add(lblWarning);

            txtName.KeyDown += txtName_KeyDown;
            txtName.Leave += txtName_Leave;

            txtName.ComboBox.GotFocus += (s, ev) => ClearPlaceholder();
            txtName.ComboBox.LostFocus += (s, ev) => SetPlaceholder();

            My.MyProject.Forms.frmMain.findAVLs(Environment.CurrentDirectory);
            if (!string.IsNullOrEmpty(My.MyProject.Forms.frmMain.txtName.Text))
            {
                txtName.Text = My.MyProject.Forms.frmMain.txtName.Text;
                if (txtName.Text == "Enter AVL Project (e.g. glider)" || txtName.Text == "Enter NACA (e.g. 2412) or dat file")
                {
                    txtName.ComboBox.ForeColor = Color.Gray;
                }
                else
                {
                    txtName.ComboBox.ForeColor = Color.Black;
                }
            }
            else
            {
                SetPlaceholder();
            }

            InitializeProjectGate();

            UpdateGeometryTitle();
            UpdateProjectWarning();
            InitializeExportButtons();
            InitializeTrefftzTab();
            InitializeLoadsTab();
            InitializePolarTab();
            InitializeDerivativesTab();
            InitializeFETab();
            InitializeModesTab();
            InitializeFileMenu();
            InitializePropertiesPanel();
            InitializeStructureTreePanel();
            InitializeValidationPanel();
            InitializeThemeToggle();

            // Final, authoritative gate check now that every panel referenced by
            // UpdateProjectGate() actually exists.
            UpdateProjectGate();

            // Initialize drag nodes button state
            btnDragMode.Text = "Drag Nodes: Off";
            btnDragMode.BackColor = Color.FromArgb(220, 220, 220);

            // Enable double-buffering recursively on all controls to prevent hover/draw flicker
            EnableDoubleBuffering(this);

            // Set ComboBox FlatStyle and double-buffering directly
            try
            {
                var dbProp = typeof(System.Windows.Forms.Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dbProp is not null)
                {
                    if (txtName is not null && txtName.ComboBox is not null)
                    {
                        dbProp.SetValue(txtName.ComboBox, true, (object[])null);
                        txtName.ComboBox.FlatStyle = FlatStyle.Flat;
                    }
                }
            }
            catch
            {
            }

            _renderTimer.Start();
        }

        // Public Sub findAVLs(path As String)
        // Dim files() As String
        // files = Directory.GetFiles(path, "*.avl", SearchOption.TopDirectoryOnly)
        // txtName.Items.Clear()
        // For Each FileName As String In files
        // 'Console.WriteLine(FileName)
        // txtName.Items.Add(System.IO.Path.GetFileNameWithoutExtension(FileName))
        // Next
        // If (txtName.Items.Count > 0) Then
        // txtName.SelectedIndex = 0
        // End If

        // End Sub

        public void loadTemplate()
        {
            txt3.Text = AvlTemplates.AvlTemplateFull;
        }

        // Marks the view as dirty. The render timer will pick it up within 30ms.
        // Safe to call from any thread or event at any frequency.
        public void drawAxes()
        {
            _renderPending = true;
            if (!_renderTimer.Enabled)
                _renderTimer.Start();
        }

        // Fires at most every 30ms; only renders when something changed.
        private async void _renderTimer_Tick(object sender, EventArgs e)
        {
            if (!_renderPending)
                return;
            _renderPending = false;

            if (_cts is not null)
                return;   // render already in flight; will re-check next tick
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                await Task.Run(() => HeavyRender(null, token));
            }
            catch (OperationCanceledException ex)
            {
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (_cts is not null)
                    _cts.Dispose();
                _cts = null;
                // If another redraw was requested while we were rendering, keep timer alive
                if (!_renderPending)
                    _renderTimer.Stop();
            }
        }

        public int findname(List<Node> l, string name)
        {
            int result = -1;
            for (int i = 0, loopTo = l.Count - 1; i <= loopTo; i++)
            {
                if ((l[i].Surface ?? "") == (name ?? ""))
                {
                    result = i;
                    break;
                }
            }
            return result;
        }

        // Returns the N+1 normalised break positions (0..1) for N panels using AVL spacing codes.
        // Sspace > 0 → cosine (clustered at both ends)
        // Sspace < 0 → sine-half (clustered at root / first end only)
        // Sspace = 0 → uniform
        private double[] GetPanelBreaks(int N, double Sspace)
        {
            int n1 = N;
            if (n1 < 1)
                n1 = 1;
            var breaks = new double[n1 + 1];
            for (int k = 0, loopTo = n1; k <= loopTo; k++)
            {
                double t = k / (double)n1;
                if (Math.Abs(Sspace) > 0.1d)
                {
                    // cosine / sine clustering
                    breaks[k] = 0.5d * (1.0d - Math.Cos(Math.PI * t));
                }
                else
                {
                    breaks[k] = t;
                }
            }
            return breaks;
        }

        // Draws the AVL vortex-lattice panel mesh for one surface onto G.
        // proj selects which 2 world coordinates map to screen X/Y.
        // "XY" → world X→screenX, world Y→screenY
        // "XZ" → world X→screenX, world Z→screenY
        // "YZ" → world Y→screenX, world Z→screenY
        private void DrawMeshForSurface(SvgGraphics G, Surface su, string proj, int W, int H, double hMin, double hMax, double vMin, double vMax, double hOff, double vOff, Pen meshPen, bool isDuplicate)
        {
            var sects = su.sections;
            if (sects is null || sects.Count < 2)
                return;
            int Nc = Math.Max(1, (int)Math.Round(su.Nchordwise > 0d ? su.Nchordwise : 8d));
            double[] cBreaks = GetPanelBreaks(Nc, su.Cspace);
            int Ns = Math.Max(1, (int)Math.Round(su.Nspanwise > 0d ? su.Nspanwise : 12d));
            double[] sBreaks = GetPanelBreaks(Ns, su.Sspace);
            double yDupVal = su.yDuplicate ? su.yDuplicatevalue : 0.0d;

            // Iterate over each spanwise panel segment (between consecutive sections)
            for (int seg = 0, loopTo = sects.Count - 2; seg <= loopTo; seg++)
            {
                var s0 = sects[seg];
                var s1 = sects[seg + 1];

                // A duplicate mirrors about Y=Ydupl (mirrorY = 2*Ydupl - Yle); the primary surface's
                // own Y is untouched by YDUPLICATE - it only affects the position of the mirror copy.
                double y0Eff = isDuplicate ? 2d * yDupVal - s0.Yle : s0.Yle;
                double y1Eff = isDuplicate ? 2d * yDupVal - s1.Yle : s1.Yle;

                // Determine the number of spanwise panels for this segment
                // (Use per-section Nspanwise if set, else fall back to surface level)
                int segNs = s0.Nspanwise > 0d ? (int)Math.Round(s0.Nspanwise) : Ns;
                double segSp = s0.Nspanwise > 0d ? s0.Sspace : su.Sspace;
                double[] segSBreaks = GetPanelBreaks(segNs, segSp);

                // Spanwise boundary lines (constant t along span, varying chord fraction)
                for (int ki = 0, loopTo1 = segSBreaks.Length - 1; ki <= loopTo1; ki++)
                {
                    double t = segSBreaks[ki];
                    // Interpolate section geometry at this spanwise station
                    double xle = s0.Xle + t * (s1.Xle - s0.Xle);
                    double yle = y0Eff + t * (y1Eff - y0Eff);
                    double zle = s0.Zle + t * (s1.Zle - s0.Zle);
                    double chord = s0.Chord + t * (s1.Chord - s0.Chord);
                    double xte = xle + chord;

                    bool isLeVisible = true;
                    bool isTeVisible = true;
                    if (proj == "3D")
                    {
                        var pLe = new Node(xle - view3DCenterX, yle - view3DCenterY, zle - view3DCenterZ, "", false, 0, Node.NodeType.Geometry);
                        var pTe = new Node(xte - view3DCenterX, yle - view3DCenterY, zle - view3DCenterZ, "", false, 0, Node.NodeType.Geometry);
                        isLeVisible = pLe.RotateZ(viewGamma).RotateY(viewAlpha).RotateX(viewBeta).Project(W, H, viewDist, viewFOV).Visible;
                        isTeVisible = pTe.RotateZ(viewGamma).RotateY(viewAlpha).RotateX(viewBeta).Project(W, H, viewDist, viewFOV).Visible;
                    }

                    if (isLeVisible && isTeVisible)
                    {
                        // Convert world → screen for LE and TE of this spanwise station
                        var leS = WorldToScreen(xle, yle, zle, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff);
                        var teS = WorldToScreen(xte, yle, zle, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff);

                        // Draw the chordwise line (LE to TE) at this spanwise boundary
                        G.DrawLine(meshPen, leS, teS);
                    }
                }

                // Chordwise panel lines: draw Nc-1 lines at fixed chord fractions across the full span segment
                for (int ki = 0, loopTo2 = cBreaks.Length - 1; ki <= loopTo2; ki++)
                {
                    double tc = cBreaks[ki];
                    // Sample a few spanwise stations to draw a smooth spanwise line at this chord fraction
                    var pts = new List<PointF>();
                    bool isSpanVisible = true;
                    for (int si = 0, loopTo3 = segSBreaks.Length - 1; si <= loopTo3; si++)
                    {
                        double ts = segSBreaks[si];
                        double xle = s0.Xle + ts * (s1.Xle - s0.Xle);
                        double yle = y0Eff + ts * (y1Eff - y0Eff);
                        double zle = s0.Zle + ts * (s1.Zle - s0.Zle);
                        double chord = s0.Chord + ts * (s1.Chord - s0.Chord);
                        double wx = xle + tc * chord;

                        if (proj == "3D")
                        {
                            var pPt = new Node(wx - view3DCenterX, yle - view3DCenterY, zle - view3DCenterZ, "", false, 0, Node.NodeType.Geometry);
                            if (!pPt.RotateZ(viewGamma).RotateY(viewAlpha).RotateX(viewBeta).Project(W, H, viewDist, viewFOV).Visible)
                            {
                                isSpanVisible = false;
                            }
                        }
                        pts.Add(WorldToScreen(wx, yle, zle, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff));
                    }
                    if (pts.Count >= 2 && isSpanVisible)
                    {
                        G.DrawLines(meshPen, pts.ToArray());
                    }
                }
            }
        }

        // Draws the straight leading-edge-to-trailing-edge line at every defined
        // Section of a surface (AVL's "CHordline" plot toggle). Unlike the mesh
        // overlay, this only draws at the sections the user actually defined, not
        // at every interpolated panel break.
        private void DrawChordlinesForSurface(SvgGraphics G, Surface su, string proj, int W, int H, double hMin, double hMax, double vMin, double vMax, double hOff, double vOff, Pen chordPen, bool isDuplicate)
        {
            var sects = su.sections;
            if (sects is null || sects.Count < 1)
                return;
            double yDupVal = su.yDuplicate ? su.yDuplicatevalue : 0.0d;

            foreach (Section s in sects)
            {
                double yEff = isDuplicate ? 2d * yDupVal - s.Yle : s.Yle;
                double xte = s.Xle + s.Chord;

                if (proj == "3D")
                {
                    var pLe = new Node(s.Xle - view3DCenterX, yEff - view3DCenterY, s.Zle - view3DCenterZ, "", false, 0, Node.NodeType.Geometry);
                    var pTe = new Node(xte - view3DCenterX, yEff - view3DCenterY, s.Zle - view3DCenterZ, "", false, 0, Node.NodeType.Geometry);
                    if (!pLe.RotateZ(viewGamma).RotateY(viewAlpha).RotateX(viewBeta).Project(W, H, viewDist, viewFOV).Visible)
                        continue;
                    if (!pTe.RotateZ(viewGamma).RotateY(viewAlpha).RotateX(viewBeta).Project(W, H, viewDist, viewFOV).Visible)
                        continue;
                }

                var leS = WorldToScreen(s.Xle, yEff, s.Zle, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff);
                var teS = WorldToScreen(xte, yEff, s.Zle, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff);
                G.DrawLine(chordPen, leS, teS);
            }
        }

        // Marks an approximate vortex-lattice control (collocation) point for every
        // panel - midspan of each spanwise strip, and at 3/4 of each chordwise
        // panel's local chord, per the standard VLM placement rule AVL follows
        // (AVL's "CNtlpoint" toggle). Positions are geometric approximations for
        // visualization, not read from a solved AVL run.
        private void DrawControlPointsForSurface(SvgGraphics G, Surface su, string proj, int W, int H, double hMin, double hMax, double vMin, double vMax, double hOff, double vOff, Brush ptBrush, float ptRadius, bool isDuplicate)
        {
            var sects = su.sections;
            if (sects is null || sects.Count < 2)
                return;
            int Nc = Math.Max(1, (int)Math.Round(su.Nchordwise > 0d ? su.Nchordwise : 8d));
            double[] cBreaks = GetPanelBreaks(Nc, su.Cspace);
            int Ns = Math.Max(1, (int)Math.Round(su.Nspanwise > 0d ? su.Nspanwise : 12d));
            double yDupVal = su.yDuplicate ? su.yDuplicatevalue : 0.0d;

            for (int seg = 0, loopTo = sects.Count - 2; seg <= loopTo; seg++)
            {
                var s0 = sects[seg];
                var s1 = sects[seg + 1];
                double y0Eff = isDuplicate ? 2d * yDupVal - s0.Yle : s0.Yle;
                double y1Eff = isDuplicate ? 2d * yDupVal - s1.Yle : s1.Yle;

                int segNs = s0.Nspanwise > 0d ? (int)Math.Round(s0.Nspanwise) : Ns;
                double segSp = s0.Nspanwise > 0d ? s0.Sspace : su.Sspace;
                double[] segSBreaks = GetPanelBreaks(segNs, segSp);

                for (int si = 0, loopTo1 = segSBreaks.Length - 2; si <= loopTo1; si++)
                {
                    double tMid = (segSBreaks[si] + segSBreaks[si + 1]) / 2.0d;
                    double xle = s0.Xle + tMid * (s1.Xle - s0.Xle);
                    double yle = y0Eff + tMid * (y1Eff - y0Eff);
                    double zle = s0.Zle + tMid * (s1.Zle - s0.Zle);
                    double chord = s0.Chord + tMid * (s1.Chord - s0.Chord);

                    for (int ci = 0, loopTo2 = cBreaks.Length - 2; ci <= loopTo2; ci++)
                    {
                        double tc = cBreaks[ci] + 0.75d * (cBreaks[ci + 1] - cBreaks[ci]);
                        double wx = xle + tc * chord;

                        if (proj == "3D")
                        {
                            var pPt = new Node(wx - view3DCenterX, yle - view3DCenterY, zle - view3DCenterZ, "", false, 0, Node.NodeType.Geometry);
                            if (!pPt.RotateZ(viewGamma).RotateY(viewAlpha).RotateX(viewBeta).Project(W, H, viewDist, viewFOV).Visible)
                                continue;
                        }

                        var pS = WorldToScreen(wx, yle, zle, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff);
                        G.FillEllipse(ptBrush, new RectangleF(pS.X - ptRadius, pS.Y - ptRadius, ptRadius * 2f, ptRadius * 2f));
                    }
                }
            }
        }

        // Draws a small red/green/blue X/Y/Z axis triad at the world origin
        // (AVL's "AXes, xyz ref." toggle). This project doesn't currently parse
        // the geometry file's Xref/Yref/Zref moment-reference point into a module
        // field, so the triad is anchored at (0,0,0) rather than the true moment
        // reference - close enough to orient the model, but not a stand-in for it.
        private void DrawReferenceAxesTriad(SvgGraphics G, string proj, int W, int H, double hMin, double hMax, double vMin, double vMax, double hOff, double vOff, double axisLength)
        {
            var origin = WorldToScreen(0d, 0d, 0d, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff);
            var xEnd = WorldToScreen(axisLength, 0d, 0d, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff);
            var yEnd = WorldToScreen(0d, axisLength, 0d, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff);
            var zEnd = WorldToScreen(0d, 0d, axisLength, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff);

            using (var pX = new Pen(Color.Red, 2f))
            using (var pY = new Pen(Color.LimeGreen, 2f))
            using (var pZ = new Pen(Color.DodgerBlue, 2f))
            {
                G.DrawLine(pX, origin, xEnd);
                G.DrawLine(pY, origin, yEnd);
                G.DrawLine(pZ, origin, zEnd);
            }
        }

        // Draws each panel's bound-vortex leg (AVL's "BOund leg" toggle) - the
        // standard VLM placement rule puts it at the local 1/4-chord of every
        // chordwise panel, spanning that one panel's width.
        private void DrawBoundLegsForSurface(SvgGraphics G, Surface su, string proj, int W, int H, double hMin, double hMax, double vMin, double vMax, double hOff, double vOff, Pen legPen, bool isDuplicate)
        {
            foreach (var seg in PanelSegments(su, isDuplicate))
            {
                for (int ci = 0, loopTo = seg.CBreaks.Length - 2; ci <= loopTo; ci++)
                {
                    double tc = seg.CBreaks[ci] + 0.25d * (seg.CBreaks[ci + 1] - seg.CBreaks[ci]);
                    for (int si = 0, loopTo1 = seg.SBreaks.Length - 2; si <= loopTo1; si++)
                    {
                        var p0 = seg.PtAt(seg.SBreaks[si], tc);
                        var p1 = seg.PtAt(seg.SBreaks[si + 1], tc);
                        if (proj == "3D" && !(Panel3DVisible(p0) && Panel3DVisible(p1)))
                            continue;
                        G.DrawLine(legPen, WorldToScreen(p0.X, p0.Y, p0.Z, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff), WorldToScreen(p1.X, p1.Y, p1.Z, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff));
                    }
                }
            }
        }

        // Draws each panel's trailing vortex legs (AVL's "TRailing legs" toggle) -
        // one leg shed from each spanwise end of every panel's bound-vortex leg,
        // trailing downstream (+X, body axis) to a fixed visualization length
        // rather than true infinity.
        private void DrawTrailingLegsForSurface(SvgGraphics G, Surface su, string proj, int W, int H, double hMin, double hMax, double vMin, double vMax, double hOff, double vOff, Pen legPen, bool isDuplicate, double trailLength)
        {
            foreach (var seg in PanelSegments(su, isDuplicate))
            {
                for (int ci = 0, loopTo = seg.CBreaks.Length - 2; ci <= loopTo; ci++)
                {
                    double tc = seg.CBreaks[ci] + 0.25d * (seg.CBreaks[ci + 1] - seg.CBreaks[ci]);
                    for (int si = 0, loopTo1 = seg.SBreaks.Length - 1; si <= loopTo1; si++)
                    {
                        var p0 = seg.PtAt(seg.SBreaks[si], tc);
                        var p1 = (X: p0.X + trailLength, Y: p0.Y, Z: p0.Z);
                        if (proj == "3D" && !(Panel3DVisible(p0) && Panel3DVisible(p1)))
                            continue;
                        G.DrawLine(legPen, WorldToScreen(p0.X, p0.Y, p0.Z, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff), this.WorldToScreen(p1.X, p1.Y, p1.Z, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff));
                    }
                }
            }
        }

        // Draws an outward panel-normal tick at the center of every panel (AVL's
        // "NOrmal vector" toggle), computed geometrically from the panel's own
        // corner points (chordwise edge x spanwise edge) - this reflects sweep,
        // dihedral and taper, but not airfoil camber (Section geometry is a flat
        // chord line only).
        private void DrawNormalVectorsForSurface(SvgGraphics G, Surface su, string proj, int W, int H, double hMin, double hMax, double vMin, double vMax, double hOff, double vOff, Pen vecPen, bool isDuplicate, double vecLength)
        {
            foreach (var seg in PanelSegments(su, isDuplicate))
            {
                for (int ci = 0, loopTo = seg.CBreaks.Length - 2; ci <= loopTo; ci++)
                {
                    double c0 = seg.CBreaks[ci];
                    double c1 = seg.CBreaks[ci + 1];
                    for (int si = 0, loopTo1 = seg.SBreaks.Length - 2; si <= loopTo1; si++)
                    {
                        double t0 = seg.SBreaks[si];
                        double t1 = seg.SBreaks[si + 1];
                        var A = seg.PtAt(t0, c0);
                        var B = seg.PtAt(t1, c0);
                        var C = seg.PtAt(t0, c1);
                        var center = seg.PtAt((t0 + t1) / 2.0d, (c0 + c1) / 2.0d);

                        double chordX = C.X - A.X;
                        double chordY = C.Y - A.Y;
                        double chordZ = C.Z - A.Z;
                        double spanX = B.X - A.X;
                        double spanY = B.Y - A.Y;
                        double spanZ = B.Z - A.Z;
                        double nX = chordY * spanZ - chordZ * spanY;
                        double nY = chordZ * spanX - chordX * spanZ;
                        double nZ = chordX * spanY - chordY * spanX;
                        double mag = Math.Sqrt(nX * nX + nY * nY + nZ * nZ);
                        if (mag < 1.0E-9d)
                            continue;
                        nX /= mag;
                        nY /= mag;
                        nZ /= mag;

                        var tip = (X: center.X + nX * vecLength, Y: center.Y + nY * vecLength, Z: center.Z + nZ * vecLength);
                        if (proj == "3D" && !(Panel3DVisible(center) && Panel3DVisible(tip)))
                            continue;
                        G.DrawLine(vecPen, WorldToScreen(center.X, center.Y, center.Z, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff), this.WorldToScreen(tip.X, tip.Y, tip.Z, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff));
                    }
                }
            }
        }

        // Shared per-panel-segment geometry used by the bound leg / trailing legs /
        // normal vector overlays - factors out the section-pair interpolation and
        // panel-break computation that DrawMeshForSurface/DrawControlPointsForSurface
        // each duplicated inline.
        private IEnumerable<PanelSegment> PanelSegments(Surface su, bool isDuplicate)
        {
            var sects = su.sections;
            if (sects is null || sects.Count < 2)
                yield break;
            int Nc = Math.Max(1, (int)Math.Round(su.Nchordwise > 0d ? su.Nchordwise : 8d));
            double[] cBreaks = GetPanelBreaks(Nc, su.Cspace);
            int Ns = Math.Max(1, (int)Math.Round(su.Nspanwise > 0d ? su.Nspanwise : 12d));
            double yDupVal = su.yDuplicate ? su.yDuplicatevalue : 0.0d;

            for (int segIdx = 0, loopTo = sects.Count - 2; segIdx <= loopTo; segIdx++)
            {
                var s0 = sects[segIdx];
                var s1 = sects[segIdx + 1];
                double y0Eff = isDuplicate ? 2d * yDupVal - s0.Yle : s0.Yle;
                double y1Eff = isDuplicate ? 2d * yDupVal - s1.Yle : s1.Yle;
                int segNs = s0.Nspanwise > 0d ? (int)Math.Round(s0.Nspanwise) : Ns;
                double segSp = s0.Nspanwise > 0d ? s0.Sspace : su.Sspace;
                double[] segSBreaks = GetPanelBreaks(segNs, segSp);

                yield return new PanelSegment(s0, s1, y0Eff, y1Eff, cBreaks, segSBreaks);
            }
        }

        private class PanelSegment
        {
            public readonly double[] CBreaks;
            public readonly double[] SBreaks;
            private readonly Section s0;
            private readonly Section s1;
            private readonly double y0Eff;
            private readonly double y1Eff;

            public PanelSegment(Section s0, Section s1, double y0Eff, double y1Eff, double[] cBreaks, double[] sBreaks)
            {
                this.s0 = s0;
                this.s1 = s1;
                this.y0Eff = y0Eff;
                this.y1Eff = y1Eff;
                CBreaks = cBreaks;
                SBreaks = sBreaks;
            }

            // t = spanwise fraction across this segment (0..1), c = chordwise fraction of local chord (0..1)
            public (double X, double Y, double Z) PtAt(double t, double c)
            {
                double xle = s0.Xle + t * (s1.Xle - s0.Xle);
                double yle = y0Eff + t * (y1Eff - y0Eff);
                double zle = s0.Zle + t * (s1.Zle - s0.Zle);
                double chord = s0.Chord + t * (s1.Chord - s0.Chord);
                return (xle + c * chord, yle, zle);
            }
        }

        private bool Panel3DVisible((double X, double Y, double Z) pt)
        {
            var n = new Node(pt.X - view3DCenterX, pt.Y - view3DCenterY, pt.Z - view3DCenterZ, "", false, 0, Node.NodeType.Geometry);
            return n.RotateZ(viewGamma).RotateY(viewAlpha).RotateX(viewBeta).Project(p3d.Width, p3d.Height, viewDist, viewFOV).Visible;
        }

        // Reads the current project's .avl source, split the same way ShowNodeProperties
        // does, so Section.lineNumber values line up for FindAirfoilForSection.
        private string[] GetProjectAvlLines()
        {
            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            if (!File.Exists(f))
                return Array.Empty<string>();
            return TrimAll(File.ReadAllText(f)).Replace(Constants.vbLf, "").Split(Conversions.ToChar(Constants.vbCrLf));
        }

        // Draws the camber-line shape at every defined section whose airfoil is a
        // NACA 4-digit code (AVL's "CAmber" toggle). Sections using AIRFOIL/AFILE
        // coordinate data are skipped - this app doesn't parse airfoil coordinate
        // files yet, so their camber shape isn't available to draw.
        private void DrawCamberlineForSurface(SvgGraphics G, Surface su, string proj, int W, int H, double hMin, double hMax, double vMin, double vMax, double hOff, double vOff, Pen camberPen, bool isDuplicate, string[] avlLines)
        {
            if (avlLines is null || avlLines.Length == 0)
                return;
            var sects = su.sections;
            if (sects is null)
                return;
            double yDupVal = su.yDuplicate ? su.yDuplicatevalue : 0.0d;

            foreach (Section s in sects)
            {
                var af = FindAirfoilForSection(avlLines, s.lineNumber);
                if (af is null || af.Item1 != "NACA")
                    continue;
                string code = af.Item2.Trim();
                if (code.Length != 4 || !code.All(char.IsDigit))
                    continue;
                double m = (Strings.Asc(code[0]) - Strings.Asc('0')) / 100.0d;
                double p = (Strings.Asc(code[1]) - Strings.Asc('0')) / 10.0d;
                if (p <= 0.0d)
                    continue; // symmetric section - camber line coincides with the chordline

                double yEff = isDuplicate ? 2d * yDupVal - s.Yle : s.Yle;
                const int N = 16;
                var pts = new List<PointF>();
                bool visible = true;
                for (int i = 0; i <= N; i++)
                {
                    double x = i / (double)N;
                    double yc;
                    if (x < p)
                    {
                        yc = m / (p * p) * (2d * p * x - x * x);
                    }
                    else
                    {
                        yc = m / ((1d - p) * (1d - p)) * (1d - 2d * p + 2d * p * x - x * x);
                    }
                    double wx = s.Xle + x * s.Chord;
                    double wz = s.Zle - yc * s.Chord;

                    if (proj == "3D")
                    {
                        if (!Panel3DVisible((wx, yEff, wz)))
                            visible = false;
                    }
                    pts.Add(WorldToScreen(wx, yEff, wz, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff));
                }
                if (visible && pts.Count >= 2)
                    G.DrawLines(camberPen, pts.ToArray());
            }
        }

        // Gathers every strip's (Y, c*cl) from the last Trefftz Plane run into one
        // Y-sorted list, for interpolating a loading value at an arbitrary Y.
        private List<(double Y, double CCl)> BuildLoadingSamples()
        {
            var samples = new List<(double Y, double CCl)>();
            if (_lastTrefftzSurfaces is null)
                return samples;
            foreach (TrefftzSurface surf in _lastTrefftzSurfaces)
            {
                foreach (TrefftzStrip strip in surf.Strips)
                    samples.Add((strip.Yle, strip.CCl));
            }
            samples.Sort((a, b) => a.Y.CompareTo(b.Y));
            return samples;
        }

        // Linearly interpolates c*cl at an arbitrary Y from a Y-sorted sample list,
        // clamping to the nearest strip past either tip.
        private double InterpolateLoading(List<(double Y, double CCl)> samples, double yTarget)
        {
            if (samples.Count == 0)
                return 0.0d;
            if (yTarget <= samples[0].Y)
                return samples[0].CCl;
            if (yTarget >= samples[samples.Count - 1].Y)
                return samples[samples.Count - 1].CCl;
            for (int i = 0, loopTo = samples.Count - 2; i <= loopTo; i++)
            {
                if (yTarget >= samples[i].Y && yTarget <= samples[i + 1].Y)
                {
                    double span = samples[i + 1].Y - samples[i].Y;
                    if (Math.Abs(span) < 1.0E-9d)
                        return samples[i].CCl;
                    double t = (yTarget - samples[i].Y) / span;
                    return samples[i].CCl + t * (samples[i + 1].CCl - samples[i].CCl);
                }
            }
            return 0.0d;
        }

        // Draws the spanwise loading (c*cl) from the last Trefftz Plane run (AVL's
        // "LOading" toggle) the same way AVL's own plot does: as the surface's own
        // chordwise/spanwise mesh grid, lifted in +Z by an amount proportional to
        // the local loading at each point's Y - so it reads as a "raised" mesh
        // (one rib per section, connected spanwise) rather than a single wire
        // running down the span. Requires a Trefftz Plane analysis to have been
        // run first (Analysis menu) - silently draws nothing until then.
        private void DrawLoadingOverlay(SvgGraphics G, string proj, int W, int H, double hMin, double hMax, double vMin, double vMax, double hOff, double vOff, Pen loadPen)
        {
            if (parsedSurfaces is null || parsedSurfaces.Count == 0)
                return;
            var samples = BuildLoadingSamples();
            if (samples.Count == 0)
                return;
            double cref = _lastTrefftzCref != 0d ? _lastTrefftzCref : 1.0d;

            // A local chord-scale height so the loading grid reads sensibly
            // regardless of the model's absolute size.
            double refChord = parsedSurfaces
                .SelectMany(su => su.sections ?? new List<Section>())
                .Select(s => s.Chord)
                .DefaultIfEmpty(1.0d)
                .Average();
            if (refChord <= 0d)
                refChord = 1.0d;

            double OffsetAt(double y) => InterpolateLoading(samples, y) / cref * refChord * 0.5d;
            (double X, double Y, double Z) Elevate((double X, double Y, double Z) p) => (p.X, p.Y, p.Z + OffsetAt(p.Y));

            foreach (var su in parsedSurfaces)
            {
                foreach (var dup in new[] { false, true })
                {
                    if (dup && !su.yDuplicate)
                        continue;
                    foreach (var seg in PanelSegments(su, dup))
                    {
                        // One "rib" per spanwise break - the LE-to-TE chordwise line at that
                        // station, lifted by that station's loading (mirrors DrawMeshForSurface's
                        // spanwise-boundary-line pass, but elevated).
                        foreach (var t in seg.SBreaks)
                        {
                            var pLe = Elevate(seg.PtAt(t, 0d));
                            var pTe = Elevate(seg.PtAt(t, 1d));
                            if (proj == "3D" && !(Panel3DVisible(pLe) && Panel3DVisible(pTe)))
                                continue;
                            G.DrawLine(loadPen, WorldToScreen(pLe.X, pLe.Y, pLe.Z, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff), WorldToScreen(pTe.X, pTe.Y, pTe.Z, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff));
                        }

                        // Spanwise lines connecting the ribs at each chordwise station,
                        // each sampled point lifted by its own Y's loading (mirrors
                        // DrawMeshForSurface's chordwise-panel-line pass).
                        foreach (var c in seg.CBreaks)
                        {
                            var pts = new List<PointF>();
                            bool vis = true;
                            foreach (var t in seg.SBreaks)
                            {
                                var p = Elevate(seg.PtAt(t, c));
                                if (proj == "3D" && !Panel3DVisible(p))
                                    vis = false;
                                pts.Add(WorldToScreen(p.X, p.Y, p.Z, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff));
                            }
                            if (vis && pts.Count >= 2)
                                G.DrawLines(loadPen, pts.ToArray());
                        }
                    }
                }
            }
        }

        private PointF WorldToScreen(double wx, double wy, double wz, string proj, int W, int H, double hMin, double hMax, double vMin, double vMax, double hOff, double vOff)
        {
            double sh;
            double sv;
            switch (proj ?? "")
            {
                case "XZ":
                    {
                        sh = wx * W / (hMax - hMin) + W / 2d + hOff;
                        sv = -wz * H / (vMax - vMin) + H / 2d + vOff;
                        break;
                    }
                case "YZ":
                    {
                        sh = -wy * W / (hMax - hMin) + W / 2d + hOff;
                        sv = -wz * H / (vMax - vMin) + H / 2d + vOff;
                        break;
                    }
                case "3D":
                    {
                        var pNode = new Node(wx - view3DCenterX, wy - view3DCenterY, wz - view3DCenterZ, "", false, 0, Node.NodeType.Geometry);
                        var projNode = pNode.RotateZ(viewGamma).RotateY(viewAlpha).RotateX(viewBeta).Project(W, H, viewDist, viewFOV);
                        sh = projNode.X;
                        sv = projNode.Y; // XY
                        break;
                    }

                default:
                    {
                        sh = wx * W / (hMax - hMin) + W / 2d + hOff;
                        sv = -wy * H / (vMax - vMin) + H / 2d + vOff;
                        break;
                    }
            }
            return new PointF((float)sh, (float)sv);
        }


        public bool anyHovered()
        {
            // Dim result As Boolean = False

            foreach (Node p in points)
            {
                if (p.Hovered)
                    return true;
            }

            return false;
        }

        private void p1_MouseMove(object sender, MouseEventArgs e)
        {
            double e1 = (xmax - xmin) / pxy.Width * (e.X - pxy.Width / 2d - xoffset);
            double e2 = (ymax - ymin) / pxy.Height * (e.Y - pxy.Height / 2d - yoffset);
            curX = e1;
            curY = -e2;
            lblCursor.Text = "Cursor: [X: " + string.Format("{0,5:###.0}", Math.Round(e1, 1)) + ", Y: " + string.Format("{0,5:###.0}", Math.Round(-e2, 1)) + "]";

            if (isDragMode)
            {
                double hitEps = 7.0d * (xmax - xmin) / pxy.Width;
                bool nearDraggable = points.Any(n => n.IsDraggable && Math.Abs(n.X - e1) < hitEps && Math.Abs(n.Y - (-e2)) < hitEps);
                pxy.Cursor = nearDraggable || isDragging ? Cursors.SizeAll : Cursors.Hand;
                if (isDragging && draggingNode is not null && e.Button == MouseButtons.Left)
                {
                    draggingNode.X = (float)e1;
                    draggingNode.Y = (float)-e2;
                    draggingNode.Point = new Point3D(e1, -e2, draggingNode.Z);
                    drawAxes();
                }
                return;
            }

            // --- Normal hover / pan behaviour ---
            // Hit tolerance is scaled to the current view bounds (constant ~7px on
            // screen) rather than a fixed world-unit eps, so nodes stay easy to
            // select regardless of how zoomed out a large-dimension aircraft is.
            double hitEpsX = 7.0d * (xmax - xmin) / pxy.Width;
            double hitEpsY = 7.0d * (ymax - ymin) / pxy.Height;
            var prevHovered = points.FirstOrDefault(p => p.Hovered);
            Node newHovered = null;
            foreach (Node p in points)
            {
                if (p.X > e1 - hitEpsX & p.X < e1 + hitEpsX & p.Y > -e2 - hitEpsY & p.Y < -e2 + hitEpsY)
                {
                    if (p.type == Node.NodeType.Geometry & showSection & showHover)
                    {
                        newHovered = p;
                        break;
                    }
                    if (p.type == Node.NodeType.Mass & showMass & showHover)
                    {
                        newHovered = p;
                    }
                }
            }

            bool hoverChanged = !ReferenceEquals(newHovered, prevHovered);
            foreach (Node p in points)
                p.Hovered = ReferenceEquals(p, newHovered);
            if (newHovered is not null)
            {
                isHovered = true;
                tc1.SelectedIndex = newHovered.type == Node.NodeType.Mass ? 1 : 0;
                selectText(newHovered.lineNumber);
            }
            else
            {
                isHovered = false;
            }

            bool panning = e.Button == MouseButtons.Left && !isHovered;
            if (panning)
            {
                xoffset = e.X - xdown;
                yoffset = e.Y - ydown;
            }

            if (hoverChanged || panning || showHover)
                drawAxes();
        }

        private void selectText(int lineNumber)
        {
            {
                ref var withBlock = ref txt3;
                // .SelectAll()
                var line = withBlock.GetLine(lineNumber);
                withBlock.SelectionStart = txt3.Text.IndexOf(withBlock.GetLineText(lineNumber));
                withBlock.SelectionLength = withBlock.GetLineLength(lineNumber);
                withBlock.Invalidate();
                withBlock.DoCaretVisible();
                // SendKeys.Send("{HOME}+{END}")
            }
            // Me.Text = lineNumber.ToString + "," + sstart.ToString + "," + send.ToString + " | " + txt3.GetLineText(lineNumber)

        }

        private void frmGeometry_Resize(object sender, EventArgs e)
        {
            ClampScupSplitter();
            drawAxes();
        }

        private void p1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseDownScreenPos = e.Location;
                if (isDragMode)
                {
                    double e1 = (xmax - xmin) / pxy.Width * (e.X - pxy.Width / 2d - xoffset);
                    double e2 = (ymax - ymin) / pxy.Height * (e.Y - pxy.Height / 2d - yoffset);
                    double hitEps = 7.0d * (xmax - xmin) / pxy.Width;
                    foreach (Node n in points)
                    {
                        if (n.IsDraggable && Math.Abs(n.X - e1) < hitEps && Math.Abs(n.Y - (-e2)) < hitEps)
                        {
                            draggingNode = n;
                            isDragging = true;
                            dragStartX = n.X;
                            dragStartY = n.Y;
                            dragStartZ = n.Z;
                            pxy.Cursor = Cursors.SizeAll;
                            break;
                        }
                    }
                    return;
                }
                pxy.Cursor = Cursors.SizeAll;
                xdown = e.X - xoffset;
                ydown = e.Y - yoffset;
                isHovered = anyHovered();
            }
        }

        private void p1_MouseWheel(object sender, MouseEventArgs e)
        {
            // Me.Text = e.Delta
            if (e.Delta < 0)
            {
                gridnumber += 1;
                drawAxes();
            }
            else if (gridnumber > 1)
            {
                gridnumber -= 1;
                drawAxes();
            }
        }

        private void p1_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragMode && isDragging && draggingNode is not null)
            {
                isDragging = false;
                double e1 = (xmax - xmin) / pxy.Width * (e.X - pxy.Width / 2d - xoffset);
                double e2 = (ymax - ymin) / pxy.Height * (e.Y - pxy.Height / 2d - yoffset);
                CommitNodeDrag(draggingNode, e1, -e2, dragStartZ);  // Z unchanged in XY view
                draggingNode = null;
                pxy.Cursor = Cursors.Hand;
                return;
            }
            pxy.Cursor = Cursors.Default;
            if (e.Button == MouseButtons.Left)
                drawAxes();
            HandlePotentialNodeClick(e);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            points.Clear();
            drawAxes();
        }

        // This menu is shown regardless of which of the Geometry/Mass/Run tabs is
        // active, but txt3 (the shared editor) only ever holds the currently
        // selected tab's content - without this guard, clicking "AVL Template"
        // while on the Mass or Run tab would silently blow away that tab's content
        // with the AVL template text instead.
        private void AVLTemplateFullToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReplaceGeometryTabWithTemplate(AvlTemplates.AvlTemplateFull);
        }

        private void AVLTemplateMinimalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReplaceGeometryTabWithTemplate(AvlTemplates.AvlTemplateMinimal);
        }

        private void ReplaceGeometryTabWithTemplate(string val)
        {
            if (tc1.SelectedTab is not null && tc1.SelectedTab.Name != "Geometry")
            {
                AppMessageBox.Show("Switch to the Geometry tab first - this would replace the " + tc1.SelectedTab.Name + " tab's content otherwise.", "Wrong Tab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Replace entire text via selection to preserve undo history
            txt3.Selection.Start = new Place(0, 0);
            txt3.Selection.End = new Place(txt3.Lines[txt3.LinesCount - 1].Length, txt3.LinesCount - 1);
            txt3.InsertText(val);

            txt3.SelectionStart = txt3.Text.Length;
            txt3.DoCaretVisible();
            FormatActiveText();
        }

        private void SurfaceFullToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InsertSurfaceTemplate(AvlTemplates.SurfaceTemplateFull);
        }

        private void SurfaceMinimalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InsertSurfaceTemplate(AvlTemplates.SurfaceTemplateMinimal);
        }

        private void InsertSurfaceTemplate(string template)
        {

            var before = CountBlocks();
            var after = CountBlocks(false);

            // 1. Calculate the current state based on what is BEFORE the cursor
            // If (Starts - Ends) > 0, we are currently inside that block.
            bool insideGeometry = before["!begingeometry"] - before["!endgeometry"] > 0;
            bool insideSurface = before["!beginsurface"] - before["!endsurface"] > 0;

            // 2. Check existence of the main wrapper tags
            bool hasGeometryTags = before["!begingeometry"] + after["!begingeometry"] > 0 && before["!endgeometry"] + after["!endgeometry"] > 0;

            // --- VALIDATION CHECKS ---

            // CHECK 1: Global Structure
            if (!hasGeometryTags)
            {
                AppMessageBox.Show("Your AVL file is missing !begingeometry or !endgeometry tags." + Environment.NewLine + Environment.NewLine + "Please add an AVL template first.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // CHECK 2: Must be inside Geometry
            if (!insideGeometry)
            {
                AppMessageBox.Show("You must insert a Surface block inside the Geometry tags." + Environment.NewLine + Environment.NewLine + "Move your cursor between !begingeometry and !endgeometry.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // CHECK 3: Cannot be inside another Surface (No Nested Surfaces)
            if (insideSurface)
            {
                AppMessageBox.Show("You cannot add a Surface block inside another Surface block." + Environment.NewLine + Environment.NewLine + "Move your cursor outside of existing !beginsurface and !endsurface tags.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // --- INSERTION LOGIC ---

            string contentToInsert = Environment.NewLine + template;
            txt3.InsertText(contentToInsert);
            txt3.Focus();

        }

        private void SectionFullToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InsertSectionTemplate(AvlTemplates.SectionTemplateFull);
        }

        private void SectionMinimalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InsertSectionTemplate(AvlTemplates.SectionTemplateMinimal);
        }

        private void InsertSectionTemplate(string template)
        {

            var before = CountBlocks();
            var after = CountBlocks(false);

            // 1. Calculate the current state based on what is BEFORE the cursor
            // If (Starts - Ends) > 0, we are currently inside that block.
            bool insideGeometry = before["!begingeometry"] - before["!endgeometry"] > 0;
            bool insideSurface = before["!beginsurface"] - before["!endsurface"] > 0;
            bool insideSection = before["!beginsection"] - before["!endsection"] > 0;
            bool insideControl = before["!begincontrol"] - before["!endcontrol"] > 0;

            // 2. Check existence of the main wrapper tags
            bool hasGeometryTags = before["!begingeometry"] + after["!begingeometry"] > 0 && before["!endgeometry"] + after["!endgeometry"] > 0;
            bool hasSurfaceTags = before["!beginsurface"] + after["!beginsurface"] > 0 && before["!endsurface"] + after["!endsurface"] > 0;

            // --- VALIDATION CHECKS ---

            // CHECK 1: Global Structure (Geometry)
            if (!hasGeometryTags)
            {
                AppMessageBox.Show("Your AVL file is missing !begingeometry or !endgeometry tags." + Environment.NewLine + Environment.NewLine + "Please add an AVL template first.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // CHECK 2: Global Structure (Surface)
            if (!hasSurfaceTags)
            {
                AppMessageBox.Show("Your AVL file is missing !beginsurface or !endsurface tags." + Environment.NewLine + Environment.NewLine + "Please add a Surface template first.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // CHECK 3: Must be inside a Surface
            if (!insideSurface)
            {
                AppMessageBox.Show("You must insert a Section block inside a Surface block." + Environment.NewLine + Environment.NewLine + "Move your cursor between !beginsurface and !endsurface.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // CHECK 4: Cannot be inside another Section (No Nested Sections)
            if (insideSection)
            {
                AppMessageBox.Show("You cannot add a Section block inside another Section block." + Environment.NewLine + Environment.NewLine + "Move your cursor outside of the existing !beginsection and !endsection tags.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // CHECK 5: Cannot be inside a Control (Section cannot be inside Control)
            if (insideControl)
            {
                AppMessageBox.Show("You cannot add a Section block inside a Control block." + Environment.NewLine + Environment.NewLine + "Move your cursor outside of the existing !begincontrol and !endcontrol tags.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            // --- INSERTION LOGIC ---

            string contentToInsert = Environment.NewLine + template;
            txt3.InsertText(contentToInsert);
            txt3.Focus();

        }

        private void ControlFullToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InsertControlTemplate(AvlTemplates.ControlTemplateFull);
        }

        private void ControlMinimalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InsertControlTemplate(AvlTemplates.ControlTemplateMinimal);
        }

        private void InsertControlTemplate(string template)
        {

            var before = CountBlocks();
            var after = CountBlocks(false);

            // 1. Calculate the current "depth" based on what is BEFORE the cursor.
            // If Depth is > 0, we are currently inside that block.
            bool insideGeometry = before["!begingeometry"] - before["!endgeometry"] > 0;
            bool insideSurface = before["!beginsurface"] - before["!endsurface"] > 0;
            bool insideSection = before["!beginsection"] - before["!endsection"] > 0;
            bool insideControl = before["!begincontrol"] - before["!endcontrol"] > 0;

            // 2. Check existence (File must actually contain the tags)
            bool hasGeometryTags = before["!begingeometry"] + after["!begingeometry"] > 0 && before["!endgeometry"] + after["!endgeometry"] > 0;
            bool hasSurfaceTags = before["!beginsurface"] + after["!beginsurface"] > 0 && before["!endsurface"] + after["!endsurface"] > 0;
            bool hasSectionTags = before["!beginsection"] + after["!beginsection"] > 0 && before["!endsection"] + after["!endsection"] > 0;

            // --- VALIDATION CHECKS ---

            // CHECK 1: Geometry Context
            if (!hasGeometryTags)
            {
                AppMessageBox.Show("Your AVL file is missing !begingeometry or !endgeometry tags." + Environment.NewLine + Environment.NewLine + "Please add an AVL template first.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!insideGeometry)
            {
                AppMessageBox.Show("You must insert this component inside the Geometry block." + Environment.NewLine + Environment.NewLine + "Move your cursor between !begingeometry and !endgeometry.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // CHECK 2: Surface Context
            if (!hasSurfaceTags)
            {
                AppMessageBox.Show("Your AVL file is missing !beginsurface or !endsurface tags.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!insideSurface)
            {
                AppMessageBox.Show("You must insert this component inside a Surface block." + Environment.NewLine + Environment.NewLine + "Move your cursor between !beginsurface and !endsurface.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // CHECK 3: Section Context (Assuming Control must be inside a Section)
            // If controls CAN exist outside sections in your specific usage, remove this block.
            if (!insideSection)
            {
                AppMessageBox.Show("You must insert the Control block inside a Section." + Environment.NewLine + Environment.NewLine + "Move your cursor between !beginsection and !endsection.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // CHECK 4: Nested Controls (Prevent Control inside Control)
            if (insideControl)
            {
                AppMessageBox.Show("You cannot place a Control block inside another Control block." + Environment.NewLine + Environment.NewLine + "Move your cursor outside of existing !begincontrol and !endcontrol tags.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // --- INSERTION LOGIC ---

            string contentToInsert = Environment.NewLine + template;
            txt3.InsertText(contentToInsert);
            txt3.Focus();

        }

        private void SeparatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string val = "#===========================================";
            int sel = txt3.SelectionStart;
            txt3.Text = txt3.Text.Insert(sel, Environment.NewLine + val);
            txt3.SelectionStart = sel + (Environment.NewLine + val).Length;
            txt3.DoCaretVisible();


        }

        private void btnDragMode_Click(object sender, EventArgs e)
        {
            isDragMode = !isDragMode;
            // Cancel any in-progress drag when the mode is toggled off
            if (!isDragMode)
            {
                isDragging = false;
                draggingNode = null;
            }

            btnDragMode.Text = isDragMode ? "Drag Nodes: On" : "Drag Nodes: Off";
            btnDragMode.BackColor = isDragMode ? Color.FromArgb(192, 255, 192) : Color.FromArgb(220, 220, 220);

            var c = isDragMode ? Cursors.Hand : Cursors.Default;
            pxy.Cursor = c;
            pxz.Cursor = c;
            pyz.Cursor = c;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ctxAddMenu.Show(btnAdd, new Point(0, btnAdd.Height));
        }

        private void btnPrettify_Click(object sender, EventArgs e)
        {
            if (txt3 is null || txt3.LinesCount == 0)
                return;
            FormatActiveText();
            UpdateUndoRedoState();
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            if (txt3 is not null && txt3.UndoEnabled)
            {
                txt3.Undo();
            }
        }

        private void btnRedo_Click(object sender, EventArgs e)
        {
            if (txt3 is not null && txt3.RedoEnabled)
            {
                txt3.Redo();
            }
        }

        private void btnTabIncrease_Click(object sender, EventArgs e)
        {
            if (txt3 is null)
                return;
            autoSpace = true;
            btnSpace.Text = "Auto Space: On";
            if (autoSpaceWidth < MaxAutoSpaceWidth)
            {
                autoSpaceWidth += 1;
            }
            SaveAutoSpaceSettings();
            ReformatPreservingScroll();
            AppToast.Show($"Auto spacing: {autoSpaceWidth}", MessageBoxIcon.Information, 1200);
        }

        private void btnTabDecrease_Click(object sender, EventArgs e)
        {
            if (txt3 is null)
                return;
            autoSpace = true;
            btnSpace.Text = "Auto Space: On";
            if (autoSpaceWidth > MinAutoSpaceWidth)
            {
                autoSpaceWidth -= 1;
            }
            SaveAutoSpaceSettings();
            ReformatPreservingScroll();
            AppToast.Show($"Auto spacing: {autoSpaceWidth}", MessageBoxIcon.Information, 1200);
        }

        private void SaveAutoSpaceSettings()
        {
            My.MySettingsProperty.Settings.autoSpaceEnabled = autoSpace;
            My.MySettingsProperty.Settings.autoSpaceWidth = autoSpaceWidth;
            My.MySettingsProperty.Settings.Save();
        }

        // FormatActiveText() already restores the scroll position it captures at its own start, but
        // the immediate (non-delayed) txt3.TextChanged handler fires mid-InsertText and re-runs syntax
        // highlighting/folding, which nudges FastColoredTextBox to scroll the caret into view - so by
        // the time FormatActiveText's own restore runs, that's not the last word. Re-apply the scroll
        // position once more here, and again after the message queue settles, so the click doesn't
        // visibly jump the viewport.
        private void ReformatPreservingScroll()
        {
            int vsv = txt3.VerticalScroll.Value;
            int hsv = txt3.HorizontalScroll.Value;

            FormatActiveText();
            UpdateUndoRedoState();

            txt3.VerticalScroll.Value = Math.Min(vsv, txt3.VerticalScroll.Maximum);
            txt3.HorizontalScroll.Value = Math.Min(hsv, txt3.HorizontalScroll.Maximum);

            BeginInvoke(() =>
                {
                    if (txt3 is null)
                        return;
                    txt3.VerticalScroll.Value = Math.Min(vsv, txt3.VerticalScroll.Maximum);
                    txt3.HorizontalScroll.Value = Math.Min(hsv, txt3.HorizontalScroll.Maximum);
                });
        }

        private void UpdateUndoRedoState()
        {
            if (txt3 is null)
                return;
            btnUndo.Enabled = txt3.UndoEnabled;
            btnRedo.Enabled = txt3.RedoEnabled;
        }

        // Writes the new world coordinates for a dragged node back to the AVL or mass file,
        // then refreshes the editor text (if the matching tab is active) and redraws.
        // 
        // Axis semantics per view:
        // pxy  → newX/newY used; newZ = dragStartZ (unchanged)
        // pxz  → newX/newZ used; newY = dragStartY (unchanged)
        // pyz  → newY/newZ used; newX = dragStartX (unchanged)
        // 
        // NodeSubType controls what column(s) get updated:
        // LeadingEdge   → Xle, Yle, Zle (cols 0-2 of the section data line)
        // TrailingEdge  → Chord = newX - Xle (col 3; Y/Z ignored since chord is axial)
        // ControlHinge  → Xhinge = (newX - parentXle) / parentChord (col 2 of the control line)
        private void CommitNodeDrag(Node node, double newX, double newY, double newZ)
        {
            if (string.IsNullOrEmpty(projectName))
                return;
            if (node.type == Node.NodeType.Geometry)
            {
                string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
                if (!File.Exists(f))
                    return;

                string raw = TrimAll(File.ReadAllText(f));
                string[] lines = raw.Replace(Constants.vbLf, "").Split(Conversions.ToChar(Constants.vbCrLf));
                int changedLine = -1;

                switch (node.SubType)
                {

                    case Node.NodeSubType.LeadingEdge:
                        {
                            int ln = node.lineNumber;
                            if (ln < 0 || ln >= lines.Length)
                                return;
                            string[] parts = lines[ln].Split(' ');
                            if (parts.Length < 3)
                                return;
                            parts[0] = newX.ToString("G6");
                            parts[1] = newY.ToString("G6");
                            parts[2] = newZ.ToString("G6");
                            string originalLineText = ln < txt3.LinesCount ? txt3.Lines[ln] : "";
                            string leadingWS = GetLeadingWhitespace(originalLineText);
                            lines[ln] = leadingWS + FormatLineText(string.Join(" ", parts)).TrimStart();
                            changedLine = ln;
                            break;
                        }

                    case Node.NodeSubType.TrailingEdge:
                        {
                            // Only X is meaningful — chord is always axial so Y/Z can't shift independently
                            int ln = node.lineNumber;
                            if (ln < 0 || ln >= lines.Length)
                                return;
                            string[] parts = lines[ln].Split(' ');
                            if (parts.Length < 4)
                                return;
                            double xle = Conversions.ToDouble(parts[0]);
                            double newChord = newX - xle;
                            if (newChord < 0.001d)
                                return;   // reject zero/negative chord
                            parts[3] = newChord.ToString("G6");
                            string originalLineText = ln < txt3.LinesCount ? txt3.Lines[ln] : "";
                            string leadingWS = GetLeadingWhitespace(originalLineText);
                            lines[ln] = leadingWS + FormatLineText(string.Join(" ", parts)).TrimStart();
                            changedLine = ln;
                            break;
                        }

                    case Node.NodeSubType.ControlHinge:
                        {
                            // Only X is meaningful — Xhinge is a chord fraction
                            if (node.parentChord <= 0f)
                                return;
                            double fraction = (newX - node.parentXle) / node.parentChord;
                            fraction = Math.Max(0.001d, Math.Min(0.999d, fraction));
                            // Preserve the original sign convention (negative Xhinge = measured from TE)
                            double newXhinge = node.originalXhinge >= 0f ? fraction : -fraction;
                            int ctrlLn = node.controlLineNumber;
                            if (ctrlLn < 0 || ctrlLn >= lines.Length)
                                return;
                            string[] ctrlParts = lines[ctrlLn].Split(' ');
                            if (ctrlParts.Length < 3)
                                return;
                            ctrlParts[2] = newXhinge.ToString("G6");
                            string originalLineText = ctrlLn < txt3.LinesCount ? txt3.Lines[ctrlLn] : "";
                            string leadingWS = GetLeadingWhitespace(originalLineText);
                            lines[ctrlLn] = leadingWS + FormatLineText(string.Join(" ", ctrlParts)).TrimStart();
                            changedLine = ctrlLn;
                            break;
                        }

                    default:
                        {
                            return;
                        }
                }

                string newContent = string.Join(Constants.vbCrLf, lines);
                File.WriteAllText(f, newContent);

                if (tc1.SelectedTab.Name == "Geometry" && changedLine != -1)
                {
                    updating = true;
                    ReplaceEditorLine(changedLine, lines[changedLine]);
                    updating = false;
                }
            }

            else if (node.type == Node.NodeType.Mass)
            {
                string f = Path.Combine(Application.StartupPath, $"{projectName}.mass");
                if (!File.Exists(f))
                    return;

                string[] lines = File.ReadAllLines(f);
                int ln = node.lineNumber;
                if (ln < 0 || ln >= lines.Length)
                    return;

                string[] parts = lines[ln].Split(' ');
                if (parts.Length < 4)
                    return;

                parts[1] = newX.ToString("G6");
                parts[2] = newY.ToString("G6");
                parts[3] = newZ.ToString("G6");
                string originalLineText = ln < txt3.LinesCount ? txt3.Lines[ln] : "";
                string leadingWS = GetLeadingWhitespace(originalLineText);
                lines[ln] = leadingWS + FormatLineText(string.Join(" ", parts)).TrimStart();

                string newContent = string.Join(Constants.vbCrLf, lines);
                File.WriteAllText(f, newContent);

                if (tc1.SelectedTab.Name == "Mass")
                {
                    updating = true;
                    ReplaceEditorLine(ln, lines[ln]);
                    updating = false;
                }
            }

            findPoints();
            drawAxes();
        }

        // Distinguishes a genuine click (open the properties panel) from a pan/drag
        // (which already has its own handling above and returns before reaching here).
        // Only fires outside drag mode - in drag mode, repositioning is the click.
        private void HandlePotentialNodeClick(MouseEventArgs e)
        {
            if (isDragMode)
                return;
            if (e.Button != MouseButtons.Left)
                return;
            int dx = e.X - mouseDownScreenPos.X;
            int dy = e.Y - mouseDownScreenPos.Y;
            if (dx * dx + dy * dy > 16)
                return;   // moved more than ~4px -> treat as a pan, not a click

            // Mirrored (YDUPLICATE) nodes are included here too - they share the
            // same underlying line as their primary, so clicking either side opens
            // the same properties. Mass nodes are always independently clickable
            // (mass points aren't mirrored/subtyped the way geometry nodes are).
            Node hovered = null;
            foreach (Node p in points)
            {
                if (p.Hovered && (p.type == Node.NodeType.Mass || p.type == Node.NodeType.Geometry && (p.SubType == Node.NodeSubType.LeadingEdge || p.SubType == Node.NodeSubType.TrailingEdge || p.SubType == Node.NodeSubType.ControlHinge)))
                {
                    hovered = p;
                    break;
                }
            }
            if (hovered is not null)
                ShowNodeProperties(hovered);
        }

        private void InitializePropertiesPanel()
        {
            if (pnlProperties is not null)
                return;

            pnlProperties = new System.Windows.Forms.Panel();
            pnlProperties.Dock = DockStyle.Right;
            pnlProperties.Width = 270;
            pnlProperties.BackColor = Color.WhiteSmoke;
            pnlProperties.BorderStyle = BorderStyle.FixedSingle;
            pnlProperties.Visible = false;
            Controls.Add(pnlProperties);

            lblPropHeader = new System.Windows.Forms.Label()
            {
                Text = "Properties",
                AutoSize = false,
                Location = new Point(10, 10),
                Size = new Size(200, 20),
                Font = new Font(Font, FontStyle.Bold)
            };
            pnlProperties.Controls.Add(lblPropHeader);

            btnPropClose.Text = "x";
            btnPropClose.Location = new Point(236, 6);
            btnPropClose.Size = new Size(24, 24);
            btnPropClose.FlatStyle = FlatStyle.Flat;
            btnPropClose.BackColor = Color.White;
            btnPropClose.Cursor = Cursors.Hand;
            btnPropClose.Click += (s, ev) => HidePropertiesPanel();
            pnlProperties.Controls.Add(btnPropClose);

            // Section fields (Leading/Trailing edge node clicked)
            pnlSectionFields = new System.Windows.Forms.Panel() { Location = new Point(0, 40), Size = new Size(268, 180) };
            AddPropRow(pnlSectionFields, 4, "Xle:", txtPropXle, "Airfoil's leading edge location, X. (AVL SECTION keyword)");
            AddPropRow(pnlSectionFields, 32, "Yle:", txtPropYle, "Airfoil's leading edge location, Y. (AVL SECTION keyword)");
            AddPropRow(pnlSectionFields, 60, "Zle:", txtPropZle, "Airfoil's leading edge location, Z. (AVL SECTION keyword)");
            AddPropRow(pnlSectionFields, 88, "Chord:", txtPropChord, "The airfoil's chord. Trailing edge is located at (Xle+Chord, Yle, Zle). (AVL SECTION keyword)");
            AddPropRow(pnlSectionFields, 116, "Ainc (twist):", txtPropAinc, "Incidence angle (deg), a rotation about the surface's spanwise axis projected onto the Y-Z plane. Only affects the flow-tangency boundary condition - does not rotate the drawn geometry. (AVL SECTION keyword)");
            lblPropAirfoilKind = new System.Windows.Forms.Label() { Text = "Airfoil:", AutoSize = true, Location = new Point(10, 147) };
            txtPropAirfoil.Location = new Point(120, 144);
            txtPropAirfoil.Width = 96;
            var helpAirfoil = new System.Windows.Forms.Label()
            {
                Text = "?",
                AutoSize = false,
                Size = new Size(18, 18),
                Location = new Point(222, 146),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Gainsboro,
                Cursor = Cursors.Help,
                Font = new Font(Font.FontFamily, 7.5f, FontStyle.Bold)
            };
            string airfoilHelpText = "Camber-line shape for this section: a NACA 4-digit code (NACA keyword) or an airfoil coordinate/.dat filename (AIRFOIL/AFILE keyword). Chord and incidence are linearly interpolated between defining sections.";
            propHelpTip.SetToolTip(helpAirfoil, airfoilHelpText);
            // Hover tooltips on these dynamically-added Properties Panel labels have proven
            // unreliable in this app (same symptom already fixed once for the 3D view's
            // rotation-hint button - see AddRotationHintTo) - clicking now shows the same text
            // explicitly so the help is reachable either way, not just on a hover that may not fire.
            helpAirfoil.Click += (s, ev) => AppMessageBox.Show(airfoilHelpText, "Airfoil", MessageBoxButtons.OK, MessageBoxIcon.Information);
            pnlSectionFields.Controls.Add(lblPropAirfoilKind);
            pnlSectionFields.Controls.Add(txtPropAirfoil);
            pnlSectionFields.Controls.Add(helpAirfoil);
            pnlProperties.Controls.Add(pnlSectionFields);

            // Control fields (hinge node clicked)
            pnlControlFields = new System.Windows.Forms.Panel() { Location = new Point(0, 40), Size = new Size(268, 210), Visible = false };
            AddPropRow(pnlControlFields, 4, "Cname:", txtPropCname, "Name of the control variable. Reuse the same name across multiple sections/surfaces to link their deflections together (e.g. flap-to-elevator mixing). (AVL CONTROL keyword)");
            AddPropRow(pnlControlFields, 32, "Cgain:", txtPropCgain, "Control deflection gain: degrees of surface deflection per unit of the control variable.");
            AddPropRow(pnlControlFields, 60, "Xhinge:", txtPropXhinge, "x/c location of the hinge line. Positive: control surface spans Xhinge..1 (trailing-edge surface). Negative: spans 0..-Xhinge (leading-edge surface).");
            AddPropRow(pnlControlFields, 88, "XYZhvec X:", txtPropHx, "X component of the vector giving the hinge axis the surface rotates about. Positive deflection is a positive (right-hand rule) rotation about this vector. Setting X,Y,Z all to 0 aligns the hinge vector along the hinge line itself.");
            AddPropRow(pnlControlFields, 116, "XYZhvec Y:", txtPropHy, "Y component of the hinge-axis vector. See XYZhvec X for details.");
            AddPropRow(pnlControlFields, 144, "XYZhvec Z:", txtPropHz, "Z component of the hinge-axis vector. See XYZhvec X for details.");
            AddPropRow(pnlControlFields, 172, "SgnDup:", txtPropSgnDup, "Sign of the deflection on the mirrored (YDUPLICATE) surface. Symmetric controls (e.g. elevator) use +1; anti-symmetric ones (e.g. aileron) use -1. A magnitude other than 1 also scales the mirrored deflection.");
            pnlProperties.Controls.Add(pnlControlFields);

            // Mass fields (mass point clicked)
            pnlMassFields = new System.Windows.Forms.Panel() { Location = new Point(0, 40), Size = new Size(268, 210), Visible = false };
            AddPropRow(pnlMassFields, 4, "Mass:", txtPropMassVal, "Mass of this item.");
            AddPropRow(pnlMassFields, 32, "X:", txtPropMassX, "Location of this item's own center of gravity, X. Must use the same coordinate system (origin, orientation, units) as the .avl geometry file.");
            AddPropRow(pnlMassFields, 60, "Y:", txtPropMassY, "Location of this item's own center of gravity, Y. Must use the same coordinate system as the .avl geometry file.");
            AddPropRow(pnlMassFields, 88, "Z:", txtPropMassZ, "Location of this item's own center of gravity, Z. Must use the same coordinate system as the .avl geometry file.");
            AddPropRow(pnlMassFields, 116, "Ixx:", txtPropIxx, "Moment of inertia about the item's own CG: Ixx = integral of (y²+z²) dm.");
            AddPropRow(pnlMassFields, 144, "Iyy:", txtPropIyy, "Moment of inertia about the item's own CG: Iyy = integral of (x²+z²) dm.");
            AddPropRow(pnlMassFields, 172, "Izz:", txtPropIzz, "Moment of inertia about the item's own CG: Izz = integral of (x²+y²) dm.");
            pnlProperties.Controls.Add(pnlMassFields);

            btnPropApply.Text = "Apply";
            btnPropApply.Location = new Point(10, 258);
            btnPropApply.Size = new Size(100, 28);
            btnPropApply.FlatStyle = FlatStyle.Flat;
            btnPropApply.BackColor = Color.White;
            btnPropApply.Cursor = Cursors.Hand;
            btnPropApply.Click += btnPropApply_Click;
            pnlProperties.Controls.Add(btnPropApply);

            var lblPropHint = new System.Windows.Forms.Label()
            {
                Text = "Click a section, control, or mass node in any view to edit it here.",
                AutoSize = false,
                Size = new Size(250, 45),
                Location = new Point(10, 300),
                ForeColor = Color.Gray
            };
            pnlProperties.Controls.Add(lblPropHint);
        }

        // The trailing "?" is a small help indicator - hover it for a description of the field
        // sourced from AVL's own documentation (avl_doc.txt). Hover tooltips on these
        // dynamically-added labels have proven unreliable in this app (same symptom already
        // fixed once for the 3D view's rotation-hint button - see AddRotationHintTo), so clicking
        // also shows the same text explicitly, guaranteeing the help is reachable either way.
        private void AddPropRow(System.Windows.Forms.Panel parent, int y, string labelText, System.Windows.Forms.TextBox tb, string helpText)
        {
            var lbl = new System.Windows.Forms.Label() { Text = labelText, AutoSize = true, Location = new Point(10, y + 3) };
            tb.Location = new Point(120, y);
            tb.Width = 96;
            var helpLbl = new System.Windows.Forms.Label()
            {
                Text = "?",
                AutoSize = false,
                Size = new Size(18, 18),
                Location = new Point(222, y + 2),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Gainsboro,
                Cursor = Cursors.Help,
                Font = new Font(Font.FontFamily, 7.5f, FontStyle.Bold)
            };
            propHelpTip.SetToolTip(helpLbl, helpText);
            helpLbl.Click += (s, ev) => AppMessageBox.Show(helpText, labelText.TrimEnd(':'), MessageBoxButtons.OK, MessageBoxIcon.Information);
            parent.Controls.Add(lbl);
            parent.Controls.Add(tb);
            parent.Controls.Add(helpLbl);
        }

        private void HidePropertiesPanel()
        {
            pnlProperties.Visible = false;
            _selectedNode = null;
            RefreshPlotAndViewLayouts();
        }

        // scup (nested inside sc1) is a SplitContainer whose SplitterDistance
        // governs Panel1's width - Panel1 holds tc1, i.e. every tab including
        // all six analysis plots. Once scup's own available width drops below
        // its current SplitterDistance, WinForms' SplitContainer has a
        // long-standing layout glitch where Panel1's content doesn't get
        // properly re-bounded to the squeezed size - that's what caused the
        // "cropped" plots. This only ever clamps DOWNWARD when the current
        // distance genuinely no longer fits - it must never unconditionally
        // force a specific "preferred" value, since that would fight the
        // designer's/user's actual width (which is exactly what an earlier
        // version of this fix did, snapping the editor back to 520px on every
        // resize even when the window had plenty of room to spare).
        private const int MinQuadViewWidth = 150;

        private void ClampScupSplitter()
        {
            if (scup is null || scup.Width <= 0)
                return;
            int maxAllowed = scup.Width - MinQuadViewWidth - scup.SplitterWidth;
            int minAllowed = scup.Panel1MinSize;
            if (maxAllowed < minAllowed)
                return;   // window too narrow to do anything sane - leave it
            if (scup.SplitterDistance > maxAllowed)
            {
                scup.SplitterDistance = maxAllowed;
            }
        }

        // Forces every custom-drawn canvas (main geometry views + all six analysis
        // plots) to recompute its layout and re-render. Needed whenever the
        // Structure Tree or Properties panel (both Dock=Left/Right siblings of
        // sc1 on the form) toggle Visible and shrink/grow the remaining client
        // area.
        // 
        // The render is deferred TWO message-loop hops, not one: the first hop
        // lets the panel's own Visible-triggered resize settle, but calling
        // ClampScupSplitter() itself changes scup.SplitterDistance, which kicks
        // off its OWN resize cascade (scup -> tc1 -> active TabPage -> its
        // TableLayoutPanel -> the PictureBox's native HWND) that does not finish
        // within that same callback. Rendering immediately after the clamp
        // consistently read the PictureBox's pre-clamp size, which is why the
        // plot was reliably (not just occasionally) smaller than its box. The
        // second hop waits for that second cascade to finish too.
        private void RefreshPlotAndViewLayouts()
        {
            ClampScupSplitter();
            sc1.PerformLayout();
            BeginInvoke(() =>
                {
                    ClampScupSplitter();
                    sc1.PerformLayout();
                    BeginInvoke(() =>
     {
                        drawAxes();
                        RenderTrefftzPlot();
                        RenderLoadsPlot();
                        RenderPolarPlot();
                        RenderFEPlot();
                        RenderModesPlot();
                    });
                });
        }

        // Scans forward from a section's data line for its NACA/AIRFOIL block.
        // Airfoil identity isn't captured by the Section struct/findPoints parser
        // at all, so this reads directly off the raw lines instead.
        private Tuple<string, string, int> FindAirfoilForSection(string[] lines, int sectionLineNumber)
        {
            int limit = Math.Min(lines.Length - 1, sectionLineNumber + 15);
            for (int i = sectionLineNumber + 1, loopTo = limit - 1; i <= loopTo; i++)
            {
                string t = lines[i].Trim().ToLowerInvariant();
                if (t == "naca")
                    return Tuple.Create("NACA", lines[i + 1].Trim(), i + 1);
                if (t == "airfoil")
                    return Tuple.Create("AIRFOIL", lines[i + 1].Trim(), i + 1);
                if (t == "section" || t == "surface" || t == "control" || t.StartsWith("!end"))
                    break;
            }
            return null;
        }

        private void ShowNodeProperties(Node node)
        {
            if (string.IsNullOrEmpty(projectName))
                return;

            // Only re-render everything if the panel is actually about to become
            // visible for the first time (i.e. the available plot area is about
            // to shrink) - re-selecting a different node while it's already open
            // doesn't change any layout, so skip the refresh in that case.
            bool wasPropertiesPanelVisible = pnlProperties.Visible;

            if (node.type == Node.NodeType.Mass)
            {
                string mf = Path.Combine(Application.StartupPath, $"{projectName}.mass");
                if (!File.Exists(mf))
                    return;
                string[] mlines = File.ReadAllLines(mf);
                int mln = node.lineNumber;
                if (mln < 0 || mln >= mlines.Length)
                    return;

                var mnumeric = new List<string>();
                foreach (var v in mlines[mln].Split(' '))
                {
                    if (Information.IsNumeric(v))
                        mnumeric.Add(v);
                }
                if (mnumeric.Count == 0)
                    return;

                _selectedNode = node;
                string header = "Mass Point";
                int cIdx = mlines[mln].IndexOf('!');
                if (cIdx >= 0)
                {
                    string c = mlines[mln].Substring(cIdx + 1).Trim();
                    if (!string.IsNullOrEmpty(c))
                        header += " — " + c;
                }
                lblPropHeader.Text = header;
                pnlSectionFields.Visible = false;
                pnlControlFields.Visible = false;
                pnlMassFields.Visible = true;
                pnlProperties.Visible = true;

                txtPropMassVal.Text = mnumeric.Count > 0 ? mnumeric[0] : "0";
                txtPropMassX.Text = mnumeric.Count > 1 ? mnumeric[1] : "0";
                txtPropMassY.Text = mnumeric.Count > 2 ? mnumeric[2] : "0";
                txtPropMassZ.Text = mnumeric.Count > 3 ? mnumeric[3] : "0";
                txtPropIxx.Text = mnumeric.Count > 4 ? mnumeric[4] : "0";
                txtPropIyy.Text = mnumeric.Count > 5 ? mnumeric[5] : "0";
                txtPropIzz.Text = mnumeric.Count > 6 ? mnumeric[6] : "0";
                if (!wasPropertiesPanelVisible)
                    RefreshPlotAndViewLayouts();
                return;
            }

            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            if (!File.Exists(f))
                return;
            string[] lines = TrimAll(File.ReadAllText(f)).Replace(Constants.vbLf, "").Split(Conversions.ToChar(Constants.vbCrLf));

            if (node.SubType == Node.NodeSubType.LeadingEdge || node.SubType == Node.NodeSubType.TrailingEdge)
            {
                int ln = node.lineNumber;
                if (ln < 0 || ln >= lines.Length)
                    return;

                var numeric = new List<string>();
                foreach (var v in lines[ln].Split(' '))
                {
                    if (Information.IsNumeric(v))
                        numeric.Add(v);
                }

                _selectedNode = node;
                string surfaceLabel = node.Surface;
                if (surfaceLabel.EndsWith("_dup"))
                    surfaceLabel = surfaceLabel.Substring(0, surfaceLabel.Length - 4);
                lblPropHeader.Text = "Section — " + surfaceLabel + (node.IsDuplicate ? " (mirrored)" : "");
                pnlSectionFields.Visible = true;
                pnlControlFields.Visible = false;
                pnlMassFields.Visible = false;
                pnlProperties.Visible = true;

                txtPropXle.Text = numeric.Count > 0 ? numeric[0] : "0";
                txtPropYle.Text = numeric.Count > 1 ? numeric[1] : "0";
                txtPropZle.Text = numeric.Count > 2 ? numeric[2] : "0";
                txtPropChord.Text = numeric.Count > 3 ? numeric[3] : "0";
                txtPropAinc.Text = numeric.Count > 4 ? numeric[4] : "0";

                var af = FindAirfoilForSection(lines, ln);
                if (af is not null)
                {
                    lblPropAirfoilKind.Text = af.Item1 + ":";
                    txtPropAirfoil.Text = af.Item2;
                    txtPropAirfoil.Enabled = true;
                }
                else
                {
                    lblPropAirfoilKind.Text = "Airfoil:";
                    txtPropAirfoil.Text = "(none found)";
                    txtPropAirfoil.Enabled = false;
                }
            }

            else if (node.SubType == Node.NodeSubType.ControlHinge)
            {
                int ln = node.controlLineNumber;
                if (ln < 0 || ln >= lines.Length)
                    return;

                var tokens = new List<string>();
                foreach (var v in lines[ln].Split(' '))
                {
                    if (v.Trim().Length > 0)
                        tokens.Add(v.Trim());
                }
                if (tokens.Count == 0)
                    return;

                _selectedNode = node;
                lblPropHeader.Text = "Control — " + tokens[0] + (node.IsDuplicate ? " (mirrored)" : "");
                pnlSectionFields.Visible = false;
                pnlControlFields.Visible = true;
                pnlMassFields.Visible = false;
                pnlProperties.Visible = true;

                txtPropCname.Text = tokens[0];
                txtPropCgain.Text = tokens.Count > 1 ? tokens[1] : "1.0";
                txtPropXhinge.Text = tokens.Count > 2 ? tokens[2] : "0.5";
                txtPropHx.Text = tokens.Count > 3 ? tokens[3] : "0";
                txtPropHy.Text = tokens.Count > 4 ? tokens[4] : "0";
                txtPropHz.Text = tokens.Count > 5 ? tokens[5] : "0";
                txtPropSgnDup.Text = tokens.Count > 6 ? tokens[6] : "1.0";
            }

            if (!wasPropertiesPanelVisible)
                RefreshPlotAndViewLayouts();
        }

        private void btnPropApply_Click(object sender, EventArgs e)
        {
            if (_selectedNode is null || string.IsNullOrEmpty(projectName))
                return;
            var node = _selectedNode;

            if (node.type == Node.NodeType.Mass)
            {
                ApplyMassProperties(node);
                return;
            }

            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            if (!File.Exists(f))
                return;

            string[] lines = TrimAll(File.ReadAllText(f)).Replace(Constants.vbLf, "").Split(Conversions.ToChar(Constants.vbCrLf));
            var changedLines = new List<int>();

            try
            {
                if (node.SubType == Node.NodeSubType.LeadingEdge || node.SubType == Node.NodeSubType.TrailingEdge)
                {
                    int ln = node.lineNumber;
                    if (ln < 0 || ln >= lines.Length)
                        return;

                    double xle = Conversions.ToDouble(txtPropXle.Text);
                    double yle = Conversions.ToDouble(txtPropYle.Text);
                    double zle = Conversions.ToDouble(txtPropZle.Text);
                    double chord = Conversions.ToDouble(txtPropChord.Text);
                    double ainc = Conversions.ToDouble(txtPropAinc.Text);
                    if (chord < 0.001d)
                    {
                        AppMessageBox.Show("Chord must be positive.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Preserve any existing Nspanwise/Sspace override columns (6th/7th) instead of dropping them
                    var origNumeric = new List<string>();
                    foreach (var v in lines[ln].Split(' '))
                    {
                        if (Information.IsNumeric(v))
                            origNumeric.Add(v);
                    }
                    string nspanwise = origNumeric.Count > 5 ? origNumeric[5] : "0";
                    string sspace = origNumeric.Count > 6 ? origNumeric[6] : "0";

                    string newLine = string.Join(" ", new string[] { xle.ToString("G6"), yle.ToString("G6"), zle.ToString("G6"), chord.ToString("G6"), ainc.ToString("G6"), nspanwise, sspace });
                    string leadingWS = GetLeadingWhitespace(ln < txt3.LinesCount ? txt3.Lines[ln] : lines[ln]);
                    lines[ln] = leadingWS + FormatLineText(newLine).TrimStart();
                    changedLines.Add(ln);

                    if (txtPropAirfoil.Enabled)
                    {
                        var af = FindAirfoilForSection(lines, ln);
                        if (af is not null)
                        {
                            int afLn = af.Item3;
                            string afLeadingWS = GetLeadingWhitespace(afLn < txt3.LinesCount ? txt3.Lines[afLn] : lines[afLn]);
                            lines[afLn] = afLeadingWS + txtPropAirfoil.Text.Trim();
                            changedLines.Add(afLn);
                        }
                    }
                }

                else if (node.SubType == Node.NodeSubType.ControlHinge)
                {
                    int ln = node.controlLineNumber;
                    if (ln < 0 || ln >= lines.Length)
                        return;

                    string cname = txtPropCname.Text.Trim();
                    if (string.IsNullOrEmpty(cname))
                    {
                        AppMessageBox.Show("Control name cannot be empty.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    double cgain = Conversions.ToDouble(txtPropCgain.Text);
                    double xhinge = Conversions.ToDouble(txtPropXhinge.Text);
                    double hx = Conversions.ToDouble(txtPropHx.Text);
                    double hy = Conversions.ToDouble(txtPropHy.Text);
                    double hz = Conversions.ToDouble(txtPropHz.Text);
                    double sgndup = Conversions.ToDouble(txtPropSgnDup.Text);

                    string newLine = string.Join(" ", new string[] { cname, cgain.ToString("G6"), xhinge.ToString("G6"), hx.ToString("G6"), hy.ToString("G6"), hz.ToString("G6"), sgndup.ToString("G6") });
                    string leadingWS = GetLeadingWhitespace(ln < txt3.LinesCount ? txt3.Lines[ln] : lines[ln]);
                    lines[ln] = leadingWS + FormatLineText(newLine).TrimStart();
                    changedLines.Add(ln);
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Please enter valid numeric values for all fields." + Environment.NewLine + ex.Message, "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string newContent = string.Join(Constants.vbCrLf, lines);
            File.WriteAllText(f, newContent);

            if (tc1.SelectedTab is not null && tc1.SelectedTab.Name == "Geometry")
            {
                updating = true;
                foreach (var cl in changedLines)
                    ReplaceEditorLine(cl, lines[cl]);
                updating = false;
                FormatActiveText();
            }

            findPoints();
            drawAxes();

            // Re-select the refreshed node instance (same line/SubType/mirror-side) so
            // the panel keeps reflecting what's now actually on disk.
            Node reselect = null;
            foreach (Node p in points)
            {
                if (p.type == Node.NodeType.Geometry && p.IsDuplicate == node.IsDuplicate && p.SubType == node.SubType)
                {
                    if (node.SubType == Node.NodeSubType.ControlHinge)
                    {
                        if (p.controlLineNumber == node.controlLineNumber)
                        {
                            reselect = p;
                            break;
                        }
                    }
                    else if (p.lineNumber == node.lineNumber)
                    {
                        reselect = p;
                        break;
                    }
                }
            }
            if (reselect is not null)
                ShowNodeProperties(reselect);
        }

        private void ApplyMassProperties(Node node)
        {
            string f = Path.Combine(Application.StartupPath, $"{projectName}.mass");
            if (!File.Exists(f))
                return;

            string[] lines = File.ReadAllLines(f);
            int ln = node.lineNumber;
            if (ln < 0 || ln >= lines.Length)
                return;

            try
            {
                double massVal = Conversions.ToDouble(txtPropMassVal.Text);
                double mx = Conversions.ToDouble(txtPropMassX.Text);
                double my = Conversions.ToDouble(txtPropMassY.Text);
                double mz = Conversions.ToDouble(txtPropMassZ.Text);
                double ixx = Conversions.ToDouble(txtPropIxx.Text);
                double iyy = Conversions.ToDouble(txtPropIyy.Text);
                double izz = Conversions.ToDouble(txtPropIzz.Text);
                if (massVal <= 0d)
                {
                    AppMessageBox.Show("Mass must be positive.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Preserve any trailing "!comment" text (e.g. "!wing structure") instead of dropping it
                string origLine = lines[ln];
                int commentIdx = origLine.IndexOf('!');
                string comment = commentIdx >= 0 ? "   " + origLine.Substring(commentIdx) : "";

                string newLine = string.Join(" ", new string[] { massVal.ToString("G6"), mx.ToString("G6"), my.ToString("G6"), mz.ToString("G6"), ixx.ToString("G6"), iyy.ToString("G6"), izz.ToString("G6") });
                string leadingWS = GetLeadingWhitespace(ln < txt3.LinesCount ? txt3.Lines[ln] : origLine);
                lines[ln] = leadingWS + FormatLineText(newLine).TrimStart() + comment;
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Please enter valid numeric values for all fields." + Environment.NewLine + ex.Message, "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string newContent = string.Join(Constants.vbCrLf, lines);
            File.WriteAllText(f, newContent);

            if (tc1.SelectedTab is not null && tc1.SelectedTab.Name == "Mass")
            {
                updating = true;
                ReplaceEditorLine(ln, lines[ln]);
                updating = false;
                FormatActiveText();
            }

            findPoints();
            drawAxes();

            Node reselect = null;
            foreach (Node p in points)
            {
                if (p.type == Node.NodeType.Mass && p.lineNumber == node.lineNumber)
                {
                    reselect = p;
                    break;
                }
            }
            if (reselect is not null)
                ShowNodeProperties(reselect);
        }

        // ===================== Structure tree =====================

        private void InitializeStructureTreePanel()
        {
            if (pnlStructureTree is not null)
                return;

            pnlStructureTree = new System.Windows.Forms.Panel();
            pnlStructureTree.Dock = DockStyle.Left;
            pnlStructureTree.Width = 240;
            pnlStructureTree.BackColor = Color.White;
            pnlStructureTree.BorderStyle = BorderStyle.FixedSingle;
            pnlStructureTree.Visible = false;
            Controls.Add(pnlStructureTree);

            // A single 2-ROW TableLayoutPanel (toolbar row, tree row) instead of
            // two Dock=Top/Dock=Fill SIBLINGS. Sibling docking depends on z-order
            // resolution between the two controls, which is where the earlier
            // overlap kept coming from; a table's row heights are computed
            // directly with no ordering ambiguity, so this can't overlap.
            var outer = new TableLayoutPanel();
            outer.Dock = DockStyle.Fill;
            outer.ColumnCount = 1;
            outer.RowCount = 2;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 76.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
            pnlStructureTree.Controls.Add(outer);

            // 2x2 grid of Add/Delete buttons, filling row 0 of `outer`.
            var toolbar = new TableLayoutPanel();
            toolbar.Dock = DockStyle.Fill;
            toolbar.BackColor = Color.WhiteSmoke;
            toolbar.ColumnCount = 2;
            toolbar.RowCount = 2;
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50.0f));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50.0f));
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 50.0f));
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 50.0f));

            var structTip = new System.Windows.Forms.ToolTip();

            var btnAddSurface = new System.Windows.Forms.Button() { Text = "+ Surface", Dock = DockStyle.Fill, Margin = new Padding(4), FlatStyle = FlatStyle.Flat, BackColor = Color.White, Cursor = Cursors.Hand };
            var btnAddSection = new System.Windows.Forms.Button() { Text = "+ Section", Dock = DockStyle.Fill, Margin = new Padding(4), FlatStyle = FlatStyle.Flat, BackColor = Color.White, Cursor = Cursors.Hand };
            var btnAddControl = new System.Windows.Forms.Button() { Text = "+ Control", Dock = DockStyle.Fill, Margin = new Padding(4), FlatStyle = FlatStyle.Flat, BackColor = Color.White, Cursor = Cursors.Hand };
            var btnDeleteBlock = new System.Windows.Forms.Button() { Text = "Delete", Dock = DockStyle.Fill, Margin = new Padding(4), FlatStyle = FlatStyle.Flat, BackColor = Color.White, Cursor = Cursors.Hand };
            structTip.SetToolTip(btnAddSurface, "Add a Surface after the selected one (or at the end)");
            structTip.SetToolTip(btnAddSection, "Add a Section after the selected one (or as the surface's last section)");
            structTip.SetToolTip(btnAddControl, "Add a Control after the selected one (or as the section's last control)");
            structTip.SetToolTip(btnDeleteBlock, "Delete the selected block");

            btnAddSurface.Click += AddSurface_Click;
            btnAddSection.Click += AddSection_Click;
            btnAddControl.Click += AddControl_Click;
            btnDeleteBlock.Click += DeleteBlock_Click;

            toolbar.Controls.Add(btnAddSurface, 0, 0);
            toolbar.Controls.Add(btnAddSection, 1, 0);
            toolbar.Controls.Add(btnAddControl, 0, 1);
            toolbar.Controls.Add(btnDeleteBlock, 1, 1);
            outer.Controls.Add(toolbar, 0, 0);

            tvStructure = new System.Windows.Forms.TreeView();
            tvStructure.Dock = DockStyle.Fill;
            tvStructure.AllowDrop = true;
            tvStructure.HideSelection = false;
            tvStructure.AfterSelect += tvStructure_AfterSelect;
            tvStructure.NodeMouseDoubleClick += tvStructure_NodeMouseDoubleClick;
            tvStructure.ItemDrag += tvStructure_ItemDrag;
            tvStructure.DragEnter += tvStructure_DragEnter;
            tvStructure.DragOver += tvStructure_DragOver;
            tvStructure.DragDrop += tvStructure_DragDrop;
            outer.Controls.Add(tvStructure, 0, 1);

            btnToggleStructureTree.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnToggleStructureTree.Text = "Structure Tree";
            btnToggleStructureTree.Click += ToggleStructureTree_Click;
            int insertAt = ToolStrip2.Items.IndexOf(btnHover);
            if (insertAt >= 0)
            {
                ToolStrip2.Items.Insert(insertAt + 1, btnToggleStructureTree);
            }
            else
            {
                ToolStrip2.Items.Add(btnToggleStructureTree);
            }
        }

        private void ToggleStructureTree_Click(object sender, EventArgs e)
        {
            pnlStructureTree.Visible = !pnlStructureTree.Visible;
            if (pnlStructureTree.Visible)
            {
                // The panel (and its docked toolbar/tree children) was built while
                // Visible=False; WinForms can skip laying out an invisible subtree,
                // so force a fresh layout pass now that it's actually on screen -
                // otherwise the toolbar's reserved height can be stale/zero and the
                // buttons appear to sit on top of the tree.
                pnlStructureTree.PerformLayout();
                RefreshStructureTree();
            }
            // Either direction changes how much width is left for sc1/the plots.
            RefreshPlotAndViewLayouts();
        }

        private void InitializeValidationPanel()
        {
            if (pnlValidation is not null)
                return;

            pnlValidation = new System.Windows.Forms.Panel();
            pnlValidation.Dock = DockStyle.Bottom;
            pnlValidation.Height = 190;
            pnlValidation.BackColor = Color.White;
            pnlValidation.BorderStyle = BorderStyle.FixedSingle;
            pnlValidation.Visible = false;
            Controls.Add(pnlValidation);

            // Same table-not-siblings pattern as pnlStructureTree (header/list/detail rows
            // with explicit row heights), which avoided a Dock=Top/Fill overlap bug there.
            var outer = new TableLayoutPanel();
            outer.Dock = DockStyle.Fill;
            outer.ColumnCount = 1;
            outer.RowCount = 3;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 28.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 48.0f));
            pnlValidation.Controls.Add(outer);

            var header = new System.Windows.Forms.Panel() { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };
            lblValidationSummary = new System.Windows.Forms.Label()
            {
                Text = "🧞 Validation Results",
                AutoSize = false,
                Dock = DockStyle.Left,
                Width = 600,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(8, 0, 0, 0)
            };
            var btnCloseValidation = new System.Windows.Forms.Button() { Text = "x", Dock = DockStyle.Right, Width = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.White, Cursor = Cursors.Hand };
            btnCloseValidation.Click += (s, ev) => HideValidationPanel();
            header.Controls.Add(lblValidationSummary);
            header.Controls.Add(btnCloseValidation);
            outer.Controls.Add(header, 0, 0);

            lvValidation = new System.Windows.Forms.ListView();
            lvValidation.Dock = DockStyle.Fill;
            lvValidation.View = View.Details;
            lvValidation.FullRowSelect = true;
            lvValidation.GridLines = true;
            lvValidation.MultiSelect = false;
            lvValidation.HideSelection = false;
            lvValidation.Columns.Add("", 28);
            lvValidation.Columns.Add("Line", 55);
            lvValidation.Columns.Add("Issue", 600);
            lvValidation.SelectedIndexChanged += lvValidation_SelectedIndexChanged;
            lvValidation.Resize += (s, ev) => { if (lvValidation.Columns.Count == 3) { lvValidation.Columns[2].Width = Math.Max(200, lvValidation.ClientSize.Width - lvValidation.Columns[0].Width - lvValidation.Columns[1].Width - 4); } };
            outer.Controls.Add(lvValidation, 0, 1);

            txtFixHint = new System.Windows.Forms.TextBox()
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(255, 252, 235),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Select an issue above to see how to fix it."
            };
            outer.Controls.Add(txtFixHint, 0, 2);
        }

        private void HideValidationPanel()
        {
            pnlValidation.Visible = false;
            ClearValidationHighlights();
            RefreshPlotAndViewLayouts();
        }

        private void ClearValidationHighlights()
        {
            if (txt3 is null)
                return;
            for (int li = 0, loopTo = txt3.LinesCount - 1; li <= loopTo; li++)
                txt3[li].BackgroundBrush = null;
            txt3.Invalidate();
        }

        private string ValidationIconFor(IssueSeverity sev)
        {
            switch (sev)
            {
                case IssueSeverity.Error:
                    {
                        return "🔴";
                    }
                case IssueSeverity.Warning:
                    {
                        return "🟠";
                    }

                default:
                    {
                        return "🔵";
                    }
            }
        }

        private Color ValidationColorFor(IssueSeverity sev)
        {
            switch (sev)
            {
                case IssueSeverity.Error:
                    {
                        return Color.FromArgb(90, 220, 53, 69);
                    }
                case IssueSeverity.Warning:
                    {
                        return Color.FromArgb(90, 255, 193, 7);
                    }

                default:
                    {
                        return Color.FromArgb(60, 13, 110, 253);
                    }
            }
        }

        /// <summary>
    /// Runs FileValidator (FileValidator.vb) against whichever text-editor tab is currently
    /// active (Geometry/Mass/Run all share the one txt3 control), lists the findings with
    /// fix suggestions, and colors each offending line's background by severity.
    /// </summary>
        private void btnValidate_Click(object sender, EventArgs e)
        {
            string tabName = tc1.SelectedTab is not null ? tc1.SelectedTab.Name : "";
            if (tabName != "Geometry" && tabName != "Mass" && tabName != "Run")
                return;

            var issues = FileValidator.ValidateFile(txt3.Text, tabName, Application.StartupPath);

            ClearValidationHighlights();
            lvValidation.Items.Clear();

            // List(Of T).Count is an instance property, which shadows the LINQ Count(predicate)
            // extension method for this type - Where(...).Count() sidesteps the collision.
            int errorCount = issues.Where(x => x.Severity == IssueSeverity.Error).Count();
            int warnCount = issues.Where(x => x.Severity == IssueSeverity.Warning).Count();
            var ordered = issues.OrderBy(x => x.Severity).ThenBy(x => x.LineNumber == 0 ? int.MaxValue : x.LineNumber).ToList();

            if (ordered.Count == 0)
            {
                string[] okCols = new[] { "✅", "", "No issues found - this file looks well-formed." };
                lvValidation.Items.Add(new System.Windows.Forms.ListViewItem(okCols));
            }
            else
            {
                if (errorCount == 0 && warnCount == 0)
                {
                    // Only Info-level findings (e.g. a block-count summary) - make it explicit that
                    // the file WAS checked and came back clean, rather than the list just looking sparse.
                    string[] cleanCols = new[] { "✅", "", "No errors or warnings found." };
                    lvValidation.Items.Add(new System.Windows.Forms.ListViewItem(cleanCols));
                }
                foreach (var issue in ordered)
                {
                    string[] cols = new[] { ValidationIconFor(issue.Severity), issue.LineNumber > 0 ? issue.LineNumber.ToString() : "", issue.Message };
                    var lvi = new System.Windows.Forms.ListViewItem(cols);
                    lvi.Tag = issue;
                    lvValidation.Items.Add(lvi);

                    if (issue.LineNumber >= 1 && issue.LineNumber <= txt3.LinesCount)
                    {
                        txt3[issue.LineNumber - 1].BackgroundBrush = new SolidBrush(ValidationColorFor(issue.Severity));
                    }
                }
            }

            lblValidationSummary.Text = $"🧞 Validation Results — {tabName}: {errorCount} error(s), {warnCount} warning(s)";
            txtFixHint.Text = "Select an issue above to see how to fix it.";
            txt3.Invalidate();

            pnlValidation.Visible = true;
            pnlValidation.PerformLayout();
            RefreshPlotAndViewLayouts();
        }

        private void lvValidation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvValidation.SelectedItems.Count == 0)
                return;
            ValidationIssue issue = lvValidation.SelectedItems[0].Tag as ValidationIssue;
            if (issue is null)
                return;
            txtFixHint.Text = string.IsNullOrEmpty(issue.FixHint) ? "(no fix suggestion available)" : "💡 " + issue.FixHint;
            if (issue.LineNumber >= 1)
            {
                SelectEditorLineRange(issue.LineNumber - 1, issue.LineNumber - 1);
            }
        }

        private void InitializeThemeToggle()
        {
            btnToggleTheme.DisplayStyle = ToolStripItemDisplayStyle.Text;
            UpdateThemeToggleButton();
            btnToggleTheme.Click += ToggleTheme_Click;
            int insertAt = ToolStrip2.Items.IndexOf(btnToggleStructureTree);
            if (insertAt >= 0)
            {
                ToolStrip2.Items.Insert(insertAt + 1, btnToggleTheme);
            }
            else
            {
                ToolStrip2.Items.Add(btnToggleTheme);
            }
        }

        private void UpdateThemeToggleButton()
        {
            btnToggleTheme.Text = IsDarkTheme ? "Theme: Dark" : "Theme: Light";
            btnToggleTheme.BackColor = IsDarkTheme ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220);
            btnToggleTheme.ForeColor = IsDarkTheme ? Color.White : Color.Black;
        }

        // Flips the theme, persists it, and re-renders every custom-drawn canvas
        // plus the Derivatives tab's text panes so the switch is immediate and
        // consistent across the whole form rather than only affecting whatever's
        // redrawn next.
        private void ToggleTheme_Click(object sender, EventArgs e)
        {
            IsDarkTheme = !IsDarkTheme;
            My.MySettingsProperty.Settings.DarkTheme = IsDarkTheme;
            My.MySettingsProperty.Settings.Save();
            UpdateThemeToggleButton();
            RefreshThemeColors();

            drawAxes();   // pxy/pxz/pyz/p3d (HeavyRender)
            RenderTrefftzPlot();
            RenderLoadsPlot();
            RenderPolarPlot();
            RenderFEPlot();
            RenderModesPlot();
            RefreshDerivativesTheme();
        }

        // Reads the current node under a tree click and jumps the editor to it,
        // highlighting its whole line range directly by index. Deliberately does
        // NOT use the existing selectText() helper here - that one finds a line by
        // searching the document text for a match, which breaks for Control blocks
        // since every Control's separator/keyword lines are textually identical
        // (IndexOf always jumps to the first one in the file).
        private void tvStructure_AfterSelect(object sender, TreeViewEventArgs e)
        {
            GeomBlock blk = e.Node.Tag as GeomBlock;
            if (blk is null || string.IsNullOrEmpty(projectName))
                return;
            tc1.SelectedIndex = 0;   // Geometry tab
            SelectEditorLineRange(blk.StartLine, blk.EndLine);
        }

        private void SelectEditorLineRange(int startLine, int endLine)
        {
            if (txt3 is null || txt3.LinesCount == 0)
                return;
            if (startLine < 0 || startLine >= txt3.LinesCount)
                return;
            int clampedEnd = Math.Min(Math.Max(endLine, startLine), txt3.LinesCount - 1);
            txt3.Selection.Start = new Place(0, startLine);
            txt3.Selection.End = new Place(txt3.Lines[clampedEnd].Length, clampedEnd);
            txt3.Invalidate();
            // DoCaretVisible() only guarantees the caret (selection end) is on
            // screen, which can leave a multi-line block's start scrolled out of
            // view. DoRangeVisible scrolls to fit as much of the whole range as
            // the viewport allows, starting from its top.
            txt3.DoRangeVisible(txt3.Selection, true);
        }

        // Double-click a Section or Control node to open it in the properties
        // panel (same panel a canvas click opens), in addition to the single-click
        // navigation above. Matches the tree block to its corresponding Node in
        // `points` via DataLine, since that's what the properties panel operates on.
        private void tvStructure_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            GeomBlock blk = e.Node.Tag as GeomBlock;
            if (blk is null || blk.DataLine < 0)
                return;
            if (blk.Kind != "Section" && blk.Kind != "Control")
                return;
            if (string.IsNullOrEmpty(projectName))
                return;

            tc1.SelectedIndex = 0;   // Geometry tab
            SelectEditorLineRange(blk.StartLine, blk.EndLine);
            findPoints();   // ensure `points` reflects the now-active Geometry tab's live text

            Node matched = null;
            foreach (Node p in points)
            {
                if (p.type != Node.NodeType.Geometry || p.IsDuplicate)
                    continue;
                if (blk.Kind == "Section")
                {
                    if (p.SubType == Node.NodeSubType.LeadingEdge && p.lineNumber == blk.DataLine)
                    {
                        matched = p;
                        break;
                    }
                }
                else if (p.SubType == Node.NodeSubType.ControlHinge && p.controlLineNumber == blk.DataLine)
                {
                    matched = p;
                    break;
                }
            }
            if (matched is not null)
                ShowNodeProperties(matched);
        }

        private void tvStructure_ItemDrag(object sender, ItemDragEventArgs e)
        {
            tvStructure.DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void tvStructure_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void tvStructure_DragOver(object sender, DragEventArgs e)
        {
            var pt = tvStructure.PointToClient(new Point(e.X, e.Y));
            var targetNode = tvStructure.GetNodeAt(pt);
            TreeNode draggedNode = e.Data.GetData(typeof(TreeNode)) as TreeNode;
            if (targetNode is not null)
                tvStructure.SelectedNode = targetNode;
            e.Effect = targetNode is not null && draggedNode is not null && !ReferenceEquals(draggedNode, targetNode) && IsValidDropTarget(draggedNode, targetNode) ? DragDropEffects.Move : DragDropEffects.None;
        }

        private void tvStructure_DragDrop(object sender, DragEventArgs e)
        {
            var pt = tvStructure.PointToClient(new Point(e.X, e.Y));
            var targetNode = tvStructure.GetNodeAt(pt);
            TreeNode draggedNode = e.Data.GetData(typeof(TreeNode)) as TreeNode;
            if (draggedNode is null || targetNode is null || ReferenceEquals(draggedNode, targetNode))
                return;
            if (!IsValidDropTarget(draggedNode, targetNode))
                return;
            MoveBlock(draggedNode, targetNode);
        }

        // Same-kind drops reorder as siblings (dropped node goes right after the
        // target). Cross-kind drops append into a container (Section -> Surface,
        // Control -> Section). Surface can only reorder against Surface.
        private bool IsValidDropTarget(TreeNode dragged, TreeNode target)
        {
            GeomBlock dblk = dragged.Tag as GeomBlock;
            GeomBlock tblk = target.Tag as GeomBlock;
            if (dblk is null || tblk is null)
                return false;
            if (IsAncestor(dragged, target))
                return false;
            switch (dblk.Kind ?? "")
            {
                case "Surface":
                    {
                        return tblk.Kind == "Surface";
                    }
                case "Section":
                    {
                        return tblk.Kind == "Section" || tblk.Kind == "Surface";
                    }
                case "Control":
                    {
                        return tblk.Kind == "Control" || tblk.Kind == "Section";
                    }
            }
            return false;
        }

        private bool IsAncestor(TreeNode possibleAncestor, TreeNode node)
        {
            var p = node.Parent;
            while (p is not null)
            {
                if (ReferenceEquals(p, possibleAncestor))
                    return true;
                p = p.Parent;
            }
            return false;
        }

        // Cuts the dragged block's exact text range out of the file and reinserts
        // it next to the drop target, adjusting for the index shift caused by the
        // removal when the target sits later in the file than the dragged block.
        private void MoveBlock(TreeNode draggedNode, TreeNode targetNode)
        {
            if (string.IsNullOrEmpty(projectName))
                return;
            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            if (!File.Exists(f))
                return;
            var lines = new List<string>(TrimAll(File.ReadAllText(f)).Replace(Constants.vbLf, "").Split(Conversions.ToChar(Constants.vbCrLf)));

            GeomBlock dblk = (GeomBlock)draggedNode.Tag;
            GeomBlock tblk = (GeomBlock)targetNode.Tag;

            var blockLines = lines.GetRange(dblk.StartLine, dblk.EndLine - dblk.StartLine + 1);
            lines.RemoveRange(dblk.StartLine, dblk.EndLine - dblk.StartLine + 1);

            int insertIndex;
            if ((dblk.Kind ?? "") == (tblk.Kind ?? ""))
            {
                // Sibling reorder: right after the target block's own closing line.
                insertIndex = tblk.EndLine + 1;
            }
            else if (dblk.Kind == "Section" && tblk.Kind == "Surface")
            {
                // Sections are nested inside !beginsurface/!endsurface - insert before !endsurface.
                insertIndex = tblk.EndLine;
            }
            else if (dblk.Kind == "Control" && tblk.Kind == "Section")
            {
                // Controls trail !endsection as siblings, not nested inside it - append
                // after the section's last existing control (or after the section itself).
                if (tblk.Children.Count > 0)
                {
                    insertIndex = tblk.Children[tblk.Children.Count - 1].EndLine + 1;
                }
                else
                {
                    insertIndex = tblk.EndLine + 1;
                }
            }
            else
            {
                return;
            }

            if (dblk.StartLine < insertIndex)
                insertIndex -= blockLines.Count;
            lines.InsertRange(insertIndex, blockLines);

            SaveAndRefreshGeometry(lines);
        }

        private GeomBlock GetSelectedBlock()
        {
            var n = tvStructure.SelectedNode;
            if (n is null)
                return null;
            return n.Tag as GeomBlock;
        }

        // Inserted right after the selected Surface if one is selected; otherwise
        // appended at the end of the geometry (before !endgeometry).
        private void AddSurface_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(projectName))
                return;
            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            if (!File.Exists(f))
                return;
            var lines = new List<string>(TrimAll(File.ReadAllText(f)).Replace(Constants.vbLf, "").Split(Conversions.ToChar(Constants.vbCrLf)));

            var selBlk = GetSelectedBlock();
            int insertIndex;
            if (selBlk is not null && selBlk.Kind == "Surface")
            {
                insertIndex = selBlk.EndLine + 1;
            }
            else
            {
                int endGeomIdx = -1;
                for (int k = 0, loopTo = lines.Count - 1; k <= loopTo; k++)
                {
                    if (lines[k].Trim().ToLowerInvariant() == "!endgeometry")
                    {
                        endGeomIdx = k;
                        break;
                    }
                }
                if (endGeomIdx == -1)
                {
                    AppMessageBox.Show("Could not find the '!endgeometry' marker in this file - cannot add a new surface.", "Add Surface", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                insertIndex = endGeomIdx;
            }

            string[] template = new[] { "#====================================================================", "SURFACE", "[New Surface]", "!beginsurface", "#Nchord  Cspace   Nspan   Sspace", "8            1.0      8           1.0", "#", "ANGLE", "0.0", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc  Nspan  Sspace", "0.0     0.0    0.0     1.0     0.0   0          0", "NACA", "0012", "!endsection", "!endsurface" };
            lines.InsertRange(insertIndex, template);
            SaveAndRefreshGeometry(lines);
        }

        // Inserted right after the selected Section if one is selected; otherwise
        // appended as the last section of the selected/ancestor Surface.
        private void AddSection_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(projectName))
                return;
            var surfBlk = GetSelectedSurfaceBlock();
            if (surfBlk is null)
            {
                AppMessageBox.Show("Select a surface (or one of its sections/controls) first.", "Add Section", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            if (!File.Exists(f))
                return;
            var lines = new List<string>(TrimAll(File.ReadAllText(f)).Replace(Constants.vbLf, "").Split(Conversions.ToChar(Constants.vbCrLf)));

            var selBlk = GetSelectedBlock();
            int insertIndex;
            if (selBlk is not null && selBlk.Kind == "Section")
            {
                insertIndex = selBlk.EndLine + 1;
            }
            else
            {
                insertIndex = surfBlk.EndLine;
            }   // before !endsurface = last section

            string[] template = new[] { "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc  Nspan  Sspace", "0.0     0.0    0.0     1.0     0.0   0          0", "NACA", "0012", "!endsection" };
            lines.InsertRange(insertIndex, template);
            SaveAndRefreshGeometry(lines);
        }

        // Inserted right after the selected Control if one is selected; otherwise
        // appended as the last control of the selected/ancestor Section.
        private void AddControl_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(projectName))
                return;
            var secBlk = GetSelectedSectionBlock();
            if (secBlk is null)
            {
                AppMessageBox.Show("Select a section (or one of its controls) first.", "Add Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            if (!File.Exists(f))
                return;
            var lines = new List<string>(TrimAll(File.ReadAllText(f)).Replace(Constants.vbLf, "").Split(Conversions.ToChar(Constants.vbCrLf)));

            var selBlk = GetSelectedBlock();
            int insertIndex;
            if (selBlk is not null && selBlk.Kind == "Control")
            {
                insertIndex = selBlk.EndLine + 1;
            }
            else if (secBlk.Children.Count > 0)
            {
                insertIndex = secBlk.Children[secBlk.Children.Count - 1].EndLine + 1;
            }
            else
            {
                insertIndex = secBlk.EndLine + 1;
            }

            string[] template = new[] { "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname   Cgain  Xhinge  XYZhvec      SgnDup", "control1 1.0    0.75    0.0 0.0 0.0  1.0", "!endcontrol" };
            lines.InsertRange(insertIndex, template);
            SaveAndRefreshGeometry(lines);
        }

        private void DeleteBlock_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(projectName))
                return;
            var n = tvStructure.SelectedNode;
            if (n is null)
                return;
            GeomBlock blk = n.Tag as GeomBlock;
            if (blk is null)
                return;

            var res = AppMessageBox.Show($"Delete this {blk.Kind.ToLowerInvariant()} ({blk.Label})? This removes its entire text block from the file.", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res == DialogResult.No)
                return;

            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            if (!File.Exists(f))
                return;
            var lines = new List<string>(TrimAll(File.ReadAllText(f)).Replace(Constants.vbLf, "").Split(Conversions.ToChar(Constants.vbCrLf)));
            lines.RemoveRange(blk.StartLine, blk.EndLine - blk.StartLine + 1);
            SaveAndRefreshGeometry(lines);
        }

        private GeomBlock GetSelectedSurfaceBlock()
        {
            var n = tvStructure.SelectedNode;
            if (n is null)
                return null;
            GeomBlock blk = n.Tag as GeomBlock;
            while (blk is not null && blk.Kind != "Surface")
            {
                n = n.Parent;
                blk = n is not null ? n.Tag as GeomBlock : null;
            }
            return blk;
        }

        private GeomBlock GetSelectedSectionBlock()
        {
            var n = tvStructure.SelectedNode;
            if (n is null)
                return null;
            GeomBlock blk = n.Tag as GeomBlock;
            if (blk is not null && blk.Kind == "Control")
            {
                n = n.Parent;
                blk = n is not null ? n.Tag as GeomBlock : null;
            }
            if (blk is not null && blk.Kind == "Section")
                return blk;
            return null;
        }

        // Shared write+refresh tail for every structure-tree mutation (move/add/delete).
        // Replaces txt3's content via a full-selection InsertText (not a raw .Text set)
        // so Ctrl+Z in the editor still undoes it when the Geometry tab is active.
        private void SaveAndRefreshGeometry(List<string> lines)
        {
            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            string newContent = string.Join(Constants.vbCrLf, lines);
            File.WriteAllText(f, newContent);

            if (tc1.SelectedTab is not null && tc1.SelectedTab.Name == "Geometry")
            {
                updating = true;
                txt3.Selection.Start = new Place(0, 0);
                txt3.Selection.End = new Place(txt3.Lines[txt3.LinesCount - 1].Length, txt3.LinesCount - 1);
                txt3.InsertText(newContent);
                updating = false;
                ApplySyntaxHighlighting();
                FormatActiveText();
            }

            findPoints();
            drawAxes();
        }

        // Rebuilds the tree from the current .avl file. Called from findPoints() so
        // it stays live across typing, drag-edits, properties-panel saves, etc. -
        // the same central refresh hook everything else already relies on.
        private void RefreshStructureTree()
        {
            if (pnlStructureTree is null || !pnlStructureTree.Visible)
                return;
            if (string.IsNullOrEmpty(projectName))
                return;

            // Mirror findPoints()'s own convention: read the LIVE editor buffer
            // when the Geometry tab is active (so typed-but-not-yet-saved edits
            // show up immediately), falling back to disk otherwise. Reading from
            // disk unconditionally was why the tree lagged behind the editor.
            string avlText;
            if (tc1.SelectedTab is not null && tc1.SelectedTab.Name == "Geometry" && txt3 is not null)
            {
                avlText = txt3.Text;
            }
            else
            {
                string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
                if (!File.Exists(f))
                    return;
                avlText = File.ReadAllText(f);
            }

            string[] lines = TrimAll(avlText).Replace(Constants.vbLf, "").Split(Conversions.ToChar(Constants.vbCrLf));
            var surfaces = ScanGeometryBlocks(lines);

            var expandedPaths = new HashSet<string>();
            CollectExpandedPaths(tvStructure.Nodes, expandedPaths);

            tvStructure.BeginUpdate();
            tvStructure.Nodes.Clear();
            foreach (var surf in surfaces)
            {
                var surfNode = new TreeNode(surf.Label) { Tag = surf };
                foreach (var sec in surf.Children)
                {
                    var secNode = new TreeNode(sec.Label) { Tag = sec };
                    foreach (var ctrl in sec.Children)
                        secNode.Nodes.Add(new TreeNode(ctrl.Label) { Tag = ctrl });
                    surfNode.Nodes.Add(secNode);
                }
                tvStructure.Nodes.Add(surfNode);
            }

            if (expandedPaths.Count == 0)
            {
                tvStructure.ExpandAll();
            }
            else
            {
                RestoreExpandedPaths(tvStructure.Nodes, expandedPaths);
            }
            tvStructure.EndUpdate();
        }

        private void CollectExpandedPaths(TreeNodeCollection nodes, HashSet<string> @into)
        {
            foreach (TreeNode n in nodes)
            {
                if (n.IsExpanded)
                    into.Add(n.FullPath);
                CollectExpandedPaths(n.Nodes, into);
            }
        }

        private void RestoreExpandedPaths(TreeNodeCollection nodes, HashSet<string> paths)
        {
            foreach (TreeNode n in nodes)
            {
                if (paths.Contains(n.FullPath))
                    n.Expand();
                RestoreExpandedPaths(n.Nodes, paths);
            }
        }

        // Parses the file into a Surface > Section > Control block tree using this
        // app's own !beginX/!endX markers, which unambiguously delimit each block's
        // exact line range - unlike findPoints' keyword-adjacency heuristic, these
        // are reliable enough to cut/paste/delete against directly.
        private List<GeomBlock> ScanGeometryBlocks(string[] lines)
        {
            var surfaces = new List<GeomBlock>();
            int i = 0;
            while (i < lines.Length)
            {
                if (lines[i].Trim().ToLowerInvariant() == "surface")
                {
                    int surfStart = ExtendBackForSeparator(lines, i);
                    string name = i + 1 < lines.Length ? lines[i + 1].Trim().Trim('[', ']') : "Surface";
                    if (string.IsNullOrEmpty(name))
                        name = "Surface";
                    int surfEnd = FindMarker(lines, i, "!endsurface");
                    if (surfEnd == -1)
                        surfEnd = lines.Length - 1;
                    var surf = new GeomBlock() { Kind = "Surface", Label = name, StartLine = surfStart, EndLine = surfEnd };

                    int j = i + 1;
                    GeomBlock lastSection = null;
                    while (j <= surfEnd)
                    {
                        string t = lines[j].Trim().ToLowerInvariant();
                        if (t == "section")
                        {
                            int secStart = ExtendBackForSeparator(lines, j);
                            int secEnd = FindMarker(lines, j, "!endsection");
                            if (secEnd == -1 || secEnd > surfEnd)
                                secEnd = surfEnd;
                            int secDataLine = FindFirstDataLine(lines, j);
                            var sec = new GeomBlock() { Kind = "Section", Label = DescribeSection(lines, secDataLine), StartLine = secStart, EndLine = secEnd, DataLine = secDataLine };
                            surf.Children.Add(sec);
                            lastSection = sec;
                            j = secEnd + 1;
                            continue;
                        }
                        if (t == "control")
                        {
                            int ctrlStart = ExtendBackForSeparator(lines, j);
                            int ctrlEnd = FindMarker(lines, j, "!endcontrol");
                            if (ctrlEnd == -1 || ctrlEnd > surfEnd)
                                ctrlEnd = surfEnd;
                            int ctrlDataLine = FindFirstDataLine(lines, j);
                            var ctrl = new GeomBlock() { Kind = "Control", Label = DescribeControl(lines, ctrlDataLine), StartLine = ctrlStart, EndLine = ctrlEnd, DataLine = ctrlDataLine };
                            if (lastSection is not null)
                            {
                                lastSection.Children.Add(ctrl);
                            }
                            else
                            {
                                surf.Children.Add(ctrl);
                            }
                            j = ctrlEnd + 1;
                            continue;
                        }
                        j += 1;
                    }

                    surfaces.Add(surf);
                    i = surfEnd + 1;
                    continue;
                }
                i += 1;
            }
            return surfaces;
        }

        private int FindMarker(string[] lines, int fromLine, string marker)
        {
            for (int k = fromLine, loopTo = lines.Length - 1; k <= loopTo; k++)
            {
                if ((lines[k].Trim().ToLowerInvariant() ?? "") == (marker ?? ""))
                    return k;
            }
            return -1;
        }

        // Rolls a decorative separator comment line (e.g. "#----", "#====") that
        // immediately precedes a block's keyword into the block itself, so
        // moving/deleting a block doesn't leave an orphaned separator behind.
        private int ExtendBackForSeparator(string[] lines, int keywordLine)
        {
            int k = keywordLine - 1;
            if (k >= 0)
            {
                string t = lines[k].Trim();
                if (t.StartsWith("#"))
                {
                    string body = t.TrimStart('#');
                    if (body.Length > 0 && string.IsNullOrEmpty(body.Trim(body[0])))
                        return k;
                }
            }
            return keywordLine;
        }

        // First non-blank, non-comment, non-marker line after a block's keyword -
        // i.e. the actual data row. Shared by the describers below AND by the
        // double-click handler, which needs this exact line number to match a
        // block against the corresponding Node in `points` (Node.lineNumber /
        // Node.controlLineNumber point at this same row).
        private int FindFirstDataLine(string[] lines, int keywordLine)
        {
            for (int k = keywordLine + 1, loopTo = Math.Min(lines.Length - 1, keywordLine + 5); k <= loopTo; k++)
            {
                string t = lines[k].Trim();
                if (string.IsNullOrEmpty(t) || t.StartsWith("#") || t.StartsWith("!"))
                    continue;
                return k;
            }
            return -1;
        }

        private string DescribeSection(string[] lines, int dataLine)
        {
            if (dataLine == -1)
                return "Section";
            var nums = new List<string>();
            foreach (var v in lines[dataLine].Split(' '))
            {
                if (Information.IsNumeric(v))
                    nums.Add(v);
            }
            if (nums.Count >= 4)
                return $"Section (Xle={nums[0]}, Chord={nums[3]})";
            return "Section";
        }

        private string DescribeControl(string[] lines, int dataLine)
        {
            if (dataLine == -1)
                return "Control";
            var toks = new List<string>();
            foreach (var v in lines[dataLine].Split(' '))
            {
                if (v.Trim().Length > 0)
                    toks.Add(v.Trim());
            }
            if (toks.Count > 0)
                return toks[0];
            return "Control";
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (AppMessageBox.Show("Are you sure you want to clear the current file? This cannot be undone.", "Clear File", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                txt3.Text = "";
            }
        }


        public void CaptureApplication(string procWindowTitle, string caption)
        {
            Process proc = null;
            while (proc == null)
            {
                Application.DoEvents();
                foreach (Process p in Process.GetProcesses())
                {
                    if ((p.MainWindowTitle ?? "") == (procWindowTitle ?? ""))
                    {
                        proc = p;
                        break;
                    }
                    // Debug.WriteLine(p.MainWindowTitle)
                }
            }
            Thread.Sleep(200); // wait for window to fully open
                               // Dim proc = Process.GetProcessesByName(procName)(0)
            var rectw = new User32.Rect();
            var rect = new User32.Rect();
            var cpoint = new User32.POINTAPI();
            User32.GetWindowRect(proc.MainWindowHandle, ref rectw);
            User32.GetClientRect(proc.MainWindowHandle, ref rect);
            User32.ClientToScreen(proc.MainWindowHandle, ref cpoint);
            User32.SetWindowPos(proc.MainWindowHandle, User32.HWND_TOPMOST, rectw.left, rectw.top, rectw.right - rectw.left, rectw.bottom - rectw.top, User32.SWP_SHOWWINDOW);
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;
            // Dim bmp = New Bitmap(width, height, PixelFormat.Format32bppArgb)
            var bmp = new Bitmap(rect.right, rect.bottom, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bmp))
            {
                // graphics.CopyFromScreen(rect.left, rect.top, 0, 0, New Size(width, height), CopyPixelOperation.SourceCopy)
                graphics.CopyFromScreen(cpoint.X, cpoint.Y, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
            }

            // bmp.Save("c:\tmp\test.png", ImageFormat.Png)

            var frm = new Form();
            var p1 = new PictureBox();
            p1.Image = bmp;
            p1.SizeMode = PictureBoxSizeMode.AutoSize;
            frm.ClientSize = p1.Size;
            p1.Dock = DockStyle.Fill;
            frm.Controls.Add(p1);
            frm.Text = caption;
            frm.Icon = My.MyProject.Forms.frmMain.Icon;
            frm.StartPosition = FormStartPosition.CenterScreen;
            frm.Show();
            // frmMain.p.StandardInput.WriteLine("quit")
            My.MyProject.Forms.frmMain.RestartConsoleToolStripMenuItem.PerformClick();
        }
        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Rect
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }
            public struct POINTAPI
            {
                public int X;
                public int Y;
            }
            [DllImport("user32.dll")]
            public static extern nint GetWindowRect(nint hWnd, ref Rect rect);
            [DllImport("user32.dll")]
            public static extern int GetClientRect(nint hWnd, ref Rect rect);
            [DllImport("user32.dll")]
            public static extern bool ClientToScreen(nint hWnd, ref POINTAPI lpPoint);
            [DllImport("user32.dll")]
            public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
            public static readonly nint HWND_TOPMOST = new nint(-1);
            public const uint SWP_NOSIZE = 1U;
            public const uint SWP_NOMOVE = 2U;
            public const uint SWP_SHOWWINDOW = 64U;


        }

        /// <summary>
    /// Applies a surface's SCALE/TRANSLATE/ANGLE keywords to its already-parsed sections, once,
    /// right after parsing and before anything (the outline-building loop below or DrawMeshForSurface)
    /// reads Xle/Yle/Zle/Chord/Ainc - so both consumers see the same already-transformed geometry
    /// instead of needing the same transform applied twice in two places. Per AVL's documented order,
    /// SCALE is applied before TRANSLATE (chords scale by Xscale specifically); dAinc is added to
    /// Ainc for data correctness, though Ainc itself is documented as not visually rotating the drawn
    /// geometry (flow-tangency only), so this has no visible effect - consistent with existing behavior.
    /// </summary>
        private void ApplySurfaceTransform(ref Surface surface)
        {
            if (surface.sections is null)
                return;
            for (int idx = 0, loopTo = surface.sections.Count - 1; idx <= loopTo; idx++)
            {
                var se = surface.sections[idx];
                se.Xle = se.Xle * surface.Xscale + surface.dX;
                se.Yle = se.Yle * surface.Yscale + surface.dY;
                se.Zle = se.Zle * surface.Zscale + surface.dZ;
                se.Chord = se.Chord * surface.Xscale;
                se.Ainc = se.Ainc + surface.dAinc;
                surface.sections[idx] = se;
            }
        }

        private void findPoints()
        {
            // Debug.WriteLine($"==================================================================")


            string geometryfilename = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            string avlText = "";
            bool isGeometryTab = tc1.SelectedTab is not null && tc1.SelectedTab.Name == "Geometry" && txt3 is not null;

            if (isGeometryTab)
            {
                avlText = txt3.Text;
            }
            else if (File.Exists(geometryfilename))
            {
                avlText = File.ReadAllText(geometryfilename);
            }
            else
            {
                return;
            }

            string[] lines = TrimAll(avlText.Replace(Constants.vbLf, "")).Split(Conversions.ToChar(Constants.vbCrLf));


            int c = 0;
            string[] vals;
            // Dim section As Section
            // Dim surface As Surface
            // Dim sections As List(Of Section)
            var surfaces = new List<Surface>();
            int i = 0;

            while (i < lines.Count() - 2)
            {
                // Debug.WriteLine($"Line number: {i} | {lines(i).ToLower.Trim = "surface"} | {lines(i)}")
                if (lines[i].ToLower().Trim() == "surface")
                {
                    // AppMessageBox.Show("found a surface")
                    // Debug.WriteLine($"Found surface at line {i + 1}")
                    var surface = new Surface();
                    surface.sections = new List<Section>();
                    // SCALE factors are multiplicative and must default to "no scaling" (1.0), not the
                    // Double zero-value a Structure naturally gets on New - a stray 0.0 here would
                    // collapse the whole surface to a point for any file that omits SCALE.
                    surface.Xscale = 1.0d;
                    surface.Yscale = 1.0d;
                    surface.Zscale = 1.0d;
                    var controls = new List<System.Windows.Controls.Control>();
                    surface.Name = lines[i + 1].Trim().Replace(Environment.NewLine, "");
                    // AppMessageBox.Show(lines(i + 1))
                    i += 2;
                    while (lines[i + 1].ToLower().Trim() != "surface" & i < lines.Count() - 2)
                    {
                        if (lines[i].ToLower().Trim() == "yduplicate")
                        {
                            surface.yDuplicate = true;
                            surface.yDuplicatevalue = Conversions.ToDouble(lines[i + 1]);
                        }

                        if (lines[i].ToLower().Trim() == "scale")
                        {
                            string[] svals = lines[i + 1].Split(' ');
                            if (svals.Length >= 3)
                            {
                                surface.Xscale = Conversions.ToDouble(svals[0]);
                                surface.Yscale = Conversions.ToDouble(svals[1]);
                                surface.Zscale = Conversions.ToDouble(svals[2]);
                            }
                        }

                        if (lines[i].ToLower().Trim() == "translate")
                        {
                            string[] tvals = lines[i + 1].Split(' ');
                            if (tvals.Length >= 3)
                            {
                                surface.dX = Conversions.ToDouble(tvals[0]);
                                surface.dY = Conversions.ToDouble(tvals[1]);
                                surface.dZ = Conversions.ToDouble(tvals[2]);
                            }
                        }

                        if (lines[i].ToLower().Trim() == "angle")
                        {
                            surface.dAinc = Conversions.ToDouble(lines[i + 1].Trim());
                        }

                        if (lines[i].ToLower().Trim() == "component" || lines[i].ToLower().Trim() == "index")
                        {
                            string[] lcvals = lines[i + 1].Split(' ');
                            if (lcvals.Length >= 1 && Information.IsNumeric(lcvals[0]))
                            {
                                surface.Lcomp = (int)Math.Round(Conversions.ToDouble(lcvals[0]));
                            }
                        }

                        if (lines[i].ToLower().Trim() == "nowake")
                        {
                            surface.NoWake = true;
                        }

                        if (lines[i].ToLower().Trim() == "noalbe")
                        {
                            surface.NoAlbe = true;
                        }

                        if (lines[i].ToLower().Trim() == "noload")
                        {
                            surface.NoLoad = true;
                        }

                        if (lines[i].ToLower().Trim().StartsWith("#nchordwise"))
                        {
                            vals = lines[i + 1].Split(' ');
                            int nc = 0;
                            for (int l = 0, loopTo = Information.UBound(vals); l <= loopTo; l++)
                            {
                                if (Information.IsNumeric(vals[l]))
                                {
                                    nc += 1;
                                    switch (nc)
                                    {
                                        case 1:
                                            {
                                                surface.Nchordwise = Conversions.ToDouble(vals[l]);
                                                break;
                                            }
                                        case 2:
                                            {
                                                surface.Cspace = Conversions.ToDouble(vals[l]);
                                                break;
                                            }
                                        case 3:
                                            {
                                                surface.Nspanwise = Conversions.ToDouble(vals[l]);
                                                break;
                                            }
                                        case 4:
                                            {
                                                surface.Sspace = Conversions.ToDouble(vals[l]);
                                                break;
                                            }
                                    }
                                }
                            }
                        }

                        // check for sections
                        if (lines[i].ToLower().Trim() == "section")
                        {
                            // Debug.WriteLine($"Found    section at line {i + 1}")
                            // AppMessageBox.Show("found a section")
                            i += 1;
                        }
                        if (lines[i].ToLower().Trim().StartsWith("#xle"))
                        {
                            vals = lines[i + 1].Split(' ');
                            int counter = 0;
                            var section = new Section();
                            section.controls = new List<ControlSurface>();
                            section.lineNumber = i + 1;
                            for (int l = 0, loopTo1 = Information.UBound(vals); l <= loopTo1; l++)
                            {
                                if (Information.IsNumeric(vals[l]))
                                {
                                    counter += 1;
                                    switch (counter)
                                    {
                                        case 1:
                                            {
                                                section.Xle = Conversions.ToDouble(vals[l]);
                                                break;
                                            }
                                        case 2:
                                            {
                                                section.Yle = Conversions.ToDouble(vals[l]);
                                                break;
                                            }
                                        case 3:
                                            {
                                                section.Zle = Conversions.ToDouble(vals[l]);
                                                break;
                                            }
                                        case 4:
                                            {
                                                section.Chord = Conversions.ToDouble(vals[l]);
                                                break;
                                            }
                                        case 5:
                                            {
                                                section.Ainc = Conversions.ToDouble(vals[l]);
                                                break;
                                            }
                                        case 6:
                                            {
                                                section.Nspanwise = Conversions.ToDouble(vals[l]);
                                                break;
                                            }
                                        case 7:
                                            {
                                                section.Sspace = Conversions.ToDouble(vals[l]);
                                                break;
                                            }
                                    }
                                }
                            }

                            while (lines[i + 2].ToLower().Trim() != "surface" & lines[i + 2].ToLower().Trim() != "section" & i < lines.Count() - 3)
                            {

                                // check for controls
                                if (lines[i].ToLower().Trim() == "control")
                                {
                                    // Debug.WriteLine($"Found       control at line {i + 1}")
                                    i += 1;
                                }

                                if (lines[i].ToLower().Trim().StartsWith("#cname"))
                                {
                                    vals = lines[i + 1].Split(' ');
                                    var control = new ControlSurface();
                                    control.lineNumber = i + 1;
                                    control.Type = vals[0];
                                    control.Cgain = Conversions.ToDouble(vals[1]);
                                    control.Xhinge = Conversions.ToDouble(vals[2]);
                                    section.controls.Add(control);
                                }

                                i += 1;

                            }


                            surface.sections.Add(section);
                        }

                        i += 1;

                    }
                    ApplySurfaceTransform(ref surface);
                    surfaces.Add(surface);
                }
                i += 1;
            }

            // Me.Text = $"Surface Count: {surfaces.Count.ToString}"

            parsedSurfaces = surfaces;
            points.Clear();

            if (surfaces.Count > 0)
            {
                foreach (Surface su in surfaces)
                {
                    foreach (Section se in su.sections)
                    {
                        // Leading edge — draggable, updates Xle/Yle/Zle
                        var p1 = new Point3D(se.Xle, se.Yle, se.Zle);
                        var n1 = new Node(p1, su.Name, false, se.lineNumber, Node.NodeType.Geometry);
                        n1.SubType = Node.NodeSubType.LeadingEdge;
                        n1.IsDuplicate = false;
                        points.Add(n1);
                        // Trailing edge — draggable, updates Chord only
                        var p2 = new Point3D(se.Xle + se.Chord, se.Yle, se.Zle);
                        var n2 = new Node(p2, su.Name, false, se.lineNumber, Node.NodeType.Geometry);
                        n2.SubType = Node.NodeSubType.TrailingEdge;
                        n2.IsDuplicate = false;
                        n2.parentXle = (float)se.Xle;
                        n2.parentChord = (float)se.Chord;
                        points.Add(n2);
                        if (se.controls.Count > 0)
                        {
                            foreach (ControlSurface cs in se.controls)
                            {
                                // Control hinge (pc1) — draggable, updates Xhinge only
                                var pc1 = new Point3D(se.Xle + (cs.Xhinge > 0d ? cs.Xhinge : 1d - cs.Xhinge) * se.Chord, se.Yle, se.Zle);
                                var nc1 = new Node(pc1, "control" + cs.Type.Replace(Constants.vbLf, ""), false, se.lineNumber, Node.NodeType.Geometry);
                                nc1.SubType = Node.NodeSubType.ControlHinge;
                                nc1.IsDuplicate = false;
                                nc1.parentXle = (float)se.Xle;
                                nc1.parentChord = (float)se.Chord;
                                nc1.controlLineNumber = cs.lineNumber;
                                nc1.originalXhinge = (float)cs.Xhinge;
                                points.Add(nc1);
                                // Control trailing edge (pc2) — not independently draggable (same as n2)
                                var pc2 = new Point3D(se.Xle + se.Chord, se.Yle, se.Zle);
                                points.Add(new Node(pc2, "control" + cs.Type.Replace(Constants.vbLf, ""), false, se.lineNumber, Node.NodeType.Geometry));
                            }
                        }
                        if (su.yDuplicate)
                        {
                            // Mirrored duplicates — not independently draggable (IsDraggable excludes
                            // them), but they point at the SAME underlying line as the primary node,
                            // so they're still clickable to open the properties panel for that line.
                            var p3 = new Point3D(se.Xle, 2d * su.yDuplicatevalue - se.Yle, se.Zle);
                            var n3 = new Node(p3, su.Name + "_dup", false, se.lineNumber, Node.NodeType.Geometry);
                            n3.SubType = Node.NodeSubType.LeadingEdge;
                            n3.IsDuplicate = true;
                            points.Add(n3);
                            var p4 = new Point3D(se.Xle + se.Chord, 2d * su.yDuplicatevalue - se.Yle, se.Zle);
                            var n4 = new Node(p4, su.Name + "_dup", false, se.lineNumber, Node.NodeType.Geometry);
                            n4.SubType = Node.NodeSubType.TrailingEdge;
                            n4.IsDuplicate = true;
                            points.Add(n4);
                            if (se.controls.Count > 0)
                            {
                                foreach (ControlSurface cs in se.controls)
                                {
                                    var pc3 = new Point3D(se.Xle + (cs.Xhinge > 0d ? cs.Xhinge : 1d - cs.Xhinge) * se.Chord, 2d * su.yDuplicatevalue - se.Yle, se.Zle);
                                    var nc3 = new Node(pc3, "control" + cs.Type.Replace(Constants.vbLf, "") + "_dup", false, se.lineNumber, Node.NodeType.Geometry);
                                    nc3.SubType = Node.NodeSubType.ControlHinge;
                                    nc3.IsDuplicate = true;
                                    nc3.parentXle = (float)se.Xle;
                                    nc3.parentChord = (float)se.Chord;
                                    nc3.controlLineNumber = cs.lineNumber;
                                    nc3.originalXhinge = (float)cs.Xhinge;
                                    points.Add(nc3);
                                    var pc4 = new Point3D(se.Xle + se.Chord, 2d * su.yDuplicatevalue - se.Yle, se.Zle);
                                    points.Add(new Node(pc4, "control" + cs.Type.Replace(Constants.vbLf, "") + "_dup", false, se.lineNumber, Node.NodeType.Geometry));
                                }
                            }
                        }
                    }
                }
            }


            string massText = "";
            bool isMassTab = tc1.SelectedTab is not null && tc1.SelectedTab.Name == "Mass" && txt3 is not null;
            bool hasMassData = false;
            string massfilename = Path.Combine(Application.StartupPath, $"{projectName}.mass");

            if (isMassTab)
            {
                massText = txt3.Text;
                hasMassData = true;
            }
            else if (File.Exists(massfilename))
            {
                massText = File.ReadAllText(massfilename);
                hasMassData = true;
            }

            if (hasMassData)
            {
                string[] massLines = massText.Split(new string[] { Constants.vbCrLf, Constants.vbLf, Constants.vbCr }, StringSplitOptions.None);
                int lnum = -1;
                foreach (string line in massLines)
                {
                    lnum += 1;
                    // Trim double spaces to support split by space
                    string cleanLine = line.Trim();
                    while (cleanLine.Contains("  "))
                        cleanLine = cleanLine.Replace("  ", " ");
                    string[] pars = cleanLine.Split(' ');
                    if (pars.Length > 0 && !string.IsNullOrEmpty(pars[0]))
                    {
                        double val = 0d;
                        if (double.TryParse(pars[0], out val))
                        {
                            double xval = 0d;
                            double yval = 0d;
                            double zval = 0d;
                            if (pars.Length > 1)
                                double.TryParse(pars[1], out xval);
                            if (pars.Length > 2)
                                double.TryParse(pars[2], out yval);
                            if (pars.Length > 3)
                                double.TryParse(pars[3], out zval);
                            points.Add(new Node(xval, yval, zval, "Mass", false, lnum, Node.NodeType.Mass, (float)val));
                        }
                    }
                }
            }

            updating = false;

            RefreshStructureTree();

        }

        private void txt3_TextChangedDelayed(object sender, object e)
        {
            if (updating == true)
                return;

            if (My.MySettingsProperty.Settings.autoSave)
            {
                ForceSaveActiveFile(true);
            }
            else
            {
                isDirty = true;
                UpdateDirtyWarning();
            }

            findPoints();
            drawAxes();
        }

        private Dictionary<string, int> CountBlocks(bool countforbefore = true)
        {
            var result = new Dictionary<string, int>();
            int loc = txt3.SelectionStart;
            string input = txt3.Text.Substring(0, loc);
            if (countforbefore == false)
            {
                input = txt3.Text.Substring(loc, txt3.Text.Length - loc);
            }
            string phrase = "!begingeometry";
            int Occurrences = Regex.Matches(input, $"{phrase}").Count;
            result[phrase] = Occurrences;
            phrase = "!beginsurface";
            Occurrences = Regex.Matches(input, $"{phrase}").Count;
            result[phrase] = Occurrences;
            phrase = "!beginsection";
            Occurrences = Regex.Matches(input, $"{phrase}").Count;
            result[phrase] = Occurrences;
            phrase = "!begincontrol";
            Occurrences = Regex.Matches(input, $"{phrase}").Count;
            result[phrase] = Occurrences;

            phrase = "!endgeometry";
            Occurrences = Regex.Matches(input, $"{phrase}").Count;
            result[phrase] = Occurrences;
            phrase = "!endsurface";
            Occurrences = Regex.Matches(input, $"{phrase}").Count;
            result[phrase] = Occurrences;
            phrase = "!endsection";
            Occurrences = Regex.Matches(input, $"{phrase}").Count;
            result[phrase] = Occurrences;
            phrase = "!endcontrol";
            Occurrences = Regex.Matches(input, $"{phrase}").Count;
            result[phrase] = Occurrences;

            // Dim before = CountBlocks()
            // Dim after = CountBlocks(False)

            if (countforbefore == true)
            {
                foreach (KeyValuePair<string, int> p in result)
                    Console.WriteLine($"BEFORE: {p.Key} has {p.Value} items");
            }
            else
            {
                foreach (KeyValuePair<string, int> p in result)

                    Console.WriteLine($"AFTER: {p.Key} has {p.Value} items");
            }


            return result;
        }



        private void txt3_ToolTipNeeded(object sender, ToolTipNeededEventArgs e)
        {

            if (!string.IsNullOrEmpty(e.HoveredWord))
            {
                e.ToolTipIcon = ToolTipIcon.Info;
                switch (e.HoveredWord.ToLower() ?? "")
                {
                    case "header":
                    case "mach":
                    case "iysym":
                    case "izsym":
                    case "zsym":
                    case "sref":
                    case "cref":
                    case "bref":
                    case "xref":
                    case "yref":
                    case "zref":
                        {
                            e.ToolTipTitle = "Header data";
                            e.ToolTipText = readLines(help, 243, 293);
                            break;
                        }
                    case "cspace":
                    case "bspace":
                    case "sspace":
                        {
                            e.ToolTipTitle = "Vortex Lattice Spacing Distributions";
                            e.ToolTipText = readLines(help, 999, 1049);
                            break;
                        }
                    case "surface":
                    case "nchord":
                    case "nspan":
                    case "nchordwise":
                    case "nspanwise":
                        {
                            e.ToolTipTitle = "Surface-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 378, 400);
                            break;
                        }
                    case "yduplicate":
                        {
                            e.ToolTipTitle = "Surface-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 423, 450);
                            break;
                        }
                    case "scale":
                        {
                            e.ToolTipTitle = "Surface-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 452, 461);
                            break;
                        }
                    case "translate":
                        {
                            e.ToolTipTitle = "Surface-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 464, 474);
                            break;
                        }
                    case "nowake":
                        {
                            e.ToolTipTitle = "Surface-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 488, 495);
                            break;
                        }
                    case "noalbe":
                        {
                            e.ToolTipTitle = "Surface-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 497, 508);
                            break;
                        }
                    case "noload":
                        {
                            e.ToolTipTitle = "Surface-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 510, 538);
                            break;
                        }
                    case "angle":
                        {
                            e.ToolTipTitle = "Surface-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 476, 486);
                            break;
                        }
                    case "cdcl":
                        {
                            e.ToolTipTitle = "Surface-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 540, 573);
                            break;
                        }
                    case "section":
                    case "xle":
                    case "yle":
                    case "zle":
                    case "chord":
                    case "ainc":
                        {
                            e.ToolTipTitle = "Section-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 576, 625);
                            break;
                        }
                    case "naca":
                        {
                            e.ToolTipTitle = "Section-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 628, 642);
                            break;
                        }
                    case "airfoil":
                        {
                            e.ToolTipTitle = "Section-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 645, 671);
                            break;
                        }
                    case "afile":
                        {
                            e.ToolTipTitle = "Section-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 674, 694);
                            break;
                        }
                    case "design":
                        {
                            e.ToolTipTitle = "Section-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 697, 727);
                            break;
                        }
                    case "control":
                    case "cname":
                    case "cgain":
                    case "xhinge":
                    case "xyzhvec":
                    case "hingevec":
                    case "sgnDup":
                        {
                            e.ToolTipTitle = "Control-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 729, 755);
                            break;
                        }
                    case "claf":
                        {
                            e.ToolTipTitle = "Control-definition keywords and data formats";
                            e.ToolTipText = readLines(help, 875, 898);
                            break;
                        }
                    case "lunit":
                    case "munit":
                    case "tunit":
                    case "g":
                    case "rho":
                        {
                            e.ToolTipTitle = "Header data";
                            e.ToolTipText = readLines(help, 1129, 1180) + Environment.NewLine + readLines(help, 1197, 1230);
                            break;
                        }
                    case "mass":
                    case "ixx":
                    case "iyy":
                    case "izz":
                    case var @case when @case == "yref":
                    case var case1 when case1 == "zref":
                        {
                            e.ToolTipTitle = "Header data";
                            e.ToolTipText = readLines(help, 1233, 1300);
                            break;
                        }

                    default:
                        {
                            e.ToolTipTitle = e.HoveredWord;
                            e.ToolTipText = "No information available for '" + e.HoveredWord + "'" + Environment.NewLine + "Use the help button (?) at in the menu to look for more information in the AVL documentation";
                            break;
                        }
                }
                if (e.ToolTipText is not null)
                {
                    e.ToolTipText = e.ToolTipText.Trim();
                }
                currentToolTipText = e.ToolTipText ?? "";
                currentHoveredWord = e.HoveredWord;
            }
        }


        public string readLines(string path, int startline, int endline = 0)
        {

            if (endline == 0)
            {
                endline = startline;
            }

            string[] all = File.ReadAllLines(path);

            var lines = new List<string>();

            for (int i = startline, loopTo = endline; i <= loopTo; i++)
                lines.Add(all[i].Trim());

            // Remove leading empty lines
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
                lines.RemoveAt(0);

            // Remove trailing empty lines
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                lines.RemoveAt(lines.Count - 1);

            return string.Join(Constants.vbCrLf, lines);
        }




        private void txt3_AutoIndentNeeded(object sender, AutoIndentEventArgs e)
        {
            string lineTextTrimmed = e.LineText.Trim().ToLower();

            // If the line starts with any !begin tag, shift next lines to the right
            if (lineTextTrimmed.StartsWith("!begingeometry") || lineTextTrimmed.StartsWith("!beginsurface") || lineTextTrimmed.StartsWith("!beginsection") || lineTextTrimmed.StartsWith("!begincontrol"))
            {
                e.ShiftNextLines = e.TabLength;
                return;
            }

            // If the line starts with any !end tag, shift current line and next lines to the left
            if (lineTextTrimmed.StartsWith("!endgeometry") || lineTextTrimmed.StartsWith("!endsurface") || lineTextTrimmed.StartsWith("!endsection") || lineTextTrimmed.StartsWith("!endcontrol"))
            {
                e.Shift = -e.TabLength;
                e.ShiftNextLines = -e.TabLength;
                return;
            }
        }

        private string TrimAll(string Text, string filename = "")
        {

            if (Text.Length == 0)
                return ""; // zero len string



            string result = Text;

            while (result.IndexOf("  ") > -1)
                result = result.Replace("  ", " ");

            // Const toRemove As String = " " & vbTab & vbCr & vbLf 'what to remove

            string[] lines = result.Split(Conversions.ToChar(Environment.NewLine));
            string output = "";

            // For Each Str As String In lines
            // Dim s As Long : s = 1
            // Dim e As Long : e = Len(Str)
            // Dim c As String

            // Do 'how many chars to skip on the left side
            // c = Mid(Str, s, 1)
            // If c = "" Or InStr(1, toRemove, c) = 0 Then Exit Do
            // s = s + 1
            // Loop
            // result += IIf(result.Length = 0, "", Environment.NewLine) + Mid(Str, s, (e - s) + 1) 'return remaining text
            // Next

            foreach (string Str in lines)
            {
                string newline = Str.Trim();
                // Debug.WriteLine(newline.Length & "|" & Str.Length & ": " & Str & newline)
                if (newline.Length > 0)
                {
                    if (newline.ToLower().StartsWith("run case"))
                    {
                        newline = newline.Replace("Run case ", "Run case  ").Replace("Run Case ", "Run case  ").Replace("run case ", "Run case  ");
                        output += newline + Environment.NewLine;
                    }
                    else
                    {
                        output += newline + Environment.NewLine;
                    }
                }
            }
            // File.WriteAllText($"{Application.StartupPath}\temp.txt", output)

            return output;

        }

        private void MassTemplateFullToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReplaceMassTabWithTemplate(AvlTemplates.MassTemplateFull);
        }

        private void MassTemplateMinimalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReplaceMassTabWithTemplate(AvlTemplates.MassTemplateMinimal);
        }

        // See the guard comment on ReplaceGeometryTabWithTemplate - same hazard.
        private void ReplaceMassTabWithTemplate(string val)
        {
            if (tc1.SelectedTab is not null && tc1.SelectedTab.Name != "Mass")
            {
                AppMessageBox.Show("Switch to the Mass tab first - this would replace the " + tc1.SelectedTab.Name + " tab's content otherwise.", "Wrong Tab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Replace entire text via selection to preserve undo history
            txt3.Selection.Start = new Place(0, 0);
            txt3.Selection.End = new Place(txt3.Lines[txt3.LinesCount - 1].Length, txt3.LinesCount - 1);
            txt3.InsertText(val);

            txt3.SelectionStart = txt3.Text.Length;
            txt3.DoCaretVisible();
            FormatActiveText();
        }


        private void RunTemplateFullToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReplaceRunTabWithTemplate(AvlTemplates.RunTemplateFull);
        }

        private void RunTemplateMinimalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReplaceRunTabWithTemplate(AvlTemplates.RunTemplateMinimal);
        }

        // See the guard comment on ReplaceGeometryTabWithTemplate - same hazard.
        private void ReplaceRunTabWithTemplate(string val)
        {
            if (tc1.SelectedTab is not null && tc1.SelectedTab.Name != "Run")
            {
                AppMessageBox.Show("Switch to the Run tab first - this would replace the " + tc1.SelectedTab.Name + " tab's content otherwise.", "Wrong Tab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Replace entire text via selection to preserve undo history
            txt3.Selection.Start = new Place(0, 0);
            txt3.Selection.End = new Place(txt3.Lines[txt3.LinesCount - 1].Length, txt3.LinesCount - 1);
            txt3.InsertText(val);

            txt3.SelectionStart = txt3.Text.Length;
            txt3.DoCaretVisible();
            FormatActiveText();
        }


        private void pxz_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseDownScreenPos = e.Location;
                if (isDragMode)
                {
                    double e1 = (xmax - xmin) / pxy.Width * (e.X - pxz.Width / 2d - xoffset);
                    double e2 = (zmax - zmin) / pxz.Height * (e.Y - pxz.Height / 2d - zoffset);
                    double hitEpsX = 7.0d * (xmax - xmin) / pxz.Width;
                    double hitEpsZ = 7.0d * (zmax - zmin) / pxz.Height;
                    foreach (Node n in points)
                    {
                        if (n.IsDraggable && Math.Abs(n.X - e1) < hitEpsX && Math.Abs(n.Z - (-e2)) < hitEpsZ)
                        {
                            draggingNode = n;
                            isDragging = true;
                            dragStartX = n.X;
                            dragStartY = n.Y;
                            dragStartZ = n.Z;
                            pxz.Cursor = Cursors.SizeAll;
                            break;
                        }
                    }
                    return;
                }
                pxz.Cursor = Cursors.SizeAll;
                xdown = e.X - xoffset;
                zdown = e.Y - zoffset;
            }
        }

        private void pxz_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta < 0)
            {
                gridnumber += 1;
                drawAxes();
            }
            else if (gridnumber > 1)
            {
                gridnumber -= 1;
                drawAxes();
            }

        }

        private void pxz_MouseMove(object sender, MouseEventArgs e)
        {
            double e1 = (xmax - xmin) / pxy.Width * (e.X - pxz.Width / 2d - xoffset);
            double e2 = (zmax - zmin) / pxz.Height * (e.Y - pxz.Height / 2d - zoffset);
            curX = e1;
            curZ = -e2;
            lblCursor.Text = "Cursor: [X: " + string.Format("{0,5:###.0}", Math.Round(e1, 1)) + ", Z: " + string.Format("{0,5:###.0}", Math.Round(-e2, 1)) + "]";

            if (isDragMode)
            {
                double hitEpsX = 7.0d * (xmax - xmin) / pxz.Width;
                double hitEpsZ = 7.0d * (zmax - zmin) / pxz.Height;
                bool nearDraggable = points.Any(n => n.IsDraggable && Math.Abs(n.X - e1) < hitEpsX && Math.Abs(n.Z - (-e2)) < hitEpsZ);
                pxz.Cursor = nearDraggable || isDragging ? Cursors.SizeAll : Cursors.Hand;
                if (isDragging && draggingNode is not null && e.Button == MouseButtons.Left)
                {
                    draggingNode.X = (float)e1;
                    draggingNode.Z = (float)-e2;
                    draggingNode.Point = new Point3D(e1, draggingNode.Y, -e2);
                    drawAxes();
                }
                return;
            }

            double hoverEpsX = 7.0d * (xmax - xmin) / pxz.Width;
            double hoverEpsZ = 7.0d * (zmax - zmin) / pxz.Height;
            var prevHovered = points.FirstOrDefault(p => p.Hovered);
            Node newHovered = null;
            foreach (Node p in points)
            {
                if (p.X > e1 - hoverEpsX & p.X < e1 + hoverEpsX & p.Z > -e2 - hoverEpsZ & p.Z < -e2 + hoverEpsZ)
                {
                    if (p.type == Node.NodeType.Geometry & showSection & showHover)
                    {
                        newHovered = p;
                        break;
                    }
                    if (p.type == Node.NodeType.Mass & showMass & showHover)
                    {
                        newHovered = p;
                    }
                }
            }

            bool hoverChanged = !ReferenceEquals(newHovered, prevHovered);
            foreach (Node p in points)
                p.Hovered = ReferenceEquals(p, newHovered);
            if (newHovered is not null)
            {
                isHovered = true;
                tc1.SelectedIndex = newHovered.type == Node.NodeType.Mass ? 1 : 0;
                selectText(newHovered.lineNumber);
            }
            else
            {
                isHovered = false;
            }

            bool panning = e.Button == MouseButtons.Left;
            if (panning)
            {
                xoffset = e.X - xdown;
                zoffset = e.Y - zdown;
            }

            if (hoverChanged || panning || showHover)
                drawAxes();
        }

        private void pxz_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragMode && isDragging && draggingNode is not null)
            {
                isDragging = false;
                double e1 = (xmax - xmin) / pxy.Width * (e.X - pxz.Width / 2d - xoffset);
                double e2 = (zmax - zmin) / pxz.Height * (e.Y - pxz.Height / 2d - zoffset);
                CommitNodeDrag(draggingNode, e1, dragStartY, -e2);  // Y unchanged in XZ view
                draggingNode = null;
                pxz.Cursor = Cursors.Hand;
                return;
            }
            pxz.Cursor = Cursors.Default;
            if (e.Button == MouseButtons.Left)
                drawAxes();
            HandlePotentialNodeClick(e);
        }

        private void btnEditor_Click(object sender, EventArgs e)
        {
            AppMessageBox.Show(@"Shortcut keys you can use for the editor:

Left, Right, Up, Down, Home, End, PageUp, PageDown - moves caret
Shift+(Left, Right, Up, Down, Home, End, PageUp, PageDown) - moves caret with selection
Ctrl+F, Ctrl+H - shows Find and Replace dialogs
F3 - find next
Ctrl+G - shows GoTo dialog
Ctrl+(C, V, X) - standard clipboard operations
Ctrl+A - selects all text
Ctrl+Z, Alt+Backspace, Ctrl+R - Undo/Redo opertions
Tab, Shift+Tab - increase/decrease left indent of selected range
Ctrl+Home, Ctrl+End - go to first/last char of the text
Shift+Ctrl+Home, Shift+Ctrl+End - go to first/last char of the text with selection
Ctrl+Left, Ctrl+Right - go word left/right
Shift+Ctrl+Left, Shift+Ctrl+Right - go word left/right with selection
Ctrl+-, Shift+Ctrl+- - backward/forward navigation
Ctrl+U, Shift+Ctrl+U - converts selected text to upper/lower case
Ctrl+Shift+C - inserts/removes comment prefix in selected lines
Ins - switches between Insert Mode and Overwrite Mode
Ctrl+Backspace, Ctrl+Del - remove word left/right
Alt+Mouse, Alt+Shift+(Up, Down, Right, Left) - enables column selection mode
Alt+Up, Alt+Down - moves selected lines up/down
Shift+Del - removes current line
Ctrl+B, Ctrl+Shift-B, Ctrl+N, Ctrl+Shift+N - add, removes and navigates to bookmark
Esc - closes all opened tooltips, menus and hints
Ctrl+Wheel - zooming
Ctrl+M, Ctrl+E - start/stop macro recording, executing of macro
Alt+F [char] - finds nearest [char]
Ctrl+(Up, Down) - scrolls Up/Down
Ctrl+(NumpadPlus, NumpadMinus, 0) - zoom in, zoom out, no zoom
Ctrl+I - forced AutoIndentChars of current line", "Editor Shortcuts", MessageBoxButtons.OK);
        }

        private void pyz_Click(object sender, EventArgs e)
        {

        }

        private void pyz_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseDownScreenPos = e.Location;
                if (isDragMode)
                {
                    double e1 = (ymax - ymin) / pyz.Width * (e.X - pyz.Width / 2d - yoffset);
                    double e2 = (zmax - zmin) / pyz.Height * (e.Y - pyz.Height / 2d - zoffset);
                    double hitEpsY = 7.0d * (ymax - ymin) / pyz.Width;
                    double hitEpsZ = 7.0d * (zmax - zmin) / pyz.Height;
                    foreach (Node n in points)
                    {
                        if (n.IsDraggable && Math.Abs(n.Y - e1) < hitEpsY && Math.Abs(n.Z - (-e2)) < hitEpsZ)
                        {
                            draggingNode = n;
                            isDragging = true;
                            dragStartX = n.X;
                            dragStartY = n.Y;
                            dragStartZ = n.Z;
                            pyz.Cursor = Cursors.SizeAll;
                            break;
                        }
                    }
                    return;
                }
                pyz.Cursor = Cursors.SizeAll;
                ydown = e.X - yoffset;
                zdown = e.Y - zoffset;
            }
        }

        private void pyz_MouseMove(object sender, MouseEventArgs e)
        {
            double e1 = (ymax - ymin) / pyz.Width * (e.X - pyz.Width / 2d - yoffset);
            double e2 = (zmax - zmin) / pyz.Height * (e.Y - pyz.Height / 2d - zoffset);
            curY = e1;
            curZ = -e2;
            lblCursor.Text = "Cursor: [Y: " + string.Format("{0,5:###.0}", Math.Round(e1, 1)) + ", Z: " + string.Format("{0,5:###.0}", Math.Round(-e2, 1)) + "]";

            if (isDragMode)
            {
                double hitEpsY = 7.0d * (ymax - ymin) / pyz.Width;
                double hitEpsZ = 7.0d * (zmax - zmin) / pyz.Height;
                bool nearDraggable = points.Any(n => n.IsDraggable && Math.Abs(n.Y - e1) < hitEpsY && Math.Abs(n.Z - (-e2)) < hitEpsZ);
                pyz.Cursor = nearDraggable || isDragging ? Cursors.SizeAll : Cursors.Hand;
                if (isDragging && draggingNode is not null && e.Button == MouseButtons.Left)
                {
                    draggingNode.Y = (float)e1;
                    draggingNode.Z = (float)-e2;
                    draggingNode.Point = new Point3D(draggingNode.X, e1, -e2);
                    drawAxes();
                }
                return;
            }

            double hoverEpsY = 7.0d * (ymax - ymin) / pyz.Width;
            double hoverEpsZ = 7.0d * (zmax - zmin) / pyz.Height;
            var prevHovered = points.FirstOrDefault(p => p.Hovered);
            Node newHovered = null;
            foreach (Node p in points)
            {
                if (p.Y > e1 - hoverEpsY & p.Y < e1 + hoverEpsY & p.Z > -e2 - hoverEpsZ & p.Z < -e2 + hoverEpsZ)
                {
                    if (p.type == Node.NodeType.Geometry & showSection & showHover)
                    {
                        newHovered = p;
                        break;
                    }
                    if (p.type == Node.NodeType.Mass & showMass & showHover)
                    {
                        newHovered = p;
                    }
                }
            }

            bool hoverChanged = !ReferenceEquals(newHovered, prevHovered);
            foreach (Node p in points)
                p.Hovered = ReferenceEquals(p, newHovered);
            if (newHovered is not null)
            {
                isHovered = true;
                tc1.SelectedIndex = newHovered.type == Node.NodeType.Mass ? 1 : 0;
                selectText(newHovered.lineNumber);
            }
            else
            {
                isHovered = false;
            }

            bool panning = e.Button == MouseButtons.Left;
            if (panning)
            {
                yoffset = e.X - ydown;
                zoffset = e.Y - zdown;
            }

            if (hoverChanged || panning || showHover)
                drawAxes();
        }

        private void pyz_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragMode && isDragging && draggingNode is not null)
            {
                isDragging = false;
                double e1 = (ymax - ymin) / pyz.Width * (e.X - pyz.Width / 2d - yoffset);
                double e2 = (zmax - zmin) / pyz.Height * (e.Y - pyz.Height / 2d - zoffset);
                CommitNodeDrag(draggingNode, dragStartX, e1, -e2);  // X unchanged in YZ view
                draggingNode = null;
                pyz.Cursor = Cursors.Hand;
                return;
            }
            pyz.Cursor = Cursors.Default;
            if (e.Button == MouseButtons.Left)
                drawAxes();
            HandlePotentialNodeClick(e);
        }

        private void pyz_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta < 0)
            {
                gridnumber += 1;
                drawAxes();
            }
            else if (gridnumber > 1)
            {
                gridnumber -= 1;
                drawAxes();
            }
        }

        // https://www.youtube.com/watch?v=ih20l3pJoeU
        private Point3D MultiplyMatrixVector(float x, float y, float z, float[,] m)
        {
            var i = new Point3D((double)x, (double)y, (double)z);
            Point3D o;

            o.X = i.X * m[0, 0] + i.Y * m[1, 0] + i.Z * m[2, 0] + m[3, 0];
            o.Y = i.X * m[0, 1] + i.Y * m[1, 1] + i.Z * m[2, 1] + m[3, 1];
            o.Z = i.X * m[0, 2] + i.Y * m[1, 2] + i.Z * m[2, 2] + m[3, 2];
            double w = i.X * m[0, 3] + i.Y * m[1, 3] + i.Z * m[2, 3] + m[3, 3];

            if (w != 0d)
            {
                o.X = o.X / w;
                o.Y = o.Y / w;
                o.Z = o.Z / w;
            }

            return default;

        }

        private Point3D getProjectedPoint(float x, float y, float z, float width, float height, float xmin, float xmax, float offsetx, float offsety, float offsetz, float[,] m)
        {
            var i = new Point3D((double)((x + offsetx) / (xmax - xmin)), (double)((y - offsety) / (xmax - xmin)), (double)((z - offsetz) / (xmax - xmin)));

            var r = MultiplyMatrixVector((float)i.X, (float)i.Y, (float)i.Z, m);

            var o = new Point3D(r.X * (double)(xmax - xmin) + (double)offsetx, r.Y * (double)(xmax - xmin) + (double)offsety, r.Z * (double)(xmax - xmin) + (double)offsetz);

            return o;

        }

        public Point PerspectiveProjection(Point3D point3D, double distanceFromViewer, double screenWidth, double screenHeight)
        {
            // Calculate the projected point using perspective rules
            var projectedPoint = new Point();
            double scaleFactor = distanceFromViewer / point3D.Z;
            projectedPoint.X = (int)Math.Round(point3D.X * scaleFactor + screenWidth / 2d);
            projectedPoint.Y = (int)Math.Round(-point3D.Y * scaleFactor + screenHeight / 2d);
            // Return the projected point
            return projectedPoint;
        }

        // Tick/axis fonts are reused across renders instead of being allocated (and leaked) on every frame.
        // HeavyRender runs via Task.Run, so cache access is locked in case two renders ever overlap.
        private Font GetTickFont(float size)
        {
            lock (_fontCacheLock)
            {
                if (_cachedTickFont is null || _cachedTickFontSize != size)
                {
                    _cachedTickFont?.Dispose();
                    // FontStyle.Regular was already correct - the "bold" look was
                    // coming from the font family itself: GenericMonospace resolves
                    // to Courier New on Windows, which has much heavier strokes than
                    // Consolas (the font this app already standardizes on for the
                    // log and code editor).
                    _cachedTickFont = new Font("Consolas", size, FontStyle.Regular);
                    _cachedTickFontSize = size;
                }
                return _cachedTickFont;
            }
        }

        private Font GetAxisFont()
        {
            lock (_fontCacheLock)
            {
                if (_cachedAxisFont is null)
                {
                    _cachedAxisFont = new Font(FontFamily.GenericSerif.Name, 12f, FontStyle.Regular);
                }
                return _cachedAxisFont;
            }
        }

        // Renders the Trefftz Plane plot to match AVL's own native "T" graphics
        // window (confirmed against a real AVL 3.37 screenshot): black background,
        // a config/case name + coefficient-grid text block, a legend, and two
        // stacked regions sharing the Y (spanwise) axis - Cl-perp/Cl/Cl*C/Cref on
        // a left axis on top, alpha_i alone on a right axis below. Mirrors the
        // SvgGraphics + capture-vectors pattern used by the geometry views so it
        // gets the same PNG/SVG/PDF export via ExportView/WriteVectorPdf.
        private void RenderTrefftzPlot(bool captureVectors = false)
        {
            if (pTrefftz is null || pTrefftz.Width <= 0 || pTrefftz.Height <= 0)
                return;

            int w = pTrefftz.Width;
            int h = pTrefftz.Height;
            var BMP = new Bitmap(w, h);
            var tickFont = GetTickFont(9f);
            string svgOut = "";
            string pdfOut = "";

            using (var G = new SvgGraphics(w, h, Graphics.FromImage(BMP), captureVectors))
            {
                G.SmoothingMode = SmoothingMode.AntiAlias;
                G.TextRenderingHint = TextRenderingHint.AntiAlias;
                G.Clear(ThemeCanvasBackColor);

                if (_lastTrefftzSurfaces is null || _lastTrefftzSurfaces.Count == 0)
                {
                    G.DrawString("Run \"Trefftz Plot\" to compute and plot spanwise loading.", tickFont, Brushes.Gray, new PointF(10f, 10f));
                }
                else
                {
                    float headerBottom = DrawTrefftzHeaderText(G, tickFont, w, h);
                    DrawTrefftzCombinedPlot(G, tickFont, w, h, headerBottom);
                }

                if (captureVectors)
                {
                    svgOut = G.GetSvgContent();
                    pdfOut = G.GetPdfContentStream();
                }
            }

            var oldImage = pTrefftz.Image;
            pTrefftz.Image = BMP;
            oldImage?.Dispose();

            if (captureVectors)
            {
                trefftzSvg = svgOut;
                trefftzPdf = pdfOut;
            }
        }

        // Draws the config/run-case name, the alpha/beta/M/CL/CY/CD/CDi/CDo/Cl'/Cm/Cn'/e
        // grid, the "AVL / Trefftz Plane" label, and the curve legend - matching the
        // text block AVL's own Trefftz Plane window shows. Returns the Y position
        // where the plot area below it should start.
        private float DrawTrefftzHeaderText(SvgGraphics G, Font font, int w, int h)
        {
            var t = _lastTrefftzTotals;
            var white = new SolidBrush(ThemeAxisColor);
            float lineH = font.Height + 4;
            float x0 = 8f;
            float y = 6f;

            var titleFont = new Font(font.FontFamily, font.Size + 2f, FontStyle.Bold);
            string cfgName = t is not null && !string.IsNullOrWhiteSpace(t.ConfigName) ? t.ConfigName : projectName;
            G.DrawString(cfgName, titleFont, white, new PointF(x0, y));
            y += titleFont.Height + 2;
            if (t is not null && !string.IsNullOrWhiteSpace(t.RunCaseName))
            {
                G.DrawString(t.RunCaseName, font, white, new PointF(x0, y));
            }
            y += lineH + 6f;

            float col1 = x0;
            float col2 = x0 + 90f;
            float col3 = x0 + 210f;
            float col4 = x0 + 320f;

            if (t is not null && t.Valid)
            {
                G.DrawString($"α = {t.Alpha:0.0000}", font, white, new PointF(col1, y));
                G.DrawString("pb/2V = 0.0000", font, white, new PointF(col2, y));
                G.DrawString($"CL  = {t.CLtot:0.0000}", font, white, new PointF(col3, y));
                G.DrawString($"Cl' = {t.ClPrimeTot:0.0000}", font, white, new PointF(col4, y));
                y += lineH;

                G.DrawString($"β = {t.Beta:0.0000}", font, white, new PointF(col1, y));
                G.DrawString("qc/2V = 0.0000", font, white, new PointF(col2, y));
                G.DrawString($"CY  = {t.CYff:0.0000}", font, white, new PointF(col3, y));
                G.DrawString($"Cm  = {t.Cmtot:0.0000}", font, white, new PointF(col4, y));
                y += lineH;

                G.DrawString($"M = {t.Mach:0.000}", font, white, new PointF(col1, y));
                G.DrawString("rb/2V = 0.0000", font, white, new PointF(col2, y));
                G.DrawString($"CD  = {t.CDtot:0.00000}", font, white, new PointF(col3, y));
                G.DrawString($"Cn' = {t.CnPrimeTot:0.0000}", font, white, new PointF(col4, y));
                y += lineH;

                G.DrawString($"CDi = {t.CDff:0.00000}", font, white, new PointF(col3, y));
                G.DrawString($"e   = {t.SpanEff:0.0000}", font, white, new PointF(col4, y));
                y += lineH;

                G.DrawString($"CDo = {t.CDvis:0.00000}", font, white, new PointF(col3, y));
                y += lineH;
            }
            else
            {
                G.DrawString("(Total-forces coefficients unavailable - see the AVL transcript)", font, Brushes.Gray, new PointF(x0, y));
                y += lineH;
            }

            // Top-right "AVL / Trefftz Plane" label
            string label1 = "AVL";
            string label2 = "Trefftz Plane";
            var s1 = G.MeasureString(label1, font);
            var s2 = G.MeasureString(label2, font);
            G.DrawString(label1, font, white, new PointF(w - s1.Width - 8f, 6f));
            G.DrawString(label2, font, white, new PointF(w - s2.Width - 8f, 6f + lineH));

            // Legend, matching AVL's curve colors/styles
            float legendX1 = w - 130;
            float legendX2 = w - 100;
            float sampleW = 22f;
            float legendY = 6f + lineH * 2f + 10f;

            var pClPerp = new Pen(ThemeMutedColor, 1f) { DashStyle = DashStyle.Dash };
            var pCl = new Pen(Color.OrangeRed, 1f) { DashStyle = DashStyle.Dash };
            var pLoad = new Pen(Color.LimeGreen, 1.5f);
            var pAlphaI = new Pen(Color.DeepSkyBlue, 1f) { DashStyle = DashStyle.Dot };

            G.DrawLine(pClPerp, legendX1, legendY, legendX1 + sampleW, legendY);
            G.DrawString("Cl" + Strings.ChrW(0x22A5), font, new SolidBrush(ThemeMutedColor), new PointF(legendX2, (float)((double)legendY - font.Height / 2d)));
            legendY += lineH;

            G.DrawLine(pCl, legendX1, legendY, legendX1 + sampleW, legendY);
            G.DrawString("Cl", font, Brushes.OrangeRed, new PointF(legendX2, (float)((double)legendY - font.Height / 2d)));
            legendY += lineH;

            G.DrawLine(pLoad, legendX1, legendY, legendX1 + sampleW, legendY);
            G.DrawString("Cl C/Cref", font, Brushes.LimeGreen, new PointF(legendX2, (float)((double)legendY - font.Height / 2d)));
            legendY += lineH;

            G.DrawLine(pAlphaI, legendX1, legendY, legendX1 + sampleW, legendY);
            G.DrawString(Strings.ChrW(0x3B1) + "i", font, Brushes.DeepSkyBlue, new PointF(legendX2, (float)((double)legendY - font.Height / 2d)));
            legendY += lineH;

            return Math.Max(y, legendY) + 6f;
        }

        // Rounds a raw axis step up to a "nice" 1/2/5 x 10^n value so gridlines get
        // clean round-number labels, matching AVL's own axis tick spacing.
        private double NiceStep(double range, int targetTicks)
        {
            if (range <= 0d)
                return 1.0d;
            double rawStep = range / Math.Max(1, targetTicks);
            double mag = Math.Pow(10d, Math.Floor(Math.Log10(rawStep)));
            double norm = rawStep / mag;
            double niceNorm;
            if (norm < 1.5d)
            {
                niceNorm = 1d;
            }
            else if (norm < 3d)
            {
                niceNorm = 2d;
            }
            else if (norm < 7d)
            {
                niceNorm = 5d;
            }
            else
            {
                niceNorm = 10d;
            }
            return niceNorm * mag;
        }

        private void NiceAxisRange(double dataMin, double dataMax, int targetTicks, ref double axisMin, ref double axisMax, ref double tickStep)
        {
            if (dataMin == dataMax)
            {
                dataMin -= 1d;
                dataMax += 1d;
            }
            tickStep = NiceStep(dataMax - dataMin, targetTicks);
            axisMin = Math.Floor(dataMin / tickStep) * tickStep;
            axisMax = Math.Ceiling(dataMax / tickStep) * tickStep;
        }

        // Draws one axis's gridlines + tick labels for the geometry views (pxy/
        // pxz/pyz) using a "nice" round-number step (1/2/5 x 10^n) instead of
        // always spacing by exactly 1 world-unit. The old approach pegged tick
        // spacing directly to gridnumber (the zoom-level control), which is fine
        // for small models but breaks down for large ones: fitting a 40-unit-wide
        // aircraft needs gridnumber ~40, which packed 40+ overlapping labels into
        // the same pixel width AND (via the old baseFontsize/(gridnumber/10)
        // formula) shrank the font toward sub-1pt sizes - illegible, and the tiny
        // fractional font size is also why the text looked "unsmooth"/jagged
        // rather than a ClearType problem (ClearTypeGridFit was already set).
        // This only changes what gets drawn - gridstep/xmin/xmax/etc (the actual
        // zoom/world-extent state that node-dragging and mesh overlay rely on)
        // are untouched.
        // 
        // pixelsPerUnitSigned: +gridstep for horizontal axes (positive = right),
        // -gridstep for vertical axes (positive = up, since screen Y grows down) -
        // this sign convention already matches every existing view's min/max
        // computation (horizontal axes subtract the offset term, vertical axes
        // add it), so callers just pass through whichever gridstep sign applies.
        private void DrawNiceAxisGrid(SvgGraphics G, Font tickFont, bool isVertical, double origin0, float labelOtherAxisPos, double pixelsPerUnitSigned, double worldMin, double worldMax, int viewW, int viewH)
        {
            double lo = Math.Min(worldMin, worldMax);
            double hi = Math.Max(worldMin, worldMax);
            if (hi - lo <= 0d)
                return;
            double tickStep = NiceStep(hi - lo, 8);
            double v = Math.Ceiling(lo / tickStep) * tickStep;
            while (v <= hi + tickStep * 0.001d)
            {
                if (Math.Abs(v) > tickStep * 0.001d)
                {
                    float px = (float)(origin0 + v * pixelsPerUnitSigned);
                    string lbl = v.ToString("0.####");
                    if (isVertical)
                    {
                        G.DrawLine(pGrid, px, 0f, px, viewH);
                        G.DrawString(lbl, tickFont, bAxisText, new PointF(px, labelOtherAxisPos));
                    }
                    else
                    {
                        G.DrawLine(pGrid, 0f, px, viewW, px);
                        G.DrawString(lbl, tickFont, bAxisText, new PointF(labelOtherAxisPos, px));
                    }
                }
                v += tickStep;
            }
        }

        // Draws the two stacked plot regions (Cl-family on a left axis, alpha_i on
        // a right axis) that share the spanwise Y x-axis, matching AVL's own
        // Trefftz Plane window layout.
        private void DrawTrefftzCombinedPlot(SvgGraphics G, Font tickFont, int w, int h, float top)
        {
            float margin = 45f;
            float plotX = margin;
            float plotY = top + 10f;
            float plotW = w - margin - 60f;
            float plotH = h - plotY - 30f;
            if (plotW <= 10f || plotH <= 10f)
                return;

            double yMin = double.MaxValue;
            double yMax = double.MinValue;
            double leftMin = 0d;
            double leftMax = double.MinValue;
            double rightMin = 0d;
            double rightMax = 0d;
            bool any = false;
            foreach (TrefftzSurface surf in _lastTrefftzSurfaces)
            {
                foreach (TrefftzStrip s in surf.Strips)
                {
                    any = true;
                    yMin = Math.Min(yMin, s.Yle);
                    yMax = Math.Max(yMax, s.Yle);
                    leftMax = Math.Max(leftMax, Math.Max(s.ClNorm, Math.Max(s.Cl, s.CCl / _lastTrefftzCref)));
                    leftMin = Math.Min(leftMin, Math.Min(s.ClNorm, Math.Min(s.Cl, s.CCl / _lastTrefftzCref)));
                    rightMin = Math.Min(rightMin, s.Ai);
                    rightMax = Math.Max(rightMax, s.Ai);
                }
            }
            if (!any)
                return;

            var xAxisMin = default(double);
            var xAxisMax = default(double);
            var xStep = default(double);
            NiceAxisRange(yMin, yMax, 6, ref xAxisMin, ref xAxisMax, ref xStep);
            var leftAxisMin = default(double);
            var leftAxisMax = default(double);
            var leftStep = default(double);
            NiceAxisRange(leftMin, leftMax, 5, ref leftAxisMin, ref leftAxisMax, ref leftStep);
            var rightAxisMin = default(double);
            var rightAxisMax = default(double);
            var rightStep = default(double);
            NiceAxisRange(rightMin, rightMax, 4, ref rightAxisMin, ref rightAxisMax, ref rightStep);

            float splitY = plotY + plotH * 0.65f;
            float upperH = splitY - plotY;
            float lowerH = plotY + plotH - splitY;

            var whitePen = new Pen(ThemeAxisColor, 1f);
            var gridPen = new Pen(ThemeGridColor, 1f) { DashStyle = DashStyle.Dash };

            G.DrawRectangle(whitePen, plotX, plotY, plotW, plotH);

            // Vertical gridlines (shared Y/spanwise axis), tick labels near the split boundary
            double xt = xAxisMin;
            while (xt <= xAxisMax + xStep * 0.001d)
            {
                float px = (float)((double)plotX + (xt - xAxisMin) / (xAxisMax - xAxisMin) * (double)plotW);
                G.DrawLine(gridPen, px, plotY, px, plotY + plotH);
                string lbl = xt.ToString("0.0");
                var lblSize = G.MeasureString(lbl, tickFont);
                G.DrawString(lbl, tickFont, new SolidBrush(ThemeAxisColor), new PointF(px - lblSize.Width / 2f, splitY + 2f));
                xt += xStep;
            }
            string yLbl = "Y";
            var yLblSize = G.MeasureString(yLbl, tickFont);
            G.DrawString(yLbl, tickFont, new SolidBrush(ThemeAxisColor), new PointF(plotX + plotW - yLblSize.Width, splitY + tickFont.Height + 4f));

            // Horizontal gridlines, upper region (left axis: Cl-perp / Cl / Cl*C/Cref)
            double lt = leftAxisMin;
            while (lt <= leftAxisMax + leftStep * 0.001d)
            {
                float py = (float)((double)splitY - (lt - leftAxisMin) / (leftAxisMax - leftAxisMin) * (double)upperH);
                G.DrawLine(gridPen, plotX, py, plotX + plotW, py);
                string lbl = lt.ToString("0.0");
                var lblSize = G.MeasureString(lbl, tickFont);
                G.DrawString(lbl, tickFont, new SolidBrush(ThemeAxisColor), new PointF(plotX - lblSize.Width - 4f, py - lblSize.Height / 2f));
                lt += leftStep;
            }

            // Horizontal gridlines, lower region (right axis: alpha_i)
            double rt = rightAxisMin;
            while (rt <= rightAxisMax + rightStep * 0.001d)
            {
                float py = (float)((double)(plotY + plotH) - (rt - rightAxisMin) / (rightAxisMax - rightAxisMin) * (double)lowerH);
                G.DrawLine(gridPen, plotX, py, plotX + plotW, py);
                string lbl = rt.ToString("0.00");
                G.DrawString(lbl, tickFont, Brushes.DeepSkyBlue, new PointF(plotX + plotW + 4f, (float)((double)py - tickFont.Height / 2d)));
                rt += rightStep;
            }

            var pClPerp = new Pen(ThemeMutedColor, 1f) { DashStyle = DashStyle.Dash };
            var pCl = new Pen(Color.OrangeRed, 1f) { DashStyle = DashStyle.Dash };
            var pLoad = new Pen(Color.LimeGreen, 1.5f);
            var pAlphaI = new Pen(Color.DeepSkyBlue, 1f) { DashStyle = DashStyle.Dot };

            foreach (TrefftzSurface surf in _lastTrefftzSurfaces)
            {
                if (surf.Strips.Count == 0)
                    continue;
                var ordered = new List<TrefftzStrip>(surf.Strips);
                ordered.Sort((a, b) => a.Yle.CompareTo(b.Yle));

                var ptsClPerp = new List<PointF>();
                var ptsCl = new List<PointF>();
                var ptsLoad = new List<PointF>();
                var ptsAi = new List<PointF>();
                foreach (TrefftzStrip s in ordered)
                {
                    float px = (float)((double)plotX + (s.Yle - xAxisMin) / (xAxisMax - xAxisMin) * (double)plotW);
                    ptsClPerp.Add(new PointF(px, (float)((double)splitY - (s.ClNorm - leftAxisMin) / (leftAxisMax - leftAxisMin) * (double)upperH)));
                    ptsCl.Add(new PointF(px, (float)((double)splitY - (s.Cl - leftAxisMin) / (leftAxisMax - leftAxisMin) * (double)upperH)));
                    ptsLoad.Add(new PointF(px, (float)((double)splitY - (s.CCl / _lastTrefftzCref - leftAxisMin) / (leftAxisMax - leftAxisMin) * (double)upperH)));
                    ptsAi.Add(new PointF(px, (float)((double)(plotY + plotH) - (s.Ai - rightAxisMin) / (rightAxisMax - rightAxisMin) * (double)lowerH)));
                }
                if (ptsClPerp.Count >= 2)
                {
                    G.DrawLines(pClPerp, ptsClPerp.ToArray());
                    G.DrawLines(pCl, ptsCl.ToArray());
                    G.DrawLines(pLoad, ptsLoad.ToArray());
                    G.DrawLines(pAlphaI, ptsAi.ToArray());
                }
            }
        }

        private void HeavyRender(IProgress<int> progress, CancellationToken token)
        {
            // Fixed, non-shrinking size (still respects the user's baseFontsize +/-
            // control) - previously this divided by gridnumber, so zooming out far
            // enough to fit a large aircraft (gridnumber ~40-50) shrank the font to
            // sub-1pt, which is both illegible and renders jagged regardless of
            // ClearType settings.
            var tickFont = GetTickFont(baseFontsize + 8);
            var axisFont = GetAxisFont();
            gridstep = (int)Math.Round(pxy.Width / (double)gridnumber);
            int x0 = (int)Math.Round(pxy.Width / 2d + xoffset);
            int y0 = (int)Math.Round(pxy.Height / 2d + yoffset);
            int xcount = 0;
            int origin = 0;
            int ycount = 0;
            float curxx;
            float curyx;
            float epsx;
            var pointsx = new List<Node>();
            int radius = 3;
            List<Node> pointsx2;
            // XY plane========================================================================
            var BMP = new Bitmap(pxy.Width, pxy.Height);
            using (var G = new SvgGraphics(pxy.Width, pxy.Height, Graphics.FromImage(BMP), _captureVectors))
            {
                G.SmoothingMode = SmoothingMode.AntiAlias;
                G.TextRenderingHint = TextRenderingHint.AntiAlias;


                // Draw grids
                xcount = (int)Math.Round(pxy.Width / (double)gridstep);
                xmin = -xcount / 2d - xoffset / gridstep;
                xmax = xmin + xcount;
                DrawNiceAxisGrid(G, tickFont, true, x0, y0, gridstep, xmin, xmax, pxy.Width, pxy.Height);
                G.DrawLine(pAxis, x0, 0f, x0, pxy.Height);
                G.DrawString(0.ToString(), tickFont, bAxisText, new PointF(x0, y0));


                gridstep = (int)Math.Round(pxy.Height / (double)gridnumber);
                ycount = (int)Math.Round(pxy.Height / (double)gridstep);
                ymin = -ycount / 2d + yoffset / gridstep;
                ymax = ymin + ycount;
                DrawNiceAxisGrid(G, tickFont, false, y0, x0, -gridstep, ymin, ymax, pxy.Width, pxy.Height);
                G.DrawLine(pAxis, 0f, y0, pxy.Width, y0);



                origin = (int)Math.Round(pxy.Width / 20d);
                G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2f, origin * 2, origin + G.MeasureString("X", axisFont).Height / 2f);
                G.DrawString("X", axisFont, bAxisText, new PointF(origin * 2, origin));
                G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2f, origin, float.Parse((origin * 0.1d).ToString()) + G.MeasureString("X", axisFont).Height / 2f);
                G.DrawString("Y", axisFont, bAxisText, new PointF(origin - G.MeasureString("Y", axisFont).Width, float.Parse((origin * 0.1d).ToString())));

                // Me.Text = "[" + xmin.ToString("0.00") + "," + xmax.ToString("0.00") + "] , [" + ymin.ToString("0.00") + "," + ymax.ToString("0.00") + "]"

                pointsx = new List<Node>();
                foreach (Node p in points)
                {
                    double xscale = p.Point.X * pxy.Width / (xmax - xmin) + pxy.Width / 2d + xoffset;
                    double yscale = -p.Point.Y * pxy.Height / (ymax - ymin) + pxy.Height / 2d + yoffset;
                    if (p.type == Node.NodeType.Geometry)
                    {
                        pointsx.Add(new Node(xscale, yscale, 0d, p.Surface, p.Hovered, p.lineNumber, p.type));
                    }
                }

                curxx = (float)(curX * pxy.Width / (xmax - xmin) + pxy.Width / 2d + xoffset);
                curyx = (float)(-curY * pxy.Height / (ymax - ymin) + pxy.Height / 2d + yoffset);
                // Fixed screen-pixel radius (matches the hover hit-test tolerance) rather than
                // a world-unit eps scaled to pixels, which used to shrink to invisible on large aircraft.
                epsx = 7.0f;

                pointsx2 = new List<Node>(pointsx);
                if (pointsx.Count > 2)
                {
                    while (pointsx.Count > 0)
                    {
                        string name = pointsx[0].Surface;
                        var ps = new List<PointF>();
                        // Debug.WriteLine($"Points count: {pointsx.Count}, Surface: {pointsx(0).Surface}")
                        while (findname(pointsx, name) != -1)
                        {
                            ps.Add(new PointF((float)pointsx[findname(pointsx, name)].Point.X, (float)pointsx[findname(pointsx, name)].Point.Y));
                            pointsx.RemoveAt(findname(pointsx, name));
                        }
                        var ps2 = new List<PointF>();
                        for (int i = 0, loopTo = ps.Count - 1; i <= loopTo; i += 2)
                            ps2.Add(ps[i]);
                        for (int i = ps.Count - 1; i >= 1; i -= 2)
                            ps2.Add(ps[i]);
                        // lblNote.Text += Environment.NewLine + str
                        using (var myPath = new GraphicsPath())
                        {
                            myPath.AddLines(ps2.ToArray());

                            if (name.ToLower().Contains("controlflap") & showControl)
                            {
                                G.DrawPath(pAxis, myPath);
                            }
                            else if (name.ToLower().Contains("controlaileron") & showControl)
                            {
                                G.DrawPath(pAxis, myPath);
                            }
                            else if (name.ToLower().Contains("controlrudder") & showControl)
                            {
                                G.DrawPath(pAxis, myPath);
                            }
                            else if (name.ToLower().Contains("controlelevator") & showControl)
                            {
                                G.DrawPath(pAxis, myPath);
                            }
                            else if (!name.ToLower().Contains("controlflap") & !name.ToLower().Contains("controlaileron") & !name.ToLower().Contains("controlrudder") & !name.ToLower().Contains("controlelevator"))
                            {
                                G.DrawPath(pAxis, myPath);
                            }

                            if (name.ToLower().Contains("controlflap") & showControl)
                            {
                                G.FillPath(bPolyFlap, myPath);
                            }
                            else if (name.ToLower().Contains("controlaileron") & showControl)
                            {
                                G.FillPath(bPolyAilern, myPath);
                            }
                            else if (name.ToLower().Contains("controlrudder") & showControl)
                            {
                                G.FillPath(bPolyRudder, myPath);
                            }
                            else if (name.ToLower().Contains("controlelevator") & showControl)
                            {
                                G.FillPath(bPolyElevator, myPath);
                            }
                            else
                            {
                                G.FillPath(bPolySurface, myPath);
                            }
                        }
                    }
                }

                if (showSection == true)
                {
                    foreach (Node p in pointsx2)
                    {
                        string name = p.Surface;
                        if (showControl | !name.ToLower().Contains("controlflap") & !name.ToLower().Contains("controlaileron") & !name.ToLower().Contains("controlrudder") & !name.ToLower().Contains("controlelevator"))
                        {
                            if (!p.Hovered)
                            {
                                G.FillEllipse(Brushes.Red, new RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2));
                            }
                            else
                            {
                                G.FillEllipse(Brushes.Green, new RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2));
                            }
                        }
                    }
                }

                if (showMass == true & File.Exists(Application.StartupPath + $@"\{projectName}.mass"))
                {
                    double mtotal = 0d;
                    int mcount = 0;
                    foreach (Node p in points)
                    {
                        if (p.type == Node.NodeType.Mass)
                        {
                            mtotal += p.mass;
                            mcount += 1;
                        }
                    }
                    double mavg = mtotal / mcount;

                    foreach (Node p in points)
                    {
                        if (p.type == Node.NodeType.Mass)
                        {
                            // Debug.WriteLine($"Found mass at {p.X}, {p.Y}, {p.Z}")
                            double xmass = (double)(p.X * pxy.Width) / (xmax - xmin) + pxy.Width / 2d + xoffset;
                            double ymass = (double)(-p.Y * pxy.Height) / (ymax - ymin) + pxy.Height / 2d + yoffset;
                            double rmass = p.mass / (mavg * 2d) * radius + radius;
                            // Debug.WriteLine($"Found mass {p.mass} at {p.X}, {p.Y}, {p.Z} -> {rmass} with mavg: {mavg}={mtotal}/{mcount}")
                            if (!p.Hovered)
                            {
                                G.FillEllipse(Brushes.Blue, new RectangleF((float)(xmass - rmass), (float)(ymass - rmass), (float)(rmass * 2d), (float)(rmass * 2d)));
                            }
                            else
                            {
                                G.FillEllipse(Brushes.Green, new RectangleF((float)(xmass - rmass), (float)(ymass - rmass), (float)(rmass * 2d), (float)(rmass * 2d)));
                            }
                        }
                    }

                }

                // Me.Text = curY.ToString + ", " + curyx.ToString + " | " + ymin.ToString + "," + ymax.ToString + " | " + yoffset.ToString
                // draw selection region
                if (showMesh && parsedSurfaces is not null)
                {
                    using (var meshPen = new Pen(Color.FromArgb(140, ThemeMeshColor)) { DashStyle = DashStyle.Dot })
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawMeshForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, meshPen, false);
                            if (su.yDuplicate)
                            {
                                DrawMeshForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, meshPen, true);
                            }
                        }
                    }
                }
                if (showChordline && parsedSurfaces is not null)
                {
                    using (var chordPen = new Pen(Color.DarkOrange, 1.5f))
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawChordlinesForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, chordPen, false);
                            if (su.yDuplicate)
                            {
                                DrawChordlinesForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, chordPen, true);
                            }
                        }
                    }
                }
                if (showControlPoints && parsedSurfaces is not null)
                {
                    foreach (Surface su in parsedSurfaces)
                    {
                        DrawControlPointsForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, Brushes.Magenta, 2.5f, false);
                        if (su.yDuplicate)
                        {
                            DrawControlPointsForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, Brushes.Magenta, 2.5f, true);
                        }
                    }
                }
                if (showAxesTriad)
                {
                    DrawReferenceAxesTriad(G, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, (xmax - xmin) * 0.15d);
                }
                if (showCamberline && parsedSurfaces is not null)
                {
                    string[] avlLines = GetProjectAvlLines();
                    using (var camberPen = new Pen(Color.Purple, 1.5f))
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawCamberlineForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, camberPen, false, avlLines);
                            if (su.yDuplicate)
                            {
                                DrawCamberlineForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, camberPen, true, avlLines);
                            }
                        }
                    }
                }
                if (showBoundLeg && parsedSurfaces is not null)
                {
                    using (var boundPen = new Pen(Color.Firebrick, 1.5f))
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawBoundLegsForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, boundPen, false);
                            if (su.yDuplicate)
                            {
                                DrawBoundLegsForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, boundPen, true);
                            }
                        }
                    }
                }
                if (showTrailingLegs && parsedSurfaces is not null)
                {
                    using (var trailPen = new Pen(Color.SlateGray, 1f) { DashStyle = DashStyle.Dash })
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawTrailingLegsForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, trailPen, false, (xmax - xmin) * 0.3d);
                            if (su.yDuplicate)
                            {
                                DrawTrailingLegsForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, trailPen, true, (xmax - xmin) * 0.3d);
                            }
                        }
                    }
                }
                if (showNormalVector && parsedSurfaces is not null)
                {
                    using (var normPen = new Pen(Color.Cyan, 1.5f))
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawNormalVectorsForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, normPen, false, (xmax - xmin) * 0.04d);
                            if (su.yDuplicate)
                            {
                                DrawNormalVectorsForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, normPen, true, (xmax - xmin) * 0.04d);
                            }
                        }
                    }
                }
                if (showLoadingOverlay)
                {
                    using (var loadPen = new Pen(Color.SpringGreen, 1.5f))
                    {
                        DrawLoadingOverlay(G, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, loadPen);
                    }
                }
                if (showHover)
                {
                    G.DrawRectangle(Pens.Red, curxx - epsx, curyx - epsx, epsx * 2f, epsx * 2f);
                }
                pxySvg = G.GetSvgContent();
                pxyPdf = G.GetPdfContentStream();
            }

            System.Drawing.Image old = null;
            var bmpXY = BMP;
            if (pxy.InvokeRequired)
            {
                pxy.Invoke(() =>
                    {
                        old = pxy.Image;
                        pxy.Image = bmpXY;
                    });
            }
            else
            {
                old = pxy.Image;
                pxy.Image = bmpXY;
            }

            // Check for cancellation request
            // This throws OperationCanceledException if Cancel() was called
            token.ThrowIfCancellationRequested();
            // Report progress back to the UI
            if (progress is not null)
            {
                progress.Report(10);
            }

            if (old is not null)
                old.Dispose();
            // XZ plane========================================================================
            int z0 = (int)Math.Round(pxz.Height / 2d + zoffset);
            double zcount = 0d;
            BMP = new Bitmap(pxz.Width, pxz.Height);
            using (var G = new SvgGraphics(pxz.Width, pxz.Height, Graphics.FromImage(BMP), _captureVectors))
            {
                G.SmoothingMode = SmoothingMode.AntiAlias;
                G.TextRenderingHint = TextRenderingHint.AntiAlias;
                gridstep = (int)Math.Round(pxz.Width / (double)gridnumber);
                x0 = (int)Math.Round(pxz.Width / 2d + xoffset);


                xcount = (int)Math.Round(pxz.Width / (double)gridstep);
                xmin = -xcount / 2d - xoffset / gridstep;
                xmax = xmin + xcount;
                DrawNiceAxisGrid(G, tickFont, true, x0, z0, gridstep, xmin, xmax, pxz.Width, pxz.Height);
                G.DrawLine(pAxis, x0, 0f, x0, pxz.Height);
                G.DrawString(0.ToString(), tickFont, bAxisText, new PointF(x0, z0));


                gridstep = (int)Math.Round(pxz.Height / (double)gridnumber);
                zcount = pxz.Height / (double)gridstep;
                zmin = -zcount / 2d + zoffset / gridstep;
                zmax = zmin + zcount;
                DrawNiceAxisGrid(G, tickFont, false, z0, x0, -gridstep, zmin, zmax, pxz.Width, pxz.Height);
                G.DrawLine(pAxis, 0f, z0, pxz.Width, z0);

                origin = (int)Math.Round(pxz.Width / 20d);
                G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2f, origin * 2, origin + G.MeasureString("X", axisFont).Height / 2f);
                G.DrawString("X", axisFont, bAxisText, new PointF(origin * 2, origin));
                G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2f, origin, float.Parse((origin * 0.1d).ToString()) + G.MeasureString("X", axisFont).Height / 2f);
                G.DrawString("Z", axisFont, bAxisText, new PointF(origin - G.MeasureString("Z", axisFont).Width, float.Parse((origin * 0.1d).ToString())));


                // Me.Text = "[" + xmin.ToString("0.00") + "," + xmax.ToString("0.00") + "] , [" + ymin.ToString("0.00") + "," + ymax.ToString("0.00") + "]"

                // curyx = CSng(-curY * (pxz.Height) / (ymax - ymin) + (pxz.Height / 2) + yoffset)
                // Dim curzx = CSng(curZ * (pxz.Width) / (zmax - zmin) + (pxz.Width / 2) + zoffset)
                curxx = (float)(curX * pxz.Width / (xmax - xmin) + pxz.Width / 2d + xoffset);
                float curzx = (float)(-curZ * pxz.Height / (zmax - zmin) + pxz.Height / 2d + zoffset);
                // Dim epsy = CSng(eps * (pxz.Width) / (ymax - ymin))


                // radius = 5
                pointsx = new List<Node>();
                foreach (Node p in points)
                {
                    double xscale = p.Point.X * pxz.Width / (xmax - xmin) + pxz.Width / 2d + xoffset;
                    double zscale = -p.Point.Z * pxz.Height / (zmax - zmin) + pxz.Height / 2d + zoffset;
                    if (p.type == Node.NodeType.Geometry)
                    {
                        pointsx.Add(new Node(xscale, zscale, 0d, p.Surface, p.Hovered, p.lineNumber, p.type));
                    }
                }
                pointsx2 = new List<Node>(pointsx);
                if (pointsx.Count > 2)
                {
                    while (pointsx.Count > 0)
                    {
                        string name = pointsx[0].Surface;
                        var ps = new List<PointF>();
                        while (findname(pointsx, name) != -1)
                        {
                            ps.Add(new PointF((float)pointsx[findname(pointsx, name)].Point.X, (float)pointsx[findname(pointsx, name)].Point.Y));
                            pointsx.RemoveAt(findname(pointsx, name));
                        }
                        // AppMessageBox.Show(ps.Count)
                        // Dim str As String = ""
                        // For Each p As PointF In ps
                        // str += " | " + "(" + p.X.ToString + "," + p.Y.ToString + ")"
                        // Next
                        var ps2 = new List<PointF>();
                        for (int i = 0, loopTo1 = ps.Count - 1; i <= loopTo1; i += 2)
                            ps2.Add(ps[i]);
                        for (int i = ps.Count - 1; i >= 1; i -= 2)
                            ps2.Add(ps[i]);
                        // lblNote.Text += Environment.NewLine + str
                        using (var myPath = new GraphicsPath())
                        {
                            myPath.AddLines(ps2.ToArray());
                            if (name.ToLower().Contains("controlflap") & showControl)
                            {
                                G.DrawPath(pAxis, myPath);
                            }
                            else if (name.ToLower().Contains("controlaileron") & showControl)
                            {
                                G.DrawPath(pAxis, myPath);
                            }
                            else if (name.ToLower().Contains("controlrudder") & showControl)
                            {
                                G.DrawPath(pAxis, myPath);
                            }
                            else if (name.ToLower().Contains("controlelevator") & showControl)
                            {
                                G.DrawPath(pAxis, myPath);
                            }
                            else if (!name.ToLower().Contains("controlflap") & !name.ToLower().Contains("controlaileron") & !name.ToLower().Contains("controlrudder") & !name.ToLower().Contains("controlelevator"))
                            {
                                G.DrawPath(pAxis, myPath);
                            }

                            if (name.ToLower().Contains("controlflap") & showControl)
                            {
                                G.FillPath(bPolyFlap, myPath);
                            }
                            else if (name.ToLower().Contains("controlaileron") & showControl)
                            {
                                G.FillPath(bPolyAilern, myPath);
                            }
                            else if (name.ToLower().Contains("controlrudder") & showControl)
                            {
                                G.FillPath(bPolyRudder, myPath);
                            }
                            else if (name.ToLower().Contains("controlelevator") & showControl)
                            {
                                G.FillPath(bPolyElevator, myPath);
                            }
                            else
                            {
                                G.FillPath(bPolySurface, myPath);
                            }
                        }
                    }
                }

                if (showSection == true)
                {
                    foreach (Node p in pointsx2)
                    {
                        string name = p.Surface;
                        if (showControl | !name.ToLower().Contains("controlflap") & !name.ToLower().Contains("controlaileron") & !name.ToLower().Contains("controlrudder") & !name.ToLower().Contains("controlelevator"))
                        {

                            if (!p.Hovered)
                            {
                                G.FillEllipse(Brushes.Red, new RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2));
                            }
                            else
                            {
                                G.FillEllipse(Brushes.Green, new RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2));
                            }
                        }
                    }
                }


                if (showMass == true & File.Exists(Application.StartupPath + $@"\{projectName}.mass"))
                {
                    float mtotal = 0f;
                    int mcount = 0;
                    foreach (Node p in points)
                    {
                        if (p.type == Node.NodeType.Mass)
                        {
                            mtotal += p.mass;
                            mcount += 1;
                        }
                    }
                    float mavg = mtotal / mcount;

                    foreach (Node p in points)
                    {
                        if (p.type == Node.NodeType.Mass)
                        {
                            double xmass = (double)(p.X * pxz.Width) / (xmax - xmin) + pxz.Width / 2d + xoffset;
                            double zmass = (double)(-p.Z * pxz.Height) / (zmax - zmin) + pxz.Height / 2d + zoffset;
                            float rmass = p.mass / (mavg * 2f) * radius + radius;
                            G.FillEllipse(Brushes.Blue, new RectangleF((float)(xmass - (double)rmass), (float)(zmass - (double)rmass), rmass * 2f, rmass * 2f));
                        }
                    }

                }

                // draw selection region
                if (showMesh && parsedSurfaces is not null)
                {
                    using (var meshPen = new Pen(Color.FromArgb(140, ThemeMeshColor)) { DashStyle = DashStyle.Dot })
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawMeshForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, meshPen, false);
                            if (su.yDuplicate)
                            {
                                DrawMeshForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, meshPen, true);
                            }
                        }
                    }
                }
                if (showChordline && parsedSurfaces is not null)
                {
                    using (var chordPen = new Pen(Color.DarkOrange, 1.5f))
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawChordlinesForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, chordPen, false);
                            if (su.yDuplicate)
                            {
                                DrawChordlinesForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, chordPen, true);
                            }
                        }
                    }
                }
                if (showControlPoints && parsedSurfaces is not null)
                {
                    foreach (Surface su in parsedSurfaces)
                    {
                        DrawControlPointsForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, Brushes.Magenta, 2.5f, false);
                        if (su.yDuplicate)
                        {
                            DrawControlPointsForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, Brushes.Magenta, 2.5f, true);
                        }
                    }
                }
                if (showAxesTriad)
                {
                    DrawReferenceAxesTriad(G, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, (xmax - xmin) * 0.15d);
                }
                if (showCamberline && parsedSurfaces is not null)
                {
                    string[] avlLines = GetProjectAvlLines();
                    using (var camberPen = new Pen(Color.Purple, 1.5f))
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawCamberlineForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, camberPen, false, avlLines);
                            if (su.yDuplicate)
                            {
                                DrawCamberlineForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, camberPen, true, avlLines);
                            }
                        }
                    }
                }
                if (showBoundLeg && parsedSurfaces is not null)
                {
                    using (var boundPen = new Pen(Color.Firebrick, 1.5f))
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawBoundLegsForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, boundPen, false);
                            if (su.yDuplicate)
                            {
                                DrawBoundLegsForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, boundPen, true);
                            }
                        }
                    }
                }
                if (showTrailingLegs && parsedSurfaces is not null)
                {
                    using (var trailPen = new Pen(Color.SlateGray, 1f) { DashStyle = DashStyle.Dash })
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawTrailingLegsForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, trailPen, false, (xmax - xmin) * 0.3d);
                            if (su.yDuplicate)
                            {
                                DrawTrailingLegsForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, trailPen, true, (xmax - xmin) * 0.3d);
                            }
                        }
                    }
                }
                if (showNormalVector && parsedSurfaces is not null)
                {
                    using (var normPen = new Pen(Color.Cyan, 1.5f))
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawNormalVectorsForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, normPen, false, (xmax - xmin) * 0.04d);
                            if (su.yDuplicate)
                            {
                                DrawNormalVectorsForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, normPen, true, (xmax - xmin) * 0.04d);
                            }
                        }
                    }
                }
                if (showLoadingOverlay)
                {
                    using (var loadPen = new Pen(Color.SpringGreen, 1.5f))
                    {
                        DrawLoadingOverlay(G, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, loadPen);
                    }
                }
                if (showHover)
                {
                    G.DrawRectangle(Pens.Red, curxx - epsx, curzx - epsx, epsx * 2f, epsx * 2f);
                }
                pxzSvg = G.GetSvgContent();
                pxzPdf = G.GetPdfContentStream();
            }

            var bmpXZ = BMP;
            if (pxz.InvokeRequired)
            {
                pxz.Invoke(() =>
                    {
                        old = pxz.Image;
                        pxz.Image = bmpXZ;
                    });
            }
            else
            {
                old = pxz.Image;
                pxz.Image = bmpXZ;
            }
            if (old is not null)
                old.Dispose();
            // YZ plane========================================================================
            BMP = new Bitmap(pyz.Width, pyz.Height);
            using (var G = new SvgGraphics(pyz.Width, pyz.Height, Graphics.FromImage(BMP), _captureVectors))
            {
                G.SmoothingMode = SmoothingMode.AntiAlias;
                G.TextRenderingHint = TextRenderingHint.AntiAlias;
                gridstep = (int)Math.Round(pyz.Width / (double)gridnumber);
                y0 = (int)Math.Round(pyz.Width / 2d + yoffset);
                z0 = (int)Math.Round(pyz.Height / 2d + zoffset);


                ycount = (int)Math.Round(pyz.Width / (double)gridstep);
                ymin = -ycount / 2d - yoffset / gridstep;
                ymax = ymin + ycount;
                DrawNiceAxisGrid(G, tickFont, true, y0, z0, gridstep, ymin, ymax, pyz.Width, pyz.Height);
                G.DrawLine(pAxis, y0, 0f, y0, pyz.Height);
                G.DrawString(0.ToString(), tickFont, bAxisText, new PointF(y0, z0));


                gridstep = (int)Math.Round(pyz.Height / (double)gridnumber);
                zcount = pyz.Height / (double)gridstep;
                zmin = -zcount / 2d + zoffset / gridstep;
                zmax = zmin + zcount;
                DrawNiceAxisGrid(G, tickFont, false, z0, y0, -gridstep, zmin, zmax, pyz.Width, pyz.Height);
                G.DrawLine(pAxis, 0f, z0, pyz.Width, z0);

                origin = (int)Math.Round(pyz.Width / 20d);
                G.DrawLine(pAxis, origin * 2, origin + G.MeasureString("Z", axisFont).Height / 2f, origin, origin + G.MeasureString("Z", axisFont).Height / 2f);
                G.DrawString("Y", axisFont, bAxisText, new PointF(origin - G.MeasureString("Y", axisFont).Width, origin));
                G.DrawLine(pAxis, origin * 2, origin + G.MeasureString("Z", axisFont).Height / 2f, origin * 2, float.Parse((origin * 0.1d).ToString()) + G.MeasureString("Z", axisFont).Height / 2f);
                G.DrawString("Z", axisFont, bAxisText, new PointF(origin * 2, float.Parse((origin * 0.1d).ToString())));


                // Me.Text = "[" + xmin.ToString("0.00") + "," + xmax.ToString("0.00") + "] , [" + ymin.ToString("0.00") + "," + ymax.ToString("0.00") + "]"
                curyx = (float)(curY * pyz.Width / (ymax - ymin) + pyz.Width / 2d + yoffset);
                float curzx = (float)(-curZ * pyz.Height / (zmax - zmin) + pyz.Height / 2d + zoffset);
                // Dim epsy = CSng(eps * (pxz.Width) / (ymax - ymin))

                // radius = 5
                pointsx = new List<Node>();
                foreach (Node p in points)
                {
                    double yscale = -p.Point.Y * pyz.Width / (ymax - ymin) + pyz.Width / 2d + yoffset;
                    double zscale = -p.Point.Z * pyz.Height / (zmax - zmin) + pyz.Height / 2d + zoffset;
                    if (p.type == Node.NodeType.Geometry)
                    {
                        pointsx.Add(new Node(yscale, zscale, 0d, p.Surface, p.Hovered, p.lineNumber, p.type));
                    }
                }
                pointsx2 = new List<Node>(pointsx);
                if (pointsx.Count > 2)
                {
                    while (pointsx.Count > 0)
                    {
                        string name = pointsx[0].Surface;
                        var ps = new List<PointF>();
                        while (findname(pointsx, name) != -1)
                        {
                            ps.Add(new PointF((float)pointsx[findname(pointsx, name)].Point.X, (float)pointsx[findname(pointsx, name)].Point.Y));
                            pointsx.RemoveAt(findname(pointsx, name));
                        }
                        // AppMessageBox.Show(ps.Count)
                        // Dim str As String = ""
                        // For Each p As PointF In ps
                        // str += " | " + "(" + p.X.ToString + "," + p.Y.ToString + ")"
                        // Next
                        var ps2 = new List<PointF>();
                        for (int i = 0, loopTo2 = ps.Count - 1; i <= loopTo2; i += 2)
                            ps2.Add(ps[i]);
                        for (int i = ps.Count - 1; i >= 1; i -= 2)
                            ps2.Add(ps[i]);
                        // lblNote.Text += Environment.NewLine + str
                        using (var myPath = new GraphicsPath())
                        {
                            myPath.AddLines(ps2.ToArray());
                            if (name.ToLower().Contains("controlflap") & showControl)
                            {
                                G.DrawPath(pAxis, myPath);
                            }
                            else if (name.ToLower().Contains("controlaileron") & showControl)
                            {
                                G.DrawPath(pAxis, myPath);
                            }
                            else if (name.ToLower().Contains("controlrudder") & showControl)
                            {
                                G.DrawPath(pAxis, myPath);
                            }
                            else if (name.ToLower().Contains("controlelevator") & showControl)
                            {
                                G.DrawPath(pAxis, myPath);
                            }
                            else if (!name.ToLower().Contains("controlflap") & !name.ToLower().Contains("controlaileron") & !name.ToLower().Contains("controlrudder") & !name.ToLower().Contains("controlelevator"))
                            {
                                G.DrawPath(pAxis, myPath);
                            }

                            if (name.ToLower().Contains("controlflap") & showControl)
                            {
                                G.FillPath(bPolyFlap, myPath);
                            }
                            else if (name.ToLower().Contains("controlaileron") & showControl)
                            {
                                G.FillPath(bPolyAilern, myPath);
                            }
                            else if (name.ToLower().Contains("controlrudder") & showControl)
                            {
                                G.FillPath(bPolyRudder, myPath);
                            }
                            else if (name.ToLower().Contains("controlelevator") & showControl)
                            {
                                G.FillPath(bPolyElevator, myPath);
                            }
                            else
                            {
                                G.FillPath(bPolySurface, myPath);
                            }
                        }
                    }
                }

                if (showSection == true)
                {

                    foreach (Node p in pointsx2)
                    {
                        string name = p.Surface;
                        if (showControl | !name.ToLower().Contains("controlflap") & !name.ToLower().Contains("controlaileron") & !name.ToLower().Contains("controlrudder") & !name.ToLower().Contains("controlelevator"))
                        {

                            if (!p.Hovered)
                            {
                                G.FillEllipse(Brushes.Red, new RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2));
                            }
                            else
                            {
                                G.FillEllipse(Brushes.Green, new RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2));
                            }
                        }
                    }
                }


                if (showMass == true & File.Exists(Application.StartupPath + $@"\{projectName}.mass"))
                {
                    float mtotal = 0f;
                    int mcount = 0;
                    foreach (Node p in points)
                    {
                        if (p.type == Node.NodeType.Mass)
                        {
                            mtotal += p.mass;
                            mcount += 1;
                        }
                    }
                    float mavg = mtotal / mcount;

                    foreach (Node p in points)
                    {
                        if (p.type == Node.NodeType.Mass)
                        {
                            double ymass = (double)(-p.Y * pyz.Width) / (ymax - ymin) + pyz.Width / 2d + yoffset;
                            double zmass = (double)(-p.Z * pyz.Height) / (zmax - zmin) + pyz.Height / 2d + zoffset;
                            float rmass = p.mass / (mavg * 2f) * radius + radius;
                            G.FillEllipse(Brushes.Blue, new RectangleF((float)(ymass - (double)rmass), (float)(zmass - (double)rmass), rmass * 2f, rmass * 2f));
                        }
                    }

                }

                // draw selection region
                if (showMesh && parsedSurfaces is not null)
                {
                    using (var meshPen = new Pen(Color.FromArgb(140, ThemeMeshColor)) { DashStyle = DashStyle.Dot })
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawMeshForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, meshPen, false);
                            if (su.yDuplicate)
                            {
                                DrawMeshForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, meshPen, true);
                            }
                        }
                    }
                }
                if (showChordline && parsedSurfaces is not null)
                {
                    using (var chordPen = new Pen(Color.DarkOrange, 1.5f))
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawChordlinesForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, chordPen, false);
                            if (su.yDuplicate)
                            {
                                DrawChordlinesForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, chordPen, true);
                            }
                        }
                    }
                }
                if (showControlPoints && parsedSurfaces is not null)
                {
                    foreach (Surface su in parsedSurfaces)
                    {
                        DrawControlPointsForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, Brushes.Magenta, 2.5f, false);
                        if (su.yDuplicate)
                        {
                            DrawControlPointsForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, Brushes.Magenta, 2.5f, true);
                        }
                    }
                }
                if (showAxesTriad)
                {
                    DrawReferenceAxesTriad(G, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, (ymax - ymin) * 0.15d);
                }
                if (showCamberline && parsedSurfaces is not null)
                {
                    string[] avlLines = GetProjectAvlLines();
                    using (var camberPen = new Pen(Color.Purple, 1.5f))
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawCamberlineForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, camberPen, false, avlLines);
                            if (su.yDuplicate)
                            {
                                DrawCamberlineForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, camberPen, true, avlLines);
                            }
                        }
                    }
                }
                if (showBoundLeg && parsedSurfaces is not null)
                {
                    using (var boundPen = new Pen(Color.Firebrick, 1.5f))
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawBoundLegsForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, boundPen, false);
                            if (su.yDuplicate)
                            {
                                DrawBoundLegsForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, boundPen, true);
                            }
                        }
                    }
                }
                if (showTrailingLegs && parsedSurfaces is not null)
                {
                    using (var trailPen = new Pen(Color.SlateGray, 1f) { DashStyle = DashStyle.Dash })
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawTrailingLegsForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, trailPen, false, (ymax - ymin) * 0.3d);
                            if (su.yDuplicate)
                            {
                                DrawTrailingLegsForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, trailPen, true, (ymax - ymin) * 0.3d);
                            }
                        }
                    }
                }
                if (showNormalVector && parsedSurfaces is not null)
                {
                    using (var normPen = new Pen(Color.Cyan, 1.5f))
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawNormalVectorsForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, normPen, false, (ymax - ymin) * 0.04d);
                            if (su.yDuplicate)
                            {
                                DrawNormalVectorsForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, normPen, true, (ymax - ymin) * 0.04d);
                            }
                        }
                    }
                }
                if (showLoadingOverlay)
                {
                    using (var loadPen = new Pen(Color.SpringGreen, 1.5f))
                    {
                        DrawLoadingOverlay(G, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, loadPen);
                    }
                }
                if (showHover)
                {
                    G.DrawRectangle(Pens.Red, curyx - epsx, curzx - epsx, epsx * 2f, epsx * 2f);
                }
                pyzSvg = G.GetSvgContent();
                pyzPdf = G.GetPdfContentStream();
            }

            var bmpYZ = BMP;
            if (pyz.InvokeRequired)
            {
                pyz.Invoke(() =>
                    {
                        old = pyz.Image;
                        pyz.Image = bmpYZ;
                    });
            }
            else
            {
                old = pyz.Image;
                pyz.Image = bmpYZ;
            }
            if (old is not null)
                old.Dispose();

            // 3D Plane =======================================================================
            if (show3D)
            {
                // 1. Setup Graphics
                BMP = new Bitmap(p3d.Width, p3d.Height);
                using (var G = new SvgGraphics(p3d.Width, p3d.Height, Graphics.FromImage(BMP), _captureVectors))
                {
                    G.SmoothingMode = SmoothingMode.AntiAlias;
                    G.TextRenderingHint = TextRenderingHint.AntiAlias;

                    // 2. Calculate Grid Bounds based on Geometry
                    float gMinX = -10;
                    float gMaxX = 10f;
                    float gMinY = -10;
                    float gMaxY = 10f;

                    var geoNodes = points.Where(n => n.@type == Node.NodeType.Geometry).ToList();
                    if (geoNodes.Count > 0)
                    {
                        gMinX = geoNodes.Min(n => n.X);
                        gMaxX = geoNodes.Max(n => n.X);
                        gMinY = geoNodes.Min(n => n.Y);
                        gMaxY = geoNodes.Max(n => n.Y);
                    }

                    // The model's bounding-box center - everything (grid, axis
                    // gizmo, geometry, mass points, and the mesh overlay via
                    // WorldToScreen's "3D" case) rotates around THIS instead of
                    // the world origin, so dragging orbits the aircraft in place
                    // rather than swinging it through a wide arc whenever its
                    // geometry isn't centered near (0,0,0) (e.g. a nonzero Xref).
                    // Stored in module fields (view3DCenterX/Y/Z) since
                    // DrawMeshForSurface/WorldToScreen are separate functions.
                    view3DCenterX = 0f;
                    view3DCenterY = 0f;
                    view3DCenterZ = 0f;
                    if (geoNodes.Count > 0)
                    {
                        view3DCenterX = (gMinX + gMaxX) / 2.0f;
                        view3DCenterY = (gMinY + gMaxY) / 2.0f;
                        view3DCenterZ = (geoNodes.Min(n => n.Z) + geoNodes.Max(n => n.Z)) / 2.0f;
                    }
                    // Order matters here: Z (Middle-drag) is applied FIRST, before
                    // Y (viewAlpha) and X (viewBeta). With the old Y->X->Z order,
                    // Z-rotation was applied in the frame AFTER pitch had already
                    // tilted it - since the default view starts pitched 120°
                    // (Fit3D/Set3DView "Default"), Middle-drag no longer spun the
                    // model around its true vertical axis, it spun around
                    // whatever direction that axis had been tilted to, which
                    // looked like an unpredictable diagonal rotation instead of
                    // "no way to rotate around Z" at all. Applying Z first means
                    // it always rotates around the model's own, untilted Z axis
                    // regardless of the current pitch/yaw, and does not change
                    // the default view's appearance (a 0° rotation is the
                    // identity regardless of when it's applied, so at startup -
                    // only viewBeta nonzero - this reorder is a no-op visually).
                    Func<Node, Node> RotateAroundCenter = (n) =>
    {
        var shifted = new Node(new Point3D((double)(n.X - view3DCenterX), (double)(n.Y - view3DCenterY), (double)(n.Z - view3DCenterZ)), n.Surface, n.Hovered, n.lineNumber, n.type, n.mass);
        return shifted.RotateZ(viewGamma).RotateY(viewAlpha).RotateX(viewBeta);
    };

                    // --- NEW STEP CALCULATION LOGIC ---
                    // Calculate the total span of the geometry
                    float span = Math.Max(gMaxX - gMinX, gMaxY - gMinY);
                    if (span == 0f)
                        span = 10f;

                    // Calculate total divisions needed (Major grids * Mini grids) to match 2D view logic
                    int totalDivisions = gridnumber;
                    if (gridnumbermini > 1)
                        totalDivisions = gridnumber * gridnumbermini;
                    if (totalDivisions < 1)
                        totalDivisions = 1;

                    float rawStep = span / totalDivisions;

                    // Snap rawStep to specific "nice" increments including 0.25
                    float step3D;

                    if ((double)rawStep <= 0.1d)
                    {
                        step3D = 0.1f;
                    }
                    else if ((double)rawStep <= 0.25d)
                    {
                        step3D = 0.25f;
                    }
                    else if ((double)rawStep <= 0.5d)
                    {
                        step3D = 0.5f;
                    }
                    else if ((double)rawStep <= 1.0d)
                    {
                        step3D = 1.0f;
                    }
                    else if ((double)rawStep <= 2.0d)
                    {
                        step3D = 2.0f;
                    }
                    else if ((double)rawStep <= 2.5d)
                    {
                        step3D = 2.5f;
                    }
                    else if ((double)rawStep <= 5.0d)
                    {
                        step3D = 5.0f;
                    }
                    else
                    {
                        // For larger steps, round to nearest 5 or 10
                        step3D = (float)(Math.Ceiling((double)(rawStep / 5f)) * 5d);
                    }
                    // ----------------------------------

                    // Expand bounds slightly to align with step
                    gMinX -= step3D;
                    gMaxX += step3D;
                    gMinY -= step3D;
                    gMaxY += step3D;

                    // Use Decimal for loop to prevent floating point errors (like 0.75000001)
                    decimal startX = (decimal)(Math.Floor((double)(gMinX / step3D)) * (double)step3D);
                    decimal endX = (decimal)(Math.Ceiling((double)(gMaxX / step3D)) * (double)step3D);
                    decimal startY = (decimal)(Math.Floor((double)(gMinY / step3D)) * (double)step3D);
                    decimal endY = (decimal)(Math.Ceiling((double)(gMaxY / step3D)) * (double)step3D);
                    decimal stepDec = (decimal)step3D;

                    // 3. Draw Grid Lines & Ticks (Z=0 Plane)
                    var gridPen = new Pen(ThemeGridColor) { DashStyle = DashStyle.Dash };

                    // --- Draw Constant X lines ---
                    for (decimal xVal = startX, loopTo3 = endX; stepDec >= 0 ? xVal <= loopTo3 : xVal >= loopTo3; xVal += stepDec)
                    {
                        float xSng = (float)xVal;

                        var pStart = new Node((double)xSng, (double)(float)startY, 0d, "", false, 0, Node.NodeType.Geometry);
                        var pEnd = new Node((double)xSng, (double)(float)endY, 0d, "", false, 0, Node.NodeType.Geometry);

                        var v1 = RotateAroundCenter(pStart).Project(p3d.Width, p3d.Height, viewDist, viewFOV);
                        var v2 = RotateAroundCenter(pEnd).Project(p3d.Width, p3d.Height, viewDist, viewFOV);

                        if (v1.Visible & v2.Visible)
                        {
                            // Draw darker line for Zero, lighter for others
                            G.DrawLine((double)Math.Abs(xSng) < 0.001d ? pAxis : gridPen, v1.X, v1.Y, v2.X, v2.Y);

                            // Text Label
                            var pTick = new Node((double)xSng, 0d, 0d, "", false, 0, Node.NodeType.Geometry);
                            var tTick = RotateAroundCenter(pTick).Project(p3d.Width, p3d.Height, viewDist, viewFOV);

                            if (tTick.Visible)
                            {
                                G.DrawString(xSng.ToString("0.##"), tickFont, bAxisText, tTick.X + 2f, tTick.Y + 2f);
                            }
                        }
                    }

                    // --- Draw Constant Y lines ---
                    for (decimal yVal = startY, loopTo4 = endY; stepDec >= 0 ? yVal <= loopTo4 : yVal >= loopTo4; yVal += stepDec)
                    {
                        float ySng = (float)yVal;

                        var pStart = new Node((double)(float)startX, (double)ySng, 0d, "", false, 0, Node.NodeType.Geometry);
                        var pEnd = new Node((double)(float)endX, (double)ySng, 0d, "", false, 0, Node.NodeType.Geometry);

                        var v1 = RotateAroundCenter(pStart).Project(p3d.Width, p3d.Height, viewDist, viewFOV);
                        var v2 = RotateAroundCenter(pEnd).Project(p3d.Width, p3d.Height, viewDist, viewFOV);

                        if (v1.Visible & v2.Visible)
                        {
                            G.DrawLine((double)Math.Abs(ySng) < 0.001d ? pAxis : gridPen, v1.X, v1.Y, v2.X, v2.Y);

                            // Text Label
                            var pTick = new Node(0d, (double)ySng, 0d, "", false, 0, Node.NodeType.Geometry);
                            var tTick = RotateAroundCenter(pTick).Project(p3d.Width, p3d.Height, viewDist, viewFOV);

                            if (tTick.Visible)
                            {
                                G.DrawString(ySng.ToString("0.##"), tickFont, bAxisText, tTick.X + 2f, tTick.Y + 2f);
                            }
                        }
                    }

                    // 4. Draw 3D Axes (Origin Gizmo)
                    var originp = new Node(0d, 0d, 0d, "", false, 0, Node.NodeType.Geometry);
                    float axisLen = (float)((double)step3D * 1.5d); // Scale axis to match grid
                    var axX = new Node((double)axisLen, 0d, 0d, "", false, 0, Node.NodeType.Geometry);
                    var axY = new Node(0d, (double)axisLen, 0d, "", false, 0, Node.NodeType.Geometry);
                    var axZ = new Node(0d, 0d, (double)axisLen, "", false, 0, Node.NodeType.Geometry);

                    var tOr = RotateAroundCenter(originp).Project(p3d.Width, p3d.Height, viewDist, viewFOV);
                    var tX = RotateAroundCenter(axX).Project(p3d.Width, p3d.Height, viewDist, viewFOV);
                    var tY = RotateAroundCenter(axY).Project(p3d.Width, p3d.Height, viewDist, viewFOV);
                    var tZ = RotateAroundCenter(axZ).Project(p3d.Width, p3d.Height, viewDist, viewFOV);

                    G.DrawLine(new Pen(Color.Red, 2f), tOr.X, tOr.Y, tX.X, tX.Y);
                    G.DrawString("X", axisFont, Brushes.Red, tX.X, tX.Y);
                    G.DrawLine(new Pen(Color.Green, 2f), tOr.X, tOr.Y, tY.X, tY.Y);
                    G.DrawString("Y", axisFont, Brushes.Green, tY.X, tY.Y);
                    G.DrawLine(new Pen(Color.Blue, 2f), tOr.X, tOr.Y, tZ.X, tZ.Y);
                    G.DrawString("Z", axisFont, Brushes.Blue, tZ.X, tZ.Y);

                    // 5. Draw Geometry + Mass Points, back-to-front by depth
                    // (painter's algorithm). Previously each surface polygon was
                    // filled immediately in whatever order it appeared in `points`
                    // (file/parse order), and mass points were always drawn in a
                    // separate pass AFTER every surface - so occlusion was
                    // essentially arbitrary: rotate the view and a surface (or
                    // mass marker) that should be hidden behind another could
                    // render on top of it instead. Collecting every fill/draw
                    // operation as a (depth, action) pair and executing them
                    // farthest-first fixes that. Depth uses the rotated (not yet
                    // projected) Z - Project() preserves it on the returned Node,
                    // and per the projection formula (scale = fov/(viewDist+Z))
                    // a LARGER Z is farther from the camera, so sorting descending
                    // draws far things first and near things last (on top).
                    var drawQueue = new List<(float Depth, Action Draw)>();
                    // Control-surface fills ("controlaileron" etc.) are coplanar
                    // with (a chordwise subset of) their own parent wing panel -
                    // same Z region, not a genuinely separate depth. Depth-sorting
                    // them against that parent is essentially a coin flip on
                    // floating-point noise, which is what let the opaque parent
                    // panel paint over its own control-surface highlight. They
                    // always draw last instead, same as the file-order behavior
                    // this is replacing gave for free (control points are always
                    // appended right after their section in findPoints()).
                    var controlOverlayQueue = new List<Action>();

                    var points3D = new List<Node>();
                    foreach (Node p in points)
                    {
                        var rNode = RotateAroundCenter(p);
                        points3D.Add(rNode.Project(p3d.Width, p3d.Height, viewDist, viewFOV));
                    }

                    var points3DGeo = points3D.Where(n => n.@type == Node.NodeType.Geometry).ToList();

                    if (points3DGeo.Count > 2)
                    {
                        while (points3DGeo.Count > 0)
                        {
                            string name = points3DGeo[0].Surface;
                            var ps = new List<PointF>();
                            var zs = new List<float>();
                            bool isPolygonVisible = true;

                            while (findname(points3DGeo, name) != -1)
                            {
                                int idx = findname(points3DGeo, name);
                                var currentNode = points3DGeo[idx];
                                ps.Add(new PointF(currentNode.X, currentNode.Y));
                                zs.Add(currentNode.Z);
                                if (currentNode.Visible == false)
                                    isPolygonVisible = false;
                                points3DGeo.RemoveAt(idx);
                            }

                            var ps2 = new List<PointF>();
                            for (int k = 0, loopTo5 = ps.Count - 1; k <= loopTo5; k += 2)
                                ps2.Add(ps[k]);
                            for (int k = ps.Count - 1; k >= 1; k -= 2)
                                ps2.Add(ps[k]);

                            if (ps2.Count > 1 & isPolygonVisible)
                            {
                                string capturedName = name;
                                var capturedPs2 = ps2;


                                Action fillAction = () => { using (var myPath = new GraphicsPath()) { myPath.AddLines(capturedPs2.ToArray()); var fillBrush = bPolySurface; if (capturedName.ToLower().Contains("controlflap")) fillBrush = bPolyFlap; if (capturedName.ToLower().Contains("controlaileron")) fillBrush = bPolyAilern; if (capturedName.ToLower().Contains("controlrudder")) fillBrush = bPolyRudder; if (capturedName.ToLower().Contains("controlelevator")) fillBrush = bPolyElevator; if (showControl | !capturedName.ToLower().Contains("control")) { G.FillPath(fillBrush, myPath); G.DrawPath(pAxis, myPath); } } };

                                if (capturedName.ToLower().StartsWith("control"))
                                {
                                    controlOverlayQueue.Add(fillAction);
                                }
                                else
                                {
                                    float avgZ = zs.Count > 0 ? zs.Average() : 0.0f;
                                    drawQueue.Add((avgZ, fillAction));
                                }
                            }
                        }
                    }

                    // 6. Mass points - like control-surface overlays, these are an
                    // abstract annotation meant to always read clearly against the
                    // aircraft ("draw a subtle border to make it pop against the
                    // aircraft", per the original intent) rather than be
                    // realistically occluded by it, and a mass point can easily
                    // sit at nearly the same Z as the surface it belongs to
                    // (same coplanar-depth ambiguity as control surfaces). They
                    // get their own always-on-top pass, depth-sorted only among
                    // themselves so overlapping mass dots still occlude sensibly.
                    var massQueue = new List<(float Depth, Action Draw)>();
                    if (showMass == true & File.Exists(Application.StartupPath + $@"\{projectName}.mass"))
                    {
                        float mtotal = 0f;
                        int mcount = 0;

                        foreach (Node p in points)
                        {
                            if (p.type == Node.NodeType.Mass)
                            {
                                mtotal += p.mass;
                                mcount += 1;
                            }
                        }

                        if (mcount > 0)
                        {
                            float mavg = mtotal / mcount;

                            foreach (Node p in points)
                            {
                                if (p.type == Node.NodeType.Mass)
                                {
                                    var rNode = RotateAroundCenter(p);
                                    var screenNode = rNode.Project(p3d.Width, p3d.Height, viewDist, viewFOV);

                                    if (screenNode.Visible)
                                    {
                                        float rmass = p.mass / (mavg * 2f) * radius + radius;
                                        float capturedX = screenNode.X;
                                        float capturedY = screenNode.Y;
                                        float capturedR = rmass;
                                        massQueue.Add((rNode.Z, () =>
                                            {
                                                G.FillEllipse(Brushes.Blue, new RectangleF(capturedX - capturedR, capturedY - capturedR, capturedR * 2f, capturedR * 2f));
                                                G.DrawEllipse(pAxis, new RectangleF(capturedX - capturedR, capturedY - capturedR, capturedR * 2f, capturedR * 2f));
                                            }

                                        ));
                                    }
                                }
                            }
                        }
                    }

                    // Farthest (largest Z) first, nearest last (drawn on top)
                    foreach (var item in drawQueue.OrderByDescending(d => d.Depth))
                        item.Draw();

                    // Control-surface overlays always draw on top of the
                    // depth-sorted pass, regardless of their (unreliable, since
                    // they're coplanar with their parent) computed depth.
                    foreach (var draw in controlOverlayQueue)
                        draw();

                    // Mass points also always draw on top, depth-sorted only
                    // relative to each other.
                    foreach (var item in massQueue.OrderByDescending(d => d.Depth))
                        item.Draw();

                    // Mesh overlay stays a separate pass drawn on top of everything,
                    // matching its existing "always visible over the fill" role.
                    if (showMesh && parsedSurfaces is not null)
                    {
                        using (var meshPen = new Pen(Color.FromArgb(140, ThemeMeshColor)) { DashStyle = DashStyle.Dot })
                        {
                            foreach (Surface su in parsedSurfaces)
                            {
                                DrawMeshForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, meshPen, false);
                                if (su.yDuplicate)
                                {
                                    DrawMeshForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, meshPen, true);
                                }
                            }
                        }
                    }
                    if (showChordline && parsedSurfaces is not null)
                    {
                        using (var chordPen = new Pen(Color.DarkOrange, 1.5f))
                        {
                            foreach (Surface su in parsedSurfaces)
                            {
                                DrawChordlinesForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, chordPen, false);
                                if (su.yDuplicate)
                                {
                                    DrawChordlinesForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, chordPen, true);
                                }
                            }
                        }
                    }
                    if (showControlPoints && parsedSurfaces is not null)
                    {
                        foreach (Surface su in parsedSurfaces)
                        {
                            DrawControlPointsForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, Brushes.Magenta, 2.5f, false);
                            if (su.yDuplicate)
                            {
                                DrawControlPointsForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, Brushes.Magenta, 2.5f, true);
                            }
                        }
                    }
                    if (showAxesTriad)
                    {
                        DrawReferenceAxesTriad(G, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, (double)span * 0.5d);
                    }
                    if (showCamberline && parsedSurfaces is not null)
                    {
                        string[] avlLines = GetProjectAvlLines();
                        using (var camberPen = new Pen(Color.Purple, 1.5f))
                        {
                            foreach (Surface su in parsedSurfaces)
                            {
                                DrawCamberlineForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, camberPen, false, avlLines);
                                if (su.yDuplicate)
                                {
                                    DrawCamberlineForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, camberPen, true, avlLines);
                                }
                            }
                        }
                    }
                    if (showBoundLeg && parsedSurfaces is not null)
                    {
                        using (var boundPen = new Pen(Color.Firebrick, 1.5f))
                        {
                            foreach (Surface su in parsedSurfaces)
                            {
                                DrawBoundLegsForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, boundPen, false);
                                if (su.yDuplicate)
                                {
                                    DrawBoundLegsForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, boundPen, true);
                                }
                            }
                        }
                    }
                    if (showTrailingLegs && parsedSurfaces is not null)
                    {
                        using (var trailPen = new Pen(Color.SlateGray, 1f) { DashStyle = DashStyle.Dash })
                        {
                            foreach (Surface su in parsedSurfaces)
                            {
                                DrawTrailingLegsForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, trailPen, false, (double)span * 0.4d);
                                if (su.yDuplicate)
                                {
                                    DrawTrailingLegsForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, trailPen, true, (double)span * 0.4d);
                                }
                            }
                        }
                    }
                    if (showNormalVector && parsedSurfaces is not null)
                    {
                        using (var normPen = new Pen(Color.Cyan, 1.5f))
                        {
                            foreach (Surface su in parsedSurfaces)
                            {
                                DrawNormalVectorsForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, normPen, false, (double)span * 0.05d);
                                if (su.yDuplicate)
                                {
                                    DrawNormalVectorsForSurface(G, su, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, normPen, true, (double)span * 0.05d);
                                }
                            }
                        }
                    }
                    if (showLoadingOverlay)
                    {
                        using (var loadPen = new Pen(Color.SpringGreen, 1.5f))
                        {
                            DrawLoadingOverlay(G, "3D", p3d.Width, p3d.Height, 0d, 0d, 0d, 0d, 0d, 0d, loadPen);
                        }
                    }

                    DrawViewParamsOverlay(G);

                    p3dSvg = G.GetSvgContent();
                    p3dPdf = G.GetPdfContentStream();
                }

                var bmp3D = BMP;
                if (p3d.InvokeRequired)
                {
                    p3d.Invoke(() =>
                        {
                            old = p3d.Image;
                            p3d.Image = bmp3D;
                        });
                }
                else
                {
                    old = p3d.Image;
                    p3d.Image = bmp3D;
                }
                if (old is not null)
                    old.Dispose();
            }

        }

        private void btnZoomin_Click(object sender, EventArgs e)
        {
            if (gridnumber > 1)
            {
                gridnumber -= 1;
                drawAxes();
            }
        }

        private void btnZoomout_Click(object sender, EventArgs e)
        {
            gridnumber += 1;
            drawAxes();
        }

        private void btnBasefontminus_Click(object sender, EventArgs e)
        {
            if (baseFontsize > 1)
            {
                baseFontsize -= 1;
                drawAxes();
            }
        }

        private void btnBasefontplus_Click(object sender, EventArgs e)
        {
            if (baseFontsize < 20)
            {
                baseFontsize += 1;
                drawAxes();
            }
        }

        private void frmGeometry_Shown(object sender, EventArgs e)
        {
            drawAxes();
            Focus();
        }
        // txt3 is shared across the Geometry/Mass/Run tabs and only ever holds
        // whichever one is currently selected (tc1_SelectedIndexChanged swaps its
        // content on tab change) - several analysis-tab buttons call SaveAVL()
        // unconditionally before running AVL, so without this guard, clicking e.g.
        // "Run Trefftz Plot" while the Mass or Run tab happens to be active would
        // silently overwrite the .avl file with Mass/Run text. When the wrong tab
        // is active there's nothing to save anyway (txt3 isn't holding unsaved
        // Geometry edits), so this is a safe no-op, not a suppressed error.
        private void SaveAVL()
        {
            if (tc1.SelectedTab is not null && tc1.SelectedTab.Name != "Geometry")
                return;
            try
            {
                string f = Path.Combine(Application.StartupPath, $"{loadedProjectName}.avl");
                File.WriteAllText(f, TrimAll(txt3.Text));
            }
            catch (Exception er)
            {
                AppMessageBox.Show("Error: " + er.Message);
                try
                {
                    My.MyProject.Forms.frmMain.p.Kill();
                    My.MyProject.Forms.frmMain.loadConsole();
                }
                catch
                {
                }
            }
        }

        /// <summary>
    /// Reads a text file with a short retry-with-backoff. avl.exe (the external process AERO
    /// Console drives from the terminal) briefly holds its own handle on the .avl/.mass/.run
    /// file right after a "load"/"mass"/"case" command is sent to it, which can otherwise cause
    /// a sharing-violation IOException if the Geometry Designer is opened in that same instant.
    /// Opening with FileShare.ReadWrite (rather than File.ReadAllText's default FileShare.Read)
    /// also lets this read succeed even while avl.exe still holds a compatible handle open.
    /// </summary>
        private string ReadFileWithRetry(string path)
        {
            const int maxAttempts = 8;
            const int retryDelayMs = 150;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var sr = new StreamReader(fs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
                catch (IOException ex) when (attempt < maxAttempts)
                {
                    Thread.Sleep(retryDelayMs);
                }
            }
            return string.Empty;
        }

        private void LoadAVL()
        {
            try
            {
                loadedProjectName = projectName;
                UpdateProjectGate();
                string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
                if (File.Exists(f))
                {
                    txt3.Text = ReadFileWithRetry(f);
                    FormatActiveText();
                }
                else
                {
                    txt3.Text = string.Empty;
                }
                isDirty = false;
                UpdateDirtyWarning();
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error: " + ex.Message);
            }

        }

        // See the guard comment on SaveAVL() - same shared-txt3 hazard applies here.
        private void SaveMass()
        {
            if (tc1.SelectedTab is not null && tc1.SelectedTab.Name != "Mass")
                return;
            try
            {
                string f = Path.Combine(Application.StartupPath, $"{loadedProjectName}.mass");
                File.WriteAllText(f, TrimAll(txt3.Text));
            }
            catch (Exception er)
            {
                AppMessageBox.Show("Error: " + er.Message);
                try
                {
                    My.MyProject.Forms.frmMain.p.Kill();
                    My.MyProject.Forms.frmMain.loadConsole();
                }
                catch
                {
                }
            }
        }

        private void LoadMass()
        {
            try
            {
                loadedProjectName = projectName;
                UpdateProjectGate();
                string f = Path.Combine(Application.StartupPath, $"{projectName}.mass");
                if (File.Exists(f))
                {
                    txt3.Text = ReadFileWithRetry(f);
                    FormatActiveText();
                }
                else
                {
                    txt3.Text = string.Empty;
                }
                isDirty = false;
                UpdateDirtyWarning();
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error: " + ex.Message);
            }

        }

        // See the guard comment on SaveAVL() - same shared-txt3 hazard applies here.
        private void SaveRun()
        {
            if (tc1.SelectedTab is not null && tc1.SelectedTab.Name != "Run")
                return;
            string f = Path.Combine(Application.StartupPath, $"{loadedProjectName}.run");
            File.WriteAllText(f, TrimAll(txt3.Text, f));
        }

        private void LoadRun()
        {
            try
            {
                loadedProjectName = projectName;
                UpdateProjectGate();
                string f = Path.Combine(Application.StartupPath, $"{projectName}.run");
                if (File.Exists(f))
                {
                    txt3.Text = ReadFileWithRetry(f);
                    FormatActiveText();
                }
                else
                {
                    txt3.Text = string.Empty;
                }
                isDirty = false;
                UpdateDirtyWarning();
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error: " + ex.Message);
            }
        }

        // Instead of sending AVL's "t" Trefftz command (which opens AVL's own native
        // graphics window and captures nothing), this runs the case and asks AVL to
        // dump strip forces ("fs") to a text file, then parses and plots that data
        // in-house so it gets the same PNG/SVG/PDF export as the geometry views.
        private async void TrefftzPlaneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                AppMessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }
            if (My.MyProject.Forms.frmMain.p is null || My.MyProject.Forms.frmMain.p.HasExited)
                return;

            btnRunTrefftz.Enabled = false;
            try
            {
                var surfaces = await RunTrefftzAnalysisAsync();
                _lastTrefftzSurfaces = surfaces;
                _lastTrefftzCref = GetCrefFromAvlText(txt3.Text);
                _lastTrefftzTotals = ParseTotalForces(_lastTrefftzLog);
                RenderTrefftzPlot();
                // The "Loading" geometry-view overlay reads _lastTrefftzSurfaces too,
                // but only pTrefftz was being redrawn here - so toggling Loading on
                // before running this analysis left the 3D/2D geometry views stuck
                // showing nothing even after the data became available.
                drawAxes();

                int totalStrips = 0;
                if (surfaces is not null)
                {
                    foreach (var s in surfaces)
                        totalStrips += s.Strips.Count;
                }
                if (surfaces is null || totalStrips == 0)
                {
                    string debugPath = Path.Combine(Application.StartupPath, $"{projectName}_trefftz_debug.txt");
                    try
                    {
                        string content = "=== AVL console transcript ===" + Constants.vbCrLf + (_lastTrefftzLog ?? "") + Constants.vbCrLf + Constants.vbCrLf + "=== Raw FS output file AVL wrote (as parsed) ===" + Constants.vbCrLf + (_lastTrefftzRawFile ?? "(no file was produced)");
                        File.WriteAllText(debugPath, content);
                        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{debugPath}\"") { UseShellExecute = true });
                    }
                    catch
                    {
                    }
                    string reason = surfaces is null || surfaces.Count == 0 ? "AVL never reported any \"Surface #\" section - the load/oper/x/fs commands likely failed." : $"AVL reported {surfaces.Count} surface(s) but no data rows were recognized under any of them - the strip-forces table format didn't match what this app expects.";
                    AppMessageBox.Show("AVL did not return any strip-force data." + Constants.vbCrLf + Constants.vbCrLf + reason + Constants.vbCrLf + Constants.vbCrLf + "Full details written to:" + Constants.vbCrLf + debugPath, "Trefftz Plot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    tc1.SelectedTab = Trefftz;
                }
            }
            finally
            {
                btnRunTrefftz.Enabled = true;
            }
        }

        // Runs the current case and asks AVL to write strip forces to a temp file
        // (OPER -> X -> FS -> <file>), then waits for that file to appear and finish
        // writing before parsing it. There's no stdout parsing anywhere in this app,
        // so file polling is how we know AVL finished the write.
        private async Task<List<TrefftzSurface>> RunTrefftzAnalysisAsync()
        {
            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            // AVL (legacy Fortran, userio.f) silently truncates filenames longer than
            // its fixed internal buffer. Application.StartupPath (e.g.
            // ...\bin\Debug\net10.0-windows\, or deeper for a published build) plus a
            // project-name-based filename can exceed that limit, so the truncated
            // path collides with an existing file/directory and AVL reports "File
            // exists" and writes nothing - confirmed against a real AVL 3.37 build.
            // The OS temp folder + a random short name keeps this comfortably short
            // and unique, so the "File exists" prompt can never trigger at all.
            string outFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            // Make sure AVL's "load" sees the current editor content. Autosave is a
            // user toggle (btnAutosave) and debounced by 200ms even when on, so the
            // .avl file on disk can be stale or (for a brand-new project) not exist
            // yet - without this, "load" fails silently in AVL, every command after
            // it gets rejected, and "fs" never runs, which looks like "no data".
            SaveAVL();
            _lastTrefftzLog = "";
            if (!File.Exists(f))
            {
                _lastTrefftzLog = "(Could not save the geometry to " + f + " - check that the project name/path is valid and writable.)";
                return new List<TrefftzSurface>();
            }

            int logLengthBefore = My.MyProject.Forms.frmMain.txtLog.Text.Length;

            // Unguarded before: if frmMain.p had already died (or dies mid-write),
            // this threw straight out of an Async Function with no Catch around its
            // caller's Await - an unhandled exception on an async continuation that
            // left `p` dead and, per frmMain's txtLog_KeyDown, the console silently
            // inert until the whole app was restarted. Now mirrors SaveAVL/SaveMass's
            // existing crash recovery (frmMain.loadConsole()) instead of propagating.
            if (My.MyProject.Forms.frmMain.p is null || My.MyProject.Forms.frmMain.p.HasExited)
                My.MyProject.Forms.frmMain.loadConsole();
            try
            {
                {
                    var withBlock = My.MyProject.Forms.frmMain;
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine("load " + f);
                    withBlock.p.StandardInput.WriteLine("oper");
                    withBlock.p.StandardInput.WriteLine("x");
                    withBlock.p.StandardInput.WriteLine("fs");
                    withBlock.p.StandardInput.WriteLine(outFile);
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.Flush();
                }
            }
            catch
            {
                My.MyProject.Forms.frmMain.loadConsole();
                _lastTrefftzLog = "(AVL had stopped responding and was restarted - please try the Trefftz Plot again.)";
                return new List<TrefftzSurface>();
            }

            var deadline = DateTime.Now.AddSeconds(10d);
            long lastLen = -1;
            do
            {
                await Task.Delay(100);
                try
                {
                    if (File.Exists(outFile))
                    {
                        long len = new FileInfo(outFile).Length;
                        if (len > 0L && len == lastLen)
                            break;
                        lastLen = len;
                    }
                }
                catch
                {
                    // file may still be locked by AVL mid-write; keep polling
                }
            }
            while (DateTime.Now < deadline);

            // Let frmMain's 75ms batched log-flush timer catch up before reading it.
            await Task.Delay(150);
            try
            {
                string logText = My.MyProject.Forms.frmMain.txtLog.Text;
                if (logLengthBefore <= logText.Length)
                {
                    _lastTrefftzLog = logText.Substring(logLengthBefore);
                }
            }
            catch
            {
            }

            if (!File.Exists(outFile))
                return new List<TrefftzSurface>();

            try
            {
                _lastTrefftzRawFile = File.ReadAllText(outFile);
            }
            catch
            {
            }

            try
            {
                return ParseTrefftzStripFile(outFile);
            }
            catch
            {
                return new List<TrefftzSurface>();
            }
            finally
            {
                try
                {
                    File.Delete(outFile);
                }
                catch
                {
                }
            }
        }

        // Parses AVL's "FS" (strip forces) output. Format confirmed against a real
        // AVL 3.37 run:
        // Surface #  1     Wing
        // j      Yle    Chord     Area     c cl      ai      cl_norm  cl       cd       cdv    cm_c/4    cm_LE  C.P.x/c
        // 1   0.0214   1.0000   0.0852   0.4802   0.0193   0.4816   0.4816   0.0046   0.0000   0.0006  -0.1194    0.249
        // Data rows are detected by shape (leading integer strip index + 12 or 13
        // numeric columns) rather than by parsing the header, since "c cl" is a
        // two-word column name that would otherwise misalign a token-count-based
        // header parse. C.P.x/c (the 13th column) is cm/cl, which AVL omits
        // entirely - not zero, not NaN, just absent - for a zero-lift strip
        // (cl = 0, a 0/0 division), so a 12-token row is equally valid and just
        // means C.P.x/c is undefined for that strip. Confirmed against a real
        // zero-lift AVL run where every row was missing that trailing column.
        private List<TrefftzSurface> ParseTrefftzStripFile(string path)
        {
            var surfaces = new List<TrefftzSurface>();
            TrefftzSurface current = null;

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.StartsWith("Surface #"))
                {
                    current = new TrefftzSurface() { Name = line };
                    surfaces.Add(current);
                    continue;
                }
                if (current is null)
                    continue;

                string[] tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 12 && tokens.Length != 13)
                    continue;

                int idx;
                if (!int.TryParse(tokens[0], out idx))
                    continue;

                int colCount = tokens.Length - 1;
                var v = new double[colCount];
                bool ok = true;
                for (int i = 1, loopTo = colCount; i <= loopTo; i++)
                {
                    if (!double.TryParse(tokens[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v[i - 1]))
                    {
                        ok = false;
                        break;
                    }
                }
                if (!ok)
                    continue;

                current.Strips.Add(new TrefftzStrip()
                {
                    Yle = v[0],
                    Chord = v[1],
                    Area = v[2],
                    CCl = v[3],
                    Ai = v[4],
                    ClNorm = v[5],
                    Cl = v[6],
                    Cd = v[7],
                    Cdv = v[8],
                    CmC4 = v[9],
                    CmLE = v[10],
                    Cpxc = colCount >= 12 ? v[11] : 0.0d
                });
            }

            return surfaces;
        }

        // Reads Cref from the .avl geometry text. AVL's file format fixes the order
        // of the first few non-comment lines: name, Mach, IYsym/IZsym/Zsym,
        // Sref/Cref/Bref, Xref/Yref/Zref - so Cref is always the 2nd token of the
        // 4th data line.
        private double GetCrefFromAvlText(string avlText)
        {
            var dataLines = new List<string>();
            foreach (var raw in avlText.Replace(Constants.vbCr, "").Split(Constants.vbLf))
            {
                string t = raw.Trim();
                if (t.Length == 0)
                    continue;
                if (t.StartsWith("#") || t.StartsWith("!"))
                    continue;
                dataLines.Add(t);
                if (dataLines.Count == 4)
                    break;
            }

            if (dataLines.Count < 4)
                return 1.0d;

            string[] toks = dataLines[3].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            double cref = 1.0d;
            if (toks.Length >= 2)
            {
                double.TryParse(toks[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out cref);
            }
            return cref != 0d ? cref : 1.0d;
        }

        // Parses the "Vortex Lattice Output -- Total Forces" block that AVL prints
        // to the console right after "X" (execute) - this is the same numeric block
        // AVL's own native Trefftz Plane window shows as text (CL, CD, CDi, CDo, e,
        // etc.), confirmed against a real AVL 3.37 Trefftz Plane screenshot:
        // CLtot =   0.42112            -> "CL"
        // CDtot =   0.00588            -> "CD"
        // CDvis =   0.00000            -> "CDo"
        // CLff  =   0.42163  CDff = 0.0058967 | Trefftz  -> CDff is "CDi"
        // CYff  =   0.00000     e =    0.9596 | Plane
        // Cl'tot, Cmtot, Cn'tot                -> "Cl'", "Cm", "Cn'"
        private TrefftzTotals ParseTotalForces(string logText)
        {
            var t = new TrefftzTotals();
            if (string.IsNullOrEmpty(logText))
                return t;

            string numPattern = @"(-?\d+\.?\d*(?:[eE][-+]?\d+)?)";
            double? ExtractNum(string pattern)
            {
                var m = Regex.Match(logText, pattern);
                if (m.Success)
                {
                    double v;
                    if (double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
                    {
                        return v;
                    }
                }
                return default;
            };

            var cfgMatch = Regex.Match(logText, @"Configuration:\s*(.+)");
            if (cfgMatch.Success)
                t.ConfigName = cfgMatch.Groups[1].Value.Trim();

            var caseMatch = Regex.Match(logText, @"Run case:\s*(.+)");
            if (caseMatch.Success)
                t.RunCaseName = caseMatch.Groups[1].Value.Trim();

            var cltot = ExtractNum(@"CLtot\s*=\s*" + numPattern);
            if (!cltot.HasValue)
                return t;

            t.CLtot = cltot.Value;
            t.Alpha = ExtractNum(@"Alpha\s*=\s*" + numPattern) ?? 0.0d;
            t.Beta = ExtractNum(@"Beta\s*=\s*" + numPattern) ?? 0.0d;
            t.Mach = ExtractNum(@"Mach\s*=\s*" + numPattern) ?? 0.0d;
            t.CDtot = ExtractNum(@"CDtot\s*=\s*" + numPattern) ?? 0.0d;
            t.CDvis = ExtractNum(@"CDvis\s*=\s*" + numPattern) ?? 0.0d;
            t.CDff = ExtractNum(@"CDff\s*=\s*" + numPattern) ?? 0.0d;
            t.CYff = ExtractNum(@"CYff\s*=\s*" + numPattern) ?? 0.0d;
            t.SpanEff = ExtractNum(@"\be\s*=\s*" + numPattern) ?? 0.0d;
            t.ClPrimeTot = ExtractNum(@"Cl'tot\s*=\s*" + numPattern) ?? 0.0d;
            t.Cmtot = ExtractNum(@"Cmtot\s*=\s*" + numPattern) ?? 0.0d;
            t.CnPrimeTot = ExtractNum(@"Cn'tot\s*=\s*" + numPattern) ?? 0.0d;
            t.Valid = true;

            return t;
        }

        // Runs the current case and asks AVL to write spanwise shear/bending-moment
        // data to a temp file (OPER -> X -> VM -> <file>). Same pattern as the
        // Trefftz FS flow: save geometry first, unique short temp path (AVL
        // truncates long paths - see RunTrefftzAnalysisAsync), poll for the file.
        private async void RunLoadsAnalysis_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                AppMessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }
            if (My.MyProject.Forms.frmMain.p is null || My.MyProject.Forms.frmMain.p.HasExited)
                return;

            btnLoads.Enabled = false;
            try
            {
                var surfaces = await RunLoadsAnalysisAsync();
                _lastVmSurfaces = surfaces;
                RenderLoadsPlot();

                int totalPoints = 0;
                if (surfaces is not null)
                {
                    foreach (var s in surfaces)
                        totalPoints += s.Points.Count;
                }

                if (surfaces is null || totalPoints == 0)
                {
                    string debugPath = Path.Combine(Application.StartupPath, $"{projectName}_loads_debug.txt");
                    try
                    {
                        File.WriteAllText(debugPath, _lastVmLog ?? "");
                        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{debugPath}\"") { UseShellExecute = true });
                    }
                    catch
                    {
                    }
                    AppMessageBox.Show("AVL did not return any shear/moment data." + Constants.vbCrLf + Constants.vbCrLf + "Full details written to:" + Constants.vbCrLf + debugPath, "Spanwise Loads", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    tc1.SelectedTab = Loads;
                }
            }
            finally
            {
                btnLoads.Enabled = true;
            }
        }

        private async Task<List<VmSurface>> RunLoadsAnalysisAsync()
        {
            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            string outFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            SaveAVL();
            _lastVmLog = "";
            if (!File.Exists(f))
            {
                _lastVmLog = "(Could not save the geometry to " + f + " - check that the project name/path is valid and writable.)";
                return new List<VmSurface>();
            }

            int logLengthBefore = My.MyProject.Forms.frmMain.txtLog.Text.Length;

            // See the matching comment in RunTrefftzAnalysisAsync - same unguarded
            // stdin-write hazard, same recovery.
            if (My.MyProject.Forms.frmMain.p is null || My.MyProject.Forms.frmMain.p.HasExited)
                My.MyProject.Forms.frmMain.loadConsole();
            try
            {
                {
                    var withBlock = My.MyProject.Forms.frmMain;
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.WriteLine("load " + f);
                    withBlock.p.StandardInput.WriteLine("oper");
                    withBlock.p.StandardInput.WriteLine("x");
                    withBlock.p.StandardInput.WriteLine("vm");
                    withBlock.p.StandardInput.WriteLine(outFile);
                    withBlock.p.StandardInput.WriteLine();
                    withBlock.p.StandardInput.Flush();
                }
            }
            catch
            {
                My.MyProject.Forms.frmMain.loadConsole();
                _lastVmLog = "(AVL had stopped responding and was restarted - please try the Loads Plot again.)";
                return new List<VmSurface>();
            }

            var deadline = DateTime.Now.AddSeconds(10d);
            long lastLen = -1;
            do
            {
                await Task.Delay(100);
                try
                {
                    if (File.Exists(outFile))
                    {
                        long len = new FileInfo(outFile).Length;
                        if (len > 0L && len == lastLen)
                            break;
                        lastLen = len;
                    }
                }
                catch
                {
                }
            }
            while (DateTime.Now < deadline);

            await Task.Delay(150);
            try
            {
                string logText = My.MyProject.Forms.frmMain.txtLog.Text;
                if (logLengthBefore <= logText.Length)
                {
                    _lastVmLog = logText.Substring(logLengthBefore);
                }
            }
            catch
            {
            }

            if (!File.Exists(outFile))
                return new List<VmSurface>();

            try
            {
                return ParseVmFile(outFile);
            }
            catch
            {
                return new List<VmSurface>();
            }
            finally
            {
                try
                {
                    File.Delete(outFile);
                }
                catch
                {
                }
            }
        }

        // Parses AVL's "VM" (strip shear/moment) output. Format confirmed against a
        // real AVL 3.37 run:
        // Surface:   1
        // [Wing]
        // 2Ymin/Bref =    0.00000000
        // 2Ymax/Bref =    1.00000000
        // 2Y/Bref      Vz/(q*Sref)      Mx/(q*Bref*Sref)
        // 0.0000  0.210609         0.478813E-01
        // Unlike FS, rows here have no leading integer index (2Y/Bref is a float,
        // possibly negative) and values can be in Fortran "E-01" scientific form,
        // which Double.TryParse with NumberStyles.Float already handles natively.
        private List<VmSurface> ParseVmFile(string path)
        {
            var surfaces = new List<VmSurface>();
            VmSurface current = null;
            bool awaitingName = false;
            bool inDataSection = false;

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith("Surface:"))
                {
                    current = new VmSurface();
                    surfaces.Add(current);
                    awaitingName = true;
                    inDataSection = false;
                    continue;
                }

                if (awaitingName)
                {
                    current.Name = line;
                    awaitingName = false;
                    continue;
                }

                if (line.StartsWith("2Y/Bref"))
                {
                    inDataSection = true;
                    continue;
                }

                if (!inDataSection || current is null)
                    continue;

                string[] tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 3)
                    continue;
                var v = new double[3];
                bool ok = true;
                for (int i = 0; i <= 2; i++)
                {
                    if (!double.TryParse(tokens[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v[i]))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                    current.Points.Add(new VmStrip() { Y2Bref = v[0], Vz = v[1], Mx = v[2] });
            }

            return surfaces;
        }

        // Renders spanwise shear (Vz) and bending moment (Mx) vs 2Y/Bref as two
        // stacked plots, styled consistently with the Trefftz plot (black bg).
        private void RenderLoadsPlot(bool captureVectors = false)
        {
            if (pLoads is null || pLoads.Width <= 0 || pLoads.Height <= 0)
                return;

            int w = pLoads.Width;
            int h = pLoads.Height;
            var BMP = new Bitmap(w, h);
            var tickFont = GetTickFont(9f);
            var axisFont = GetAxisFont();
            string svgOut = "";
            string pdfOut = "";

            using (var G = new SvgGraphics(w, h, Graphics.FromImage(BMP), captureVectors))
            {
                G.SmoothingMode = SmoothingMode.AntiAlias;
                G.TextRenderingHint = TextRenderingHint.AntiAlias;
                G.Clear(ThemeCanvasBackColor);

                if (_lastVmSurfaces is null || _lastVmSurfaces.Count == 0)
                {
                    G.DrawString("Run \"Shear & Bending Moment\" to compute and plot spanwise loads.", tickFont, Brushes.Gray, new PointF(10f, 10f));
                }
                else
                {
                    float margin = 50f;
                    float plotH = (h - margin * 3f) / 2f;
                    float plotW = w - margin * 2f;
                    DrawVmSubplot(G, tickFont, axisFont, margin, margin, plotW, plotH, "Vz / (q*Sref)  (spanwise shear)", Color.OrangeRed, (p) => p.Vz);
                    DrawVmSubplot(G, tickFont, axisFont, margin, margin * 2f + plotH, plotW, plotH, "Mx / (q*Bref*Sref)  (bending moment)", Color.LimeGreen, (p) => p.Mx);
                }

                if (captureVectors)
                {
                    svgOut = G.GetSvgContent();
                    pdfOut = G.GetPdfContentStream();
                }
            }

            var oldImage = pLoads.Image;
            pLoads.Image = BMP;
            oldImage?.Dispose();

            if (captureVectors)
            {
                loadsSvg = svgOut;
                loadsPdf = pdfOut;
            }
        }

        // Draws one 2Y/Bref-vs-value line plot (one polyline per AVL surface) into the given sub-rectangle.
        private void DrawVmSubplot(SvgGraphics G, Font tickFont, Font axisFont, float x, float y, float w, float h, string title, Color curveColor, Func<VmStrip, double> valueOf)
        {
            double xMin = double.MaxValue;
            double xMax = double.MinValue;
            double vMin = double.MaxValue;
            double vMax = double.MinValue;
            bool any = false;
            foreach (VmSurface surf in _lastVmSurfaces)
            {
                foreach (VmStrip p in surf.Points)
                {
                    any = true;
                    xMin = Math.Min(xMin, p.Y2Bref);
                    xMax = Math.Max(xMax, p.Y2Bref);
                    double v = valueOf(p);
                    vMin = Math.Min(vMin, v);
                    vMax = Math.Max(vMax, v);
                }
            }
            if (!any)
                return;
            vMin = Math.Min(0d, vMin);
            vMax = Math.Max(0d, vMax);
            if (xMin == xMax)
            {
                xMin -= 1d;
                xMax += 1d;
            }
            if (vMin == vMax)
            {
                vMin -= 1d;
                vMax += 1d;
            }
            double vPad = (vMax - vMin) * 0.1d;
            vMin -= vPad;
            vMax += vPad;

            var whitePen = new Pen(ThemeAxisColor, 1f);
            var gridPen = new Pen(ThemeGridColor, 1f) { DashStyle = DashStyle.Dash };

            G.DrawRectangle(whitePen, x, y, w, h);
            G.DrawString(title, axisFont, new SolidBrush(ThemeAxisColor), new PointF(x, y - axisFont.Height - 2f));

            if (vMin < 0d && vMax > 0d)
            {
                float zeroY = (float)((double)(y + h) - (0d - vMin) / (vMax - vMin) * (double)h);
                G.DrawLine(gridPen, x, zeroY, x + w, zeroY);
            }

            var pen = new Pen(curveColor, 1.5f);
            foreach (VmSurface surf in _lastVmSurfaces)
            {
                if (surf.Points.Count == 0)
                    continue;
                var ordered = new List<VmStrip>(surf.Points);
                ordered.Sort((a, b) => a.Y2Bref.CompareTo(b.Y2Bref));
                var pts = new List<PointF>();
                foreach (VmStrip p in ordered)
                {
                    float px = (float)((double)x + (p.Y2Bref - xMin) / (xMax - xMin) * (double)w);
                    float py = (float)((double)(y + h) - (valueOf(p) - vMin) / (vMax - vMin) * (double)h);
                    pts.Add(new PointF(px, py));
                }
                if (pts.Count >= 2)
                    G.DrawLines(pen, pts.ToArray());
            }

            G.DrawString(xMin.ToString("0.00"), tickFont, new SolidBrush(ThemeAxisColor), new PointF(x, y + h + 2f));
            string maxLabel = xMax.ToString("0.00");
            var maxLabelSize = G.MeasureString(maxLabel, tickFont);
            G.DrawString(maxLabel, tickFont, new SolidBrush(ThemeAxisColor), new PointF(x + w - maxLabelSize.Width, y + h + 2f));

            string vMaxLabel = vMax.ToString("0.0000");
            var vMaxLabelSize = G.MeasureString(vMaxLabel, tickFont);
            G.DrawString(vMaxLabel, tickFont, new SolidBrush(ThemeAxisColor), new PointF(x - vMaxLabelSize.Width - 2f, y));
            string vMinLabel = vMin.ToString("0.0000");
            var vMinLabelSize = G.MeasureString(vMinLabel, tickFont);
            G.DrawString(vMinLabel, tickFont, new SolidBrush(ThemeAxisColor), new PointF(x - vMinLabelSize.Width - 2f, y + h - vMinLabelSize.Height));
        }

        private async void RunPolarSweep_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                AppMessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }
            if (My.MyProject.Forms.frmMain.p is null || My.MyProject.Forms.frmMain.p.HasExited)
                return;

            double aMin;
            double aMax;
            double aStep;
            if (!double.TryParse(txtPolarMin.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out aMin) || !double.TryParse(txtPolarMax.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out aMax) || !double.TryParse(txtPolarStep.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out aStep) || aStep <= 0d || aMax < aMin)
            {
                AppMessageBox.Show("Enter valid numeric alpha min/max/step (step must be positive, max >= min).", "Drag Polar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int pointCount = (int)Math.Round(Math.Floor((aMax - aMin) / aStep)) + 1;
            if (pointCount > 60)
            {
                AppMessageBox.Show("That's more than 60 points - narrow the range or increase the step size.", "Drag Polar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnRunPolar.Enabled = false;
            try
            {
                var points = await RunPolarSweepAsync(aMin, aMax, aStep);
                _lastPolarPoints = points;
                RenderPolarPlot();

                if (points is null || points.Count == 0)
                {
                    string debugPath = Path.Combine(Application.StartupPath, $"{projectName}_polar_debug.txt");
                    try
                    {
                        File.WriteAllText(debugPath, _lastPolarLog ?? "");
                        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{debugPath}\"") { UseShellExecute = true });
                    }
                    catch
                    {
                    }
                    AppMessageBox.Show("AVL did not return any polar data." + Constants.vbCrLf + Constants.vbCrLf + "Full details written to:" + Constants.vbCrLf + debugPath, "Drag Polar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                btnRunPolar.Enabled = true;
            }
        }

        // Sweeps alpha from aMin to aMax and collects CL/CD/Cm at each point. AVL has
        // no built-in sweep command, so this scripts one manually: from the OPER top
        // level, "a" alone enters a constraint-select submenu, and "a <value>" inside
        // that submenu sets alpha and returns to OPER (confirmed against real AVL -
        // sending "a <value>" directly at the OPER top level is NOT recognized).
        // Each "x" prints a full "Total Forces" block to the console; rather than
        // polling a file per point, this reads them all out of the console
        // transcript afterward using the existing ParseTotalForces regex parser.
        private async Task<List<PolarPoint>> RunPolarSweepAsync(double aMin, double aMax, double aStep)
        {
            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            var points = new List<PolarPoint>();

            SaveAVL();
            _lastPolarLog = "";
            if (!File.Exists(f))
            {
                _lastPolarLog = "(Could not save the geometry to " + f + " - check that the project name/path is valid and writable.)";
                return points;
            }

            int logLengthBefore = My.MyProject.Forms.frmMain.txtLog.Text.Length;
            int pointCount = (int)Math.Round(Math.Floor((aMax - aMin) / aStep)) + 1;

            {
                var withBlock = My.MyProject.Forms.frmMain;
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine("load " + f);
                withBlock.p.StandardInput.WriteLine("oper");

                double alpha = aMin;
                while (alpha <= aMax + aStep * 0.001d)
                {
                    withBlock.p.StandardInput.WriteLine("a");
                    withBlock.p.StandardInput.WriteLine("a " + alpha.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    withBlock.p.StandardInput.WriteLine("x");
                    alpha += aStep;
                }

                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.Flush();
            }

            await Task.Delay(Math.Min(8000, 200 * pointCount + 300));

            try
            {
                string logText = My.MyProject.Forms.frmMain.txtLog.Text;
                if (logLengthBefore <= logText.Length)
                {
                    _lastPolarLog = logText.Substring(logLengthBefore);
                }
            }
            catch
            {
            }

            if (string.IsNullOrEmpty(_lastPolarLog))
                return points;

            // Split the transcript into one chunk per "Total Forces" block (one per sweep point).
            string[] blocks = Regex.Split(_lastPolarLog, "(?=Vortex Lattice Output -- Total Forces)");
            foreach (var block in blocks)
            {
                if (!block.Contains("Vortex Lattice Output -- Total Forces"))
                    continue;
                var t = ParseTotalForces(block);
                if (t.Valid)
                {
                    points.Add(new PolarPoint() { Alpha = t.Alpha, CL = t.CLtot, CD = t.CDtot, Cm = t.Cmtot });
                }
            }

            return points;
        }

        // Renders the 2x2 drag-polar grid (CL-alpha, CD-alpha, Cm-alpha, CL-CD),
        // styled consistently with the other in-house plots (black bg, PNG/SVG/PDF
        // export via ExportView).
        private void RenderPolarPlot(bool captureVectors = false)
        {
            if (pPolar is null || pPolar.Width <= 0 || pPolar.Height <= 0)
                return;

            int w = pPolar.Width;
            int h = pPolar.Height;
            var BMP = new Bitmap(w, h);
            var tickFont = GetTickFont(9f);
            var axisFont = GetAxisFont();
            string svgOut = "";
            string pdfOut = "";

            using (var G = new SvgGraphics(w, h, Graphics.FromImage(BMP), captureVectors))
            {
                G.SmoothingMode = SmoothingMode.AntiAlias;
                G.TextRenderingHint = TextRenderingHint.AntiAlias;
                G.Clear(ThemeCanvasBackColor);

                if (_lastPolarPoints is null || _lastPolarPoints.Count == 0)
                {
                    G.DrawString("Set an alpha range and click \"Run Polar Sweep\" to compute the drag polar.", tickFont, Brushes.Gray, new PointF(10f, 10f));
                }
                else
                {
                    var ordered = new List<PolarPoint>(_lastPolarPoints);
                    ordered.Sort((a, b) => a.Alpha.CompareTo(b.Alpha));

                    float marginX = 55f;
                    float marginY = 35f;
                    float cellW = (w - marginX * 3f) / 2f;
                    float cellH = (h - marginY * 3f) / 2f;

                    DrawPolarSubplot(G, tickFont, axisFont, marginX, marginY, cellW, cellH, "CL vs alpha", Color.OrangeRed, ordered, p => p.Alpha, p => p.CL);
                    DrawPolarSubplot(G, tickFont, axisFont, marginX * 2f + cellW, marginY, cellW, cellH, "CD vs alpha", Color.DeepSkyBlue, ordered, p => p.Alpha, p => p.CD);
                    DrawPolarSubplot(G, tickFont, axisFont, marginX, marginY * 2f + cellH, cellW, cellH, "Cm vs alpha", Color.LimeGreen, ordered, p => p.Alpha, p => p.Cm);
                    DrawPolarSubplot(G, tickFont, axisFont, marginX * 2f + cellW, marginY * 2f + cellH, cellW, cellH, "CL vs CD  (drag polar)", Color.Gold, ordered, p => p.CD, p => p.CL);
                }

                if (captureVectors)
                {
                    svgOut = G.GetSvgContent();
                    pdfOut = G.GetPdfContentStream();
                }
            }

            var oldImage = pPolar.Image;
            pPolar.Image = BMP;
            oldImage?.Dispose();

            if (captureVectors)
            {
                polarSvg = svgOut;
                polarPdf = pdfOut;
            }
        }

        private void DrawPolarSubplot(SvgGraphics G, Font tickFont, Font axisFont, float x, float y, float w, float h, string title, Color curveColor, List<PolarPoint> points, Func<PolarPoint, double> xOf, Func<PolarPoint, double> yOf)
        {
            if (points.Count == 0)
                return;

            double xMin = double.MaxValue;
            double xMax = double.MinValue;
            double yMin = double.MaxValue;
            double yMax = double.MinValue;
            foreach (var p in points)
            {
                double xv = xOf(p);
                double yv = yOf(p);
                xMin = Math.Min(xMin, xv);
                xMax = Math.Max(xMax, xv);
                yMin = Math.Min(yMin, yv);
                yMax = Math.Max(yMax, yv);
            }
            if (xMin == xMax)
            {
                xMin -= 1d;
                xMax += 1d;
            }
            if (yMin == yMax)
            {
                yMin -= 1d;
                yMax += 1d;
            }
            double yPad = (yMax - yMin) * 0.1d;
            yMin -= yPad;
            yMax += yPad;

            var whitePen = new Pen(ThemeAxisColor, 1f);
            var gridPen = new Pen(ThemeGridColor, 1f) { DashStyle = DashStyle.Dash };

            G.DrawRectangle(whitePen, x, y, w, h);
            G.DrawString(title, axisFont, new SolidBrush(ThemeAxisColor), new PointF(x, y - axisFont.Height - 2f));

            if (yMin < 0d && yMax > 0d)
            {
                float zeroY = (float)((double)(y + h) - (0d - yMin) / (yMax - yMin) * (double)h);
                G.DrawLine(gridPen, x, zeroY, x + w, zeroY);
            }

            var pen = new Pen(curveColor, 1.5f);
            var markerBrush = new SolidBrush(curveColor);
            var pts = new List<PointF>();
            foreach (var p in points)
            {
                float px = (float)((double)x + (xOf(p) - xMin) / (xMax - xMin) * (double)w);
                float py = (float)((double)(y + h) - (yOf(p) - yMin) / (yMax - yMin) * (double)h);
                pts.Add(new PointF(px, py));
            }
            if (pts.Count >= 2)
                G.DrawLines(pen, pts.ToArray());
            foreach (var pt in pts)
                G.FillEllipse(markerBrush, pt.X - 2f, pt.Y - 2f, 4f, 4f);

            G.DrawString(xMin.ToString("0.00"), tickFont, new SolidBrush(ThemeAxisColor), new PointF(x, y + h + 2f));
            string maxLabel = xMax.ToString("0.00");
            var maxLabelSize = G.MeasureString(maxLabel, tickFont);
            G.DrawString(maxLabel, tickFont, new SolidBrush(ThemeAxisColor), new PointF(x + w - maxLabelSize.Width, y + h + 2f));

            string yMaxLabel = yMax.ToString("0.0000");
            var yMaxLabelSize = G.MeasureString(yMaxLabel, tickFont);
            G.DrawString(yMaxLabel, tickFont, new SolidBrush(ThemeAxisColor), new PointF(x - yMaxLabelSize.Width - 2f, y));
            string yMinLabel = yMin.ToString("0.0000");
            var yMinLabelSize = G.MeasureString(yMinLabel, tickFont);
            G.DrawString(yMinLabel, tickFont, new SolidBrush(ThemeAxisColor), new PointF(x - yMinLabelSize.Width - 2f, y + h - yMinLabelSize.Height));
        }

        private async void RunDerivatives_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                AppMessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }
            if (My.MyProject.Forms.frmMain.p is null || My.MyProject.Forms.frmMain.p.HasExited)
                return;

            btnRunDerivatives.Enabled = false;
            try
            {
                string text = await RunDerivativesAnalysisAsync();
                _lastDerivativesText = text;
                txtDerivatives.Text = string.IsNullOrWhiteSpace(text) ? "AVL did not return any data. Check the geometry and try again." : text;
                UpdateDerivativesInsights(ParseDerivativesInsight(_lastStRawText));
                tc1.SelectedTab = Derivatives;
            }
            finally
            {
                btnRunDerivatives.Enabled = true;
            }
        }

        // Runs ST, SB, FN, FB, HM back-to-back in one OPER session (chained without
        // a blank line between them, since AVL auto-returns to the OPER menu after
        // each data-write command - a blank line there pops back out to the top
        // level and breaks the chain, confirmed while testing this sequence) and
        // combines all five outputs into one formatted text block. FB/HM are
        // legitimately empty for geometries with no bodies/control surfaces.
        private async Task<string> RunDerivativesAnalysisAsync()
        {
            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");

            SaveAVL();
            if (!File.Exists(f))
            {
                return "(Could not save the geometry to " + f + " - check that the project name/path is valid and writable.)";
            }

            string stFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string sbFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string fnFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string fbFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string hmFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            {
                var withBlock = My.MyProject.Forms.frmMain;
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine("load " + f);
                withBlock.p.StandardInput.WriteLine("oper");
                withBlock.p.StandardInput.WriteLine("x");
                withBlock.p.StandardInput.WriteLine("st");
                withBlock.p.StandardInput.WriteLine(stFile);
                withBlock.p.StandardInput.WriteLine("sb");
                withBlock.p.StandardInput.WriteLine(sbFile);
                withBlock.p.StandardInput.WriteLine("fn");
                withBlock.p.StandardInput.WriteLine(fnFile);
                withBlock.p.StandardInput.WriteLine("fb");
                withBlock.p.StandardInput.WriteLine(fbFile);
                withBlock.p.StandardInput.WriteLine("hm");
                withBlock.p.StandardInput.WriteLine(hmFile);
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.Flush();
            }

            // Poll the last file in the chain (hm) - by the time it's stable, the
            // earlier ones are already fully written.
            var deadline = DateTime.Now.AddSeconds(12d);
            long lastLen = -1;
            do
            {
                await Task.Delay(150);
                try
                {
                    if (File.Exists(hmFile))
                    {
                        long len = new FileInfo(hmFile).Length;
                        if (len > 0L && len == lastLen)
                            break;
                        lastLen = len;
                    }
                }
                catch
                {
                }
            }
            while (DateTime.Now < deadline);
            await Task.Delay(150);

            _lastStRawText = File.Exists(stFile) ? File.ReadAllText(stFile).Trim() : "";

            var sb = new StringBuilder();
            AppendDerivativesSection(sb, "STABILITY DERIVATIVES (stability axis)", stFile);
            AppendDerivativesSection(sb, "BODY-AXIS DERIVATIVES", sbFile);
            AppendDerivativesSection(sb, "SURFACE FORCES", fnFile);
            AppendDerivativesSection(sb, "BODY FORCES", fbFile);
            AppendDerivativesSection(sb, "CONTROL HINGE MOMENTS", hmFile);

            foreach (var fp in new string[] { stFile, sbFile, fnFile, fbFile, hmFile })
            {
                try
                {
                    File.Delete(fp);
                }
                catch
                {
                }
            }

            return sb.ToString();
        }

        private void AppendDerivativesSection(StringBuilder sb, string title, string path)
        {
            sb.AppendLine("======================================================================");
            sb.AppendLine(title);
            sb.AppendLine("======================================================================");
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path).Trim();
                if (string.IsNullOrWhiteSpace(content))
                {
                    sb.AppendLine("(no data - this geometry may not have bodies/control surfaces defined)");
                }
                else
                {
                    sb.AppendLine(content);
                }
            }
            else
            {
                sb.AppendLine("(AVL did not produce output for this command)");
            }
            sb.AppendLine();
        }

        // Reads the handful of stability-axis values needed for a static-stability
        // verdict directly off AVL's own "ST" text (real captured format:
        // "Cma =  -1.161636", "Xnp =   0.241924", "Xref =  0.0000", plus AVL's own
        // "Clb Cnr / Clr Cnb = ... ( > 1 if spirally stable )" line, which is reused
        // as-is rather than recomputed to avoid any sign-convention mismatch).
        private DerivativesInsight ParseDerivativesInsight(string stText)
        {
            var r = new DerivativesInsight();
            if (string.IsNullOrWhiteSpace(stText))
                return r;

            // AVL prints small values in scientific notation (confirmed against a
            // real capture, e.g. "Zref = 0.50000E-01"), so the numeric part must
            // tolerate an optional E±NN suffix - a plain [\d.]+ would silently
            // truncate "0.50000E-01" to 0.50000 (10x off) instead of failing loudly.
            string numPattern = @"(-?[\d.]+(?:[Ee][+-]?\d+)?)";
            var cma = Regex.Match(stText, @"\bCma\s*=\s*" + numPattern);
            var clb = Regex.Match(stText, @"\bClb\s*=\s*" + numPattern);
            var cnb = Regex.Match(stText, @"\bCnb\s*=\s*" + numPattern);
            var cyb = Regex.Match(stText, @"\bCYb\s*=\s*" + numPattern);
            var xnp = Regex.Match(stText, @"\bXnp\s*=\s*" + numPattern);
            var xref = Regex.Match(stText, @"\bXref\s*=\s*" + numPattern);
            var cref = Regex.Match(stText, @"\bCref\s*=\s*" + numPattern);
            var spiral = Regex.Match(stText, @"Clb Cnr / Clr Cnb\s*=\s*" + numPattern);

            if (cma.Success && clb.Success && cnb.Success && xnp.Success && xref.Success && cref.Success)
            {
                r.Cma = Conversions.ToDouble(cma.Groups[1].Value);
                r.Clb = Conversions.ToDouble(clb.Groups[1].Value);
                r.Cnb = Conversions.ToDouble(cnb.Groups[1].Value);
                r.CYb = cyb.Success ? Conversions.ToDouble(cyb.Groups[1].Value) : 0.0d;
                r.Xnp = Conversions.ToDouble(xnp.Groups[1].Value);
                r.Xref = Conversions.ToDouble(xref.Groups[1].Value);
                r.Cref = Conversions.ToDouble(cref.Groups[1].Value);
                r.HasData = true;
            }
            if (spiral.Success)
            {
                r.SpiralRatio = Conversions.ToDouble(spiral.Groups[1].Value);
                r.HasSpiralRatio = true;
            }
            return r;
        }

        // Plain-language static-stability verdicts, each colored green/red by
        // whether the classic sign criterion is met:
        // Pitch:      Cma < 0    (nose-down restoring moment with increasing AoA)
        // Directional: Cnb > 0   (restoring yaw/"weathercock" moment with sideslip)
        // Roll:        Clb < 0   (restoring roll moment with sideslip, i.e. dihedral effect)
        // Spiral:      Clb*Cnr/(Clr*Cnb) > 1 (AVL prints this ratio itself)
        // Static margin = (Xnp - Xref) / Cref, the same sign convention AVL's own
        // Cma/Xnp values in the test captures cross-checked against.
        private void UpdateDerivativesInsights(DerivativesInsight insight)
        {
            rtbDerivInsights.Clear();
            if (!insight.HasData)
            {
                AppendInsightLine(rtbDerivInsights, "Run the analysis to see a static-stability summary here.", Color.Gray);
                return;
            }

            double sm = (insight.Xnp - insight.Xref) / insight.Cref * 100.0d;
            bool smStable = sm > 0d;
            AppendInsightLine(rtbDerivInsights, $"Static margin: {sm:0.0}% of Cref ({(smStable ? "stable" : "UNSTABLE")}) — neutral pt Xnp={insight.Xnp:0.000}, CG Xref={insight.Xref:0.000}", smStable ? ThemeStableColor : ThemeUnstableColor);

            bool pitchStable = insight.Cma < 0d;
            AppendInsightLine(rtbDerivInsights, $"Pitch stability (Cma = {insight.Cma:0.000}): {(pitchStable ? "stable — nose-down restoring moment with increasing AoA" : "UNSTABLE — needs Cma < 0; try moving the CG forward or adding tail area/arm")}", pitchStable ? ThemeStableColor : ThemeUnstableColor);

            bool yawStable = insight.Cnb > 0d;
            AppendInsightLine(rtbDerivInsights, $"Directional/weathercock stability (Cnb = {insight.Cnb:0.000}): {(yawStable ? "stable — restoring yaw moment with sideslip" : "UNSTABLE — needs Cnb > 0; try increasing vertical tail area/arm")}", yawStable ? ThemeStableColor : ThemeUnstableColor);

            bool rollStable = insight.Clb < 0d;
            AppendInsightLine(rtbDerivInsights, $"Roll/dihedral stability (Clb = {insight.Clb:0.000}): {(rollStable ? "stable — restoring roll moment with sideslip" : "unstable — needs Clb < 0; try adding dihedral or a high wing")}", rollStable ? ThemeStableColor : ThemeUnstableColor);

            if (insight.HasSpiralRatio)
            {
                bool spiralStable = insight.SpiralRatio > 1d;
                AppendInsightLine(rtbDerivInsights, $"Spiral stability (Clb·Cnr / Clr·Cnb = {insight.SpiralRatio:0.00}, AVL: >1 is spirally stable): {(spiralStable ? "stable" : "unstable — usually just a slow, mild, pilot-correctable divergence, and is often traded off against Dutch roll damping (more dihedral helps spiral but hurts Dutch roll, and vice versa)")}", spiralStable ? ThemeStableColor : ThemeCautionColor);
            }
        }

        private void AppendInsightLine(System.Windows.Forms.RichTextBox rtb, string text, Color color)
        {
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionColor = color;
            rtb.AppendText(text + Environment.NewLine);
        }

        private void ExportDerivatives_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_lastDerivativesText))
            {
                AppMessageBox.Show("Run the analysis first.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var sfd = new SaveFileDialog();
            sfd.Filter = "Text File (*.txt)|*.txt";
            sfd.FileName = $"{projectName}_derivatives.txt";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, _lastDerivativesText);
                    AppToast.Show("Exported to " + Path.GetFileName(sfd.FileName));
                }
                catch (Exception ex)
                {
                    AppMessageBox.Show("Error exporting file: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void RunFEAnalysis_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                AppMessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }
            if (My.MyProject.Forms.frmMain.p is null || My.MyProject.Forms.frmMain.p.HasExited)
                return;

            btnRunFE.Enabled = false;
            try
            {
                var strips = await RunFEAnalysisAsync();
                _lastFeStrips = strips;

                cmbFeStrip.Items.Clear();
                if (strips is not null)
                {
                    foreach (var s in strips)
                        cmbFeStrip.Items.Add($"{s.SurfaceName}  Y={s.Yle:0.000}  (strip {s.StripIndex})");
                }

                if (strips is null || strips.Count == 0)
                {
                    cmbFeStrip.Enabled = false;
                    RenderFEPlot();
                    string debugPath = Path.Combine(Application.StartupPath, $"{projectName}_pressure_debug.txt");
                    try
                    {
                        File.WriteAllText(debugPath, _lastFeLog ?? "");
                        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{debugPath}\"") { UseShellExecute = true });
                    }
                    catch
                    {
                    }
                    AppMessageBox.Show("AVL did not return any element-force data." + Constants.vbCrLf + Constants.vbCrLf + "Full details written to:" + Constants.vbCrLf + debugPath, "Pressure Distribution", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    cmbFeStrip.Enabled = true;
                    cmbFeStrip.SelectedIndex = 0; // triggers RenderFEPlot via SelectedIndexChanged
                    tc1.SelectedTab = FE;
                }
            }
            finally
            {
                btnRunFE.Enabled = true;
            }
        }

        // Runs the current case and asks AVL to write element (chordwise panel)
        // forces to a temp file (OPER -> X -> FE -> <file>). Same save/short-temp-
        // path/poll pattern as the other analyses.
        private async Task<List<FeStrip>> RunFEAnalysisAsync()
        {
            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            string outFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            SaveAVL();
            _lastFeLog = "";
            if (!File.Exists(f))
            {
                _lastFeLog = "(Could not save the geometry to " + f + " - check that the project name/path is valid and writable.)";
                return new List<FeStrip>();
            }

            int logLengthBefore = My.MyProject.Forms.frmMain.txtLog.Text.Length;

            {
                var withBlock = My.MyProject.Forms.frmMain;
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine("load " + f);
                withBlock.p.StandardInput.WriteLine("oper");
                withBlock.p.StandardInput.WriteLine("x");
                withBlock.p.StandardInput.WriteLine("fe");
                withBlock.p.StandardInput.WriteLine(outFile);
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.Flush();
            }

            var deadline = DateTime.Now.AddSeconds(15d);
            long lastLen = -1;
            do
            {
                await Task.Delay(150);
                try
                {
                    if (File.Exists(outFile))
                    {
                        long len = new FileInfo(outFile).Length;
                        if (len > 0L && len == lastLen)
                            break;
                        lastLen = len;
                    }
                }
                catch
                {
                }
            }
            while (DateTime.Now < deadline);

            await Task.Delay(150);
            try
            {
                string logText = My.MyProject.Forms.frmMain.txtLog.Text;
                if (logLengthBefore <= logText.Length)
                {
                    _lastFeLog = logText.Substring(logLengthBefore);
                }
            }
            catch
            {
            }

            if (!File.Exists(outFile))
                return new List<FeStrip>();

            try
            {
                return ParseFeFile(outFile);
            }
            catch
            {
                return new List<FeStrip>();
            }
            finally
            {
                try
                {
                    File.Delete(outFile);
                }
                catch
                {
                }
            }
        }

        // Parses AVL's "FE" (element forces) output. Format confirmed against a
        // real AVL 3.37 run - one block per spanwise strip:
        // Strip #  1     # Chordwise =  8   First Vortex =   1
        // Xle =   0.00000 ...
        // Yle =   0.02139    Strip Width  =   0.08519 ...
        // cl  =   0.48163       cd  =   0.00462      cdv =   0.00000
        // I        X           Y           Z           DX        Slope        dCp
        // 1     0.00851     0.04259     0.00000     0.05242     0.00000     2.14797
        // dCp is the local chordwise pressure-coefficient jump across each panel.
        private List<FeStrip> ParseFeFile(string path)
        {
            var strips = new List<FeStrip>();
            string currentSurfaceName = "";
            FeStrip current = null;
            bool inTable = false;

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();

                var surfMatch = Regex.Match(line, @"^Surface #\s*\d+\s+(.+)$");
                if (surfMatch.Success)
                {
                    currentSurfaceName = surfMatch.Groups[1].Value.Trim();
                    continue;
                }

                var stripMatch = Regex.Match(line, @"^Strip #\s*(\d+)");
                if (stripMatch.Success)
                {
                    current = new FeStrip() { SurfaceName = currentSurfaceName, StripIndex = int.Parse(stripMatch.Groups[1].Value) };
                    strips.Add(current);
                    inTable = false;
                    continue;
                }

                if (current is null)
                    continue;

                if (line.StartsWith("Yle"))
                {
                    var m = Regex.Match(line, @"Yle\s*=\s*(-?[\d.]+)");
                    if (m.Success)
                        double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out current.Yle);
                    continue;
                }

                if (line.StartsWith("cl"))
                {
                    var mCl = Regex.Match(line, @"cl\s*=\s*(-?[\d.]+)");
                    var mCd = Regex.Match(line, @"cd\s*=\s*(-?[\d.]+)");
                    if (mCl.Success)
                        double.TryParse(mCl.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out current.Cl);
                    if (mCd.Success)
                        double.TryParse(mCd.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out current.Cd);
                    continue;
                }

                if (line.StartsWith("I ") && line.Contains("dCp"))
                {
                    inTable = true;
                    continue;
                }

                if (!inTable)
                    continue;

                string[] tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 7)
                {
                    inTable = false;
                    continue;
                }
                int idx;
                if (!int.TryParse(tokens[0], out idx))
                {
                    inTable = false;
                    continue;
                }
                double x;
                double dcp;
                if (double.TryParse(tokens[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x) && double.TryParse(tokens[6], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out dcp))
                {
                    current.Panels.Add(new FePanel() { X = x, DCp = dcp });
                }
            }

            return strips;
        }

        // Renders the chordwise dCp distribution for the currently selected span
        // station (cmbFeStrip), styled consistently with the other in-house plots.
        private void RenderFEPlot(bool captureVectors = false)
        {
            if (pFE is null || pFE.Width <= 0 || pFE.Height <= 0)
                return;

            int w = pFE.Width;
            int h = pFE.Height;
            var BMP = new Bitmap(w, h);
            var tickFont = GetTickFont(9f);
            var axisFont = GetAxisFont();
            string svgOut = "";
            string pdfOut = "";

            using (var G = new SvgGraphics(w, h, Graphics.FromImage(BMP), captureVectors))
            {
                G.SmoothingMode = SmoothingMode.AntiAlias;
                G.TextRenderingHint = TextRenderingHint.AntiAlias;
                G.Clear(ThemeCanvasBackColor);

                FeStrip strip = null;
                if (_lastFeStrips is not null && cmbFeStrip.SelectedIndex >= 0 && cmbFeStrip.SelectedIndex < _lastFeStrips.Count)
                {
                    strip = _lastFeStrips[cmbFeStrip.SelectedIndex];
                }

                if (strip is null || strip.Panels.Count == 0)
                {
                    G.DrawString("Run \"Pressure Analysis\" and pick a span station to see its chordwise dCp distribution.", tickFont, Brushes.Gray, new PointF(10f, 10f));
                }
                else
                {
                    float margin = 55f;
                    float plotW = w - margin * 2f;
                    float plotH = h - margin * 2f;

                    string title = $"dCp vs X/c  -  {strip.SurfaceName}  Y={strip.Yle:0.000}  (cl={strip.Cl:0.0000}, cd={strip.Cd:0.0000})";
                    DrawFeSubplot(G, tickFont, axisFont, margin, margin, plotW, plotH, title, strip);
                }

                if (captureVectors)
                {
                    svgOut = G.GetSvgContent();
                    pdfOut = G.GetPdfContentStream();
                }
            }

            var oldImage = pFE.Image;
            pFE.Image = BMP;
            oldImage?.Dispose();

            if (captureVectors)
            {
                feSvg = svgOut;
                fePdf = pdfOut;
            }
        }

        private void DrawFeSubplot(SvgGraphics G, Font tickFont, Font axisFont, float x, float y, float w, float h, string title, FeStrip strip)
        {
            double xMin = double.MaxValue;
            double xMax = double.MinValue;
            double vMin = double.MaxValue;
            double vMax = double.MinValue;
            foreach (var p in strip.Panels)
            {
                xMin = Math.Min(xMin, p.X);
                xMax = Math.Max(xMax, p.X);
                vMin = Math.Min(vMin, p.DCp);
                vMax = Math.Max(vMax, p.DCp);
            }
            vMin = Math.Min(0d, vMin);
            if (xMin == xMax)
            {
                xMin -= 1d;
                xMax += 1d;
            }
            if (vMin == vMax)
            {
                vMin -= 1d;
                vMax += 1d;
            }
            double vPad = (vMax - vMin) * 0.1d;
            vMin -= vPad;
            vMax += vPad;

            var whitePen = new Pen(ThemeAxisColor, 1f);
            var gridPen = new Pen(ThemeGridColor, 1f) { DashStyle = DashStyle.Dash };

            G.DrawRectangle(whitePen, x, y, w, h);
            G.DrawString(title, axisFont, new SolidBrush(ThemeAxisColor), new PointF(x, y - axisFont.Height - 2f));

            if (vMin < 0d && vMax > 0d)
            {
                float zeroY = (float)((double)(y + h) - (0d - vMin) / (vMax - vMin) * (double)h);
                G.DrawLine(gridPen, x, zeroY, x + w, zeroY);
            }

            var ordered = new List<FePanel>(strip.Panels);
            ordered.Sort((a, b) => a.X.CompareTo(b.X));
            var pen = new Pen(Color.Gold, 1.5f);
            var markerBrush = new SolidBrush(Color.Gold);
            var pts = new List<PointF>();
            foreach (var p in ordered)
            {
                float px = (float)((double)x + (p.X - xMin) / (xMax - xMin) * (double)w);
                float py = (float)((double)(y + h) - (p.DCp - vMin) / (vMax - vMin) * (double)h);
                pts.Add(new PointF(px, py));
            }
            if (pts.Count >= 2)
                G.DrawLines(pen, pts.ToArray());
            foreach (var pt in pts)
                G.FillEllipse(markerBrush, pt.X - 2.5f, pt.Y - 2.5f, 5f, 5f);

            G.DrawString(xMin.ToString("0.00"), tickFont, new SolidBrush(ThemeAxisColor), new PointF(x, y + h + 2f));
            string maxLabel = xMax.ToString("0.00");
            var maxLabelSize = G.MeasureString(maxLabel, tickFont);
            G.DrawString(maxLabel, tickFont, new SolidBrush(ThemeAxisColor), new PointF(x + w - maxLabelSize.Width, y + h + 2f));
            var xAxisLabelSize = G.MeasureString("X/c", tickFont);
            G.DrawString("X/c", tickFont, new SolidBrush(ThemeAxisColor), new PointF(x + w / 2f - xAxisLabelSize.Width / 2f, y + h + 2f));

            string vMaxLabel = vMax.ToString("0.00");
            var vMaxLabelSize = G.MeasureString(vMaxLabel, tickFont);
            G.DrawString(vMaxLabel, tickFont, new SolidBrush(ThemeAxisColor), new PointF(x - vMaxLabelSize.Width - 2f, y));
            string vMinLabel = vMin.ToString("0.00");
            var vMinLabelSize = G.MeasureString(vMinLabel, tickFont);
            G.DrawString(vMinLabel, tickFont, new SolidBrush(ThemeAxisColor), new PointF(x - vMinLabelSize.Width - 2f, y + h - vMinLabelSize.Height));
        }

        private async void RunModesAnalysis_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                AppMessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }
            if (My.MyProject.Forms.frmMain.p is null || My.MyProject.Forms.frmMain.p.HasExited)
                return;

            string massPath = Path.Combine(Application.StartupPath, $"{projectName}.mass");
            string runPath = Path.Combine(Application.StartupPath, $"{projectName}.run");
            if (!File.Exists(massPath) || !File.Exists(runPath))
            {
                var res = AppMessageBox.Show("No saved Mass and/or Run file found for this project." + Constants.vbCrLf + Constants.vbCrLf + "Eigenmode analysis still needs mass/inertia and a trimmed, non-zero velocity to mean anything - " + "without them AVL uses placeholder values (mass=1kg, Ixx=Iyy=Izz=1) and the result won't reflect your aircraft." + Constants.vbCrLf + Constants.vbCrLf + "Continue anyway?", "Missing Mass/Run Data", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.No)
                    return;
            }

            btnRunModes.Enabled = false;
            _lastModesHoverIndex = -1;
            try
            {
                var eigs = await RunModesAnalysisAsync(massPath, runPath);

                // Match each eigenvalue (from the "W" file) to its eigenvector
                // (from the console transcript, only source that reports it) by
                // position - both come from the same "N" computation in the same
                // order, since this app only ever runs a single selected case.
                if (eigs is not null && eigs.Count > 0)
                {
                    var vectors = ParseModeEigenvectors(_lastModesLog);
                    if (vectors.Count == eigs.Count)
                    {
                        for (int i = 0, loopTo = eigs.Count - 1; i <= loopTo; i++)
                        {
                            eigs[i].MagU = vectors[i].MagU;
                            eigs[i].MagV = vectors[i].MagV;
                            eigs[i].MagW = vectors[i].MagW;
                            eigs[i].MagP = vectors[i].MagP;
                            eigs[i].MagQ = vectors[i].MagQ;
                            eigs[i].MagR = vectors[i].MagR;
                            eigs[i].MagPhi = vectors[i].MagPhi;
                            eigs[i].MagTheta = vectors[i].MagTheta;
                            eigs[i].MagPsi = vectors[i].MagPsi;
                        }
                        ClassifyModes(eigs);
                    }
                }

                _lastEigenvalues = eigs;
                RenderModesPlot();

                if (eigs is null || eigs.Count == 0)
                {
                    string debugPath = Path.Combine(Application.StartupPath, $"{projectName}_modes_debug.txt");
                    try
                    {
                        File.WriteAllText(debugPath, _lastModesLog ?? "");
                        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{debugPath}\"") { UseShellExecute = true });
                    }
                    catch
                    {
                    }
                    AppMessageBox.Show("AVL did not return any eigenvalues." + Constants.vbCrLf + Constants.vbCrLf + "This usually means the trim/mass data isn't valid (e.g. zero velocity)." + Constants.vbCrLf + Constants.vbCrLf + "Full details written to:" + Constants.vbCrLf + debugPath, "Eigenvalue Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    tc1.SelectedTab = ModesTab;
                }
            }
            finally
            {
                btnRunModes.Enabled = true;
            }
        }

        // Runs AVL's top-level ".MODE" menu (NOT under OPER) to compute dynamic-
        // stability eigenvalues: load -> [mass/mset] -> [case] -> oper -> x ->
        // <blank to exit OPER> -> mode -> n (compute) -> w -> <file> -> <blank to
        // exit MODE>. Confirmed against real AVL - "mode" is only valid from the
        // top-level menu, so OPER must be explicitly exited with a blank line first
        // (chaining straight from "x" the way other OPER commands chain does NOT
        // work here). Does NOT force-save the Mass/Run tabs (see RunModesAnalysis_
        // Click) since txt3 may currently hold a different tab's content - only
        // uses massPath/runPath if they already exist on disk.
        private async Task<List<EigenValue>> RunModesAnalysisAsync(string massPath, string runPath)
        {
            string f = Path.Combine(Application.StartupPath, $"{projectName}.avl");
            string outFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            SaveAVL();
            _lastModesLog = "";
            if (!File.Exists(f))
            {
                _lastModesLog = "(Could not save the geometry to " + f + " - check that the project name/path is valid and writable.)";
                return new List<EigenValue>();
            }

            int logLengthBefore = My.MyProject.Forms.frmMain.txtLog.Text.Length;

            {
                var withBlock = My.MyProject.Forms.frmMain;
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine();
                // The "N" (new eigenmode calculation) command inherently pops open
                // AVL's own native root-locus graphics window as a side effect (per
                // AVL's docs, unlike the FS/VM/ST/SB/FN/FB/HM/FE commands used
                // elsewhere in this app, which are pure text/file output with no
                // graphics side effect) - with no window-close command in this
                // scripted flow, that window would be left open indefinitely.
                // Disabling AVL's graphics-enable flag first (PLOP -> G) prevents
                // it from ever opening at all; confirmed against real AVL that the
                // eigenvalue computation and file write still work identically.
                withBlock.p.StandardInput.WriteLine("plop");
                withBlock.p.StandardInput.WriteLine("g");
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine("load " + f);
                if (File.Exists(massPath))
                {
                    withBlock.p.StandardInput.WriteLine("mass " + massPath);
                    withBlock.p.StandardInput.WriteLine("mset 1");
                }
                if (File.Exists(runPath))
                {
                    withBlock.p.StandardInput.WriteLine("case " + runPath);
                }
                withBlock.p.StandardInput.WriteLine("oper");
                withBlock.p.StandardInput.WriteLine("x");
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.WriteLine("mode");
                withBlock.p.StandardInput.WriteLine("n");
                withBlock.p.StandardInput.WriteLine("w");
                withBlock.p.StandardInput.WriteLine(outFile);
                withBlock.p.StandardInput.WriteLine();
                withBlock.p.StandardInput.Flush();
            }

            var deadline = DateTime.Now.AddSeconds(15d);
            long lastLen = -1;
            do
            {
                await Task.Delay(150);
                try
                {
                    if (File.Exists(outFile))
                    {
                        long len = new FileInfo(outFile).Length;
                        if (len > 0L && len == lastLen)
                            break;
                        lastLen = len;
                    }
                }
                catch
                {
                }
            }
            while (DateTime.Now < deadline);

            await Task.Delay(150);
            try
            {
                string logText = My.MyProject.Forms.frmMain.txtLog.Text;
                if (logLengthBefore <= logText.Length)
                {
                    _lastModesLog = logText.Substring(logLengthBefore);
                }
            }
            catch
            {
            }

            if (!File.Exists(outFile))
                return new List<EigenValue>();

            try
            {
                return ParseEigFile(outFile);
            }
            catch
            {
                return new List<EigenValue>();
            }
            finally
            {
                try
                {
                    File.Delete(outFile);
                }
                catch
                {
                }
            }
        }

        // Parses AVL's ".MODE" -> "W" eigenvalue output. Format confirmed against
        // a real AVL 3.37 run:
        // # [AR Plane]
        // #
        // #   Run case     Eigenvalue
        // 1    -15.427003         33.093903
        private List<EigenValue> ParseEigFile(string path)
        {
            var results = new List<EigenValue>();
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;
                string[] tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 3)
                    continue;
                int rc;
                double re;
                double im;
                if (int.TryParse(tokens[0], out rc) && double.TryParse(tokens[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out re) && double.TryParse(tokens[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out im))
                {
                    results.Add(new EigenValue() { RunCase = rc, Real = re, Imag = im });
                }
            }
            return results;
        }

        // Parses the eigenvector blocks AVL's "N" (new eigenmode calculation)
        // command prints to the console for each mode - confirmed against a real
        // AVL 3.37 run:
        // mode 1:  -15.4270       33.0939
        // u  :     0.0000    -0.0000      v  :    -0.1575    -0.1720      x  : -0.3561E-09 -0.5899E-10
        // w  :     0.0000     0.0000      p  :    -0.0082    -0.0579      y  : -0.1560E-02 -0.9599E-03
        // ...
        // Scans each per-mode chunk for every "name : re im" triple regardless of
        // which line it's wrapped onto, rather than assuming a fixed column
        // layout. This is separate from the eigenvalues themselves (which come
        // from the "W" file via ParseEigFile) - the console transcript is the only
        // place AVL reports the eigenvectors needed to classify each mode.
        private List<EigenValue> ParseModeEigenvectors(string logText)
        {
            var results = new List<EigenValue>();
            if (string.IsNullOrEmpty(logText))
                return results;

            string numPat = @"(-?\d+\.?\d*(?:[eE][-+]?\d+)?)";
            string modeHeaderPat = @"mode\s+\d+:\s*" + numPat + @"\s+" + numPat;
            var headers = Regex.Matches(logText, modeHeaderPat);
            for (int i = 0, loopTo = headers.Count - 1; i <= loopTo; i++)
            {
                var h = headers[i];
                int blockStart = h.Index + h.Length;
                int blockEnd = i + 1 < headers.Count ? headers[i + 1].Index : logText.Length;
                string block = logText.Substring(blockStart, blockEnd - blockStart);

                var ev = new EigenValue();
                ev.Real = double.Parse(h.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                ev.Imag = double.Parse(h.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

                string varPat = @"(\w+)\s*:\s*" + numPat + @"\s+" + numPat;
                foreach (Match m in Regex.Matches(block, varPat))
                {
                    string name = m.Groups[1].Value.ToLowerInvariant();
                    double re;
                    double im;
                    if (!double.TryParse(m.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out re))
                        continue;
                    if (!double.TryParse(m.Groups[3].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out im))
                        continue;
                    double mag = Math.Sqrt(re * re + im * im);
                    switch (name ?? "")
                    {
                        case "u":
                            {
                                ev.MagU = mag;
                                break;
                            }
                        case "v":
                            {
                                ev.MagV = mag;
                                break;
                            }
                        case "w":
                            {
                                ev.MagW = mag;
                                break;
                            }
                        case "p":
                            {
                                ev.MagP = mag;
                                break;
                            }
                        case "q":
                            {
                                ev.MagQ = mag;
                                break;
                            }
                        case "r":
                            {
                                ev.MagR = mag;
                                break;
                            }
                        case "phi":
                            {
                                ev.MagPhi = mag;
                                break;
                            }
                        case "the":
                            {
                                ev.MagTheta = mag;
                                break;
                            }
                        case "psi":
                            {
                                ev.MagPsi = mag;
                                break;
                            }
                    }
                }
                results.Add(ev);
            }
            return results;
        }

        // Classifies each mode as a named flight-dynamics mode using standard
        // heuristics: which state group dominates the eigenvector (u/w/q/theta =
        // longitudinal, v/p/r/phi = lateral-directional), whether the root is
        // complex (oscillatory) or real, and - since phugoid vs short-period and
        // spiral vs roll-subsidence are only distinguishable by RELATIVE frequency/
        // speed, not an absolute threshold - ranking same-category roots against
        // each other (matching how this is actually done in flight-dynamics
        // analysis). This is a best-effort label, not a guaranteed-correct
        // classification for unusual configurations.
        private void ClassifyModes(List<EigenValue> modes)
        {
            double longEnergy(EigenValue m) => m.MagU * m.MagU + m.MagW * m.MagW + m.MagQ * m.MagQ + m.MagTheta * m.MagTheta;
            double latEnergy(EigenValue m) => m.MagV * m.MagV + m.MagP * m.MagP + m.MagR * m.MagR + m.MagPhi * m.MagPhi;
            double freq(EigenValue m) => Math.Sqrt(m.Real * m.Real + m.Imag * m.Imag);

            var longModes = new List<EigenValue>();
            var latModes = new List<EigenValue>();
            foreach (var m in modes)
            {
                if (longEnergy(m) >= latEnergy(m))
                {
                    longModes.Add(m);
                }
                else
                {
                    latModes.Add(m);
                }
            }

            var longOsc = longModes.FindAll(m => Math.Abs(m.Imag) > 0.001d);
            var longReal = longModes.FindAll(m => Math.Abs(m.Imag) <= 0.001d);
            longOsc.Sort((a, b) => freq(a).CompareTo(freq(b)));
            foreach (var m in longOsc)
                m.ModeLabel = freq(m) < 2.0d ? "Phugoid" : "Short Period";
            foreach (var m in longReal)
                m.ModeLabel = "Longitudinal";

            var latOsc = latModes.FindAll(m => Math.Abs(m.Imag) > 0.001d);
            var latReal = latModes.FindAll(m => Math.Abs(m.Imag) <= 0.001d);
            latReal.Sort((a, b) => Math.Abs(b.Real).CompareTo(Math.Abs(a.Real)));
            foreach (var m in latOsc)
                m.ModeLabel = "Dutch Roll";
            for (int i2 = 0, loopTo = latReal.Count - 1; i2 <= loopTo; i2++)
            {
                if (i2 == 0 && latReal.Count > 1)
                {
                    latReal[i2].ModeLabel = "Roll Subsidence";
                }
                else if (Math.Abs(latReal[i2].Real) < 1.0d)
                {
                    latReal[i2].ModeLabel = "Spiral";
                }
                else
                {
                    latReal[i2].ModeLabel = "Roll Subsidence";
                }
            }
        }

        // Wires a small button to pop up the raw AVL command sequence that reproduces
        // this tab's result if the user runs AVL directly (outside this app) - each
        // tab drives AVL via its own StandardInput script, and this just documents
        // that same script in AVL's own command-line terms.
        private void StyleAvlCommandsButton(System.Windows.Forms.Button btn, string dialogTitle, string commandsText)
        {
            btn.Text = "AVL Commands";
            btn.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            btn.BackColor = Color.White;
            btn.ForeColor = Color.Black;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.LightGray;
            btn.Cursor = Cursors.Hand;
            btn.Click += (s, ev) => AppMessageBox.Show(commandsText, dialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Shows a popup with mode-specific suggestions for turning an unstable
        // root stable via geometry or mass-distribution changes. Standard,
        // textbook flight-dynamics guidance (Nelson, Etkin) rather than anything
        // computed from this specific aircraft's derivatives - a starting point to
        // try, not a guarantee, and re-running the analysis after a change is the
        // only way to confirm it actually helped (fixes for one mode can worsen
        // another - e.g. fin size trades off Dutch roll against spiral stability).
        private void ShowModeStabilityTips_Click(object sender, EventArgs e)
        {
            if (_lastEigenvalues is null || _lastEigenvalues.Count == 0)
            {
                AppMessageBox.Show("Run the eigenvalue analysis first.", "Stability Tips", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var unstable = _lastEigenvalues.FindAll(m => m.Real >= 0d);
            if (unstable.Count == 0)
            {
                AppMessageBox.Show("All computed modes are stable (every root has a negative real part) - no changes needed." + Constants.vbCrLf + Constants.vbCrLf + "This only reflects the mass/inertia and trim condition used for this run - re-check after any significant geometry or loading change.", "Stability Tips", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var seenLabels = new List<string>();
            var sb = new StringBuilder();
            sb.AppendLine($"{unstable.Count} unstable mode(s) found (positive real part = a growing oscillation or divergence):");
            sb.AppendLine();
            foreach (var m in unstable)
            {
                string label = string.IsNullOrEmpty(m.ModeLabel) ? "Unclassified" : m.ModeLabel;
                if (seenLabels.Contains(label))
                    continue; // a complex-conjugate pair shares one tip
                seenLabels.Add(label);
                sb.AppendLine(GetStabilityTip(label));
                sb.AppendLine();
            }
            sb.Append("These are general aerodynamic guidelines, not computed for this specific configuration. " + "After making a change, re-run the analysis to confirm it actually helped - a fix for one mode can weaken another.");

            AppMessageBox.Show(sb.ToString(), "Stability Improvement Tips", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private string GetStabilityTip(string modeLabel)
        {
            switch (modeLabel ?? "")
            {
                case "Short Period":
                    {
                        return "SHORT PERIOD (longitudinal, fast pitch oscillation)" + Constants.vbCrLf + "Usually caused by insufficient static margin - the CG sitting too close to, or behind, the neutral point." + Constants.vbCrLf + "Try: moving the CG forward (shift mass forward - battery/payload/engine placement), or increasing the horizontal " + "tail's stabilizing effect (larger tail area, or a longer tail moment arm) to move the neutral point aft. " + "Either one increases the static margin, which directly increases short-period stability.";
                    }
                case "Phugoid":
                    {
                        return "PHUGOID (longitudinal, slow speed/altitude exchange)" + Constants.vbCrLf + "Less commonly unstable than short period. Usually linked to weak pitch damping, or the short-period and phugoid " + "modes interacting badly because the static margin is very small." + Constants.vbCrLf + "Try: increasing pitch damping (a larger or longer-arm horizontal tail increases Cmq), and confirming there's a " + "healthy static margin. Since the phugoid is fundamentally a slow trade between speed and altitude (kinetic vs " + "potential energy), reducing excess drag at the trim condition can also help.";
                    }
                case "Dutch Roll":
                    {
                        return "DUTCH ROLL (lateral-directional, yaw/roll oscillation)" + Constants.vbCrLf + "Usually caused by too little directional stability or yaw damping relative to the dihedral effect." + Constants.vbCrLf + "Try: increasing vertical tail (fin) area, or moving it further aft for a longer moment arm - this increases both " + "directional stability and yaw damping. Reducing excessive wing dihedral (or sweep, which acts like added " + "dihedral) can also help. Trade-off: a bigger fin helps Dutch roll but can hurt spiral stability (see below) - " + "aircraft design usually balances the two rather than maximizing either.";
                    }
                case "Spiral":
                    {
                        return "SPIRAL (lateral-directional, slow roll/heading divergence)" + Constants.vbCrLf + "Usually caused by too much directional stability (a big fin) relative to dihedral effect - the aircraft " + "\"weathervanes\" into a bank faster than the dihedral rolls it back level." + Constants.vbCrLf + "Try: increasing wing dihedral angle (or sweep), or reducing vertical tail size/moment arm. Spiral instability is " + "usually the mildest and slowest of the classic instabilities and is easy for a pilot (or autopilot) to correct, " + "so a small amount is common even in certified aircraft - only worth chasing if it's fast.";
                    }
                case "Roll Subsidence":
                    {
                        return "ROLL SUBSIDENCE (lateral, roll-rate damping)" + Constants.vbCrLf + "This mode is almost always stable for a conventional aircraft - roll damping is inherently stabilizing for any " + "wing that's generating lift. If it's showing unstable here, double-check the mass/inertia data (Ixx) and control " + "surface definitions first - that usually points to a setup issue rather than a real aerodynamic problem.";
                    }
                case "Longitudinal":
                    {
                        return "LONGITUDINAL (non-oscillatory)" + Constants.vbCrLf + "A real (non-oscillatory) unstable longitudinal root, dominated by speed/pitch states rather than a classic " + "phugoid or short-period oscillation." + Constants.vbCrLf + "Try the same fixes as short period: move the CG forward, or increase horizontal tail size/moment arm to add " + "static margin.";
                    }

                default:
                    {
                        return "UNCLASSIFIED MODE" + Constants.vbCrLf + "This root's eigenvector wasn't clearly dominated by either the longitudinal or lateral-directional state group, " + "so a specific recommendation isn't available. Check the geometry for anything unusual (e.g. a canard, an " + "unconventional tail, or strongly coupled control surfaces) that could mix the two axes together.";
                    }
            }
        }

        // Renders the eigenvalues in the complex plane (real vs imaginary part) -
        // a standard root-locus plot. Points are colored by stability (stable/
        // left-half-plane = green, unstable/right-half-plane = red), with a
        // vertical line marking the Real=0 stability boundary.
        private void RenderModesPlot(bool captureVectors = false)
        {
            if (pModes is null || pModes.Width <= 0 || pModes.Height <= 0)
                return;

            int w = pModes.Width;
            int h = pModes.Height;
            var BMP = new Bitmap(w, h);
            var tickFont = GetTickFont(9f);
            var axisFont = GetAxisFont();
            string svgOut = "";
            string pdfOut = "";

            using (var G = new SvgGraphics(w, h, Graphics.FromImage(BMP), captureVectors))
            {
                G.SmoothingMode = SmoothingMode.AntiAlias;
                G.TextRenderingHint = TextRenderingHint.AntiAlias;
                G.Clear(ThemeCanvasBackColor);

                if (_lastEigenvalues is null || _lastEigenvalues.Count == 0)
                {
                    G.DrawString("Run \"Run Eigenvalue Analysis\" to compute and plot the root locus.", tickFont, Brushes.Gray, new PointF(10f, 10f));
                }
                else
                {
                    float margin = 55f;
                    float plotW = w - margin * 2f;
                    float plotH = h - margin * 2f;
                    DrawModesSubplot(G, tickFont, axisFont, margin, margin, plotW, plotH);
                }

                if (captureVectors)
                {
                    svgOut = G.GetSvgContent();
                    pdfOut = G.GetPdfContentStream();
                }
            }

            var oldImage = pModes.Image;
            pModes.Image = BMP;
            oldImage?.Dispose();

            if (captureVectors)
            {
                modesSvg = svgOut;
                modesPdf = pdfOut;
            }
        }

        private void DrawModesSubplot(SvgGraphics G, Font tickFont, Font axisFont, float x, float y, float w, float h)
        {
            double xMin = double.MaxValue;
            double xMax = double.MinValue;
            double yMin = double.MaxValue;
            double yMax = double.MinValue;
            foreach (var ev in _lastEigenvalues)
            {
                xMin = Math.Min(xMin, ev.Real);
                xMax = Math.Max(xMax, ev.Real);
                yMin = Math.Min(yMin, ev.Imag);
                yMax = Math.Max(yMax, ev.Imag);
            }
            xMin = Math.Min(0d, xMin);
            xMax = Math.Max(0d, xMax);
            yMin = Math.Min(0d, yMin);
            yMax = Math.Max(0d, yMax);
            if (xMin == xMax)
            {
                xMin -= 1d;
                xMax += 1d;
            }
            if (yMin == yMax)
            {
                yMin -= 1d;
                yMax += 1d;
            }
            double xPad = (xMax - xMin) * 0.15d;
            double yPad = (yMax - yMin) * 0.15d;
            xMin -= xPad;
            xMax += xPad;
            yMin -= yPad;
            yMax += yPad;

            // Apply the current zoom/pan around the auto-fit range computed above.
            double fitCenterX = (xMin + xMax) / 2.0d;
            double fitCenterY = (yMin + yMax) / 2.0d;
            double halfW = (xMax - xMin) / 2.0d / _modesZoom;
            double halfH = (yMax - yMin) / 2.0d / _modesZoom;
            xMin = fitCenterX + _modesPanX - halfW;
            xMax = fitCenterX + _modesPanX + halfW;
            yMin = fitCenterY + _modesPanY - halfH;
            yMax = fitCenterY + _modesPanY + halfH;

            // Cache the bounds and pixel rect so the mouse-wheel handler can map cursor
            // position back to data coordinates for zoom-to-cursor.
            _modesXMin = xMin;
            _modesXMax = xMax;
            _modesYMin = yMin;
            _modesYMax = yMax;
            _modesPlotX = x;
            _modesPlotY = y;
            _modesPlotW = w;
            _modesPlotH = h;

            var whitePen = new Pen(ThemeAxisColor, 1f);
            var gridPen = new Pen(ThemeGridColor, 1f) { DashStyle = DashStyle.Dash };
            var stabilityPen = new Pen(Color.Gold, 1.5f) { DashStyle = DashStyle.Dash };

            G.DrawRectangle(whitePen, x, y, w, h);
            G.DrawString("Root Locus  (Real vs Imaginary part of each eigenvalue)", axisFont, new SolidBrush(ThemeAxisColor), new PointF(x, y - axisFont.Height - 2f));

            // Stability boundary (Real = 0) and Imag = 0 axis
            if (xMin < 0d && xMax > 0d)
            {
                float zeroX = (float)((double)x + (0d - xMin) / (xMax - xMin) * (double)w);
                G.DrawLine(stabilityPen, zeroX, y, zeroX, y + h);
            }
            if (yMin < 0d && yMax > 0d)
            {
                float zeroY = (float)((double)(y + h) - (0d - yMin) / (yMax - yMin) * (double)h);
                G.DrawLine(gridPen, x, zeroY, x + w, zeroY);
            }

            var stableBrush = new SolidBrush(Color.LimeGreen);
            var unstableBrush = new SolidBrush(Color.OrangeRed);
            foreach (var ev in _lastEigenvalues)
            {
                float px = (float)((double)x + (ev.Real - xMin) / (xMax - xMin) * (double)w);
                float py = (float)((double)(y + h) - (ev.Imag - yMin) / (yMax - yMin) * (double)h);
                ev.ScreenX = px;
                ev.ScreenY = py;
                // Skip points panned/zoomed off the visible plot rect entirely.
                if (px < x - 4f || px > x + w + 4f || py < y - 4f || py > y + h + 4f)
                    continue;
                var brush = ev.Real < 0d ? stableBrush : unstableBrush;
                G.FillEllipse(brush, px - 4f, py - 4f, 8f, 8f);
                if (!string.IsNullOrEmpty(ev.ModeLabel))
                {
                    G.DrawString(ev.ModeLabel, tickFont, new SolidBrush(ThemeMutedColor), new PointF(px + 6f, py - tickFont.Height - 2f));
                }
            }

            G.DrawString(xMin.ToString("0.0"), tickFont, new SolidBrush(ThemeAxisColor), new PointF(x, y + h + 2f));
            string maxLabel = xMax.ToString("0.0");
            var maxLabelSize = G.MeasureString(maxLabel, tickFont);
            G.DrawString(maxLabel, tickFont, new SolidBrush(ThemeAxisColor), new PointF(x + w - maxLabelSize.Width, y + h + 2f));
            var xAxisLabelSize = G.MeasureString("Real", tickFont);
            G.DrawString("Real", tickFont, new SolidBrush(ThemeAxisColor), new PointF(x + w / 2f - xAxisLabelSize.Width / 2f, y + h + 2f));

            string yMaxLabel = yMax.ToString("0.0");
            var yMaxLabelSize = G.MeasureString(yMaxLabel, tickFont);
            G.DrawString(yMaxLabel, tickFont, new SolidBrush(ThemeAxisColor), new PointF(x - yMaxLabelSize.Width - 2f, y));
            string yMinLabel = yMin.ToString("0.0");
            var yMinLabelSize = G.MeasureString(yMinLabel, tickFont);
            G.DrawString(yMinLabel, tickFont, new SolidBrush(ThemeAxisColor), new PointF(x - yMinLabelSize.Width - 2f, y + h - yMinLabelSize.Height));

            // Legend
            float legendX = x + w - 110f;
            float legendY = y + 8f;
            G.FillEllipse(stableBrush, legendX, legendY, 8f, 8f);
            G.DrawString("stable (Re<0)", tickFont, Brushes.LimeGreen, new PointF(legendX + 12f, legendY - 2f));
            G.FillEllipse(unstableBrush, legendX, legendY + 16f, 8f, 8f);
            G.DrawString("unstable (Re" + Strings.ChrW(0x2265) + "0)", tickFont, Brushes.OrangeRed, new PointF(legendX + 12f, legendY + 14f));
        }

        private void txtName_Click(object sender, EventArgs e)
        {

        }

        private string GetPlaceholderText()
        {
            if (My.MyProject.Forms.frmMain.curApp.ToLower() == "avl")
            {
                return "Enter AVL Project (e.g. glider)";
            }
            else
            {
                return "Enter NACA (e.g. 2412) or dat file";
            }
        }

        private void SetPlaceholder()
        {
            if (string.IsNullOrEmpty(txtName.Text) || txtName.Text == "Enter AVL Project (e.g. glider)" || txtName.Text == "Enter NACA (e.g. 2412) or dat file")
            {
                txtName.TextChanged -= txtName_TextChanged;
                txtName.Text = GetPlaceholderText();
                txtName.ComboBox.ForeColor = Color.Gray;
                txtName.TextChanged += txtName_TextChanged;
            }
        }

        private void ClearPlaceholder()
        {
            if (txtName.Text == "Enter AVL Project (e.g. glider)" || txtName.Text == "Enter NACA (e.g. 2412) or dat file")
            {
                txtName.TextChanged -= txtName_TextChanged;
                txtName.Text = "";
                txtName.ComboBox.ForeColor = Color.Black;
                txtName.TextChanged += txtName_TextChanged;
            }
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            // This window's own state (projectName/title/warning) must stay in sync with its own
            // txtName text regardless of WHY that text changed - including when it was just set by
            // frmMain (or another Geometry window via frmMain) syncing a project switch into us
            // (frmMain.IsSyncingProject = True at that point). Only the re-broadcast to frmMain below
            // needs to skip during a sync, to avoid an infinite ping-pong between forms - otherwise
            // this window's own warning label kept showing the stale pre-sync project name.
            string txt = txtName.Text;
            if (txt == "Enter AVL Project (e.g. glider)" || txt == "Enter NACA (e.g. 2412) or dat file")
            {
                projectName = "";
            }
            else
            {
                projectName = txt;
            }

            UpdateGeometryTitle();
            UpdateProjectWarning();

            if (frmMain.IsSyncingProject)
                return;

            // Sync with frmMain
            frmMain.IsSyncingProject = true;
            try
            {
                if ((My.MyProject.Forms.frmMain.txtName.Text ?? "") != (txtName.Text ?? ""))
                {
                    My.MyProject.Forms.frmMain.txtName.Text = txtName.Text;
                }
                if (txtName.Text == "Enter AVL Project (e.g. glider)" || txtName.Text == "Enter NACA (e.g. 2412) or dat file")
                {
                    My.MyProject.Forms.frmMain.txtName.ComboBox.ForeColor = Color.Gray;
                }
                else
                {
                    My.MyProject.Forms.frmMain.txtName.ComboBox.ForeColor = Color.Black;
                }
            }
            finally
            {
                frmMain.IsSyncingProject = false;
            }
        }

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                e.SuppressKeyPress = true;
                LoadActiveProject();
            }
        }

        private void txtName_Leave(object sender, EventArgs e)
        {
            LoadActiveProject();
        }

        // Switches to whatever project name is currently in txtName - used both when the user
        // types a new/existing name and presses Enter/tabs away, and when they pick an existing
        // name from the dropdown (see txtName_SelectedIndexChanged).
        // 
        // Always flushes the outgoing project's in-progress edits to disk first, unconditionally -
        // it does NOT gate on isDirty/autoSave like tab-switching does. isDirty is only set by the
        // text editor's debounced TextChangedDelayed handler (a ~200ms delay), and autoSave's own
        // save runs on that same debounce - so a user who types and immediately hits Enter here
        // could race past both and have their edit silently dropped when the new project's
        // (possibly blank) content overwrites txt3. Saving unconditionally removes that race
        // entirely instead of trying to close the timing window. Saves target loadedProjectName,
        // not projectName - by the time this runs, projectName already equals the NEW name (kept
        // live-synced by txtName_TextChanged on every keystroke), so saving to projectName here
        // would misfile the outgoing project's edits under the incoming project's filename.
        private async void LoadActiveProject()
        {
            if (string.IsNullOrEmpty(projectName))
                return;

            if (!string.IsNullOrEmpty(loadedProjectName))
            {
                switch (tc1.SelectedTab.Name ?? "")
                {
                    case "Geometry":
                        {
                            SaveAVL();
                            break;
                        }
                    case "Mass":
                        {
                            SaveMass();
                            break;
                        }
                    case "Run":
                        {
                            SaveRun();
                            break;
                        }
                }
                isDirty = false;
                UpdateDirtyWarning();
            }

            // projectName is normally kept live-synced with txtName.Text by txtName_TextChanged on
            // every keystroke, but that handler no-ops while frmMain is mid-sync across open
            // Geometry windows (see frmMain.IsSyncingProject) - re-assert it here so LoadAVL/
            // LoadMass/LoadRun (which read projectName, not txtName.Text) never load a stale name.
            projectName = txtName.Text;

            // Reload files based on selected tab
            switch (tc1.SelectedTab.Name ?? "")
            {
                case "Geometry":
                    {
                        LoadAVL();
                        // Wait out the editor's own re-parse debounce before fitting the view, so
                        // the fit is computed from the newly-loaded geometry, not whatever the
                        // previous project last parsed into points/parsedSurfaces.
                        await Task.Delay(txt3.DelayedTextChangedInterval + 100);
                        btnFitAll_Click(null, null);
                        break;
                    }
                case "Mass":
                    {
                        LoadMass();
                        break;
                    }
                case "Run":
                    {
                        LoadRun();
                        break;
                    }
            }
        }

        public void UpdateGeometryTitle()
        {
            string activeFile = "";
            if (tc1.SelectedTab is not null)
            {
                switch (tc1.SelectedTab.Name ?? "")
                {
                    case "Geometry":
                        {
                            activeFile = $"{projectName}.avl";
                            break;
                        }
                    case "Mass":
                        {
                            activeFile = $"{projectName}.mass";
                            break;
                        }
                    case "Run":
                        {
                            activeFile = $"{projectName}.run";
                            break;
                        }
                }
            }

            string versionStr = My.MyProject.Application.Info.Version.ToString();
            Text = $"Geometry Designer (v{versionStr}) - Active File: {activeFile} (Auto-saved)";
        }

        public void UpdateProjectWarning()
        {
            ToolStripLabel lbl = ToolStrip1.Items["lblWarning"] as ToolStripLabel;
            if (lbl is not null)
            {
                // The "project name is empty" case used to live here too, but UpdateProjectGate()
                // now owns that message (as the full-window "No project selected" overlay) and
                // handles it more accurately - this label's old text claimed saving/autosave/
                // analysis were "disabled", which isn't even true anymore for the one case where
                // projectName can still be empty while a project IS loaded (mid-edit of the name
                // box - see UpdateProjectGate()'s comment), since Save*() targets loadedProjectName,
                // not the live-typed projectName.
                if (!string.IsNullOrEmpty(projectName) && projectName.ToLower() == "test")
                {
                    lbl.Text = "⚠️ Warning: Using default 'test' project. Changes will be overwritten!";
                    lbl.Visible = true;
                }
                else
                {
                    lbl.Visible = false;
                }
            }
        }

        // Builds the full-window overlay shown whenever no project is loaded - see
        // UpdateProjectGate() for what it actually locks down and why.
        private void InitializeProjectGate()
        {
            if (pnlProjectGate is not null)
                return;

            pnlProjectGate = new System.Windows.Forms.Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke
            };

            var lbl = new System.Windows.Forms.Label()
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 12.0f, FontStyle.Regular),
                Text = "No project selected." + Constants.vbCrLf + Constants.vbCrLf + "Type a new project name in the box above and press Enter, or choose" + Constants.vbCrLf + "an existing one from the dropdown, to start editing."
            };
            pnlProjectGate.Controls.Add(lbl);

            Controls.Add(pnlProjectGate);
        }

        // Locks the whole editor down to just the project selector (name box, its label, Help,
        // and the File menu, whose own Load Test Project item stays enabled below - itself a
        // valid way to get a project going, and the easiest onboarding path for a new user
        // facing the locked screen) until a project has actually finished loading - so nothing
        // (editing, running analysis, autosave) can act on files that aren't associated with any
        // project. Gates on loadedProjectName (set only when LoadAVL/LoadMass/LoadRun actually
        // completes), not projectName (which is live-synced to whatever's currently typed, before
        // the switch is committed) - so the UI unlocks exactly when the user finishes creating/
        // loading a project, not partway through typing a name.
        private void UpdateProjectGate()
        {
            if (pnlProjectGate is null)
                return;

            bool hasProject = !string.IsNullOrEmpty(loadedProjectName);

            sc1.Visible = hasProject;
            pnlProjectGate.Visible = !hasProject;
            if (!hasProject)
                pnlProjectGate.BringToFront();

            foreach (ToolStripItem item in ToolStrip1.Items)
            {
                if (!ReferenceEquals(item, ToolStripLabel1) && !ReferenceEquals(item, txtName) && !ReferenceEquals(item, btnHelp) && !ReferenceEquals(item, btnFileMenu))
                {
                    item.Enabled = hasProject;
                }
            }
            ToolStrip2.Enabled = hasProject;

            // Within the File menu: Load Test Project stays available (it's itself a valid way
            // to get a project going), but Make a Copy needs an actual loaded project to copy.
            btnMakeCopy.Enabled = hasProject;

            // These float over sc1 but are parented directly to the form, so hiding sc1 alone
            // doesn't hide them - only ever force them OFF here, never force them on, so each
            // panel's own show/hide toggle still governs whether it reappears once unlocked.
            if (pnlProperties is not null && !hasProject)
                pnlProperties.Visible = false;
            if (pnlStructureTree is not null && !hasProject)
                pnlStructureTree.Visible = false;
            if (pnlValidation is not null && !hasProject)
                pnlValidation.Visible = false;
        }

        // Splits one line of tooltip body text into (text, isBold) runs around whole-word,
        // case-insensitive matches of 'term' - shared by MeasureBodyText and DrawBodyText so
        // the reserved tooltip size and the actual rendering can never disagree about where the
        // bold runs fall (the same class of bug fixed above for the title/body line-height gap).
        private List<(string Text, bool Bold)> SplitHighlightSegments(string line, string term)
        {
            var segments = new List<(string Text, bool Bold)>();
            if (string.IsNullOrEmpty(term))
            {
                segments.Add((line, false));
                return segments;
            }

            int lastEnd = 0;
            foreach (Match m in Regex.Matches(line, @"\b" + Regex.Escape(term) + @"\b", RegexOptions.IgnoreCase))
            {
                if (m.Index > lastEnd)
                    segments.Add((line.Substring(lastEnd, m.Index - lastEnd), false));
                segments.Add((m.Value, true));
                lastEnd = m.Index + m.Length;
            }
            if (lastEnd < line.Length)
                segments.Add((line.Substring(lastEnd), false));
            if (segments.Count == 0)
                segments.Add((line, false));
            return segments;
        }

        // GenericTypographic disables the small per-call padding Graphics.MeasureString/DrawString
        // otherwise add. Used only for the one-off calibration measurements below, NOT for
        // positioning individual segments - see the comment on GetMonospaceCharWidth for why.
        private readonly StringFormat ToolTipBodyStringFormat = StringFormat.GenericTypographic;

        // Body text is always drawn in Consolas, a monospace font whose regular and bold weights
        // share the same per-character advance width (a deliberate design goal of coding fonts, so
        // bold syntax highlighting doesn't shift column alignment). That makes it possible - and far
        // more reliable - to position each segment purely from its character offset times this one
        // calibrated width, instead of summing each segment's own MeasureString width: GDI+ measures
        // trailing whitespace and font-weight metrics inconsistently enough between fragments that
        // summing them drifted out of sync with how a single DrawString call would have laid out the
        // same text, which is what was breaking the spacing on the line containing the bolded term.
        private float GetMonospaceCharWidth(Graphics g, Font fontRegular)
        {
            const string sample = "0123456789012345678901234567890123456789";
            return g.MeasureString(sample, fontRegular, int.MaxValue, ToolTipBodyStringFormat).Width / sample.Length;
        }

        private SizeF MeasureBodyText(Graphics g, string text, Font fontRegular)
        {
            string[] lines = text.Split(new string[] { Constants.vbCrLf }, StringSplitOptions.None);
            int maxChars = 0;
            foreach (var line in lines)
                maxChars = Math.Max(maxChars, line.Length);
            float charWidth = GetMonospaceCharWidth(g, fontRegular);
            float lineHeight = g.MeasureString("M", fontRegular, int.MaxValue, ToolTipBodyStringFormat).Height;
            return new SizeF(maxChars * charWidth, lineHeight * lines.Length);
        }

        private void DrawBodyText(Graphics g, string text, Font fontRegular, Font fontBold, Brush brushRegular, Brush brushHighlight, string term, float x, float y)
        {
            float charWidth = GetMonospaceCharWidth(g, fontRegular);
            float lineHeight = g.MeasureString("M", fontRegular, int.MaxValue, ToolTipBodyStringFormat).Height;
            float curY = y;
            foreach (var line in text.Split(new string[] { Constants.vbCrLf }, StringSplitOptions.None))
            {
                int curChar = 0;
                foreach (var seg in SplitHighlightSegments(line, term))
                {
                    var font = seg.Bold ? fontBold : fontRegular;
                    var brush = seg.Bold ? brushHighlight : brushRegular;
                    g.DrawString(seg.Text, font, brush, new PointF(x + curChar * charWidth, curY), ToolTipBodyStringFormat);
                    curChar += seg.Text.Length;
                }
                curY += lineHeight;
            }
        }

        private void ToolTip_Popup(object sender, PopupEventArgs e)
        {
            var fontTitle = new Font("Consolas", 11f, FontStyle.Bold);
            var fontBody = new Font("Consolas", 10f, FontStyle.Regular);
            System.Windows.Forms.ToolTip tt = (System.Windows.Forms.ToolTip)sender;

            Size totalSize;
            // Measure with GDI+ (Graphics.MeasureString), matching the GDI+ (Graphics.DrawString) calls
            // used in ToolTip_Draw. Mixing GDI's TextRenderer.MeasureText with GDI+ drawing overestimates
            // line height, and that overestimate compounds across many lines - producing a large blank
            // band at the bottom of long tooltips (e.g. the SECTION keyword's help text).
            using (var g = txt3.CreateGraphics())
            {
                var bodySize = MeasureBodyText(g, currentToolTipText, fontBody);
                if (!string.IsNullOrEmpty(tt.ToolTipTitle))
                {
                    var titleSize = g.MeasureString(tt.ToolTipTitle, fontTitle);
                    totalSize = new Size((int)Math.Round(Math.Ceiling((double)Math.Max(titleSize.Width, bodySize.Width))) + 16, (int)Math.Round(Math.Ceiling((double)titleSize.Height)) + (int)Math.Round(Math.Ceiling((double)bodySize.Height)) + 20);
                }
                else
                {
                    totalSize = new Size((int)Math.Round(Math.Ceiling((double)bodySize.Width)) + 16, (int)Math.Round(Math.Ceiling((double)bodySize.Height)) + 16);
                }
            }

            e.ToolTipSize = totalSize;
        }

        private void ToolTip_Draw(object sender, DrawToolTipEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var bounds = e.Bounds;

            // Draw background (white card background)
            using (var bgBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(bgBrush, bounds);
            }

            // Draw border (thin dark border)
            using (var borderPen = new Pen(Color.FromArgb(60, 60, 60), 1f))
            {
                g.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            }

            System.Windows.Forms.ToolTip tt = (System.Windows.Forms.ToolTip)sender;
            var fontTitle = new Font("Consolas", 11f, FontStyle.Bold);
            var fontBody = new Font("Consolas", 10f, FontStyle.Regular);
            var fontBodyBold = new Font("Consolas", 10f, FontStyle.Bold);

            int textY = bounds.Y + 8;
            if (!string.IsNullOrEmpty(tt.ToolTipTitle))
            {
                g.DrawString(tt.ToolTipTitle, fontTitle, Brushes.Navy, bounds.X + 8, textY);
                // Advance by the actual measured title height (matching ToolTip_Popup's sizing) rather
                // than a fixed guess, so the separator line and body land where Popup expected them to.
                textY += (int)Math.Round(Math.Ceiling((double)g.MeasureString(tt.ToolTipTitle, fontTitle).Height));
                // Draw thin line separating title and body
                using (var sepPen = new Pen(Color.FromArgb(220, 220, 220), 1f))
                {
                    g.DrawLine(sepPen, bounds.X + 8, textY, bounds.Right - 8, textY);
                }
                textY += 6;
            }

            // Bold AND color every whole-word occurrence of the hovered term within the body text -
            // bold alone was too subtle against plain black body text to stand out at a glance.
            DrawBodyText(g, e.ToolTipText, fontBody, fontBodyBold, Brushes.Black, ToolTipHighlightBrush, currentHoveredWord, bounds.X + 8, textY);
        }

        private void btnAutosave_Click(object sender, EventArgs e)
        {
            My.MySettingsProperty.Settings.autoSave = !My.MySettingsProperty.Settings.autoSave;
            My.MySettingsProperty.Settings.Save();

            btnAutosave.Text = My.MySettingsProperty.Settings.autoSave ? "Autosave: On" : "Autosave: Off";
            btnAutosave.BackColor = My.MySettingsProperty.Settings.autoSave ? Color.FromArgb(192, 255, 192) : Color.FromArgb(255, 192, 192);

            btnSave.Visible = !My.MySettingsProperty.Settings.autoSave;

            if (My.MySettingsProperty.Settings.autoSave)
            {
                ForceSaveActiveFile(true);
                if (!string.IsNullOrEmpty(projectName))
                {
                    isDirty = false;
                    UpdateDirtyWarning();
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            ForceSaveActiveFile(false);
            if (!string.IsNullOrEmpty(projectName))
            {
                isDirty = false;
                UpdateDirtyWarning();
            }
        }

        private void ForceSaveActiveFile(bool isAutoSave = false)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                if (!isAutoSave)
                {
                    AppMessageBox.Show("Please enter a project name in the text box at the top before saving.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtName.Focus();
                }
                return;
            }
            switch (tc1.SelectedTab.Name ?? "")
            {
                case "Geometry":
                    {
                        SaveAVL();
                        break;
                    }
                case "Mass":
                    {
                        SaveMass();
                        break;
                    }
                case "Run":
                    {
                        SaveRun();
                        break;
                    }
            }
        }

        private void UpdateDirtyWarning()
        {
            if (lblDirtyWarning is not null)
            {
                lblDirtyWarning.Visible = !My.MySettingsProperty.Settings.autoSave && isDirty;
            }
        }

        private void frmGeometry_FormClosing(object sender, FormClosingEventArgs e)
        {
            _renderTimer.Stop();
            if (_cts is not null)
                _cts.Cancel();
            if (!My.MySettingsProperty.Settings.autoSave && isDirty)
            {
                var res = AppMessageBox.Show("You have unsaved changes. Would you like to save them before closing?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes)
                {
                    ForceSaveActiveFile();
                }
                else if (res == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        public void ApplySyntaxHighlighting()
        {
            if (txt3 is null)
                return;

            // Clear previous highlighting
            {
                var withBlock = txt3.Range;
                withBlock.ClearStyle();
                withBlock.ClearFoldingMarkers();

                // 1. AVL (GEOMETRY) KEYWORDS
                string avlRegex = @"(?<![!#].*)(?i)\b(" + "Mach|IYsym|IZsym|Zsym|Sref|Cref|Bref|Xref|Yref|Zref|" + "Nchordwise|Cspace|Nspanwise|Sspace|" + "Xle|Yle|Zle|Chord|Ainc|ANGLE|YDUPLICATE|SCALE|TRANSLATE|" + "Cname|Cgain|Xhinge|HingeVec|SgnDup" + @")\b";

                // 2. MASS FILE KEYWORDS
                string massRegex = @"(?<![!#].*)(?i)\b(" + "mass|Lunit|Munit|Tunit|g|rho|" + "x|y|z|X_cg|Y_cg|Z_cg|" + "Ixx|Iyy|Izz|Ixy|Iyz|Izx" + @")\b";

                // 3. RUN CASE KEYWORDS (Standard)
                string runStandardRegex = @"(?<![!#].*)(?i)\b(" + "alpha|beta|pb/2V|qc/2V|rb/2V|" + "aileron|flap|elevator|rudder|" + "CL|CDo|visc|" + "bank|elevation|heading|velocity|density|" + "CL_a|CL_u|CM_a|CM_u|" + "Cl|roll|mom|Cm|pitch|Cn|yaw|" + "deg|m/s|m/s^2|kg/m^3|kg-m^2|kg|m" + @")\b";

                // 4. RUN CASE KEYWORDS (Special Dot-Enders)
                string runDotRegex = @"(?<![!#].*)(?i)\b(" + @"grav\.acc\.|turn_rad\.|load_fac\." + @")(?=\s|$)";

                // Apply Styles
                withBlock.SetStyle(blueStyle, avlRegex, RegexOptions.ExplicitCapture);
                withBlock.SetStyle(blueStyle, massRegex, RegexOptions.ExplicitCapture);
                withBlock.SetStyle(blueStyle, runStandardRegex, RegexOptions.ExplicitCapture);
                withBlock.SetStyle(blueStyle, runDotRegex, RegexOptions.ExplicitCapture);

                // Apply Comment Styles
                withBlock.SetStyle(greenStyle, @"(?i:\bsurface\b|\bsection\b|\bcontrol\b)", RegexOptions.ExplicitCapture);
                withBlock.SetStyle(lightgreenStyle, "(?i:#.*)");
                withBlock.SetStyle(lightgreenStyle, "!.*$", RegexOptions.Multiline);

                // Folding and Blocks
                withBlock.SetStyle(ellipseStyle1, "(?i:!beginsurface|!endsurface)");
                withBlock.SetStyle(ellipseStyle2, "(?i:!beginsection|!endsection)");
                withBlock.SetStyle(ellipseStyle3, "(?i:!begincontrol|!endcontrol)");
                withBlock.SetStyle(ellipseStyle4, "(?i:!begingeometry|!endgeometry)");
                withBlock.SetStyle(redStyle, @"\[[^\]]*\]");

                withBlock.SetFoldingMarkers("{", "}");
                withBlock.SetFoldingMarkers(@"!beginsurface\b", @"!endsurface\b", RegexOptions.IgnoreCase);
                withBlock.SetFoldingMarkers(@"!beginsection\b", @"!endsection\b", RegexOptions.IgnoreCase);
                withBlock.SetFoldingMarkers(@"!begincontrol\b", @"!endcontrol\b", RegexOptions.IgnoreCase);
                withBlock.SetFoldingMarkers(@"!begingeometry\b", @"!endgeometry\b", RegexOptions.IgnoreCase);
            }
            txt3.AdjustFolding();
        }

        private string GetLeadingWhitespace(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            string ws = "";
            foreach (var c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    ws += Conversions.ToString(c);
                }
                else
                {
                    break;
                }
            }
            return ws;
        }

        private string FormatLineText(string lineText)
        {
            if (string.IsNullOrEmpty(lineText))
                return "";
            int spacelen = autoSpace ? autoSpaceWidth : 2;
            bool foundexclam = false;
            string[] pars = lineText.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string str = "";
            for (int j = 0, loopTo = pars.Count() - 1; j <= loopTo; j++)
            {
                if (pars[j].Contains("!") | pars[j].Contains("[") | lineText.StartsWith("Run Case", StringComparison.CurrentCultureIgnoreCase))
                {
                    foundexclam = true;
                }
                if (j != pars.Count() - 1)
                {
                    if (foundexclam == false)
                    {
                        if (pars[j].Contains("hingevec", StringComparison.CurrentCultureIgnoreCase))
                        {
                            str += string.Format("{0,-" + (spacelen * 3).ToString() + "}", pars[j].Replace(" ", "")) + " ";
                        }
                        else if (!pars[j].Contains("visc", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (j < pars.Count() - 2)
                            {
                                if (!(pars[j + 1].Contains("roll", StringComparison.CurrentCultureIgnoreCase) | pars[j + 1].Contains("pitch", StringComparison.CurrentCultureIgnoreCase) | pars[j + 1].Contains("yaw", StringComparison.CurrentCultureIgnoreCase) | pars[j + 1].Contains("mom", StringComparison.CurrentCultureIgnoreCase)))
                                {
                                    str += string.Format("{0,-" + spacelen.ToString() + "}", pars[j].Replace(" ", "")) + " ";
                                }
                                else
                                {
                                    str += pars[j].Replace(" ", "") + " ";
                                }
                            }
                            else
                            {
                                str += string.Format("{0,-" + spacelen.ToString() + "}", pars[j].Replace(" ", "")) + " ";
                            }
                        }
                        else
                        {
                            str += pars[j].Replace(" ", "") + " ";
                        }
                    }
                    else
                    {
                        str += pars[j].Replace(" ", "") + " ";
                    }
                }
                else
                {
                    str += pars[j].Replace(" ", "");
                }
            }
            return str;
        }

        /// <summary>
    /// Splits a run-case constraint line ("alpha  ->  CL  =  0.40000") into its 3 fields.
    /// Returns False (leaving the ByRef args untouched) if the line doesn't match that shape.
    /// </summary>
        private bool TrySplitArrowLine(string line, ref string leftPart, ref string rightPart, ref string rest)
        {
            int arrowIdx = line.IndexOf("->");
            if (arrowIdx < 0)
                return false;
            string afterArrow = line.Substring(arrowIdx + 2);
            int eqIdx = afterArrow.IndexOf('=');
            if (eqIdx < 0)
                return false;
            string l = line.Substring(0, arrowIdx).Trim();
            string r = afterArrow.Substring(0, eqIdx).Trim();
            if (l.Length == 0 || r.Length == 0)
                return false;
            leftPart = l;
            rightPart = r;
            rest = afterArrow.Substring(eqIdx + 1).Trim();
            return true;
        }

        /// <summary>Splits a plain "name = value [unit]" run-file parameter line into its 2 fields.</summary>
        private bool TrySplitAssignLine(string line, ref string name, ref string rest)
        {
            int eqIdx = line.IndexOf('=');
            if (eqIdx < 0)
                return false;
            string n = line.Substring(0, eqIdx).Trim();
            if (n.Length == 0)
                return false;
            name = n;
            rest = line.Substring(eqIdx + 1).Trim();
            return true;
        }

        /// <summary>Splits off the first whitespace-delimited token, returning the rest (trimmed) via ByRef.</summary>
        private string FirstToken(string s, ref string remainder)
        {
            string[] toks = s.Split(new char[] { ' ', ControlChars.Tab }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (toks.Length == 0)
            {
                remainder = "";
                return "";
            }
            else if (toks.Length == 1)
            {
                remainder = "";
                return toks[0];
            }
            else
            {
                remainder = toks[1].Trim();
                return toks[0];
            }
        }

        /// <summary>
    /// Table-column formatter for .run files, replacing the generic per-token/per-line padder used
    /// for .avl/.mass (see FormatActiveText below) - that one pads each word to a fixed width in
    /// isolation, which works for .avl/.mass because each field there really is exactly one token
    /// (Xle, Yle, Chord, ...). Run-case target names can be multiple words ("Cl roll mom"), so a
    /// fixed-per-token width makes the "=" and value column land at a different place on every row
    /// depending on how many words came before it - the actual reported "crooked" look. This instead
    /// measures the widest left-name/right-name/value across the WHOLE file first, then pads every
    /// row to those same widths, so the "->", "=", and value columns line up like a real table.
    /// </summary>
        private string FormatRunFileText(string text)
        {
            string[] rawLines = text.Replace(Constants.vbCr, "").Split(Constants.vbLf);

            int maxLeft = 0;
            int maxRight = 0;
            int maxArrowValue = 0;
            int maxPlainName = 0;
            int maxPlainValue = 0;
            string leftP = "";
            string rightP = "";
            string restP = "";
            string nameP = "";
            string remainderP = "";

            foreach (var raw in rawLines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || Regex.IsMatch(line, "^-+$"))
                    continue;
                if (Regex.IsMatch(line, @"(?i)^run\s*case\b"))
                    continue;

                if (TrySplitArrowLine(line, ref leftP, ref rightP, ref restP))
                {
                    string valueTok = FirstToken(restP, ref remainderP);
                    maxLeft = Math.Max(maxLeft, leftP.Length);
                    maxRight = Math.Max(maxRight, rightP.Length);
                    maxArrowValue = Math.Max(maxArrowValue, valueTok.Length);
                }
                else if (TrySplitAssignLine(line, ref nameP, ref restP))
                {
                    string valueTok = FirstToken(restP, ref remainderP);
                    maxPlainName = Math.Max(maxPlainName, nameP.Length);
                    maxPlainValue = Math.Max(maxPlainValue, valueTok.Length);
                }
            }

            int gapSmall = autoSpace ? Math.Max(1, autoSpaceWidth / 6) : 1;
            int gapMed = autoSpace ? Math.Max(1, autoSpaceWidth / 4) : 1;

            var outLines = new List<string>();
            foreach (var raw in rawLines)
            {
                string line = raw.Trim();
                if (line.Length == 0)
                {
                    outLines.Add("");
                }
                else if (Regex.IsMatch(line, "^-+$"))
                {
                    outLines.Add(line);
                }
                else if (Regex.IsMatch(line, @"(?i)^run\s*case\b"))
                {
                    outLines.Add(" " + Regex.Replace(line, @"(?i)^run\s*case", "Run case"));
                }
                else if (TrySplitArrowLine(line, ref leftP, ref rightP, ref restP))
                {
                    string valueTok = FirstToken(restP, ref remainderP);
                    var sb = new StringBuilder();
                    sb.Append(' ').Append(leftP.PadRight(maxLeft)).Append(new string(' ', gapSmall)).Append("->").Append(new string(' ', gapSmall));
                    sb.Append(rightP.PadRight(maxRight)).Append(new string(' ', gapMed)).Append("=").Append(new string(' ', gapMed));
                    sb.Append(valueTok.PadRight(maxArrowValue));
                    if (remainderP.Length > 0)
                        sb.Append(new string(' ', gapMed)).Append(remainderP);
                    outLines.Add(sb.ToString().TrimEnd());
                }
                else if (TrySplitAssignLine(line, ref nameP, ref restP))
                {
                    string valueTok = FirstToken(restP, ref remainderP);
                    var sb = new StringBuilder();
                    sb.Append(' ').Append(nameP.PadRight(maxPlainName)).Append(new string(' ', gapMed)).Append("=").Append(new string(' ', gapMed));
                    sb.Append(valueTok.PadRight(maxPlainValue));
                    if (remainderP.Length > 0)
                        sb.Append(new string(' ', gapMed)).Append(remainderP);
                    outLines.Add(sb.ToString().TrimEnd());
                }
                else
                {
                    outLines.Add(" " + line);
                }
            }

            return string.Join(Environment.NewLine, outLines);
        }

        public void FormatActiveText()
        {
            if (updating == true || txt3 is null || txt3.LinesCount == 0)
                return;

            int seli = txt3.SelectionStart;
            int vsv = txt3.VerticalScroll.Value;
            int hsv = txt3.HorizontalScroll.Value;
            int spacelen = autoSpace ? autoSpaceWidth : 2;

            updating = true;
            string text = "";
            if (tc1.SelectedTab is not null && tc1.SelectedTab.Name == "Run")
            {
                text = FormatRunFileText(txt3.Text);
            }
            else
            {
                for (int i = 0, loopTo = txt3.LinesCount - 1; i <= loopTo; i++)
                {
                    string leadingWS = GetLeadingWhitespace(txt3.Lines[i]);
                    // A "#"/"!" comment line is either a short column-header label sitting directly above
                    // a data row (e.g. "#Xle Yle Zle Chord Ainc") - which reads best padded to the SAME
                    // per-token columns as that data row - or a prose explanation sentence, which turns
                    // into an unreadable picket fence if padded the same way. Sentence punctuation (a
                    // period, comma, parenthesis, or a " - " aside) is what actually distinguishes the two;
                    // a bare list of field names never has any of it.
                    string trimmedLine = txt3.Lines[i].TrimStart();
                    bool isCommentLine = trimmedLine.StartsWith("#") || trimmedLine.StartsWith("!");
                    bool looksLikeProse = isCommentLine && (trimmedLine.Contains(".") || trimmedLine.Contains(",") || trimmedLine.Contains("(") || trimmedLine.Contains(")") || trimmedLine.Contains(" - "));
                    bool foundexclam = looksLikeProse;
                    string[] pars = txt3.Lines[i].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    string str = "";
                    for (int j = 0, loopTo1 = pars.Count() - 1; j <= loopTo1; j++)
                    {
                        if (pars[j].Contains("!") | pars[j].Contains("[") | txt3.Lines[i].StartsWith("Run Case", StringComparison.CurrentCultureIgnoreCase))
                        {
                            foundexclam = true;
                        }
                        if (j != pars.Count() - 1)
                        {
                            if (foundexclam == false)
                            {
                                if (pars[j].Contains("hingevec", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    str += string.Format("{0,-" + (spacelen * 3).ToString() + "}", pars[j].Replace(" ", "")) + " ";
                                }
                                else if (!pars[j].Contains("visc", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    if (j < pars.Count() - 2)
                                    {
                                        if (!(pars[j + 1].Contains("roll", StringComparison.CurrentCultureIgnoreCase) | pars[j + 1].Contains("pitch", StringComparison.CurrentCultureIgnoreCase) | pars[j + 1].Contains("yaw", StringComparison.CurrentCultureIgnoreCase) | pars[j + 1].Contains("mom", StringComparison.CurrentCultureIgnoreCase)))
                                        {
                                            str += string.Format("{0,-" + spacelen.ToString() + "}", pars[j].Replace(" ", "")) + " ";
                                        }
                                        else
                                        {
                                            str += pars[j].Replace(" ", "") + " ";
                                        }
                                    }
                                    else
                                    {
                                        str += string.Format("{0,-" + spacelen.ToString() + "}", pars[j].Replace(" ", "")) + " ";
                                    }
                                }
                                else
                                {
                                    str += pars[j].Replace(" ", "") + " ";
                                }
                            }
                            else
                            {
                                str += pars[j].Replace(" ", "") + " ";
                            }
                        }
                        else
                        {
                            str += pars[j].Replace(" ", "");
                        }
                    }
                    text += leadingWS + str + Environment.NewLine;
                }

                // Always strip the trailing newline the loop appends after the last line.
                // Without this, a document that already ends with \r\n would accumulate
                // an extra blank line on every prettify call.
                if (text.EndsWith(Environment.NewLine))
                {
                    text = text.Substring(0, text.Length - Environment.NewLine.Length);
                }
            }

            if ((txt3.Text ?? "") != (text ?? ""))
            {
                var savedCaret = txt3.Selection.Start;

                // Replace all text via selection to preserve undo history
                txt3.Selection.Start = new Place(0, 0);
                txt3.Selection.End = new Place(txt3.Lines[txt3.LinesCount - 1].Length, txt3.LinesCount - 1);

                bool savedAutoIndent = txt3.AutoIndent;
                txt3.AutoIndent = false;
                txt3.InsertText(text);
                txt3.AutoIndent = savedAutoIndent;

                // Recalculate block-level auto-indentation on the selection
                txt3.Selection.Start = new Place(0, 0);
                txt3.Selection.End = new Place(txt3.Lines[txt3.LinesCount - 1].Length, txt3.LinesCount - 1);
                txt3.DoAutoIndent();

                // Clamp caret position safely
                var newCaret = savedCaret;
                if (newCaret.iLine >= txt3.LinesCount)
                {
                    newCaret.iLine = txt3.LinesCount - 1;
                }
                if (newCaret.iLine >= 0)
                {
                    if (newCaret.iChar > txt3.Lines[newCaret.iLine].Length)
                    {
                        newCaret.iChar = txt3.Lines[newCaret.iLine].Length;
                    }
                }
                else
                {
                    newCaret = new Place(0, 0);
                }
                txt3.Selection.Start = newCaret;

                txt3.VerticalScroll.Value = Math.Min(vsv, txt3.VerticalScroll.Maximum);
                txt3.HorizontalScroll.Value = Math.Min(hsv, txt3.HorizontalScroll.Maximum);
            }

            ApplySyntaxHighlighting();
            updating = false;
        }

        private void ReplaceEditorLine(int lineIndex, string newText)
        {
            if (txt3 is null || lineIndex < 0 || lineIndex >= txt3.LinesCount)
                return;
            var savedCaret = txt3.Selection.Start;
            int vsv = txt3.VerticalScroll.Value;
            int hsv = txt3.HorizontalScroll.Value;

            txt3.Selection.Start = new Place(0, lineIndex);
            txt3.Selection.End = new Place(txt3.Lines[lineIndex].Length, lineIndex);

            bool savedAutoIndent = txt3.AutoIndent;
            txt3.AutoIndent = false;
            txt3.InsertText(newText);
            txt3.AutoIndent = savedAutoIndent;

            // Recalculate block-level auto-indentation on the line selection
            txt3.Selection.Start = new Place(0, lineIndex);
            txt3.Selection.End = new Place(txt3.Lines[lineIndex].Length, lineIndex);
            txt3.DoAutoIndent();

            txt3.Selection.Start = savedCaret;
            txt3.VerticalScroll.Value = vsv;
            txt3.HorizontalScroll.Value = hsv;
        }

        private void btnHelp_Click_1(object sender, EventArgs e)
        {

        }

        private void btnHelp_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

            switch (e.ClickedItem.Text ?? "")
            {
                case "Full Help Document":
                    {
                        // Process.Start(rootPath + "\avl_doc.txt")
                        My.MyProject.Forms.frmHelp.Show();
                        My.MyProject.Forms.frmHelp.txt1.Text = readLines(help, 1, 2388);
                        break;
                    }
                case "Geometry File (.avl)":
                    {
                        My.MyProject.Forms.frmHelp.Show();
                        My.MyProject.Forms.frmHelp.txt1.Text = readLines(help, 215, 1124);
                        break;
                    }
                case "Mass File (.mass)":
                    {
                        My.MyProject.Forms.frmHelp.Show();
                        My.MyProject.Forms.frmHelp.txt1.Text = readLines(help, 1126, 1300);
                        break;
                    }
                case "Run File (.run)":
                    {
                        My.MyProject.Forms.frmHelp.Show();
                        My.MyProject.Forms.frmHelp.txt1.Text = readLines(help, 1302, 1310) + Environment.NewLine + readLines(help, 1993, 2101);
                        break;
                    }
                case "Program Commands":
                    {
                        My.MyProject.Forms.frmHelp.Show();
                        My.MyProject.Forms.frmHelp.txt1.Text = readLines(help, 1312, 2388);
                        break;
                    }

            }
        }

        private void btnHelpMass_Click(object sender, EventArgs e)
        {

        }

        private void btnSpace_Click(object sender, EventArgs e)
        {
            if (btnSpace.Text.Contains("On"))
            {
                autoSpace = false;
                btnSpace.Text = "Auto Space: Off";
                txt3_TextChangedDelayed(sender, new FastColoredTextBoxNS.TextChangedEventArgs(txt3.Range));
            }
            else
            {
                autoSpace = true;
                btnSpace.Text = "Auto Space: On";
                txt3_TextChangedDelayed(sender, new FastColoredTextBoxNS.TextChangedEventArgs(txt3.Range));
            }
            SaveAutoSpaceSettings();
        }

        private void btnHelpAVL_Click(object sender, EventArgs e)
        {

        }

        private void btnHelpFull_Click(object sender, EventArgs e)
        {

        }

        private void txtName_Click_1(object sender, EventArgs e)
        {

        }

        // Builds a small solid-color square used as the check-swatch icon for each
        // "Display" dropdown item, so each layer's on-screen color is visible right
        // in the menu without needing designer-time image resources.
        private System.Drawing.Image MakeLayerSwatch(Color c)
        {
            var bmp = new Bitmap(12, 12);
            using (var g = Graphics.FromImage(bmp))
            {
                using (var b = new SolidBrush(c))
                {
                    g.FillRectangle(b, 0, 0, 12, 12);
                }
                g.DrawRectangle(Pens.Black, 0, 0, 11, 11);
            }
            return bmp;
        }

        private void InitLayerMenuSwatches()
        {
            mnuLayerSection.Image = MakeLayerSwatch(Color.Red);
            mnuLayerMass.Image = MakeLayerSwatch(Color.Blue);
            mnuLayerControl.Image = MakeLayerSwatch(Color.Yellow);
            mnuLayerMesh.Image = MakeLayerSwatch(Color.FromArgb(140, ThemeMeshColor));
            mnuLayerChordline.Image = MakeLayerSwatch(Color.DarkOrange);
            mnuLayerControlPoints.Image = MakeLayerSwatch(Color.Magenta);
            mnuLayerAxes.Image = MakeLayerSwatch(Color.DodgerBlue);
            mnuLayerCamberline.Image = MakeLayerSwatch(Color.Purple);
            mnuLayerNormalVector.Image = MakeLayerSwatch(Color.Cyan);
            mnuLayerBoundLeg.Image = MakeLayerSwatch(Color.Firebrick);
            mnuLayerTrailingLegs.Image = MakeLayerSwatch(Color.SlateGray);
            mnuLayerLoading.Image = MakeLayerSwatch(Color.SpringGreen);
            mnuLayerOffBody.Image = MakeLayerSwatch(Color.Gray);
        }

        private void mnuLayerMass_CheckedChanged(object sender, EventArgs e)
        {
            showMass = mnuLayerMass.Checked;
            drawAxes();
        }

        private void mnuLayerControl_CheckedChanged(object sender, EventArgs e)
        {
            showControl = mnuLayerControl.Checked;
            drawAxes();
        }

        private void mnuLayerSection_CheckedChanged(object sender, EventArgs e)
        {
            showSection = mnuLayerSection.Checked;
            drawAxes();
        }

        private void mnuLayerMesh_CheckedChanged(object sender, EventArgs e)
        {
            showMesh = mnuLayerMesh.Checked;
            drawAxes();
        }

        private void mnuLayerChordline_CheckedChanged(object sender, EventArgs e)
        {
            showChordline = mnuLayerChordline.Checked;
            drawAxes();
        }

        private void mnuLayerControlPoints_CheckedChanged(object sender, EventArgs e)
        {
            showControlPoints = mnuLayerControlPoints.Checked;
            drawAxes();
        }

        private void mnuLayerAxes_CheckedChanged(object sender, EventArgs e)
        {
            showAxesTriad = mnuLayerAxes.Checked;
            drawAxes();
        }

        private void mnuLayerCamberline_CheckedChanged(object sender, EventArgs e)
        {
            showCamberline = mnuLayerCamberline.Checked;
            drawAxes();
        }

        private void mnuLayerNormalVector_CheckedChanged(object sender, EventArgs e)
        {
            showNormalVector = mnuLayerNormalVector.Checked;
            drawAxes();
        }

        private void mnuLayerBoundLeg_CheckedChanged(object sender, EventArgs e)
        {
            showBoundLeg = mnuLayerBoundLeg.Checked;
            drawAxes();
        }

        private void mnuLayerTrailingLegs_CheckedChanged(object sender, EventArgs e)
        {
            showTrailingLegs = mnuLayerTrailingLegs.Checked;
            drawAxes();
        }

        private void mnuLayerLoading_CheckedChanged(object sender, EventArgs e)
        {
            showLoadingOverlay = mnuLayerLoading.Checked;
            drawAxes();
        }

        private void lblNote_Click(object sender, EventArgs e)
        {

        }

        private void btn3D_Click(object sender, EventArgs e)
        {
            if (btn3D.Text.Contains("On"))
            {
                show3D = false;
                btn3D.Text = "3D: Off";
                Debug.WriteLine("Show 3D is false");
                p3d.Visible = false;
                pxy.Visible = true;
            }
            else
            {
                show3D = true;
                btn3D.Text = "3D: On";
                Debug.WriteLine("Show 3D is true");
                p3d.Visible = true;
                p3d.Dock = DockStyle.Fill;
                pxy.Visible = false;
                Fit3D();
            }
            drawAxes();
        }

        private void btnFitAll_Click(object sender, EventArgs e)
        {
            // XY plane========================================================================
            int x0;
            int y0;
            int xcount;
            int ycount;
            var Xs = new List<double>();
            var Ys = new List<double>();
            int dx = 100;
            int dy = 100;
            xoffset = 0d;
            yoffset = 0d;

            int iter = 0;

            while (!(dx == 0 & dy == 0 | iter > 200))
            {

                iter += 1;

                gridstep = (int)Math.Round(pxy.Width / (double)gridnumber);
                x0 = (int)Math.Round(pxy.Width / 2d + xoffset);
                y0 = (int)Math.Round(pxy.Height / 2d + yoffset);

                // Draw grids 
                xcount = (int)Math.Round(pxy.Width / (double)gridstep);
                xmin = -xcount / 2d - xoffset / gridstep;
                xmax = xmin + xcount;

                gridstep = (int)Math.Round(pxy.Height / (double)gridnumber);
                ycount = (int)Math.Round(pxy.Height / (double)gridstep);
                ymin = -ycount / 2d + yoffset / gridstep;
                ymax = ymin + ycount;

                Xs = new List<double>();
                Ys = new List<double>();
                foreach (Node p in points)
                {
                    double xscale = p.Point.X * pxy.Width / (xmax - xmin) + pxy.Width / 2d + xoffset;
                    double yscale = -p.Point.Y * pxy.Height / (ymax - ymin) + pxy.Height / 2d + yoffset;
                    Xs.Add(xscale);
                    Ys.Add(yscale);
                }

                if (Xs.Count == 0)
                    return;
                dx = (int)Math.Round(pxy.Width / 2d - Xs.Average());
                dy = (int)Math.Round(pxy.Height / 2d - Ys.Average());

                xoffset += dx;
                yoffset += dy;

                Debug.WriteLine($"iter: {iter} | dx,dy = {dx},{dy} | xoffset,yoffset = {xoffset},{yoffset}");

                for (int i = 1; i <= 50; i++)
                {
                    gridnumber = i;
                    gridstep = (int)Math.Round(pxy.Width / (double)gridnumber);
                    x0 = (int)Math.Round(pxy.Width / 2d + xoffset);
                    y0 = (int)Math.Round(pxy.Height / 2d + yoffset);

                    // Draw grids 
                    xcount = (int)Math.Round(pxy.Width / (double)gridstep);
                    xmin = -xcount / 2d - xoffset / gridstep;
                    xmax = xmin + xcount;

                    gridstep = (int)Math.Round(pxy.Height / (double)gridnumber);
                    ycount = (int)Math.Round(pxy.Height / (double)gridstep);
                    ymin = -ycount / 2d + yoffset / gridstep;
                    ymax = ymin + ycount;

                    Xs = new List<double>();
                    Ys = new List<double>();
                    foreach (Node p in points)
                    {
                        double xscale = p.Point.X * pxy.Width / (xmax - xmin) + pxy.Width / 2d + xoffset;
                        double yscale = -p.Point.Y * pxy.Height / (ymax - ymin) + pxy.Height / 2d + yoffset;
                        Xs.Add(xscale);
                        Ys.Add(yscale);
                    }

                    if (Xs.Count == 0)
                    {
                        return;
                    }

                    int margin = Math.Min(30, (int)Math.Round(Math.Min(pxy.Width, pxy.Height) * 0.1d));
                    if (Xs.Max() <= pxy.Width - margin && Xs.Min() >= margin && Ys.Max() <= pxy.Height - margin && Ys.Min() >= margin)
                    {
                        break;
                    }

                }

                Debug.WriteLine($"Exited loop for FitAll at scale {gridnumber}");
                Debug.WriteLine($"{Xs.Max()} <= {pxy.Width} And {Xs.Min()} >= 0 And {Ys.Max()} <= {pxy.Height} And {Ys.Min()} >= 0");



            }

            drawAxes();


        }

        private void tc1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!My.MySettingsProperty.Settings.autoSave && isDirty)
            {
                var res = AppMessageBox.Show($"You have unsaved changes in the {lastActiveTabName} tab. Would you like to save them before switching?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes)
                {
                    switch (lastActiveTabName ?? "")
                    {
                        case "Geometry":
                            {
                                SaveAVL();
                                break;
                            }
                        case "Mass":
                            {
                                SaveMass();
                                break;
                            }
                        case "Run":
                            {
                                SaveRun();
                                break;
                            }
                    }
                    isDirty = false;
                    UpdateDirtyWarning();
                }
                else if (res == DialogResult.Cancel)
                {
                    tc1.SelectedIndexChanged -= tc1_SelectedIndexChanged;
                    tc1.SelectedTab = tc1.TabPages[lastActiveTabName];
                    tc1.SelectedIndexChanged += tc1_SelectedIndexChanged;
                    return;
                }
                else
                {
                    isDirty = false;
                    UpdateDirtyWarning();
                }
            }

            lastActiveTabName = tc1.SelectedTab.Name;

            // Every analysis tab (Trefftz/Loads/Polar/Derivatives/FE/Modes) is built
            // at Load time while it's NOT the selected tab, i.e. while WinForms
            // treats it as an invisible control - and an invisible control's docked
            // children (control-strip Dock=Top, plot PictureBox Dock=Fill) can end
            // up with stale/zero bounds because layout gets skipped for invisible
            // subtrees. This is the same bug the structure tree panel had. Force a
            // fresh layout of whichever tab just became visible so its control
            // strip and plot never end up overlapping.
            tc1.SelectedTab.PerformLayout();

            // txt3 (and its floating Add/Prettify/Undo/Redo/Clear/Tab+/Tab- action buttons,
            // which all operate on it) only belongs on the three text-editor tabs -
            // the analysis tabs (Trefftz, Loads, Polar, etc.) have their own
            // content and must not have the editor grafted onto them too.
            if (tc1.SelectedTab.Name == "Geometry" || tc1.SelectedTab.Name == "Mass" || tc1.SelectedTab.Name == "Run")
            {
                if (!tc1.SelectedTab.Controls.Contains(txt3))
                {
                    tc1.SelectedTab.Controls.Add(txt3);
                }

                foreach (System.Windows.Forms.Control editorBtn in new System.Windows.Forms.Control[] { btnAdd, btnPrettify, btnValidate, btnUndo, btnRedo, btnClear, btnTabIncrease, btnTabDecrease })
                {
                    if (!tc1.SelectedTab.Controls.Contains(editorBtn))
                    {
                        tc1.SelectedTab.Controls.Add(editorBtn);
                    }
                    editorBtn.BringToFront();
                }
            }

            switch (tc1.SelectedTab.Name ?? "")
            {
                case "Geometry":
                    {
                        // No fit-to-view needed here - this only runs for tab-switches within the
                        // same project (Geometry <-> Mass <-> Run), where the geometry hasn't
                        // changed. Switching projects goes through LoadActiveProject() instead,
                        // which does its own fit after the newly-loaded geometry settles.
                        LoadAVL();
                        break;
                    }
                case "Mass":
                    {
                        LoadMass();
                        break;
                    }
                case "Run":
                    {
                        LoadRun();
                        break;
                    }
            }

            UpdateGeometryTitle();

        }

        // Fires when the user picks an existing project from the txtName dropdown list (as opposed
        // to typing a name and pressing Enter/tabbing away, which goes through txtName_KeyDown/
        // txtName_Leave) - routed through the same LoadActiveProject() used by those so there's one
        // save-then-switch implementation for every way of changing the active project, not one that
        // can drift out of sync with another over time.
        private void txtName_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadActiveProject();
        }

        private void btnHover_Click(object sender, EventArgs e)
        {
            if (btnHover.Text.Contains("On"))
            {
                showHover = false;
                btnHover.Text = "Highlight Hover: Off";
            }
            else
            {
                showHover = true;
                btnHover.Text = "Highlight Hover: On";
            }
            drawAxes();
        }

        private void p3d_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left | e.Button == MouseButtons.Right | e.Button == MouseButtons.Middle)
            {
                lastMouseLoc = e.Location;
                p3d.Cursor = Cursors.SizeAll;
            }
        }

        private void p3d_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Rotate Alpha/Beta (Existing)
                float dx = e.X - lastMouseLoc.X;
                float dy = e.Y - lastMouseLoc.Y;
                viewAlpha += dx * 0.5f;
                viewBeta -= dy * 0.5f;
                lastMouseLoc = e.Location;
                drawAxes();
            }

            // Rotate around X
            else if (e.Button == MouseButtons.Right)
            {
                float dx = e.X - lastMouseLoc.X;
                viewBeta += dx * 0.5f;
                lastMouseLoc = e.Location;
                drawAxes();
            }

            // Rotate around Z (Roll) - viewGamma drives RotateZ in the render
            // chain; this comment previously (incorrectly) said "around Y".
            else if (e.Button == MouseButtons.Middle)
            {
                float dx = e.X - lastMouseLoc.X;
                viewGamma += dx * 0.5f;
                lastMouseLoc = e.Location;
                drawAxes();
            }
        }

        private void p3d_MouseUp(object sender, MouseEventArgs e)
        {
            p3d.Cursor = Cursors.Default;
        }

        private void p3d_MouseEnter(object sender, EventArgs e)
        {
            p3d.Focus();
        }

        // Sensible "fit to view" distance for the CURRENT geometry and viewport
        // size. Shared by Fit3D() and the scroll-wheel zoom, which used to clamp
        // to a fixed [2, 5] with no relationship to this value - Fit3D() alone
        // can easily compute 20-100+ for a normal-sized aircraft, so scrolling
        // used to snap the view to a wildly wrong, usually near-plane-clipped
        // scale the instant you touched the wheel. Extent is measured from the
        // model's own centroid (view3DCenterX/Y/Z), matching what HeavyRender
        // now actually rotates/displays around, not the world origin.
        private float ComputeFitViewDist()
        {
            var geoNodes = points.Where(n => n.@type == Node.NodeType.Geometry).ToList();
            if (geoNodes.Count == 0)
                return 30.0f;

            float cx = (geoNodes.Min(n => n.X) + geoNodes.Max(n => n.X)) / 2.0f;
            float cy = (geoNodes.Min(n => n.Y) + geoNodes.Max(n => n.Y)) / 2.0f;
            float cz = (geoNodes.Min(n => n.Z) + geoNodes.Max(n => n.Z)) / 2.0f;

            float maxExtent = 0f;
            foreach (Node p in geoNodes)
            {
                maxExtent = Math.Max(maxExtent, Math.Abs(p.X - cx));
                maxExtent = Math.Max(maxExtent, Math.Abs(p.Y - cy));
                maxExtent = Math.Max(maxExtent, Math.Abs(p.Z - cz));
            }

            // Multiply by 2 because we need to fit the whole diameter (-max to +max)
            float objectSize = maxExtent * 2.5f;
            float minScreenDim = Math.Min(p3d.Width, p3d.Height);
            if (minScreenDim <= 0f || objectSize <= 0f)
                return 30.0f;

            return objectSize * viewFOV / minScreenDim;
        }

        private void p3d_MouseWheel(object sender, MouseEventArgs e)
        {
            // Divide by 120 because e.Delta usually returns +/- 120 per "click"
            int scrollAmount = (int)Math.Round(e.Delta / 120d);

            // Scale both the zoom step and the clamp range to the model's own
            // fit-to-view distance instead of fixed absolute bounds, so zooming
            // feels proportionally similar regardless of aircraft size.
            float fitDist = ComputeFitViewDist();
            float minDist = Math.Max(2.0f, fitDist * 0.1f);
            float maxDist = fitDist * 5.0f;
            float zoomStep = Math.Max(0.5f, fitDist * 0.05f);

            viewDist -= scrollAmount * zoomStep;

            if (viewDist < minDist)
                viewDist = minDist;
            if (viewDist > maxDist)
                viewDist = maxDist;

            drawAxes();
        }

        private void Fit3D()
        {
            if (!points.Any(n => n.type == Node.NodeType.Geometry))
                return;

            viewDist = ComputeFitViewDist();
            if (viewDist < 2f)
                viewDist = 2f;

            // Reset Angles for a nice ISO view
            viewAlpha = 0f; // Yaw (Rotation around Y)
            viewBeta = 120f; // Pitch (Rotation around X)
            viewGamma = 0f; // Roll (Rotation around Z)
            drawAxes();
        }

        private void InitializeExportButtons()
        {
            AddExportButtonTo(pxy, "XY_Plane");
            AddExportButtonTo(pxz, "XZ_Plane");
            AddExportButtonTo(pyz, "YZ_Plane");
            AddExportButtonTo(p3d, "3D_View");
            AddViewPresetButtonTo(p3d);
            AddRotationHintTo(p3d);
            AddAngleNudgeControls(p3d);
        }

        // Builds the in-house Trefftz/loading-plot tab (spanwise loading, induced drag,
        // induced angle) so it gets PNG/SVG/PDF export via the same machinery as the
        // geometry views, instead of relying on AVL's native Trefftz graphics window.
        private void InitializeTrefftzTab()
        {
            if (Trefftz is not null)
                return;

            Trefftz = new TabPage("Trefftz");
            tc1.Controls.Add(Trefftz);

            // A single 2-row TableLayoutPanel (control strip, plot) instead of a
            // Dock=Top strip + Dock=Fill plot as SIBLINGS of the TabPage. This tab
            // is built while it's not the selected tab, and WinForms can leave a
            // still-invisible control's docked-sibling children with stale/zero
            // bounds - a table's row layout has no such ordering ambiguity.
            var outer = new TableLayoutPanel();
            outer.Dock = DockStyle.Fill;
            outer.ColumnCount = 1;
            outer.RowCount = 2;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 36.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
            Trefftz.Controls.Add(outer);

            var controlPanel = new System.Windows.Forms.Panel();
            controlPanel.Dock = DockStyle.Fill;
            controlPanel.BackColor = Color.WhiteSmoke;

            btnRunTrefftz.Text = "Run Trefftz Plot";
            btnRunTrefftz.Location = new Point(8, 6);
            btnRunTrefftz.Size = new Size(130, 25);
            btnRunTrefftz.FlatStyle = FlatStyle.Flat;
            btnRunTrefftz.BackColor = Color.White;
            btnRunTrefftz.Cursor = Cursors.Hand;
            btnRunTrefftz.Click += TrefftzPlaneToolStripMenuItem_Click;

            btnAvlCommandsTrefftz.Location = new Point(146, 6);
            btnAvlCommandsTrefftz.Size = new Size(120, 25);
            StyleAvlCommandsButton(btnAvlCommandsTrefftz, "Trefftz Plot - AVL Commands", "To reproduce this Trefftz/spanwise-loading plot directly in AVL:" + Constants.vbCrLf + Constants.vbCrLf + "1. load yourfile.avl" + Constants.vbCrLf + "2. oper" + Constants.vbCrLf + "3. x            (run the case)" + Constants.vbCrLf + "4. fs           (write Trefftz-plane strip forces)" + Constants.vbCrLf + "5. <Enter to print to screen, or a filename to save>" + Constants.vbCrLf + Constants.vbCrLf + "The 'fs' output lists spanwise loading, induced (downwash) angle, and induced drag per strip - the same data this tab plots.");

            controlPanel.Controls.Add(btnRunTrefftz);
            controlPanel.Controls.Add(btnAvlCommandsTrefftz);
            outer.Controls.Add(controlPanel, 0, 0);

            pTrefftz = new PictureBox();
            pTrefftz.BackColor = ThemeCanvasBackColor;
            pTrefftz.BorderStyle = BorderStyle.FixedSingle;
            pTrefftz.Dock = DockStyle.Fill;
            pTrefftz.Name = "pTrefftz";
            pTrefftz.TabStop = false;
            outer.Controls.Add(pTrefftz, 0, 1);

            AddExportButtonToPanel(controlPanel, pTrefftz, "Trefftz_Loading");
            pTrefftz.Resize += (s, ev) => RenderTrefftzPlot();

            RenderTrefftzPlot();
        }

        // Builds the spanwise shear/bending-moment tab, driven by AVL's "VM" command.
        private void InitializeLoadsTab()
        {
            if (Loads is not null)
                return;

            Loads = new TabPage("Loads");
            tc1.Controls.Add(Loads);

            var outer = new TableLayoutPanel();
            outer.Dock = DockStyle.Fill;
            outer.ColumnCount = 1;
            outer.RowCount = 2;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 36.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
            Loads.Controls.Add(outer);

            var controlPanel = new System.Windows.Forms.Panel();
            controlPanel.Dock = DockStyle.Fill;
            controlPanel.BackColor = Color.WhiteSmoke;

            btnLoads.Text = "Run Shear && Bending Moment";
            btnLoads.Location = new Point(8, 6);
            btnLoads.Size = new Size(190, 25);
            btnLoads.FlatStyle = FlatStyle.Flat;
            btnLoads.BackColor = Color.White;
            btnLoads.Cursor = Cursors.Hand;
            btnLoads.Click += RunLoadsAnalysis_Click;

            btnAvlCommandsLoads.Location = new Point(206, 6);
            btnAvlCommandsLoads.Size = new Size(120, 25);
            StyleAvlCommandsButton(btnAvlCommandsLoads, "Shear & Bending Moment - AVL Commands", "To reproduce this shear/bending-moment plot directly in AVL:" + Constants.vbCrLf + Constants.vbCrLf + "1. load yourfile.avl" + Constants.vbCrLf + "2. oper" + Constants.vbCrLf + "3. x            (run the case)" + Constants.vbCrLf + "4. vm           (write spanwise shear V and bending moment M)" + Constants.vbCrLf + "5. <Enter to print to screen, or a filename to save>");

            controlPanel.Controls.Add(btnLoads);
            controlPanel.Controls.Add(btnAvlCommandsLoads);
            outer.Controls.Add(controlPanel, 0, 0);

            pLoads = new PictureBox();
            pLoads.BackColor = ThemeCanvasBackColor;
            pLoads.BorderStyle = BorderStyle.FixedSingle;
            pLoads.Dock = DockStyle.Fill;
            pLoads.Name = "pLoads";
            pLoads.TabStop = false;
            outer.Controls.Add(pLoads, 0, 1);

            AddExportButtonToPanel(controlPanel, pLoads, "Spanwise_Loads");
            pLoads.Resize += (s, ev) => RenderLoadsPlot();

            RenderLoadsPlot();
        }

        // Builds the drag-polar tab: a small alpha-min/max/step control strip above
        // a 4-panel CL-alpha / CD-alpha / Cm-alpha / CL-CD plot, driven by a loop of
        // "a" -> "a <value>" -> "x" AVL commands (verified sequence - AVL's OPER-level
        // "a" enters a constraint-select submenu; "a <value>" inside that submenu
        // sets alpha and returns to OPER) reusing ParseTotalForces per point.
        private void InitializePolarTab()
        {
            if (Polar is not null)
                return;

            Polar = new TabPage("Polar");
            tc1.Controls.Add(Polar);

            var outer = new TableLayoutPanel();
            outer.Dock = DockStyle.Fill;
            outer.ColumnCount = 1;
            outer.RowCount = 2;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 36.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
            Polar.Controls.Add(outer);

            var controlPanel = new System.Windows.Forms.Panel();
            controlPanel.Dock = DockStyle.Fill;
            controlPanel.BackColor = Color.WhiteSmoke;

            // A nested TableLayoutPanel instead of hand-placed Locations: each control gets its
            // own dedicated auto-sized cell, so it's structurally impossible for one to overlap
            // the next regardless of the font/DPI it actually renders at. (A previous fix here
            // tried computing X offsets from TextRenderer.MeasureText, but that measured the
            // Label's pre-parented default font, not whatever font it actually inherits once
            // added to controlPanel/Polar/tc1/Me - so the gap was still wrong.)
            var fields = new TableLayoutPanel();
            fields.Location = new Point(8, 4);
            fields.AutoSize = true;
            fields.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            fields.ColumnCount = 8;
            fields.RowCount = 1;
            for (int i = 0; i <= 7; i++)
                fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblMin = new System.Windows.Forms.Label() { Text = "Alpha min:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 9, 4, 3) };

            txtPolarMin.Text = "-4";
            txtPolarMin.Width = 40;
            txtPolarMin.Anchor = AnchorStyles.Left;
            txtPolarMin.Margin = new Padding(0, 4, 16, 3);

            var lblMax = new System.Windows.Forms.Label() { Text = "max:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 9, 4, 3) };

            txtPolarMax.Text = "10";
            txtPolarMax.Width = 40;
            txtPolarMax.Anchor = AnchorStyles.Left;
            txtPolarMax.Margin = new Padding(0, 4, 16, 3);

            var lblStep = new System.Windows.Forms.Label() { Text = "step:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 9, 4, 3) };

            txtPolarStep.Text = "2";
            txtPolarStep.Width = 35;
            txtPolarStep.Anchor = AnchorStyles.Left;
            txtPolarStep.Margin = new Padding(0, 4, 24, 3);

            btnRunPolar.Text = "Run Polar Sweep";
            btnRunPolar.Size = new Size(130, 25);
            btnRunPolar.Anchor = AnchorStyles.Left;
            btnRunPolar.Margin = new Padding(0, 2, 3, 3);
            btnRunPolar.FlatStyle = FlatStyle.Flat;
            btnRunPolar.BackColor = Color.White;
            btnRunPolar.Cursor = Cursors.Hand;
            btnRunPolar.Click += RunPolarSweep_Click;

            btnAvlCommandsPolar.Size = new Size(120, 25);
            btnAvlCommandsPolar.Anchor = AnchorStyles.Left;
            btnAvlCommandsPolar.Margin = new Padding(8, 2, 3, 3);
            StyleAvlCommandsButton(btnAvlCommandsPolar, "Drag Polar Sweep - AVL Commands", "To reproduce this drag-polar sweep directly in AVL, repeat these steps for each alpha in your range:" + Constants.vbCrLf + Constants.vbCrLf + "1. load yourfile.avl" + Constants.vbCrLf + "2. oper" + Constants.vbCrLf + "3. a             (select the alpha constraint)" + Constants.vbCrLf + "4. a <value>     (e.g. 'a -4', then 'a -2', 'a 0' ... stepping up to your max)" + Constants.vbCrLf + "5. x             (run the case)" + Constants.vbCrLf + Constants.vbCrLf + "Repeat steps 3-5 for each alpha value, reading CL, CDtot, and Cm off the Total Forces output each time to build the CL-alpha / CD-alpha / Cm-alpha / CL-CD curves.");

            fields.Controls.Add(lblMin, 0, 0);
            fields.Controls.Add(txtPolarMin, 1, 0);
            fields.Controls.Add(lblMax, 2, 0);
            fields.Controls.Add(txtPolarMax, 3, 0);
            fields.Controls.Add(lblStep, 4, 0);
            fields.Controls.Add(txtPolarStep, 5, 0);
            fields.Controls.Add(btnRunPolar, 6, 0);
            fields.Controls.Add(btnAvlCommandsPolar, 7, 0);

            controlPanel.Controls.Add(fields);
            outer.Controls.Add(controlPanel, 0, 0);

            pPolar = new PictureBox();
            pPolar.BackColor = ThemeCanvasBackColor;
            pPolar.BorderStyle = BorderStyle.FixedSingle;
            pPolar.Dock = DockStyle.Fill;
            pPolar.Name = "pPolar";
            pPolar.TabStop = false;
            outer.Controls.Add(pPolar, 0, 1);

            AddExportButtonToPanel(controlPanel, pPolar, "Drag_Polar");
            pPolar.Resize += (s, ev) => RenderPolarPlot();

            RenderPolarPlot();
        }

        // Builds the stability-derivatives/forces tab: unlike the other tabs this is
        // tabular/key-value data (ST/SB/FN/FB/HM), not an XY curve, so it's shown as
        // AVL's own formatted monospace text rather than a custom chart.
        private void InitializeDerivativesTab()
        {
            if (Derivatives is not null)
                return;

            Derivatives = new TabPage("Derivatives");
            tc1.Controls.Add(Derivatives);

            var outer = new TableLayoutPanel();
            outer.Dock = DockStyle.Fill;
            outer.ColumnCount = 1;
            outer.RowCount = 3;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 36.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 118.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
            Derivatives.Controls.Add(outer);

            var controlPanel = new System.Windows.Forms.Panel();
            controlPanel.Dock = DockStyle.Fill;
            controlPanel.BackColor = Color.WhiteSmoke;

            btnRunDerivatives.Text = "Run Stability && Forces Analysis";
            btnRunDerivatives.Location = new Point(8, 6);
            btnRunDerivatives.Size = new Size(210, 25);
            btnRunDerivatives.FlatStyle = FlatStyle.Flat;
            btnRunDerivatives.BackColor = Color.White;
            btnRunDerivatives.Cursor = Cursors.Hand;
            btnRunDerivatives.Click += RunDerivatives_Click;

            btnExportDerivatives.Text = "Export as Text...";
            btnExportDerivatives.Location = new Point(226, 6);
            btnExportDerivatives.Size = new Size(120, 25);
            btnExportDerivatives.FlatStyle = FlatStyle.Flat;
            btnExportDerivatives.BackColor = Color.White;
            btnExportDerivatives.Cursor = Cursors.Hand;
            btnExportDerivatives.Click += ExportDerivatives_Click;

            btnAvlCommandsDerivatives.Location = new Point(354, 6);
            btnAvlCommandsDerivatives.Size = new Size(120, 25);
            StyleAvlCommandsButton(btnAvlCommandsDerivatives, "Stability & Forces - AVL Commands", "To reproduce this stability & forces analysis directly in AVL:" + Constants.vbCrLf + Constants.vbCrLf + "1. load yourfile.avl" + Constants.vbCrLf + "2. oper" + Constants.vbCrLf + "3. x                       (run the case)" + Constants.vbCrLf + "4. st  <Enter/filename>    (stability derivatives, body axes)" + Constants.vbCrLf + "5. sb  <Enter/filename>    (stability derivatives, stability axes)" + Constants.vbCrLf + "6. fn  <Enter/filename>    (neutral point / trim)" + Constants.vbCrLf + "7. fb  <Enter/filename>    (body-axis forces)" + Constants.vbCrLf + "8. hm  <Enter/filename>    (hinge moments)");

            controlPanel.Controls.Add(btnRunDerivatives);
            controlPanel.Controls.Add(btnExportDerivatives);
            controlPanel.Controls.Add(btnAvlCommandsDerivatives);
            outer.Controls.Add(controlPanel, 0, 0);

            // Static-stability summary (pitch/yaw/roll/spiral verdicts + static
            // margin) computed from the raw ST output - see ParseDerivativesInsight
            // / UpdateDerivativesInsights.
            rtbDerivInsights.Dock = DockStyle.Fill;
            rtbDerivInsights.ReadOnly = true;
            rtbDerivInsights.BorderStyle = BorderStyle.None;
            rtbDerivInsights.BackColor = ThemePanelBackColor;
            rtbDerivInsights.Font = new Font("Segoe UI", 9.0f);
            rtbDerivInsights.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtbDerivInsights.Text = "Run the analysis to see a static-stability summary here.";
            rtbDerivInsights.SelectAll();
            rtbDerivInsights.SelectionColor = Color.Gray;
            rtbDerivInsights.DeselectAll();
            outer.Controls.Add(rtbDerivInsights, 0, 1);

            txtDerivatives.Multiline = true;
            txtDerivatives.ReadOnly = true;
            txtDerivatives.ScrollBars = ScrollBars.Both;
            txtDerivatives.WordWrap = false;
            txtDerivatives.Dock = DockStyle.Fill;
            txtDerivatives.Font = new Font(FontFamily.GenericMonospace, 9.5f);
            txtDerivatives.BackColor = ThemeCanvasBackColor;
            txtDerivatives.ForeColor = ThemeAxisColor;
            txtDerivatives.BorderStyle = BorderStyle.None;
            txtDerivatives.Text = "Click \"Run Stability & Forces Analysis\" to compute ST/SB/FN/FB/HM data.";
            outer.Controls.Add(txtDerivatives, 0, 2);

        }

        // Builds the chordwise pressure-distribution tab, driven by AVL's "FE"
        // (element forces) command. FE gives dCp at every chordwise panel of every
        // spanwise strip - too much to usefully overlay at once, so this shows one
        // strip's chordwise dCp distribution at a time via a span-station picker.
        private void InitializeFETab()
        {
            if (FE is not null)
                return;

            FE = new TabPage("Pressure");
            tc1.Controls.Add(FE);

            var outer = new TableLayoutPanel();
            outer.Dock = DockStyle.Fill;
            outer.ColumnCount = 1;
            outer.RowCount = 2;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 36.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
            FE.Controls.Add(outer);

            var controlPanel = new System.Windows.Forms.Panel();
            controlPanel.Dock = DockStyle.Fill;
            controlPanel.BackColor = Color.WhiteSmoke;

            // A nested TableLayoutPanel instead of hand-placed Locations: each control gets its
            // own dedicated auto-sized cell, so it's structurally impossible for one to overlap
            // the next regardless of the font/DPI it actually renders at - see InitializePolarTab
            // for the same fix and why a fixed-pixel-gap approach isn't reliable here.
            var fields = new TableLayoutPanel();
            fields.Location = new Point(8, 4);
            fields.AutoSize = true;
            fields.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            fields.ColumnCount = 4;
            fields.RowCount = 1;
            for (int i = 0; i <= 3; i++)
                fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            btnRunFE.Text = "Run Pressure Analysis";
            btnRunFE.Size = new Size(150, 25);
            btnRunFE.Anchor = AnchorStyles.Left;
            btnRunFE.Margin = new Padding(0, 2, 16, 3);
            btnRunFE.FlatStyle = FlatStyle.Flat;
            btnRunFE.BackColor = Color.White;
            btnRunFE.Cursor = Cursors.Hand;
            btnRunFE.Click += RunFEAnalysis_Click;

            var lblStation = new System.Windows.Forms.Label() { Text = "Span station:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 4, 3) };

            cmbFeStrip.Width = 220;
            cmbFeStrip.Anchor = AnchorStyles.Left;
            cmbFeStrip.Margin = new Padding(0, 4, 3, 3);
            cmbFeStrip.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFeStrip.Enabled = false;
            cmbFeStrip.SelectedIndexChanged += (s, ev) => RenderFEPlot();

            btnAvlCommandsFE.Size = new Size(120, 25);
            btnAvlCommandsFE.Anchor = AnchorStyles.Left;
            btnAvlCommandsFE.Margin = new Padding(16, 2, 3, 3);
            StyleAvlCommandsButton(btnAvlCommandsFE, "Pressure Distribution - AVL Commands", "To reproduce this chordwise pressure distribution directly in AVL:" + Constants.vbCrLf + Constants.vbCrLf + "1. load yourfile.avl" + Constants.vbCrLf + "2. oper" + Constants.vbCrLf + "3. x            (run the case)" + Constants.vbCrLf + "4. fe           (write element/panel forces - dCp at every chordwise panel of every spanwise strip)" + Constants.vbCrLf + "5. <Enter to print to screen, or a filename to save>" + Constants.vbCrLf + Constants.vbCrLf + "This app then picks out one span station's chordwise dCp row at a time from that FE output - use the \"Span station\" dropdown here to pick which one, or read the matching strip's rows directly from the FE output yourself.");

            fields.Controls.Add(btnRunFE, 0, 0);
            fields.Controls.Add(lblStation, 1, 0);
            fields.Controls.Add(cmbFeStrip, 2, 0);
            fields.Controls.Add(btnAvlCommandsFE, 3, 0);

            controlPanel.Controls.Add(fields);
            outer.Controls.Add(controlPanel, 0, 0);

            pFE = new PictureBox();
            pFE.BackColor = ThemeCanvasBackColor;
            pFE.BorderStyle = BorderStyle.FixedSingle;
            pFE.Dock = DockStyle.Fill;
            pFE.Name = "pFE";
            pFE.TabStop = false;
            outer.Controls.Add(pFE, 0, 1);

            AddExportButtonToPanel(controlPanel, pFE, "Pressure_Distribution");
            pFE.Resize += (s, ev) => RenderFEPlot();

            RenderFEPlot();
        }

        // Builds the eigenvalue/root-locus tab, driven by AVL's top-level ".MODE"
        // menu (outside OPER - dynamic-stability eigenmode analysis). Meaningful
        // results require mass/inertia data (uses {project}.mass if present) and a
        // trimmed run case with a real velocity (uses {project}.run if present) -
        // without those AVL still computes something using placeholder mass=1kg,
        // Ixx=Iyy=Izz=1 (confirmed against real AVL), so a warning is shown instead
        // of blocking the run.
        private void InitializeModesTab()
        {
            if (ModesTab is not null)
                return;

            ModesTab = new TabPage("Dynamics");
            tc1.Controls.Add(ModesTab);

            var outer = new TableLayoutPanel();
            outer.Dock = DockStyle.Fill;
            outer.ColumnCount = 1;
            outer.RowCount = 2;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 36.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
            ModesTab.Controls.Add(outer);

            var controlPanel = new System.Windows.Forms.Panel();
            controlPanel.Dock = DockStyle.Fill;
            controlPanel.BackColor = Color.WhiteSmoke;

            btnRunModes.Text = "Run Eigenvalue Analysis";
            btnRunModes.Location = new Point(8, 6);
            btnRunModes.Size = new Size(170, 25);
            btnRunModes.FlatStyle = FlatStyle.Flat;
            btnRunModes.BackColor = Color.White;
            btnRunModes.Cursor = Cursors.Hand;
            btnRunModes.Click += RunModesAnalysis_Click;

            btnModeTips.Text = "Tips to Improve Stability";
            btnModeTips.Location = new Point(186, 6);
            btnModeTips.Size = new Size(170, 25);
            btnModeTips.FlatStyle = FlatStyle.Flat;
            btnModeTips.BackColor = Color.White;
            btnModeTips.Cursor = Cursors.Hand;
            btnModeTips.Click += ShowModeStabilityTips_Click;

            btnModesZoomIn.Text = "+";
            btnModesZoomIn.Location = new Point(364, 6);
            btnModesZoomIn.Size = new Size(28, 25);
            btnModesZoomIn.FlatStyle = FlatStyle.Flat;
            btnModesZoomIn.BackColor = Color.White;
            btnModesZoomIn.Cursor = Cursors.Hand;
            btnModesZoomIn.Click += (s, ev) => ZoomModesPlot(1.25d, pModes.Width / 2.0f, pModes.Height / 2.0f);

            btnModesZoomOut.Text = "-";
            btnModesZoomOut.Location = new Point(394, 6);
            btnModesZoomOut.Size = new Size(28, 25);
            btnModesZoomOut.FlatStyle = FlatStyle.Flat;
            btnModesZoomOut.BackColor = Color.White;
            btnModesZoomOut.Cursor = Cursors.Hand;
            btnModesZoomOut.Click += (s, ev) => ZoomModesPlot(1d / 1.25d, pModes.Width / 2.0f, pModes.Height / 2.0f);

            btnModesZoomReset.Text = "Fit All";
            btnModesZoomReset.Location = new Point(424, 6);
            btnModesZoomReset.Size = new Size(60, 25);
            btnModesZoomReset.FlatStyle = FlatStyle.Flat;
            btnModesZoomReset.BackColor = Color.White;
            btnModesZoomReset.Cursor = Cursors.Hand;
            btnModesZoomReset.Click += (s, ev) => ResetModesZoom();

            btnAvlCommandsModes.Location = new Point(492, 6);
            btnAvlCommandsModes.Size = new Size(120, 25);
            StyleAvlCommandsButton(btnAvlCommandsModes, "Dynamics / Eigenvalues - AVL Commands", "To reproduce this eigenvalue/root-locus analysis directly in AVL:" + Constants.vbCrLf + Constants.vbCrLf + "1. load yourfile.avl" + Constants.vbCrLf + "2. mass yourfile.mass     (needed for meaningful inertia - without it AVL uses a placeholder mass=1kg, Ixx=Iyy=Izz=1)" + Constants.vbCrLf + "3. mset 1                 (apply that mass set to case 1)" + Constants.vbCrLf + "4. case yourfile.run      (load a trimmed run case with a real velocity)" + Constants.vbCrLf + "5. oper" + Constants.vbCrLf + "6. x                      (run/trim the case)" + Constants.vbCrLf + "7. mode                   (enter the dynamic-mode menu, outside OPER)" + Constants.vbCrLf + "8. n                      (compute a new set of eigenvalues)" + Constants.vbCrLf + "9. w  <Enter/filename>    (write the eigenvalues)" + Constants.vbCrLf + Constants.vbCrLf + "('plop' / 'g' at the very start of this app's own script just disables AVL's popup graphics windows - only needed when driving AVL non-interactively like this app does, not when typing commands yourself.)");

            var lblHint = new System.Windows.Forms.Label()
            {
                Text = "Needs a saved Mass tab and a trimmed Run case (with velocity) for meaningful results.",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Location = new Point(620, 11)
            };

            controlPanel.Controls.Add(btnRunModes);
            controlPanel.Controls.Add(btnModeTips);
            controlPanel.Controls.Add(btnModesZoomIn);
            controlPanel.Controls.Add(btnModesZoomOut);
            controlPanel.Controls.Add(btnModesZoomReset);
            controlPanel.Controls.Add(btnAvlCommandsModes);
            controlPanel.Controls.Add(lblHint);
            outer.Controls.Add(controlPanel, 0, 0);

            pModes = new PictureBox();
            pModes.BackColor = ThemeCanvasBackColor;
            pModes.BorderStyle = BorderStyle.FixedSingle;
            pModes.Dock = DockStyle.Fill;
            pModes.Name = "pModes";
            pModes.TabStop = false;
            outer.Controls.Add(pModes, 0, 1);

            AddExportButtonToPanel(controlPanel, pModes, "Root_Locus");
            pModes.Resize += (s, ev) => RenderModesPlot();
            pModes.MouseMove += pModes_MouseMove;
            pModes.MouseLeave += (s, ev) => modesTip.Hide(pModes);
            pModes.MouseWheel += pModes_MouseWheel;
            pModes.MouseDown += pModes_MouseDown;
            pModes.MouseUp += pModes_MouseUp;

            RenderModesPlot();
        }

        // Click-and-drag panning: left-button-down starts a drag, and each move
        // shifts _modesPanX/Y by exactly the data-space distance the cursor moved,
        // so the point under the cursor at drag-start tracks the cursor throughout
        // the drag (standard "grab the canvas" behavior).
        private void pModes_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            if (_lastEigenvalues is null || _lastEigenvalues.Count == 0)
                return;
            _modesIsPanning = true;
            _modesDragLastX = e.X;
            _modesDragLastY = e.Y;
            pModes.Cursor = Cursors.SizeAll;
            modesTip.Hide(pModes);
            _lastModesHoverIndex = -1;
        }

        private void pModes_MouseUp(object sender, MouseEventArgs e)
        {
            if (_modesIsPanning)
            {
                _modesIsPanning = false;
                pModes.Cursor = Cursors.Default;
            }
        }

        // Zooms the root-locus plot by factorMultiplier around the given pixel location
        // (cursorX/cursorY, in pModes-relative coordinates), keeping the data point
        // under that pixel fixed on screen - the same "zoom to cursor" feel as the 3D
        // view's mouse wheel. Falls back to zooming around the plot center when there's
        // no cached bounds yet (e.g. before the first render).
        private void ZoomModesPlot(double factorMultiplier, float cursorX, float cursorY)
        {
            if (_lastEigenvalues is null || _lastEigenvalues.Count == 0)
                return;

            double newZoom = Math.Max(0.5d, Math.Min(_modesZoom * factorMultiplier, 50.0d));
            if (newZoom == _modesZoom)
                return;

            if (_modesPlotW > 0f && _modesPlotH > 0f)
            {
                float fracX = (cursorX - _modesPlotX) / _modesPlotW;
                float fracY = (cursorY - _modesPlotY) / _modesPlotH;
                double dataX = _modesXMin + (double)fracX * (_modesXMax - _modesXMin);
                double dataY = _modesYMax - (double)fracY * (_modesYMax - _modesYMin);

                double fitCenterX = (_modesXMin + _modesXMax) / 2.0d - _modesPanX;
                double fitCenterY = (_modesYMin + _modesYMax) / 2.0d - _modesPanY;
                double fitHalfW = (_modesXMax - _modesXMin) / 2.0d * _modesZoom;
                double fitHalfH = (_modesYMax - _modesYMin) / 2.0d * _modesZoom;

                double newHalfW = fitHalfW / newZoom;
                double newHalfH = fitHalfH / newZoom;

                _modesPanX = dataX - fitCenterX - newHalfW * (double)(2f * fracX - 1f);
                _modesPanY = dataY - fitCenterY + newHalfH * (double)(2f * fracY - 1f);
            }

            _modesZoom = newZoom;
            RenderModesPlot();
        }

        private void ResetModesZoom()
        {
            _modesZoom = 1.0d;
            _modesPanX = 0.0d;
            _modesPanY = 0.0d;
            RenderModesPlot();
        }

        private void pModes_MouseWheel(object sender, MouseEventArgs e)
        {
            ZoomModesPlot(e.Delta > 0 ? 1.25d : 1d / 1.25d, e.X, e.Y);
        }

        // Shows mode details (name, eigenvalue, natural frequency, damping ratio,
        // period) when hovering near a plotted root - ScreenX/ScreenY are stamped
        // onto each EigenValue by DrawModesSubplot every render.
        private void pModes_MouseMove(object sender, MouseEventArgs e)
        {
            if (_lastEigenvalues is null || _lastEigenvalues.Count == 0)
                return;

            if (_modesIsPanning)
            {
                if (_modesPlotW > 0f && _modesPlotH > 0f)
                {
                    int deltaPixelX = e.X - _modesDragLastX;
                    int deltaPixelY = e.Y - _modesDragLastY;
                    _modesPanX -= deltaPixelX * (_modesXMax - _modesXMin) / _modesPlotW;
                    _modesPanY += deltaPixelY * (_modesYMax - _modesYMin) / _modesPlotH;
                    _modesDragLastX = e.X;
                    _modesDragLastY = e.Y;
                    RenderModesPlot();
                }
                return;
            }

            EigenValue nearest = null;
            double nearestDist = double.MaxValue;
            int nearestIndex = -1;
            for (int i = 0, loopTo = _lastEigenvalues.Count - 1; i <= loopTo; i++)
            {
                var ev = _lastEigenvalues[i];
                float dx = e.X - ev.ScreenX;
                float dy = e.Y - ev.ScreenY;
                double dist = Math.Sqrt((double)(dx * dx + dy * dy));
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = ev;
                    nearestIndex = i;
                }
            }

            if (nearest is null || nearestDist > 8d)
            {
                if (_lastModesHoverIndex != -1)
                {
                    modesTip.Hide(pModes);
                    _lastModesHoverIndex = -1;
                }
                return;
            }

            if (nearestIndex == _lastModesHoverIndex)
                return;
            _lastModesHoverIndex = nearestIndex;

            double omega = Math.Sqrt(nearest.Real * nearest.Real + nearest.Imag * nearest.Imag);
            double zeta = omega > 0d ? -nearest.Real / omega : 0.0d;
            var lines = new List<string>();
            lines.Add(string.IsNullOrEmpty(nearest.ModeLabel) ? "Unclassified mode" : nearest.ModeLabel);
            lines.Add($"Eigenvalue: {nearest.Real:0.###} {(nearest.Imag >= 0d ? "+" : "-")} {Math.Abs(nearest.Imag):0.###}i");
            lines.Add($"Natural freq (ωn): {omega:0.###} rad/s");
            lines.Add($"Damping ratio (ζ): {zeta:0.###}");
            if (Math.Abs(nearest.Imag) > 0.001d)
            {
                lines.Add($"Period: {2d * Math.PI / Math.Abs(nearest.Imag):0.###} s");
            }
            lines.Add(nearest.Real < 0d ? "Stable" : "Unstable");

            modesTip.Show(string.Join(Constants.vbCrLf, lines), pModes, e.X + 12, e.Y + 12, 6000);
        }

        // Adds a "Load Test Project" toolbar dropdown next to "New Project" offering three ready-made
        // .avl/.mass/.run trios, simple to complex, each verified against the real AVL 3.37 binary
        // (LOAD/MASS/MSET/CASE/OPER/ST/SB/VM/FN/FE/MODE all return real non-empty data, no crashes):
        // Simple   - a single flat rectangular wing. No controls, no advanced keywords - just enough
        // to exercise Trefftz/Loads on the fastest possible geometry.
        // Standard - the original 3-surface aircraft (wing+aileron, tail+elevator, fin+rudder) that
        // exercises every analysis tab. Unchanged from before.
        // Advanced - 5 surfaces deliberately exercising several keywords added in this pass: SCALE and
        // TRANSLATE (a winglet scaled down and translated onto the wingtip), ANGLE with a
        // nonzero value, COMPONENT grouping (wing+winglet share one Lcomp, tail+fin share
        // another), and NOWAKE (a simple non-lifting fuselage side-profile). Deliberately
        // does NOT include a standalone demo of NOALBE/NOLOAD/nonzero-YDUPLICATE - an
        // earlier version had an isolated "DemoStub" surface for that, but a disconnected
        // floating block with no aerodynamic role of its own was more confusing than useful
        // for students looking at the geometry.
        private void InitializeFileMenu()
        {
            btnFileMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnFileMenu.Text = "File";

            btnLoadTestProject.Text = "Load Test Project";

            var itemSimple = new ToolStripMenuItem("Simple (Single Wing)");
            itemSimple.ToolTipText = "One flat rectangular wing, no controls, no advanced keywords - the fastest way to sanity-check Trefftz/Loads.";
            itemSimple.Click += (s, ev) => LoadTestProject("TestWingSimple", TestProjectAvlTextSimple(), TestProjectMassTextSimple(), TestProjectRunTextSimple(), "a single flat rectangular wing with no controls." + Constants.vbCrLf + Constants.vbCrLf + "It's the fastest way to sanity-check Trefftz/Loads on a minimal geometry.");

            var itemStandard = new ToolStripMenuItem("Standard (3-Surface Aircraft)");
            itemStandard.ToolTipText = "Wing+aileron, tail+elevator, fin+rudder - exercises every analysis tab.";
            itemStandard.Click += (s, ev) => LoadTestProject("TestAircraft", TestProjectAvlTextStandard(), TestProjectMassTextStandard(), TestProjectRunTextStandard(), "a 3-surface aircraft (wing+aileron, tail+elevator, fin+rudder) with a mass breakdown and a trimmed run case." + Constants.vbCrLf + Constants.vbCrLf + "It's set up to exercise every analysis tab - Trefftz, Loads, Polar, Derivatives, Pressure, and Dynamics.");

            var itemAdvanced = new ToolStripMenuItem("Advanced (All New Features)");
            itemAdvanced.ToolTipText = "5 surfaces exercising SCALE, TRANSLATE, ANGLE, COMPONENT, and NOWAKE.";
            itemAdvanced.Click += (s, ev) => LoadTestProject("TestAircraftAdvanced", TestProjectAvlTextAdvanced(), TestProjectMassTextAdvanced(), TestProjectRunTextAdvanced(), "a 5-surface aircraft deliberately exercising several recently-added keywords:" + Constants.vbCrLf + "- SCALE + TRANSLATE (a winglet scaled down and positioned onto the wingtip)" + Constants.vbCrLf + "- ANGLE with a nonzero incidence offset" + Constants.vbCrLf + "- COMPONENT grouping (wing+winglet share one Lcomp, tail+fin share another)" + Constants.vbCrLf + "- NOWAKE (a simple non-lifting fuselage side-profile)");

            btnLoadTestProject.DropDownItems.AddRange(new ToolStripItem[] { itemSimple, itemStandard, itemAdvanced });

            btnMakeCopy.Text = "Make a Copy of This Project...";
            btnMakeCopy.ToolTipText = "Copy the current project's .avl/.mass/.run files under a new name (suffix), then switch to editing the copy - handy for branching off a design before trying something risky.";
            btnMakeCopy.Click += MakeProjectCopy_Click;

            btnFileMenu.DropDownItems.AddRange(new ToolStripItem[] { btnLoadTestProject, new ToolStripSeparator(), btnMakeCopy });

            int insertAt = ToolStrip1.Items.IndexOf(txtName);
            if (insertAt >= 0)
            {
                ToolStrip1.Items.Insert(insertAt + 1, btnFileMenu);
            }
            else
            {
                ToolStrip1.Items.Add(btnFileMenu);
            }
        }

        private void LoadTestProject(string pname, string avlText, string massText, string runText, string description)
        {
            string avlPath = Path.Combine(Application.StartupPath, $"{pname}.avl");
            string massPath = Path.Combine(Application.StartupPath, $"{pname}.mass");
            string runPath = Path.Combine(Application.StartupPath, $"{pname}.run");

            if (File.Exists(avlPath) || File.Exists(massPath) || File.Exists(runPath))
            {
                var res = AppMessageBox.Show($"A project named \"{pname}\" already exists and will be overwritten with the test project." + Constants.vbCrLf + Constants.vbCrLf + "Continue?", "Load Test Project", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.No)
                    return;
            }

            try
            {
                File.WriteAllText(avlPath, avlText);
                File.WriteAllText(massPath, massText);
                File.WriteAllText(runPath, runText);
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error creating test project files: " + ex.Message, "Load Test Project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            projectName = pname;
            txtName.Text = projectName;
            txtName_SelectedIndexChanged(null, null);
            tc1.SelectedIndex = 0;

            AppMessageBox.Show($"Test project \"{pname}\" created: " + description, "Test Project Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Copies the currently loaded project's .avl/.mass/.run files (whichever exist) to
        // "{loadedProjectName}{suffix}.ext", then switches to editing the copy - a quick way to
        // branch off a design before trying something risky, without losing the original.
        // Uses loadedProjectName (what's actually on disk/in the editor right now), not
        // projectName (which can be mid-typed), and force-saves the active tab first so the copy
        // reflects the latest in-progress edits rather than a stale on-disk version.
        private void MakeProjectCopy_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(loadedProjectName))
            {
                AppMessageBox.Show("Load or create a project first.", "Make a Copy of This Project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            switch (tc1.SelectedTab.Name ?? "")
            {
                case "Geometry":
                    {
                        SaveAVL();
                        break;
                    }
                case "Mass":
                    {
                        SaveMass();
                        break;
                    }
                case "Run":
                    {
                        SaveRun();
                        break;
                    }
            }

            string suffix = "";
            char[] invalidChars = Path.GetInvalidFileNameChars();
            do
            {
                string entered = Interaction.InputBox($"Enter a suffix to append to \"{loadedProjectName}\" for the copy (e.g. _v2):", "Make a Copy of This Project", "");
                suffix = entered.Trim();
                if (string.IsNullOrEmpty(suffix))
                    return; // Cancelled, or left blank

                if (suffix.IndexOfAny(invalidChars) >= 0)
                {
                    AppMessageBox.Show("That suffix contains characters that aren't allowed in a file name.", "Make a Copy of This Project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    suffix = "";
                    continue;
                }
                break;
            }
            while (string.IsNullOrEmpty(suffix));

            string newName = loadedProjectName + suffix;
            string[] exts = new[] { "avl", "mass", "run" };

            bool anySource = false;
            bool targetExists = false;
            foreach (var ext in exts)
            {
                if (File.Exists(Path.Combine(Application.StartupPath, $"{loadedProjectName}.{ext}")))
                    anySource = true;
                if (File.Exists(Path.Combine(Application.StartupPath, $"{newName}.{ext}")))
                    targetExists = true;
            }

            if (!anySource)
            {
                AppMessageBox.Show($"No .avl/.mass/.run files found for \"{loadedProjectName}\" to copy.", "Make a Copy of This Project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (targetExists)
            {
                var res = AppMessageBox.Show($"A project named \"{newName}\" already exists and will be overwritten with the copy." + Constants.vbCrLf + Constants.vbCrLf + "Continue?", "Make a Copy of This Project", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.No)
                    return;
            }

            try
            {
                foreach (var ext in exts)
                {
                    string srcPath = Path.Combine(Application.StartupPath, $"{loadedProjectName}.{ext}");
                    string dstPath = Path.Combine(Application.StartupPath, $"{newName}.{ext}");
                    if (File.Exists(srcPath))
                        File.Copy(srcPath, dstPath, true);
                }
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error copying project files: " + ex.Message, "Make a Copy of This Project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Refresh the project dropdown (in this window and every other open Geometry window)
            // so the copy shows up immediately, then switch to editing it.
            My.MyProject.Forms.frmMain.findAVLs(Environment.CurrentDirectory);
            txtName.Text = newName;
            txtName_SelectedIndexChanged(null, null);

            AppToast.Show($"Created \"{newName}\" as a copy of \"{loadedProjectName}\"");
        }

        private string TestProjectAvlTextSimple()
        {
            return string.Join(Constants.vbCrLf, new string[] { "[Simple Wing]", "#Mach", "0.0", "#IYsym   IZsym   Zsym", "0        0       0.0", "#Sref    Cref    Bref", "10.0     1.0     10.0", "#Xref    Yref    Zref", "0.25     0.0     0.0", "!begingeometry", "SURFACE", "[Wing]", "!beginsurface", "#Nchord  Cspace   Nspan   Sspace", "8            1.0      10          1.0", "#", "YDUPLICATE", "0.0", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc", "0.0     0.0    0.0     1.0     0.0", "NACA", "0012", "!endsection", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc", "0.0     5.0    0.0     1.0     0.0", "NACA", "0012", "!endsection", "!endsurface", "!endgeometry", "" });
        }

        private string TestProjectMassTextSimple()
        {
            return string.Join(Constants.vbCrLf, new string[] { "Lunit = 1.0 m", "Munit = 1.0 kg", "Tunit = 1.0 s", "g = 9.81", "rho = 1.225", "", "#mass   x       y      z      Ixx    Iyy    Izz", "2.0     0.25    0.0    0.0    1.0    0.3    1.2   !wing", "" });
        }

        private string TestProjectRunTextSimple()
        {
            return string.Join(Constants.vbCrLf, new string[] { " ---------------------------------------------", " Run case  1:  Cruise", "", " alpha        ->  alpha       =   2.00000", " beta         ->  beta        =   0.00000", " pb/2V        ->  pb/2V       =   0.00000", " qc/2V        ->  qc/2V       =   0.00000", " rb/2V        ->  rb/2V       =   0.00000", "", " alpha     =   2.00000     deg", " beta      =   0.00000     deg", " pb/2V     =   0.00000", " qc/2V     =   0.00000", " rb/2V     =   0.00000", " CL        =   0.00000", " CDo       =   0.00000", " bank      =   0.00000     deg", " elevation =   0.00000     deg", " heading   =   0.00000     deg", " Mach      =   0.00000", " velocity  =  15.00000     m/s", " density   =   1.22500     kg/m^3", " grav.acc. =   9.81000     m/s^2", " turn_rad. =   0.00000     m", " load_fac. =   0.00000", " X_cg      =   0.25000     m", " Y_cg      =   0.00000     m", " Z_cg      =   0.00000     m", " mass      =   2.00000     kg", " Ixx       =   1.00000     kg-m^2", " Iyy       =   0.30000     kg-m^2", " Izz       =   1.20000     kg-m^2", " Ixy       =   0.00000     kg-m^2", " Iyz       =   0.00000     kg-m^2", " Izx       =   0.00000     kg-m^2", " visc CL_a =   0.00000", " visc CL_u =   0.00000", " visc CM_a =   0.00000", " visc CM_u =   0.00000", "" });
        }

        private string TestProjectAvlTextStandard()
        {
            return string.Join(Constants.vbCrLf, new string[] { "[Test Aircraft]", "#Mach", "0.0", "#IYsym   IZsym   Zsym", "0        0       0.0", "#Sref    Cref    Bref", "7.2      0.9     8.0", "#Xref    Yref    Zref", "0.87     0.0     0.0", "!begingeometry", "#====================================================================", "SURFACE", "[Wing]", "!beginsurface", "#Nchord  Cspace   Nspan   Sspace", "10           1.0      16          1.0", "#", "YDUPLICATE", "0.0", "#", "ANGLE", "0.0", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc  Nspan  Sspace", "0.0     0.0    0.0     1.2     0.0   0          0", "NACA", "4412", "!endsection", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc  Nspan  Sspace", "0.21    2.8    0.245   0.78    -2.1  0          0", "NACA", "4412", "!endsection", "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname   Cgain  Xhinge  XYZhvec      SgnDup", "aileron  1.0    0.75    0.0 0.0 0.0  -1.0", "!endcontrol", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc  Nspan  Sspace", "0.30    4.0    0.35    0.6     -3.0  0          0", "NACA", "4412", "!endsection", "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname   Cgain  Xhinge  XYZhvec      SgnDup", "aileron  1.0    0.75    0.0 0.0 0.0  -1.0", "!endcontrol", "!endsurface", "#====================================================================", "SURFACE", "[Htail]", "!beginsurface", "#Nchord  Cspace   Nspan   Sspace", "8            1.0      8           1.0", "#", "YDUPLICATE", "0.0", "#", "ANGLE", "0.0", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc  Nspan  Sspace", "3.5     0.0    0.1     0.5     0.0   0          0", "NACA", "0012", "!endsection", "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname    Cgain  Xhinge  XYZhvec      SgnDup", "elevator  1.0    0.6     0.0 0.0 0.0  1.0", "!endcontrol", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc  Nspan  Sspace", "3.5     1.4    0.1     0.5     0.0   0          0", "NACA", "0012", "!endsection", "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname    Cgain  Xhinge  XYZhvec      SgnDup", "elevator  1.0    0.6     0.0 0.0 0.0  1.0", "!endcontrol", "!endsurface", "#====================================================================", "SURFACE", "[Vtail]", "!beginsurface", "#Nchord  Cspace   Nspan   Sspace", "8            1.0      8           1.0", "#", "ANGLE", "0.0", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc  Nspan  Sspace", "3.5     0.0    0.1     0.5     0.0   0          0", "NACA", "0012", "!endsection", "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname   Cgain  Xhinge  XYZhvec      SgnDup", "rudder   1.0    0.6     0.0 0.0 0.0  1.0", "!endcontrol", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc  Nspan  Sspace", "3.7     0.0    1.3     0.3     0.0   0          0", "NACA", "0012", "!endsection", "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname   Cgain  Xhinge  XYZhvec      SgnDup", "rudder   1.0    0.6     0.0 0.0 0.0  1.0", "!endcontrol", "!endsurface", "!endgeometry", "" });
        }

        private string TestProjectMassTextStandard()
        {
            return string.Join(Constants.vbCrLf, new string[] { "#[Test Aircraft]", "!x,y,z cordinate system matches AVL default", "Lunit = 1.0 m", "Munit = 1.0 kg", "Tunit = 1.0 s", "#-------------------------", "g = 9.81", "rho = 1.225", "#-------------------------", "#mass   x       y      z      Ixx    Iyy    Izz", "5.0     0.3     0.0    0.0    2.0    0.6    2.5   !wing structure", "1.5     3.5     0.0    0.1    0.05   0.15   0.2   !tail structure", "3.0     0.5     0.0    -0.05  0.02   0.3    0.32  !fuselage + payload", "" });
        }

        private string TestProjectRunTextStandard()
        {
            return string.Join(Constants.vbCrLf, new string[] { " ---------------------------------------------", " Run case  1:  Cruise", "", " alpha        ->  alpha       =   3.00000", " beta         ->  beta        =   0.00000", " pb/2V        ->  pb/2V       =   0.00000", " qc/2V        ->  qc/2V       =   0.00000", " rb/2V        ->  rb/2V       =   0.00000", " aileron      ->  Cl roll mom =   0.00000", " elevator     ->  Cm pitchmom =   0.00000", " rudder       ->  Cn yaw  mom =   0.00000", "", " alpha     =   3.00000     deg", " beta      =   0.00000     deg", " pb/2V     =   0.00000", " qc/2V     =   0.00000", " rb/2V     =   0.00000", " CL        =   0.00000", " CDo       =   0.02000", " bank      =   0.00000     deg", " elevation =   0.00000     deg", " heading   =   0.00000     deg", " Mach      =   0.00000", " velocity  =  18.00000     m/s", " density   =   1.22500     kg/m^3", " grav.acc. =   9.81000     m/s^2", " turn_rad. =   0.00000     m", " load_fac. =   0.00000", " X_cg      =   0.87000     m", " Y_cg      =   0.00000     m", " Z_cg      =   0.05000     m", " mass      =   9.50000     kg", " Ixx       =   2.50000     kg-m^2", " Iyy       =   3.20000     kg-m^2", " Izz       =   5.10000     kg-m^2", " Ixy       =   0.00000     kg-m^2", " Iyz       =   0.00000     kg-m^2", " Izx       =   0.00000     kg-m^2", " visc CL_a =   0.00000", " visc CL_u =   0.00000", " visc CM_a =   0.00000", " visc CM_u =   0.00000", "" });
        }

        // 6 surfaces exercising every keyword added in this pass - verified against the real AVL 3.37
        // binary directly (LOAD/MASS/MSET/CASE/OPER/X/ST/VM/FN/FE/MODE all return real non-empty data,
        // no crashes) before being written here. See the comment on InitializeFileMenu for what
        // each surface demonstrates.
        private string TestProjectAvlTextAdvanced()
        {
            return string.Join(Constants.vbCrLf, new string[] { "[Advanced Test Aircraft]", "#Mach", "0.0", "#IYsym   IZsym   Zsym", "0        0       0.0", "#Sref    Cref    Bref", "7.2      0.9     8.0", "#Xref    Yref    Zref", "0.87     0.0     0.0", "!begingeometry", "#====================================================================", "SURFACE", "[Wing]", "!beginsurface", "#Nchord  Cspace   Nspan   Sspace", "10           1.0      14          1.0", "#", "COMPONENT", "1", "#", "YDUPLICATE", "0.0", "#", "ANGLE", "1.0", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc", "0.0     0.0    0.0     1.2     0.0", "NACA", "4412", "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname   Cgain  Xhinge  XYZhvec      SgnDup", "aileron  1.0    0.75    0.0 0.0 0.0  -1.0", "!endcontrol", "!endsection", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc", "0.3     4.0    0.2     0.7     -1.0", "NACA", "4412", "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname   Cgain  Xhinge  XYZhvec      SgnDup", "aileron  1.0    0.75    0.0 0.0 0.0  -1.0", "!endcontrol", "!endsection", "!endsurface", "#====================================================================", "SURFACE", "[Winglet]", "!beginsurface", "#Nchord  Cspace   Nspan   Sspace", "6            1.0      6           1.0", "#", "COMPONENT", "1", "#", "SCALE", "0.4  0.4  0.4", "#", "TRANSLATE", "0.3  4.0  0.2", "#", "YDUPLICATE", "0.0", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc", "0.0     0.0    0.0     1.0     0.0", "NACA", "0012", "!endsection", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc", "0.2     0.0    1.0     0.6     0.0", "NACA", "0012", "!endsection", "!endsurface", "#====================================================================", "SURFACE", "[Htail]", "!beginsurface", "#Nchord  Cspace   Nspan   Sspace", "8            1.0      8           1.0", "#", "COMPONENT", "2", "#", "YDUPLICATE", "0.0", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc", "3.5     0.0    0.1     0.5     0.0", "NACA", "0012", "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname    Cgain  Xhinge  XYZhvec      SgnDup", "elevator  1.0    0.6     0.0 0.0 0.0  1.0", "!endcontrol", "!endsection", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc", "3.5     1.4    0.1     0.5     0.0", "NACA", "0012", "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname    Cgain  Xhinge  XYZhvec      SgnDup", "elevator  1.0    0.6     0.0 0.0 0.0  1.0", "!endcontrol", "!endsection", "!endsurface", "#====================================================================", "SURFACE", "[Vtail]", "!beginsurface", "#Nchord  Cspace   Nspan   Sspace", "8            1.0      8           1.0", "#", "COMPONENT", "2", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc", "3.5     0.0    0.1     0.5     0.0", "NACA", "0012", "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname   Cgain  Xhinge  XYZhvec      SgnDup", "rudder   1.0    0.6     0.0 0.0 0.0  1.0", "!endcontrol", "!endsection", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc", "3.7     0.0    1.3     0.3     0.0", "NACA", "0012", "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", "CONTROL", "!begincontrol", "#Cname   Cgain  Xhinge  XYZhvec      SgnDup", "rudder   1.0    0.6     0.0 0.0 0.0  1.0", "!endcontrol", "!endsection", "!endsurface", "#====================================================================", "SURFACE", "[Fuselage]", "!beginsurface", "#Nchord  Cspace   Nspan   Sspace", "6            1.0      1           0.0", "#", "NOWAKE", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc", "-0.6    0.0    -0.35   4.6     0.0", "NACA", "0006", "!endsection", "#-------------------------------------------------------------", "SECTION", "!beginsection", "#Xle    Yle    Zle     Chord   Ainc", "-0.6    0.02   -0.35   4.6     0.0", "NACA", "0006", "!endsection", "!endsurface", "!endgeometry", "" });
        }

        private string TestProjectMassTextAdvanced()
        {
            return string.Join(Constants.vbCrLf, new string[] { "Lunit = 1.0 m", "Munit = 1.0 kg", "Tunit = 1.0 s", "g = 9.81", "rho = 1.225", "", "#mass   x       y      z      Ixx    Iyy    Izz", "5.0     0.3     0.0    0.0    2.0    0.6    2.5   !wing structure", "0.3     0.35    2.5    0.3    0.1    0.05   0.12  !winglets (both sides)", "1.5     3.5     0.0    0.1    0.05   0.15   0.2   !tail structure", "3.0     0.5     0.0    -0.05  0.02   0.3    0.32  !fuselage + payload", "" });
        }

        private string TestProjectRunTextAdvanced()
        {
            return string.Join(Constants.vbCrLf, new string[] { " ---------------------------------------------", " Run case  1:  Cruise", "", " alpha        ->  alpha       =   3.00000", " beta         ->  beta        =   0.00000", " pb/2V        ->  pb/2V       =   0.00000", " qc/2V        ->  qc/2V       =   0.00000", " rb/2V        ->  rb/2V       =   0.00000", " aileron      ->  Cl roll mom =   0.00000", " elevator     ->  Cm pitchmom =   0.00000", " rudder       ->  Cn yaw  mom =   0.00000", "", " alpha     =   3.00000     deg", " beta      =   0.00000     deg", " pb/2V     =   0.00000", " qc/2V     =   0.00000", " rb/2V     =   0.00000", " CL        =   0.00000", " CDo       =   0.02000", " bank      =   0.00000     deg", " elevation =   0.00000     deg", " heading   =   0.00000     deg", " Mach      =   0.00000", " velocity  =  18.00000     m/s", " density   =   1.22500     kg/m^3", " grav.acc. =   9.81000     m/s^2", " turn_rad. =   0.00000     m", " load_fac. =   0.00000", " X_cg      =   0.87000     m", " Y_cg      =   0.00000     m", " Z_cg      =   0.05000     m", " mass      =   9.80000     kg", " Ixx       =   4.04000     kg-m^2", " Iyy       =  13.60000     kg-m^2", " Izz       =  17.40000     kg-m^2", " Ixy       =   0.00000     kg-m^2", " Iyz       =   0.00000     kg-m^2", " Izx       =   0.00000     kg-m^2", " visc CL_a =   0.00000", " visc CL_u =   0.00000", " visc CM_a =   0.00000", " visc CM_u =   0.00000", "" });
        }

        // Was positioned once at creation time (Math.Max(pb.Width, 160) - 85) and
        // left to the Anchor property to keep up from there - the same fragile
        // pattern AddExportButtonToPanel replaced for the analysis tabs. Anchor
        // only preserves the ORIGINAL right-edge offset as the parent resizes; if
        // the PictureBox's real final width ever ended up smaller than the 160px
        // fallback this assumed (e.g. a cramped layout with a side panel open),
        // the button's assumed position no longer fit and it went out of view.
        // Actively repositioning off the CURRENT ClientSize on every resize fixes
        // it the same way it did there.
        private void AddExportButtonTo(PictureBox pb, string viewName)
        {
            var btnExport = new System.Windows.Forms.Button();
            btnExport.Text = "Export ▾";
            btnExport.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            btnExport.BackColor = Color.White;
            btnExport.ForeColor = Color.Black;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.FlatAppearance.BorderSize = 1;
            btnExport.FlatAppearance.BorderColor = Color.LightGray;
            btnExport.Size = new Size(75, 25);
            btnExport.Top = 10;
            btnExport.Cursor = Cursors.Hand;

            var menu = new ContextMenuStrip();

            var pngItem = new ToolStripMenuItem("Export as PNG...", null, (s, ev) => ExportView(pb, "PNG", viewName));
            var svgItem = new ToolStripMenuItem("Export as SVG...", null, (s, ev) => ExportView(pb, "SVG", viewName));
            var pdfItem = new ToolStripMenuItem("Export as PDF...", null, (s, ev) => ExportView(pb, "PDF", viewName));

            menu.Items.Add(pngItem);
            menu.Items.Add(svgItem);
            menu.Items.Add(pdfItem);

            btnExport.Click += (s, ev) => menu.Show(btnExport, new Point(0, btnExport.Height));

            void reposition() => btnExport.Left = Math.Max(0, pb.ClientSize.Width - btnExport.Width - 10);
            pb.Resize += (s, ev) => reposition();

            pb.Controls.Add(btnExport);
            btnExport.BringToFront();
            reposition();
        }

        // Same export menu as AddExportButtonTo, but placed as a normal docked
        // child of the tab's top control panel instead of floating on top of the
        // PictureBox. Anchored to the panel's right edge and actively repositioned
        // on every panel resize (rather than a fixed X guessed at creation time),
        // so it can't end up hidden behind other controls or off-panel regardless
        // of layout timing or how wide any auto-sized labels in the panel turn out
        // to be (e.g. the Dynamics tab's hint text).
        private void AddExportButtonToPanel(System.Windows.Forms.Panel panel, PictureBox pb, string viewName)
        {
            var btnExport = new System.Windows.Forms.Button();
            btnExport.Text = "Export ▾";
            btnExport.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            btnExport.BackColor = Color.White;
            btnExport.ForeColor = Color.Black;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.FlatAppearance.BorderSize = 1;
            btnExport.FlatAppearance.BorderColor = Color.LightGray;
            btnExport.Size = new Size(75, 25);
            btnExport.Top = 6;
            btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExport.Cursor = Cursors.Hand;

            var menu = new ContextMenuStrip();

            var pngItem = new ToolStripMenuItem("Export as PNG...", null, (s, ev) => ExportView(pb, "PNG", viewName));
            var svgItem = new ToolStripMenuItem("Export as SVG...", null, (s, ev) => ExportView(pb, "SVG", viewName));
            var pdfItem = new ToolStripMenuItem("Export as PDF...", null, (s, ev) => ExportView(pb, "PDF", viewName));

            menu.Items.Add(pngItem);
            menu.Items.Add(svgItem);
            menu.Items.Add(pdfItem);

            btnExport.Click += (s, ev) => menu.Show(btnExport, new Point(0, btnExport.Height));

            void reposition() => btnExport.Left = Math.Max(0, panel.ClientSize.Width - btnExport.Width - 10);
            panel.Resize += (s, ev) => reposition();

            panel.Controls.Add(btnExport);
            btnExport.BringToFront();
            reposition();
        }

        private void AddViewPresetButtonTo(PictureBox pb)
        {
            var btnView = new System.Windows.Forms.Button();
            btnView.Text = "View ▾";
            btnView.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            btnView.BackColor = Color.White;
            btnView.ForeColor = Color.Black;
            btnView.FlatStyle = FlatStyle.Flat;
            btnView.FlatAppearance.BorderSize = 1;
            btnView.FlatAppearance.BorderColor = Color.LightGray;
            btnView.Size = new Size(65, 25);
            btnView.Top = 10;
            btnView.Cursor = Cursors.Hand;

            var menu = new ContextMenuStrip();

            // Isometric/Front/Back angles below were re-derived (not hand-guessed) against the actual
            // rotation pipeline (RotateZ(gamma) -> RotateY(alpha) -> RotateX(beta), in that order - see
            // RotateAroundCenter's comment for why gamma is deliberately applied first). The old presets
            // picked a nonzero gamma alongside a nonzero alpha as if gamma were a camera roll applied
            // AFTER aiming the camera, but since gamma actually rotates the model in its own original
            // frame BEFORE yaw/pitch, that combination doesn't do what it looks like it should: Front/
            // Back were rendering a side-view shape (fuselage spread across the screen, wingspan
            // collapsed to depth - backwards).
            // Isometric specifically needs gamma <> 0: with gamma=0, RotateY leaves Y untouched and
            // RotateX leaves X untouched, so a point's span (Y) coordinate can NEVER contribute to the
            // horizontal screen axis - only its chord/thickness (X/Z) can. For a real wing (long span,
            // short chord) that collapses the whole wing into a tall vertical blade instead of a
            // recognizable rectangle, even though a same-size synthetic test cube looks fine (a cube's
            // axes are all equal length, so the missing horizontal contribution isn't visually obvious).
            // Earlier attempts here were solved by numeric optimization against a proxy metric and
            // turned out visually wrong twice over - this value was instead picked interactively (via
            // the alpha/beta/gamma readout DrawViewParamsOverlay draws in the 3D view) and confirmed
            // to look correct, which is the more trustworthy check for "does this look like a proper
            // isometric" than any proxy metric.
            var isoItem = new ToolStripMenuItem("Isometric", null, (s, ev) => Set3DView(0f, 120f, 50f));
            var defaultItem = new ToolStripMenuItem("Default", null, (s, ev) => Set3DView(0f, 120f, 0f));
            var sep = new ToolStripSeparator();
            var frontItem = new ToolStripMenuItem("Front", null, (s, ev) => Set3DView(0f, 90f, 90f));
            var backItem = new ToolStripMenuItem("Back", null, (s, ev) => Set3DView(0f, 90f, -90));
            var leftItem = new ToolStripMenuItem("Left", null, (s, ev) => Set3DView(180f, -90, 0f));
            var rightItem = new ToolStripMenuItem("Right", null, (s, ev) => Set3DView(0f, 90f, 0f));
            var topItem = new ToolStripMenuItem("Top", null, (s, ev) => Set3DView(0f, 180f, 0f));
            var bottomItem = new ToolStripMenuItem("Bottom", null, (s, ev) => Set3DView(0f, 0f, 0f));

            menu.Items.Add(isoItem);
            menu.Items.Add(defaultItem);
            menu.Items.Add(sep);
            menu.Items.Add(frontItem);
            menu.Items.Add(backItem);
            menu.Items.Add(leftItem);
            menu.Items.Add(rightItem);
            menu.Items.Add(topItem);
            menu.Items.Add(bottomItem);

            btnView.Click += (s, ev) => menu.Show(btnView, new Point(0, btnView.Height));

            void reposition() => btnView.Left = Math.Max(0, pb.ClientSize.Width - btnView.Width - 95);
            pb.Resize += (s, ev) => reposition();

            pb.Controls.Add(btnView);
            reposition();
        }

        // Small "?" hint button, verified against the actual rotation code
        // (p3d_MouseMove) and Node.RotateX/Y/Z rather than assumed from the
        // viewAlpha/Beta/Gamma field comments alone: viewAlpha feeds RotateY
        // (Yaw), viewBeta feeds RotateX (Pitch), viewGamma feeds RotateZ (Roll) -
        // confirmed both by the rotation-matrix math in each RotateX/Y/Z
        // function and by cross-checking Set3DView's own preset angles (e.g.
        // "Top" = beta 180, "Bottom" = beta 0, consistent with beta/Pitch being
        // what tips the camera between looking from above vs. below).
        private void AddRotationHintTo(PictureBox pb)
        {
            var btnHint = new System.Windows.Forms.Button();
            btnHint.Text = "?";
            btnHint.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            btnHint.BackColor = Color.White;
            btnHint.ForeColor = Color.Black;
            btnHint.FlatStyle = FlatStyle.Flat;
            btnHint.FlatAppearance.BorderSize = 1;
            btnHint.FlatAppearance.BorderColor = Color.LightGray;
            btnHint.Size = new Size(25, 25);
            btnHint.Top = 10;
            btnHint.Cursor = Cursors.Help;

            // Described by axis LETTER (X/Y/Z), matching the colored gizmo drawn
            // in the view (Red=X, Green=Y, Blue=Z), rather than roll/pitch/yaw -
            // this app's internal Yaw/Pitch/Roll naming (viewAlpha/Beta/Gamma)
            // doesn't line up with standard aircraft-body-axis convention (where
            // yaw is the Z/vertical axis, not Y), so using that terminology here
            // would only invite confusion for anyone thinking in aircraft terms.
            string hintText = "3D view controls (see the colored X/Y/Z axis lines):" + Constants.vbCrLf + "Left-drag: rotate around Y (sideways) and X (up/down)" + Constants.vbCrLf + "Right-drag sideways: rotate around X" + Constants.vbCrLf + "Middle-drag sideways: rotate around Z" + Constants.vbCrLf + "Scroll wheel: zoom";

            var tip = new System.Windows.Forms.ToolTip();
            tip.AutoPopDelay = 15000;
            tip.InitialDelay = 200;
            tip.SetToolTip(btnHint, hintText);

            // A ToolTip only appears on hover - clicking the button did nothing,
            // which is what was reported. Clicking now shows the same text
            // explicitly so the hint is reachable either way.
            btnHint.Click += (s, ev) => AppMessageBox.Show(hintText, "3D View Controls", MessageBoxButtons.OK, MessageBoxIcon.Information);

            void reposition() => btnHint.Left = Math.Max(0, pb.ClientSize.Width - btnHint.Width - 170);
            pb.Resize += (s, ev) => reposition();

            pb.Controls.Add(btnHint);
            btnHint.BringToFront();
            reposition();
        }

        // Small +/- buttons placed to the right of DrawViewParamsOverlay's readout box,
        // one pair per angle, so each of alpha/beta/gamma can be nudged individually
        // without dragging the whole view. Real WinForms Buttons on top of p3d (like
        // AddRotationHintTo's "?" button), not part of the SVG/PDF-captured render.
        private void Set3DView(float alpha, float beta, float gamma)
        {
            viewAlpha = alpha;
            viewBeta = beta;
            viewGamma = gamma;
            drawAxes();
        }

        // Shared by DrawViewParamsOverlay and the +/- nudge buttons positioned over it
        // (AddAngleNudgeControls), so both agree on exactly what text is shown. Back to
        // the real Greek glyphs + degree sign - the PDF export path (SvgGraphics.DrawString
        // / WriteVectorPdf) now handles them properly instead of mangling them to "?".
        private List<(string Text, Color Color)> GetViewParamLines()
        {
            return new List<(string Text, Color Color)>() { ($"{Strings.ChrW(0x3B1)} (Y-axis): {viewAlpha:0.0}{Strings.ChrW(0xB0)}", Color.MediumSeaGreen), ($"{Strings.ChrW(0x3B2)} (X-axis): {viewBeta:0.0}{Strings.ChrW(0xB0)}", Color.IndianRed), ($"{Strings.ChrW(0x3B3)} (Z-axis): {viewGamma:0.0}{Strings.ChrW(0xB0)}", Color.RoyalBlue) };
        }

        // Shows the current viewAlpha/Beta/Gamma (the same 3 numbers Set3DView takes, and the same
        // ones the View presets pick) in the top-left corner of the 3D view - handy for confirming
        // what a preset actually set, or for picking/verifying a custom orientation by eye. Each
        // line is colored to match the axis gizmo (Red=X, Green=Y, Blue=Z) so it's visible at a
        // glance which rotation feeds which axis without re-reading the "?" rotation hint: alpha
        // feeds RotateY, beta feeds RotateX, gamma feeds RotateZ.
        private void DrawViewParamsOverlay(SvgGraphics G)
        {
            var paramFont = GetTickFont(10f);
            var lines = GetViewParamLines();

            float lineHeight = G.MeasureString("M", paramFont).Height;
            float textWidth = 0f;
            foreach (var ln in lines)
                textWidth = Math.Max(textWidth, G.MeasureString(ln.Text, paramFont).Width);

            const float pad = 5f;
            float boxX = 6f;
            float boxY = 6f;
            float boxW = textWidth + pad * 2f;
            float boxH = lineHeight * lines.Count + pad * 2f;

            using (var bgBrush = new SolidBrush(Color.FromArgb(190, ThemeCanvasBackColor)))
            {
                G.FillRectangle(bgBrush, boxX, boxY, boxW, boxH);
            }
            using (var borderPen = new Pen(ThemeGridColor, 1f))
            {
                G.DrawRectangle(borderPen, boxX, boxY, boxW, boxH);
            }

            for (int i = 0, loopTo = lines.Count - 1; i <= loopTo; i++)
            {
                using (var lineBrush = new SolidBrush(lines[i].Color))
                {
                    G.DrawString(lines[i].Text, paramFont, lineBrush, boxX + pad, boxY + pad + i * lineHeight);
                }
            }
        }

        // Small clickable +/- labels placed to the right of DrawViewParamsOverlay's
        // readout box, one pair per angle, so each of alpha/beta/gamma can be nudged
        // individually without dragging the whole view. Labels rather than Buttons -
        // a WinForms Button's glyph gets squeezed/clipped by its built-in chrome
        // (border/padding) at this small a size, where a Label just draws the
        // character centered in the box with nothing eating into it. Real controls
        // placed on top of p3d (like AddRotationHintTo's "?" button), not part of
        // the SVG/PDF-captured render.
        private void AddAngleNudgeControls(PictureBox pb)
        {
            // A private Font, NOT GetTickFont(10): the tick-font cache disposes its
            // current Font whenever any caller asks for a different size (see
            // GetTickFont), and drawAxes does exactly that on every render - so a
            // GetTickFont reference captured here would be disposed out from under
            // the reposition closure, crashing MeasureString the next time p3d
            // resizes (e.g. when Show3D flips p3d.Dock to Fill).
            var paramFont = new Font("Consolas", 10f, FontStyle.Regular);
            const float pad = 5f;
            const float boxX = 6f;
            const float boxY = 6f;
            const float stepDeg = 5.0f;

            System.Windows.Forms.Label MakeLbl(string txt)
            {
                var lbl = new System.Windows.Forms.Label();
                lbl.Text = txt;
                lbl.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);
                lbl.Size = new Size(20, 20);
                lbl.BorderStyle = BorderStyle.FixedSingle;
                lbl.BackColor = Color.White;
                lbl.ForeColor = Color.Black;
                lbl.TextAlign = ContentAlignment.MiddleCenter;
                lbl.Margin = Padding.Empty;
                lbl.Padding = Padding.Empty;
                lbl.Cursor = Cursors.Hand;
                // Labels have no built-in hover/pressed chrome the way Button does,
                // so give them a manual hover tint as the only other affordance that
                // this is clickable.
                lbl.MouseEnter += (s, ev) => lbl.BackColor = Color.FromArgb(225, 235, 255);
                lbl.MouseLeave += (s, ev) => lbl.BackColor = Color.White;
                return lbl;
            };

            var mAlpha = MakeLbl("-");
            var pAlpha = MakeLbl("+");
            var mBeta = MakeLbl("-");
            var pBeta = MakeLbl("+");
            var mGamma = MakeLbl("-");
            var pGamma = MakeLbl("+");

            Action reposition = null;

            mAlpha.Click += (s, ev) =>
                {
                    Set3DView(viewAlpha - stepDeg, viewBeta, viewGamma);
                    reposition();
                };
            pAlpha.Click += (s, ev) =>
                {
                    Set3DView(viewAlpha + stepDeg, viewBeta, viewGamma);
                    reposition();
                };
            mBeta.Click += (s, ev) =>
                {
                    Set3DView(viewAlpha, viewBeta - stepDeg, viewGamma);
                    reposition();
                };
            pBeta.Click += (s, ev) =>
                {
                    Set3DView(viewAlpha, viewBeta + stepDeg, viewGamma);
                    reposition();
                };
            mGamma.Click += (s, ev) =>
                {
                    Set3DView(viewAlpha, viewBeta, viewGamma - stepDeg);
                    reposition();
                };
            pGamma.Click += (s, ev) =>
                {
                    Set3DView(viewAlpha, viewBeta, viewGamma + stepDeg);
                    reposition();
                };

            System.Windows.Forms.Label[] allLbls = new[] { mAlpha, pAlpha, mBeta, pBeta, mGamma, pGamma };
            pb.Controls.AddRange(allLbls);
            foreach (var lbl in allLbls)
                lbl.BringToFront();

            (System.Windows.Forms.Label Minus, System.Windows.Forms.Label Plus)[] rows = new[] { (mAlpha, pAlpha), (mBeta, pBeta), (mGamma, pGamma) };

            // Recomputes the same box-width math DrawViewParamsOverlay uses (via a
            // throwaway measuring Graphics rather than the SvgGraphics only available
            // mid-render) so the buttons sit just past the readout text regardless of
            // how wide the current angle values happen to be.
            reposition = new Action(() =>
                {
                    var lines = GetViewParamLines();
                    float lineHeight;
                    float textWidth = 0f;
                    // Measured off a throwaway Bitmap rather than pb.CreateGraphics() -
                    // this runs from pb's own Resize event, and toggling Show3D drives
                    // that Resize by flipping p3d.Dock to Fill, which can recreate the
                    // control's window handle mid-layout. CreateGraphics() during that
                    // window is a known crash (the Graphics can be torn down under it);
                    // a Bitmap-backed Graphics has no dependency on pb's handle at all.
                    using (var measureBmp = new Bitmap(1, 1))
                    {
                        using (var mg = Graphics.FromImage(measureBmp))
                        {
                            lineHeight = mg.MeasureString("M", paramFont).Height;
                            foreach (var ln in lines)
                                textWidth = Math.Max(textWidth, mg.MeasureString(ln.Text, paramFont).Width);
                        }
                    }
                    float boxW = textWidth + pad * 2f;
                    int btnLeft = (int)Math.Round(boxX + boxW + 6f);

                    for (int i = 0, loopTo = rows.Length - 1; i <= loopTo; i++)
                    {
                        int rowTop = (int)Math.Round(boxY + pad + i * lineHeight + (lineHeight - rows[i].Minus.Height) / 2.0f);
                        rows[i].Minus.Left = btnLeft;
                        rows[i].Minus.Top = rowTop;
                        rows[i].Plus.Left = btnLeft + rows[i].Minus.Width + 2;
                        rows[i].Plus.Top = rowTop;
                    }
                });
            pb.Resize += (s, ev) => reposition();
            reposition();
        }

        // Triggers a render that captures SVG/PDF vector data for export.
        // Waits for it to complete before returning.
        private async Task drawAxesCapture()
        {
            _captureVectors = true;
            _renderPending = false;
            if (_cts is not null)
                _cts.Cancel();
            try
            {
                await Task.Delay(50);  // let any in-flight render finish
            }
            catch
            {
            }
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            try
            {
                await Task.Run(() => HeavyRender(null, token));
            }
            catch
            {
            }
            finally
            {
                if (_cts is not null)
                    _cts.Dispose();
                _cts = null;
                _captureVectors = false;
            }
        }

        private async void ExportView(PictureBox pb, string format, string defaultName)
        {
            string svgContent = "";
            string pdfContent = "";

            if (ReferenceEquals(pb, pTrefftz))
            {
                if (format != "PNG")
                    RenderTrefftzPlot(true);
                svgContent = trefftzSvg;
                pdfContent = trefftzPdf;
            }
            else if (ReferenceEquals(pb, pLoads))
            {
                if (format != "PNG")
                    RenderLoadsPlot(true);
                svgContent = loadsSvg;
                pdfContent = loadsPdf;
            }
            else if (ReferenceEquals(pb, pPolar))
            {
                if (format != "PNG")
                    RenderPolarPlot(true);
                svgContent = polarSvg;
                pdfContent = polarPdf;
            }
            else if (ReferenceEquals(pb, pFE))
            {
                if (format != "PNG")
                    RenderFEPlot(true);
                svgContent = feSvg;
                pdfContent = fePdf;
            }
            else if (ReferenceEquals(pb, pModes))
            {
                if (format != "PNG")
                    RenderModesPlot(true);
                svgContent = modesSvg;
                pdfContent = modesPdf;
            }
            else
            {
                if (format != "PNG")
                {
                    await drawAxesCapture();
                }

                if (ReferenceEquals(pb, pxy))
                {
                    svgContent = pxySvg;
                    pdfContent = pxyPdf;
                }
                else if (ReferenceEquals(pb, pxz))
                {
                    svgContent = pxzSvg;
                    pdfContent = pxzPdf;
                }
                else if (ReferenceEquals(pb, pyz))
                {
                    svgContent = pyzSvg;
                    pdfContent = pyzPdf;
                }
                else if (ReferenceEquals(pb, p3d))
                {
                    svgContent = p3dSvg;
                    pdfContent = p3dPdf;
                }
            }

            if (format == "PNG")
            {
                if (pb.Image is null)
                {
                    AppMessageBox.Show("There is no image to export.", "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (string.IsNullOrEmpty(svgContent))
            {
                AppMessageBox.Show("The view has not finished rendering. Please wait a moment and try again.", "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var sfd = new SaveFileDialog();
            sfd.Title = $"Export {defaultName} as {format}";

            string projName = txtName.Text.Trim();
            if (string.IsNullOrEmpty(projName) || projName.Contains("Enter AVL Project") || projName.Contains("Enter NACA"))
            {
                sfd.FileName = defaultName;
            }
            else
            {
                foreach (var c in Path.GetInvalidFileNameChars())
                    projName = projName.Replace(c, '_');
                sfd.FileName = $"{projName}_{defaultName}";
            }

            switch (format ?? "")
            {
                case "PNG":
                    {
                        sfd.Filter = "PNG Image (*.png)|*.png";
                        sfd.DefaultExt = "png";
                        break;
                    }
                case "SVG":
                    {
                        sfd.Filter = "SVG Image (*.svg)|*.svg";
                        sfd.DefaultExt = "svg";
                        break;
                    }
                case "PDF":
                    {
                        sfd.Filter = "PDF Document (*.pdf)|*.pdf";
                        sfd.DefaultExt = "pdf";
                        break;
                    }
            }

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    switch (format ?? "")
                    {
                        case "PNG":
                            {
                                System.Drawing.Image imgCopy = null;
                                lock (pb)
                                {
                                    if (pb.Image is not null)
                                    {
                                        imgCopy = (System.Drawing.Image)pb.Image.Clone();
                                    }
                                }
                                if (imgCopy is not null)
                                {
                                    imgCopy.Save(sfd.FileName, ImageFormat.Png);
                                    imgCopy.Dispose();
                                }
                                else
                                {
                                    throw new Exception("Failed to clone picture box image.");
                                }

                                break;
                            }
                        case "SVG":
                            {
                                File.WriteAllText(sfd.FileName, svgContent, Encoding.UTF8);
                                break;
                            }
                        case "PDF":
                            {
                                WriteVectorPdf(pdfContent, pb.Width, pb.Height, sfd.FileName);
                                break;
                            }
                    }

                    AppToast.Show($"{format} exported to " + Path.GetFileName(sfd.FileName));
                }
                catch (Exception ex)
                {
                    AppMessageBox.Show("Error exporting file: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        internal static void WriteVectorPdf(string pdfContentStream, double width, double height, string outputPath)
        {
            double pdfWidth = width * 72.0d / 96.0d;
            double pdfHeight = height * 72.0d / 96.0d;

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                // Latin1 (not ASCII) so single-byte characters above 0x7F - like the
                // degree sign the 3D-view angle readout uses - survive as themselves
                // instead of being silently turned into "?" by ASCII's byte conversion.
                // Greek letters (also used there) are still out of Latin-1's range, so
                // those go through the separate Symbol-font substitution in
                // SvgGraphics.DrawString/SplitPdfTextRuns instead.
                using (var writer = new StreamWriter(fs, Encoding.Latin1))
                {
                    var offsets = new List<long>();

                    writer.Write("%PDF-1.4" + Constants.vbLf);

                    writer.Flush();
                    offsets.Add(fs.Position);
                    writer.Write("1 0 obj" + Constants.vbLf + "<< /Type /Catalog /Pages 2 0 R >>" + Constants.vbLf + "endobj" + Constants.vbLf);

                    writer.Flush();
                    offsets.Add(fs.Position);
                    writer.Write("2 0 obj" + Constants.vbLf + "<< /Type /Pages /Kids [ 3 0 R ] /Count 1 >>" + Constants.vbLf + "endobj" + Constants.vbLf);

                    writer.Flush();
                    offsets.Add(fs.Position);
                    string wStr = pdfWidth.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string hStr = pdfHeight.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    writer.Write("3 0 obj" + Constants.vbLf + $"<< /Type /Page /Parent 2 0 R /MediaBox [ 0 0 {wStr} {hStr} ] /Resources << /Font << /F1 5 0 R /F2 6 0 R /F3 7 0 R /F4 8 0 R /F5 9 0 R >> >> /Contents 4 0 R >>" + Constants.vbLf + "endobj" + Constants.vbLf);

                    byte[] contentBytes = Encoding.Latin1.GetBytes(pdfContentStream);
                    writer.Flush();
                    offsets.Add(fs.Position);
                    writer.Write("4 0 obj" + Constants.vbLf + $"<< /Length {contentBytes.Length} >>" + Constants.vbLf + "stream" + Constants.vbLf);
                    writer.Flush();
                    fs.Write(contentBytes, 0, contentBytes.Length);
                    writer.Write(Constants.vbLf + "endstream" + Constants.vbLf + "endobj" + Constants.vbLf);

                    writer.Flush();
                    offsets.Add(fs.Position);
                    // /Encoding /WinAnsiEncoding is required here, not optional: with no
                    // /Encoding entry a Type1 standard font falls back to its own built-in
                    // encoding (Adobe StandardEncoding), whose upper-128 layout does NOT
                    // put the degree sign at 0xB0 the way WinAnsi/Latin-1 (and this app's
                    // Encoding.Latin1 content-stream bytes) do - that mismatch, not the
                    // byte encoding, was why "deg" was still rendering wrong after the
                    // Encoding.Latin1 change.
                    writer.Write("5 0 obj" + Constants.vbLf + "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>" + Constants.vbLf + "endobj" + Constants.vbLf);

                    writer.Flush();
                    offsets.Add(fs.Position);
                    writer.Write("6 0 obj" + Constants.vbLf + "<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold /Encoding /WinAnsiEncoding >>" + Constants.vbLf + "endobj" + Constants.vbLf);

                    writer.Flush();
                    offsets.Add(fs.Position);
                    writer.Write("7 0 obj" + Constants.vbLf + "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>" + Constants.vbLf + "endobj" + Constants.vbLf);

                    writer.Flush();
                    offsets.Add(fs.Position);
                    writer.Write("8 0 obj" + Constants.vbLf + "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>" + Constants.vbLf + "endobj" + Constants.vbLf);

                    // Standard PDF font #14 - always available in a conforming viewer, no
                    // embedding needed. Deliberately no /Encoding entry: Symbol has its
                    // own fixed built-in encoding (imposing WinAnsi on it would break the
                    // very glyph mapping SplitPdfTextRuns relies on).
                    writer.Flush();
                    offsets.Add(fs.Position);
                    writer.Write("9 0 obj" + Constants.vbLf + "<< /Type /Font /Subtype /Type1 /BaseFont /Symbol >>" + Constants.vbLf + "endobj" + Constants.vbLf);

                    writer.Flush();
                    long startXref = fs.Position;
                    writer.Write("xref" + Constants.vbLf + "0 10" + Constants.vbLf + "0000000000 65535 f " + Constants.vbLf);
                    foreach (var offset in offsets)
                        writer.Write(offset.ToString("D10") + " 00000 n " + Constants.vbLf);
                    writer.Write("trailer" + Constants.vbLf + "<< /Size 10 /Root 1 0 R >>" + Constants.vbLf + "startxref" + Constants.vbLf + startXref + Constants.vbLf + "%%EOF" + Constants.vbLf);
                }
            }
        }

        // Enable double-buffering on Form controls recursively (using fully qualified System.Windows.Forms.Control)
        private void EnableDoubleBuffering(System.Windows.Forms.Control ctrl)
        {
            try
            {
                var dbProp = typeof(System.Windows.Forms.Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dbProp is not null)
                {
                    dbProp.SetValue(ctrl, true, (object[])null);
                }
            }
            catch
            {
            }

            foreach (System.Windows.Forms.Control child in ctrl.Controls)
                EnableDoubleBuffering(child);
        }
    }

    public class SvgGraphics : IDisposable
    {

        public int Width;
        public int Height;
        public Graphics g;

        // When False, all SVG/PDF string building is skipped (normal interactive renders).
        // Set to True only when exporting.
        public bool CaptureVectors = false;

        private StringBuilder sbSvg = null;
        private StringBuilder sbPdf = null;

        public SvgGraphics(int w, int h, Graphics realGraphics, bool captureVectors = false)
        {
            Width = w;
            Height = h;
            g = realGraphics;
            // Must be "Me.CaptureVectors" - VB is case-insensitive, so a bare
            // "CaptureVectors = captureVectors" here resolves BOTH sides to the
            // local parameter (self-assignment, a no-op) rather than the field,
            // since the parameter shadows the field for the rest of this
            // constructor. That silently left the field stuck at False forever,
            // so every method past the constructor (Clear/DrawLine/DrawString/...)
            // skipped vector emission entirely - only the constructor's own
            // initial white background rect ever made it into the SVG/PDF,
            // which is exactly the reported "blank white page" export.
            CaptureVectors = captureVectors;

            if (captureVectors)
            {
                sbSvg = new StringBuilder();
                sbPdf = new StringBuilder();
                sbSvg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"{w}\" height=\"{h}\" viewBox=\"0 0 {w} {h}\">");
                sbSvg.AppendLine($"  <rect width=\"{w}\" height=\"{h}\" fill=\"white\" />");
                sbPdf.AppendLine("q");
                // WriteVectorPdf sizes the page's MediaBox at 72/96 of the pixel
                // dimensions (converting 96-DPI pixels to 72-DPI PDF points), but
                // every content-stream coordinate below is still emitted in raw
                // pixel units. Without also scaling this initial transform by the
                // same 72/96 factor, content drawn out to the full pixel width/
                // height extends past the smaller page and gets clipped - that's
                // the reported "cropped" export.
                double pdfScale = 72.0d / 96.0d;
                sbPdf.AppendLine($"{pdfScale.ToString(System.Globalization.CultureInfo.InvariantCulture)} 0 0 {(-pdfScale).ToString(System.Globalization.CultureInfo.InvariantCulture)} 0 {(h * pdfScale).ToString(System.Globalization.CultureInfo.InvariantCulture)} cm");
                sbPdf.AppendLine("1 w");
                sbPdf.AppendLine("0 0 0 RG");
                sbPdf.AppendLine("0 0 0 rg");
            }
        }

        public void Clear(Color c)
        {
            if (g is not null)
                g.Clear(c);
            if (!CaptureVectors)
                return;
            string cHex = ColorToHex(c);
            sbSvg.AppendLine($"  <rect width=\"{Width}\" height=\"{Height}\" fill=\"{cHex}\" />");
            sbPdf.AppendLine($"{ColorToPdfColor(c)} rg");
            sbPdf.AppendLine($"0 0 {Width} {Height} re");
            sbPdf.AppendLine("f");
        }

        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            if (g is not null)
                g.DrawLine(pen, x1, y1, x2, y2);
            if (!CaptureVectors)
                return;
            string cHex = ColorToHex(pen.Color);
            string dash = GetSvgDashArray(pen);
            sbSvg.AppendLine($"  <line x1=\"{x1.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" y1=\"{y1.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" x2=\"{x2.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" y2=\"{y2.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" stroke=\"{cHex}\" stroke-width=\"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" {(string.IsNullOrEmpty(dash) ? "" : $"stroke-dasharray=\"{dash}\"")} />");
            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG");
            sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w");
            string pdfDash = GetPdfDashArray(pen);
            if (!string.IsNullOrEmpty(pdfDash))
                sbPdf.AppendLine(pdfDash);
            sbPdf.AppendLine($"{x1.ToString(System.Globalization.CultureInfo.InvariantCulture)} {y1.ToString(System.Globalization.CultureInfo.InvariantCulture)} m");
            sbPdf.AppendLine($"{x2.ToString(System.Globalization.CultureInfo.InvariantCulture)} {y2.ToString(System.Globalization.CultureInfo.InvariantCulture)} l");
            sbPdf.AppendLine("S");
            sbPdf.AppendLine("Q");
        }

        public void DrawLine(Pen pen, PointF p1, PointF p2)
        {
            DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y);
        }

        public void DrawEllipse(Pen pen, RectangleF rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawEllipse(Pen pen, Rectangle rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillEllipse(Brush brush, RectangleF rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillEllipse(Brush brush, Rectangle rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawRectangle(Pen pen, Rectangle rect)
        {
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawRectangle(Pen pen, RectangleF rect)
        {
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillRectangle(Brush brush, RectangleF rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawLines(Pen pen, PointF[] points)
        {
            if (g is not null)
                g.DrawLines(pen, points);
            if (!CaptureVectors)
                return;
            if (points.Length < 2)
                return;

            string ptsStr = PointsToString(points);
            string cHex = ColorToHex(pen.Color);
            string dash = GetSvgDashArray(pen);
            sbSvg.AppendLine($"  <polyline points=\"{ptsStr}\" fill=\"none\" stroke=\"{cHex}\" stroke-width=\"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" {(string.IsNullOrEmpty(dash) ? "" : $"stroke-dasharray=\"{dash}\"")} />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG");
            sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w");
            string pdfDash = GetPdfDashArray(pen);
            if (!string.IsNullOrEmpty(pdfDash))
                sbPdf.AppendLine(pdfDash);

            sbPdf.AppendLine($"{points[0].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[0].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} m");
            for (int i = 1, loopTo = points.Length - 1; i <= loopTo; i++)
                sbPdf.AppendLine($"{points[i].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[i].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} l");
            sbPdf.AppendLine("S");
            sbPdf.AppendLine("Q");
        }

        public void DrawPolygon(Pen pen, PointF[] points)
        {
            if (g is not null)
                g.DrawPolygon(pen, points);
            if (!CaptureVectors)
                return;
            if (points.Length < 2)
                return;

            string ptsStr = PointsToString(points);
            string cHex = ColorToHex(pen.Color);
            string dash = GetSvgDashArray(pen);
            sbSvg.AppendLine($"  <polygon points=\"{ptsStr}\" fill=\"none\" stroke=\"{cHex}\" stroke-width=\"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" {(string.IsNullOrEmpty(dash) ? "" : $"stroke-dasharray=\"{dash}\"")} />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG");
            sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w");
            string pdfDash = GetPdfDashArray(pen);
            if (!string.IsNullOrEmpty(pdfDash))
                sbPdf.AppendLine(pdfDash);

            sbPdf.AppendLine($"{points[0].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[0].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} m");
            for (int i = 1, loopTo = points.Length - 1; i <= loopTo; i++)
                sbPdf.AppendLine($"{points[i].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[i].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} l");
            sbPdf.AppendLine("h S");
            sbPdf.AppendLine("Q");
        }

        public void FillPolygon(Brush brush, PointF[] points)
        {
            if (g is not null)
                g.FillPolygon(brush, points);
            if (!CaptureVectors)
                return;
            if (points.Length < 2)
                return;

            string ptsStr = PointsToString(points);
            var color = GetBrushColor(brush);
            string cHex = ColorToHex(color);
            double opacity = color.A / 255.0d;
            sbSvg.AppendLine($"  <polygon points=\"{ptsStr}\" fill=\"{cHex}\" opacity=\"{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(color)} rg");
            sbPdf.AppendLine($"{points[0].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[0].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} m");
            for (int i = 1, loopTo = points.Length - 1; i <= loopTo; i++)
                sbPdf.AppendLine($"{points[i].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[i].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} l");
            sbPdf.AppendLine("f");
            sbPdf.AppendLine("Q");
        }

        public void DrawEllipse(Pen pen, float x, float y, float w, float h)
        {
            if (g is not null)
                g.DrawEllipse(pen, x, y, w, h);
            if (!CaptureVectors)
                return;
            float cx = x + w / 2f;
            float cy = y + h / 2f;
            float rx = w / 2f;
            float ry = h / 2f;
            string cHex = ColorToHex(pen.Color);
            sbSvg.AppendLine($"  <ellipse cx=\"{cx.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" cy=\"{cy.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" rx=\"{rx.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" ry=\"{ry.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" stroke=\"{cHex}\" stroke-width=\"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" fill=\"none\" />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG");
            sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w");
            AppendPdfEllipse(x, y, w, h, "S");
            sbPdf.AppendLine("Q");
        }

        public void FillEllipse(Brush brush, float x, float y, float w, float h)
        {
            if (g is not null)
                g.FillEllipse(brush, x, y, w, h);
            if (!CaptureVectors)
                return;
            float cx = x + w / 2f;
            float cy = y + h / 2f;
            float rx = w / 2f;
            float ry = h / 2f;
            var color = GetBrushColor(brush);
            string cHex = ColorToHex(color);
            double opacity = color.A / 255.0d;
            sbSvg.AppendLine($"  <ellipse cx=\"{cx.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" cy=\"{cy.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" rx=\"{rx.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" ry=\"{ry.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" fill=\"{cHex}\" opacity=\"{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(color)} rg");
            AppendPdfEllipse(x, y, w, h, "f");
            sbPdf.AppendLine("Q");
        }

        public void DrawRectangle(Pen pen, float x, float y, float w, float h)
        {
            if (g is not null)
                g.DrawRectangle(pen, x, y, w, h);
            if (!CaptureVectors)
                return;
            string cHex = ColorToHex(pen.Color);
            sbSvg.AppendLine($"  <rect x=\"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" y=\"{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" width=\"{w.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" height=\"{h.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" stroke=\"{cHex}\" stroke-width=\"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" fill=\"none\" />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG");
            sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w");
            sbPdf.AppendLine($"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)} {y.ToString(System.Globalization.CultureInfo.InvariantCulture)} {w.ToString(System.Globalization.CultureInfo.InvariantCulture)} {h.ToString(System.Globalization.CultureInfo.InvariantCulture)} re");
            sbPdf.AppendLine("S");
            sbPdf.AppendLine("Q");
        }

        public void FillRectangle(Brush brush, float x, float y, float w, float h)
        {
            if (g is not null)
                g.FillRectangle(brush, x, y, w, h);
            if (!CaptureVectors)
                return;
            var color = GetBrushColor(brush);
            string cHex = ColorToHex(color);
            double opacity = color.A / 255.0d;
            sbSvg.AppendLine($"  <rect x=\"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" y=\"{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" width=\"{w.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" height=\"{h.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" fill=\"{cHex}\" opacity=\"{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(color)} rg");
            sbPdf.AppendLine($"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)} {y.ToString(System.Globalization.CultureInfo.InvariantCulture)} {w.ToString(System.Globalization.CultureInfo.InvariantCulture)} {h.ToString(System.Globalization.CultureInfo.InvariantCulture)} re");
            sbPdf.AppendLine("f");
            sbPdf.AppendLine("Q");
        }

        public void FillRectangle(Brush brush, Rectangle rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawPath(Pen pen, GraphicsPath path)
        {
            if (g is not null)
                g.DrawPath(pen, path);
            if (!CaptureVectors)
                return;
            string d = PathDataToSvgD(path.PathData);
            string cHex = ColorToHex(pen.Color);
            sbSvg.AppendLine($"  <path d=\"{d}\" stroke=\"{cHex}\" stroke-width=\"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" fill=\"none\" {(string.IsNullOrEmpty(GetSvgDashArray(pen)) ? "" : $"stroke-dasharray=\"{GetSvgDashArray(pen)}\"")} />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG");
            sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w");
            string pdfDash = GetPdfDashArray(pen);
            if (!string.IsNullOrEmpty(pdfDash))
                sbPdf.AppendLine(pdfDash);
            AppendPdfPath(path.PathData);
            sbPdf.AppendLine("S");
            sbPdf.AppendLine("Q");
        }

        public void FillPath(Brush brush, GraphicsPath path)
        {
            if (g is not null)
                g.FillPath(brush, path);
            if (!CaptureVectors)
                return;
            string d = PathDataToSvgD(path.PathData);
            var color = GetBrushColor(brush);
            string cHex = ColorToHex(color);
            double opacity = color.A / 255.0d;
            sbSvg.AppendLine($"  <path d=\"{d}\" fill=\"{cHex}\" opacity=\"{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(color)} rg");
            AppendPdfPath(path.PathData);
            sbPdf.AppendLine("f");
            sbPdf.AppendLine("Q");
        }

        public void DrawString(string s, Font font, Brush brush, float x, float y)
        {
            if (g is not null)
                g.DrawString(s, font, brush, x, y);
            if (!CaptureVectors)
                return;
            var color = GetBrushColor(brush);
            string cHex = ColorToHex(color);
            double opacity = color.A / 255.0d;
            string escaped = EscapeXml(s);

            bool isBold = font.Bold;
            bool isItalic = font.Italic;
            string fontName = font.Name;

            sbSvg.AppendLine($"  <text x=\"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" y=\"{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" font-family=\"{fontName}\" font-size=\"{font.Size.ToString(System.Globalization.CultureInfo.InvariantCulture)}px\" {(isBold ? "font-weight=\"bold\"" : "")} {(isItalic ? "font-style=\"italic\"" : "")} fill=\"{cHex}\" opacity=\"{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" dominant-baseline=\"hanging\">{escaped}</text>");

            string pdfFontRef = "F1";
            if (fontName.Contains("Consolas") || fontName.Contains("Monospace") || fontName.Contains("Courier"))
            {
                pdfFontRef = isBold ? "F2" : "F1";
            }
            else
            {
                pdfFontRef = isBold ? "F4" : "F3";
            }

            // PDF text strings are written as a single-byte-per-char literal (see
            // WriteVectorPdf/Encoding.Latin1), so anything outside Latin-1 - like the
            // Greek alpha/beta/gamma this app's 3D-view angle readout uses - can't be
            // written directly. Adobe's standard "Symbol" font (always available in a
            // PDF viewer, no embedding needed) maps its own a/b/g/... codes to Greek
            // lowercase letters, so those characters are split into their own run and
            // rendered through /F5 (Symbol) instead, with everything else going
            // through the normal font unchanged. Run widths are measured with the
            // actual Unicode font (Segoe UI/Consolas both render Greek glyphs fine on
            // Windows) rather than Symbol's real metrics - close enough at this
            // overlay's small point size to keep runs visually contiguous.
            float baselineY = y + font.Size * 0.8f;
            float curX = x;
            sbPdf.AppendLine("BT");
            foreach (var run in SplitPdfTextRuns(s))
            {
                string runFontRef = run.IsSymbol ? "F5" : pdfFontRef;
                string runText = run.IsSymbol ? run.Text : EscapePdfString(run.Text);
                sbPdf.AppendLine($"/{runFontRef} {font.Size.ToString(System.Globalization.CultureInfo.InvariantCulture)} Tf");
                sbPdf.AppendLine($"{ColorToPdfColor(color)} rg");
                sbPdf.AppendLine($"1 0 0 -1 {curX.ToString(System.Globalization.CultureInfo.InvariantCulture)} {baselineY.ToString(System.Globalization.CultureInfo.InvariantCulture)} Tm");
                sbPdf.AppendLine($"({runText}) Tj");
                curX += MeasureString(run.OriginalText, font).Width;
            }
            sbPdf.AppendLine("ET");
        }

        // Adobe Symbol-font encoding: its own a-w code points render as these Greek
        // lowercase letters, independent of any WinAnsi/Latin-1 text encoding.
        private static readonly Dictionary<char, char> GreekToSymbolMap = new Dictionary<char, char>() { { Strings.ChrW(0x3B1), 'a' }, { Strings.ChrW(0x3B2), 'b' }, { Strings.ChrW(0x3B3), 'g' }, { Strings.ChrW(0x3B4), 'd' }, { Strings.ChrW(0x3B5), 'e' }, { Strings.ChrW(0x3B6), 'z' }, { Strings.ChrW(0x3B7), 'h' }, { Strings.ChrW(0x3B8), 'q' }, { Strings.ChrW(0x3B9), 'i' }, { Strings.ChrW(0x3BA), 'k' }, { Strings.ChrW(0x3BB), 'l' }, { Strings.ChrW(0x3BC), 'm' }, { Strings.ChrW(0x3BD), 'n' }, { Strings.ChrW(0x3BE), 'x' }, { Strings.ChrW(0x3BF), 'o' }, { Strings.ChrW(0x3C0), 'p' }, { Strings.ChrW(0x3C1), 'r' }, { Strings.ChrW(0x3C3), 's' }, { Strings.ChrW(0x3C4), 't' }, { Strings.ChrW(0x3C5), 'u' }, { Strings.ChrW(0x3C6), 'f' }, { Strings.ChrW(0x3C7), 'c' }, { Strings.ChrW(0x3C8), 'y' }, { Strings.ChrW(0x3C9), 'w' } };

        // Splits a string into alternating runs of "ordinary" text and single Greek
        // letters that need the Symbol-font substitution (see DrawString/GreekToSymbolMap).
        // OriginalText is kept per-run purely for width measurement (with the real font);
        // Text is what actually gets written into the PDF content stream for that run.
        private List<(string OriginalText, string Text, bool IsSymbol)> SplitPdfTextRuns(string s)
        {
            var runs = new List<(string OriginalText, string Text, bool IsSymbol)>();
            var plain = new StringBuilder();
            foreach (var c in s)
            {
                if (GreekToSymbolMap.ContainsKey(c))
                {
                    if (plain.Length > 0)
                    {
                        runs.Add((plain.ToString(), plain.ToString(), false));
                        plain.Clear();
                    }
                    runs.Add((c.ToString(), GreekToSymbolMap[c].ToString(), true));
                }
                else
                {
                    plain.Append(c);
                }
            }
            if (plain.Length > 0)
            {
                runs.Add((plain.ToString(), plain.ToString(), false));
            }
            return runs;
        }

        public void DrawString(string s, Font font, Brush brush, PointF pt)
        {
            DrawString(s, font, brush, pt.X, pt.Y);
        }

        public SizeF MeasureString(string text, Font font)
        {
            if (g is not null)
            {
                return g.MeasureString(text, font);
            }
            else
            {
                using (var tempBmp = new Bitmap(1, 1))
                {
                    using (var tempG = Graphics.FromImage(tempBmp))
                    {
                        return tempG.MeasureString(text, font);
                    }
                }
            }
        }

        public SmoothingMode SmoothingMode
        {
            get
            {
                if (g is not null)
                    return g.SmoothingMode;
                return SmoothingMode.Default;
            }
            set
            {
                if (g is not null)
                    g.SmoothingMode = value;
            }
        }

        public TextRenderingHint TextRenderingHint
        {
            get
            {
                if (g is not null)
                    return g.TextRenderingHint;
                return TextRenderingHint.SystemDefault;
            }
            set
            {
                if (g is not null)
                    g.TextRenderingHint = value;
            }
        }

        public PixelOffsetMode PixelOffsetMode
        {
            get
            {
                if (g is not null)
                    return g.PixelOffsetMode;
                return PixelOffsetMode.Default;
            }
            set
            {
                if (g is not null)
                    g.PixelOffsetMode = value;
            }
        }

        public float DpiX
        {
            get
            {
                if (g is not null)
                    return g.DpiX;
                return 96.0f;
            }
        }

        public string GetSvgContent()
        {
            if (sbSvg is null)
                return "";
            return sbSvg.ToString() + "</svg>";
        }

        public string GetPdfContentStream()
        {
            if (sbPdf is null)
                return "";
            return sbPdf.ToString() + "Q" + Constants.vbLf;
        }

        private string ColorToHex(Color c)
        {
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }

        private string ColorToPdfColor(Color c)
        {
            double r = c.R / 255.0d;
            double g = c.G / 255.0d;
            double b = c.B / 255.0d;
            return $"{r.ToString(System.Globalization.CultureInfo.InvariantCulture)} {g.ToString(System.Globalization.CultureInfo.InvariantCulture)} {b.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }

        private Color GetBrushColor(Brush brush)
        {
            if (brush is SolidBrush)
            {
                return ((SolidBrush)brush).Color;
            }
            return Color.Black;
        }

        private string GetSvgDashArray(Pen pen)
        {
            if (pen.DashStyle == DashStyle.Dash)
            {
                return "4,4";
            }
            else if (pen.DashStyle == DashStyle.Dot)
            {
                return "1,3";
            }
            else if (pen.DashStyle == DashStyle.DashDot)
            {
                return "4,3,1,3";
            }
            return "";
        }

        private string GetPdfDashArray(Pen pen)
        {
            if (pen.DashStyle == DashStyle.Dash)
            {
                return "[4 4] 0 d";
            }
            else if (pen.DashStyle == DashStyle.Dot)
            {
                return "[1 3] 0 d";
            }
            else if (pen.DashStyle == DashStyle.DashDot)
            {
                return "[4 3 1 3] 0 d";
            }
            return "";
        }

        private string PointsToString(PointF[] points)
        {
            var sbPoints = new StringBuilder();
            foreach (var pt in points)
                sbPoints.Append($"{pt.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{pt.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} ");
            return sbPoints.ToString().Trim();
        }

        private string PathDataToSvgD(PathData pathData)
        {
            var sbD = new StringBuilder();
            PointF[] pts = pathData.Points;
            byte[] types = pathData.Types;

            int i = 0;
            while (i < pts.Length)
            {
                byte @type = types[i];
                var pt = pts[i];
                string xStr = pt.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string yStr = pt.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);

                if ((type & 0x7) == 0)
                {
                    sbD.Append($"M {xStr} {yStr} ");
                    i += 1;
                }
                else if ((type & 0x7) == 1)
                {
                    sbD.Append($"L {xStr} {yStr} ");
                    if ((type & 0x80) == 0x80)
                        sbD.Append("Z ");
                    i += 1;
                }
                else if ((type & 0x7) == 3)
                {
                    if (i + 2 < pts.Length)
                    {
                        var cp1 = pts[i];
                        var cp2 = pts[i + 1];
                        var ep = pts[i + 2];
                        string cp1x = cp1.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string cp1y = cp1.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string cp2x = cp2.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string cp2y = cp2.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string epx = ep.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string epy = ep.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);

                        sbD.Append($"C {cp1x} {cp1y}, {cp2x} {cp2y}, {epx} {epy} ");
                        byte epType = types[i + 2];
                        if ((epType & 0x80) == 0x80)
                            sbD.Append("Z ");
                        i += 3;
                    }
                    else
                    {
                        i += 1;
                    }
                }
                else
                {
                    i += 1;
                }
            }
            return sbD.ToString().Trim();
        }

        private void AppendPdfPath(PathData pathData)
        {
            PointF[] pts = pathData.Points;
            byte[] types = pathData.Types;

            int i = 0;
            while (i < pts.Length)
            {
                byte @type = types[i];
                var pt = pts[i];
                string xStr = pt.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string yStr = pt.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);

                if ((type & 0x7) == 0)
                {
                    sbPdf.AppendLine($"{xStr} {yStr} m");
                    i += 1;
                }
                else if ((type & 0x7) == 1)
                {
                    sbPdf.AppendLine($"{xStr} {yStr} l");
                    if ((type & 0x80) == 0x80)
                        sbPdf.AppendLine("h");
                    i += 1;
                }
                else if ((type & 0x7) == 3)
                {
                    if (i + 2 < pts.Length)
                    {
                        var cp1 = pts[i];
                        var cp2 = pts[i + 1];
                        var ep = pts[i + 2];
                        string cp1x = cp1.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string cp1y = cp1.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string cp2x = cp2.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string cp2y = cp2.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string epx = ep.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string epy = ep.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);

                        sbPdf.AppendLine($"{cp1x} {cp1y} {cp2x} {cp2y} {epx} {epy} c");
                        byte epType = types[i + 2];
                        if ((epType & 0x80) == 0x80)
                            sbPdf.AppendLine("h");
                        i += 3;
                    }
                    else
                    {
                        i += 1;
                    }
                }
                else
                {
                    i += 1;
                }
            }
        }

        private void AppendPdfEllipse(float x, float y, float w, float h, string op)
        {
            float cx = x + w / 2f;
            float cy = y + h / 2f;
            float rx = w / 2f;
            float ry = h / 2f;

            double kappa = 0.55228474983079345d;
            double ox = (double)rx * kappa;
            double oy = (double)ry * kappa;

            string cxStr = cx.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cyStr = cy.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string rxStr = rx.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string ryStr = ry.ToString(System.Globalization.CultureInfo.InvariantCulture);

            string xM = (cx - rx).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string xP = (cx + rx).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string yM = (cy - ry).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string yP = (cy + ry).ToString(System.Globalization.CultureInfo.InvariantCulture);

            string cpXM_ox = ((double)(cx - rx) + ox).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cpXP_ox = ((double)(cx + rx) - ox).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cpYM_oy = ((double)(cy - ry) + oy).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cpYP_oy = ((double)(cy + ry) - oy).ToString(System.Globalization.CultureInfo.InvariantCulture);

            string cpCX_ox = ((double)cx - ox).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cpCX_pox = ((double)cx + ox).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cpCY_oy = ((double)cy - oy).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cpCY_poy = ((double)cy + oy).ToString(System.Globalization.CultureInfo.InvariantCulture);

            sbPdf.AppendLine($"{xM} {cyStr} m");
            sbPdf.AppendLine($"{xM} {cpCY_oy} {cpCX_ox} {yM} {cxStr} {yM} c");
            sbPdf.AppendLine($"{cpCX_pox} {yM} {xP} {cpCY_oy} {xP} {cyStr} c");
            sbPdf.AppendLine($"{xP} {cpCY_poy} {cpCX_pox} {yP} {cxStr} {yP} c");
            sbPdf.AppendLine($"{cpCX_ox} {yP} {xM} {cpCY_poy} {xM} {cyStr} c");
            sbPdf.AppendLine($"h {op}");
        }

        private string EscapeXml(string s)
        {
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        private string EscapePdfString(string s)
        {
            return s.Replace(@"\", @"\\").Replace("(", @"\(").Replace(")", @"\)");
        }

        public void Dispose()
        {
            if (g is not null)
            {
                g.Dispose();
            }
        }
    }


    // Static-stability quantities parsed from AVL's "ST" (stability-axis
    // derivatives) output, used to build the plain-language summary above the
    // raw Derivatives text.
    public class DerivativesInsight
    {
        public bool HasData = false;
        public double Cma;
        public double Clb;
        public double Cnb;
        public double CYb;
        public double Xnp;
        public double Xref;
        public double Cref;
        public bool HasSpiralRatio = false;
        public double SpiralRatio;
    }

    // A Surface/Section/Control text block in the .avl file, delimited by this
    // app's own !beginX/!endX markers. Used by the structure tree to navigate,
    // reorder (cut/paste line ranges), and add/delete blocks.
    public class GeomBlock
    {
        public string Kind;   // "Surface" / "Section" / "Control"
        public string Label;
        public int StartLine;   // inclusive
        public int EndLine;     // inclusive - the !endX line
        public int DataLine = -1;   // Section/Control's own data row - matches Node.lineNumber/controlLineNumber
        public List<GeomBlock> Children = new List<GeomBlock>();
    }

    public class Node
    {
        public Point3D Point;
        public string Surface;
        public float X;
        public float Y;
        public float Z;
        public bool Hovered;
        public int lineNumber;
        public NodeType @type;
        public float mass = 0f;
        public bool Visible = true;
        public bool IsDuplicate = false;
        // SubType encodes what this node controls in the file
        public NodeSubType SubType = NodeSubType.None;
        // Extra metadata used by CommitNodeDrag for TrailingEdge / ControlHinge nodes
        public float parentXle = 0f;
        public float parentChord = 0f;
        public int controlLineNumber = -1;   // line index of the CONTROL data row
        public float originalXhinge = 0f;        // sign-convention reference for Xhinge

        public enum NodeSubType
        {
            None = 0,
            LeadingEdge = 1,
            TrailingEdge = 2,
            ControlHinge = 3
        }

        public bool IsDraggable
        {
            get
            {
                if (@type == NodeType.Mass)
                    return true;
                if (@type == NodeType.Geometry && !IsDuplicate)
                {
                    return SubType == NodeSubType.LeadingEdge || SubType == NodeSubType.TrailingEdge || SubType == NodeSubType.ControlHinge;
                }
                return false;
            }
        }

        public enum NodeType
        {
            Geometry = 0,
            Mass = 1
        }

        public Node()
        {
            Hovered = false;
            lineNumber = 0;
        }

        public Node(Point3D p, string surfacename, bool hovered, int linenumber, NodeType nodetype, float mass = 0f)
        {
            Point = p;
            X = (float)p.X;
            Y = (float)p.Y;
            Z = (float)p.Z;
            Surface = surfacename;
            Hovered = hovered;
            lineNumber = linenumber;
            @type = nodetype;
            this.mass = mass;
        }

        public Node(double x, double y, double z, string surfacename, bool hovered, int linenumber, NodeType nodetype, float mass = 0f)
        {
            Point = new Point3D(x, y, z);
            X = (float)x;
            Y = (float)y;
            Z = (float)z;
            Surface = surfacename;
            Hovered = hovered;
            lineNumber = linenumber;
            @type = nodetype;
            this.mass = mass;
        }

        public Node RotateZ(float angle)
        {
            float rad = (float)((double)angle * Math.PI / 180d);
            float cosa = (float)Math.Cos((double)rad);
            float sina = (float)Math.Sin((double)rad);
            float Xn = X * cosa - Y * sina;
            float Yn = X * sina + Y * cosa;
            return new Node(new Point3D((double)Xn, (double)Yn, Z), Surface, Hovered, lineNumber, @type, mass);
        }

        public Node RotateX(float angle)
        {
            float rad = (float)((double)angle * Math.PI / 180d);
            float cosa = (float)Math.Cos((double)rad);
            float sina = (float)Math.Sin((double)rad);
            float yn = Y * cosa - Z * sina;
            float zn = Y * sina + Z * cosa;
            return new Node(new Point3D(X, (double)yn, (double)zn), Surface, Hovered, lineNumber, @type, mass);
        }

        public Node RotateY(float angle)
        {
            float rad = (float)((double)angle * Math.PI / 180d);
            float cosa = (float)Math.Cos((double)rad);
            float sina = (float)Math.Sin((double)rad);
            float Zn = Z * cosa - X * sina;
            float Xn = Z * sina + X * cosa;
            return new Node(new Point3D((double)Xn, Y, (double)Zn), Surface, Hovered, lineNumber, @type, mass);
        }

        public Node Project(float viewWidth, float viewHeight, float viewDistance, float fov = 500f)
        {
            // 1. Check if point is behind or too close to camera (Near Plane Clipping)
            // We use 1.0 as a safety buffer.
            if (viewDistance + Z < 0f)
            {
                // It is behind the camera. Mark as invisible.
                var n = new Node(new Point3D(0d, 0d, Z), Surface, Hovered, lineNumber, @type, mass);
                n.Visible = false;
                return n;
            }

            // 2. Safe Projection
            float scale = fov / (viewDistance + Z);
            float px = X * scale + viewWidth / 2f;
            float py = Y * scale + viewHeight / 2f;

            return new Node(new Point3D((double)px, (double)py, Z), Surface, Hovered, lineNumber, @type, mass);
        }
    }

    // One row of AVL's "FS" (strip forces) output table.
    public class TrefftzStrip
    {
        public double Yle;
        public double Chord;
        public double Area;
        public double CCl;       // "c cl" = chord * local cl -> spanwise loading when divided by Cref
        public double Ai;        // induced angle of attack, radians
        public double ClNorm;
        public double Cl;
        public double Cd;        // local induced-drag coefficient
        public double Cdv;
        public double CmC4;
        public double CmLE;
        public double Cpxc;
    }

    // One "Surface # n ..." block of AVL's FS output, with its strips in span order.
    public class TrefftzSurface
    {
        public string Name;
        public List<TrefftzStrip> Strips = new List<TrefftzStrip>();
    }

    // Totals block AVL prints after "X" (execute) - "Vortex Lattice Output -- Total
    // Forces" - matching what AVL's own native Trefftz Plane window displays as text.
    public class TrefftzTotals
    {
        public bool Valid = false;
        public string ConfigName = "";
        public string RunCaseName = "";
        public double Alpha;
        public double Beta;
        public double Mach;
        public double CLtot;
        public double CDtot;
        public double CDvis;
        public double CDff;
        public double CYff;
        public double SpanEff;
        public double ClPrimeTot;
        public double Cmtot;
        public double CnPrimeTot;
    }

    // One row of AVL's "VM" (strip shear/moment) output table.
    public class VmStrip
    {
        public double Y2Bref;    // 2Y/Bref, normalized span position (-1 to 1)
        public double Vz;        // Vz/(q*Sref), spanwise shear
        public double Mx;        // Mx/(q*Bref*Sref), spanwise bending moment
    }

    public class VmSurface
    {
        public string Name;
        public List<VmStrip> Points = new List<VmStrip>();
    }

    // One point of a drag-polar alpha sweep (reuses ParseTotalForces per point).
    public class PolarPoint
    {
        public double Alpha;
        public double CL;
        public double CD;
        public double Cm;
    }

    // One chordwise vortex-panel row of AVL's "FE" (element forces) output.
    public class FePanel
    {
        public double X;
        public double DCp;
    }

    // One spanwise strip's chordwise dCp distribution from "FE".
    public class FeStrip
    {
        public string SurfaceName;
        public int StripIndex;
        public double Yle;
        public double Cl;
        public double Cd;
        public List<FePanel> Panels = new List<FePanel>();
    }

    // One eigenvalue (a complex root) from AVL's ".MODE" eigenmode analysis, plus
    // the eigenvector magnitudes (from the console transcript) used to classify it
    // as a named flight-dynamics mode (phugoid, dutch roll, etc).
    public class EigenValue
    {
        public int RunCase;
        public double Real;
        public double Imag;
        public double MagU;
        public double MagV;
        public double MagW;
        public double MagP;
        public double MagQ;
        public double MagR;
        public double MagPhi;
        public double MagTheta;
        public double MagPsi;
        public string ModeLabel = "";
        // Set by DrawModesSubplot for the currently rendered plot, used for hover
        // hit-testing in pModes_MouseMove - not persisted/exported.
        public float ScreenX;
        public float ScreenY;
    }

    internal class EllipseStyle : Style
    {
        private Color lineColor = Color.Red;
        private int linewidth = 1;
        public EllipseStyle()
        {
        }
        public EllipseStyle(Color color)
        {
            lineColor = color;
        }
        public EllipseStyle(Color color, int width)
        {
            lineColor = color;
            linewidth = width;
        }
        public EllipseStyle(int width)
        {
            linewidth = width;
        }
        public override void Draw(Graphics gr, Point position, FastColoredTextBoxNS.Range range)
        {
            var size = GetSizeOfRange(range);
            var rect = new Rectangle(position, size);
            rect.Inflate(1, 0);
            var path = GetRoundedRectangle(rect, 7);
            gr.DrawPath(new Pen(lineColor, linewidth), path);
        }
    }


    public class ModernFastColoredTextBox : FastColoredTextBox
    {

        // DPI scale factor
        private float _dpiScale = 1.0f;

        public ModernFastColoredTextBox() : base()
        {

            // Calculate DPI scale
            using (var g = CreateGraphics())
            {
                _dpiScale = g.DpiX / 96.0f;
            }

            // Modern font scaled by DPI
            Font = new Font("Consolas", 10f * _dpiScale);
            ForeColor = Color.Black;
            BackColor = Color.White;
            ShowLineNumbers = true;

            // Smooth rendering
            DoubleBuffered = true;

        }

        // Paint override for ClearType + anti-alias
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            base.OnPaint(e);
        }

    }
}