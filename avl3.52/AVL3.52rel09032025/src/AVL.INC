C   Include file for AVL variables
C
      INCLUDE 'AINDEX.INC'
      INCLUDE 'ADIMEN.INC'
C
C   Derived array limits
C
C    ITMAX  number of triangles for hidden-line routines
C    IPMAX  number of parameters
C    ICMAX  number of constraints
C    JEMAX  number of eigenmode components
C
      PARAMETER (ITMAX=2*NVMAX,
     &           IVMAX=IVTOT+NDMAX,
     &           ICMAX=ICTOT+NDMAX,
     &           IPMAX=IPTOT,
     &           JEMAX=JETOT )
C
C
C---- unit values, names, and namelengths
      CHARACTER*32 UNCHL,UNCHM,UNCHT,UNCHF,UNCHS,UNCHV,UNCHA,UNCHI,UNCHD
      COMMON /UN_R/UNITL,UNITM,UNITT,UNITF,UNITS,UNITV,UNITA,UNITI,UNITD
      COMMON /UN_C/UNCHL,UNCHM,UNCHT,UNCHF,UNCHS,UNCHV,UNCHA,UNCHI,UNCHD
      COMMON /UN_I/NUL  ,NUM  ,NUT  ,NUF  ,NUS  ,NUV  ,NUA  ,NUI  ,NUD
C
      REAL MACH, MACH0
C
      CHARACTER*256 FILDEF, FRNDEF, FMSDEF, FPRDEF, FEVDEF
      CHARACTER*80 TITLE
      CHARACTER*40 STITLE, BTITLE, RTITLE
      CHARACTER*16 DNAME, GNAME
      CHARACTER*12 VARNAM, CONNAM
      CHARACTER*12 VARKEY
      CHARACTER*3  CONKEY
      CHARACTER*10 PARNAM
      CHARACTER*32 PARUNCH
      COMMON /CASE_C/
     & FILDEF,         ! default configuration save file
     & FRNDEF,         ! default run case save file
     & FMSDEF,         ! default mass distribution file
     & FPRDEF,         ! default dimensional parameter file
     & FEVDEF,         ! default eigenvalue save file
     & TITLE,          ! configuration title
     & STITLE(NFMAX),  ! surface title
     & BTITLE(NBMAX),  ! body title
     & RTITLE(NRMAX),  ! run case title
     & DNAME(NDMAX),   ! control variable name
     & GNAME(NGMAX),   ! design  variable name
     & VARNAM(IVMAX),  ! variable   name
     & CONNAM(ICMAX),  ! constraint name
     & VARKEY(IVMAX),  ! variable   selection key
     & CONKEY(ICMAX),  ! constraint selection key
     & PARNAM(IPMAX),  ! run case parameter name
     & PARUNCH(IPMAX)  ! run case parameter unit name
C
      COMMON /CASE_I/
     & LUINP,             ! logical unit for configuration file
     & LURUN,             ! logical unit for run case file
     & LUOUT,             ! logical unit for output dump file
     & LUSTD,             ! logical unit for stability deriv. dump file
     & LUSYS,             ! logical unit for dynamic system matrix dump file
     & IYSYM,IZSYM,       ! y,z image symm.  (0=no image, 1=image)
     & MATSYM,            ! matrix symmetry flag
     & NVOR,              ! number of horseshoe vortices
     & NSTRIP,            ! number of chordwise strips
     & NSURF,             ! number of surfaces
     & NLNODE,            ! number of body source+doublet line nodes
     & NBODY,             ! number of bodies
     & ICON(IVMAX,NRMAX), ! index of constraint for each variable
     & NVTOT,             ! total number of available variables
     & NCTOT,             ! total number of available constraints
     & NPTOT,             ! total number of available parameters
     & NCONTROL,          ! number of control variables
     & NDESIGN,           ! number of design variables
     & NITMAX,            ! max number of Newton iterations
     & IRUN, NRUN,        ! current run case, number of run cases stored
     & IRUNE,             ! target run case for eigenmode calculations
     & IRUNT,             ! target run case for time march initial state
     & ITRIM(NRMAX),      ! trim type used for run case (if any)
     & NEIGEN(NRMAX),     ! number of valid eigenmodes available for run case
     & NEIGENDAT(NRMAX)   ! number reference data eigenvalues
C
      LOGICAL LGEO,LENC,LAIC,LSRD,LVEL,LSOL,LSEN,
     &        LVISC,LMASS,
     &        LCONDEF, LDESDEF,
     &        LPTOT,LPSURF,LPSTRP,LPELE,LPHINGE,LPDERIV,
     &        LBFORCE,LNFLD_WV,
     &        LNASA_SA, LSA_RATES,
     &        LMWAIT
      LOGICAL LPPAR
      COMMON /CASE_L/
     & LGEO,     ! T if geometry exists
     & LENC,     ! T if all normal vectors are valid
     & LAIC,     ! T if AIC matrix has been generated
     & LSRD,     ! T if unit source+doublet strengths are computed
     & LVEL,     ! T if induced velocity matrix has been computed
     & LSOL,     ! T if valid solution exists
     & LSEN,     ! T if valid sensitivities exist
     & LVISC,    ! T if viscous profile drag terms are to be added
     & LMASS,    ! T if mass data has been read in
     & LCONDEF(NDMAX),  ! T if control variable has been declared
     & LDESDEF(NGMAX),  ! T if design  variable has been declared
     & LPTOT,    ! T if total   forces are to be printed
     & LPSURF,   ! T if surface forces are to be printed
     & LPSTRP,   ! T if strip   forces are to be printed
     & LPELE,    ! T if element forces are to be printed
     & LPHINGE,  ! T if hinge moments are to be printed
     & LPDERIV,  ! T if stability and control derivs are to be printed
     & LBFORCE,  ! T if body forces are to be included in total forces
     & LNFLD_WV, ! T if near-field forces are calculated with body induced vel.
     & LNASA_SA,    ! T if NASA-std stability axes are to be used
     & LSA_RATES,   ! T if stability-axis rates are to be used
     & LMWAIT,      ! T if mode display is to wait for real time
     & LPPAR(IPMAX) ! T if parameter value is to be plotted
C
       COMMON /CASE_R/
     & VERSION,              ! AVL version number
     & DTR,     PI,          ! 3.14159/180 ,  3.14159
     & YSYM,    ZSYM,        ! y- and z-locations of symmetry planes
     & ALFA,   BETA,         ! alpha, beta
     & VINF(3),              ! freestream velocity in body axes
     & VINF_A(3),            ! d(Vinf)/d(alpha)
     & VINF_B(3),            ! d(Vinf)/d(beta)
     & WROT(3),              ! rotation rates in body axes
     & DTIMED,               ! time step for eigenmode movie integration
     & PARVAL(IPMAX,NRMAX),  ! parameter values for run cases
     & CONVAL(ICMAX,NRMAX),  ! imposed constraint value
     & DELCON(NDMAX),        ! imposed control variable value
     & DELDES(NGMAX),        ! imposed design  variable value
     & SREF,  CREF,  BREF,   ! Area, Chord, Span  reference values
     & XYZREF(3),            ! X,Y,Z location for moments
     & XYZREF0(3),           ! X,Y,Z location for moments (default)
     & MACH,                 ! freestream Mach number
     & MACH0,                ! freestream Mach number (default)
     & CDREF,                ! baseline profile CD
     & CDREF0,               ! baseline profile CD (default)
     & VRCOREC,              ! vortex core radius fraction of strip chord
     & VRCOREW,              ! vortex core radius fraction of strip width
     & SRCORE,               ! source core radius fraction of body radius
     & CLFF, CYFF, CDFF,     ! Trefftz-plane  CL,CY,CDi
     & CLFF_U(NUMAX),CYFF_U(NUMAX),CDFF_U(NUMAX), ! deriv wrt Vinf,Wrot
     & CLFF_D(NDMAX),CYFF_D(NDMAX),CDFF_D(NDMAX), ! deriv wrt control
     & CLFF_G(NGMAX),CYFF_G(NGMAX),CDFF_G(NGMAX), ! deriv wrt design
     & SPANEF,                ! span efficiency
     & SPANEF_A,              ! d(SPANEF)/d(alpha)
     & SPANEF_U(NUMAX),       ! d(SPANEF)/d(U,W))
     & SPANEF_D(NDMAX),       ! d(SPANEF)/d(control)
     & SPANEF_G(NGMAX),       ! d(SPANEF)/d(design)
C
C  Total forces referred to SREF, moments to SREF,CREF,BREF about XYZref
     & CDTOT, CYTOT, CLTOT,         ! total CD,CY,CL in stability axes 
     & CDTOT_A, CLTOT_A,            ! sensitivities wrt alpha
     & CDTOT_U(NUMAX),CYTOT_U(NUMAX),CLTOT_U(NUMAX),     !sens wrt u,v,w,rot
     & CDTOT_D(NDMAX),CYTOT_D(NDMAX),CLTOT_D(NDMAX),     !sens wrt control
     & CDTOT_G(NGMAX),CYTOT_G(NGMAX),CLTOT_G(NGMAX),     !sens wrt design 
C
     & CFTOT(3),                     ! body axes total CFx,CFy,CFz referred to SREF
     & CFTOT_U(3,NUMAX),             !sens wrt u,v,w,rot 
     & CFTOT_D(3,NDMAX),             !sens wrt control
     & CFTOT_G(3,NGMAX),	     !sens wrt design 
C
     & CMTOT(3),                     ! body axes total CMX,CMY,CMZ referred to SREF, CREF about XYZref
     & CMTOT_U(3,NUMAX),             !sens wrt u,v,w,rot 
     & CMTOT_D(3,NDMAX),             !sens wrt control
     & CMTOT_G(3,NGMAX),	     !sens wrt design 
C
     & CDVTOT,                       ! total viscous drag (includes CDREF)
C
     & CHINGE(NDMAX),                  ! hinge moment for control
     & CHINGE_U(NDMAX,NUMAX),          ! sens wrt U,W
     & CHINGE_D(NDMAX,NDMAX),          ! sens wrt control
     & CHINGE_G(NDMAX,NGMAX),          ! sens wrt design
     & DCL_A0, DCM_A0,   ! additional default CL_a, CM_a
     & DCL_U0, DCM_U0    ! additional default CL_u, CM_u
C
      COMPLEX EVAL, EVEC, EVALDAT
      COMMON /CASE_Z/
     &  EVAL(JEMAX,NRMAX),       ! mode eigenvalue
     &  EVEC(JEMAX,JEMAX,NRMAX), ! mode eigenvector
     &  EVALDAT(JEMAX,NRMAX)     ! mode eigenvalue reference data

      COMMON /TIME_I/
     &  ITLEV,           ! current time level
     &  NTLEV,           ! number of stored time levels
     &  NTSTEPS          ! default number of time steps to run

      COMMON /TIME_R/
     &  DELTAT,                 ! integration time step
     &  TDER(3),                ! time-derivative differencing coefficient
     &  TLEV(NTMAX),            ! time values
     &  TPARS(KPTOT,NTMAX),     ! scalar parameter time trace
     &  TPARV(3,KPVTOT,NTMAX),  ! vector parameter time trace
     &  TPARD(NDMAX,NTMAX)      ! control parameter time traces

      COMMON /MASS_R/
     &   RHO0, GEE0,     ! density, gravity   | from .mass file
     &   XYZMASS0(3),    ! mass centroid      | from .mass file
     &   RMASS0     ,    ! real mass          | from .mass file
     &   RINER0(3,3),    ! real inertia       | from .mass file
     &   AMASS(3,3),     ! apparent mass/rho     | from geometry
     &   AINER(3,3)      ! apparent inertia/rho  | from geometry


      LOGICAL LFWAKE, LFALBE, LFLOAD, LRANGE
      COMMON /SURF_L/
     & LFWAKE(NFMAX),   ! T if surface sheds a wake
     & LFALBE(NFMAX),   ! T if surface is to see freestream alpha,beta
     & LFLOAD(NFMAX),   ! T if surface contributes to overall loads
     & LRANGE(NFMAX)    ! T if surface determined using full airfoil range

      COMMON /SURF_I/
     & NJ(NFMAX),       ! number of elements along span  in surface
     & NK(NFMAX),       ! number of elements along chord in surface
     & IFRST(NFMAX),    ! index of first element in surface
     & JFRST(NFMAX),    ! index of first strip in surface
     & IMAGS(NFMAX),    ! indicates whether surface is a YDUPlicated one
cc#ifdef USE_CPOML
     & LNCOMP(NFMAX),   ! logical surface component index
     & ICNTFRST(NFMAX), ! index of first section counter in surface
     & NCNTSEC(NFMAX)   ! number of section counters in surface
cc#else
cc     & LNCOMP(NFMAX)    ! logical surface component index
cc#endif

      COMMON /SURF_R/
C   Surface forces and moments referenced to SREF,CREF,BREF about XREF,YREF,ZREF
     & CDSURF(NFMAX),CYSURF(NFMAX),CLSURF(NFMAX),      ! surface CD,CY,CL in stability axes
     & CDS_A(NFMAX), CYS_A(NFMAX), CLS_A(NFMAX),       ! alpha sens.
     & CDS_U(NFMAX,NUMAX), CYS_U(NFMAX,NUMAX), CLS_U(NFMAX,NUMAX),   ! velocity and rotation sens. 
     & CDS_D(NFMAX,NDMAX), CYS_D(NFMAX,NDMAX), CLS_D(NFMAX,NDMAX),   ! control sens.		 
     & CDS_G(NFMAX,NDMAX), CYS_G(NFMAX,NGMAX), CLS_G(NFMAX,NGMAX),   ! design sens.                
C
     & CFSURF(3,NFMAX),                            ! body axes surface CFx,CFy,CFz referred to SREF about XYZref 
     & CFS_U(3,NFMAX,NUMAX),                       ! velocity and rotation sens. 
     & CFS_D(3,NFMAX,NDMAX),                       ! control sens.		 
     & CFS_G(3,NFMAX,NGMAX),                       ! design sens.                
C
     & CMSURF(3,NFMAX),                            ! body axes surface CMx,CMy,CMz referred to SREF, CREF about XYZref
     & CMS_U(3,NFMAX,NUMAX),                       ! velocity and rotation sens.
     & CMS_D(3,NFMAX,NDMAX),                       ! control sens.
     & CMS_G(3,NFMAX,NGMAX),                       ! design sens.
C
     & CDVSURF(NFMAX),                             ! surface viscous CD
C
     & SSURF(NFMAX), CAVESURF(NFMAX),       ! surface area and average chord
C
C   Surface local forces and moments, referred to surface area, average chord, moments about root LE
     & CF_LSRF(3,NFMAX), CM_LSRF(3,NFMAX),      ! body axes forces and moments
     & CL_LSRF(NFMAX),   CD_LSRF(NFMAX)         ! stability axes forces
C
      COMMON /STRP_I/
     & LSSURF(NSMAX),    ! index of surface which contains this strip
     & IJFRST(NSMAX),    ! index of first element in strip
     & NVSTRP(NSMAX)     ! number of elements in strip

      LOGICAL LSTRIPOFF,LVISCSTRP,LJ1SECT,LJ2SECT
      COMMON /STRP_L/
     & LSTRIPOFF(NSMAX),  ! T if strip is "turned off" (outside of fluid)
     & LVISCSTRP(NSMAX),  ! T is strip has viscous drag data
     & LJ1SECT(NSMAX),    ! T if station 1 is a section
     & LJ2SECT(NSMAX)     ! T if station 2 is a section

      COMMON /STRP_R/
     & RLE(3,NSMAX),  CHORD(NSMAX),    ! strip c.p. line LE point, chord
     & RLE1(3,NSMAX), CHORD1(NSMAX),   ! strip left  end LE point, chord
     & RLE2(3,NSMAX), CHORD2(NSMAX),   ! strip right end LE point, chord
     & WSTRIP(NSMAX),                  ! strip y-z width
     & TANLE(NSMAX),  TANTE(NSMAX),    ! strip LE,TE sweep slopes
     & CLCD(6,NSMAX),                  ! strip viscous polar parameters (CL0,CD0,CL1,CD1,CL2,CD2) 
     & SAXFR,                          ! x/c of spanwise axis for Vperp def
     & ESS(3,NSMAX),                   ! spanwise unit vector for Vperp def
     & ENSY(NSMAX), ENSZ(NSMAX),       ! strip normal vector in Trefftz-Pln
     & XSREF(NSMAX),YSREF(NSMAX),ZSREF(NSMAX),   ! strip reference point
     & AINC(NSMAX),                    ! strip's incidence twist angle
     & AINC_G(NSMAX,NGMAX),            ! dAINC/dG
C
C  Strip forces referenced to strip area in stability axes
     & CDSTRP(NSMAX), CYSTRP(NSMAX), CLSTRP(NSMAX),      ! strip forces CD,CY,CL in stability axes
     & CDST_A(NSMAX), CYST_A(NSMAX), CLST_A(NSMAX),      ! alpha sens.
     & CDST_U(NSMAX,NUMAX), CYST_U(NSMAX,NUMAX), CLST_U(NSMAX,NUMAX),
     & CDST_D(NSMAX,NDMAX), CYST_D(NSMAX,NDMAX), CLST_D(NSMAX,NDMAX),
     & CDST_G(NSMAX,NGMAX), CYST_G(NSMAX,NGMAX), CLST_G(NSMAX,NGMAX),
C
C  Strip forces referenced to strip area and chord
     & CFSTRP(3,NSMAX),                                  ! strip forces CFx,CFy,CFz in body axes
     & CFST_U(3,NSMAX,NUMAX),                            ! freestream velocity and rotation sens.
     & CFST_D(3,NSMAX,NDMAX),                            ! control sens.
     & CFST_G(3,NSMAX,NGMAX),                            ! design sens.
C  Strip moments referenced to strip area and chord with moments about XYZref
     & CMSTRP(3,NSMAX),                                  ! strip momentss CMx,CMy,CMz in body axes
     & CMST_U(3,NSMAX,NUMAX),                            ! freestream velocity and rotation sens.
     & CMST_D(3,NSMAX,NDMAX),                            ! control sens.
     & CMST_G(3,NSMAX,NGMAX),                            ! design (incidence) sens.
C  Strip local forces referenced to strip area and chord with moments about 1/4 chord
     & CF_LSTRP(3,NSMAX),  CM_LSTRP(3,NSMAX),   ! strip forces in body axes referenced to strip area and 1/4 chord
     & CN_LSTRP(NSMAX),    CA_LSTRP(NSMAX),     ! strip forces in local dihedral plane (normal and axial forces)
     & CD_LSTRP(NSMAX),    CL_LSTRP(NSMAX),     ! strip forces in local dihedral plane (lift and drag forces)
     & CDV_LSTRP(NSMAX),                        ! strip viscous drag in stability axes
     & CLT_LSTRP(NSMAX),   CLA_LSTRP(NSMAX),    ! strip CL referenced to Vperp, CL referenced to total local velocity
     & CMC4_LSTRP(NSMAX),  CMLE_LSTRP(NSMAX),   ! strip pitching moment about c/4 and pitching moment about LE vector
     & CNC(NSMAX),         DWWAKE(NSMAX),       ! strip spanloading and downwash in Trefftz plane
     & CNC_U(NSMAX,NUMAX),                      ! spanloading sens.
     & CNC_D(NSMAX,NDMAX),
     & CNC_G(NSMAX,NGMAX)
C
cc#ifdef USE_CPOML
      COMMON /STRP_S/
     & AINC1(NSMAX), AINC2(NSMAX),  ! left/right strip incidence twist angle
     & ICNTSEC(NSMAX)               ! section counters for surface
cc#endif
C
      LOGICAL LVNC, LVALBE
      COMMON /VRTX_L/
     & LVNC(NVMAX),    ! T if V.n=0 is to be enforced for this c.p.
     & LVALBE(NVMAX)   ! T if c.p. is to see freestream alpha,beta

      COMMON /VRTX_I/
     & LVCOMP(NVMAX)    ! index of component which contains vortex element
      COMMON /VRTX_R/
     & RV1(3,NVMAX),    ! h.v. vortex left  points
     & RV2(3,NVMAX),    ! h.v. vortex right points
     & RV(3,NVMAX),     ! h.v. vortex center points
     & RC(3,NVMAX),     ! h.v. control points
     & RS(3,NVMAX),     ! h.v. source points
     & RL(3,NLMAX),RADL(NLMAX),  ! source line node points, body radius
     & DXV(NVMAX),               ! chord of element
     & CHORDV(NVMAX),            ! chord of element-containing strip
     & SLOPEV(NVMAX),            ! camber slopes at h.v. bound leg
     & SLOPEC(NVMAX),            ! camber slopes at c.p.
     & DCONTROL(NVMAX,NDMAX),    ! d(normal angle)/dCONTROL
     & VHINGE(3,NSMAX,NDMAX),    ! hinge vector for CONTROL rot. of normal
     & PHINGE(3,NSMAX,NDMAX),    ! point on hingeline for hinge moment calculation
     & VREFL(NSMAX,NDMAX),       ! sign applied to hinge vec. of refl. surface
     & ENC(3,NVMAX),             ! control point normal vector
     & ENV(3,NVMAX),             ! horseshoe vortex normal vector
     & ENC_D(3,NVMAX,NDMAX),     ! control point normal vector sensitivities
     & ENC_G(3,NVMAX,NGMAX),
     & ENV_D(3,NVMAX,NDMAX),     ! horseshoe vortex normal vector sensitivities
     & ENV_G(3,NVMAX,NGMAX),
     & DCP(NVMAX),               ! delta(Cp) on vortex element
     & DCP_U(NVMAX,NUMAX),
     & DCP_D(NVMAX,NDMAX),
     & DCP_G(NVMAX,NGMAX),
     & GAM(NVMAX),               ! circulation of horseshoe vortex
     & GAM_U_0(NVMAX,NUMAX),
     & GAM_U_D(NVMAX,NUMAX,NDMAX),
     & GAM_U_G(NVMAX,NUMAX,NGMAX),
     & GAM_U(NVMAX,NUMAX),       ! circulation sensitivities
     & GAM_D(NVMAX,NDMAX),
     & GAM_G(NVMAX,NGMAX),
     & SRC(NLMAX),               ! source  strength of source+doublet line elem
     & DBL(3,NLMAX),             ! doublet strength of source+doublet line elem
     & SRC_U(NLMAX,NUMAX),       ! sensitivities
     & DBL_U(3,NLMAX,NUMAX),
     & WCSRD(3,NVMAX),           ! velocity at c.p. from sources and doublets
     & WCSRD_U(3,NVMAX,NUMAX),   
     & WVSRD(3,NVMAX),           ! velocity at h.v. from sources and doublets
cc#ifdef USE_CPOML
     & WVSRD_U(3,NVMAX,NUMAX),
     & CPT(NVMAX)                ! thickness-based Cp at panel midpoint
cc#else
cc     & WVSRD_U(3,NVMAX,NUMAX)
cc#endif
C
cc#ifdef USE_CPOML
      COMMON /VRTX_S/
     & XYN1(2,NVMAX), XYN2(2,NVMAX),  ! left/right aft-node of element
     & ZLON1(NVMAX), ZLON2(NVMAX),    ! left/right lower z-coord of aft-node
     & ZUPN1(NVMAX), ZUPN2(NVMAX)     ! left/right upper z-coord of aft-node
cc#endif
C
      COMMON /BODY_I/
     & NL(NBMAX),       ! number of source-line nodes in body
     & LFRST(NBMAX)     ! index of first line node in body

      COMMON /BODY_R/
     & ELBDY(NBMAX),                            ! body length
     & SRFBDY(NBMAX),                           ! body surface area
     & VOLBDY(NBMAX),                           ! body volume
     & DCPB(3,NLMAX),                           ! dCp loading on sources
     & CDBDY(NBMAX),CYBDY(NBMAX),CLBDY(NBMAX),  ! body CD,CY,CL referred to SREF in stability axes
     & CFBDY(3,NBMAX),                          ! body axes CFX,CFY,CFZ referred to SREF
     & CMBDY(3,NBMAX)                           ! body axes CMX,CMY,CMZ referred to SREF, CREF about XYZref
C
      COMMON /SOLV_I/
     & IAPIV(NVMAX)           ! pivot indices for LU solver
      COMMON /SOLV_R/
     & AMACH,                 ! Mach number at which AIC matrices were computed
cc     & AICN(NVMAX,NVMAX),     ! normalwash AIC matrix (and VL system matrix)
cc     & WC_GAM(3,NVMAX,NVMAX), ! c.p. velocity/Gamma influence matrix
cc     & WV_GAM(3,NVMAX,NVMAX), ! h.v. velocity/Gamma influence matrix

     & VC(3,NVMAX),           ! h.v. induced velocity at c.p.
     & VC_U(3,NVMAX,NUMAX),
     & VC_D(3,NVMAX,NDMAX),
     & VC_G(3,NVMAX,NGMAX),
C
     & WC(3,NVMAX),           ! total induced velocity at c.p.
     & WC_U(3,NVMAX,NUMAX),
     & WC_D(3,NVMAX,NDMAX),
     & WC_G(3,NVMAX,NGMAX),
C
     & VV(3,NVMAX),           ! h.v. induced velocity at h.v.
     & VV_U(3,NVMAX,NUMAX),
     & VV_D(3,NVMAX,NDMAX),
     & VV_G(3,NVMAX,NGMAX),
C
     & WV(3,NVMAX),           ! total induced velocity at h.v.
     & WV_U(3,NVMAX,NUMAX),
     & WV_D(3,NVMAX,NDMAX),
     & WV_G(3,NVMAX,NGMAX)
C
      LOGICAL
     & LWAKEPLT,   LLOADPLT,   LHINGEPLT,
     & LBOUNDLEG,  LCHORDLINE, LCAMBER,   LCNTLPTS,
     & LNRMLPLT,   LAXESPLT,   LRREFPLT,
     & LCLPERPLT,  LDWASHPLT,  LLABSURF,   LTRFORCE,
     & LABEL_SURF, LABEL_STRP, LABEL_VRTX, LABEL_BODY, LABEL_OB,
     & LPLTNEW,
     & LPLTSURF,
     & LPLTBODY,
     & LOBPLT,
     & LSVMOV
      COMMON /PLOT_L/          ! plotting flags
     & LWAKEPLT,   LLOADPLT,   LHINGEPLT,
     & LBOUNDLEG,  LCHORDLINE, LCAMBER,   LCNTLPTS,
     & LNRMLPLT,   LAXESPLT,   LRREFPLT,
     & LCLPERPLT,  LDWASHPLT,  LLABSURF,   LTRFORCE,
     & LABEL_SURF, LABEL_STRP, LABEL_VRTX, LABEL_BODY, LABEL_OB,
     & LPLTNEW,
     & LPLTSURF(NFMAX),
     & LPLTBODY(NBMAX),
     & LOBPLT,
     & LSVMOV
      COMMON /PLOT_I/ NTRI, IMARKSURF, ICOLORSURF, NAXANN(3),
     &                IRCOLOR(NRMAX)
      COMMON /PLOT_R/ TRI(16,ITMAX),
     &                AXMIN(3),AXMAX(3),AXDEL(3),AXSPAN(3),
     &                GMIN(3),GMAX(3),GMINP(3),GMAXP(3),
     &                VMIN(3),VMAX(3),VMINP(3),VMAXP(3),
     &                DTMOVIE,TMOVIE, SLOMOF,
     &                EPHASE, EIGENF, TMOFAC

C  off-body variables

      LOGICAL LOBAIC, LOBVEL
      COMMON /OFFBODY_L/
     & LOBAIC,                        ! T if offbody AIC matrix has been generated
     & LOBVEL                         ! T if offbody velocities have been generated
      COMMON /OFFBODY_I/
     & NOB                            ! # of offbody xyz points

      COMMON /OFFBODY_R/
     & ROB(3,NOBMAX),                 ! offbody xyz points
     & VOB(3,NOBMAX),                 ! h.v. induced velocity at offbody points
     & WOB(3,NOBMAX),                 ! total induced velocity at offbody points
     & VOB_GAM(3,NOBMAX,NVMAX),       ! offbody h.v. influence matrix
     & WOBSRD(3,NOBMAX),              ! offbody source+doublet influence matrix
     & WOBSRD_U(3,NOBMAX,NUMAX)       ! offbody source+doublet influence matrix
