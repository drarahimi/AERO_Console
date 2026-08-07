
      SUBROUTINE PLTINI(SCRNFR,IPSLU,IDEV,FACTOR,LPLOT,LLAND)
      LOGICAL LPLOT,LLAND
C
C---- terminate old plot if any
      IF(LPLOT) CALL PLEND
C
C---- initialize new plot
      IF(LLAND) THEN
        SIGNFR =  SCRNFR
      ELSE
        SIGNFR = -SCRNFR
      ENDIF
      CALL PLOPEN(SIGNFR,IPSLU,IDEV)
      LPLOT = .TRUE.
C
cC---- set X-window size in inches (might have been resized by user)
c      CALL GETWINSIZE(XWIND,YWIND)
cC
cC---- draw plot page outline offset by margins
c      CALL NEWPEN(5)
c      IF(XMARG .GT. 0.0) THEN
c        CALL PLOTABS(      XMARG,      YMARG,3)
c        CALL PLOTABS(      XMARG,YPAGE-YMARG,2)
c        CALL PLOTABS(XPAGE-XMARG,      YMARG,3)
c        CALL PLOTABS(XPAGE-XMARG,YPAGE-YMARG,2)
c      ENDIF
c      IF(YMARG .GT. 0.0) THEN
c        CALL PLOTABS(      XMARG,      YMARG,3)
c        CALL PLOTABS(XPAGE-XMARG,      YMARG,2)
c        CALL PLOTABS(      XMARG,YPAGE-YMARG,3)
c        CALL PLOTABS(XPAGE-XMARG,YPAGE-YMARG,2)
c      ENDIF
c      CALL NEWPEN(1)
cC
c      CALL PLOTABS(XMARG,YMARG,-3)
c      CALL NEWCLIPABS( XMARG, XPAGE-XMARG, YMARG, YPAGE-YMARG )
C
      CALL NEWFACTOR(FACTOR)
C
      RETURN
      END


      SUBROUTINE PLSUBS(XC,YC,CHX,STRING,ANGLE,NC,PLFONT)
C----------------------------------------------------------------
C     Plots character string as a subscript with font routine PLFONT.
C
C      XC,YC  = user coordinates of character to be subscripted 
C      CHX    = character width (user coordinates)
C      STRING = subscript character string to plot with NC characters
C      ANGLE  = angle of character (radians, positive is righthanded rotation)
C      NC     = number of subscript characters to plot
C               if NC<0 the length of the string is determined automatically 
C----------------------------------------------------------------
      PARAMETER (PI=3.1415926535897932384)
      PARAMETER (PI2I=0.5/PI)
      PARAMETER (DTR=PI/180.)
C
      CHARACTER*(*) STRING
      EXTERNAL PLFONT
C
C---- subscript character reduction factor, and x,y-shift/chx
      DATA CHFAC, CHDX, CHDY / 0.7, 0.9, -0.4 /
C
      SINA = SIN(ANGLE/DTR)
      COSA = COS(ANGLE/DTR)
C
      X = XC + CHX*(CHDX*COSA - CHDY*SINA)
      Y = YC + CHX*(CHDX*SINA + CHDY*COSA)
      CALL PLFONT(X,Y,CHX*CHFAC,STRING,ANGLE,NC)
C
      RETURN
      END



      SUBROUTINE PLSUPS(XC,YC,CHX,STRING,ANGLE,NC,PLFONT)
C----------------------------------------------------------------
C     Plots character string as a superscript with font routine PLFONT.
C
C      XC,YC  = user coordinates of character to be superscripted
C      CHX    = character width (user coordinates)
C      STRING = superscript character string to plot with NC characters
C      ANGLE  = angle of character (radians, positive is righthanded rotation)
C      NC     = number of superscript characters to plot
C               if NC<0 the length of the string is determined automatically 
C----------------------------------------------------------------
      PARAMETER (PI=3.1415926535897932384)
      PARAMETER (PI2I=0.5/PI)
      PARAMETER (DTR=PI/180.)
C
      CHARACTER*(*) STRING
      EXTERNAL PLFONT
C
C---- superscript character reduction factor, and x,y-shift/chx
      DATA CHFAC, CHDX, CHDY / 0.7, 0.95, 0.7 /
C
      SINA = SIN(ANGLE/DTR)
      COSA = COS(ANGLE/DTR)
C
      X = XC + CHX*(CHDX*COSA - CHDY*SINA)
      Y = YC + CHX*(CHDX*SINA + CHDY*COSA)
      CALL PLFONT(X,Y,CHX*CHFAC,STRING,ANGLE,NC)
C
      RETURN
      END



      SUBROUTINE SCALIT(II,Y,YOFF,YSF)
      DIMENSION Y(II)
C.............................................................
C
C     Y(1:II)  array whose scaling factor is to be determined
C     YOFF     offset of Y array  (Y-YOFF is actually scaled)
C     YSF      Y scaling factor:
C
C            YSF * max(Y(i)-YOFF)  will be < 1, and O(1)
C     hence, 1/YSF is a good max axis-annotation value.
C.............................................................
C
      AG2 = ALOG10(2.0)
      AG5 = ALOG10(5.0)
C
      YMAX = ABS(Y(1) - YOFF)
      DO 10 I=2, II
        YMAX = MAX( YMAX , ABS(Y(I)-YOFF) )
   10 CONTINUE
C
      IF(YMAX .EQ. 0.0) THEN
        YSF = 1.0E8
        RETURN
      ENDIF
C
      YLOG = ALOG10(YMAX)
C
C---- find log of nearest power of 10 above YMAX
      YLOG1 = AINT(YLOG+100.0) - 99.0
 
C---- find log of nearest 2x(power of 10) above YMAX
      YLOG2 = YLOG1 + AG2
      IF(YLOG2-1.0.GT.YLOG) YLOG2 = YLOG2 - 1.0
C
C---- find log of nearest 5x(power of 10) above YMAX
      YLOG5 = YLOG1 + AG5
      IF(YLOG5-1.0.GT.YLOG) YLOG5 = YLOG5 - 1.0
C
C---- find log of smallest upper bound
      GMIN = AMIN1( YLOG1 , YLOG2 , YLOG5 )
C
C---- set scaling factor
      YSF = 10.0**(-GMIN)
C
      RETURN
      END ! SCALIT


      SUBROUTINE PLCIRC(X0,Y0,RAD, NSEG)
      DTR = ATAN(1.0) / 45.0
C
      IF(NSEG.EQ.0) THEN
C------ draw solid circle
        CALL PLOT(X0+RAD,Y0,3)
        DO I=1, 360
          T = FLOAT(I) * DTR
          CALL PLOT(X0+RAD*COS(T),Y0+RAD*SIN(T),2)
        ENDDO
      ELSE
C------ draw dashed circle in NSEG segments
        KSEG = 360/NSEG
        IGAP = MAX( KSEG/5 , 1 )
        DO ISEG=1, NSEG
          I1 = (ISEG-1)*KSEG  + IGAP
          I2 =  ISEG   *KSEG  - IGAP
          IPEN = 3
          DO I=I1, I2
            T = FLOAT(I) * DTR
            CALL PLOT(X0+RAD*COS(T),Y0+RAD*SIN(T),IPEN)
            IPEN = 2
          ENDDO
        ENDDO
      ENDIF
C
      RETURN
      END
      
