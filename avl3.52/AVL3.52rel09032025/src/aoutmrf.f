C***********************************************************************
C    Module:  aoutmrf.f
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

      SUBROUTINE MRFTOT(LUN, FILEID)
C
C...PURPOSE  To print out results of the vortex lattice calculation
C            for the input configuration.
C
C...INPUT    Configuration data for case in labeled commons
C
C...OUTPUT   Machine-readable format output on logical unit LUN
C
      INCLUDE 'AVL.INC'
      CHARACTER*(*) FILEID
      CHARACTER*50 SATYPE

      write(*,'(a,a)') 'mrftot: fileid = ', fileid
C
      IF (LUN.EQ.0) RETURN
C
      CALL GETSA(LNASA_SA,SATYPE,DIR)
C
      CA = COS(ALFA)
      SA = SIN(ALFA)
C
C---- set normalized rates in Stability or Body axes
      RX_S = (WROT(1)*CA + WROT(3)*SA) * BREF/2.0
      RY_S =  WROT(2)                  * CREF/2.0
      RZ_S = (WROT(3)*CA - WROT(1)*SA) * BREF/2.0
      RX_B =  WROT(1) * BREF/2.0
      RY_B =  WROT(2) * CREF/2.0
      RZ_B =  WROT(3) * BREF/2.0
C
C---- set body forces in Geometric axes
      CXTOT = CDTOT*CA - CLTOT*SA
      CZTOT = CDTOT*SA + CLTOT*CA
C
C---- set moments in stability axes
      CRSAX = CMTOT(1)*CA + CMTOT(3)*SA
      CMSAX = CMTOT(2)
      CNSAX = CMTOT(3)*CA - CMTOT(1)*SA
C
C---- dump it
      WRITE(LUN,'(A)') FILEID
      WRITE(LUN,'(A)') 'VERSION 1.0'
C
      WRITE(LUN,'(A)') 'Vortex Lattice Output -- Total Forces'
      WRITE(LUN,'(A)') TITLE(1:60)
      WRITE(LUN,'(I6,6X,A)') NSURF,  '| # surfaces'
      WRITE(LUN,'(I6,6X,A)') NSTRIP, '| # strips'
      WRITE(LUN,'(I6,6X,A)') NVOR,   '| # vortices'
      IF(IYSYM.GT.0) THEN
       WRITE(LUN,'(ES23.15,6X,A)') YSYM, '| Y Symmetry: Wall plane'
      ENDIF
      IF(IYSYM.LT.0) THEN
       WRITE(LUN,'(ES23.15,6X,A)') YSYM, '| Y Symmetry: Free surface'
      ENDIF
      IF(IZSYM.GT.0) THEN
       WRITE(LUN,'(ES23.15,6X,A)') ZSYM, '| Z Symmetry: Ground plane'
      ENDIF
      IF(IZSYM.LT.0) THEN
       WRITE(LUN,'(ES23.15,6X,A)') ZSYM, '| Z Symmetry: Free surface'
      ENDIF
C
      WRITE(LUN,'(3(ES23.15),6X,A)') SREF, CREF, BREF,
     &  '| Sref, Cref, Bref'
      WRITE(LUN,'(3(ES23.15),6X,A)') XYZREF(1), XYZREF(2), XYZREF(3),
     &  '| Xref, Yref, Zref'
C
      WRITE(LUN,'(A)') SATYPE
C
      WRITE(LUN,'(A)') RTITLE(IRUN)
      WRITE(LUN,'(3(ES23.15),6X,A)') ALFA/DTR, DIR*RX_B, DIR*RX_S,
     &  '| Alpha, pb/2V, p''b/2V'
      WRITE(LUN,'(2(ES23.15),6X,A)') BETA/DTR,     RY_B,
     &  '| Beta, qc/2V'
      WRITE(LUN,'(3(ES23.15),6X,A)') AMACH   , DIR*RZ_B, DIR*RZ_S,
     &  '| Mach, rb/2V, r''b/2V'
C
      CDITOT = CDTOT - CDVTOT
      WRITE(LUN,'(3(ES23.15),6X,A)')
     &      DIR*CFTOT(1), DIR*CMTOT(1), DIR*CRSAX,
     &  '| CXtot, Cltot, Cl''tot'
      WRITE(LUN,'(2(ES23.15),6X,A)')     CFTOT(2),     CMTOT(2),
     &  '| CYtot, Cmtot'
      WRITE(LUN,'(3(ES23.15),6X,A)')
     &      DIR*CFTOT(3), DIR*CMTOT(3), DIR*CNSAX,
     &  '| CZtot, Cntot, Cn''tot'
      WRITE(LUN,'(1(ES23.15),6X,A)') CLTOT ,
     &  '| CLtot'
      WRITE(LUN,'(1(ES23.15),6X,A)') CDTOT ,
     &  '| CDtot'
      WRITE(LUN,'(2(ES23.15),6X,A)') CDVTOT, CDITOT,
     &  '| CDvis, CDind'
      WRITE(LUN,'(4(ES23.15),6X,A)') CLFF, CDFF, CYFF, SPANEF,
     &  '| Trefftz Plane: CLff, CDff, CYff, e'
C
      WRITE(LUN,'(A)') 'CONTROL'
      WRITE(LUN,'(I6)') NCONTROL
      DO K = 1, NCONTROL
        WRITE(LUN,'(ES23.15,2X,A)') DELCON(K), DNAME(K)
      ENDDO

      WRITE(LUN,'(A)') 'DESIGN'
      WRITE(LUN,'(I6)') NDESIGN
      DO K = 1, NDESIGN
        WRITE(LUN,'(ES23.15,2X,A)') DELDES(K), GNAME(K)
      ENDDO
C
      RETURN
      END ! MRFTOT


      SUBROUTINE MRFSURF(LUN)
C
C...PURPOSE  To print out surface forces from the vortex lattice calculation
C
C...INPUT    Configuration data for case in labeled commons
C
C...OUTPUT   Machine-readable format output for each surface on logical unit LUN
C
      INCLUDE 'AVL.INC'
      CHARACTER*50 SATYPE
C
      IF (LUN.EQ.0) RETURN
C
      CALL GETSA(LNASA_SA,SATYPE,DIR)
C
C...Print out the results
C
      WRITE(LUN,'(A)') 'SURF'
      WRITE(LUN,'(A)') 'VERSION 1.0'
C
      WRITE(LUN,'(A)') SATYPE
      WRITE(LUN,'(3(ES23.15),6X,A)') SREF, CREF, BREF,
     &  '| Sref, Cref, Bref'
      WRITE(LUN,'(3(ES23.15),6X,A)') XYZREF(1), XYZREF(2), XYZREF(3),
     &  '| Xref, Yref, Zref'
C
      WRITE(LUN,'(I6,6X,A)') NSURF, '| # surfaces'
      DO N = 1, NSURF
        CALL STRIP(STITLE(N),NT)

        WRITE(LUN,'(A)') 'SURFACE'
        WRITE(LUN,'(A)') STITLE(N)(1:NT)
C
C...Force components from each surface (stability axes forces)
        WRITE(LUN,'(I3,1X,9(ES23.15),1X,A,A,A)')
     &    N,SSURF(N),CLSURF(N),CDSURF(N),CMSURF(2,N),
     &    CYSURF(N),DIR*CMSURF(3,N),DIR*CMSURF(1,N),
     &    CDSURF(N)-CDVSURF(N),CDVSURF(N),
     &    '| Surface Forces (referred to Sref,Cref,Bref, ',
     &    'moments in body axes about Xref,Yref,Zref) : ',
     &    'n Area CL CD Cm CY Cn Cl CDi CDv'
C
C--- Surface forces normalized by local reference quantities
        WRITE(LUN,'(I3,1X,5(ES23.15),1X,A,A,A)')
     &    N,SSURF(N),CAVESURF(N),
     &    CL_LSRF(N),CD_LSRF(N),
     &    CDVSURF(N)*SREF/SSURF(N),
     &    '| Surface Forces (referred to Ssurf, Cave ',
     &    'about root LE on hinge axis) : ',
     &    'n Ssurf Cave cl cd cdv'
      END DO

C
      RETURN
      END ! MRFSURF


      SUBROUTINE MRFBODY(LUN)
C
C...PURPOSE  To print out body forces from source/doublet calculation
C
C...INPUT    Configuration data for case in labeled commons
C
C...OUTPUT   Machine-readable format output for each body on logical unit LUN
C
      INCLUDE 'AVL.INC'
      CHARACTER*50 SATYPE
C
 1000 FORMAT (A)
C
      IF (LUN.EQ.0) RETURN
C
      CALL GETSA(LNASA_SA,SATYPE,DIR)
C
C...Print out the results
C
      WRITE(LUN,'(A)') 'BODY'
      WRITE(LUN,'(A)') 'VERSION 1.0'
C
      WRITE(LUN,'(A)') SATYPE
      WRITE(LUN,'(3(ES23.15),6X,A)') SREF, CREF, BREF,
     &  '| Sref, Cref, Bref'
      WRITE(LUN,'(3(ES23.15),6X,A)') XYZREF(1), XYZREF(2), XYZREF(3),
     &  '| Xref, Yref, Zref'
C
      WRITE(LUN,'(I4,1X,A)') NBODY, '| # bodies'
      DO IB = 1, NBODY
        CALL STRIP(BTITLE(N),NT)

        WRITE(LUN,'(A)') 'BODY'
        WRITE(LUN,'(A)') BTITLE(N)(1:NT)
C
        ELBD = ELBDY(IB)
        SBDY = SRFBDY(IB)
        VBDY = VOLBDY(IB)
        WRITE(LUN,'(I4,1X,9(ES23.15),1X,A,A,A)')
     &    IB,ELBD,SBDY,VBDY,
     &    CLBDY(IB),CDBDY(IB),CMBDY(2,IB),
     &    CFBDY(2,IB),DIR*CMBDY(3,IB),DIR*CMBDY(1,IB),
     &    '| Body Forces (referred to Sref,Cref,Bref ',
     &    'about Xref,Yref,Zref) : ',
     &    'Ibdy Length Asurf Vol CL CD Cm CY Cn Cl'
      ENDDO
C
      RETURN
      END ! MRFBODY


      SUBROUTINE MRFSTRP(LUN)
C
C...PURPOSE  To print out results of the vortex lattice calculation
C            for the input configuration strip and surface forces.
C
C...INPUT    Configuration data for case in labeled commons
C
C...OUTPUT   Machine-readable format output on logical unit LUN
C
      INCLUDE 'AVL.INC'
      CHARACTER*50 SATYPE
C
      IF (LUN.EQ.0) RETURN
C
      CALL GETSA(LNASA_SA,SATYPE,DIR)
C
C...Print out the results -> Forces by surface and strip
C
      WRITE(LUN,'(A)') 'STRP'
      WRITE(LUN,'(A)') 'VERSION 1.0'
C
      WRITE(LUN,'(A)') SATYPE
      WRITE(LUN,'(3(ES23.15),6X,A)') SREF, CREF, BREF,
     &  '| Sref, Cref, Bref'
      WRITE(LUN,'(3(ES23.15),6X,A)') XYZREF(1), XYZREF(2), XYZREF(3),
     &  '| Xref, Yref, Zref'
C
      WRITE(LUN,'(A,A)') 'Surface and Strip Forces by surface',
     &  ' (referred to Sref,Cref,Bref about Xref,Yref,Zref)'
C
      WRITE(LUN,'(I6,6X,A)') NSURF, '| surfaces'
      DO N = 1, NSURF
        NS = NJ(N)
        NV = NK(N)
        J1 = JFRST(N)
C
        WRITE(LUN,'(A)') 'SURFACE'
        WRITE(LUN,'(A)') STITLE(N)
        WRITE(LUN,'(4(I4,1X),3X,A)')
     &    N,NV,NS,J1,
     &    '| Surface #, # Chordwise, # Spanwise, First strip'
        WRITE(LUN,'(2(ES23.15),3X,A)')
     &    SSURF(N),CAVESURF(N),
     &    '| Surface area Ssurf, Ave. chord Cave'
C
        CDISURF = CDSURF(N)-CDVSURF(N)
        WRITE(LUN,'(8(ES23.15),3X,A,A,A)')
     &    CLSURF(N),DIR*CMSURF(1,N),
     &    CYSURF(N),    CMSURF(2,N),
     &    CDSURF(N),DIR*CMSURF(3,N),
     &    CDISURF,CDVSURF(N),
     &    '| CLsurf, Clsurf, CYsurf, Cmsurf, ',
     &    'CDsurf, Cnsurf, CDisurf, CDvsurf',
     &    '; Forces referred to Sref, Cref, Bref about Xref, Yref, Zref'
C
        WRITE(LUN,'(2(ES23.15),3X,A,A)')
     &    CL_LSRF(N),CD_LSRF(N),
     &    '| CL_srf CD_srf',
     &    '; Forces referred to Ssurf, Cave'
C
        WRITE(LUN,'(A)') 'Strip Forces referred to Strip Area, Chord'
        WRITE(LUN,'(A,A)') 'j, Xle, Yle, Zle, Chord, Area, c_cl, ai, ',
     &    'cl_perp, cl, cd, cdv, cm_c/4, cm_LE, C.P.x/c'
        DO JJ = 1, NS
          J = J1 + JJ-1
          ASTRP = WSTRIP(J)*CHORD(J)
          XCP = 999.
          IF(CL_LSTRP(J).NE.0.)  XCP = 0.25 - CMC4_LSTRP(J)/CL_LSTRP(J)
          WRITE(LUN,'(I4,14(ES23.15))')
     &      J,RLE(1,J),RLE(2,J),RLE(3,J),
     &      CHORD(J),ASTRP,CNC(J),DWWAKE(J),
     &      CLT_LSTRP(J),CL_LSTRP(J),CD_LSTRP(J),CDV_LSTRP(J),
     &      CMC4_LSTRP(J),CMLE_LSTRP(J),XCP
        END DO
      END DO
C
      RETURN
      END ! MRFSTRP


      SUBROUTINE MRFELE(LUN)
      INCLUDE 'AVL.INC'
      CHARACTER*50 SATYPE
C
      IF (LUN.EQ.0) RETURN
C
      CALL GETSA(LNASA_SA,SATYPE,DIR)
C
      WRITE(LUN,'(A)') 'ELE'
      WRITE(LUN,'(A)') 'VERSION 1.0'
C
      WRITE(LUN,'(A)') SATYPE
      WRITE(LUN,'(3(ES23.15),6X,A)') SREF, CREF, BREF,
     &  '| Sref, Cref, Bref'
      WRITE(LUN,'(3(ES23.15),6X,A)') XYZREF(1), XYZREF(2), XYZREF(3),
     &  '| Xref, Yref, Zref'
C
C...Forces on each strip and element (long output, and slow to printout)
      WRITE(LUN,'(A)') 'Vortex Strengths (by surface, by strip)'
C
      WRITE(LUN,'(I6,6X,A)') NSURF, '| # surfaces'
      DO N = 1, NSURF
        NS = NJ(N)
        NV = NK(N)
        J1 = JFRST(N)
C
        WRITE(LUN,'(A)') 'SURFACE'
        WRITE(LUN,'(A)') STITLE(N)
        WRITE(LUN,'(4(I4,1X),3X,A)')
     &    N,NV,NS,J1,
     &    '| Surface #, # Chordwise, # Spanwise, First strip'
        WRITE(LUN,'(2(ES23.15),3X,A)')
     &    SSURF(N),CAVESURF(N),
     &    '| Surface area, Ave. chord'
C
        CDISURF = CDSURF(N)-CDVSURF(N)
        WRITE(LUN,'(8(ES23.15),3X,A,A)')
     &    CLSURF(N),DIR*CMSURF(1,N),
     &    CYSURF(N),    CMSURF(2,N),
     &    CDSURF(N),DIR*CMSURF(3,N),
     &    CDISURF,CDVSURF(N),
     &    '| CLsurf, Clsurf, CYsurf, Cmsurf, ',
     &    'CDsurf, Cnsurf, CDisurf, CDvsurf'
        WRITE(LUN,'(2(ES23.15),3X,A,A)')
     &    CL_LSRF(N),CD_LSRF(N),
     &    '| CL_srf CD_srf',
     &    '; Forces referred to Ssurf, Cave about hinge axis thru LE'
C
        DO JJ = 1, NS
          J = J1 + JJ-1
          I1 = IJFRST(J)
          ASTRP = WSTRIP(J)*CHORD(J)
          DIHED = -ATAN2(ENSY(J),ENSZ(J))/DTR
C
          WRITE(LUN,'(A)') 'STRIP'
          WRITE(LUN,'(3(I4),2X,A)') J,NV,I1,
     &      '| Strip #, # Chordwise, First Vortex'
          WRITE(LUN,'(8(ES23.15),2X,A,A)')
     &      RLE(1,J),CHORD(J),AINC(J)/DTR,
     &      RLE(2,J),WSTRIP(J),ASTRP,
     &      RLE(3,J),DIHED,
     &      '| Xle, Ave. Chord, Incidence (deg), Yle, ',
     &      'Strip Width, Strip Area, Zle, Strip Dihed (deg)'
          WRITE(LUN,'(9(ES23.15),2X,A)')
     &      CL_LSTRP(J), CD_LSTRP(J), CDV_LSTRP(J),
     &      CN_LSTRP(J), CA_LSTRP(J),
     &      CNC(J),    DWWAKE(J),
     &      CMLE_LSTRP(J),   CMC4_LSTRP(J),
     &      '| cl, cd, cdv, cn, ca, cnc, wake dnwsh, cmLE, cm c/4'
C
          !!WRITE(LUN,'(I4,2X,A)') NV, '| # vortices'
          DO II = 1, NV
            I = I1 + (II-1)
            XM = 0.5*(RV1(1,I)+RV2(1,I))
            YM = 0.5*(RV1(2,I)+RV2(2,I))
            ZM = 0.5*(RV1(3,I)+RV2(3,I))
            WRITE(LUN,'(I4,6(ES23.15),2X,A)')
     &        I,XM,YM,ZM,DXV(I),SLOPEC(I),DCP(I),
     &        '| I, X, Y, Z, DX, Slope, dCp'
          END DO
        END DO
      END DO
C
      END ! MRFELE


      SUBROUTINE MRFHINGE(LUN)
C
C...PURPOSE  To print out results of the vortex lattice calculation
C            for the input configuration.
C
C...INPUT    Configuration data for case in labeled commons
C
C...OUTPUT   Machine-readable format output on logical unit LUN
C
      INCLUDE 'AVL.INC'
      CHARACTER*50 SATYPE
C
      IF (LUN.EQ.0) RETURN
C
      CALL GETSA(LNASA_SA,SATYPE,DIR)
C
C...Print out the results
C
      WRITE(LUN,'(A)') 'HINGE'
      WRITE(LUN,'(A)') 'VERSION 1.0'
C
      WRITE(LUN,'(A)') SATYPE
      WRITE(LUN,'(2(ES23.15),6X,A)') SREF, CREF,
     &  '| Sref, Cref'
C
C...Hinge moments for each CONTROL
      !!WRITE(LUN,'(A)') 'Control Hinge Moments (referred to Sref, Cref)'
      WRITE(LUN,'(I4,2X,A)') NCONTROL, '| # controls'
      DO N = 1, NCONTROL
        WRITE(LUN,'(ES23.15,2X,A,2X,A,A)') CHINGE(N), DNAME(N),
     &    '| Control Hinge Moments (referred to Sref, Cref) : ',
     &    'Chinge, Control'
      END DO
C
      RETURN
      END ! MRFHINGE


      SUBROUTINE MRFCNC(LUN)
C
C...PURPOSE  To write out a CNC loading file
C            for the input configuration strips
C
C...INPUT    Configuration data for case in labeled commons
C
C...OUTPUT   Machine-readable format output on logical unit LUN
C
      INCLUDE 'AVL.INC'
      CHARACTER*1 ANS
      CHARACTER*256 FNAM
      SAVE FNAM
      DATA FNAM /' '/
C
      IF (LUN.EQ.0) RETURN
C
C...Print out the results -> strip loadings
C
      WRITE(LUN,'(A)') 'CNC'
      WRITE(LUN,'(A)') 'VERSION 1.0'
C
      WRITE(LUN,'(A,A)') 'Strip Loadings: ',
     &  ' XM, YM, ZM, CNCM, CLM, CHM, DYM, ASM'
      WRITE(LUN,'(I4,3X,A)') NSTRIP, '| # strips'
C
      DO J=1, NSTRIP
        I = IJFRST(J)
        XM = 0.5*(RV1(1,I)+RV2(1,I))
        YM = 0.5*(RV1(2,I)+RV2(2,I))
        ZM = 0.5*(RV1(3,I)+RV2(3,I))
        CNCM = CNC(J)
        CLM  = CL_LSTRP(J)
        CHM  = CHORD(J)
        DYM  = WSTRIP(J)
        ASM  = DYM*CHM
        WRITE(LUN,'(8(ES23.15))') XM,YM,ZM,CNCM,CLM,CHM,DYM,ASM
      END DO
C
      RETURN
      END ! MRFCNC


      SUBROUTINE MRFVM(LU)
C...PURPOSE  Print STRIP SHEAR and BENDING QUANTITIES, ie. V, BM
C            Integrates spanload to get shear and bending moment
C     NOTE:  only works for single surface at at time (ie. V,BM on each panel)
C
      INCLUDE 'AVL.INC'
      REAL V(NSMAX), BM(NSMAX), YSTRP(NSMAX)
      CHARACTER*256 FNAMVB
C
      IF (LU.EQ.0) RETURN
C
      WRITE(LU,'(A)') 'VM'
      WRITE(LU,'(A)') 'VERSION 1.0'
C
      WRITE(LU,'(A)') 'Shear/q and Bending Moment/q vs Y'
      WRITE(LU,'(A)') TITLE(1:60)
      WRITE(LU,'(6(ES23.15),2X,A)')
     &  AMACH,ALFA/DTR,CLTOT,BETA/DTR,SREF,BREF,
     &  '| Mach, alpha, CLtot, beta, Sref, Bref'
C
C---- Process the surfaces one by one, calculating shear and bending on each,
C      with moments refered to Y=0 (centerline)
C
      WRITE(LU,'(I6,6X,A)') NSURF, '| # surfaces'
C
      DO N = 1, NSURF
        J1 = JFRST(N)
        JN = J1 + NJ(N) - 1
C
        WRITE(LU,'(A)') 'SURFACE'
        WRITE(LU,'(A)') STITLE(N)
        WRITE(LU,'(2(I4,1X),3X,A)')
     &    N, NJ(N),
     &    '| Surface #, # strips'
C
        YMIN =  1.0E10
        YMAX = -1.0E10
        DO J = J1, JN
          IV = IJFRST(J)
          YMIN = MIN(YMIN,RV1(2,IV),RV2(2,IV))
          YMAX = MAX(YMAX,RV1(2,IV),RV2(2,IV))
        ENDDO
C
C------ Integrate spanload from first strip to last strip defined for
C        this surface to get shear and bending moment
        CNCLST = 0.0
        BMLST  = 0.0
        WLST   = 0.0
        VLST   = 0.0
C
        JF = J1
        JL = JN
        JINC = 1
C
C------ Integrate from first to last strip in surface
        DO J = JL, JF, -JINC
          JJ = JINC*(J - JF + JINC)
C
          DY = 0.5*(WSTRIP(J)+WLST)
          YSTRP(JJ) = RLE(2,J)
          V(JJ)     = VLST  + 0.5*(CNC(J)+CNCLST) * DY
          BM(JJ)    = BMLST + 0.5*(V(JJ)+VLST)    * DY
C
          VLST   = V(JJ)
          BMLST  = BM(JJ)
          CNCLST = CNC(J)
          WLST   = WSTRIP(J)
        ENDDO
C
C------ Inboard edge Y,Vz,Mx
        VROOT  = VLST  +      CNCLST      * 0.5*DY
        BMROOT = BMLST + 0.5*(VROOT+VLST) * 0.5*DY
        VTIP   = 0.0
        BMTIP  = 0.0
        IF(IMAGS(N).GE.0) THEN
         YROOT = RLE1(2,J1)
         YTIP  = RLE2(2,JN)
        ELSE
         YROOT = RLE2(2,J1)
         YTIP  = RLE1(2,JN)
        ENDIF
C
        DIR = 1.0
        IF(YMIN+YMAX.LT.0.0) DIR = -1.0
C
        WRITE(LU,'(2(ES23.15),2X,A)') 2.0*YMIN/BREF, 2.0*YMAX/BREF,
     &    '| 2Ymin/Bref, 2Ymax/Bref'
C
        WRITE(LU,'(3(ES23.15),2X,A)')
     &    2.*YROOT/BREF,VROOT/SREF,DIR*BMROOT/SREF/BREF,
     &    '| 2Y/Bref, Vz/(q*Sref), Mx/(q*Bref*Sref) : root'
        DO J = 1, NJ(N)
          WRITE(LU,'(3(ES23.15),2X,A)')
     &      2.*YSTRP(J)/BREF,V(J)/SREF,DIR*BM(J)/SREF/BREF,
     &      '| 2Y/Bref, Vz/(q*Sref), Mx/(q*Bref*Sref)'
        ENDDO
        WRITE(LU,'(3(ES23.15),2X,A)')
     &    2.*YTIP/BREF,VTIP/SREF,DIR*BMTIP/SREF/BREF,
     &    '| 2Y/Bref, Vz/(q*Sref), Mx/(q*Bref*Sref) : tip'
      ENDDO
C
      RETURN
      END ! MRFVM
