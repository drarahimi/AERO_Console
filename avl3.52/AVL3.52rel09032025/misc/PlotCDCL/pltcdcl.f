
cc      SUBROUTINE CLCDPLT(ISEC)
C------------------------------------------------------------
C     Plots CD(CL) polar characteristics for section ISEC
C------------------------------------------------------------
      INCLUDE 'PLOT.INC'
      PARAMETER ( NTMP=101 )
      EXTERNAL PLCHAR,PLMATH
C
      REAL CDCLPOL(6)
      DIMENSION XTMP(NTMP), YTMP(NTMP)
      DIMENSION XLIN(2), YLIN(2)
C
      DATA LMASK1, LMASK2, LMASK3 / -32640, -30584, -21846 /
C

      CALL INIT
C=============================================
      CALL PLINITIALIZE
C---- set up color spectrum
      NCOLOR = 64
      CALL COLORSPECTRUMHUES(NCOLOR,'BCGYORM')
C      
 10   print *, ' '
      print *, 'Enter polar parameters: '
      print *, 'CLmin, CDmin, CL0, CD0, CLmax, CDmax: '
      read(*,*,err=10) clmn,cdmn,cl0,cd0,clmx,cdmx

      cdclpol(1) = clmn
      cdclpol(2) = cdmn
      cdclpol(3) = cl0  
      cdclpol(4) = cd0  
      cdclpol(5) = clmx
      cdclpol(6) = cdmx
c
      clrange = clmx - clmn
      cl0 = clmn - 0.2*clrange 
      cl1 = clmx + 0.2*clrange 
      clrange = cl1 - cl0
      
C---- get polar data points for section 
      do i = 1, NTMP
        frac = float(i-1)/float(NTMP-1)
        cl = cl0 + frac*clrange
        call cdcl(cdclpol,cl,cd,cd_cl)
        YTMP(I) = cl
        XTMP(I) = cd
        write(8,*) i,ytmp(i),xtmp(i)
      end do

C---- set plot limits
      CLMIN = 999.
      CDMIN = 999.
      CLMAX = -999.
      CDMAX = -999.
      do i = 1, NTMP
         CLMIN = MIN(CLMIN,YTMP(i))
         CDMIN = MIN(CDMIN,XTMP(i))
         CLMAX = MAX(CLMAX,YTMP(i))
         CDMAX = MAX(CDMAX,XTMP(i))
      end do
      write(*,*) 'CD min max ',CDMIN,CDMAX
      write(*,*) 'CL min max ',CLMIN,CLMAX
C     
C---- plot scale factor and aspect ratio
      PLFAC = 0.8
      PLPAR = PAR
C
C---- character size for axis numbers, labels
      CS  = CSIZE*0.8
      CSL = CSIZE*1.0
C
C---- CD, CL, alpha axis annotation increments
      DCD = 0.005
      DCL = 0.2
      IF(CLMAX-CLMIN .GT.  2.01)  DCL =  0.5
C
cc      IF(5.0*CDMIN   .GT. 0.016)  DCD = 0.005
      IF(5.0*CDMIN   .GT. 0.040)  DCD = 0.01
      IF(5.0*CDMIN   .GT. 0.080)  DCD = 0.02
      IF(5.0*CDMIN   .GT. 0.160)  DCD = 0.05
      IF(5.0*CDMIN   .GT. 0.400)  DCD = 0.10
C
C---- set plot limits
      CLMIN0 = DCL * AINT( MIN(CLMIN,0.0)/DCL - 0.5 )
      CLMAX0 = DCL * AINT( MAX(CLMAX,0.0)/DCL + 0.5 )
C
      CDMIN0 = 0.0
      CDMAX0 = DCD * AINT( 10.0*CDMIN/DCD + 0.5 )
cc      CDMAX0 = DCD * AINT( 5.0*CDMIN/DCD + 0.5 )
      CDMAX0 = MAX( CDMAX0 , 0.001 )
      write(*,*) 'CD min max dCD ',CDMIN0,CDMAX0,DCD
      write(*,*) 'CL min max dCL ',CLMIN0,CLMAX0,DCL
C
C---- set CL, CD scaling factors
      CLWT = PLPAR/(CLMAX0-CLMIN0)
      CDWT = 0.8 /(CDMAX0-CDMIN0)
C
C===========================================================
C  Start plot
      CALL PLTINI(SCRNFR,IPSLU,IDEV,PLFAC*SIZE,LPLOT,LLAND)
      CALL PLOTABS(1.0,0.75,-3)
C
      ICOL0 = 1
cc      CALL GETCOLOR(ICOL0)
      CALL NEWCOLORNAME('black')
C
C---- re-origin for CD-CL plot
      CALL PLOT(0.0,-CLWT*CLMIN0,-3)
      CALL PLOT(5.0*CS,0.0,-3)
C
C---- plot case name, section # and r/R location
      CALL NEWPEN(2)
      XPLT = 0.0
      YPLT = CLWT*CLMAX0 + 1.0*CSL
      CALL PLCHAR(XPLT,YPLT,CSL,NAME,0.0,-1)
      XPLT = 0.0
      YPLT = CLWT*CLMIN0 - 2.5*CS
      CALL PLCHAR(XPLT,YPLT,CS,'Section # ',0.0,10)
      CALL PLNUMB(999.,999.,CS, FLOAT(ISEC),0.0,-1)
c      CALL PLCHAR(999.,999.,CS,'  r/R = ',0.0,8)
c      CALL PLNUMB(999.,999.,CS, XISECT,0.0,4)
c      CALL PLNUMB(999.,999.,CS, REREF,0.0,-1)
C
C---- plot CD-CL axes
      CALL NEWPEN(2)
      CALL YAXIS(0.0,CLWT*CLMIN0, CLWT*(CLMAX0-CLMIN0),
     &               CLWT*DCL,CLMIN0,DCL,CS,1)
      NDCD = 2
      if(DCD.LT.0.01) NDCD = 3
      CALL XAXIS(CDWT*CDMIN0,0.0,-CDWT*(CDMAX0-CDMIN0),
     &               CDWT*DCD,CDMIN0,DCD,CS,NDCD)
      IF(LGRID) THEN
       CALL NEWPEN(1)
       NXG = INT( (CDMAX0-CDMIN0)/DCD + 0.01 )
       NYG = INT( (CLMAX0-CLMIN0)/DCL + 0.01 )
       CALL PLGRID(CDWT*CDMIN0,CLWT*CLMIN0, 
     &             NXG,CDWT*DCD, NYG,CLWT*DCL, LMASK2 )
      ENDIF
      CALL PLFLUSH
ccc      PAUSE
C     
C---- legend location
      XLEG    = CDWT*CDMIN0 + 10.0*CSL
      YLEG    = CLWT*CLMAX0 +  3.5*CSL
      XLIN(1) = CDWT*CDMIN0
      XLIN(2) = CDWT*CDMIN0 +  8.0*CSL
      YLIN(1) = CLWT*CLMAX0 +  3.5*CSL
      YLIN(2) = CLWT*CLMAX0 +  3.5*CSL
C
C---- CL label
      CALL NEWPEN(3)
      XPLT = -3.0*CSL
      YPLT = CLWT*(CLMAX0-1.5*DCL) - 0.3*CSL
      CALL PLCHAR(XPLT,YPLT,CSL,'c',0.0,1)
      CALL PLSUBS(XPLT,YPLT,CSL,'V',0.0,1,PLMATH)
C
C---- CD label
      CALL NEWPEN(2)
      XPLT = CDWT*(CDMAX0-1.5*DCD) - 0.7*CSL
      YPLT = -2.8*CSL
      CALL PLCHAR(XPLT,YPLT,CSL,'c',0.0,1)
      CALL PLSUBS(XPLT,YPLT,CSL,'d',0.0,1,PLCHAR)
C
C---- plot CL-CD polar curve 
      CALL NEWPEN(3)
      CALL NEWCOLORNAME('red')
      ILIN = 0
      N = NTMP
      CALL CHKLIM(N,N1,N2,XTMP,1.1*CDMAX0)
      CALL XYLINE(N2-N1+1,XTMP(N1),YTMP(N1),0.0,CDWT,0.0,CLWT,ILIN)
ccc      CALL XYLINE(NTMP,XTMP,YTMP,0.0,CDWT,0.0,CLWT,ILIN)
C------ plot legend
c      DELY = 2.5*CSL*FLOAT(IMACH-1)
c      CALL XYLINE(2,XLIN,YLIN,0.0,1.0,-DELY,1.0,ILIN)
c      CALL PLCHAR(XLEG,YLEG+DELY,CSL,'M = ',0.0,4)
c      CALL PLNUMB(999.,999.,CSL, MACH,0.0,2)
C
      ICOL0 = 1
cc      CALL NEWCOLORNAME('red')
      CALL PLFLUSH
ccc      PAUSE
C     
C---- set factors and offsets for CL(CD) plot
      XYOFF(1) = 0.
      XYOFF(2) = 0.
      XYFAC(1) = CDWT
      XYFAC(2) = CLWT
C
      CALL PLOTABS(1.0,0.5,-3)
      CALL PLOT(0.0,-CLWT*CLMIN0,-3)
      CALL PLOT(5.0*CS,0.0,-3)
      GO TO 10
C     
      RETURN
      END ! AERPLT


      SUBROUTINE INIT
      INCLUDE 'PLOT.INC'
C--------------------------------------
C     Initializes everything
C--------------------------------------
C
C---- Plotting flag
      IDEV = 1   ! X11 window only
c     IDEV = 2   ! B&W PostScript output file only (no color)
c     IDEV = 3   ! both X11 and B&W PostScript file
c     IDEV = 4   ! Color PostScript output file only 
c     IDEV = 5   ! both X11 and Color PostScript file 
C
C---- Re-plotting flag (for hardcopy)
c      IDEVRP = 2   ! B&W PostScript
      IDEVRP = 4   ! Color PostScript
C
C---- PostScript output logical unit and file specification
      IPSLU = 0  ! output to file  plot.ps   on LU 4    (default case)
c     IPSLU = ?  ! output to file  plot?.ps  on LU 10+?
C
C---- screen fraction taken up by plot window upon opening
      SCRNFR = 0.70
C
C---- Default plot size in inches
C-    (Default plot window is 11.0 x 8.5)
C-   (Must be smaller than XPAGE if objects are to fit on paper page)
      SIZE = 10.0
      CSIZE = 0.015

C---- plot-window dimensions in inches for plot blowup calculations
C-    currently,  11.0 x 8.5  default window is hard-wired in libPlt
      XPAGE = 11.0
      YPAGE = 8.5
      PAR = YPAGE/XPAGE
C     
C---- page margins in inches
      XMARG = 0.0
      YMARG = 0.0
C
      LGRID = .TRUE.
      LPLOT = .FALSE.
      LLAND = .TRUE.
C
C---- no x-y plot active yat
      XYOFF(1) = 0.
      XYOFF(2) = 0.
      XYFAC(1) = 0.
      XYFAC(2) = 0.
C
      RETURN
      END ! INIT

C***********************************************************************
C    Module:  cdcl.f
C 
C    Copyright (C) 2020 Mark Drela, Harold Youngren
C 
C    This program is free software; you can redistribute it and/or modify
C    it under the terms of the GNU General Public License as published by
C    the Free Software Foundation; either version 2 of the License, or
C    (at your option) any later version.
C
C    This program is distributed in the hope that it will be useful,
C    but WITHOUT ANY WARRANTY; without even the implied warranty of
C    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
C    GNU General Public License for more details.
C
C    You should have received a copy of the GNU General Public License
C    along with this program; if not, write to the Free Software
C    Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
C***********************************************************************

      subroutine cdcl(cdclpol,cl,cd,cd_cl)
c-------------------------------------------------------------------------
c    Returns cd as a function of cl, or drag polar.
c
c    The polar is defined in 4 pieces, between and outside 3 cl,cd pairs,
c    shown as x symbols on diagram below
c
c     1) negative stall region
c     2) parabolic cd(cl) region between negative stall and the drag minimum
c     2) parabolic cd(cl) region between the drag minimum and positive stall
c     4) positive stall region
c
c             clpos,cdpos       <-  region 4 (quadratic above clpos)
c    cl |     x--------      
c       |    /                     
c       |   |                   <-  region 3 (quadratic above clcdmin)
c       |   x clcdmin,cdmin  
c       |   |                
c       |    \                  <-  region 2 (quadratic below clcdmin)
c       |     x_________      
c       |     clneg,cdneg       <-  region 1 (quadratic below clneg)
c       |                             
c       -------------------------
c                       cd
c
c    The 3 cd,cl pairs are specified in the input cdclpol(.) array:
c      cdclpol(1)   cl (clneg) at point before the negative stall drag rise
c      cdclpol(2)   cd (cdneg) at clneg
c      cdclpol(3)   cl (clcdmin) at minimum drag (middle of polar)
c      cdclpol(4)   cd (cdmin) at clcdmin
c      cdclpol(5)   cl (clpos) at point before the positive stall drag rise
c      cdclpol(6)   cd (cdpos) at clpos
c-------------------------------------------------------------------------
      real cdclpol(6)
c
c--- clinc and cdinc control the rate of increase of drag in the stall regions
c    clinc=0.2 forces drag to increase by cdinc over deltacl=0.2 after stall
c    the cdinc term is the cd increment added by deltacl=clinc after stall
      data  clinc, cdinc / 0.2, 0.0500 /
c
      cd    = 0.0
      cd_cl = 0.0

c---- unpack the cd,cl parameters for the polar
      clmin = cdclpol(1)
      cdmin = cdclpol(2)
      cl0   = cdclpol(3)
      cd0   = cdclpol(4)
      clmax = cdclpol(5)
      cdmax = cdclpol(6)
c      write(*,*) 'CD min max ',CDMIN,CDMAX
c      write(*,*) 'CL min max ',CLMIN,CLMAX
      if(clmax.le.cl0 .or. cl0.le.clmin) then
        write(*,*) '* CDCL: input CL data out of order'
        return
      endif
c
c---- matching parameters that make the slopes smooth at the stall joins
      cdx1 =  2.0*(cdmin-cd0)*(clmin-cl0)/(clmin-cl0)**2 
      cdx2 =  2.0*(cdmax-cd0)*(clmax-cl0)/(clmax-cl0)**2 
      clfac = 1.0 / clinc
c
      if(cl.lt.clmin) then
c------ negative stall region
c-      slope matches lower side, quadratic drag rise
        cd = cdmin
     &        + cdinc*clfac**2 * (cl-clmin)**2
     &        + cdx1*(1.0 - (cl-cl0)/(clmin-cl0))
        cd_cl = cdinc*clfac**2 * (cl-clmin)*2.0
     &        - cdx1                /(clmin-cl0)
c
       elseif(cl.lt.cl0) then
c------ lower quadratic
c-      slope matches negative stall, and has zero slope at min drag point
        cd = cd0
     &        + (cdmin-cd0)*(cl-cl0)**2 /(clmin-cl0)**2  
        cd_cl = (cdmin-cd0)*(cl-cl0)*2.0/(clmin-cl0)**2  
c
       elseif(cl.lt.clmax) then
c------ upper quadratic
c-      slope matches positive stall, and has zero slope at min drag point
        cd = cd0
     &        + (cdmax-cd0)*(cl-cl0)**2 /(clmax-cl0)**2  
        cd_cl = (cdmax-cd0)*(cl-cl0)*2.0/(clmax-cl0)**2  
c
       else
c------ positive stall region
c-      slope matches upper side, quadratic drag rise
        cd = cdmax
     &        + cdinc*clfac**2 * (cl-clmax)**2  
     &        - cdx2*(1.0 - (cl-cl0)/(clmax-cl0))
        cd_cl = cdinc*clfac**2 * (cl-clmax)*2.0
     &        + cdx2                /(clmax-cl0)
      endif
c
      return
      end ! cdcl


      SUBROUTINE CHKLIM(N,NSTRT,NEND,F,FMAX)
C--- Get starting and end index for array values F(i) < FMAX
      DIMENSION F(N)
      NSTRT = 1
      NEND  = N
C--- Look for first point where F(i)<FMAX
      DO I=1,N
          IF(F(I).LT.FMAX) GO TO 10
      END DO
 10   NSTRT = MAX(I-1,1)
C--- Look for last point where F(i)<FMAX
      DO I=N,1,-1
          IF(F(I).LT.FMAX) GO TO 20
      END DO
 20   NEND = MIN(I+1,N)
C
      RETURN
      END
