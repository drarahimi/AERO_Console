
      SUBROUTINE LUDCMP(NSIZ,N,A,INDX,WORK)
C     *******************************************************
C     *   Factors a full NxN matrix into an LU form.        *
C     *   Uses LINPACK routines for linear algebra          *
C     *******************************************************
C
      REAL A(NSIZ,NSIZ), WORK(NSIZ)
      INTEGER INDX(NSIZ)
Cr
C      CALL SGETRF(N,N,A,NSIZ,INDX,INFO)
      CALL DGETRF(N,N,A,NSIZ,INDX,INFO)
C
      RETURN
      END ! LUDCMP


      SUBROUTINE BAKSUB(NSIZ,N,A,INDX,B)
      REAL A(NSIZ,NSIZ), B(NSIZ)
      INTEGER INDX(NSIZ)
C     *******************************************************
C     *   BAKSUB does back-substitution with RHS using      *
C     *   stored LU decomposition.                          *
C     *   Uses LINPACK routines for linear algebra          *
C     *******************************************************
C
      MRHS = 1
C      CALL SGETRS('N',N,MRHS,A,NSIZ,INDX,B,NSIZ,INFO)
      CALL DGETRS('N',N,MRHS,A,NSIZ,INDX,B,NSIZ,INFO)
C
      RETURN
      END ! BAKSUB



