
FC = gfortran
FLG = -O 
#FLG = -O -m64
AR = ar r
RAN = ranlib

##---------------------------------------
### GNU gfortran
FC = gfortran
# add -m32 or -m64 flag to generate 32 bit or 64 bit for your environment  
#FC = gfortran -m64  

FLG = -O2 -fomit-frame-pointer
# Debug flags (symbols, array bounds)
#FLG = -g -O0 -C

EISLIB = libeispack.a

$(EISLIB): eispack.f
	$(FC) -c $(FLG) $(DP) eispack.f
	$(AR) $(EISLIB) eispack.o
	$(RAN) $(EISLIB)

clean:
	/bin/rm *.o *.a


