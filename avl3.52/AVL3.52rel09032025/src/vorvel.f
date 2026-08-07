
      SUBROUTINE VORVEL(X,Y,Z,LBOUND,X1,Y1,Z1,X2,Y2,Z2,BETA,
     &                   U,V,W )
C----------------------------------------------------------
C     Same as VORVEL1, with somewhat different formulation
C----------------------------------------------------------
      LOGICAL LBOUND
C
      REAL A(3), B(3), AXB(3)
C
      DATA PI4INV  / 0.079577472 /
C
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
      U = 0.
      V = 0.
      W = 0.
C
C---- contribution from the transverse bound leg
      IF (LBOUND .AND.  AMAG*BMAG .NE. 0.0) THEN
        AXB(1) = A(2)*B(3) - A(3)*B(2)
        AXB(2) = A(3)*B(1) - A(1)*B(3)
        AXB(3) = A(1)*B(2) - A(2)*B(1)
C
        ADB = A(1)*B(1) + A(2)*B(2) + A(3)*B(3)
        DEN = AMAG*BMAG + ADB
C
        IF(DEN .NE. 0.0) THEN
         T = (1.0/AMAG + 1.0/BMAG) / DEN
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
C
        ADI = A(1)
        RSQ = AXISQ
C
        T = - (1.0 - ADI/AMAG) / RSQ
C
        V = V + A(3)*T
        W = W - A(2)*T
      ENDIF
C
C---- trailing leg attached to B
      IF (BMAG .NE. 0.0) THEN
        BXISQ = B(3)**2 + B(2)**2
C
        BDI = B(1)
        RSQ = BXISQ
C
        T =   (1.0 - BDI/BMAG) / RSQ
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
      END ! VORVEL
