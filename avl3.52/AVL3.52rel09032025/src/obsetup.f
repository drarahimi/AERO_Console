C***********************************************************************
C    Module:  obsetup.f
C 
C    Copyright (C) 2024 Mark Drela, Harold Youngren
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

      SUBROUTINE OBAIC
C
C...  PURPOSE  To set up the AIC for off-body points
C
C     An AIC matrix is assembled for off-body points
C     For flow surveys.
C     Two matrices are created for velocity influences from
C     horseshoe vortices and from body sources and doublets.
C            
C...INPUT    Global Data in labelled commons, defining configuration
C          
C...OUTPUT   WOB_GAM(3,iob,i)    h.v. induced velocity/GAM
C            WOBSRD_U(3,iob,6)  body induced velocity for unit VINF
C
C...COMMENTS   
C
      INCLUDE 'AVL.INC'
C     
      AMACH = MACH
      BETM = SQRT(1.0 - AMACH**2)
C
      IF(.NOT.LOBAIC) THEN
       WRITE(*,*) ' Building off-body h.v. AIC matrix...'
       CALL VVOR(BETM,IYSYM,YSYM,IZSYM,ZSYM,
     &           VRCOREC,VRCOREW,
     &           NVOR,RV1,RV2,LVCOMP,CHORDV,
     &           NOB,ROB,     LVCOMP,.FALSE.,
     &           VOB_GAM,NOBMAX)
C
       WRITE(*,*) ' Building off-body source+doublet AIC matrix...'
       CALL SRDSET(BETM,XYZREF,IYSYM,
     &             NBODY,LFRST,NLMAX,
     &             NL,RL,RADL,
     &             SRC_U,DBL_U)
       NU = 6
       WRITE(*,*) ' Building off-body velocity matrix...'
       CALL VSRD(BETM,IYSYM,YSYM,IZSYM,ZSYM,SRCORE,
     &           NBODY,LFRST,NLMAX,
     &           NL,RL,RADL,
     &           NU,SRC_U,DBL_U,
     &           NOB,ROB,
     &           WOBSRD_U,NOBMAX)
C
        LOBAIC = .TRUE.
        LOBVEL = .FALSE.
      ENDIF
C
      RETURN
      END ! OBSETUP


      SUBROUTINE OBVELSUM
      INCLUDE 'AVL.INC'
C---------------------------------------------------------
C     Sums off-body AIC components to get induced velocities and
C     source/doublet induced velocities from freestream VINF
C
C   Output:     
C     VOB     .h.v. induced at off-body pts
C     WOB     total induced at off-body pts
C     WOBSRD  body source/doublet induced velocities at off-body pts
C---------------------------------------------------------
C
      IF(.NOT. LSOL) THEN
        WRITE(*,*) '* Execute flow solution first!'
      ELSE   
C
        IF(.NOT.LOBVEL) THEN
          WRITE(*,*) ' Summing off-body velocities...'
C
          DO I = 1, NOB
            DO K = 1, 3
              VOB(K,I) = 0.0
C--- h.v. velocity at off-body points
              DO J = 1, NVOR
                VOB(K,I) = VOB(K,I) + VOB_GAM(K,I,J)*GAM(J)
              ENDDO
C--- velocity contribution from body sources and doublets
              WOBSRD(K,I) = WOBSRD_U(K,I,1)*VINF(1)
     &                    + WOBSRD_U(K,I,2)*VINF(2)
     &                    + WOBSRD_U(K,I,3)*VINF(3)
     &                    + WOBSRD_U(K,I,4)*WROT(1)
     &                    + WOBSRD_U(K,I,5)*WROT(2)
     &                    + WOBSRD_U(K,I,6)*WROT(3)
C--- total velocity at off-body points
              WOB(K,I) = VOB(K,I) + WOBSRD(K,I) + VINF(K)
            END DO
C        WRITE(*,*) 'IOB,VOB ',I,(VOB(K,I),K=1,3)
C        WRITE(*,*) 'IOB,WOB ',I,(WOB(K,I),K=1,3)
C
          END DO
          LOBVEL = .TRUE.
       ENDIF
      ENDIF
C     
      RETURN
      END ! OBVELSUM


      SUBROUTINE OBOPER
C-------------------------------------------------
C     Coordinates off-body flow surveys
C     Reads points, execute velocity calculations
C     and prints flow survey output
C-------------------------------------------------
      INCLUDE 'AVL.INC'
      INCLUDE 'AVLPLT.INC'
      CHARACTER*4 OPT
      CHARACTER*80 COMARG
      CHARACTER*128 FNAME
      LOGICAL ERROR
C
      REAL    P1(3), P2(3), P3(3), P4(3)
      REAL    RINPUT(20)
      INTEGER IINPUT(20)
      LOGICAL LSAVE
C     
      LUN = 13
 1000 FORMAT(A)
C     
 100  WRITE(*,1110) NOB, NOBMAX
 1110 FORMAT(/'   ======================================'
     &       /'    # off-body points: ',I4,' max: ',I4,
     &      //'    I nput off-body xyz points',
     &       /'    L ine of off-body points',
     &       /'    G rid of off-body points',
     &       /'    R ead  off-body xyz points',
     &       /'    W rite off-body xyz points',
     &       /'    S how  off-body points ',
     &       /'    C lear off-body points ',
     &       /'    T ranslate off-body points ',
     &       /'    P lot flow survey velocities',
     &       /'    F low survey output',
     &       /'    X execute flow survey')
C
      CALL ASKC(' ..Select action^',OPT,COMARG)
      CALL LC2UC(OPT)
C------------------------------------------------------
 101  CONTINUE
      IF    (OPT.EQ.'    ') THEN
        RETURN
C
C---------------------------------
      ELSEIF(OPT(1:1).EQ.'C') THEN
        LOBAIC = .FALSE.
        LOBVEL = .FALSE.
        NOB =  0
C 
C---------------------------------
      ELSEIF(OPT(1:1).EQ.'S') THEN
        IF(NOB.GT.0) THEN
          WRITE(*,*) ' '
          WRITE(*,*) '  I        X             Y             Z'
          DO L = 1, NOB
            WRITE(*,110) L,(ROB(K,L),K=1,3)
          END DO
        ENDIF
 110   FORMAT(I4,3X,3(G14.6))
C 
C---------------------------------
      ELSEIF(OPT(1:1).EQ.'T') THEN
        IF(NOB.GT.0) THEN
         CALL ASKS(' Enter dx,dy,dz translation : ',COMARG)
         IF(COMARG.EQ.' ') THEN
          OPT = 'S'  
          GO TO 101
         ENDIF           
         NINP = 3
         CALL GETFLT(COMARG,RINPUT,NINP,ERROR)
         IF(NINP.EQ.3) THEN
          DO L = 1, NOB
           DO K = 1, 3
            ROB(K,L) = ROB(K,L) + RINPUT(K) 
           END DO
          END DO
         ENDIF
        ENDIF
        LOBAIC = .FALSE.
        LOBVEL = .FALSE.
C
C---------------------------------
      ELSEIF(OPT(1:1).EQ.'I') THEN
 120    CALL ASKS(' Enter x,y,z point : ',COMARG)
        IF(COMARG.EQ.' ') THEN
          OPT = 'S'  
          GO TO 101
        ENDIF           
        NINP = 3
        CALL GETFLT(COMARG,RINPUT,NINP,ERROR)
        IF(NINP.EQ.3) THEN
          ROB(1,NOB+1) = RINPUT(1)
          ROB(2,NOB+1) = RINPUT(2)
          ROB(3,NOB+1) = RINPUT(3)
          NOB = NOB + 1
          LOBAIC = .FALSE.
          LOBVEL = .FALSE.
        ELSE
          OPT = 'S'  
          GO TO 101
        ENDIF
        GO TO 120
C
C---------------------------------
      ELSEIF(OPT(1:1).EQ.'L') THEN
        CALL ASKS(' Enter first x,y,z point of line: ',COMARG)
        IF(COMARG.EQ.' ') GO TO 100
C
        NINP = 3
        CALL GETFLT(COMARG,RINPUT,NINP,ERROR)
        IF(NINP.EQ.3) THEN
          P1(1) = RINPUT(1)
          P1(2) = RINPUT(2)
          P1(3) = RINPUT(3)
        ENDIF
        CALL ASKS(' Enter last x,y,z point of line: ',COMARG)
        IF(COMARG.EQ.' ') GO TO 100
        NINP = 3
        CALL GETFLT(COMARG,RINPUT,NINP,ERROR)
        IF(NINP.EQ.3) THEN
          P2(1) = RINPUT(1)
          P2(2) = RINPUT(2)
          P2(3) = RINPUT(3)
        ENDIF
C
        CALL ASKI(' Enter # of points on line: ',NP)
        IF(NP.GT.0 .AND. NP.LE.NOBMAX-NOB-1) THEN
          DO I = 1,NP
            FRAC = FLOAT(I-1)/FLOAT(NP-1)
            ROB(1,NOB+1) = (1.0-FRAC)*P1(1) + FRAC*P2(1)
            ROB(2,NOB+1) = (1.0-FRAC)*P1(2) + FRAC*P2(2)
            ROB(3,NOB+1) = (1.0-FRAC)*P1(3) + FRAC*P2(3)
            NOB = NOB + 1
          END DO
          LOBAIC = .FALSE.
          LOBVEL = .FALSE.
          OPT = 'S'  
          GO TO 101
        ENDIF
C
C---------------------------------
      ELSEIF(OPT(1:1).EQ.'G') THEN
        WRITE(*,*) ' Mesh points - define 4 points around perimeter'
        CALL ASKS(' Enter x,y,z of 1st grid corner point: ',COMARG)
        IF(COMARG.EQ.' ') GO TO 100
C
        NINP = 3
        CALL GETFLT(COMARG,RINPUT,NINP,ERROR)
        IF(NINP.EQ.3) THEN
          P1(1) = RINPUT(1)
          P1(2) = RINPUT(2)
          P1(3) = RINPUT(3)
        ENDIF
        CALL ASKS(' Enter x,y,z of 2nd grid corner point: ',COMARG)
        IF(COMARG.EQ.' ') GO TO 100
        NINP = 3
        CALL GETFLT(COMARG,RINPUT,NINP,ERROR)
        IF(NINP.EQ.3) THEN
          P2(1) = RINPUT(1)
          P2(2) = RINPUT(2)
          P2(3) = RINPUT(3)
        ENDIF
        CALL ASKS(' Enter x,y,z of 3rd grid corner point: ',COMARG)
        IF(COMARG.EQ.' ') GO TO 100
        NINP = 3
        CALL GETFLT(COMARG,RINPUT,NINP,ERROR)
        IF(NINP.EQ.3) THEN
          P3(1) = RINPUT(1)
          P3(2) = RINPUT(2)
          P3(3) = RINPUT(3)
        ENDIF
        CALL ASKS(' Enter x,y,z of 4th grid corner point: ',COMARG)
        IF(COMARG.EQ.' ') GO TO 100
        NINP = 3
        CALL GETFLT(COMARG,RINPUT,NINP,ERROR)
        IF(NINP.EQ.3) THEN
          P4(1) = RINPUT(1)
          P4(2) = RINPUT(2)
          P4(3) = RINPUT(3)
        ENDIF
C
        CALL ASKI(' Enter # of points between pts 1/2 and 3/4: ',II)
        CALL ASKI(' Enter # of points between pts 2/3 and 4/1: ',JJ)
        IF(II.GT.0 .AND. JJ.GT.0 .AND. II*JJ.LE.NOBMAX-NOB-1) THEN
          DO I = 1,II
            FRACI = -1.0 + 2.0*FLOAT(I-1)/FLOAT(II-1)
            DO J = 1,JJ
              FRACJ = -1.0 + 2.0*FLOAT(J-1)/FLOAT(JJ-1)
              NI = NOB
              DO K = 1, 3  
               ROB(K,NI+1) = ( (1.0-FRACI)*(1.0-FRACJ) *P1(K)
     &                        +(1.0+FRACI)*(1.0-FRACJ) *P2(K)
     &                        +(1.0+FRACI)*(1.0+FRACJ) *P3(K)
     &                        +(1.0-FRACI)*(1.0+FRACJ) *P4(K) ) / 4.0
              END DO
              NOB = NOB + 1
            END DO
          END DO
          LOBAIC = .FALSE.
          LOBVEL = .FALSE.
          OPT = 'S'  
          GO TO 101
        ENDIF
C     
C---------------------------------
      ELSEIF(OPT(1:1).EQ.'R') THEN
C------ read off-body points
        FNAME = COMARG
        IF(FNAME.EQ.' ') THEN
          CALL ASKS('Enter filename of off-body points',FNAME)
          IF(FNAME.EQ.' ') GO TO 100
        ENDIF
C     
        CALL STRIP(FNAME,NFN)
        OPEN(UNIT=LUN,FILE=FNAME(1:NFN),STATUS='OLD',ERR=10)
C
        DO L = NOB, NOBMAX
          READ(LUN,1000,END=5,ERR=5) COMARG
          NINP = 3
          CALL GETFLT(COMARG,RINPUT,NINP,ERROR)
          IF(NINP.EQ.3) THEN
            ROB(1,NOB+1) = RINPUT(1)
            ROB(2,NOB+1) = RINPUT(2)
            ROB(3,NOB+1) = RINPUT(3)
            IF(NOB.LT.NOBMAX) NOB = NOB + 1
            LOBAIC = .FALSE.
            LOBVEL = .FALSE.
          ENDIF
        END DO
C------ list points
 5      CLOSE(LUN)
        LOBAIC = .FALSE.
        LOBVEL = .FALSE.
        OPT = 'S'
        GO TO 101
C
 10     WRITE(*,*)
        WRITE(*,*) '** Open error on file: ', FNAME(1:NFN)

C---------------------------------
      ELSEIF(OPT(1:1).EQ.'W') THEN
C------ write off-body points
        FNAME = COMARG
        IF(FNAME.EQ.' ') THEN     
          CALL ASKS('Enter filename of off-body points',FNAME)
          IF(FNAME.EQ.' ') GO TO 100
        ENDIF 
C     
        CALL STRIP(FNAME,NFN)
        OPEN(UNIT=LUN,FILE=FNAME(1:NFN),STATUS='NEW',ERR=20)
C
        DO L = 1, NOB
          WRITE(LUN,115) (ROB(K,L),K=1,3)
        END DO
        CLOSE(LUN)
 115    FORMAT(2X,3(G14.6))
        GO TO 100
C     
 20     WRITE(*,*)
        WRITE(*,*) '** Open error on file: ', FNAME(1:NFN)
C     
C---------------------------------
      ELSEIF(OPT(1:1).EQ.'F') THEN
C------ print flow survey data
        IF(LOBAIC .AND. LOBVEL) THEN
          LU = LUN
          CALL GETFILE(LU,COMARG)
          IF(LU.LE.-1) THEN
            WRITE(*,*) '* Filename error *'
          ELSEIF(LU.EQ.0) THEN
            WRITE(*,*) '* Data not written'
          ELSE
            CALL OUTOB(LU)
          ENDIF
          IF(LU.NE.5 .AND. LU.NE.6) CLOSE(LU)
C
        ELSE
         WRITE(*,*) '* Execute off-body flow survey first!'
        ENDIF
C     
C---------------------------------
      ELSEIF(OPT(1:1).EQ.'X') THEN
         IF(.NOT. LSOL) THEN
            WRITE(*,*) '* Execute flow solution first!'
         ELSEIF(NOB.LE.0) THEN
            WRITE(*,*) '* No off-body points!'
         ELSE
C------check for new off-body points and generate AIC and velocities
          IF(.NOT. LOBAIC) THEN
            CALL OBAIC
          ENDIF
          IF(.NOT. LOBVEL) THEN
            CALL OBVELSUM
          ENDIF
         ENDIF
C
cc         OPT = 'F'
cc         GO TO 101
C
C---------------------------------
      ELSEIF(OPT(1:1).EQ.'P') THEN
        IF(.NOT. LSOL) THEN
          WRITE(*,*) '* Execute flow solution first!'
        ELSEIF(NOB.LE.0) THEN
            WRITE(*,*) '* No off-body points!'
        ELSE
          IF(.NOT. LOBAIC) THEN
            CALL OBAIC
          ENDIF
          IF(.NOT. LOBVEL) THEN
            CALL OBVELSUM
          ENDIF
C     
C------plot geometry and off-body flow velocities
          LSAVE = LOBPLT
          LOBPLT = .TRUE.
          CALL PLOTVL(AZIMOB, ELEVOB, TILTOB, ROBINV)
          LPLOT = .TRUE.
          LOBPLT = LSAVE
        ENDIF
C     
C------------------------------------------------------
         
C---------------------------------
C---------------------------------
       ELSE
        WRITE(*,*) 'Option not recognized'
       ENDIF
       GO TO 100
C
      END ! OBOPER


      SUBROUTINE OUTOB(LUN)
C
C...PURPOSE  To print out off-body flow survey data
C
C...INPUT    Configuration data for case in labeled commons
C          
C...OUTPUT   Printed output for off-body points on logical unit LUN
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
C...Force components from each body 
      WRITE(LUN,200)
      WRITE (LUN,210) SATYPE,
     &                SREF,CREF,BREF, 
     &                XYZREF(1), XYZREF(2), XYZREF(3)
      WRITE (LUN,212) ALFA/DTR, BETA/DTR, AMACH
C
      WRITE(LUN,215)
      DO IOB = 1, NOB
        WX = WOB(1,IOB)
        WY = WOB(2,IOB)
        WZ = WOB(3,IOB)
        WMAG = SQRT(WX**2 + WY**2 + WZ**2)
        AMCH = AMACH*WMAG
C
        ALF = ATAN2(WZ,WX) / DTR
        BET = ATAN2(WY,WX) / DTR
        CP0 = 1.0 - WMAG**2
C
        VX = VOB(1,IOB) + WOBSRD(1,IOB)
        VY = VOB(2,IOB) + WOBSRD(2,IOB)
        VZ = VOB(3,IOB) + WOBSRD(3,IOB)
        CP1 = -( 2*VX + (1.0-AMCH**2)*VX**2 + VY**2 + VZ**2 )
C
        WRITE (LUN,220) IOB,(ROB(K,IOB),K=1,3),
     &                  WX,WY,WZ,WMAG,AMCH,ALF,BET,CP0
      END DO
C
      WRITE(LUN,200)
C
  200 FORMAT(1X,
     &'---------------------------------------------------------------')
 210  FORMAT (/'**** AVL Off-Body Flow Survey ****',
     &      /5X,A/
     &      5X,'Sref =',G12.4,1X,'Cref =',F12.4,1X,'Bref =',F12.4/
     &      5X,'Xref =',F12.4,1X,'Yref =',F12.4,1X,'Zref =',F12.4)

 212  FORMAT (/2X,'Alpha =',F10.4,5X,'Beta =',F10.4,5X,'Minf =',F10.4)
C     
 215  FORMAT (/'   I',12X,'X',12X,'Y',10X,'Z',
     &        11X,'Vx',11X,'Vy',11X,'Vz',
     &        9X,'Vmag',9X,'Mach',9X,'Alfa',9X,'Beta',10X,'Cp0')
C
 220  FORMAT (I4,3(1X,F12.6),9(1X,F12.6))
 230  FORMAT (/)
C
      RETURN
      END ! OUTOB
