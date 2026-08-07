C***********************************************************************
C    Module:  pdf_subs.f
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
C --- Driver for hardcopy output to PDF files
C
C     Version 4.56 12/19/22
CC
C     Notes:  PDF Plotting coordinates in points (1pt=1/72in.)
C             These routines only create PDF files.
C             
C***********************************************************************

      subroutine pdf_setup(nunit)
C
C---Sets defaults for PDF output
C
C    nunit   specifies logical unit and suffix for name for .pdf output file
C            if nunit<0 output file is named "plotNNN.pdf" where NNN is the
C               plot sequential number (i.e. separate plot files are created 
C               for each plot) 
C            if nunit=0 output file is named "plot.pd"  
C            if nunit>0  output file is named "plotunitUUU.pdf" where UUU is 
C               the number of 'nunit' formatted to 3 integers with leading 0's 
C
      include 'pltlib.inc'
      character numunit*3
C
cc      print *, 'pdf_setup nunit ',nunit
      PX_ORG = 10.
      PY_ORG = 10.
      P_SCALE = 0.
      LPDF_OPEN    = .FALSE. 
      LP_UNSTROKED = .FALSE.
      LP_ONEFILE   = .TRUE.
      I_PAGES = 0
C
C---Default PDF output file is "plot.pdf", specified if nunit=0,
C
C   If user specifies nunit<0 each plot will be printed to a separate
C   file with name assigned as "plotNNN.pdf" where NNN is the sequential 
C   plot number
C
C   If user specifies a logical unit to use for the plot file the logical
C   unit is used for IO and the name assigned is "plotUUU.pdf" where
C   UUU is the logical unit number (0>UUU<1000)
C
      nunit0 = nunit
      if(nunit.EQ.0) then
       PDF_FILE = 'plot.pdf'
      elseif(nunit.EQ.NPRIM_UNIT_DEFAULT .OR. nunit.GT.999) then
       PDF_FILE = 'plot.pdf'
      elseif(nunit.LT.0) then
c       write(*,*) 'PDF_SETUP: separate PDF files used for each plot'
c       write(*,*) '          Using unit ',NPS_UNIT_DEFAULT
c       write(*,*) '          Using files "plot###.pdf"'
       PDF_FILE = 'plot000.pdf'
       LP_ONEFILE = .FALSE.
      else
       write(numunit,10) nunit0
       PDF_FILE = 'plotunit' // numunit // '.pdf'
      endif
 10   format(I3.3)
      return
      end


      subroutine pdf_initialize
C---Initializes PDF plotting and global plot variables
      include 'pltlib.inc'
C
cc      print *, 'pdf_intialize' 
C---- set PS page Portrait/Landscape orientation
      if(I_PAGETYPE.EQ.Page_Portrait .OR. LP_PORT) then
       IPS_MODE = 0
      else
       IPS_MODE = 1
      endif
C
      N_VECS  = 0
C...P_SCALE set so user graphics scales to 1.0inch/(absolute unit) on page
      if(P_SCALE .EQ. 0.0)  P_SCALE = 72.0
      PX_SIZ = P_SCALE*X_PAGE
      PY_SIZ = P_SCALE*Y_PAGE
C
      call pdf_open
C
      return
      end


      subroutine pdf_open
C...Initializes PDF files for plotting commands
      include 'pltlib.inc'
      logical LEXIST, LOPEN
      character*256 PDF_FILE2
      character*1 ans
      character numpage*3
C
      call a_strip(' ',PDF_FILE)
      NCHPDF = index(PDF_FILE,' ') - 1
C
cc      print *, 'pdf_open LPDF_GEN, LPDF_OPEN ', LPDF_GEN, LPDF_OPEN
C.....Start up PDF file
      LOPEN = LPDF_OPEN
      if(LPDF_GEN) then
        if(.NOT.LPDF_OPEN) then
         call pdf_init   
         LPDF_OPEN = .TRUE.
        endif
C
        if(.NOT.LP_ONEFILE) then
          write(numpage,102) N_PAGES
          PDF_FILE = 'plot' // numpage // '.pdf'
          NCHPDF = index(PDF_FILE,' ') - 1
        endif
 102    format(I3.3)

C...  Set up new PDF page in desired orientation
        call pdf_page(IPS_MODE)
C
        if(LOPEN) then
          write(*,1067) PDF_FILE(1:NCHPDF)
        else
          write(*,1066) PDF_FILE(1:NCHPDF)
        endif
C
        N_PAGES = N_PAGES + 1
        I_PAGES = I_PAGES + 1
C
 1066   format(' Writing   PDF to file  ',A,' ...')
 1067   format(' Appending PDF to file  ',A,' ...')
C        
      endif  ! end of PDF setup
C
      LP_UNSTROKED = .TRUE.
      N_VECS = 0
C      
 1100 format(a)
      return
      end


      subroutine pdf_close
C...Closes PDF file for plotting
      include 'pltlib.inc'
C
cc      print *, 'pdf_close LPDF_OPEN ', LPDF_OPEN
      if(LPDF_OPEN) then
C...  Close and save the PDF file
        PDF_FILE=TRIM(ADJUSTL(PDF_FILE))//char(0)
        call pdf_savefile(PDF_FILE)
        LPDF_OPEN = .FALSE.
      endif
C      
      return
      end


      subroutine pdf_endpage
C...Ends PDF page
      include 'pltlib.inc'
C
cc      print*,'pdf_endpage LPDF_OPEN,LP_UNSTROKED ',
cc     &        LPDF_OPEN,LP_UNSTROKED
      if(.NOT.LP_UNSTROKED) return
C
      if(LPDF_OPEN) then
        if(N_VECS.GT.0) then
          call pdf_stroke
        endif
      endif
C
      LP_UNSTROKED = .FALSE.
      N_VECS = 0
C
      return
      end


      subroutine pdf_flush
C...Flushes out buffered plot output to PDF file
      include 'pltlib.inc'
C...Flush out existing lines at old color
      if(LPDF_OPEN) then
        if(N_VECS.GT.0) then
          call pdf_stroke
          N_VECS = 0
        endif
      endif
      return
      end


      subroutine pdf_color(icolor)
C...Sets PDF foreground color from stored RGB colormap
C   Note: The background color for PDF is always white
C         the foreground color is normally black 
C         you get color when color PDF printing is enabled 
C         and the color is set to one of the colors in the color tables
C    icolor =  1 mapped to black
C    icolor =  2 mapped to white
C     ...
C    icolor =  N_COLOR mapped to last color in color table
C   See the colormapping routines in plt_color.f for assigned colors
C
      include 'pltlib.inc'
      character*22 colorname
C
      if(.NOT.LP_COLOR) return
C
C...Flush out existing lines at old color
      if(N_VECS.GT.0) then
        if(LPDF_OPEN) call pdf_stroke
        N_VECS = 0
      endif
C
C---Consult color map for RGB values 
      icol = icolor
      if(N_COLOR.LE.0) icol = 1
      call GETCOLORRGB(icol,ired,igrn,iblu,colorname)
C
      if(LPDF_OPEN) then
        r = float(ired)/255.0
        g = float(igrn)/255.0
        b = float(iblu)/255.0
        call pdf_setcolor(r,g,b)
      endif
C
      return
      end

      subroutine pdf_pen(jpen)
C...Sets PDF line width
      include 'pltlib.inc'
C
      if(.NOT.LPDF_OPEN) return
C
C...Change the line width for new lines
      if(N_VECS.GT.0) then
        N_VECS = 0
        call pdf_stroke
      endif
C
      scalepdfpen = 0.25
      wpen = scalepdfpen*float(jpen)
      call pdf_setlinewidth(wpen)
C
      return
      end

      subroutine pdf_linepattern(lmask)
C...Sets PDF line pattern 
      include 'pltlib.inc'
C
      integer iseg(32)
      data mskall /-1/
      data nsegmax / 8 /
C
      if(.NOT.LPDF_OPEN) return
C
      if(lmask.EQ.0 .OR. lmask.eq.mskall) then
        if(N_VECS.GT.0) then
          call pdf_stroke
        endif
        call pdf_setdash(iseg,0)
C
       else
C...Set line pattern from lower 16 bits of line mask (integer)
C   Note: no more than 10 pattern elements can be written to PS!
        call bitpat(lmask,nseg,iseg)
        nsg = min(nseg,nsegmax)
	if(N_VECS.GT.0) then
          call pdf_stroke
        endif
        call pdf_setdash(iseg,nsg)
      endif
C
      N_VECS = 0
C
      return
      end


      subroutine pdf_line(X1,Y1,X2,Y2)
C
C...Plots vector in absolute coordinates to PDF file
C
C   Note: coordinates are in points, 1/72 in
C
      include 'pltlib.inc'
C
      if(.NOT.LPDF_OPEN) return
C
      PX1 = X1*P_SCALE + PX_ORG
      PY1 = Y1*P_SCALE + PY_ORG
      PX2 = X2*P_SCALE + PX_ORG
      PY2 = Y2*P_SCALE + PY_ORG
      BB_XMAX = MAX(BB_XMAX,PX1,PX2)
      BB_XMIN = MIN(BB_XMIN,PX1,PX2)
      BB_YMAX = MAX(BB_YMAX,PY1,PY2)
      BB_YMIN = MIN(BB_YMIN,PY1,PY2)      
C
C===============================================
C...PDF line plot commands
      if(PX1.EQ.PS_LSTX .AND. PY1.EQ.PS_LSTY .AND. N_VECS.NE.0) then
        call pdf_lineto(PX2,PY2)
      else
C...On a new move limit the number of unstroked path segments
        if(N_VECS.GE.500) then
          call pdf_stroke
	  N_VECS = 0
        endif
        call pdf_moveto(PX1,PY1)
        call pdf_lineto(PX2,PY2)
      endif
C
      PS_LSTX = PX2
      PS_LSTY = PY2
      N_VECS = N_VECS + 1
C
      return
      end


      subroutine pdf_setscale(factor)
C---Resets PDF plot scaling to factor*72pts/in
      include 'pltlib.inc'
C...P_SCALE set so user graphics scales to factor of 1.0inch/(absolute unit)
      P_SCALE = factor*72.
      PX_SIZ = P_SCALE*X_PAGE
      PY_SIZ = P_SCALE*Y_PAGE
      return
      end



      subroutine pdf_polyline(X,Y,n,ifill)
C...Plots polyline to postscript output	
C
C   Note for non-color PDF plots, colors in the colormap spectrum
C   can be used to shade filled polylines with a grey fill spectrum.
C
C   Note: this simply uses the pdf_line routine to put up the path,
C         then fills and strokes the path.  It is important that 
C         the number of points not exceed the stroke limit in pdf_line
C         or it will try to stroke the path we need to fill...
C
      include 'pltlib.inc'
      real mingrey, maxgrey
      real X(n), Y(n)
      data mingrey, maxgrey / 0.10, 0.95 /
      if(n.LE.1) return
C
C...Flush out existing paths
      if(N_VECS.GT.0) then
        if(LPDF_OPEN) call pdf_stroke
        N_VECS = 0
      endif
C
C...If this is not a color plot, re-shade Spectrum color indices with
C   grey shades, from light grey to near black to replace the color shading
C
      if(.NOT.LP_COLOR .AND. N_COLOR.GT.0) then
C...Create grey color for fill
        call GETCOLOR(icol)
        if(icol.EQ.2) then
          grey = 1.0
         elseif(icol.LT.0) then
          ispec = -icol
          greyfrac = float(ispec-1)/float(N_SPECTRUM-1)
          grey = mingrey + (maxgrey-mingrey)*greyfrac
         else
          grey = 0.0
        endif
        if(LPDF_OPEN) call pdf_setgray(grey)
      endif
C
C...Plot polyline path
      X1 = X(1)
      Y1 = Y(1)
      do i = 2, n
        X2 = X(I)
        Y2 = Y(i)
        call pdf_line(X1,Y1,X2,Y2)
        X1 = X2
        Y1 = Y2
      end do
C
      if(ifill.eq.0) then 
C...Not filled, stroke the polyline
        if(LPDF_OPEN) call pdf_closepathstroke
        N_VECS = 0

      else
C...Fill the path
        if(LPDF_OPEN) call pdf_closepathfillstroke
      endif
      N_VECS = 0
C
      return
      end

