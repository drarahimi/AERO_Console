C***********************************************************************
C    Module:  aero.f
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

      SUBROUTINE AERO
C---- Calculate forces - inviscid forces from horseshoe vortices,
C     inviscid forces from bodies, viscous forces from drag and 
C     far-field forces in the Trefftz plane
C
      INCLUDE 'AVL.INC'
C
      CDTOT = 0.
      CYTOT = 0.
      CLTOT = 0.
      DO L = 1, 3
        CFTOT(L) = 0.
        CMTOT(L) = 0.
      ENDDO
      CDVTOT = 0.
C
      CDTOT_A = 0.
      CLTOT_A = 0.
C
      DO L = 1, NCONTROL
        CHINGE(L) = 0.
      ENDDO
C
      DO N=1, NUMAX
        CDTOT_U(N) = 0.
        CYTOT_U(N) = 0.
        CLTOT_U(N) = 0.
        DO L = 1, 3
          CFTOT_U(L,N) = 0.
          CMTOT_U(L,N) = 0.
        ENDDO
        DO L = 1, NCONTROL
          CHINGE_U(L,N) = 0.
        ENDDO
      ENDDO
C
      DO N=1, NCONTROL
        CDTOT_D(N) = 0.
        CYTOT_D(N) = 0.
        CLTOT_D(N) = 0.
        DO L = 1, 3
          CFTOT_D(L,N) = 0.
          CMTOT_D(L,N) = 0.
        ENDDO
        DO L = 1, NCONTROL
          CHINGE_D(L,N) = 0.
        ENDDO
      ENDDO
C
      DO N=1, NDESIGN
        CDTOT_G(N) = 0.
        CYTOT_G(N) = 0.
        CLTOT_G(N) = 0.
        DO L = 1, 3
          CFTOT_G(L,N) = 0.
          CMTOT_G(L,N) = 0.
        ENDDO
        DO L = 1, NCONTROL
          CHINGE_G(L,N) = 0.
        ENDDO
      ENDDO
C
C---------------------------------------------------------
C--- Evaluate forces on surface, bodies and Trefftz plane  
      CALL SFFORC
      CALL BDFORC
      CALL TPFORC
C
C---------------------------------------------------------
C--- If case is XZ symmetric (IYSYM=1), add contributions from images,
C    zero out the asymmetric forces and double the symmetric ones
C
      IF(IYSYM.EQ.1) THEN
C
        CDTOT   = 2.0 * CDTOT
        CYTOT   = 0.
        CLTOT   = 2.0 * CLTOT
        CFTOT(1)   = 2.0 * CFTOT(1)
        CFTOT(2)   = 0.0
        CFTOT(3)   = 2.0 * CFTOT(3)
        CMTOT(1)   = 0.
        CMTOT(2)   = 2.0 * CMTOT(2)
        CMTOT(3)   = 0.
        CDVTOT  = 2.0 * CDVTOT
C
        CDTOT_A = 2.0 * CDTOT_A
        CLTOT_A = 2.0 * CLTOT_A
C
        DO N=1, NUMAX
          CDTOT_U(N) = 2.0 * CDTOT_U(N)
          CYTOT_U(N) = 0.
          CLTOT_U(N) = 2.0 * CLTOT_U(N)
          CFTOT_U(1,N) = 2.0 * CFTOT_U(1,N)
          CFTOT_U(2,N) = 0.0
          CFTOT_U(3,N) = 2.0 * CFTOT_U(3,N)
          CMTOT_U(1,N) = 0.
          CMTOT_U(2,N) = 2.0 * CMTOT_U(2,N)
          CMTOT_U(3,N) = 0.
        ENDDO
C
        DO N=1, NCONTROL
          CDTOT_D(N) = 2.0 * CDTOT_D(N)
          CYTOT_D(N) = 0.
          CLTOT_D(N) = 2.0 * CLTOT_D(N)
          CFTOT_D(1,N) = 2.0 * CFTOT_D(1,N)
          CFTOT_D(2,N) = 0.
          CFTOT_D(3,N) = 2.0 * CFTOT_D(3,N)
          CMTOT_D(1,N) = 0.
          CMTOT_D(2,N) = 2.0 * CMTOT_D(2,N)
          CMTOT_D(3,N) = 0.
        ENDDO
C
        DO N=1, NDESIGN
          CDTOT_G(N) = 2.0 * CDTOT_G(N)
          CYTOT_G(N) = 0.
          CLTOT_G(N) = 2.0 * CLTOT_G(N)
          CFTOT_G(1,N) = 2.0 * CFTOT_G(1,N)
          CFTOT_G(2,N) = 0.
          CFTOT_G(3,N) = 2.0 * CFTOT_G(3,N)
          CMTOT_G(1,N) = 0.
          CMTOT_G(2,N) = 2.0 * CMTOT_G(2,N)
          CMTOT_G(3,N) = 0.
       ENDDO
C
      ENDIF
C
C---------------------------------------------------------
C---- add baseline reference CD to totals
C     force in direction of freestream
C
c      SINA = SIN(ALFA)
c      COSA = COS(ALFA)
C
      VSQ = VINF(1)**2 + VINF(2)**2 + VINF(3)**2
      VMAG = SQRT(VSQ)
C
ccc   print *,"VINF,VSQ,VMAG ",VINF,VSQ,VMAG
ccc   print *,"CDTOT,CDVTOT,CDREF ",CDTOT,CDVTOT,CDREF
      CDVTOT = CDVTOT + CDREF*VSQ
      CDTOT = CDTOT + CDREF*VSQ
      CYTOT = CYTOT + CDREF*VINF(2)*VMAG
      DO L = 1, 3
        CFTOT(L) = CFTOT(L) + CDREF*VINF(L)*VMAG
        CFTOT_U(L,L) = CFTOT_U(L,L) + CDREF*VMAG
      ENDDO
C 
      DO IU = 1, 3
        CDTOT_U(IU) = CDTOT_U(IU) + CDREF*2.0*VINF(IU)
        DO L = 1, 3
          CFTOT_U(L,IU) = CFTOT_U(L,IU) + CDREF*VINF(L)*VINF(IU)/VMAG
        ENDDO
      ENDDO
ccc      print *,"CFTOT ",CFTOT
C
      RETURN
      END ! AERO



      SUBROUTINE SFFORC
C
C...PURPOSE  To calculate the forces on the configuration,
C            by vortex, strip and surface.
C
C...INPUT    Global Data in labelled commons, defining configuration
C            ALFA       Angle of attack (for stability-axis definition)
C            VINF()     Freestream velocity components
C            WROT()     Roll,Pitch,Yaw  rates
C            MACH       Mach number
C            NVOR       Number of vortices
C            R1         Coordinates of endpoint #1 of bound vortex
C            R2         Coordinates of endpoint #2 of bound vortex
C            ENV        Normal vector at bound vortex midpoint
C            DX         X-length of vortex lattice panel
C            NVSTRP     No. of vortices in strip
C          
C...OUTPUT   DCP                   Vortex element loadings
C            CXYZTOT                Total force,moment coefficients
C            CDFF                  Far-field drag (Trefftz plane)
C            CxxSURF               Surface force,moment coefficients
C            CxxSTRP               Strip force coefficients
C            CNC                   Span load for each strip
C
C...COMMENTS   
C
      INCLUDE 'AVL.INC'
C
      REAL RC4(3),  RROT(3)
      REAL VEFF(3), VEFF_U(3,6), VEFFMAG_U(NUMAX)
      REAL VEFF_D(3,NDMAX), VEFF_G(3,NGMAX)
      REAL VROT(3), VROT_U(3), WROT_U(3)
      REAL VPERP(3)
      REAL G(3), R(3), RH(3), MH(3)
      REAL F(3), F_U(3,6), F_D(3,NDMAX), F_G(3,NGMAX)
      REAL FGAM(3), FGAM_U(3,6), FGAM_D(3,NDMAX), FGAM_G(3,NGMAX)
      REAL ENAVE(3), SPN(3)
      REAL ULIFT(3), ULIFT_U(3,NUMAX)
      REAL ULIFT_D(3,NDMAX), ULIFT_G(3,NGMAX)
      REAL UDRAG(3), UDRAG_U(3,NUMAX)
      REAL ULMAG_U(NUMAX)
C
      REAL CFX, CFY, CFZ, CMX, CMY, CMZ
      REAL CFX_U(NUMAX), CFY_U(NUMAX), CFZ_U(NUMAX),
     &     CMX_U(NUMAX), CMY_U(NUMAX), CMZ_U(NUMAX), 
     &     CFX_D(NDMAX), CFY_D(NDMAX), CFZ_D(NDMAX),
     &     CMX_D(NDMAX), CMY_D(NDMAX), CMZ_D(NDMAX),
     &     CFX_G(NGMAX), CFY_G(NGMAX), CFZ_G(NGMAX),
     &     CMX_G(NGMAX), CMY_G(NGMAX), CMZ_G(NGMAX),
     &     CLV_U(NUMAX),
     &     CLV_D(NDMAX),
     &     CLV_G(NGMAX)
C
c---- indices for forming cross-products
      INTEGER ICRS(3), JCRS(3)
      DATA ICRS / 2, 3, 1 / , JCRS / 3, 1, 2 /
C
      SINA = SIN(ALFA)
      COSA = COS(ALFA)
C
C***********************************************************************
C...Integrate the forces strip-wise, surface-wise and total-wise
C***********************************************************************
C
C...Calculate strip forces...
C    normalized to strip reference quantities (strip area, chord)
      DO 100 J = 1, NSTRIP
C
        I1  = IJFRST(J)
        NVC = NVSTRP(J)
C
        CR = CHORD(J)
        SR = CHORD(J)*WSTRIP(J)
C
        XTE1 = RLE1(1,J) + CHORD1(J)
        XTE2 = RLE2(1,J) + CHORD2(J)
C     
C--- Define local strip lift and drag directions
C--- The "spanwise" vector is cross product of strip normal with X chordline 
        SPN(1) =  0.0
        SPN(2) =  ENSZ(J)
        SPN(3) = -ENSY(J)
C---  Wind axes stream vector defines drag direction
C     HHY 02272024 Changed, used to be stability axis
        DO K = 1, 3
          UDRAG(K) = VINF(K)
          DO N = 1, NUMAX
            UDRAG_U(K,N) = 0.
          ENDDO
          UDRAG_U(K,K) = 1.0
        ENDDO          

C--- Lift direction is vector product of "stream" and spanwise vector
        CALL CROSS(UDRAG,SPN,ULIFT)
        ULMAG = SQRT(DOT(ULIFT,ULIFT))
cc        write(6,*) 'ULIFT0 ',ULIFT,' ULMAG0 ',ULMAG
        IF(ULMAG.EQ.0.) THEN

           ULIFT(1) = 0.0
           ULIFT(2) = 0.0
           ULIFT(3) = 1.0
           DO N = 1, NUMAX
             ULIFT_U(1,N) = 0.
             ULIFT_U(2,N) = 0.
             ULIFT_U(3,N) = 0.
          ENDDO
          
         ELSE

          DO K = 1, 3
           IC = ICRS(K)
           JC = JCRS(K)
           ULIFT(K) = UDRAG(IC)*SPN(JC) - UDRAG(JC)*SPN(IC)
           DO N = 1, NUMAX
            ULIFT_U(K,N) = UDRAG_U(IC,N)*SPN(JC) - UDRAG_U(JC,N)*SPN(IC)
           ENDDO
          ENDDO
          ULMAG = SQRT(DOT(ULIFT,ULIFT))

          DO N = 1, NUMAX
             ULMAG_U(N) = ( ULIFT(1)*ULIFT_U(1,N)
     &                  +   ULIFT(2)*ULIFT_U(2,N)
     &                  +   ULIFT(3)*ULIFT_U(3,N) ) / ULMAG
          ENDDO

          DO K = 1, 3
            ULIFT(K) = ULIFT(K)/ULMAG
            DO N = 1, NUMAX
               ULIFT_U(K,N) = (ULIFT_U(K,N)
     &                      - ULIFT(K)*ULMAG_U(N)) / ULMAG
            ENDDO
c            write(6,*) 'Strip J ',J
c            write(6,*) 'UDRAG ',UDRAG
c            write(6,*) 'ULIFT ',ULIFT,' ULMAG ',ULMAG
c            write(6,3) 'ULIFT(1)_U ',(ULIFT_U(1,L),L=1,NUMAX)
c            write(6,3) 'ULIFT(2)_U ',(ULIFT_U(2,L),L=1,NUMAX)
c            write(6,3) 'ULIFT(3)_U ',(ULIFT_U(3,L),L=1,NUMAX)
          ENDDO
 3        FORMAT(A,6(2X,F8.5))
        ENDIF
C
C...Use the strip 1/4 chord location for strip moments
        RC4(1)  = RLE(1,J) + 0.25*CR
        RC4(2)  = RLE(2,J)
        RC4(3)  = RLE(3,J)
C
        CFX = 0.
        CFY = 0.
        CFZ = 0.
        CMX = 0.
        CMY = 0.
        CMZ = 0.
        CNC(J) = 0.
C
        DO N=1, NUMAX
          CFX_U(N) = 0.
          CFY_U(N) = 0.
          CFZ_U(N) = 0.
          CMX_U(N) = 0.
          CMY_U(N) = 0.
          CMZ_U(N) = 0.
          CNC_U(J,N) = 0.
        ENDDO
C
        DO N=1, NCONTROL
          CFX_D(N) = 0.
          CFY_D(N) = 0.
          CFZ_D(N) = 0.
          CMX_D(N) = 0.
          CMY_D(N) = 0.
          CMZ_D(N) = 0.
          CNC_D(J,N) = 0.
        ENDDO
C
        DO N=1, NDESIGN
          CFX_G(N) = 0.
          CFY_G(N) = 0.
          CFZ_G(N) = 0.
          CMX_G(N) = 0.
          CMY_G(N) = 0.
          CMZ_G(N) = 0.
          CNC_G(J,N) = 0.
        ENDDO
C
C...Sum the forces in the strip as generated by velocity
C    (freestream + rotation + induced) acting on bound vortex 
        DO 40 II = 1, NVC
          I = I1 + (II-1)
C
C------- local moment reference vector from vortex midpoint to strip c/4 pt.
          R(1) = RV(1,I) - RC4(1)
          R(2) = RV(2,I) - RC4(2)
          R(3) = RV(3,I) - RC4(3)
C
C------- vector from rotation axes
          RROT(1) = RV(1,I) - XYZREF(1)
          RROT(2) = RV(2,I) - XYZREF(2)
          RROT(3) = RV(3,I) - XYZREF(3)
C
          CALL CROSS(RROT,WROT,VROT)
C
          IF(LNFLD_WV) THEN
C======================================================================
C   Here the forces on h.v.'s are calculated using total induced velocity
C   WV, including velocities from h.v.'s and body source+doublets  
C   Set total effective velocity = freestream + rotation + h.v.+body induced
           VEFF(1) = VINF(1) + VROT(1) + WV(1,I)
           VEFF(2) = VINF(2) + VROT(2) + WV(2,I)
           VEFF(3) = VINF(3) + VROT(3) + WV(3,I)
C
C-------- set VEFF sensitivities to freestream,rotation,induced,controls,design
           DO K = 1, 3
            VEFF_U(1,K) = WV_U(1,I,K)
            VEFF_U(2,K) = WV_U(2,I,K)
            VEFF_U(3,K) = WV_U(3,I,K)
            VEFF_U(K,K) = 1.0  +  VEFF_U(K,K)
           ENDDO
           DO K = 4, 6
            WROT_U(1) = 0.
            WROT_U(2) = 0.
            WROT_U(3) = 0.
            WROT_U(K-3) = 1.0
            CALL CROSS(RROT,WROT_U,VROT_U)
            VEFF_U(1,K) = VROT_U(1) + WV_U(1,I,K)
            VEFF_U(2,K) = VROT_U(2) + WV_U(2,I,K)
            VEFF_U(3,K) = VROT_U(3) + WV_U(3,I,K)
           ENDDO
           DO N = 1, NCONTROL
            VEFF_D(1,N) = WV_D(1,I,N)
            VEFF_D(2,N) = WV_D(2,I,N)
            VEFF_D(3,N) = WV_D(3,I,N)
           END DO
           DO N = 1, NDESIGN
            VEFF_G(1,N) = WV_G(1,I,N)
            VEFF_G(2,N) = WV_G(2,I,N)
            VEFF_G(3,N) = WV_G(3,I,N)
           END DO
C         
          ELSE
C======================================================================
C   Here the forces on h.v.'s are calculated only with h.v. vortex induced
C   velocities VV, excluding velocities from body source+doublets  
C   Set total effective velocity = freestream + rotation + h.v. induced
           VEFF(1) = VINF(1) + VROT(1) + VV(1,I)
           VEFF(2) = VINF(2) + VROT(2) + VV(2,I)
           VEFF(3) = VINF(3) + VROT(3) + VV(3,I)
C
C-------- set VEFF sensitivities to freestream,rotation,.h.v. induced
           DO K = 1, 3
            VEFF_U(1,K) = VV_U(1,I,K)
            VEFF_U(2,K) = VV_U(2,I,K)
            VEFF_U(3,K) = VV_U(3,I,K)
            VEFF_U(K,K) = 1.0  +  VEFF_U(K,K)
           ENDDO
           DO K = 4, 6
            WROT_U(1) = 0.
            WROT_U(2) = 0.
            WROT_U(3) = 0.
            WROT_U(K-3) = 1.0
            CALL CROSS(RROT,WROT_U,VROT_U)
            VEFF_U(1,K) = VROT_U(1) + VV_U(1,I,K)
            VEFF_U(2,K) = VROT_U(2) + VV_U(2,I,K)
            VEFF_U(3,K) = VROT_U(3) + VV_U(3,I,K)
           ENDDO
           DO N = 1, NCONTROL
            VEFF_D(1,N) = VV_D(1,I,N)
            VEFF_D(2,N) = VV_D(2,I,N)
            VEFF_D(3,N) = VV_D(3,I,N)
           END DO
           DO N = 1, NDESIGN
            VEFF_G(1,N) = VV_G(1,I,N)
            VEFF_G(2,N) = VV_G(2,I,N)
            VEFF_G(3,N) = VV_G(3,I,N)
           END DO
C         
          ENDIF
C
C-------- Force coefficient on vortex segment is 2(Veff x Gamma)
          G(1) = RV2(1,I)-RV1(1,I)
          G(2) = RV2(2,I)-RV1(2,I)
          G(3) = RV2(3,I)-RV1(3,I)
          CALL CROSS(VEFF, G, F)
          DO N = 1, NUMAX
            CALL CROSS(VEFF_U(1,N), G, F_U(1,N))
          ENDDO
          DO N = 1, NCONTROL
            CALL CROSS(VEFF_D(1,N), G, F_D(1,N))
          ENDDO
          DO N = 1, NDESIGN
            CALL CROSS(VEFF_G(1,N), G, F_G(1,N))
          ENDDO
C
          FGAM(1) = 2.0*GAM(I)*F(1)
          FGAM(2) = 2.0*GAM(I)*F(2)
          FGAM(3) = 2.0*GAM(I)*F(3)
          DO N = 1, NUMAX
            FGAM_U(1,N) = 2.0*GAM_U(I,N)*F(1) + 2.0*GAM(I)*F_U(1,N)
            FGAM_U(2,N) = 2.0*GAM_U(I,N)*F(2) + 2.0*GAM(I)*F_U(2,N)
            FGAM_U(3,N) = 2.0*GAM_U(I,N)*F(3) + 2.0*GAM(I)*F_U(3,N)
          ENDDO
          DO N = 1, NCONTROL
            FGAM_D(1,N) = 2.0*GAM_D(I,N)*F(1) + 2.0*GAM(I)*F_D(1,N)
            FGAM_D(2,N) = 2.0*GAM_D(I,N)*F(2) + 2.0*GAM(I)*F_D(2,N)
            FGAM_D(3,N) = 2.0*GAM_D(I,N)*F(3) + 2.0*GAM(I)*F_D(3,N)
          ENDDO
          DO N = 1, NDESIGN
            FGAM_G(1,N) = 2.0*GAM_G(I,N)*F(1) + 2.0*GAM(I)*F_G(1,N)
            FGAM_G(2,N) = 2.0*GAM_G(I,N)*F(2) + 2.0*GAM(I)*F_G(2,N)
            FGAM_G(3,N) = 2.0*GAM_G(I,N)*F(3) + 2.0*GAM(I)*F_G(3,N)
          ENDDO
C
C
C-------- Delta Cp (loading across lifting surface) from vortex 
          FNV = DOT(ENV(1,I),FGAM)
          DCP(I) = FNV / (DXV(I)*WSTRIP(J))
C
          DO N = 1, NUMAX
            FNV_U = DOT(ENV(1,I),FGAM_U(1,N))
            DCP_U(I,N) = FNV_U / (DXV(I)*WSTRIP(J))
          ENDDO
C
          DO N = 1, NCONTROL
            FNV_D = DOT(ENV(1,I),FGAM_D(1,N)) + DOT(ENV_D(1,I,N),FGAM)
            DCP_D(I,N) = FNV_D / (DXV(I)*WSTRIP(J))
          ENDDO
C
          DO N = 1, NDESIGN
            FNV_G = DOT(ENV(1,I),FGAM_G(1,N)) + DOT(ENV_G(1,I,N),FGAM)
            DCP_G(I,N) = FNV_G / (DXV(I)*WSTRIP(J))
          ENDDO
C
C-------- vortex contribution to strip forces
          DCFX = FGAM(1) / SR
          DCFY = FGAM(2) / SR
          DCFZ = FGAM(3) / SR
C
C-------- forces normalized by strip area
          CFX = CFX +  DCFX
          CFY = CFY +  DCFY
          CFZ = CFZ +  DCFZ
C
C-------- moments referred to strip c/4 pt., normalized by strip chord and area
          CMX = CMX + (DCFZ*R(2) - DCFY*R(3))/CR
          CMY = CMY + (DCFX*R(3) - DCFZ*R(1))/CR
          CMZ = CMZ + (DCFY*R(1) - DCFX*R(2))/CR
C
C-------- accumulate strip spanloading = c*CN
          CNC(J) = CNC(J) + CR*(ENSY(J)*DCFY + ENSZ(J)*DCFZ)
C
C-------- freestream and rotation derivatives
          DO N=1, NUMAX
            DCFX_U = FGAM_U(1,N)/SR
            DCFY_U = FGAM_U(2,N)/SR
            DCFZ_U = FGAM_U(3,N)/SR
C
            CFX_U(N) = CFX_U(N) +  DCFX_U
            CFY_U(N) = CFY_U(N) +  DCFY_U
            CFZ_U(N) = CFZ_U(N) +  DCFZ_U
            CMX_U(N) = CMX_U(N) + (DCFZ_U*R(2) - DCFY_U*R(3))/CR
            CMY_U(N) = CMY_U(N) + (DCFX_U*R(3) - DCFZ_U*R(1))/CR
            CMZ_U(N) = CMZ_U(N) + (DCFY_U*R(1) - DCFX_U*R(2))/CR
C
            CNC_U(J,N) = CNC_U(J,N) 
     &                 + CR*(ENSY(J)*DCFY_U + ENSZ(J)*DCFZ_U)
          ENDDO
C
C-------- control derivatives
          DO N=1, NCONTROL
            DCFX_D = FGAM_D(1,N)/SR
            DCFY_D = FGAM_D(2,N)/SR
            DCFZ_D = FGAM_D(3,N)/SR
C
            CFX_D(N) = CFX_D(N) +  DCFX_D
            CFY_D(N) = CFY_D(N) +  DCFY_D
            CFZ_D(N) = CFZ_D(N) +  DCFZ_D
            CMX_D(N) = CMX_D(N) + (DCFZ_D*R(2) - DCFY_D*R(3))/CR
            CMY_D(N) = CMY_D(N) + (DCFX_D*R(3) - DCFZ_D*R(1))/CR
            CMZ_D(N) = CMZ_D(N) + (DCFY_D*R(1) - DCFX_D*R(2))/CR
C
            CNC_D(J,N) = CNC_D(J,N) 
     &                 + CR*(ENSY(J)*DCFY_D + ENSZ(J)*DCFZ_D)
          ENDDO
C
C-------- design derivatives
          DO N=1, NDESIGN
            DCFX_G = FGAM_G(1,N)/SR
            DCFY_G = FGAM_G(2,N)/SR
            DCFZ_G = FGAM_G(3,N)/SR
C
            CFX_G(N) = CFX_G(N) +  DCFX_G
            CFY_G(N) = CFY_G(N) +  DCFY_G
            CFZ_G(N) = CFZ_G(N) +  DCFZ_G
            CMX_G(N) = CMX_G(N) + (DCFZ_G*R(2) - DCFY_G*R(3))/CR
            CMY_G(N) = CMY_G(N) + (DCFX_G*R(3) - DCFZ_G*R(1))/CR
            CMZ_G(N) = CMZ_G(N) + (DCFY_G*R(1) - DCFX_G*R(2))/CR
C
            CNC_G(J,N) = CNC_G(J,N) 
     &                 + CR*(ENSY(J)*DCFY_G + ENSZ(J)*DCFZ_G)
          ENDDO
C
C-------- hinge moments
          DO L = 1, NCONTROL
            RH(1) = RV(1,I) - PHINGE(1,J,L)
            RH(2) = RV(2,I) - PHINGE(2,J,L)
            RH(3) = RV(3,I) - PHINGE(3,J,L)
C
            DFAC = DCONTROL(I,L) / (SREF*CREF)
C
            CALL CROSS(RH,FGAM,MH)
            CHINGE(L) = CHINGE(L) + DOT(MH,VHINGE(1,J,L))*DFAC
C
            DO N = 1, NUMAX
              CALL CROSS(RH,FGAM_U(1,N),MH)
              CHINGE_U(L,N) = CHINGE_U(L,N) + DOT(MH,VHINGE(1,J,L))*DFAC
            ENDDO
            DO N = 1, NCONTROL
              CALL CROSS(RH,FGAM_D(1,N),MH)
              CHINGE_D(L,N) = CHINGE_D(L,N) + DOT(MH,VHINGE(1,J,L))*DFAC
            ENDDO
            DO N = 1, NDESIGN
              CALL CROSS(RH,FGAM_G(1,N),MH)
              CHINGE_G(L,N) = CHINGE_G(L,N) + DOT(MH,VHINGE(1,J,L))*DFAC
            ENDDO
          ENDDO
C
   40   CONTINUE
C
C
C...Add h.v. forces from the parts of trailing legs which lie on the surface
        IF(.NOT.LTRFORCE) GO TO 80
C
C----- Sum forces on trailing legs using velocity = (freestream + rotation)
        DO 72 II = 1, NVC
          I = I1 + (II-1)
C
ccc          ddcmx = 0.0
          DO 71 ILEG = 1, 2
            IF(ILEG.EQ.1) THEN
C----------- local moment reference vector from vortex midpoint to strip c/4 pt
             R(1) = 0.5*(RV1(1,I) + XTE1) - RC4(1)
             R(2) =      RV1(2,I)         - RC4(2)
             R(3) =      RV1(3,I)         - RC4(3)
C 
C----------- vector from rotation axes
             RROT(1) = 0.5*(RV1(1,I) + XTE1) - XYZREF(1)
             RROT(2) =      RV1(2,I)         - XYZREF(2)
             RROT(3) =      RV1(3,I)         - XYZREF(3)
C
C----------- part of trailing leg lying on surface
             G(1) = RV1(1,I) - XTE1
             G(2) = 0.
             G(3) = 0.
C
            ELSE
C----------- local moment reference vector from vortex midpoint to strip c/4 pt
             R(1) = 0.5*(RV2(1,I) + XTE2) - RC4(1)
             R(2) =      RV2(2,I)         - RC4(2)
             R(3) =      RV2(3,I)         - RC4(3)
C
C----------- vector from rotation axes
             RROT(1) = 0.5*(RV2(1,I) + XTE2) - XYZREF(1)
             RROT(2) =      RV2(2,I)         - XYZREF(2)
             RROT(3) =      RV2(3,I)         - XYZREF(3)
C
C----------- part of trailing leg lying on surface
             G(1) = XTE2 - RV2(1,I)
             G(2) = 0.
             G(3) = 0.
            ENDIF
C
C---------- set total effective velocity = freestream + rotation
C           this ignores the h.v. induced contribution as this changes
C           along the trailing leg portion on the wing       
            CALL CROSS(RROT,WROT,VROT)
            VEFF(1) = VINF(1) + VROT(1)
            VEFF(2) = VINF(2) + VROT(2)
            VEFF(3) = VINF(3) + VROT(3)
C
C---------- set VEFF sensitivities to freestream,rotation components
            DO K = 1, 3
              VEFF_U(1,K) = 0.
              VEFF_U(2,K) = 0.
              VEFF_U(3,K) = 0.
              VEFF_U(K,K) = 1.0
            ENDDO
            DO K = 4, 6
              WROT_U(1) = 0.
              WROT_U(2) = 0.
              WROT_U(3) = 0.
              WROT_U(K-3) = 1.0
              CALL CROSS(RROT,WROT_U,VROT_U)
              VEFF_U(1,K) = VROT_U(1)
              VEFF_U(2,K) = VROT_U(2)
              VEFF_U(3,K) = VROT_U(3)
            ENDDO
C
C---------- Force coefficient on vortex segment is 2(Veff x Gamma)
            CALL CROSS (VEFF, G, F)
C
            DO N = 1, NUMAX
              CALL CROSS(VEFF_U(1,N), G, F_U(1,N))
            ENDDO
C
            FGAM(1) = 2.0*GAM(I)*F(1)
            FGAM(2) = 2.0*GAM(I)*F(2)
            FGAM(3) = 2.0*GAM(I)*F(3)
            DO N = 1, NUMAX
              FGAM_U(1,N) = 2.0*GAM_U(I,N)*F(1) + 2.0*GAM(I)*F_U(1,N)
              FGAM_U(2,N) = 2.0*GAM_U(I,N)*F(2) + 2.0*GAM(I)*F_U(2,N)
              FGAM_U(3,N) = 2.0*GAM_U(I,N)*F(3) + 2.0*GAM(I)*F_U(3,N)
            ENDDO
            DO N = 1, NCONTROL
              FGAM_D(1,N) = 2.0*GAM_D(I,N)*F(1)
              FGAM_D(2,N) = 2.0*GAM_D(I,N)*F(2)
              FGAM_D(3,N) = 2.0*GAM_D(I,N)*F(3)
            ENDDO
            DO N = 1, NDESIGN
              FGAM_G(1,N) = 2.0*GAM_G(I,N)*F(1)
              FGAM_G(2,N) = 2.0*GAM_G(I,N)*F(2)
              FGAM_G(3,N) = 2.0*GAM_G(I,N)*F(3)
            ENDDO
C
cC---------- Delta Cp (loading across lifting surface) due to vortex 
c            FNV = DOT(ENV(1,I),FGAM)
c            DCP(I) = FNV / (DXV(I)*WSTRIP(J))
cC
c            DO N = 1, NUMAX
c              FNV_U = DOT(ENV(1,I),FGAM_U(1,N))
c              DCP_U(I,N) = FNV_U / (DXV(I)*WSTRIP(J))
c            ENDDO
cC
c            DO N = 1, NCONTROL
c              FNV_D = DOT(ENV(1,I),FGAM_D(1,N)) + DOT(ENV_D(1,I,N),FGAM)
c              DCP_D(I,N) = FNV_D / (DXV(I)*WSTRIP(J))
c            ENDDO
cC
c            DO N = 1, NDESIGN
c              FNV_G = DOT(ENV(1,I),FGAM_G(1,N)) + DOT(ENV_G(1,I,N),FGAM)
c              DCP_G(I,N) = FNV_G / (DXV(I)*WSTRIP(J))
c            ENDDO
C
C
C---------- vortex contribution to strip forces
            DCFX = FGAM(1) / SR
            DCFY = FGAM(2) / SR
            DCFZ = FGAM(3) / SR
C
C---------- forces normalized by strip area
            CFX = CFX +  DCFX
            CFY = CFY +  DCFY
            CFZ = CFZ +  DCFZ
C
C---------- moments referred to strip c/4 pt., normalized by strip chord and area
            CMX = CMX + (DCFZ*R(2) - DCFY*R(3))/CR
            CMY = CMY + (DCFX*R(3) - DCFZ*R(1))/CR
            CMZ = CMZ + (DCFY*R(1) - DCFX*R(2))/CR
ccc            ddcmx = ddcmx + (DCFZ*R(2) - DCFY*R(3))/CR
ccc            write(22,*) I,ILEG,DCFY,DCFZ,(DCFZ*R(2) - DCFY*R(3))/CR
ccc            write(23,*) I,ddcmx
C     
C---------- accumulate strip spanloading = c*CN
            CNC(J) = CNC(J) + CR*(ENSY(J)*DCFY + ENSZ(J)*DCFZ)
C
C---------- freestream and rotation derivatives
            DO N=1, NUMAX
              DCFX_U = FGAM_U(1,N)/SR
              DCFY_U = FGAM_U(2,N)/SR
              DCFZ_U = FGAM_U(3,N)/SR
C
              CFX_U(N) = CFX_U(N) +  DCFX_U
              CFY_U(N) = CFY_U(N) +  DCFY_U
              CFZ_U(N) = CFZ_U(N) +  DCFZ_U
              CMX_U(N) = CMX_U(N) + (DCFZ_U*R(2) - DCFY_U*R(3))/CR
              CMY_U(N) = CMY_U(N) + (DCFX_U*R(3) - DCFZ_U*R(1))/CR
              CMZ_U(N) = CMZ_U(N) + (DCFY_U*R(1) - DCFX_U*R(2))/CR
C
              CNC_U(J,N) = CNC_U(J,N) 
     &                   + CR*(ENSY(J)*DCFY_U + ENSZ(J)*DCFZ_U)
            ENDDO
C
C---------- control derivatives
            DO N=1, NCONTROL
              DCFX_D = FGAM_D(1,N)/SR
              DCFY_D = FGAM_D(2,N)/SR
              DCFZ_D = FGAM_D(3,N)/SR
C  
              CFX_D(N) = CFX_D(N) +  DCFX_D
              CFY_D(N) = CFY_D(N) +  DCFY_D
              CFZ_D(N) = CFZ_D(N) +  DCFZ_D
              CMX_D(N) = CMX_D(N) + (DCFZ_D*R(2) - DCFY_D*R(3))/CR
              CMY_D(N) = CMY_D(N) + (DCFX_D*R(3) - DCFZ_D*R(1))/CR
              CMZ_D(N) = CMZ_D(N) + (DCFY_D*R(1) - DCFX_D*R(2))/CR
C  
              CNC_D(J,N) = CNC_D(J,N) 
     &                   + CR*(ENSY(J)*DCFY_D + ENSZ(J)*DCFZ_D)
            ENDDO
C
C---------- design derivatives
            DO N=1, NDESIGN
              DCFX_G = FGAM_G(1,N)/SR
              DCFY_G = FGAM_G(2,N)/SR
              DCFZ_G = FGAM_G(3,N)/SR
C
              CFX_G(N) = CFX_G(N) +  DCFX_G
              CFY_G(N) = CFY_G(N) +  DCFY_G
              CFZ_G(N) = CFZ_G(N) +  DCFZ_G
              CMX_G(N) = CMX_G(N) + (DCFZ_G*R(2) - DCFY_G*R(3))/CR
              CMY_G(N) = CMY_G(N) + (DCFX_G*R(3) - DCFZ_G*R(1))/CR
              CMZ_G(N) = CMZ_G(N) + (DCFY_G*R(1) - DCFX_G*R(2))/CR
C
              CNC_G(J,N) = CNC_G(J,N) 
     &                   + CR*(ENSY(J)*DCFY_G + ENSZ(J)*DCFZ_G)
            ENDDO
C
cC---------- hinge moments
c            DO L=1, NCONTROL
c              RH(1) = RV(1,I) - PHINGE(1,J,L)
c              RH(2) = RV(2,I) - PHINGE(2,J,L)
c              RH(3) = RV(3,I) - PHINGE(3,J,L)
cC
c              DFAC = DCONTROL(I,L) / (SREF * CREF)
cC
c              CALL CROSS(RH,FGAM,MH)
c              CHINGE(L) = CHINGE(L) + DOT(MH,VHINGE(1,J,L))*DFAC
cC
c              DO N = 1, NUMAX
c                CALL CROSS(RH,FGAM_U(1,N),MH)
c                CHINGE_U(L,N) = CHINGE_U(L,N) + DOT(MH,VHINGE(1,J,L))*DFAC
c              ENDDO
c              DO N = 1, NCONTROL
c                CALL CROSS(RH,FGAM_D(1,N),MH)
c                CHINGE_D(L,N) = CHINGE_D(L,N) + DOT(MH,VHINGE(1,J,L))*DFAC
c              ENDDO
c              DO N = 1, NDESIGN
c                CALL CROSS(RH,FGAM_G(1,N),MH)
c                CHINGE_G(L,N) = CHINGE_G(L,N) + DOT(MH,VHINGE(1,J,L))*DFAC
c              ENDDO
c            ENDDO
C
   71     CONTINUE
   72   CONTINUE
 80     CONTINUE
C
ccc        write(26,*) J, cmx,cmy,cmz
C     
C*******************************************************************
C--- Drag terms due to viscous effects
C    Drag forces are assumed to be characterized by velocity at the c/4 
C    point and are assumed to act thru the same point. CD is defined by 
C    user-specified CD(CL) polar.  Drag comes from function lookup on 
C    section polar drag using local lift coefficient.  
C
        CDV_LSTRP(J) = 0.0
C
        IF(LVISC.AND.LVISCSTRP(J)) THEN
C--- local moment reference vector from ref point to c/4 point
c         R(1) = RC4(1) - RC4(1)
c         R(2) = RC4(2) - RC4(2)
c         R(3) = RC4(3) - RC4(3)
C--- Get rotational velocity at strip 1/4 chord reference point 
         RROT(1) = RC4(1) - XYZREF(1)
         RROT(2) = RC4(2) - XYZREF(2)
         RROT(3) = RC4(3) - XYZREF(3)
C--- Onset velocity at strip c/4 = freestream + rotation
         CALL CROSS(RROT,WROT,VROT)
         VEFF(1) = VINF(1) + VROT(1)
         VEFF(2) = VINF(2) + VROT(2)
         VEFF(3) = VINF(3) + VROT(3)
         VEFFMAG = SQRT(VEFF(1)**2 +VEFF(2)**2 +VEFF(3)**2)
C
C------- set sensitivities to freestream,rotation components
         DO K = 1, 3
           VEFF_U(1,K) = 0.
           VEFF_U(2,K) = 0.
           VEFF_U(3,K) = 0.
         ENDDO
         VEFF_U(1,1) = 1.0
         VEFF_U(2,2) = 1.0
         VEFF_U(3,3) = 1.0
         DO K = 4, 6
           WROT_U(1) = 0.
           WROT_U(2) = 0.
           WROT_U(3) = 0.
           WROT_U(K-3) = 1.0
           CALL CROSS(RROT,WROT_U,VROT_U)
           VEFF_U(1,K) = VROT_U(1)
           VEFF_U(2,K) = VROT_U(2)
           VEFF_U(3,K) = VROT_U(3)
         ENDDO
         DO N = 1, NUMAX
           VEFFMAG_U(N) = ( VEFF(1)*VEFF_U(1,N)
     &                  +   VEFF(2)*VEFF_U(2,N)
     &                  +   VEFF(3)*VEFF_U(3,N) ) / VEFFMAG
         enddo
C
C--- Generate CD from stored function using strip CL as parameter
         CLV = ULIFT(1)*CFX + ULIFT(2)*CFY + ULIFT(3)*CFZ
cc         write(6,*) '***** CLV CFX CFY CFZ ', CLV, CFX,CFY,CFZ
         DO N = 1, NUMAX
           CLV_U(N) = ULIFT(1)*CFX_U(N) + ULIFT_U(1,N)*CFX
     &              + ULIFT(2)*CFY_U(N) + ULIFT_U(2,N)*CFY
     &              + ULIFT(3)*CFZ_U(N) + ULIFT_U(3,N)*CFZ
        END DO
C
         DO N = 1, NCONTROL
           CLV_D(N) = ULIFT(1)*CFX_D(N) + ULIFT_D(1,N)*CFX
     &              + ULIFT(2)*CFY_D(N) + ULIFT_D(2,N)*CFY
     &              + ULIFT(3)*CFZ_D(N) + ULIFT_D(3,N)*CFZ
         END DO
C
         DO N = 1, NDESIGN
           CLV_G(N) = ULIFT(1)*CFX_G(N) + ULIFT_G(1,N)*CFX
     &              + ULIFT(2)*CFY_G(N) + ULIFT_G(2,N)*CFY
     &              + ULIFT(3)*CFZ_G(N) + ULIFT_G(3,N)*CFZ
         END DO
C
C--- Get CD from CLCD function using strip CL as parameter
         CALL CDCL(CLCD(1,J),CLV,CDV,CDV_CLV)
C
C--- Strip viscous force contribution (per unit strip area)
         DCVFX = VEFF(1)*VEFFMAG * CDV
         DCVFY = VEFF(2)*VEFFMAG * CDV
         DCVFZ = VEFF(3)*VEFFMAG * CDV
c         write(6,*) 'Jstrip CLV,CDV ',J,CLV,CDV
c         write(6,*) 'DCVFXYZ ',DCVFX,DCVFY,DCVFZ
C
C--- Add viscous terms to strip forces and moments
         CFX = CFX +  DCVFX
         CFY = CFY +  DCVFY
         CFZ = CFZ +  DCVFZ
C--- Viscous forces acting at c/4 have no effect on moments at c/4 pt.
c         CMX = CMX + (DCVFZ*R(2) - DCVFY*R(3))/CR
c         CMY = CMY + (DCVFX*R(3) - DCVFZ*R(1))/CR
c         CMZ = CMZ + (DCVFY*R(1) - DCVFX*R(2))/CR
C
         CDV_LSTRP(J) = UDRAG(1)*DCVFX + UDRAG(2)*DCVFY + UDRAG(3)*DCVFZ
C
C--- Add the sensitivity of viscous forces to the flow conditions


      DO N=1, NUMAX
           DCVFX_U = (VEFF_U(1,N)*VEFFMAG + VEFF(1)*VEFFMAG_U(N))*CDV
     &             +  VEFF(1)*VEFFMAG * CDV_CLV*CLV_U(N)
           DCVFY_U = (VEFF_U(2,N)*VEFFMAG + VEFF(2)*VEFFMAG_U(N))*CDV
     &             +  VEFF(2)*VEFFMAG * CDV_CLV*CLV_U(N)
           DCVFZ_U = (VEFF_U(3,N)*VEFFMAG + VEFF(3)*VEFFMAG_U(N))*CDV
     &             +  VEFF(3)*VEFFMAG * CDV_CLV*CLV_U(N)
C           
           CFX_U(N) = CFX_U(N) +  DCVFX_U
           CFY_U(N) = CFY_U(N) +  DCVFY_U
           CFZ_U(N) = CFZ_U(N) +  DCVFZ_U
C--- Viscous forces acting at c/4 have no effect on moments at c/4 pt.
c           CMX_U(N) = CMX_U(N) + (DCVFZ_U*R(2) - DCVFY_U*R(3))/CR
c           CMY_U(N) = CMY_U(N) + (DCVFX_U*R(3) - DCVFZ_U*R(1))/CR
c           CMZ_U(N) = CMZ_U(N) + (DCVFY_U*R(1) - DCVFX_U*R(2))/CR
C
           CNC_U(J,N) = CNC_U(J,N) 
     &                + CR * (ENSY(J)*DCVFY_U + ENSZ(J)*DCVFZ_U)
         ENDDO
C
         DO N=1, NCONTROL
           DCVFX_D = VEFF(1)*VEFFMAG * CDV_CLV*CLV_D(N)
           DCVFY_D = VEFF(2)*VEFFMAG * CDV_CLV*CLV_D(N)
           DCVFZ_D = VEFF(3)*VEFFMAG * CDV_CLV*CLV_D(N)
C
           CFX_D(N) = CFX_D(N) +  DCVFX_D
           CFY_D(N) = CFY_D(N) +  DCVFY_D
           CFZ_D(N) = CFZ_D(N) +  DCVFZ_D
C--- Viscous forces acting at c/4 have no effect on moments at c/4 pt.
c           CMX_D(N) = CMX_D(N) + (DCVFZ_D*R(2) - DCVFY_D*R(3))/CR
c           CMY_D(N) = CMY_D(N) + (DCVFX_D*R(3) - DCVFZ_D*R(1))/CR
c           CMZ_D(N) = CMZ_D(N) + (DCVFY_D*R(1) - DCVFX_D*R(2))/CR
C
           CNC_D(J,N) = CNC_D(J,N) 
     &                + CR * (ENSY(J)*DCVFY_D + ENSZ(J)*DCVFZ_D)
         ENDDO
C
         DO N=1, NDESIGN
           DCVFX_G = VEFF(1)*VEFFMAG * CDV_CLV*CLV_G(N)
           DCVFY_G = VEFF(2)*VEFFMAG * CDV_CLV*CLV_G(N)
           DCVFZ_G = VEFF(3)*VEFFMAG * CDV_CLV*CLV_G(N)
C
           CFX_G(N) = CFX_G(N) +  DCVFX_G
           CFY_G(N) = CFY_G(N) +  DCVFY_G
           CFZ_G(N) = CFZ_G(N) +  DCVFZ_G
C--- Viscous forces acting at c/4 have no effect on moments at c/4 pt.
c           CMX_G(N) = CMX_G(N) + (DCVFZ_G*R(2) - DCVFY_G*R(3))/CR
c           CMY_G(N) = CMY_G(N) + (DCVFX_G*R(3) - DCVFZ_G*R(1))/CR
c           CMZ_G(N) = CMZ_G(N) + (DCVFY_G*R(1) - DCVFX_G*R(2))/CR
C
           CNC_G(J,N) = CNC_G(J,N) 
     &                + CR * (ENSY(J)*DCVFY_G + ENSZ(J)*DCVFZ_G)
         ENDDO
C
        ENDIF        
C
C*******************************************************************
C
C---  At this point the forces are accumulated for the strip in body axes,
C     referenced to strip 1/4 chord and normalized by strip area and chord
C     CFX, CFY, CFZ   ! body axes forces 
C     CMX, CMY, CMZ   ! body axes moments about c/4 point 
C     CNC             ! strip spanloading CN*chord
C     CDV_LSTRP       ! strip viscous drag in wind axes
C
C...Store strip X,Y,Z body axes forces and moments 
C   referred to c/4 and strip area and chord
        CF_LSTRP(1,J) = CFX
        CF_LSTRP(2,J) = CFY
        CF_LSTRP(3,J) = CFZ
        CM_LSTRP(1,J) = CMX
        CM_LSTRP(2,J) = CMY
        CM_LSTRP(3,J) = CMZ
ccc        write(24,*) J,cmx
C
C...Strip body axes forces, referred to strip area and chord
        CFSTRP(1,J) =  CFX
        CFSTRP(2,J) =  CFY
        CFSTRP(3,J) =  CFZ
C...Transform strip body axes forces into stability axes,
C   referred to strip area and chord
        CDSTRP(J) =  CFX*COSA + CFZ*SINA
        CYSTRP(J) =  CFY
        CLSTRP(J) = -CFX*SINA + CFZ*COSA 
C
        CDST_A(J) = -CFX*SINA + CFZ*COSA
        CYST_A(J) =  0.0
        CLST_A(J) = -CFX*COSA - CFZ*SINA 
C
        DO N=1, NUMAX
          CDST_U(J,N) =  CFX_U(N)*COSA + CFZ_U(N)*SINA
          CYST_U(J,N) =  CFY_U(N)
          CLST_U(J,N) = -CFX_U(N)*SINA + CFZ_U(N)*COSA 
          CFST_U(1,J,N) =  CFX_U(N)
          CFST_U(2,J,N) =  CFY_U(N)
          CFST_U(3,J,N) =  CFZ_U(N)
        END DO
C
        DO N=1, NCONTROL
          CDST_D(J,N) =  CFX_D(N)*COSA + CFZ_D(N)*SINA
          CYST_D(J,N) =  CFY_D(N)
          CLST_D(J,N) = -CFX_D(N)*SINA + CFZ_D(N)*COSA 
          CFST_D(1,J,N) =  CFX_D(N)
          CFST_D(2,J,N) =  CFY_D(N)
          CFST_D(3,J,N) =  CFZ_D(N)
        END DO
C
        DO N=1, NDESIGN
          CDST_G(J,N) =  CFX_G(N)*COSA + CFZ_G(N)*SINA
          CYST_G(J,N) =  CFY_G(N)
          CLST_G(J,N) = -CFX_G(N)*SINA + CFZ_G(N)*COSA 
          CFST_G(1,J,N) =  CFX_G(N)
          CFST_G(2,J,N) =  CFY_G(N)
          CFST_G(3,J,N) =  CFZ_G(N)
        END DO
C
C------ vector from chord c/4 reference point to case reference point XYZREF 
        R(1) = RC4(1) - XYZREF(1)
        R(2) = RC4(2) - XYZREF(2)
        R(3) = RC4(3) - XYZREF(3)
C... Strip moments in body axes about the case moment reference point XYZREF 
C    normalized by strip area and chord
        CMSTRP(1,J) = CMX + (CFZ*R(2) - CFY*R(3))/CR
        CMSTRP(2,J) = CMY + (CFX*R(3) - CFZ*R(1))/CR
        CMSTRP(3,J) = CMZ + (CFY*R(1) - CFX*R(2))/CR
C
        DO N=1, NUMAX
          CMST_U(1,J,N) = CMX_U(N) + (CFZ_U(N)*R(2) - CFY_U(N)*R(3))/CR
          CMST_U(2,J,N) = CMY_U(N) + (CFX_U(N)*R(3) - CFZ_U(N)*R(1))/CR
          CMST_U(3,J,N) = CMZ_U(N) + (CFY_U(N)*R(1) - CFX_U(N)*R(2))/CR
        ENDDO
C
        DO N=1, NCONTROL
          CMST_D(1,J,N) = CMX_D(N) + (CFZ_D(N)*R(2) - CFY_D(N)*R(3))/CR
          CMST_D(2,J,N) = CMY_D(N) + (CFX_D(N)*R(3) - CFZ_D(N)*R(1))/CR
          CMST_D(3,J,N) = CMZ_D(N) + (CFY_D(N)*R(1) - CFX_D(N)*R(2))/CR
        ENDDO
C
        DO N=1, NDESIGN
          CMST_G(1,J,N) = CMX_G(N) + (CFZ_G(N)*R(2) - CFY_G(N)*R(3))/CR
          CMST_G(2,J,N) = CMY_G(N) + (CFX_G(N)*R(3) - CFZ_G(N)*R(1))/CR
          CMST_G(3,J,N) = CMZ_G(N) + (CFY_G(N)*R(1) - CFX_G(N)*R(2))/CR
        ENDDO
C
C...Components of X,Y,Z forces in local strip axes 
        CL_LSTRP(J) = ULIFT(1)*CFX + ULIFT(2)*CFY + ULIFT(3)*CFZ
        CD_LSTRP(J) = UDRAG(1)*CFX + UDRAG(2)*CFY + UDRAG(3)*CFZ
        CMC4_LSTRP(J) = ENSZ(J)*CMY - ENSY(J)*CMZ

C...Axial/normal forces and lift/drag in plane normal to dihedral of strip
        CAXL0 = CFX
        CNRM0 = ENSY(J)*CFY + ENSZ(J)*CFZ
C...CN,CA forces are rotated to be in and normal to strip incidence
C   HHY bugfix 01102024 added rotation by AINC
        SINAINC = SIN(AINC(J))
        COSAINC = COS(AINC(J))
        CA_LSTRP(J) = CAXL0*COSAINC - CNRM0*SINAINC
        CN_LSTRP(J) = CNRM0*COSAINC + CAXL0*SINAINC
C
C------ vector at chord reference point from rotation axes
        RROT(1) = XSREF(J) - XYZREF(1)
        RROT(2) = YSREF(J) - XYZREF(2)
        RROT(3) = ZSREF(J) - XYZREF(3)
c        print *,"WROT ",WROT
C
C------ set total effective velocity = freestream + rotation
        CALL CROSS(RROT,WROT,VROT)
        VEFF(1) = VINF(1) + VROT(1)
        VEFF(2) = VINF(2) + VROT(2)
        VEFF(3) = VINF(3) + VROT(3)
C
        VSQ = VEFF(1)**2 + VEFF(2)**2 + VEFF(3)**2
        IF(VSQ .EQ. 0.0) THEN
         VSQI = 1.0
        ELSE
         VSQI = 1.0 / VSQ
        ENDIF
C
C------ spanwise and perpendicular velocity components
        VSPAN = VEFF(1)*ESS(1,J) + VEFF(2)*ESS(2,J) + VEFF(3)*ESS(3,J)
        VPERP(1) = VEFF(1) - ESS(1,J)*VSPAN
        VPERP(2) = VEFF(2) - ESS(2,J)*VSPAN
        VPERP(3) = VEFF(3) - ESS(3,J)*VSPAN
C
        VPSQ = VPERP(1)**2 + VPERP(2)**2 + VPERP(3)**2
        IF(VPSQ .EQ. 0.0) THEN
         VPSQI = 1.0
        ELSE
         VPSQI = 1.0 / VPSQ
        ENDIF
ccc     CLT_LSTRP(J) = CN_LSTRP(J) * VPSQI
        CLT_LSTRP(J) = CL_LSTRP(J) * VPSQI
        CLA_LSTRP(J) = CL_LSTRP(J) * VSQI
C
C--- Moment about strip LE midpoint in direction of LE segment
        R(1) = RC4(1) - RLE(1,J)
        R(2) = RC4(2) - RLE(2,J)
        R(3) = RC4(3) - RLE(3,J)
        DELX = RLE2(1,J) - RLE1(1,J)
        DELY = RLE2(2,J) - RLE1(2,J)
        DELZ = RLE2(3,J) - RLE1(3,J)
C
        IF(IMAGS(LSSURF(J)).LT.0) THEN 
         DELX = -DELX
         DELY = -DELY
         DELZ = -DELZ
        ENDIF
        DMAG = SQRT(DELX**2+DELY**2+DELZ**2)
        CMLE_LSTRP(J) = 0.0
        IF(DMAG.NE.0.0) THEN
         CMLE_LSTRP(J) = DELX/DMAG*(CMX + (CFZ*R(2) - CFY*R(3))/CR)
     &                 + DELY/DMAG*(CMY + (CFX*R(3) - CFZ*R(1))/CR)
     &                 + DELZ/DMAG*(CMZ + (CFY*R(1) - CFX*R(2))/CR)
        ENDIF
C
  100 CONTINUE
C
C
C
C...Surface forces and moments summed from strip forces...
C   XXSURF values normalized to configuration reference quantities SREF,CREF,BREF about XYZref
C   XX_LSRF values normalized to each surface's reference quantities (area and average chord)
      DO 150 IS = 1, NSURF
        CDSURF(IS) = 0.
        CYSURF(IS) = 0.
        CLSURF(IS) = 0.
        DO L=1,3
          CFSURF(L,IS) = 0.
          CMSURF(L,IS) = 0.
        ENDDO
        CDVSURF(IS) = 0.
C
        CDS_A(IS) = 0.
        CYS_A(IS) = 0.
        CLS_A(IS) = 0.
        DO N=1, NUMAX
          CDS_U(IS,N) = 0.
          CYS_U(IS,N) = 0.
          CLS_U(IS,N) = 0.
          DO L=1,3
            CFS_U(L,IS,N) = 0.
            CMS_U(L,IS,N) = 0.
          ENDDO
       ENDDO
        DO N=1, NCONTROL
          CDS_D(IS,N) = 0.
          CYS_D(IS,N) = 0.
          CLS_D(IS,N) = 0.
          DO L=1,3
            CFS_D(L,IS,N) = 0.
            CMS_D(L,IS,N) = 0.
          ENDDO
        ENDDO
        DO N=1, NDESIGN
          CDS_G(IS,N) = 0.
          CYS_G(IS,N) = 0.
          CLS_G(IS,N) = 0.
          DO L=1,3
            CFS_G(L,IS,N) = 0.
            CMS_G(L,IS,N) = 0.
          ENDDO
        ENDDO
C
C--- Surface body axes forces and moments
        DO L = 1, 3
          CF_LSRF(L,IS) = 0.0
          CM_LSRF(L,IS) = 0.0
          ENAVE(L)    = 0.0
        END DO
C
        NSTRPS = NJ(IS)
        DO 120 JJ = 1, NSTRPS
          J = JFRST(IS) + JJ-1
          SR = CHORD(J)*WSTRIP(J)
          CR = CHORD(J)
          RC4(1) = RLE(1,J) + 0.25*CHORD(J)
          RC4(2) = RLE(2,J)
          RC4(3) = RLE(3,J)
C
ccc          write(25,*) IS,J,JJ,CM_LSTRP(1,J),CM_LSTRP(2,J),CM_LSTRP(3,J)

          ENAVE(1) = 0.0
          ENAVE(2) = ENAVE(2) + SR*ENSY(J)
          ENAVE(3) = ENAVE(3) + SR*ENSZ(J)
C
C--- Surface lift and drag referenced to case SREF, CREF, BREF 
          CDSURF(IS) = CDSURF(IS) + CDSTRP(J)*SR/SREF
          CYSURF(IS) = CYSURF(IS) + CYSTRP(J)*SR/SREF
          CLSURF(IS) = CLSURF(IS) + CLSTRP(J)*SR/SREF
C--- Surface body axes forces referenced to case SREF, CREF, BREF
          CFSURF(1,IS) = CFSURF(1,IS) + CFSTRP(1,J)*SR/SREF
          CFSURF(2,IS) = CFSURF(2,IS) + CFSTRP(2,J)*SR/SREF
          CFSURF(3,IS) = CFSURF(3,IS) + CFSTRP(3,J)*SR/SREF
C--- Surface body axes moments referenced to case SREF, CREF, BREF about XYZREF
          CMSURF(1,IS) = CMSURF(1,IS) + CMSTRP(1,J)*(SR/SREF)*(CR/BREF)
          CMSURF(2,IS) = CMSURF(2,IS) + CMSTRP(2,J)*(SR/SREF)*(CR/CREF)
          CMSURF(3,IS) = CMSURF(3,IS) + CMSTRP(3,J)*(SR/SREF)*(CR/BREF)
C
C--- Bug fix, HHY/S.Allmaras 
C--- Surface viscous drag referenced to case SREF, CREF, BREF
          CDVSURF(IS)  = CDVSURF(IS) + CDV_LSTRP(J)*(SR/SREF)
C
          CDS_A(IS) = CDS_A(IS) + CDST_A(J)*SR/SREF
          CYS_A(IS) = CYS_A(IS) + CYST_A(J)*SR/SREF
          CLS_A(IS) = CLS_A(IS) + CLST_A(J)*SR/SREF
C
          DO N=1, NUMAX
            CDS_U(IS,N) = CDS_U(IS,N) + CDST_U(J,N)*SR/SREF
            CYS_U(IS,N) = CYS_U(IS,N) + CYST_U(J,N)*SR/SREF
            CLS_U(IS,N) = CLS_U(IS,N) + CLST_U(J,N)*SR/SREF
C
            DO L = 1, 3
              CFS_U(L,IS,N) = CFS_U(L,IS,N) + CFST_U(L,J,N)*SR/SREF
            ENDDO
            CMS_U(1,IS,N) = CMS_U(1,IS,N)
     &                    + CMST_U(1,J,N)*(SR/SREF)*(CR/BREF)
            CMS_U(2,IS,N) = CMS_U(2,IS,N)
     &                    + CMST_U(2,J,N)*(SR/SREF)*(CR/CREF)
            CMS_U(3,IS,N) = CMS_U(3,IS,N)
     &                    + CMST_U(3,J,N)*(SR/SREF)*(CR/BREF)
          ENDDO
C
          DO N=1, NCONTROL
            CDS_D(IS,N) = CDS_D(IS,N) + CDST_D(J,N)*SR/SREF
            CYS_D(IS,N) = CYS_D(IS,N) + CYST_D(J,N)*SR/SREF
            CLS_D(IS,N) = CLS_D(IS,N) + CLST_D(J,N)*SR/SREF
C
            DO L = 1, 3
              CFS_D(L,IS,N) = CFS_D(L,IS,N) + CFST_D(L,J,N)*SR/SREF
            ENDDO
            CMS_D(1,IS,N) = CMS_D(1,IS,N)
     &                    + CMST_D(1,J,N)*(SR/SREF)*(CR/BREF)
            CMS_D(2,IS,N) = CMS_D(2,IS,N)
     &                    + CMST_D(2,J,N)*(SR/SREF)*(CR/CREF)
            CMS_D(3,IS,N) = CMS_D(3,IS,N)
     &                    + CMST_D(3,J,N)*(SR/SREF)*(CR/BREF)
          ENDDO
C
          DO N=1, NDESIGN
            CDS_G(IS,N) = CDS_G(IS,N) + CDST_G(J,N)*SR/SREF
            CYS_G(IS,N) = CYS_G(IS,N) + CYST_G(J,N)*SR/SREF
            CLS_G(IS,N) = CLS_G(IS,N) + CLST_G(J,N)*SR/SREF
C
            DO L = 1, 3
              CFS_G(L,IS,N) = CFS_G(L,IS,N) + CFST_G(L,J,N)*SR/SREF
            ENDDO
            CMS_G(1,IS,N) = CMS_G(1,IS,N)
     &                    + CMST_G(1,J,N)*(SR/SREF)*(CR/BREF)
            CMS_G(2,IS,N) = CMS_G(2,IS,N)
     &                    + CMST_G(2,J,N)*(SR/SREF)*(CR/CREF)
            CMS_G(3,IS,N) = CMS_G(3,IS,N)
     &                    + CMST_G(3,J,N)*(SR/SREF)*(CR/BREF)
          ENDDO
C
C--- reference point for surface LE (hinge) moments
C    defined by surface hinge vector direction thru first strip LE point
          IF(IMAGS(IS).GE.0) THEN
            R(1) = RC4(1) - RLE1(1,JFRST(IS))
            R(2) = RC4(2) - RLE1(2,JFRST(IS))
            R(3) = RC4(3) - RLE1(3,JFRST(IS))
           ELSE
            R(1) = RC4(1) - RLE2(1,JFRST(IS))
            R(2) = RC4(2) - RLE2(2,JFRST(IS))
            R(3) = RC4(3) - RLE2(3,JFRST(IS))
          ENDIF
C---  Surface forces and moments (about root strip LE point, normalized
C     locally by surface area and average chord)
          DO K = 1, 3
            IC = ICRS(K)
            JC = JCRS(K)
C
            CF_LSRF(K,IS) = CF_LSRF(K,IS) + CF_LSTRP(K,J)*SR/SSURF(IS)
C
            DCM = SR/SSURF(IS) * CR/CAVESURF(IS) *
     &          ( CM_LSTRP(K,J) 
     &          + CF_LSTRP(JC,J)*R(IC) - CF_LSTRP(IC,J)*R(JC) ) / CR
C
            CM_LSRF(K,IS) = CM_LSRF(K,IS) + DCM 
          END DO
C
  120   CONTINUE
C
C--- To define surface CL and CD we need local lift and drag directions...
C--- Define drag and lift directions for surface using average strip normal
        ENAVE(1) = ENAVE(1)/SSURF(IS)
        ENAVE(2) = ENAVE(2)/SSURF(IS)
        ENAVE(3) = ENAVE(3)/SSURF(IS)
        ENMAG = SQRT(DOT(ENAVE,ENAVE))
        IF(ENMAG.EQ.0.) THEN
          ENAVE(3) = 1.0
         ELSE
          ENAVE(1) = ENAVE(1)/ENMAG 
          ENAVE(2) = ENAVE(2)/ENMAG 
          ENAVE(3) = ENAVE(3)/ENMAG 
        ENDIF
C--- Define a "spanwise" vector with cross product of average surface normal 
C    with chordline (x direction)
        SPN(1) =  0.0
        SPN(2) =  ENAVE(3)
        SPN(3) = -ENAVE(2)
C--- Wind axes stream vector defines drag direction
        UDRAG(1) = VINF(1)
        UDRAG(2) = VINF(2)
        UDRAG(3) = VINF(3)
C--- Lift direction is vector product of "stream" and spanwise vector
        CALL CROSS(UDRAG,SPN,ULIFT)
        ULMAG = SQRT(DOT(ULIFT,ULIFT))
        IF(ULMAG.EQ.0.) THEN
          ULIFT(3) = 1.0
         ELSE
          ULIFT(1) = ULIFT(1)/ULMAG
          ULIFT(2) = ULIFT(2)/ULMAG
          ULIFT(3) = ULIFT(3)/ULMAG
        ENDIF
        CL_LSRF(IS) = DOT(ULIFT,CF_LSRF(1,IS))
        CD_LSRF(IS) = DOT(UDRAG,CF_LSRF(1,IS))
C
C---  Surface hinge moments defined by surface LE moment about hinge vector 
ccc        CMLE_LSRF(IS) = DOT(CM_LSRF(1,IS),VHINGE(1,IS))
C
C
C-------------------------------------------------
        IF(LFLOAD(IS)) THEN
C--- Total forces summed from surface forces
C    normalized to case reference quantities SREF, CREF, BREF
         CFTOT(1) = CFTOT(1) + CFSURF(1,IS)
         CFTOT(2) = CFTOT(2) + CFSURF(2,IS)
         CFTOT(3) = CFTOT(3) + CFSURF(3,IS)
         CDTOT = CDTOT + CDSURF(IS)
         CYTOT = CYTOT + CYSURF(IS)
         CLTOT = CLTOT + CLSURF(IS)
         CDVTOT = CDVTOT + CDVSURF(IS)
C--- Total body axes moments about XYZREF summed from surface moments
C    normalized to case reference quantities SREF, CREF, BREF
         CMTOT(1) = CMTOT(1) + CMSURF(1,IS)
         CMTOT(2) = CMTOT(2) + CMSURF(2,IS)
         CMTOT(3) = CMTOT(3) + CMSURF(3,IS)
C
         CDTOT_A = CDTOT_A + CDS_A(IS)
         CYTOT_A = CYTOT_A + CYS_A(IS)
         CLTOT_A = CLTOT_A + CLS_A(IS)
C
         DO N=1, NUMAX
           CDTOT_U(N) = CDTOT_U(N) + CDS_U(IS,N)
           CYTOT_U(N) = CYTOT_U(N) + CYS_U(IS,N)
           CLTOT_U(N) = CLTOT_U(N) + CLS_U(IS,N)
           DO L = 1, 3
             CFTOT_U(L,N) = CFTOT_U(L,N) + CFS_U(L,IS,N)
             CMTOT_U(L,N) = CMTOT_U(L,N) + CMS_U(L,IS,N)
           ENDDO
         ENDDO  
C
         DO N=1, NCONTROL
           CDTOT_D(N) = CDTOT_D(N) + CDS_D(IS,N)
           CYTOT_D(N) = CYTOT_D(N) + CYS_D(IS,N)
           CLTOT_D(N) = CLTOT_D(N) + CLS_D(IS,N)
           DO L = 1, 3
             CFTOT_D(L,N) = CFTOT_D(L,N) + CFS_D(L,IS,N)
             CMTOT_D(L,N) = CMTOT_D(L,N) + CMS_D(L,IS,N)
           ENDDO
         ENDDO
C
         DO N=1, NDESIGN
           CDTOT_G(N) = CDTOT_G(N) + CDS_G(IS,N)
           CYTOT_G(N) = CYTOT_G(N) + CYS_G(IS,N)
           CLTOT_G(N) = CLTOT_G(N) + CLS_G(IS,N)
           DO L = 1, 3
             CFTOT_G(L,N) = CFTOT_G(L,N) + CFS_G(L,IS,N)
             CMTOT_G(L,N) = CMTOT_G(L,N) + CMS_G(L,IS,N)
           ENDDO
         ENDDO
        ENDIF
C---------------------------------------------------------
C
  150 CONTINUE
C
      RETURN
      END ! SFFORC
C


      SUBROUTINE BDFORC
      INCLUDE 'AVL.INC'
C
      REAL RROT(3)
      REAL VEFF(3)    , VROT(3)  ,
     &     VEFF_U(3,6), VROT_U(3), WROT_U(3)
      REAL DRL(3), ESL(3), 
     &     FB(3), FB_U(3,NUMAX),
     &     MB(3), MB_U(3,NUMAX)
      REAL CDBDY_U(NUMAX), CYBDY_U(NUMAX), CLBDY_U(NUMAX), 
     &     CFBDY_U(3,NUMAX), 
     &     CMBDY_U(3,NUMAX)
C
C
      BETM = SQRT(1.0 - MACH**2)
C
      SINA = SIN(ALFA)
      COSA = COS(ALFA)
C
C
C---- add on body force contributions
      DO 200 IB = 1, NBODY
        CDBDY(IB) = 0.0
        CYBDY(IB) = 0.0
        CLBDY(IB) = 0.0
        DO L = 1, 3
          CFBDY(L,IB) = 0.0
          CMBDY(L,IB) = 0.0
        ENDDO
C     
        DO IU = 1, 6
          CDBDY_U(IU) = 0.0
          CYBDY_U(IU) = 0.0
          CLBDY_U(IU) = 0.0
          DO L = 1, 3
            CFBDY_U(L,IU) = 0.0
            CMBDY_U(L,IU) = 0.0
          ENDDO
        ENDDO
C
        DO 205 ILSEG = 1, NL(IB)-1
          L1 = LFRST(IB) + ILSEG - 1
          L2 = LFRST(IB) + ILSEG
C
          L = L1
C
          DRL(1) = (RL(1,L2) - RL(1,L1))/BETM
          DRL(2) =  RL(2,L2) - RL(2,L1)
          DRL(3) =  RL(3,L2) - RL(3,L1)
          DRLMAG = SQRT(DRL(1)**2 + DRL(2)**2 + DRL(3)**2)
          IF(DRLMAG.EQ.0.0) THEN
           DRLMI = 0.0
          ELSE
           DRLMI = 1.0/DRLMAG
          ENDIF
C
          DIA = RADL(L1) + RADL(L2)
          IF(DIA.LE.0.0) THEN
           DINV = 0.0
          ELSE
           DINV = 1.0/DIA
          ENDIF
C
C-------- unit vector along line segment
          ESL(1) = DRL(1) * DRLMI
          ESL(2) = DRL(2) * DRLMI
          ESL(3) = DRL(3) * DRLMI
C
          RROT(1) = 0.5*(RL(1,L2)+RL(1,L1)) - XYZREF(1)
          RROT(2) = 0.5*(RL(2,L2)+RL(2,L1)) - XYZREF(2)
          RROT(3) = 0.5*(RL(3,L2)+RL(3,L1)) - XYZREF(3)
C
C-------- go over freestream velocity and rotation components
          CALL CROSS(RROT,WROT,VROT)
C
          VEFF(1) = (VINF(1) + VROT(1))/BETM
          VEFF(2) =  VINF(2) + VROT(2)
          VEFF(3) =  VINF(3) + VROT(3)
C
C-------- set VEFF sensitivities to freestream,rotation components
          DO K = 1, 3
            VEFF_U(1,K) = 0.
            VEFF_U(2,K) = 0.
            VEFF_U(3,K) = 0.
            VEFF_U(K,K) = 1.0
          ENDDO
C
          DO K = 4, 6
            WROT_U(1) = 0.
            WROT_U(2) = 0.
            WROT_U(3) = 0.
            WROT_U(K-3) = 1.0
            CALL CROSS(RROT,WROT_U,VROT_U)
C
            VEFF_U(1,K) = VROT_U(1)
            VEFF_U(2,K) = VROT_U(2)
            VEFF_U(3,K) = VROT_U(3)
          ENDDO
C
C-------- U.es
          US = VEFF(1)*ESL(1) + VEFF(2)*ESL(2) + VEFF(3)*ESL(3)
C
C
C-------- velocity projected on normal plane = U - (U.es) es
          DO K = 1, 3
            UN = VEFF(K) - US*ESL(K)
            FB(K) = UN*SRC(L)
C
            DO IU = 1, 6
              UN_U = VEFF_U(K,IU)
     &             - ( VEFF_U(1,IU)*ESL(1)
     &                +VEFF_U(2,IU)*ESL(2)
     &                +VEFF_U(3,IU)*ESL(3))*ESL(K)
              FB_U(K,IU) = UN *SRC_U(L,IU) + UN_U*SRC(L)
            ENDDO
C
            DCPB(K,L) = FB(K) * 2.0 * DINV*DRLMI
          ENDDO
C
          CALL CROSS(RROT,FB,MB)
          DO IU = 1, 6
            CALL CROSS(RROT,FB_U(1,IU),MB_U(1,IU)) 
          ENDDO
C
          CDBDY(IB) = CDBDY(IB) + ( FB(1)*COSA + FB(3)*SINA) * 2.0/SREF
          CYBDY(IB) = CYBDY(IB) +   FB(2) * 2.0/SREF
          CLBDY(IB) = CLBDY(IB) + (-FB(1)*SINA + FB(3)*COSA) * 2.0/SREF
          DO L = 1, 3
            CFBDY(L,IB) = CFBDY(L,IB) +   FB(L) * 2.0/SREF
          ENDDO
          CMBDY(1,IB) = CMBDY(1,IB) +   MB(1) * 2.0/SREF / BREF
          CMBDY(2,IB) = CMBDY(2,IB) +   MB(2) * 2.0/SREF / CREF
          CMBDY(3,IB) = CMBDY(3,IB) +   MB(3) * 2.0/SREF / BREF
C
          DO IU = 1, 6
            CDBDY_U(IU) = CDBDY_U(IU) + ( FB_U(1,IU)*COSA
     &                                  + FB_U(3,IU)*SINA) * 2.0/SREF
            CYBDY_U(IU) = CYBDY_U(IU) +   FB_U(2,IU) * 2.0/SREF
            CLBDY_U(IU) = CLBDY_U(IU) + (-FB_U(1,IU)*SINA
     &           + FB_U(3,IU)*COSA) * 2.0/SREF
C            
            DO L = 1, 3
              CFBDY_U(L,IU) = CFBDY_U(L,IU) +   FB_U(L,IU) * 2.0/SREF
            ENDDO
C
            CMBDY_U(1,IU) = CMBDY_U(1,IU)
     &                  +   MB_U(1,IU) * 2.0/SREF / BREF
            CMBDY_U(2,IU) = CMBDY_U(2,IU)
     &                  +   MB_U(2,IU) * 2.0/SREF / CREF
            CMBDY_U(3,IU) = CMBDY_U(3,IU)
     &                  +   MB_U(3,IU) * 2.0/SREF / BREF
          ENDDO
 205    CONTINUE
C
C---- add body forces and sensitivities to totals
        CDTOT = CDTOT + CDBDY(IB)
        CYTOT = CYTOT + CYBDY(IB)
        CLTOT = CLTOT + CLBDY(IB)
C
        DO L = 1, 3
          CFTOT(L) = CFTOT(L) + CFBDY(L,IB)
          CMTOT(L) = CMTOT(L) + CMBDY(L,IB)
        ENDDO
C
        DO IU = 1, 6
          CDTOT_U(IU) = CDTOT_U(IU) + CDBDY_U(IU)
          CYTOT_U(IU) = CYTOT_U(IU) + CYBDY_U(IU)
          CLTOT_U(IU) = CLTOT_U(IU) + CLBDY_U(IU)
C
          DO L = 1, 3
            CFTOT_U(L,IU) = CFTOT_U(L,IU) + CFBDY_U(L,IU)
            CMTOT_U(L,IU) = CMTOT_U(L,IU) + CMBDY_U(L,IU)
          ENDDO
        ENDDO
 200  CONTINUE
C
      RETURN
      END ! BDFORC



      SUBROUTINE VINFAB
C
C...Purpose:  To calculate free stream vector components and sensitivities
C
C...Input:   ALFA       Angle of attack (for stability-axis definition)
C            BETA       Sideslip angle (positive wind on right cheek facing fwd)
C...Output:  VINF(3)    Velocity components of free stream
C            VINF_A(3)  dVINF()/dALFA
C            VINF_B(3)  dVINF()/dBETA
C
      INCLUDE 'AVL.INC'
C
      SINA = SIN(ALFA)
      COSA = COS(ALFA)
      SINB = SIN(BETA)
      COSB = COS(BETA)
C
      VINF(1) =  COSA*COSB
      VINF(2) =      -SINB
      VINF(3) =  SINA*COSB
C
      VINF_A(1) = -SINA*COSB
      VINF_A(2) =  0.
      VINF_A(3) =  COSA*COSB
C
      VINF_B(1) = -COSA*SINB
      VINF_B(2) =      -COSB
      VINF_B(3) = -SINA*SINB
C
      RETURN
      END ! VINFAB
