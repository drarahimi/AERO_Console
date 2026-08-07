
      SUBROUTINE VORVELC(X,Y,Z,LBOUND,X1,Y1,Z1,X2,Y2,Z2,BETA,
     &                   U,V,W, RCORE)
C----------------------------------------------------------
C     Same as VORVEL, with finite core radius
C     Original Scully (AKA Burnham-Hallock) core model 
C       Vtan = Gam/2*pi . r/(r^2 +rcore^2)
C      
C     Uses Leishman's R^4 variant of Scully (AKA Burnham-Hallock) core model 
C       Vtan = Gam/2*pi . r/sqrt(r^4 +rcore^4)
C----------------------------------------------------------
      LOGICAL LBOUND
C
C
      REAL A(3), B(3), AXB(3)
C
      DATA PI4INV  / 0.079577472 /
C
C---- Prandtl-Glauert coordinates 
      A(1) = (X1 - X)/BETA
      A(2) =  Y1 - Y
      A(3) =  Z1 - Z
C
      B(1) = (X2 - X)/BETA
      B(2) =  Y2 - Y
      B(3) =  Z2 - Z
C
      ASQ = A(1)**2 + A(2)**2 + A(3)**2
      BSQ = B(1)**2 + B(2)**2 + B(3)**2
C
      AMAG = SQRT(ASQ)
      BMAG = SQRT(BSQ)
C
      RCORE2 = RCORE**2
      RCORE4 = RCORE2**2
C
      U = 0.
      V = 0.
      W = 0.
C
C---- contribution from the transverse bound leg
      IF (LBOUND  .AND.  AMAG*BMAG .NE. 0.0) THEN
        AXB(1) = A(2)*B(3) - A(3)*B(2)
        AXB(2) = A(3)*B(1) - A(1)*B(3)
        AXB(3) = A(1)*B(2) - A(2)*B(1)
        AXBSQ = AXB(1)**2 + AXB(2)**2 + AXB(3)**2
C
        IF(AXBSQ .NE. 0.0) THEN 
         ADB = A(1)*B(1) + A(2)*B(2) + A(3)*B(3)
         ALSQ = ASQ + BSQ - 2.0*ADB
cc c     RSQ = AXBSQ / ALSQ
C     
         ABMAG = AMAG*BMAG
C---- Scully core model      
cccc        T = (AMAG+BMAG)*(1.0 - ADB/ABMAG) / (AXBSQ + ALSQ*RCORE2)
cc        T = (  (BSQ-ADB)/SQRT(BSQ+RCORE2)
cc     &       + (ASQ-ADB)/SQRT(ASQ+RCORE2) ) / (AXBSQ + ALSQ*RCORE2)
C---- Leishman core model
         T = (  (BSQ-ADB)/SQRT(SQRT(BSQ**2+RCORE4))
     &        + (ASQ-ADB)/SQRT(SQRT(ASQ**2+RCORE4)) )
     &        / SQRT(AXBSQ**2 + ALSQ**2*RCORE4)
C
         U = AXB(1)*T
         V = AXB(2)*T
         W = AXB(3)*T
        ENDIF
      ENDIF
C
C---- trailing leg attached to A
      IF (AMAG .NE. 0.0) THEN
        AXISQ = A(3)**2 + A(2)**2
        ADX = A(1)
        RSQ = AXISQ
C
C---- Scully core model      
cc        T = - (1.0 - ADX/AMAG) / (RSQ + RCORE2)
C---- Leishman core model
        T = - (1.0 - ADX/AMAG) / SQRT(RSQ**2 + RCORE4)
C
        V = V + A(3)*T
        W = W - A(2)*T
      ENDIF
C
C---- trailing leg attached to B
      IF (BMAG .NE. 0.0) THEN
        BXISQ = B(3)**2 + B(2)**2
        BDX = B(1)
        RSQ = BXISQ
C
C---- Scully core model      
cc        T =   (1.0 - BDX/BMAG) / (RSQ + RCORE2)
C---- Leishman core modeld
        T =   (1.0 - BDX/BMAG) / SQRT(RSQ**2 + RCORE4)
C
        V = V + B(3)*T
        W = W - B(2)*T
      ENDIF
C
      U = U*PI4INV / BETA
      V = V*PI4INV 
      W = W*PI4INV 
C
      RETURN
      END ! VORVELC

