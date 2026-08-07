#
# Makefile for AVL3.44 using GNU gfortran
# H.Youngren 1/10/2023, S.Allmaras 2/24/22
#

BINDIR = .
#BINDIR = /home/codes/bin/
INSTALLCMD = install -s

#SRC = ../src
SRC = .

PROGS = avl
AOBJS = avl.o aoper.o amode.o amass.o asetup.o \
amake.o ainput.o aoutput.o aoutmrf.o aero.o atrim.o atpforc.o \
aic.o cdcl.o airutil.o autil.o aoml.o \
aplotvl.o aplottp.o aplotmd.o limits.o \
pltops.o hidden.o \
plsubs.o \
userio.o plutil.o arrow3d.o getvm.o \
spline.o sgutil.o \
second.o

# Original generic matrix solver (slow but self-contained)
MATRIX = matrix.o
#MLIBS =

# Use the lapack solver for faster matrix solves
#MATRIX = matrix-lapack.o
# This requires AVL to be linked with system lapack and blas libraries
#MLIBS = -llapack -lblas
# Alternatively link with the very fast Intel Math Kernel Library
#MLIBS = -lmkl

# Other libraries needed
PLTOBJ = ../plotlib/libPlt.a
EIGOBJ = ../eispack/libeispack.a

###================================================
###  Default compilers and flags
FC = f77
FFLAGS = -O 
DP =
DBG =
LFLG =

PLTLIB =
FTNLIB =
#Subroutine SECOND source file
SECOND = second_g77.f

##--------------------------

# Review flags for desired machine...

##---------------------------------------
##  Setup for gfortran
FC = gfortran
OPT = -O -fomit-frame-pointer
DP =
ACHK =
DBG =
GFIX =
MBITS =
LFLG =

# graphics system libraries (X11 libs and path)
PLTLIB = -L/usr/X11R6/lib -lX11

SECOND = second_g77.f

# gfortran array mismatch fix - if needed for GCC/GFORTRAN version > 8
GFIX = -fallow-argument-mismatch

# debug
OPT = -O0
#DBG= -fbacktrace -Wall -g
DBG= -fbacktrace -ggdb
#ACHK = -fbounds-check -finit-real=inf
#ACHK = -fbounds-check -finit-real=inf -ffpe-trap=invalid,zero,denormal

PDFLIB = 
# use this to link with PDF library (change -L library location for your system)
PDFLIB = -L/opt/local/lib -lhpdf

PLTOBJ = ../plotlib/libPlt_gSP.a $(PDFLIB)

##--------------------------
# double precision (compile appropriate libPlt_gDP)
#DP = -fdefault-real-8
#PLTOBJ = ../plotlib/libPlt_gDP.a $(PDFLIB)
##--------------------------

# use -m32 for 32-bit binary, -m64 for 64-bit (check your system!)
#MBITS = -m64

FFLAGS = $(OPT) $(MBITS) $(ACHK) $(DP) $(GFIX) $(DBG) 

# to make static executable
#LFLG = -static
#LFLG = -g
##---------------------------------------



all:	 $(PROGS)

install: $(PROGS)
	$(INSTALLCMD) $(PROGS) $(BINDIR)

clean:
	-/bin/rm $(PROGS)
	-/bin/rm *.o

$(SRC)/AVL.INC: $(SRC)/AINDEX.INC
	touch $(SRC)/AVL.INC

avl: $(AOBJS) $(MATRIX)
	$(FC) -g -o avl $(AOBJS) $(MATRIX) $(ALIBS) $(MLIBS) $(PLTOBJ) $(EIGOBJ) $(PLTLIB) $(FTNLIB) $(LFLG)
	$(INSTALLCMD) avl $(BINDIR)

dtest: dtest.o
	$(FC) -o dtest dtest.o $(LFLG)

avl.o: $(SRC)/avl.f $(SRC)/AVL.INC $(SRC)/AVLPLT.INC
	$(FC) -c $(FFLAGS) $(SRC)/avl.f
aoper.o: $(SRC)/aoper.f $(SRC)/AVL.INC $(SRC)/AVLPLT.INC
	$(FC) -c $(FFLAGS) $(SRC)/aoper.f
amode.o: $(SRC)/amode.f $(SRC)/AVL.INC $(SRC)/AVLPLT.INC
	$(FC) -c $(FFLAGS) $(SRC)/amode.f
amass.o: $(SRC)/amass.f $(SRC)/AVL.INC
	$(FC) -c $(FFLAGS) $(SRC)/amass.f
asetup.o: $(SRC)/asetup.f $(SRC)/AVL.INC
	$(FC) -c $(FFLAGS) $(SRC)/asetup.f
cdcl.o: $(SRC)/cdcl.f $(SRC)/AVL.INC
	$(FC) -c $(FFLAGS)  $(SRC)/cdcl.f
amake.o: $(SRC)/amake.f $(SRC)/AVL.INC
	$(FC) -c $(FFLAGS) $(SRC)/amake.f
ainput.o: $(SRC)/ainput.f $(SRC)/AVL.INC
	$(FC) -c $(FFLAGS) $(SRC)/ainput.f
aoutput.o: $(SRC)/aoutput.f $(SRC)/AVL.INC
	$(FC) -c $(FFLAGS) $(SRC)/aoutput.f
aoutmrf.o: $(SRC)/aoutmrf.f $(SRC)/AVL.INC
	$(FC) -c $(FFLAGS) $(SRC)/aoutmrf.f
aero.o: $(SRC)/aero.f $(SRC)/AVL.INC
	$(FC) -c $(FFLAGS) $(SRC)/aero.f
getvm.o: $(SRC)/getvm.f $(SRC)/AVL.INC
	$(FC) -c $(FFLAGS) $(SRC)/getvm.f
atrim.o: $(SRC)/atrim.f $(SRC)/AVL.INC
	$(FC) -c $(FFLAGS) $(SRC)/atrim.f
atpforc.o: $(SRC)/atpforc.f $(SRC)/AVL.INC
	$(FC) -c $(FFLAGS) $(SRC)/atpforc.f
aoml.o: $(SRC)/aoml.f $(SRC)/AVL.INC
	$(FC) -c $(FFLAGS) $(SRC)/aoml.f
aplotvl.o: $(SRC)/aplotvl.f $(SRC)/AVL.INC $(SRC)/AVLPLT.INC $(SRC)/MASKS.INC
	$(FC) -c $(FFLAGS) $(SRC)/aplotvl.f
aplottp.o: $(SRC)/aplottp.f $(SRC)/AVL.INC $(SRC)/AVLPLT.INC $(SRC)/MASKS.INC
	$(FC) -c $(FFLAGS) $(SRC)/aplottp.f
aplotmd.o: $(SRC)/aplotmd.f $(SRC)/AVL.INC $(SRC)/AVLPLT.INC $(SRC)/MASKS.INC
	$(FC) -c $(FFLAGS) $(SRC)/aplotmd.f
limits.o: $(SRC)/limits.f $(SRC)/AVL.INC $(SRC)/AVLPLT.INC
	$(FC) -c $(FFLAGS) $(SRC)/limits.f
pltops.o: $(SRC)/pltops.f $(SRC)/AVLPLT.INC
	$(FC) -c $(FFLAGS) $(SRC)/pltops.f
plsubs.o: $(SRC)/plsubs.f $(SRC)/AINDEX.INC
	$(FC) -c $(FFLAGS) $(SRC)/plsubs.f
hidden.o: $(SRC)/hidden.f
	$(FC) -c $(FFLAGS) $(SRC)/hidden.f
matrix.o: $(SRC)/matrix.f
	$(FC) -c $(FFLAGS)  $(SRC)/matrix.f
matrix-lapack.o: $(SRC)/matrix-lapack.f
	$(FC) -c $(FFLAGS)  $(SRC)/matrix-lapack.f
matrix-lapackdp.o: $(SRC)/matrix-lapackdp.f
	$(FC) -c $(FFLAGS)  $(SRC)/matrix-lapackdp.f

aic.o: $(SRC)/aic.f
	$(FC) -c $(FFLAGS) $(SRC)/aic.f
userio.o: $(SRC)/userio.f
	$(FC) -c $(FFLAGS) $(SRC)/userio.f
plutil.o: $(SRC)/plutil.f $(SRC)/MASKS.INC
	$(FC) -c $(FFLAGS) $(SRC)/plutil.f
arrow3d.o: $(SRC)/arrow3d.f $(SRC)/MASKS.INC
	$(FC) -c $(FFLAGS) $(SRC)/arrow3d.f

spline.o: $(SRC)/spline.f
	$(FC) -c $(FFLAGS)  $(SRC)/spline.f
sgutil.o: $(SRC)/sgutil.f
	$(FC) -c $(FFLAGS)  $(SRC)/sgutil.f
airutil.o: $(SRC)/airutil.f
	$(FC) -c $(FFLAGS)  $(SRC)/airutil.f
autil.o: $(SRC)/autil.f
	$(FC) -c $(FFLAGS)  $(SRC)/autil.f

dtest.o: $(SRC)/dtest.f
	$(FC) -c $(FFLAGS) $(SRC)/dtest.f

second.o: $(SRC)/$(SECOND)
	cp $(SRC)/$(SECOND) $(SRC)/second.f
	$(FC) -c $(FFLAGS) $(SRC)/second.f
