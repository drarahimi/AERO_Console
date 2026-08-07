#***********************************************************************
#    Module:  Makefile (examples directory)
# 
#    Copyright (C) 1996 Harold Youngren, Mark Drela 
# 
#    This library is free software; you can redistribute it and/or
#    modify it under the terms of the GNU Library General Public
#    License as published by the Free Software Foundation; either
#    version 2 of the License, or (at your option) any later version.
#
#    This library is distributed in the hope that it will be useful,
#    but WITHOUT ANY WARRANTY; without even the implied warranty of
#    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
#    Library General Public License for more details.
#
#    You should have received a copy of the GNU Library General Public
#    License along with this library; if not, write to the Free
#    Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
# 
#    Report problems to:    guppy@maine.com 
#                        or drela@mit.edu  
#***********************************************************************

########################################
# makefile for Xplot11 library examples
########################################


###================================================
### This line includes your compiler/make options
### with definitions for compiler and flags

include ../config.make

###================================================

LIB  = ../$(PLTLIB)
LINKLIBS = $(LINKLIB) $(PDFLIBDIR)

#FC = ifort
#FLGS = -r8

FLG = $(FFLAGS)
#FLG = $(FFLAGS) -fbounds-check -finit-real=inf -ffpe-trap=invalid,zero

PROGS = volts volts_old \
        symbols symbolsall \
	squares squaresdoublebuff \
	spectrum cmap2 cmap3 defmap \
        gridtest zoomtest contest \
        mousexy keyinchk


all:  $(PROGS)

examples:  $(PROGS)

test:  $(PROGS)

clean:
	-/bin/rm *.o
	-/bin/rm $(PROGS)
	-/bin/rm plot*.ps
	-/bin/rm plot*.pdf



#Test routines for package

cmap2: cmap2.o 
	$(FC) -o cmap2 cmap2.o $(LIB) $(LINKLIBS)

cmap3: cmap3.o 
	$(FC) -o cmap3 cmap3.o $(LIB) $(LINKLIBS)

contest: contest.o 
	$(FC) -o contest contest.o $(LIB) $(LINKLIBS)

defmap: defmap.o
	$(FC) -o defmap defmap.o $(LIB) $(LINKLIBS)

gridtest: gridtest.o 
	$(FC) -o gridtest gridtest.o $(LIB) $(LINKLIBS)

mousexy: mousexy.o 
	$(FC) -o mousexy mousexy.o $(LIB) $(LINKLIBS)

keyinchk: keyinchk.o 
	$(FC) -o keyinchk keyinchk.o $(LIB) $(LINKLIBS)

spectrum: spectrum.o 
	$(FC) -o spectrum spectrum.o $(LIB) $(LINKLIBS)

squares: squares.o
	$(FC) -o squares squares.o $(LIB) $(LINKLIBS)

squaresdoublebuff: squaresdoublebuff.o
	$(FC) -o squaresdoublebuff squaresdoublebuff.o $(LIB) $(LINKLIBS)

symbols: symbols.o 
	$(FC) -o symbols symbols.o $(LIB) $(LINKLIBS)

symbolsall: symbolsall.o 
	$(FC) -o symbolsall symbolsall.o $(LIB) $(LINKLIBS)

zoomtest: zoomtest.o 
	$(FC) -o zoomtest zoomtest.o $(LIB) $(LINKLIBS)


volts: volts.o 
	$(FC) -o volts volts.o $(LIB) $(LINKLIBS)

volts_old: volts_old.o 
	$(FC) -o volts_old volts_old.o $(LIB) $(LINKLIBS)



volts.o: volts.f
	$(FC) -c $(FLG) $<

volts_old.o: volts_old.f
	$(FC) -c $(FLG) $<

symbols.o: symbols.f
	$(FC) -c $(FLG) $<

symbolsall.o: symbolsall.f
	$(FC) -c $(FLG) $<

squares.o: squares.f
	$(FC) -c $(FLG) $<

squaresdoublebuff.o: squaresdoublebuff.f
	$(FC) -c $(FLG) $<

spectrum.o: spectrum.f
	$(FC) -c $(FLG) $<

cmap2.o: cmap2.f
	$(FC) -c $(FLG) $<

cmap3.o: cmap3.f
	$(FC) -c $(FLG) $<

defmap.o: defmap.f
	$(FC) -c $(FLG) $<

gridtest.o: gridtest.f
	$(FC) -c $(FLG) $<

zoomtest.o: zoomtest.f
	$(FC) -c $(FLG) $<

contest.o: contest.f
	$(FC) -c $(FLG) $<

mousexy.o: mousexy.f
	$(FC) -c $(FLG) $<


#May need to specify these on a brain-dead make system
#.f.o:	$(FC) -c $(FLG) $<
#.c.o:	$(CC) -c $(CFLAGS) $<






