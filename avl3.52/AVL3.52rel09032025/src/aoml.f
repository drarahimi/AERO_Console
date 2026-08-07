C***********************************************************************
C    Module:  aoml.f
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



      SUBROUTINE CPOML
C
      INCLUDE 'AVL.INC'
C
      LOGICAL LRANGEALL
C
C...  check that all surfaces use full range of airfoil definitions
      LRANGEALL = .TRUE.
      DO ISURF = 1, NSURF
        LRANGEALL = LRANGEALL .AND. LRANGE(ISURF)
      ENDDO
      IF (.NOT.LRANGEALL) THEN
        WRITE(*,*) 'ERROR in CPOML: implemented only for surfaces ',
     &    'defined using full range of input airfoils'
        WRITE(*,*) '  returning without writing OML output'
        RETURN
      ENDIF
C
      CALL CPTHK()
      CALL CPDUMP()
C
      RETURN
      END


      SUBROUTINE CPTHK
C
C...PURPOSE  compute sectional pressure coefficients due to thickness
C            via constant-source panels along thickness surface
C            NOTE: airfoil assumed in (x,z) plane even if there is dihedral
C
C...INPUT
C
C...OUTPUT   CPT        thickness based pressure coefficient (in common)
C
C...COMMENTS
C
      INCLUDE 'AVL.INC'
C
      PARAMETER (NCMAX=256)
      DIMENSION AICT(NCMAX,NCMAX)       ! AIC for no normal flow BC
      DIMENSION BICT(NCMAX,NCMAX)       ! AIC for tangential flow (used after solve for Cp)
      DIMENSION RHS(NCMAX), WORK(NCMAX)
      DIMENSION QSINF(NCMAX)            ! freestream tangential velocity
      DIMENSION SRCTHK(NCMAX)
C
      DO ISTRIP = 1, NSTRIP
C
        I1  = IJFRST(ISTRIP)
        NVC = NVSTRP(ISTRIP)
        IF (NVC.GT.NCMAX) THEN
          WRITE(*,*) '* CPTHK: Array overflow.  Increase NCMAX to', NVC
          STOP
        ENDIF
C
        XLE = 0.5*(RLE1(1,ISTRIP) + RLE2(1,ISTRIP))
        ZLE = 0.5*(RLE1(3,ISTRIP) + RLE2(3,ISTRIP))
C
        DO II = 1, NVC
          I = I1 + (II-1)
C
C...      control point: panel center
          IF (II.EQ.1) THEN
            X   = 0.5*(XLE + 0.5*(XYN1(1,I) + XYN2(1,I))) - XLE
            ZLO = 0.5*(ZLE + 0.5*(ZLON1(I) + ZLON2(I)))   - ZLE
            ZUP = 0.5*(ZLE + 0.5*(ZUPN1(I) + ZUPN2(I)))   - ZLE
          ELSE
            X = 0.25*(XYN1(1,I-1) + XYN2(1,I-1) + XYN1(1,I) + XYN2(1,I))
            ZLO = 0.25*(ZLON1(I-1) + ZLON2(I-1) + ZLON1(I) + ZLON2(I))
            ZUP = 0.25*(ZUPN1(I-1) + ZUPN2(I-1) + ZUPN1(I) + ZUPN2(I))
            X   = X   - XLE
            ZLO = ZLO - ZLE
            ZUP = ZUP - ZLE
          ENDIF
          Z = 0.5*(ZUP - ZLO)
C
C...      unit-normal to thickness at panel center
          IF (II.EQ.1) THEN
            DX   = 0.5*(XYN1(1,I) + XYN2(1,I)) - XLE
            DZLO = 0.5*(ZLON1(I) + ZLON2(I)) - ZLE
            DZUP = 0.5*(ZUPN1(I) + ZUPN2(I)) - ZLE
          ELSE
            DX = 0.5*(XYN1(1,I) - XYN1(1,I-1) + XYN2(1,I) - XYN2(1,I-1))
            DZLO = 0.5*(ZLON1(I) - ZLON1(I-1) + ZLON2(I) - ZLON2(I-1))
            DZUP = 0.5*(ZUPN1(I) - ZUPN1(I-1) + ZUPN2(I) - ZUPN2(I-1))
          ENDIF
          DZ = 0.5*(DZUP - DZLO)

          DEN = SQRT(DX*DX + DZ*DZ)
          ESX = DX/DEN
          ESZ = DZ/DEN
          ENX = -ESZ
          ENZ =  ESX
C
C...      thickness solution at zero alpha
          VINF1 = 1
          VINF3 = 0
          RHS(II)   = ENX*VINF1 + ENZ*VINF3
          QSINF(II) = ESX*VINF1 + ESZ*VINF3   ! needed for Cp
C
          DO JJ = 1, NVC
            J = I1 + (JJ-1)
C
            IF (JJ.EQ.1) THEN
              X1 = 0
              Z1 = 0
            ELSE
              X1   = 0.5*(XYN1(1,J-1) + XYN2(1,J-1)) - XLE
              Z1LO = 0.5*(ZLON1(J-1) + ZLON2(J-1))   - ZLE
              Z1UP = 0.5*(ZUPN1(J-1) + ZUPN2(J-1))   - ZLE
              Z1   = 0.5*(Z1UP - Z1LO)
            ENDIF
C
            X2   = 0.5*(XYN1(1,J) + XYN2(1,J)) - XLE
            Z2LO = 0.5*(ZLON1(J) + ZLON2(J))   - ZLE
            Z2UP = 0.5*(ZUPN1(J) + ZUPN2(J))   - ZLE
            Z2   = 0.5*(Z2UP - Z2LO)
            IF (JJ.EQ.II) THEN
              U = 0.5*ENX;    !! trick: needed to ensure proper velocity at ii=jj panel;
              W = 0.5*ENZ;    !! if (x,z) below panel, then get -0.5 (which is bad)
            ELSE
              CALL SRCPANEL( X, Z, X1, Z1, X2, Z2, U, W )
            ENDIF
            AICT(II,JJ) = ENX*U + ENZ*W
            BICT(II,JJ) = ESX*U + ESZ*W
C
            CALL SRCPANEL( X, Z, X1, -Z1, X2, -Z2, U, W )
            AICT(II,JJ) = AICT(II,JJ) + ENX*U + ENZ*W
            BICT(II,JJ) = BICT(II,JJ) + ESX*U + ESZ*W
          ENDDO
        ENDDO
C
        CALL LUDCMP(NCMAX,NVC,AICT,IAPIV,WORK)
        CALL BAKSUB(NCMAX,NVC,AICT,IAPIV,RHS)
C
        DO II = 1, NVC
          SRCTHK(II) = -RHS(II)
        ENDDO
C
        DO II = 1, NVC
          I = I1 + (II-1)
C
          QS = QSINF(II)
          DO JJ = 1, NVC
            QS = QS + BICT(II,JJ)*SRCTHK(JJ)
          ENDDO
          CPT(I) = 1 - QS*QS
        ENDDO
      ENDDO ! ISTRIP
C
      RETURN
      END ! CPTHK


      SUBROUTINE CPDUMP
C
C...PURPOSE     output OML upper and lower grid and pressure coefficients
C
C...INPUT
C
C...OUTPUT
C
C...COMMENTS    C code reader in read_cpoml.c
C
      INCLUDE 'AVL.INC'
C
      LU = 12
      OPEN(LU, FILE='cpoml.dat', FORM='FORMATTED', STATUS='UNKNOWN')
C
      WRITE(LU,'(A)') 'CPOML'
C
      WRITE(LU,'(A)') 'VERSION 1.0'
      WRITE(LU,'(I6,6X,A)') NSURF, '  | surfaces'
      DO ISURF = 1, NSURF
        NVC = NK(ISURF)
C
C...    determine if surface is L-to-R or R-to-L
        IF (NJ(ISURF) .EQ. 1) THEN
          ISTRIP0 = JFRST(ISURF)
          ISTRIP1 = ISTRIP0 + NJ(ISURF) - 1
          ISTEP = 1
        ELSE
          ISTRIP0 = JFRST(ISURF)
          ISTRIP1 = ISTRIP0 + NJ(ISURF) - 1
C
          Y0 = RLE1(2,ISTRIP0)
          Y1 = RLE1(2,ISTRIP1)
          IF (Y1 .GT. Y0) THEN
            ISTEP = 1                           ! L-to-R
          ELSE
            ISTRIP1 = JFRST(ISURF)
            ISTRIP0 = ISTRIP1 + NJ(ISURF) - 1
            ISTEP = -1                          ! R-to-L
          ENDIF
        ENDIF
C
        WRITE(LU,'(A)') 'SURFACE'
        WRITE(LU,'(A)') STITLE(ISURF)
        WRITE(LU,'(I6,6X,A)') LNCOMP(ISURF), '  | component'
        WRITE(LU,'(2I6,A)') NJ(ISURF), NK(ISURF),
     &    '  | elements (span, chord)'
        WRITE(LU,'(I6,6X,A)') IMAGS(ISURF),
     &    '  | imags (<0 if Y-duplicated)'
C
        NSEC = NCNTSEC(ISURF)
        II = ICNTFRST(ISURF)
        WRITE(LU,'(I6,6X,A)') NSEC, '  | section indices'
        IF (ISTEP .GT. 0) THEN
          WRITE(LU,*) (ICNTSEC(II + ISEC-1), ISEC=1,NSEC)
        ELSE
          NSPAN = NJ(ISURF) + 1
          WRITE(LU,*) (NSPAN - ICNTSEC(II + ISEC-1) + 1, ISEC=NSEC,1,-1)
        ENDIF
C
        WRITE(LU,'(A,1X,A)') 'VERTEX_GRID',
     &    '(x_lo, x_up, y_lo, y_up, z_lo, z_up)'
        DO J = ISTRIP0, ISTRIP1, ISTEP
          I1 = IJFRST(J)
C
          DYLE = RLE2(2,J) - RLE1(2,J)
          DZLE = RLE2(3,J) - RLE1(3,J)

          CSA = COS(AINC1(J))
          SNA = SIN(AINC1(J))
C
          XLE = RLE1(1,J)
          YLE = RLE1(2,J)
          ZLE = RLE1(3,J)
          WRITE(LU,'(6(ES23.15))') XLE, XLE, YLE, YLE, ZLE, ZLE
C
          DO II = 1, NVC
            I = I1 + (II-1)
C
            X0 = XYN1(1,I)
            Y0 = XYN1(2,I)
            ZLO0 = ZLON1(I)
            ZUP0 = ZUPN1(I)
C
C...        rotate airfoil in (y,z) so that it is perpendicular to dihedral
            DY = XYN2(2,I) - XYN1(2,I)
            DZ = 0.5*(ZLON2(I) - ZLON1(I))
     &         + 0.5*(ZUPN2(I) - ZUPN1(I))
            CSD  = DY/SQRT(DY*DY + DZ*DZ)
            SND  = DZ/SQRT(DY*DY + DZ*DZ)
            YLOD = YLE + (Y0 - YLE)*CSD - (ZLO0 - ZLE)*SND
            YUPD = YLE + (Y0 - YLE)*CSD - (ZUP0 - ZLE)*SND
            ZLOD = ZLE - (Y0 - YLE)*SND + (ZLO0 - ZLE)*CSD
            ZUPD = ZLE - (Y0 - YLE)*SND + (ZUP0 - ZLE)*CSD
C
C...        rotate airfoil in (x,z) for twist
            XLO = XLE + (X0 - XLE)*CSA + (ZLOD - ZLE)*SNA
            XUP = XLE + (X0 - XLE)*CSA + (ZUPD - ZLE)*SNA
            ZLO = ZLE - (X0 - XLE)*SNA + (ZLOD - ZLE)*CSA
            ZUP = ZLE - (X0 - XLE)*SNA + (ZUPD - ZLE)*CSA
            YLO = YLOD
            YUP = YUPD
            WRITE(LU,'(6(ES23.15))') XLO, XUP, YLO, YUP, ZLO, ZUP
          ENDDO
        ENDDO
C
        J = ISTRIP1
        I1 = IJFRST(J)
C
        CSA = COS(AINC2(J))
        SNA = SIN(AINC2(J))
C
        XLE = RLE2(1,J)
        YLE = RLE2(2,J)
        ZLE = RLE2(3,J)
        WRITE(LU,'(6(ES23.15))') XLE, XLE, YLE, YLE, ZLE, ZLE
C
        DO II = 1, NVC
          I = I1 + (II-1)
C
          X0 = XYN2(1,I)
          Y0 = XYN2(2,I)
          ZLO0 = ZLON2(I)
          ZUP0 = ZUPN2(I)
C
C...      rotate airfoil in (y,z) so that it is perpendicular to dihedral
          DY = XYN2(2,I) - XYN1(2,I)
          DZ = 0.5*(ZLON2(I) - ZLON1(I))
     &       + 0.5*(ZUPN2(I) - ZUPN1(I))
          CSD  = DY/SQRT(DY*DY + DZ*DZ)
          SND  = DZ/SQRT(DY*DY + DZ*DZ)
          YLOD = YLE + (Y0 - YLE)*CSD - (ZLO0 - ZLE)*SND
          YUPD = YLE + (Y0 - YLE)*CSD - (ZUP0 - ZLE)*SND
          ZLOD = ZLE - (Y0 - YLE)*SND + (ZLO0 - ZLE)*CSD
          ZUPD = ZLE - (Y0 - YLE)*SND + (ZUP0 - ZLE)*CSD
C
C...      rotate airfoil in (x,z) for twist
          XLO = XLE + (X0 - XLE)*CSA + (ZLOD - ZLE)*SNA
          XUP = XLE + (X0 - XLE)*CSA + (ZUPD - ZLE)*SNA
          ZLO = ZLE - (X0 - XLE)*SNA + (ZLOD - ZLE)*CSA
          ZUP = ZLE - (X0 - XLE)*SNA + (ZUPD - ZLE)*CSA
          YLO = YLOD
          YUP = YUPD
          WRITE(LU,'(6(ES23.15))') XLO, XUP, YLO, YUP, ZLO, ZUP
        ENDDO
C
        WRITE(LU,'(A,1X,A)') 'ELEMENT_CP',
     &    '(x_lo, x_up, y_lo, y_up, z_lo, z_up, cp_lo, cp_up)'
        DO J = ISTRIP0, ISTRIP1, ISTEP
          I1 = IJFRST(J)
C
          CSA = COS(AINC(J))
          SNA = SIN(AINC(J))
C
          XLE = 0.5*(RLE1(1,J) + RLE2(1,J))
          YLE = 0.5*(RLE1(2,J) + RLE2(2,J))
          ZLE = 0.5*(RLE1(3,J) + RLE2(3,J))
C
          X0   = 0.5*(XLE + 0.5*(XYN1(1,I1) + XYN2(1,I1)))
          Y0   = 0.5*(YLE + 0.5*(XYN1(2,I1) + XYN2(2,I1)))
          ZLO0 = 0.5*(ZLE + 0.5*(ZLON1(I1) + ZLON2(I1)))
          ZUP0 = 0.5*(ZLE + 0.5*(ZUPN1(I1) + ZUPN2(I1)))
C
C...      rotate airfoil in (y,z) so that it is perpendicular to dihedral
          DY = XYN2(2,I1) - XYN1(2,I1)
          DZ = 0.5*(ZLON2(I1) - ZLON1(I1))
     &       + 0.5*(ZUPN2(I1) - ZUPN1(I1))
          CSD  = DY/SQRT(DY*DY + DZ*DZ)
          SND  = DZ/SQRT(DY*DY + DZ*DZ)
          YLOD = YLE + (Y0 - YLE)*CSD - (ZLO0 - ZLE)*SND
          YUPD = YLE + (Y0 - YLE)*CSD - (ZUP0 - ZLE)*SND
          ZLOD = ZLE - (Y0 - YLE)*SND + (ZLO0 - ZLE)*CSD
          ZUPD = ZLE - (Y0 - YLE)*SND + (ZUP0 - ZLE)*CSD
C
C...      rotate airfoil in (x,z) for twist
          XLO = XLE + (X0 - XLE)*CSA + (ZLO0 - ZLE)*SNA
          XUP = XLE + (X0 - XLE)*CSA + (ZUP0 - ZLE)*SNA
          ZLO = ZLE - (X0 - XLE)*SNA + (ZLO0 - ZLE)*CSA
          ZUP = ZLE - (X0 - XLE)*SNA + (ZUP0 - ZLE)*CSA
          YLO = YLOD
          YUP = YUPD
C
          CPU = CPT(I1) + 0.5*DCP(I1)
          CPL = CPT(I1) - 0.5*DCP(I1)
          WRITE(LU,'(8(ES23.15))') XLO,XUP, YLO,YUP, ZLO,ZUP, CPL,CPU
C
          DO II = 2, NVC
            I = I1 + (II-1)
C
            X0 = 0.5*(XYN1(1,I-1) + XYN2(1,I-1))
            Y0 = 0.5*(XYN1(2,I-1) + XYN2(2,I-1))
            X1 = 0.5*(XYN1(1,I  ) + XYN2(1,I  ))
            Y1 = 0.5*(XYN1(2,I  ) + XYN2(2,I  ))
            X0 = 0.5*(X0 + X1)
            Y0 = 0.5*(Y0 + Y1)
C
            ZLO0 = 0.5*(ZLON1(I-1) + ZLON2(I-1))
            ZUP0 = 0.5*(ZUPN1(I-1) + ZUPN2(I-1))
            ZLO1 = 0.5*(ZLON1(I  ) + ZLON2(I  ))
            ZUP1 = 0.5*(ZUPN1(I  ) + ZUPN2(I  ))
            ZLO0 = 0.5*(ZLO0 + ZLO1)
            ZUP0 = 0.5*(ZUP0 + ZUP1)
C
C...        rotate airfoil in (y,z) so that it is perpendicular to dihedral
            DY = XYN2(2,I) - XYN1(2,I)
            DZ = 0.5*(ZLON2(I) - ZLON1(I))
     &         + 0.5*(ZUPN2(I) - ZUPN1(I))
            CSD  = DY/SQRT(DY*DY + DZ*DZ)
            SND  = DZ/SQRT(DY*DY + DZ*DZ)
            YLOD = YLE + (Y0 - YLE)*CSD - (ZLO0 - ZLE)*SND
            YUPD = YLE + (Y0 - YLE)*CSD - (ZUP0 - ZLE)*SND
            ZLOD = ZLE - (Y0 - YLE)*SND + (ZLO0 - ZLE)*CSD
            ZUPD = ZLE - (Y0 - YLE)*SND + (ZUP0 - ZLE)*CSD
C
C...        rotate airfoil in (x,z) for twist
            XLO = XLE + (X0 - XLE)*CSA + (ZLOD - ZLE)*SNA
            XUP = XLE + (X0 - XLE)*CSA + (ZUPD - ZLE)*SNA
            ZLO = ZLE - (X0 - XLE)*SNA + (ZLOD - ZLE)*CSA
            ZUP = ZLE - (X0 - XLE)*SNA + (ZUPD - ZLE)*CSA
            YLO = YLOD
            YUP = YUPD
C
            CPU = CPT(I) + 0.5*DCP(I)
            CPL = CPT(I) - 0.5*DCP(I)
            WRITE(LU,'(8(ES23.15))') XLO,XUP, YLO,YUP, ZLO,ZUP, CPL,CPU
          ENDDO
        ENDDO
      ENDDO
C
      CLOSE(LU)
C
      RETURN
      END ! CPDUMP


      SUBROUTINE SRCPANEL( X, Z, X1, Z1, X2, Z2, U, W )
C
C...PURPOSE  compute velocities induced by 2D unit-strength constant-source panel
C
C...INPUT    x, z       control point
C            x1, z1     panel endpoint 1
C            x2, z2     panel endpoint 2
C
C...OUTPUT   u, w       induced velocity in global coordinates
C
C...COMMENTS            offset from endpoint 1 mitigates issues with atan branch cut
C
C...  rotate to local panel coordinates
      DEN = SQRT((X2 - X1)**2 + (Z2 - Z1)**2)
      CS  = (X2 - X1)/DEN
      SN  = (Z2 - Z1)/DEN
C
      XP  =  CS*(X - X1) + SN*(Z - Z1)
      ZP  = -SN*(X - X1) + CS*(Z - Z1)
      X1P = 0
      Z1P = 0
      X2P = DEN
      Z2P = 0
C
C...  velocity induced by unit-strength constant-source panel
      PI   = 4*ATAN(1.)
      R1SQ = (XP - X1P)**2 + (ZP - Z1P)**2
      R2SQ = (XP - X2P)**2 + (ZP - Z2P)**2
      TH1  = ATAN2(ZP - Z1P, XP - X1P)
      TH2  = ATAN2(ZP - Z2P, XP - X2P)
      UP   = LOG(R1SQ/R2SQ) / (4*PI)
      WP   = (TH2 - TH1) / (2*PI)
C
C...  rotate velocity to global
      U = CS*UP - SN*WP
      W = SN*UP + CS*WP

      RETURN
      END ! SRCPANEL


      SUBROUTINE DUMPMARKET(NNDIM,NN,A)
      DIMENSION A(NNDIM,NNDIM)
C
      OPEN(10, FILE='jac.mtx', FORM='FORMATTED', STATUS='UNKNOWN')
      WRITE(10,100)
      WRITE(10,*) NN, NN, NN*NN
      DO I = 1, NN
        DO J = 1, NN
          WRITE(10,*) I, J, A(I,J)
        ENDDO
      ENDDO
      CLOSE(10)
C
100   FORMAT('%%MatrixMarket matrix coordinate real general')
      RETURN
      END

