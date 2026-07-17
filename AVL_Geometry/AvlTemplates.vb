Option Explicit On
Option Strict On

''' <summary>
''' Starter templates for the .avl/.mass/.run editor's "Add Template or Element" menu, hardcoded here
''' instead of extracted from Resources\appdata.zip at runtime (that zip previously bundled
''' template_avl.txt/template_surface.txt/etc. alongside the actual avl.exe/xfoil.exe binaries -
''' keeping editable starter text inside a binary zip resource made it awkward to review or change,
''' and pulled it through an unnecessary file-extraction step for content that never needs to touch
''' disk). Field descriptions are taken directly from AVL's own bundled documentation
''' (appdata/avl_doc.txt) rather than paraphrased from memory, and every field that AVL treats as
''' optional (defaults applied if omitted) is explicitly marked "[optional]".
'''
''' Each template comes in two versions - "Full" (with explanatory comments, for someone still
''' learning the format) and "Minimal" (just the field names and data, matching AVL's own generated
''' file style, for someone who already knows the format and doesn't want the extra reading). The Add
''' menu on the editor lets the user pick either one for each template.
''' </summary>
Public Module AvlTemplates

    Public ReadOnly AvlTemplateFull As String =
"[PlaneName]
#--------------------------------------------------------------------
# Mach number - default freestream Mach for the Prandtl-Glauert compressibility correction
#Mach
0.0
# IYsym  IZsym  Zsym - symmetry flags and the Z=Zsym mirror-plane location.
#   IYsym/IZsym MUST be written as whole numbers (0, 1, or -1) - not 0.0/1.0 - AVL reads them as integers.
#   IYsym:  1 = symmetric about Y=0 (solid wall)   -1 = antisymmetric about Y=0   0 = no Y-symmetry
#   IZsym:  1 = symmetric about Z=Zsym (e.g. ground effect, Zsym = ground height)   -1 = antisymmetric   0 = no Z-symmetry
#   Only the half (non-image) geometry needs to be modeled when symmetry is used.
#IYsym   IZsym   Zsym
0        0       0.0
# Sref  Cref  Bref - reference area/chord/span used to define CL, CD, Cm, Cl, Cn. Should be positive.
#Sref    Cref    Bref
9.0      0.9     10.0
# Xref  Yref  Zref - default moment/rotation-rate reference point (must be the CG location if trimming)
#Xref    Yref    Zref
0.0      0.0     0.0
# CDp [optional] - default profile drag coefficient added at Xref,Yref,Zref. Omit this whole line
# entirely (not just set to 0) if you don't need it - AVL assumes 0 when the line is absent.
!begingeometry

!endgeometry
"

    Public ReadOnly AvlTemplateMinimal As String =
"[PlaneName]
#Mach
0.0
#IYsym   IZsym   Zsym
0        0       0.0
#Sref    Cref    Bref
9.0      0.9     10.0
#Xref    Yref    Zref
0.0      0.0     0.0
!begingeometry

!endgeometry
"

    Public ReadOnly SurfaceTemplateFull As String =
"#====================================================================
SURFACE
[SurfaceName]
!beginsurface
# Nchord  Cspace  [Nspan  Sspace]  - Nspan/Sspace are OPTIONAL here; if you leave them
# off this line, give them per-SECTION instead. Cspace/Sspace (spacing parameters, described in the
# Prettify/? help) must be between -3.0 and +3.0: 1=cosine (most efficient), 0 or 3=equal, -2=-sine.
#Nchord  Cspace   Nspan   Sspace
8            1.0       12         1.0
#
# YDUPLICATE [optional] - mirrors this surface about Y=Ydupl to create the other half automatically.
# Only valid when the file's IYsym = 0 - combining it with IYsym<>0 will almost certainly crash AVL.
YDUPLICATE
0.0
#
# ANGLE [optional] - adds an incidence offset to every SECTION's Ainc in this surface at once.
ANGLE
0.0
# Other optional surface-level keywords not included here: SCALE, TRANSLATE, COMPONENT/INDEX,
# NOWAKE, NOALBE, NOLOAD, CDCL - see the '?' help button for details on each.
!endsurface
"

    Public ReadOnly SurfaceTemplateMinimal As String =
"#====================================================================
SURFACE
[SurfaceName]
!beginsurface
#Nchord  Cspace   Nspan   Sspace
8            1.0       12         1.0
#
YDUPLICATE
0.0
#
ANGLE
0.0
!endsurface
"

    Public ReadOnly SectionTemplateFull As String =
"#-------------------------------------------------------------
SECTION
!beginsection
# Xle Yle Zle Chord Ainc [Nspan Sspace] - the last 2 are OPTIONAL: only used if the parent
# SURFACE's own Nchord/Cspace line didn't already set them. Chord should be positive - AVL
# requires at least 2 SECTIONs per surface (e.g. root and tip); chord/incidence are interpolated
# linearly between them.
#Xle    Yle    Zle     Chord   Ainc  Nspan  Sspace
0.      0.     0.      0.2     0.0   0          0
# Camber line shape - choose ONE of NACA / AIRFOIL / AFILE (defaults to a flat plate if all omitted).
# NACA takes a 4- or 5-digit code; AIRFOIL takes inline x/c,y/c coordinate pairs; AFILE reads
# coordinates from an external XFOIL-format file (quote the filename if it has spaces).
NACA
2312
# Other optional keywords available here (not included in this template): CONTROL (deflectable
# hinge - add via the Control template), CLAF, CDCL, DESIGN - see the '?' help button.
!endsection
"

    Public ReadOnly SectionTemplateMinimal As String =
"#-------------------------------------------------------------
SECTION
!beginsection
#Xle    Yle    Zle     Chord   Ainc  Nspan  Sspace
0.      0.     0.      0.2     0.0   0          0
NACA
2312
!endsection
"

    Public ReadOnly ControlTemplateFull As String =
"#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
CONTROL
!begincontrol
#   Cname:   name of the control variable - reuse the same name on multiple sections/surfaces to
#            link their deflections together (e.g. flap-to-elevator mixing).
#   Cgain:   degrees of surface deflection per unit of the control variable.
#   Xhinge:  x/c hinge location. Positive = trailing-edge surface (Xhinge..1). Negative = leading-edge
#            surface (0..-Xhinge).
#   XYZhvec: axis the surface rotates about. Setting all three to 0 puts the hinge vector along the
#            hinge line itself (the usual choice).
#   SgnDup:  sign of the deflection on the mirrored (YDUPLICATE) surface - a symmetric control like an
#            elevator uses +1, an antisymmetric one like an aileron uses -1.
#Cname   Cgain  Xhinge  XYZhvec      SgnDup
flap     1.0    0.75    0.0 0.0 0.0   1.0
!endcontrol
"

    Public ReadOnly ControlTemplateMinimal As String =
"#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
CONTROL
!begincontrol
#Cname   Cgain  Xhinge  XYZhvec      SgnDup
flap     1.0    0.75    0.0 0.0 0.0   1.0
!endcontrol
"

    Public ReadOnly MassTemplateFull As String =
"#[PlaneName]
!x,y,z coordinate system matches AVL default
# Lunit/Munit/Tunit [optional] - unit conversion factors used for run-case setup and eigenmode
# calculations; omit any of the 3 lines to default that unit's magnitude to 1.0.
Lunit = 1.0 m
Munit = 1.0 kg
Tunit = 1.0 s
#-------------------------
!Gravity and density to be used as default values in trim setup (saves runtime typing).
!Must be in the unit names given above (i.e. m, kg, s).
# g / rho [optional] - default to 1.0 each if these lines are omitted.
g = 9.81
rho = 0.163      !at xkm above sea level, value matches value from density calc.
#-------------------------
!Mass & Inertia breakdown.
!x y z is the location of the item's own CG.
!x,y,z system here must be exactly the same one used in the .avl input file
!(same orientation, same origin location, same length units)
!
! Only mass, x, y, z are REQUIRED on each row below. Ixx, Iyy, Izz, Ixy, Ixz, Iyz are all
! individually OPTIONAL and default to 0 if left off the end of a row.
!
! A line starting with '*' sets multipliers, and '+' sets added constants, applied to every data
! row below it until the next '*'/'+' line - both are optional; omit entirely for multiplier=1, adder=0.

#  mass   x       y      z      Ixx    Iyy    Izz
   1      0       0      0      0      0      0   !1st description
"

    Public ReadOnly MassTemplateMinimal As String =
"#[PlaneName]
Lunit = 1.0 m
Munit = 1.0 kg
Tunit = 1.0 s
g = 9.81
rho = 0.163

#mass   x       y      z      Ixx    Iyy    Izz
1       0       0      0      0      0      0   !1st description
"

    Public ReadOnly RunTemplateFull As String =
" ---------------------------------------------
 Run case  1:  [RunName]

! Each line below constrains one flight variable (left) to a target quantity/value (right of '->').
! A constraint (the name right after '->') can only be used once per run case - e.g. two different
! variables can't both target CL in the same case.
 alpha        ->  CL          =   0.00000
 beta         ->  beta        =   0.00000
 pb/2V        ->  pb/2V       =   0.00000
 qc/2V        ->  qc/2V       =   0.00000
 rb/2V        ->  rb/2V       =   0.00000
 aileron      ->  Cl roll mom =   0.00000    !note that if you don't have control surfaces in your .avl file, this will be ignored
 elevator     ->  Cm pitchmom =   0.00000    !note that if you don't have control surfaces in your .avl file, this will be ignored
 rudder       ->  Cn yaw  mom =   0.00000    !note that if you don't have control surfaces in your .avl file, this will be ignored

! The parameters below are plain values, not constraints - AVL recomputes most of them after a run.
 alpha     =   0.00000     deg
 beta      =   0.00000     deg
 pb/2V     =   0.00000
 qc/2V     =   0.00000
 rb/2V     =   0.00000
 CL        =   0.00000
 CDo       =   0.00000
 bank      =   0.00000     deg
 elevation =   0.00000     deg
 heading   =   0.00000     deg
 Mach      =   0.00000
 velocity  =   0.00000     Lunit/Tunit
 density   =   0.00000     Munit/Lunit^3
 grav.acc. =   9.81000     Lunit/Tunit^2
 turn_rad. =   0.00000     Lunit
 load_fac. =   1.00000
 X_cg      =   0.00000     Lunit
 Y_cg      =   0.00000     Lunit
 Z_cg      =   0.00000     Lunit
 mass      =   1.00000     Munit
 Ixx       =   1.00000     Munit-Lunit^2
 Iyy       =   1.00000     Munit-Lunit^2
 Izz       =   1.00000     Munit-Lunit^2
 Ixy       =   0.00000     Munit-Lunit^2
 Iyz       =   0.00000     Munit-Lunit^2
 Izx       =   0.00000     Munit-Lunit^2
 visc CL_a =   0.00000
 visc CL_u =   0.00000
 visc CM_a =   0.00000
 visc CM_u =   0.00000
"

    Public ReadOnly RunTemplateMinimal As String =
" ---------------------------------------------
 Run case  1:  [RunName]

 alpha        ->  CL          =   0.00000
 beta         ->  beta        =   0.00000
 pb/2V        ->  pb/2V       =   0.00000
 qc/2V        ->  qc/2V       =   0.00000
 rb/2V        ->  rb/2V       =   0.00000
 aileron      ->  Cl roll mom =   0.00000
 elevator     ->  Cm pitchmom =   0.00000
 rudder       ->  Cn yaw  mom =   0.00000

 alpha     =   0.00000     deg
 beta      =   0.00000     deg
 pb/2V     =   0.00000
 qc/2V     =   0.00000
 rb/2V     =   0.00000
 CL        =   0.00000
 CDo       =   0.00000
 bank      =   0.00000     deg
 elevation =   0.00000     deg
 heading   =   0.00000     deg
 Mach      =   0.00000
 velocity  =   0.00000     Lunit/Tunit
 density   =   0.00000     Munit/Lunit^3
 grav.acc. =   9.81000     Lunit/Tunit^2
 turn_rad. =   0.00000     Lunit
 load_fac. =   1.00000
 X_cg      =   0.00000     Lunit
 Y_cg      =   0.00000     Lunit
 Z_cg      =   0.00000     Lunit
 mass      =   1.00000     Munit
 Ixx       =   1.00000     Munit-Lunit^2
 Iyy       =   1.00000     Munit-Lunit^2
 Izz       =   1.00000     Munit-Lunit^2
 Ixy       =   0.00000     Munit-Lunit^2
 Iyz       =   0.00000     Munit-Lunit^2
 Izx       =   0.00000     Munit-Lunit^2
 visc CL_a =   0.00000
 visc CL_u =   0.00000
 visc CM_a =   0.00000
 visc CM_u =   0.00000
"

End Module
