#======================================================#
# Makefile options for Xplot11 library                 #
#   With GNU gfortran and gcc compilers                #
#   Set up or select a set of compile                  #
#   options for your system                            # 
#                                                      #
#   Set PLTLIB to name of library                      #
#   Set DP for real precision                          #
#======================================================#

# Set library name (either libPlt.a or variant with compiler and precision)
PLTLIB = libPlt_gSP.a
#PLTLIB = libPlt_gfortran.a
#PLTLIB = libPlt_gfortranSP.a
#PLTLIB = libPlt_gfortranDP.a  ! use this for DP library if preferred

# Some fortrans need trailing underscores in C interface symbols (see Xwin.c)
# The UNDERSCORE define should work for most of the "unix" fortran compilers

# The DBL_ARGS define is needed to make the double precision pdf interface
# The DEBUG define can be used to debug the pdf interface

DEFINE = -DUNDERSCORE
#DEFINE = -DUNDERSCORE -DDEBUG

FC = gfortran
#CC  = gcc
CC  = cc

# Depending on your system and libraries you might specify an architecture flag
# to gcc/gfortran to give a compatible binary 32 bit or 64 bit 
# use -m32 for 32 bit binary, -m64 for 64 bit binary
MARCH =
#MARCH = -m64

# Fortran double precision (real) flag
DP =
#DP = -fdefault-real-8

CHECK = 
#CHECK = -fdollar-ok -fbounds-check -finit-real=inf -ffpe-trap=invalid,zero

# compile flags may need additions for particular systems, like -std=c99
FFLAGS  = -O2 $(MARCH) $(DP) $(CHECK)
CFLAGS  = -O2 $(MARCH) $(DEFINE)
CFLAGS0 = -O0 $(MARCH) $(DEFINE)

#FFLAGS  = -ggdb -O0 $(MARCH) $(DP) $(CHECK)
#CFLAGS  = -g -O0 $(MARCH) $(DEFINE)
#CFLAGS0 = -g -O0 $(MARCH) $(DEFINE)

AR = ar r
RANLIB = ranlib 
# directory for X libraries
LINKLIB = -L/usr/X11R6/lib -lX11 
# include directory for Xlib.h
INCDIR = -I/opt/X11/include
WOBJ = Xwin2.o
WSRC = xwin11

# setup for LIBHARU pdf library interface (source, includes and libs)
PDFSRC = pdf
PDFOBJ = hpdf-stubs.o
PDFINCDIR = 
PDFLIBDIR = 

# uncomment these for pdf output, be sure to check the include and lib paths
#PDFOBJ = hpdf-interface.o
# Linux setup
#PDFINCDIR = -I/usr/include
#PDFLIBDIR = -L/usr/lib -lhpdf
# Macports setup
#PDFINCDIR = -I/opt/local/include
#PDFLIBDIR = -L/opt/local/lib -lhpdf
# OSX brew setup
#PDFLIBDIR = -I/usr/local/lib
#PDFINCDIR = -L/usr/local/include
