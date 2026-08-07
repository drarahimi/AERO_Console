C***********************************************************************
C    Module: plt_color.f 
C 
C    Copyright (C) 1996 Harold Youngren, Mark Drela 
C 
C    This library is free software; you can redistribute it and/or
C    modify it under the terms of the GNU Library General Public
C    License as published by the Free Software Foundation; either
C    version 2 of the License, or (at your option) any later version.
C
C    This library is distributed in the hope that it will be useful,
C    but WITHOUT ANY WARRANTY; without even the implied warranty of
C    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
C    Library General Public License for more details.
C
C    You should have received a copy of the GNU Library General Public
C    License along with this library; if not, write to the Free
C    Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
C 
C    Report problems to:    guppy@maine.com 
C                        or drela@mit.edu  
C***********************************************************************
C
C***********************************************************************
C --- Xplot11 color plotting routines
C
C     Version 4.56 01/16/23
C
C     Note:  These routines implement the interface to setup, select and 
C            query colors in the XPLOT11 plot package.
C***********************************************************************
C
C   The default colormap defines these colors and associated color indices
C   (before the user defines any more)...
C        BLACK   =  1
C        WHITE   =  2
C        YELLOW  =  3
C        ORANGE  =  4
C        RED     =  5
C        GREEN   =  6
C        CYAN    =  7
C        BLUE    =  8
C        MAGENTA =  9
C        VIOLET  =  10


      subroutine NEWCOLOR(icol)
C...Sets color by composite color index
C   color is set by the map index (for +icol) 
C   or spectrum index (for -icol)
C     Color map indices      run from 0  ->  N_COLORS
C     Color spectrum indices run from -1 -> -N_SPECTRUM
C
C    (see colormap subroutines below for setting colormap colors)
C
      include 'pltlib.inc'
c
      if(icol.GT.0) then
        if(icol.GT.N_COLOR) then
          write(*,*) 'NEWCOLOR: color index out of bounds: ',
     &                icol,N_COLOR
          return
        endif
        icindex = icol
      else
        if(-icol.GT.N_SPECTRUM) then
          write(*,*) 'NEWCOLOR: spectrum index out of bounds: ',
     &                -icol,N_SPECTRUM
          return
        endif
        icindex = IFIRST_SPECTRUM - icol - 1
      endif
c
C...Skip if this is the current color 
      if(icindex.EQ.I_CLR) return
c
C...Install color command into display primitives list
      I_CLR = icindex
      call putprim(ColorCommand,I_CLR,0.0,0.0)
c
      return
      end


      subroutine GETCOLOR(icol)
C...Returns current foreground color composite index 
C   if icol>0 the index is the color table index (non-spectrum colors)
C   if icol<0 the index is -(color spectrum index) 
C
      include 'pltlib.inc'
      if(I_CLR.ge.IFIRST_SPECTRUM .and. 
     &   I_CLR.le.IFIRST_SPECTRUM+N_SPECTRUM-1) then
        icol = IFIRST_SPECTRUM - I_CLR - 1
      else
        icol = I_CLR
      endif
      return
      end


      subroutine GETCOLORINDEX(icindex)
C...Returns color table index (not spectrum color index) 
C   of current foreground color table index (icindex runs from 0 -> N_COLOR)
C
      include 'pltlib.inc'
      icindex = I_CLR
      return
      end


      subroutine NEWCOLORNAME(colorname)
C...Sets color for plotting by named string 
C   (to circumvent knowing the color table index)
C   Valid color names (either upper or lower case) are found by 
C   running the X11 command: showrgb
C
      character colorname*(*), colorin*22
      include 'pltlib.inc'

C...Convert input color to uppercase
      colorin = colorname
      call convrt2uc(colorin)
c
C...Search for color name in current colortable
      do ic = 1, N_COLOR
c       write(*,*) 'colorbyname table ic=',ic,' ',colorin,' ',
c     &             COLOR_NAME(ic),' ci ',G_COLOR_CINDEX(ic)
        if(colorin.eq.COLOR_NAME(ic)) then
c         write(*,*) 'NEWCOLORNAME table:', colorin(1:12), ic
          call NEWCOLOR(ic)
          return  
        endif
      end do
c
C...Add new color to colortable
C...Get RGB components for named color
      call gw_cname2rgb(colorname,ired1,igrn1,iblu1)
c      write(*,*) 'NEWCOLORNAME new rgb:', colorname, ired1,igrn1,iblu1
c
      if (ired1.ge.0) then
        N = N_COLOR + 1
        if(N.gt.Ncolors_max) then
          write(*,*) 'NEWCOLORNAME: New color not added --',
     &               ' would exceed color table size.'
          return
        endif

        COLOR_RGB1(N) = iblu1 + 256*(igrn1 + 256*ired1)

        iblu2 = ifix( float(iblu1)*COL12FAC + 0.5 )
        igrn2 = ifix( float(igrn1)*COL12FAC + 0.5 )
        ired2 = ifix( float(ired1)*COL12FAC + 0.5 )
        COLOR_RGB2(N) = iblu2 + 256*(igrn2 + 256*ired2)

        G_COLOR_CINDEX(N) = -1
        COLOR_NAME(N) = colorin
        N_COLOR = N
        call NEWCOLOR(N)
c      write(*,*) 'NEWCOLORNAME new N_COLOR:', N_COLOR

      else
        write(*,*)
     &        'NEWCOLORNAME: Color not found ',colorname
      endif
c
      return
      end ! NEWCOLORNAME


      subroutine NEWCOLORRGB(ired,igrn,iblu)
C...Sets color for plotting by R,G,B components
C   (to circumvent knowing the color table index)
C   Valid color components for red,green,blue run from 0-255
C
      include 'pltlib.inc'
c
C...Search for r,g,b color in current colortable
      do ic = 1, N_COLOR
        irgb = iblu + 256*(igrn + 256*ired)
c        write(*,*) 'NEWCOLORRGB table ic=',ic,' ',irgb,' ',
c     &             COLOR_RGB1(ic),
c     &             COLOR_NAME(ic),' ci ',G_COLOR_CINDEX(ic)
        if(irgb.eq.COLOR_RGB1(ic) .or. 
     &     irgb.eq.COLOR_RGB2(ic)     ) then
          call NEWCOLOR(ic)
          return  
        endif
      end do

      N = N_COLOR + 1
      if(N .gt. Ncolors_max) then
        write(*,*)
     &      'NEWCOLORRGB: Colortable overflow. New color ignored.'
c------ use black instead
        call NEWCOLOR(1)
        return
      endif
      G_COLOR_CINDEX(N) = -1

      ired1 = ired
      igrn1 = igrn
      iblu1 = iblu
      irgb1 = iblu1 + 256*(igrn1 + 256*ired1) 

      iblu2 = ifix( float(iblu1)*COL12FAC + 0.5 )
      igrn2 = ifix( float(igrn1)*COL12FAC + 0.5 )
      ired2 = ifix( float(ired1)*COL12FAC + 0.5 )
      irgb2 = iblu2 + 256*(igrn2 + 256*ired2)

      COLOR_RGB1(N) = irgb1
      COLOR_RGB2(N) = irgb2
      COLOR_NAME(N) = 'RGBCOLOR'
      N_COLOR = N
      call NEWCOLOR(N)

      return
      end ! NEWCOLORRGB



      subroutine GETCOLORRGB(icol,ired,igrn,iblu,colorname)
C...Gets color information for color designated by index icol
C                 if icol<=0, color -icol in Spectrum is queried
C   Returns   ired    - red   color component (0-255)   (-1 if no color)
C             igrn    - green color component (0-255)   (-1 if no color)
C             iblu    - blue  color component (0-255)   (-1 if no color)
C             colorname - name of current color (lowercase)
C
      include 'pltlib.inc'
      character*(*) colorname
C
C...First assume color "icol" does not exist
      ired = -1
      igrn = -1
      iblu = -1
      colorname = ' '
c
      if(icol.GT.0) then
        ic = icol
      else
        if(-icol.GT.N_SPECTRUM) then
          write(*,*) 'GETCOLORRGB: spectrum index out of bounds: ',
     &                -icol,N_SPECTRUM
          return
        endif
        ic = IFIRST_SPECTRUM - icol - 1
      endif
c
      if(ic.GT.N_COLOR) then
          write(*,*) 'GETCOLORRGB: color index out of bounds: ',
     &                ic,N_COLOR
        return
      endif
c
      if(LGW_REVVIDEO .AND. (.NOT.LPS_GEN)) THEN
       irgb = COLOR_RGB1(ic)
      else
       irgb = COLOR_RGB2(ic)
      endif

      irg = irgb/256 
      ired = irg/256 
      igrn = irg  - 256*ired
      iblu = irgb - 256*irg
      colorname = COLOR_NAME(ic)
c
      return
      end ! GETCOLORRGB


      subroutine convrt2uc(input)
C...Convert string to uppercase
C   Note that the string must be writeable (a variable, not a constant)
c
      character*(*) input
      character*26 lcase, ucase
      data lcase /'abcdefghijklmnopqrstuvwxyz'/
      data ucase /'ABCDEFGHIJKLMNOPQRSTUVWXYZ'/
c
      n = len(input)
      do i=1, n
        k = index(lcase, input(i:i))
        if(k.gt.0) input(i:i) = ucase(k:k)
      end do

      return
      end 


      subroutine GETNUMCOLOR(ncol)
C...Gets current number of defined colors
C
      include 'pltlib.inc'
      ncol = N_COLOR
      return
      end


      subroutine GETNUMSPECTRUM(nspec)
C...Gets current number of defined colors in Spectrum
C
      include 'pltlib.inc'
      nspec = N_SPECTRUM
      return
      end


      subroutine COLORMAPDEFAULT
C------------------------------------------------------------------------
C   Creates default colormap palette containing a small number of basic 
C   colors defined in DATA statement below.  The first two colors 
C   are used as the default foreground and background.
C
C   The default colormap contains these defined colors
C        BLACK   =  1
C        WHITE   =  2
C        YELLOW  =  3
C        ORANGE  =  4
C        RED     =  5
C        GREEN   =  6
C        CYAN    =  7
C        BLUE    =  8
C        MAGENTA =  9
C        VIOLET  =  10 = NCMAP
C
C   These colors are then accessible by a normal NEWCOLOR(icol) call:
C       icol = 1 .. NCMAP
C
C   Also installs the RGB components of these colors and these color
C   names in the color table.  The colorindex is set to -1 to indicate 
C   that the color has not yet been mapped to the screen color hardware 
C   (this step happens the first time the color is actually used).
C
      include 'pltlib.inc'
c
      PARAMETER (NCMAP=10)
C
      INTEGER      DEFCMAPRGB1(3,NCMAP),
     &             DEFCMAPRGB2(3,NCMAP)
      CHARACTER*10 DEFCMAPNAMES(NCMAP)
c
      SAVE ifirst
      DATA ifirst / 0 /
c
C---- hues for reverse-video (black background), use full saturation
      DATA  ((DEFCMAPRGB1(L,I),L=1,3),I=1,NCMAP) 
     &              /    0,   0,   0,   ! black  
     &                 255, 255, 255,   ! white  
     &                 255,   0,   0,   ! red    
     &                 255, 165,   0,   ! orange 
     &                 255, 255,   0,   ! yellow 
     &                   0, 245,   0,   ! green  
     &                   0, 255, 255,   ! cyan   
     &                   0,  90, 255,   ! blue   
     &                 160,   0, 255,   ! violet 
     &                 255,   0, 255 /  ! magenta
C
C---- hues for regular-video (white background), use partial saturation
c-     Colormap 2a
c      DATA  ((DEFCMAPRGB2(L,I),L=1,3),I=1,NCMAP) 
c     &              /    0,   0,   0,   ! black  
c     &                 255, 255, 255,   ! white  
c     &                 255,   0,   0,   ! red    
c     &                 255, 150,   0,   ! orange 
c     &                 255, 220,   0,   ! yellow 
c     &                   0, 190,   0,   ! green  
c     &                   0, 215, 225,   ! cyan   
c     &                   0,  75, 255,   ! blue   
c     &                 165,   0, 255,   ! violet 
c     &                 255,   0, 255 /  ! magenta
C
C---- hues for regular-video (white background), even less saturation
c-     Colormap 2b
      DATA  ((DEFCMAPRGB2(L,I),L=1,3),I=1,NCMAP) 
     &              /    0,   0,   0,   ! black  
     &                 255, 255, 255,   ! white  
     &                 235,   0,   0,   ! red    
     &                 235, 130,   0,   ! orange 
     &                 235, 200,   0,   ! yellow 
     &                   0, 170,   0,   ! green  
     &                   0, 195, 205,   ! cyan   
     &                   0,  55, 235,   ! blue   
     &                 145,   0, 235,   ! violet 
     &                 235,   0, 235 /  ! magenta
C
       DATA  DEFCMAPNAMES
     &               / 'BLACK     ',
     &                 'WHITE     ',
     &                 'RED       ',
     &                 'ORANGE    ',
     &                 'YELLOW    ',
     &                 'GREEN     ',
     &                 'CYAN      ',
     &                 'BLUE      ',
     &                 'VIOLET    ',
     &                 'MAGENTA   ' /
C
c      write(*,*) 'Entering COLORMAPDEFAULT. N_COLOR N_SPECTRUM =',    !@@@
c     &    N_COLOR, N_SPECTRUM
c 
C---Initialize the colormap indices for first call
       if(ifirst.EQ.0) then
        N_COLOR = 0
        N_SPECTRUM = 0
        IFIRST_SPECTRUM = 0
        ifirst = 1
       endif
C
C--- Skip installing new default map if there already are NCMAP colors
      if(N_COLOR.EQ.NCMAP) return
C
C--- Flush current colormap if any, to free up space for new map
      if(N_COLOR.GT.0) call gw_newcmap
c
C--- Fill in the colormap with with the default colors and set colorindex 
C     to -1 to indicate that the color is still unallocated by hardware
C
      do n = 1, NCMAP
        ired = DEFCMAPRGB1(1,n)
        igrn = DEFCMAPRGB1(2,n)
        iblu = DEFCMAPRGB1(3,n)
        COLOR_RGB1(n) = iblu + 256*(igrn + 256*ired)

        ired = DEFCMAPRGB2(1,n)
        igrn = DEFCMAPRGB2(2,n)
        iblu = DEFCMAPRGB2(3,n)
        COLOR_RGB2(n) = iblu + 256*(igrn + 256*ired)

        COLOR_NAME(n) = DEFCMAPNAMES(n)
        G_COLOR_CINDEX(n) = -1
      end do
C
      N_COLOR = NCMAP
c      write(*,*) 'Exiting  COLORMAPDEFAULT. N_COLOR N_SPECTRUM =',    !@@@
c     &    N_COLOR, N_SPECTRUM
C
      return
      end ! COLORMAPDEFAULT


      subroutine COLORSPECTRUMHUES(NCOLS,HUESTR)
      character*(*) HUESTR
C--------------------------------------------------------------------------
C   Sets up a color "Spectrum" table that gives a continuous blend
C   between a small number of base colors specified in the character 
C   string HUESTR, which can be "KWROYGCBM" or a subset thereof
C   (K = black).  But the non-monochrome colors ROYGCBM should be 
C   in that order, or in reverse order "M..R", else the resulting 
C   spectrum will be non-monotonic and look terrible.
C
C   The RGB components associated with each specified color are set in 
C   the DATA statement below. These colors are appended to any existing 
C   colormap data, typically set up by COLORMAPDEFAULT.
C
C   These Spectrum colors are then accessible by NEWCOLOR(-icol)
C       -icol = 1 .. NCOLS
C
C   If NCOLS is too big for the definable number of colors,
C   it will be reset to the max allowable value.
C
C NOTE: The maximum number of colors available to the Spectrum is LESS 
C       than the screen depth would indicate.  Some of the X colormap 
C       is used by other X window applications, typically this will be 
C       around 30-40 colormap entries. So, for an 8 bit depth, this 
C       leaves around 220 or so available for use, only 210 or so after 
C       the Palette colors (typ. 10) are assigned.  Less are available 
C       if other applications are using the X colormap.
C--------------------------------------------------------------------------
      include 'pltlib.inc'
C
C---- arrays for setting up rgb tables below
      parameter (NRGBT = 9)
      INTEGER IRGBTABLE1(3,NRGBT),
     &        IRGBTABLE2(3,NRGBT)
      REAL COLORWIDTH1(NRGBT),
     &     COLORWIDTH2(NRGBT)
      CHARACTER*(NRGBT) COLORCHARS

C---- Set up RGB components of the Spectrum-defining base colors
C-      COLWIDTH controls the relative extent of that defining color
      DATA COLORCHARS / 'KWROYGCBM' /

C---- hues for reverse-video (black background), use full saturation
      DATA ((IRGBTABLE1(L,I),L=1,3), COLORWIDTH1(I), I=1, NRGBT) 
     &               /   0,   0,   0,  0.7,   ! black
     &                 255, 255, 255,  0.7,   ! white  
     &                 255,   0,   0,  1.0,   ! red    
     &                 255, 165,   0,  1.0,   ! orange 
     &                 255, 255,   0,  1.3,   ! yellow 
     &                   0, 255,   0,  0.7,   ! green  
     &                   0, 255, 255,  1.3,   ! cyan   
     &                   0,  75, 255,  1.0,   ! blue   
     &                 255,   0, 255,  1.1 /  ! magenta

C---- hues for regular-video (white background), use partial saturation
C-     Color map 2a
c      DATA ((IRGBTABLE2(L,I),L=1,3), COLORWIDTH2(I), I=1, NRGBT) 
c     &               /   0,   0,   0,  0.7,   ! black
c     &                 245, 245, 245,  0.7,   ! white  
c     &                 255,   0,   0,  1.0,   ! red    
c     &                 255, 150,   0,  1.0,   ! orange 
c     &                 255, 220,   0,  1.3,   ! yellow 
c     &                   0, 190,   0,  0.7,   ! green  
c     &                   0, 215, 225,  1.3,   ! cyan   
c     &                   0,  75, 255,  1.0,   ! blue   
c     &                 255,   0, 255,  1.1 /  ! magenta

C---- hues for regular-video (white background), even less saturation
C-     Color map 2b
      DATA ((IRGBTABLE2(L,I),L=1,3), COLORWIDTH2(I), I=1, NRGBT) 
     &               /   0,   0,   0,  0.7,   ! black
     &                 235, 235, 235,  0.7,   ! white  
     &                 235,   0,   0,  1.0,   ! red    
     &                 235, 130,   0,  1.0,   ! orange 
     &                 235, 200,   0,  1.3,   ! yellow 
     &                   0, 170,   0,  0.7,   ! green  
     &                   0, 195, 205,  1.3,   ! cyan   
     &                   0,  55, 235,  1.0,   ! blue   
     &                 235,   0, 235,  1.1 /  ! magenta

      call SPECTRUMSETUP(NCOLS,HUESTR,
     &                   NRGBT,COLORCHARS,
     &                   IRGBTABLE1,COLORWIDTH1,
     &                   IRGBTABLE2,COLORWIDTH2 )

      return
      end ! COLORSPECTRUMHUES



      subroutine COLORSPECTRUMTRP(ncols,NBASE,IRGBBASE,COLWIDTH)
C...Interpolates a color "Spectrum" table of 1..ncols colors that are 
C   a continuous blend between a small number of defined base colors.
C   The blending between the base colors is controlled by the color 
C   "width" COLWIDTH.  
C
C  Input:
C     ncols    number desired interpolated colors in spectrum
C     NBASE    number base colors defined in IRGBBASE
C     IRGBBASE array(3,*) of integer RGB components for the base colors
C     COLWIDTH color pseudo "width" to use for interpolation
C
C   Overwrites the definition of any existing Spectrum.
C
C
      DIMENSION IRGBBASE(3,NBASE)
      DIMENSION COLWIDTH(NBASE)
C
      include 'pltlib.inc'
C
      DIMENSION COLAXIS(NColors_max), IRGBTBL(3,NColors_max)
c
      if(NBASE.GT.NColors_max)
     &  STOP 'COLORSPECTRUM: Local IRGBBASE array overflow.'
C
C
C---Don't allow less than 2 spectrum colors defined by interpolation table
      if(ncols.LT.2) return
c
C--- Check to make sure we have enough room in the color table
      if(N_COLOR+ncols+1 .gt. Ncolors_max) then
        write(*,*) 'COLORSPECTRUMTRP: Too many colors specified.'
        return
      endif
C
      COLAXIS(1) = 0.
      do ibase=2, NBASE
        COLAXIS(ibase) = COLAXIS(ibase-1)
     &                + 0.5*(COLWIDTH(ibase-1)+COLWIDTH(ibase))
        if(COLAXIS(ibase) .LE. COLAXIS(ibase-1))
     &   STOP 'COLORSPECTRUM: Non-monotonic color axis. Check COLWIDTH.'
      enddo
C
C--- Now fill in the rgb table for the Spectrum colors, 
C    interpolating colors between the entries in the passed-in color table
      ibase = 1
      do i = 1, ncols
        xcol = COLAXIS(NBASE) * float(i-1)/float(ncols-1)
c
 5      xnorm = (xcol            -COLAXIS(ibase))
     &        / (COLAXIS(ibase+1)-COLAXIS(ibase))
c
        if(xnorm.GT.1.0  .AND.  ibase.LT.NBASE) then
          ibase = ibase + 1
          go to 5
        endif
c
        w0 = COLWIDTH(ibase  )
        w1 = COLWIDTH(ibase+1)
        frac = w1*xnorm / (w0 + (w1-w0)*xnorm)
C
        red0 = float(IRGBBASE(1,ibase)  )
        grn0 = float(IRGBBASE(2,ibase)  )
        blu0 = float(IRGBBASE(3,ibase)  )
        red1 = float(IRGBBASE(1,ibase+1))
        grn1 = float(IRGBBASE(2,ibase+1))
        blu1 = float(IRGBBASE(3,ibase+1))
c
        IRGBTBL(1,i) = ifix( (red0 + frac*(red1-red0)) + 0.5 )
        IRGBTBL(2,i) = ifix( (grn0 + frac*(grn1-grn0)) + 0.5 )
        IRGBTBL(3,i) = ifix( (blu0 + frac*(blu1-blu0)) + 0.5 )
      end do
      call COLORSPECTRUMRGB(ncols,IRGBTBL)
c
      return
      end

      
      subroutine COLORSPECTRUMRGB(NRGB,IRGB)
C...Sets up a color "Spectrum" table for NRGB colors that are 
C   defined by r,g,b values (0-255) in the IRGB array. 
C
C Input:
C     NRGB    number r,g,b colors defined in IRGB
C     IRGB    array(3,*) of integer RGB components for the colors
C
C   Overwrites any existing Spectrum.
C
      DIMENSION IRGB(3,NRGB)
C
      include 'pltlib.inc'
C
      if(N_COLOR.LE.0 .OR. N_COLOR.GT.10) then
        CALL COLORMAPDEFAULT
      endif
C
C--- Check to make sure we have enough room in the color table
      if(N_COLOR+NRGB .gt. Ncolors_max) then
        write(*,*) 'COLORSPECTRUMRGB: Too many colors specified.'
        return
      endif
C
C--- starting index of Spectrum in colormap arrays
      IFIRST_SPECTRUM = N_COLOR + 1
C
C--- Now fill in the Spectrum colors from the passed-in color table
      do i = 1, NRGB
        ired = IRGB(1,i)
        igrn = IRGB(2,i)
        iblu = IRGB(3,i)
C
        IC = IFIRST_SPECTRUM + i - 1
c
        COLOR_RGB1(IC)  = iblu + 256*(igrn + 256*ired)
        COLOR_NAME(IC) = 'SPECTRUM'
        G_COLOR_CINDEX(IC) = -1
      end do
c
      N_SPECTRUM = NRGB
      N_COLOR = IC
c     write(*,*) 'COLORSPECTRUMRGB: NCOLOR,NSPECTRUM ',N_COLOR,N_SPECTRUM
c
      return
      end

      subroutine SPECTRUMSETUP(NCOLS,HUESTR,
     &                         NRGBT,COLORCHARS,
     &                         IRGBTABLE1,COLORWIDTH1,
     &                         IRGBTABLE2,COLORWIDTH2 )
      CHARACTER*(*) HUESTR
      INTEGER IRGBTABLE1(3,NRGBT),
     &        IRGBTABLE2(3,NRGBT)
      REAL COLORWIDTH1(NRGBT),
     &     COLORWIDTH2(NRGBT)
      CHARACTER*(NRGBT) COLORCHARS

      CHARACTER*32 HUESIN

      include 'pltlib.inc'

C---- local arrays for making irgb table from specified HUESTR colors
      parameter (nhuemax = 14)
      INTEGER irgbhue(3,nhuemax,2)
      REAL huewidth(nhuemax,2)
      REAL colaxis(nhuemax)

      CHARACTER*2 cnum

      HUESIN = HUESTR
      NHUESIN = len(HUESTR)

      call convrt2uc(HUESIN(1:NHUESIN))

      if(N_COLOR.LE.0 .OR. N_COLOR.GT.10) then
C----- make sure basic colors are set up
       CALL COLORMAPDEFAULT
      endif
C
C---- set up user hue tables from specified HUESTR colors
      nhue = 0
      do k = 1, NHUESIN
        i = index( COLORCHARS , HUESIN(k:k) )
        if(i.ne.0) then
         nhue = nhue + 1
         ihue = min( nhue , nhuemax)

         irgbhue(1,ihue,1) = IRGBTABLE1(1,i)
         irgbhue(2,ihue,1) = IRGBTABLE1(2,i)
         irgbhue(3,ihue,1) = IRGBTABLE1(3,i)
         huewidth(ihue,1)  = COLORWIDTH1(i)

         irgbhue(1,ihue,2) = IRGBTABLE2(1,i)
         irgbhue(2,ihue,2) = IRGBTABLE2(2,i)
         irgbhue(3,ihue,2) = IRGBTABLE2(3,i)
         huewidth(ihue,2)  = COLORWIDTH2(i)
        endif
      enddo

      if(nhue .gt. nhuemax) then
       write(*,*) 'SPECTRUMSETUP: Too many colors in string.', 
     &            ' Limited to array dimension', nhuemax
       nhue = nhuemax
      endif

C---- check to make sure we have enough room in the color table
      if(N_COLOR+NCOLS+1 .gt. Ncolors_max) then
       write(*,*) 'SPECTRUMSETUP: Number of spectrum colors',
     &            ' limited to array dimension', Ncolors_max
       NCOLS = Ncolors_max - N_COLOR - 1
      endif

C---- starting index of Spectrum in colormap arrays
      IFIRST_SPECTRUM = N_COLOR + 1
C
      izero = ichar('0')

      do 100 ispect = 1, 2
        colaxis(1) = 0.
        do ihue = 2, nhue
          colaxis(ihue) = colaxis(ihue-1)
     &       + 0.5*(huewidth(ihue-1,ispect) + huewidth(ihue,ispect))
          if(colaxis(ihue) .LE. colaxis(ihue-1)) then
           write(*,*) '? Non-monotonic color axis -- bad huewidth data.'
           return
          endif
        enddo

C------ fill in the Spectrum rgb tables COLOR_RGB*,
C-        interpolating colors between the entries in user hue table
        ihue = 1
        do i = 1, NCOLS
          xcol = colaxis(nhue) * float(i-1)/float(NCOLS-1)
c
 5        continue
          xnorm = (xcol           -colaxis(ihue))
     &          / (colaxis(ihue+1)-colaxis(ihue))
          if(xnorm .GT. 1.0  .AND.  ihue .LT. nhue) then
           ihue = ihue + 1
           go to 5
          endif
c
          w0 = huewidth(ihue  ,ispect)
          w1 = huewidth(ihue+1,ispect)
          frac = w1*xnorm / (w0 + (w1-w0)*xnorm)
C
C-------- hues at ends of user hue table interval
          red0 = float(irgbhue(1,ihue  ,ispect))
          grn0 = float(irgbhue(2,ihue  ,ispect))
          blu0 = float(irgbhue(3,ihue  ,ispect))
          red1 = float(irgbhue(1,ihue+1,ispect))
          grn1 = float(irgbhue(2,ihue+1,ispect))
          blu1 = float(irgbhue(3,ihue+1,ispect))

          ired = ifix( (red0 + frac*(red1-red0)) + 0.5 )
          igrn = ifix( (grn0 + frac*(grn1-grn0)) + 0.5 )
          iblu = ifix( (blu0 + frac*(blu1-blu0)) + 0.5 )

          irgb = iblu + 256*(igrn + 256*ired)

C-------- interpolate user rgb table to make spectrum rgb values
          IC = IFIRST_SPECTRUM + i - 1
          if    (ispect.eq.1) then
           COLOR_RGB1(IC) = irgb
          elseif(ispect.eq.2) then
           COLOR_RGB2(IC) = irgb
          endif
        enddo ! next i
 100  continue ! next ispect

      do i = 1, ncols
        IC = IFIRST_SPECTRUM + i - 1

        khun = i / 100
        kten = (i - 100*khun)/10
        kone =  i - 100*khun - 10*kten
        cnum = char(izero+kten) // char(izero+kone)
        COLOR_NAME(IC) = 'SPECTRUM' // cnum
        G_COLOR_CINDEX(IC) = -1
      enddo ! next i

      N_SPECTRUM = ncols
      N_COLOR = IFIRST_SPECTRUM + ncols - 1

c      write(*,*) 'Exiting  SPECTRUMSETUP. N_COLOR N_SPECTRUM =',    !@@@
c     &    N_COLOR, N_SPECTRUM

      return
      end ! SPECTRUMSETUP



      subroutine LWR2UPR(INPUT)
      CHARACTER*(*) INPUT
C
      CHARACTER*26 LCASE, UCASE
      DATA LCASE / 'abcdefghijklmnopqrstuvwxyz' /
      DATA UCASE / 'ABCDEFGHIJKLMNOPQRSTUVWXYZ' /
C
      N = LEN(INPUT)
C
      do I=1, N
        K = INDEX( LCASE , INPUT(I:I) )
        IF(K.GT.0) INPUT(I:I) = UCASE(K:K)
      end do
C
      return
      end 
