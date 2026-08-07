//**********************************************************************
//   Module:  avlRead_VM.c
//
//   Copyright (C) 2022 Mark Drela, Harold Youngren, Steven Allmaras, Marshall Galbraith
//
//   This program is free software; you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation; either version 2 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program; if not, write to the Free Software
//   Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
//**********************************************************************

#include "avlRead_VM.h"
#define AVL_MRF_INTERNAL
#include "read_util.h"

//----------------------------------------------------------------------------//
// VM: shear and bending moments
//
int avlRead_VM( const char *filename, avlVm *vm, bool verbose )
{
  int i, isurf, istrp;
  char *string = NULL, *version = NULL;
  size_t stringsize = 128;
  bool isOK;
  double val[6];
  FILE *fp = NULL;
  avlVmSurf *surf = NULL;

  const char *names[] = {"2Y/Bref", "Vz/(q*Sref)", "Mx/(q*Bref*Sref)"};

  avlLineBuffer line;
  line.line = NULL;
  line.linesize = 0;

  if (vm == NULL) return -1;

  avlFree_VM( vm );

  fp = fopen( filename, "r" );
  if (fp == NULL)
  {
    printf( "ERROR: unable to open %s\n", filename );
    return -1;
  }

  string = (char *) malloc( stringsize * sizeof(char) );
  if (string == NULL)
  {
    printf( "ERROR: malloc error on 'string'\n" );
    goto error;
  }

  version = (char *) malloc( stringsize * sizeof(char) );
  if (version == NULL)
  {
    printf( "ERROR: malloc error on 'version'\n" );
    goto error;
  }

  // VM
  isOK = getLine_string1( fp, "VM", &line, string );
  if (isOK)
  {
    if (strcmp(string, "VM") != 0)
    {
      printf( "ERROR in %s: expected 'VM' file ID but got '%s'\n", __func__, string );
      goto error;
    }
    if (verbose) printf( "%s\n", string );
  }
  else
    goto error;

  // VERSION
  isOK = getLine_string2( fp, "VERSION", &line, string, version );
  if (isOK)
  {
    if (strcmp(string, "VERSION") != 0)
    {
      printf( "ERROR: expected VERSION keyword but got '%s'\n", string );
      goto error;
    }
    if (strcmp(version, "1.0") != 0)
    {
      printf( "ERROR: unexpected VERSION number '%s'\n", version );
      goto error;
    }
    if (verbose) printf( "VERSION = %s\n", version );
  }
  else
    goto error;

  // 'Shear/q and Bending Moment/q vs Y'
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // configuration name
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // 'Mach, alpha, CLtot, beta, Sref, Bref'
  isOK = getLine_realn( fp, "Mach, alpha, CLtot, beta, Sref, Bref", &line, val, 6 );
  if (isOK)
  {
    vm->Mach  = val[0];
    vm->alpha = val[1];
    vm->CLtot = val[2];
    vm->beta  = val[3];
    vm->Sref  = val[4];
    vm->Bref  = val[5];
    if (verbose)
      printf( "Mach = %lf  alpha = %lf  CLtot = %lf  beta = %lf  Sref = %lf  Bref = %lf\n",
              vm->Mach, vm->alpha, vm->CLtot, vm->beta, vm->Sref, vm->Bref );
  }
  else
    goto error;

  // # of surfaces
  isOK = getLine_int1( fp, "# of surfaces", &line, &vm->nSurf );
  if (isOK)
  {
    if (verbose) printf( "# surfaces = %d\n", vm->nSurf );
  }
  else
    goto error;

  // allocated surfaces
  vm->surf = (avlVmSurf *) malloc( vm->nSurf * sizeof(avlVmSurf) );
  if (vm->surf == NULL)
  {
    vm->nSurf = 0;
    printf( "ERROR: malloc error on 'avlBodyProp'\n" );
    goto error;
  }
  for (isurf = 0; isurf < vm->nSurf; isurf++)
  {
    vm->surf[isurf].name = NULL;
    for (i = 0; i < AVL_VM_NSTRP_DATA; i++)
    {
      vm->surf[isurf].data[i].name = names[i];
      vm->surf[isurf].data[i].val = NULL;
    }
  }

  for (isurf = 0; isurf < vm->nSurf; isurf++)
  {
    surf = &vm->surf[isurf];

    // SURFACE
    isOK = getLine_string1( fp, "SURFACE", &line, string );
    if (isOK)
    {
      if (strcmp(string, "SURFACE") != 0)
      {
        printf( "ERROR: expected SURFACE keyword but got '%s'\n", string );
        goto error;
      }
      if (verbose) printf( "%s\n", string );
    }
    else
      goto error;

    // surface name
    isOK = getLine_line( fp, &line );
    if (isOK)
    {
      surf->name = strdup(chop_newline(line.line));
      if (surf->name == NULL) goto error;
      if (verbose) printf( "%s\n", surf->name );
    }
    else
      goto error;

    // 'Surface #, # strips'
    isOK = getLine_int2( fp, "Surface #, # strips", &line, &surf->iSurf, &surf->nStrp );
    if (isOK)
    {
      if (verbose) printf( "isurf = %d  nstrp = %d\n", surf->iSurf, surf->nStrp );
    }
    else
      goto error;

    // add root/tip to list of strips
    surf->nStrp += 2;

    // allocate strip data
    for (i = 0; i < AVL_VM_NSTRP_DATA; i++)
      surf->data[i].val = (double*)malloc(surf->nStrp*sizeof(double));

    // '2Ymin/Bref, 2Ymax/Bref'
    isOK = getLine_real2( fp, "2Ymin/Bref, 2Ymax/Bref", &line, &surf->YminRef, &surf->YmaxRef );
    if (isOK)
    {
      if (verbose) printf( "2Ymin/Bref = %lf  2Ymax/Bref = %lf\n", surf->YminRef, surf->YmaxRef );
    }
    else
      goto error;

    // 'root: 2Y/Bref, Vz/(q*Sref), Mx/(q*Bref*Sref)'
    isOK = getLine_realn( fp, "root: 2Y/Bref, Vz/(q*Sref), Mx/(q*Bref*Sref)", &line, val, AVL_VM_NSTRP_DATA );
    if (isOK)
    {
      istrp = 0;
      for (i = 0; i < AVL_VM_NSTRP_DATA; i++)
        surf->data[i].val[istrp] = val[i];

      if (verbose)
        printf( "2Y/Bref = %e  Vz/(q*Sref) = %e  Mx/(q*Bref*Sref) = %e : root\n", val[0], val[1], val[2] );
    }
    else
      goto error;

    for (istrp = 1; istrp < surf->nStrp-1; istrp++)
    {
      // '2Y/Bref, Vz/(q*Sref), Mx/(q*Bref*Sref)'
      isOK = getLine_realn( fp, "2Y/Bref, Vz/(q*Sref), Mx/(q*Bref*Sref)", &line, val, AVL_VM_NSTRP_DATA );
      if (isOK)
      {
        for (i = 0; i < AVL_VM_NSTRP_DATA; i++)
          surf->data[i].val[istrp] = val[i];

        if (verbose) printf( "2Y/Bref = %e  Vz/(q*Sref) = %e  Mx/(q*Bref*Sref) = %e\n", val[0], val[1], val[2] );
      }
      else
        goto error;
    }

    // 'tip: 2Y/Bref, Vz/(q*Sref), Mx/(q*Bref*Sref)'
    isOK = getLine_realn( fp, "tip: 2Y/Bref, Vz/(q*Sref), Mx/(q*Bref*Sref)", &line, val, AVL_VM_NSTRP_DATA );
    if (isOK)
    {
      istrp = surf->nStrp-1;
      for (i = 0; i < AVL_VM_NSTRP_DATA; i++)
        surf->data[i].val[istrp] = val[i];

      if (verbose) printf( "2Y/Bref = %e  Vz/(q*Sref) = %e  Mx/(q*Bref*Sref) = %e : tip\n", val[0], val[1], val[2] );
    }
    else
      goto error;
  }

  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  if (verbose) printf( "VM file read OK\n" );
  return 0;

  error:
  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  return -1;
}


//----------------------------------------------------------------------------//
//
void avlInit_VM( avlVm *vm )
{
  if (vm == NULL) return;

  vm->surf = NULL;
  vm->nSurf = 0;

  vm->Mach  = 0;
  vm->alpha = 0;
  vm->CLtot = 0;
  vm->beta  = 0;
  vm->Sref  = 0;
  vm->Bref  = 0;
}


//----------------------------------------------------------------------------//
//
void avlFree_VM( avlVm *vm )
{
  int i, n;

  if (vm == NULL) return;

  for (i = 0; i < vm->nSurf; i++)
  {
    avl_free(vm->surf[i].name);
    for (n = 0; n < AVL_VM_NSTRP_DATA; n++)
    {
      avl_free(vm->surf[i].data[n].val);
    }
  }
  avl_free(vm->surf);

  avlInit_VM( vm );
}
