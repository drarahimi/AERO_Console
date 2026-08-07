C***********************************************************************
C    Module:  aplottp.f
C 
C    Copyright (C) 2002 Mark Drela, Harold Youngren
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

      SUBROUTINE PLOTTP
C-------------------------------------------------
C     Plots circulation, CL, downwash versus y
C-------------------------------------------------
      INCLUDE 'AVL.INC'
      INCLUDE 'AVLPLT.INC'
C
      CHARACTER OPT*2, LINE*80, YAXTYPE*1, SATYPE*16, PLTTYPE*16
      REAL XTMP(2), YTMP(2)
      REAL SLE(NSMAX), XPLT, YPLT
      LOGICAL LCOLHC
      CHARACTER*24 COL1, COL2, COL3, COL4
C
      REAL RINP(3)
      LOGICAL ERROR
C
      DATA  YAXTYPE / 'Y' /
      DATA  PLTTYPE / 'YZCNC' /
C
      INCLUDE 'MASKS.INC'
C
C--- PLFAC is scale factor adjustment for this whole plot 
C--- WFAC  is scale factor for W axis (as fraction of C axis)
C--- VTITLEFRAC is fraction of vertical axis to use for big label block
      DATA PLFAC, WFAC, VTITLEFRAC / 0.85, 0.5, 0.15 /
C
      SMOD(SS) = SSF*(SS - SOFF)
      XMOD(XX) = XSF*(XX - XOFF)
      YMOD(YY) = YSF*(YY - YOFF)
C
      IF(.NOT.LSOL) THEN
        WRITE(*,*) '*** No flow solution...'
        RETURN
      ENDIF
C
C---- Initialize plot stuff
      SIZEOLD = SIZE
      CHOLD = CH
      SIZE  = 9.0
      CH    = 0.012
C---- number of control-variable lines in excess of 6
ccc      NCONLIN = (NCONTROL+1)/2
      NCONLIN = MAX( NCONTROL-6 , 0 )
C
C---- line y-spacing factor
      YSP = 2.0
C
C--- define colors used for plot data
      COL1 = 'green'   ! cl c/Cref
      COL2 = 'blue'    ! alpha_i
      COL3 = 'red'     ! cl_perp
ccc   COL4 = 'yellow'  ! cl
      COL4 = 'orange'  ! cl
C
C
      CALL GETSA(LNASA_SA,SATYPE,DIR)
C
      CA = COS(ALFA)
      SA = SIN(ALFA)
      CRSAX = CMTOT(1)*CA + CMTOT(3)*SA
      CMSAX = CMTOT(2)              
      CNSAX = CMTOT(3)*CA - CMTOT(1)*SA  
C
C---- Find the min/max of Y,Z,CL,CNC,downwash
 50   YMIN =  RV1(2,1)
      YMAX =  RV1(2,1)
      ZMIN =  RV1(3,1)
      ZMAX =  RV1(3,1)
      CYMIN = YMIN
      CYMAX = YMAX
      CZMIN = ZMIN
      CZMAX = ZMAX
      FMIN =  CNC(1)
      FMAX =  CNC(1)
      CMIN = 0.0
      CMAX = 0.0
      WMAX = 0.0
      WMIN = 0.0
      DO I=1, NSTRIP
        IV = IJFRST(I)
        YMIN = MIN( YMIN, RV1(2,IV), RV2(2,IV), 0.0 )
        YMAX = MAX( YMAX, RV1(2,IV), RV2(2,IV), 0.0 )
        ZMIN = MIN( ZMIN, RV1(3,IV), RV2(3,IV), 0.0 )
        ZMAX = MAX( ZMAX, RV1(3,IV), RV2(3,IV), 0.0 )
        FMIN = MIN( FMIN, CNC(I), 0.0 )
        FMAX = MAX( FMAX, CNC(I), 0.0 )
C---- use either min-max or L8 norms for axis length determination
C     L8 prevents large local spikes from giving inappropriate axes
C--- L8 norm
        WMIN = WMIN + MIN( -DWWAKE(I)  , 0.0 ) ** 8
        WMAX = WMAX + MAX( -DWWAKE(I)  , 0.0 ) ** 8
C--- Regular min/max
        CMIN = MIN( CMIN, CLT_LSTRP(I)   , 0.0 )
        CMAX = MAX( CMAX, CLT_LSTRP(I)   , 0.0 )
c        WMIN = MIN( WMIN, -DWWAKE(I)  , 0.0 )
c        WMAX = MAX( WMAX, -DWWAKE(I)  , 0.0 )
      END DO
C
C--- Process L8 sums to norms
      IF(WMIN .NE. 0.0) WMIN = -(ABS(WMIN)/NSTRIP) ** 0.125
      IF(WMAX .NE. 0.0) WMAX =  (ABS(WMAX)/NSTRIP) ** 0.125
C
C---- normalized spanload CNC/Cref and CL will have the same axis, so...
      CMIN = MIN(FMIN/CREF,CMIN)
      CMAX = MAX(FMAX/CREF,CMAX)
C
      IF(CMAX-CMIN .LT. 1.0E-5) THEN
       CMIN = 0.0
       CMAX = 0.1
      ENDIF
C
      IF(WMAX-WMIN .LT. 1.0E-5) THEN
       WMIN = 0.0
       WMAX = 0.1
      ENDIF
C
C---- determine "nice" upper,lower bounds on y, z, CNC/Cref, CL, 
C-    and corresponding "nice" number of annotations
      CALL AXISADJ(YMIN,YMAX,YSPAN,DELY,NYANN)
      CALL AXISADJ(ZMIN,ZMAX,ZSPAN,DELZ,NZANN)
      CALL AXISADJ(WMIN,WMAX,WSPAN,DELW,NWANN)
      CALL AXISADJ(CMIN,CMAX,CSPAN,DELC,NCANN)
C---- Ratio of W and CL scale factors
      WOC = WFAC*(CMAX-CMIN)/(WMAX-WMIN)
C
C
C
C---------------------------------------------------
C---- Plot YZ Trefftz plane geometry and loading
 100  IF(PLTTYPE.NE.'TRACE') GO TO 150 
C
C---- start with geometry sizing scale
      YSF  = 1.0    / (YMAX-YMIN)
      ZSF  = PLOTAR / (ZMAX-ZMIN)
      XOFF = YMIN
      YOFF = ZMIN
      SF = MIN(YSF,ZSF)
      XSF = SF
      YSF = SF
c      write(*,*) 'YMIN,YMAX ',YMIN,YMAX,XSF
c      write(*,*) 'ZMIN,ZMAX ',ZMIN,ZMAX,YSF
c      VECSCL = 0.3*PLOTAR/SF
c      VECSCL = 0.5*PLOTAR/SF
      VECSCL = PLOTAR/SF
c      FMAG = FMAX-FMIN
c      IF(FMAG.NE.0) VECSCL = VECSCL/FMAG
ccc      write(*,*) 'FMIN,FMAX ',FMIN,FMAX,FMAG
c      write(*,*) 'VECSCL ',VECSCL
C
      YMIN =  RV1(2,1)
      YMAX =  RV1(2,1)
      ZMIN =  RV1(3,1)
      ZMAX =  RV1(3,1)
      CYMIN = YMIN
      CYMAX = YMAX
      CZMIN = ZMIN
      CZMAX = ZMAX
      CNCYZAV = 0.0
      CNCYZMX = 0.0
      DO N=1, NSURF
        J1 = JFRST(N)
        DO J = 1, NJ(N)
          JJ = J1+J-1
          Y0 = RLE(2,JJ)
          Z0 = RLE(3,JJ)
          Y1 = Y0 + VECSCL*CNC(JJ)*ENSY(JJ) 
          Z1 = Z0 + VECSCL*CNC(JJ)*ENSZ(JJ) 
          YMIN  = MIN( YMIN, Y0 )
          YMAX  = MAX( YMAX, Y0 )
          ZMIN  = MIN( ZMIN, Z0 )
          ZMAX  = MAX( ZMAX, Z0 )
          CYMIN = MIN( CYMIN, Y1 )
          CYMAX = MAX( CYMAX, Y1 )
          CZMIN = MIN( CZMIN, Z1 )
          CZMAX = MAX( CZMAX, Z1 )
          CNCYZAV = CNCYZAV + SQRT((Y1-Y0)**2 + (Z1-Z0)**2)
          CNCYZMX = MAX(CNCYZMX,SQRT((Y1-Y0)**2 + (Z1-Z0)**2))
        END DO
      END DO
      CNCYZAV = CNCYZAV/NSTRIP
cc      write(*,*) 'YMIN,YMAX ',YMIN,YMAX
cc      write(*,*) 'ZMIN,ZMAX ',ZMIN,ZMAX
cc      write(*,*) 'CYMIN,CYMAX ',CYMIN,CYMAX
cc      write(*,*) 'CZMIN,CZMAX ',CZMIN,CZMAX
cc      write(*,*) 'CNCYZAV,CNCYZMX ',CNCYZAV,CNCYZMX
C
C---- rescale CNC vectors and geometry to fit
      DCYMIN = CYMIN - YMIN
      DCYMAX = CYMAX - YMAX
      DCZMIN = CZMIN - ZMIN
      DCZMAX = CZMAX - ZMAX
cc      CALL AXISADJ(CYMIN,CYMAX,CYSPAN,DELCY,NCYANN)
cc      CALL AXISADJ(CZMIN,CZMAX,CZSPAN,DELCZ,NCZANN)
      YSPAN = YMAX - YMIN
      ZSPAN = ZMAX - ZMIN
      CYSPAN = CYMAX - CYMIN
      CZSPAN = CZMAX - CZMIN
cc      write(*,*) 'YSPAN ',YSPAN
cc      write(*,*) 'ZSPAN ',ZSPAN
cc      write(*,*) 'CYMIN,CYMAX,CYSPAN ',CYMIN,CYMAX,CYSPAN
cc      write(*,*) 'CZMIN,CZMAX,CZSPAN ',CZMIN,CZMAX,CZSPAN

C---- scale CNC vector envelope to 30% beyond geometry Y,Z
cc      CSCL = 1.3/(MAX(CYSPAN/YSPAN,CZSPAN/ZSPAN))
cc      CSCL = 0.3*MAX(YSPAN,ZSPAN)/CNCYZAV 
cc      CSCL = 0.5*MAX(YSPAN,ZSPAN)/CNCYZMX 
      CSCL = 0.3*MAX(YSPAN,ZSPAN)/CNCYZMX 
c      CSCL = 0.1*MAX(YSPAN,ZSPAN)/CNCYZMX 
cc      write(*,*) 'CSCL ',CSCL
c      YMIN = YMIN - MAX(0.0,CSCL*(CYSPAN-YSPAN))
c      YMAX = YMAX + MAX(0.0,CSCL*(CYSPAN-YSPAN))
c      ZMIN = ZMIN - MAX(0.0,CSCL*(CZSPAN-ZSPAN))
c      ZMAX = ZMAX + MAX(0.0,CSCL*(CZSPAN-ZSPAN))
C
      YMIN = YMIN - MAX(0.0,CSCL*(CYMIN-YMIN))
      YMAX = YMAX + MAX(0.0,CSCL*(CYMAX-YMAX))
      ZMIN = ZMIN - MAX(0.0,CSCL*(CZMIN-ZMIN))
      ZMAX = ZMAX + MAX(0.0,CSCL*(CZMAX-ZMAX))
cc      write(*,*) 'YMIN,YMAX ',YMIN,YMAX
cc      write(*,*) 'ZMIN,ZMAX ',ZMIN,ZMAX
C---- reset "nice" Y,Z plot limits
      CALL AXISADJ(YMIN,YMAX,YSPAN,DELY,NYANN)
      CALL AXISADJ(ZMIN,ZMAX,ZSPAN,DELZ,NZANN)
cc      write(*,*) 'adj YMIN,YMAX ',YMIN,YMAX
cc      write(*,*) 'adj ZMIN,ZMAX ',ZMIN,ZMAX
C
      XSF  = 1.0    / (YMAX-YMIN)
      YSF  = PLOTAR / (ZMAX-ZMIN)
      XOFF = YMIN
      YOFF = ZMIN
      SF = MIN(XSF,YSF)
      XSF = SF
      YSF = SF
      VECSCL = CSCL*VECSCL
c      write(*,*) 'CSCL, VECSCL ',CSCL,VECSCL
C
C---- start the Y,Z,CNC plot
      CALL PLTINI(IDEV)
      CALL NEWFACTOR(PLFAC*SIZE)
      CALL GETCOLOR(ICOL0)
C
C----Offset the X-axis, leaving room on left for labels
      CALL PLOT(12.0*CH,8.0*CH,-3)
C
C---Put up the Xaxis (suppressing the end annotations)
      CALL NEWPEN(3)
      XLAB = XMOD(YMAX - 1.5*DELY)
      YLAB = YMOD(ZMAX - 1.5*DELZ)
      IFLG = 1
      CALL XAXIS2(0.0,0.0,XSF*(YMAX-YMIN),XSF*DELY,
     &            YMIN,DELY,IFLG,0.9*CH,-2)
      CALL PLCHAR(XLAB-0.7*CH,-3.5*CH,1.2*CH,'Y',0.,1)
C
      CALL YAXIS(0.0,0.0,YSF*(ZMAX-ZMIN),YSF*DELZ,
     &           ZMIN,DELZ, 0.9*CH,-2)
      CALL PLCHAR(-6.5*CH,YLAB-0.7*CH,1.2*CH,'Z',0.,1)
C
C---Put up a reference grid on Y,Z 
      NX = IFIX(0.1 + (YMAX-YMIN)/DELY)
      NY = IFIX(0.1 + (ZMAX-ZMIN)/DELZ)
      CALL PLGRID(0.0,0.0,NX,XSF*DELY,NY,YSF*DELZ,LMASK2)

C---Plot the Y,Z trace
      CALL NEWPEN(4)
      CALL NEWCOLORNAME(COL1)
      CALL GETCOLOR(ICOL1)
      CALL NEWCOLORNAME(COL4)
      CALL GETCOLOR(ICOL4)
C
      DO N = 1, NSURF
        J1 = JFRST(N)
cc        write(*,*) 'N,J,NJ ',N,J,NJ(N)
C
C---- plot Y,Z geometry of far-field trace 
        CALL NEWCOLOR(ICOL4)
        DO J = 1, NJ(N)
          IV = IJFRST(J1+J-1)
          IF(IMAGS(N).EQ.1) THEN
           IF(J.EQ.1) THEN
            XPLT = XMOD(RV1(2,IV))
            YPLT = YMOD(RV1(3,IV))
            CALL PLOT(XPLT,YPLT,3)
           ELSE
            XPLT = XMOD(RV2(2,IV))
            YPLT = YMOD(RV2(3,IV))
            CALL PLOT(XPLT,YPLT,2)
           ENDIF
          ELSE
           IF(J.EQ.1) THEN
            XPLT = XMOD(RV2(2,IV))
            YPLT = YMOD(RV2(3,IV))
            CALL PLOT(XPLT,YPLT,3)
           ELSE
            XPLT = XMOD(RV1(2,IV))
            YPLT = YMOD(RV1(3,IV))
            CALL PLOT(XPLT,YPLT,2)
           ENDIF
          ENDIF
        END DO
C---- plot CNC loading as arrows
        CALL NEWCOLOR(ICOL1)
        DO J = 1, NJ(N)
          JJ = J1+J-1
          X0 = RLE(2,JJ)
          Y0 = RLE(3,JJ)
          X1 = X0 + VECSCL*CNC(JJ)*ENSY(JJ) 
          Y1 = Y0 + VECSCL*CNC(JJ)*ENSZ(JJ) 
          XPLT = XMOD(X0)
          YPLT = YMOD(Y0)
          CALL PLOT(XPLT,YPLT,3)
          XPLT = XMOD(X1)
          YPLT = YMOD(Y1)
          CALL PLOT(XPLT,YPLT,2)
          DX = X1 - X0
          DY = Y1 - Y0
          X2 = X0 + 0.8*DX + 0.02*DY
          Y2 = Y0 + 0.8*DY - 0.02*DX
          X3 = X0 + 0.8*DX - 0.02*DY
          Y3 = Y0 + 0.8*DY + 0.02*DX
          CALL PLOT(XMOD(X2),YMOD(Y2),2)
          CALL PLOT(XMOD(X3),YMOD(Y3),2)
          CALL PLOT(XMOD(X1),YMOD(Y1),2)
        END DO
C
        IF(LLABSURF) THEN
          JLAB = J1 + 0.50*NJ(N)
          XLAB = XMOD(RLE(2,JLAB))         
          YLAB = YMOD(RLE(3,JLAB))         
          CALL PLNUMB(XLAB+0.5*CH,YLAB+0.5*CH,0.8*CH,FLOAT(N),0.0,-1)
        ENDIF
      ENDDO
C
      CALL PLFLUSH
      CALL NEWCOLOR(ICOL0)
C
C--- Forces block at top of Y,Z,CNC plot
      XLAB = 0.0
      YLAB = YSF*(ZMAX-ZMIN) + YSP*CH*(8.0+FLOAT(NCONLIN))
      GO TO 60
C
C---------------------------------------------------
C---- Plot CNC/Cref, CL, DW vs Y or Z
 150  CONTINUE
      IF(YAXTYPE.EQ.'Y') THEN
        SMIN = YMIN
        SMAX = YMAX
        DELS  = DELY
        DO J = 1, NSTRIP
          SLE(J) = RLE(2,J)
        END DO
       ELSE
        SMIN = ZMIN
        SMAX = ZMAX
        DELS  = DELZ
        DO J = 1, NSTRIP
          SLE(J) = RLE(3,J)
        END DO
      ENDIF
C
 155  CONTINUE
      SSF  = 1.0 / (SMAX-SMIN)
      SOFF = SMIN
      IF(LDWASHPLT ) THEN
       CSF = PLOTAR / (MAX(CMAX,WOC*WMAX)-MIN(CMIN,WOC*WMIN))
C---- Modify CSF to allow portion of vertical space for a big label block
       CSF = (1.0 - VTITLEFRAC)*CSF
       WSF = WOC * CSF
      ELSE
       CSF = PLOTAR / (CMAX-CMIN)
C---- Modify CSF to allow portion of vertical space for a big label block
       CSF = (1.0 - VTITLEFRAC)*CSF
       WSF = 0.0
      ENDIF
C
      CALL PLTINI(IDEV)
      CALL NEWFACTOR(PLFAC*SIZE)
      CALL GETCOLOR(ICOL0)
C
C----Offset the X-axis, leaving room on left for labels
      FMIN = MIN(WSF*WMIN,CSF*CMIN)
      CALL PLOT(14.0*CH,6.0*CH-FMIN,-3)
C
C---Put up the Xaxis (suppressing the end annotations)
      CALL NEWPEN(3)
      XLAB = SMOD(SMAX - 1.5*DELS)
      IF(LDWASHPLT ) THEN
       IFLG = 3
      ELSE
       IFLG = 1
      ENDIF
      CALL XAXIS2(0.0,0.0,SSF*(SMAX-SMIN),SSF*DELS,
     &            SMIN,DELS,IFLG,0.9*CH,-2)
      CALL PLCHAR(XLAB-0.7*CH,-3.5*CH,1.2*CH,YAXTYPE,0.,1)
C
C--- cl_perp, cl, cl*c/Cref axis
      CALL NEWPEN(2)
      XLAB = SMOD(SMIN)
C
      IF(LCLPERPLT) THEN
       YLAB = CSF*(CMAX - 0.5*DELC)
       CALL NEWCOLORNAME(COL3)
       CALL PLCHAR(XLAB-6.5*CH,YLAB-0.5*CH,1.2*CH,'c'  ,0., 1)
       CALL PLMATH(XLAB-5.5*CH,YLAB-0.9*CH,0.8*CH, 'V' ,0., 1)
       CALL PLCHAR(XLAB-4.2*CH,YLAB-0.4*CH,0.7*CH,'T'  ,180.0, 1)
      ENDIF
C
      YLAB = CSF*(CMAX - 1.5*DELC)
      CALL NEWCOLORNAME(COL4)
      CALL PLCHAR(XLAB-6.5*CH,YLAB-0.5*CH,1.2*CH,'c'  ,0., 1)
      CALL PLMATH(XLAB-5.5*CH,YLAB-0.9*CH,0.8*CH, 'V' ,0., 1)
C
      YLAB = CSF*(CMAX - 2.5*DELC)
      CALL NEWCOLORNAME(COL1)
      CALL PLCHAR(XLAB-8.5*CH,YLAB-0.5*CH,1.2*CH,'c c/c',0.,5)
      CALL PLCHAR(999.,999.,0.65*CH,'ref',0.,3)
      CALL PLMATH(XLAB-7.5*CH,YLAB-0.9*CH,0.8*CH, 'V'    ,0.,1)
      CALL NEWCOLOR(ICOL0)
      CALL YAXIS(0.0,CSF*CMIN,CSF*(CMAX-CMIN),CSF*DELC,
     &           CMIN,DELC, 0.9*CH,-2)
C
C---Put up a reference grid on the Y/Z and cl_perp, cl, cl*c/Cref axes
      NY = IFIX(0.1 + (SMAX-SMIN)/DELS)
      NC = IFIX(0.1 + (CMAX-CMIN)/DELC)
      CALL PLGRID(0.0,CSF*CMIN,NY,SSF*DELS,NC,CSF*DELC,LMASK2)
C
C---Downwash axis
      IF(LDWASHPLT ) THEN
        CALL NEWPEN(3)
        CALL NEWCOLORNAME(COL2)
        XLAB = SMOD(SMAX)
        YLAB = WSF*(WMIN + 0.5*DELW)
        CALL PLMATH(XLAB+6.5*CH,YLAB-0.4*CH,1.2*CH,'a',0., 1)
        CALL PLCHAR(XLAB+7.6*CH,YLAB-0.8*CH,0.9*CH,'i',0., 1)
        CALL NEWPEN(4)
        CALL YAXIS(SMOD(SMAX),WSF*WMIN,WSF*(WMAX-WMIN),WSF*DELW,
     &             WMIN,DELW,-0.9*CH,-2)
      ENDIF
C
C---Plot the CL,CLC/Cref aand Downwash curves
      CALL NEWPEN(4)
      DO N = 1, NSURF
        J  = JFRST(N)
        JLABCLC = J + 0.33*NJ(N)
        JLABDW  = J + 0.50*NJ(N)
        JLABCL  = J + 0.66*NJ(N)
C
        CALL NEWCOLORNAME(COL4)
        CALL XYLINE(NJ(N),SLE(J),CLA_LSTRP(J),SMIN,SSF,0.0,CSF,2)
        IF(LLABSURF) THEN
          XLAB = SMOD(SLE(JLABCL))         
          YLAB = CSF*CLA_LSTRP(JLABCL)         
          CALL PLNUMB(XLAB+0.5*CH,YLAB+0.5*CH,0.8*CH,FLOAT(N),0.0,-1)
        ENDIF
C
        IF(LCLPERPLT) THEN
         CALL NEWCOLORNAME(COL3)
         CALL XYLINE(NJ(N),SLE(J),CLT_LSTRP(J),SMIN,SSF,0.0,CSF,3)
         IF(LLABSURF) THEN
           XLAB = SMOD(SLE(JLABCL))         
           YLAB = CSF*CLT_LSTRP(JLABCL)         
           CALL PLNUMB(XLAB+0.5*CH,YLAB+0.5*CH,0.8*CH,FLOAT(N),0.0,-1)
         ENDIF
        ENDIF
C
        CALL NEWCOLORNAME(COL1)
        CALL XYLINE(NJ(N),SLE(J),CNC(J),SMIN,SSF,0.0,CSF/CREF,1)
        IF(LLABSURF) THEN
          XLAB = SMOD(SLE(JLABCLC))         
          YLAB = CSF/CREF*CNC(JLABCLC)         
          CALL PLNUMB(XLAB+0.5*CH,YLAB+0.5*CH,0.8*CH,FLOAT(N),0.0,-1)
        ENDIF
C
        CALL NEWCOLORNAME(COL2)
        IF(LDWASHPLT ) THEN
          CALL XYLINE(NJ(N),SLE(J),DWWAKE(J),SMIN,SSF,0.0,-WSF,4)
          IF(LLABSURF) THEN
            XLAB = SMOD(SLE(JLABDW))         
            YLAB = -WSF*DWWAKE(JLABDW)         
            CALL PLNUMB(XLAB+0.5*CH,YLAB+0.5*CH,0.8*CH,FLOAT(N),0.0,-1)
          ENDIF
        ENDIF
      END DO
C
C---- Put up curve legends
      CALL NEWPEN(4)
C
      XTMP(1) = 0.75*SMAX 
      XTMP(2) = 0.85*SMAX
      YLAB = MAX(WSF*WMAX,CSF*CMAX) + 12.55*CH
C
      IF(LCLPERPLT) THEN
       CALL NEWCOLORNAME(COL3)
       YTMP(1) = YLAB/CSF
       YTMP(2) = YTMP(1)
       CALL XYLINE(2,XTMP,YTMP,SMIN,SSF,0.0,CSF,3)
       CALL PLCHAR(SMOD(XTMP(2))+1.5*CH,CSF*YTMP(2)-0.5*CH,
     &            1.2*CH,'c'  ,0., 1)
       CALL PLMATH(SMOD(XTMP(2))+2.5*CH,CSF*YTMP(2)-0.9*CH,
     &            0.8*CH, 'V' ,0., 1)
       CALL PLCHAR(SMOD(XTMP(2))+3.8*CH,CSF*YTMP(2)-0.4*CH,
     &            0.7*CH,'T'  ,180.0, 1)
      ENDIF
C
      CALL NEWCOLORNAME(COL4)
      YTMP(1) = YLAB/CSF - YSP*CH/CSF
      YTMP(2) = YTMP(1)
      CALL XYLINE(2,XTMP,YTMP,SMIN,SSF,0.0,CSF,2)
      CALL PLCHAR(SMOD(XTMP(2))+1.5*CH,CSF*YTMP(2)-0.5*CH,
     &            1.2*CH,'c'  ,0., 1)
      CALL PLMATH(SMOD(XTMP(2))+2.5*CH,CSF*YTMP(2)-0.9*CH,
     &            0.8*CH, 'V' ,0., 1)
C
      CALL NEWCOLORNAME(COL1)
      YTMP(1) = YLAB/CSF - 4.4*CH/CSF
      YTMP(2) = YTMP(1)
      CALL XYLINE(2,XTMP,YTMP,SMIN,SSF,0.0,CSF,1)
      CALL PLCHAR(SMOD(XTMP(2))+1.5*CH,CSF*YTMP(2)-0.5*CH,
     &            1.2*CH,'c c/c',0.,5)
      CALL PLCHAR(999.,999.,0.6*CH,'ref',0.,3)
      CALL PLMATH(SMOD(XTMP(2))+2.6*CH,CSF*YTMP(2)-0.9*CH,
     &            0.8*CH, 'V'    ,0.,1)
C
      YTMP(1) = YLAB/CSF - 6.6*CH/CSF
      YTMP(2) = YTMP(1)
      IF(LDWASHPLT) THEN
        CALL NEWCOLORNAME(COL2)
        CALL XYLINE(2,XTMP,YTMP,SMIN,SSF,0.0,CSF,4)
        CALL PLMATH(SMOD(XTMP(2))+1.5*CH,CSF*YTMP(2)-0.7*CH,
     &              1.2*CH,'a',0., 1)
        CALL PLCHAR(SMOD(XTMP(2))+2.6*CH,CSF*YTMP(2)-1.1*CH,
     &              0.9*CH,'i',0., 1)
      ENDIF
C
      CALL NEWPEN(2)
      CALL NEWCOLOR(ICOL0)
C
C---- Trefftz plane plot labels
      XLAB2 = SMOD(SMAX) - 13.0*0.8*CH
      YLAB2 = CSF*CMAX + 0.9*CH
      CALL PLCHAR(XLAB2,YLAB2,0.8*CH,'Trefftz Plane',0.0,-1)
C
      XLAB2 = SMOD(SMAX) - 8.0*0.8*CH
      YLAB2 = CSF*CMAX + 2.5*CH
      CALL PLCHAR(XLAB2,YLAB2,0.8*CH,'AVL ',0.0,4)
      CALL PLNUMB(999.,999.,0.8*CH,VERSION,0.0,2)
C
c      XLAB = SMOD(SMIN) + 45.5*CH
c      YLAB = CSF*CMAX + 1.0*CH
c      CALL PLCHAR(XLAB,YLAB,0.8*CH,SATYPE,0.0,16)
C
      XLAB = SMOD(SMIN)
      YLAB = MAX(WSF*WMAX,CSF*CMAX) + YSP*CH*(8.0+FLOAT(NCONLIN))
C
C     
C---- Label Block with flow condition and forces located above plot
C
C---- Case title, run title
 60   CALL NEWPEN(3)
      CALL PLCHAR(XLAB,YLAB,1.2*CH,TITLE,0.0,LEN(TITLE))
      YLAB = YLAB - YSP*CH
      IF(INDEX(RTITLE(IRUN),'unnamed') .EQ. 0) THEN
       CALL PLCHAR(XLAB,YLAB,1.1*CH,RTITLE(IRUN),0.0,LEN(RTITLE(IRUN)))
      ENDIF
C
      CALL NEWPEN(2)
      XL1 = XLAB
      XL2 = XLAB + 15.0*CH
      XL3 = XLAB + 29.0*CH
      XL4 = XLAB + 45.0*CH
C
      YLAB = YLAB - 0.6*CH
C
C--- Flow condition and forces
      RX = WROT(1)*BREF/2.0
      RY = WROT(2)*CREF/2.0
      RZ = WROT(3)*BREF/2.0
C
C--- Row 1
      YLAB = YLAB - YSP*CH
      YLAB1 = YLAB
      CALL PLMATH(XL1       ,YLAB,1.1*CH,'a'   ,0.0,1)
      CALL PLCHAR(XL1       ,YLAB,CH,'  = ',0.0,4)
      CALL PLNUMB(XL1+4.0*CH,YLAB,CH, ALFA/DTR,0.0,4)
C
      CALL PLCHAR(XL2       ,YLAB,CH,'  CL = ',0.0,7)
      CALL PLNUMB(XL2+7.0*CH,YLAB,CH, CLTOT  ,0.0,4)
C
      CALL PLCHAR(XL3       ,YLAB,CH,'  Cl = ',0.0,7)
      CALL PLMATH(XL3       ,YLAB,CH,'    `'  ,0.0,5)
cc    CALL PLNUMB(XL3+7.0*CH,YLAB,CH, DIR*CRTOT ,0.0,4)
      CALL PLNUMB(XL3+7.0*CH,YLAB,CH, DIR*CRSAX ,0.0,4)
C
C--- Row 2
      YLAB = YLAB - YSP*CH
      CALL PLMATH(XL1        ,YLAB,1.1*CH,'b'   ,0.0,1)
      CALL PLCHAR(XL1        ,YLAB,CH,'  = ',0.0,4)
      CALL PLNUMB(XL1+4.0*CH,YLAB,CH, BETA/DTR,0.0,4)
C
      CALL PLCHAR(XL2       ,YLAB,CH,'  CY = ',0.0,7)
      CALL PLNUMB(XL2+7.0*CH,YLAB,CH, CYTOT   ,0.0,4)
C
      CALL PLCHAR(XL3       ,YLAB,CH,'  Cm = ',0.0,7)
      CALL PLNUMB(XL3+7.0*CH,YLAB,CH, CMTOT(2),0.0,4)
C
C--- Row 3
      YLAB = YLAB - YSP*CH
      CALL PLCHAR(XL1       ,YLAB,CH,'M = ',0.0,4)
      CALL PLNUMB(XL1+4.0*CH,YLAB,CH,AMACH ,0.0,3)
C
      CALL PLCHAR(XL2       ,YLAB,CH,'  CD = ',0.0,7)
      CALL PLNUMB(XL2+7.0*CH,YLAB,CH, CDTOT   ,0.0,5)
C
      CALL PLCHAR(XL3       ,YLAB,CH,'  Cn = ',0.0,7)
      CALL PLMATH(XL3       ,YLAB,CH,'    `'  ,0.0,5)
cc    CALL PLNUMB(XL3+7.0*CH,YLAB,CH, DIR*CMTOT(3), 0.0,4)
      CALL PLNUMB(XL3+7.0*CH,YLAB,CH, DIR*CNSAX, 0.0,4)
C
C--- Row 4
      YLABI = YLAB
      YLABI = YLABI - YSP*CH
      CALL PLCHAR(XL1       ,YLABI,CH,'pb/2V = ',0.0,8)
      CALL PLNUMB(XL1+8.0*CH,YLABI,CH, DIR*RX   ,0.0,4)
C
      CALL PLCHAR(XL2       ,YLABI,CH,'  CD = ',0.0,7)
      CALL PLCHAR(XL2+3.8*CH,YLABI-0.4*CH,
     &                         0.8*CH,'i'      ,0.0,1)
      CALL PLNUMB(XL2+7.0*CH,YLABI,CH, CDFF    ,0.0,5)
C
      CALL PLCHAR(XL3       ,YLABI,CH,'   e = ',0.0,7)
      CALL PLNUMB(XL3+7.0*CH,YLABI,CH, SPANEF  ,0.0,4)
C
C--- Row 5
      YLABI = YLABI - YSP*CH
      CALL PLCHAR(XL1       ,YLABI,CH,'qc/2V = ',0.0,8)
      CALL PLNUMB(XL1+8.0*CH,YLABI,CH, RY   ,0.0,4)
C
      CALL PLCHAR(XL2       ,YLABI,CH,'  CD = ',0.0,7)
      CALL PLCHAR(XL2+3.8*CH,YLABI-0.4*CH,
     &                         0.8*CH,'p'      ,0.0,1)
      CALL PLNUMB(XL2+7.0*CH,YLABI,CH, CDVTOT  ,0.0,5)
C
C---  Row 6
      YLABI = YLABI - YSP*CH
      CALL PLCHAR(XL1       ,YLABI,CH,'rb/2V = ',0.0,8)
      CALL PLNUMB(XL1+8.0*CH,YLABI,CH, DIR*RZ  ,0.0,4)
C
C      YLAB = YLAB - 0.3*CH
      YLAB = YLAB1 + YSP*CH

      NUMD = 0
      DO N = 1, NCONTROL
        CALL STRIP(DNAME(N),NUMDK)
        NUMD = MAX(NUMD,NUMDK)
      ENDDO
      DO N = 1, NCONTROL
        XLAB = XL4
        YLAB = YLAB - YSP*CH
        CALL PLCHAR(XLAB,YLAB,CH,DNAME(N) ,0.0,NUMD)
        CALL PLCHAR(999.,YLAB,CH,' = '    ,0.0,3)
        CALL PLNUMB(999.,YLAB,CH,DELCON(N),0.0,4)
      ENDDO
C
      CALL PLFLUSH
C
C*********************************************************
C
   15 LCOLHC = IDEVH.EQ.4
      WRITE(*,1010) LCLPERPLT, LDWASHPLT, LLABSURF, LCOLHC
   16 WRITE(*,1030)
C
 1010 FORMAT(/' ======================================================'
     &       /'   T plot YZ trace'
     &       /'   Y plot data vs Y'
     &       /'   Z plot data vs Z'
     &       /'   P erpendicular cl plot toggle (currently ',L2,')'
     &       /'   D ownwash angle   plot toggle (currently ',L2,')'
     &      //'   L imits for plot'
     &       /'   R eset plot limits'
     &      //'   N umber surfaces toggle (currently ',L2,')'
     &       /'   C olor hardcopy  toggle (currently ',L2,')'
     &       /'   A nnotate plot'
     &       /'   H ardcopy current plot'
     &      //'   ZM zoom'
     &       /'   U nzoom'
     &       /'   S ize change'/)
 1030 FORMAT( ' Trefftz plot command: ',$)
C
      READ(*,1000) OPT
      CALL TOUPER(OPT) 
 1000 FORMAT(A)
C
      IF(OPT.EQ.' ') THEN
        CALL CLRZOOM
        CALL PLEND
        SIZE = SIZEOLD
        CH = CHOLD
        WRITE(*,*) ' '
        RETURN
C
C---- Reset plot limits
      ELSEIF(OPT.EQ.'R') THEN
        GO TO 50
C
C---- Set plot limits
       ELSE IF(OPT.EQ.'L') THEN
        WRITE(*,*) 'Enter new plot limits (<return> for no change)'
        RINP(1) = SMIN
        RINP(2) = SMAX
        RINP(3) = DELS
        WRITE(*,32) SMIN,SMAX,DELS
 32     FORMAT('    xmin,xmax,xdel: ',3G12.6)
        CALL READR(3,RINP,ERROR)
        IF(ERROR) GO TO 15
        SMIN = RINP(1)
        SMAX = RINP(2)
        DELS = RINP(3)
C
        RINP(1) = CMIN
        RINP(2) = CMAX
        RINP(3) = DELC
        WRITE(*,34) CMIN,CMAX,DELC
 34     FORMAT('    ymin,ymax,ydel: ',3G12.6)
        CALL READR(3,RINP,ERROR)
        IF(ERROR) GO TO 15
        CMIN = RINP(1)
        CMAX = RINP(2)
        DELC = RINP(3)
        GO TO 155
C
C---- Plot trace
      ELSEIF(OPT.EQ.'T') THEN
        PLTTYPE = 'TRACE'
        GO TO 100
C
C---- Use Y as abscissa for plot
      ELSEIF(OPT.EQ.'Y') THEN
        PLTTYPE = 'YZCNC'
        YAXTYPE = 'Y'
        GO TO 100
C
C---- Use Z as abscissa for plot
      ELSEIF(OPT.EQ.'Z') THEN
        PLTTYPE = 'YZCNC'
        YAXTYPE = 'Z'
        GO TO 100
C
C---- Zoom in on plot
      ELSEIF(OPT.EQ.'ZM') THEN
        CALL USETZOOM(.FALSE.,.TRUE.)
        CALL REPLOT(IDEV)
        GO TO 15
C
C---- Reset zoom on plot
      ELSEIF(OPT.EQ.'U') THEN
        CALL CLRZOOM
        CALL REPLOT(IDEV)
C
C---- Set plot size
      ELSEIF(OPT.EQ.'S') THEN
   10   WRITE(*,*)
        WRITE(*,*) 'Currently SIZE = ',SIZE*PLFAC
   12   WRITE(*,5050)
 5050   FORMAT(' Enter new value:  ',$)
        READ (*,*,ERR=12,END=15) SPLF
        PLFAC = SPLF/SIZE
        CALL CLRZOOM
        GO TO 100
C
C---- Number loadings by surface index
      ELSEIF(OPT.EQ.'N') THEN
        LLABSURF = .NOT.LLABSURF
        GO TO 100
C
C---- Annotate plot
      ELSEIF(OPT.EQ.'A') THEN
        WRITE(*,*)
        WRITE(*,*) '================================='
        CALL ANNOT(CH)
        GO TO 15
C
C---- Hardcopy color toggle
      ELSEIF(OPT.EQ.'C') THEN
        LCOLHC = .NOT. LCOLHC
        IF(LCOLHC) THEN
         IDEVH = 4
        ELSE
         IDEVH = 2
        ENDIF
        GO TO 15
C
C---- Hardcopy plot
      ELSEIF(OPT.EQ.'H') THEN
        CALL REPLOT(IDEVH)
C
C---- Toggle display of cl_perp on plot
      ELSEIF(OPT.EQ.'P') THEN
        LCLPERPLT = .NOT.LCLPERPLT
        CALL CLRZOOM
        GO TO 100
C
C---- Toggle display of downwash on plot
      ELSEIF(OPT.EQ.'D') THEN
        LDWASHPLT = .NOT.LDWASHPLT
        CALL CLRZOOM
        GO TO 100
C
      ENDIF
C
      GO TO 16
      END ! PLOTTP

