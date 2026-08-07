C***********************************************************************
C    Module:  cdcl.f
C 
C    Copyright (C) 2020 Mark Drela, Harold Youngren
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

      subroutine cdcl(cdclpol,cl,cd,cd_cl)
c-------------------------------------------------------------------------
c    Returns cd as a function of cl, or drag polar.
c
c    The polar is defined in 4 pieces, between and outside 3 cl,cd pairs,
c    shown as x symbols on diagram below
c
c     1) negative stall region
c     2) parabolic cd(cl) region between negative stall and the drag minimum
c     2) parabolic cd(cl) region between the drag minimum and positive stall
c     4) positive stall region
c
c             clpos,cdpos       <-  region 4 (quadratic above clpos)
c    cl |     x--------      
c       |    /                     
c       |   |                   <-  region 3 (quadratic above clcdmin)
c       |   x clcdmin,cdmin  
c       |   |                
c       |    \                  <-  region 2 (quadratic below clcdmin)
c       |     x_________      
c       |     clneg,cdneg       <-  region 1 (quadratic below clneg)
c       |                             
c       -------------------------
c                       cd
c
c    The 3 cd,cl pairs are specified in the input cdclpol(.) array:
c      cdclpol(1)   cl (clneg) at point before the negative stall drag rise
c      cdclpol(2)   cd (cdneg) at clneg
c      cdclpol(3)   cl (clcdmin) at minimum drag (middle of polar)
c      cdclpol(4)   cd (cdmin) at clcdmin
c      cdclpol(5)   cl (clpos) at point before the positive stall drag rise
c      cdclpol(6)   cd (cdpos) at clpos
c-------------------------------------------------------------------------
      real cdclpol(6)
c
c--- clinc and cdinc control the rate of increase of drag in the stall regions
c    clinc=0.2 forces drag to increase by cdinc over deltacl=0.2 after stall
c    the cdinc term is the cd increment added by deltacl=clinc after stall
      data  clinc, cdinc / 0.2, 0.0500 /
c
      cd    = 0.0
      cd_cl = 0.0

c---- unpack the cd,cl parameters for the polar
      clmin = cdclpol(1)
      cdmin = cdclpol(2)
      cl0   = cdclpol(3)
      cd0   = cdclpol(4)
      clmax = cdclpol(5)
      cdmax = cdclpol(6)
      if(clmax.le.cl0 .or. cl0.le.clmin) then
        write(*,*) '* CDCL: input CL data out of order'
        return
      endif
c
c---- matching parameters that make the slopes smooth at the stall joins
      cdx1 =  2.0*(cdmin-cd0)*(clmin-cl0)/(clmin-cl0)**2 
      cdx2 =  2.0*(cdmax-cd0)*(clmax-cl0)/(clmax-cl0)**2 
      clfac = 1.0 / clinc
c
      if(cl.lt.clmin) then
c------ negative stall region
c-      slope matches lower side, quadratic drag rise
        cd = cdmin
     &        + cdinc*clfac**2 * (cl-clmin)**2
     &        + cdx1*(1.0 - (cl-cl0)/(clmin-cl0))
        cd_cl = cdinc*clfac**2 * (cl-clmin)*2.0
     &        - cdx1                /(clmin-cl0)
c
       elseif(cl.lt.cl0) then
c------ lower quadratic
c-      slope matches negative stall, and has zero slope at min drag point
        cd = cd0
     &        + (cdmin-cd0)*(cl-cl0)**2 /(clmin-cl0)**2  
        cd_cl = (cdmin-cd0)*(cl-cl0)*2.0/(clmin-cl0)**2  
c
       elseif(cl.lt.clmax) then
c------ upper quadratic
c-      slope matches positive stall, and has zero slope at min drag point
        cd = cd0
     &        + (cdmax-cd0)*(cl-cl0)**2 /(clmax-cl0)**2  
        cd_cl = (cdmax-cd0)*(cl-cl0)*2.0/(clmax-cl0)**2  
c
       else
c------ positive stall region
c-      slope matches upper side, quadratic drag rise
        cd = cdmax
     &        + cdinc*clfac**2 * (cl-clmax)**2  
     &        - cdx2*(1.0 - (cl-cl0)/(clmax-cl0))
        cd_cl = cdinc*clfac**2 * (cl-clmax)*2.0
     &        + cdx2                /(clmax-cl0)
      endif
c
      return
      end ! cdcl
