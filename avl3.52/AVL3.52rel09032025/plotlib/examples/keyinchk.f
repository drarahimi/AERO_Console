C*********************************************************************** 
C    Module:  keyinchk.f
C 
C    Copyright (C) 2025 Harold Youngren, Mark Drela 
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
C
C    Report problems to:    guppy@maine.com 
C                        or drela@mit.edu  
C*********************************************************************** 


      program keyincheck
c---------------------------------------------------------------
c     Keyboard input test program.
c---------------------------------------------------------------
c
      parameter (ipx=100)
c
      character*1  cin
      character*80 line
      logical lok
c
      ch = 0.13
C
 1000 format(a)
 1050 format(/' Enter 1 to test GETCURSORXY, 2 to test GW_CURS: ')
C
C---Initialize the plot package before we get into plotting...
      CALL PLINITIALIZE
      call PLOPEN(0.8,0,1)
      XMSG = 0.5
      YMSG = 1.0
      CALL PLCHAR(XMSG,YMSG,1.2*CH,
     &            'Test for mouse and keyboard input',0.0,-1)

C---put up plot window and refresh
      XMSG = 0.5
      YMSG = 0.5
      CALL PLCHAR(XMSG,YMSG,1.0*CH,
     &            'Enter keystokes, terminate with q',0.0,-1)
c
 5    write(*,1050) 
      read(*,1000) line 
      read(line,*,err=5) itest
      if(itest.EQ.1) then
       write(*,*) 'Testing GETCURSORXY(x,y,chr)'
      elseif(itest.EQ.2) then
       write(*,*) 'Testing low-level GW_CURS(x,y,ikey)'
      else
       go to 5
      endif
C
C--- Get initial set of points from user
      call NEWCOLORNAME('green')
      call NEWPEN(1)
C
      ii = 0
      do j = 1, 100
        if(itest.eq.1) then
         call GETCURSORXY(xx,yy,cin)
         write(*,*) 'getcursorxy -> xx,yy,chr ',xx,yy,cin
        else
         call GW_CURS(xx,yy,ikey)
         ikey2 = iand(127,ikey)
         cin = char(ikey2)
         write(*,*) 'gw_curs-> xx,yy,ikey,chr ',xx,yy,ikey,cin
c         write(*,*) 'ikey cin ',ikey, cin
c        write(*,*) 'ikey2 cin ',ikey2, cin
        endif
c
        if(cin.EQ.'q' .OR. cin.EQ.'Q') go to 20
c
        CALL PLCHAR(xx,yy,1.0*CH,cin,0.0,1)
        CALL PLFLUSH
      end do
C
 20   call plend
ccc      pause
      stop
      end

