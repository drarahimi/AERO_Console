//**********************************************************************
//   Module:  avlRead_test.c
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

#include "avlmrf.h"

#include <stdio.h>
#include <stdlib.h>

//----------------------------------------------------------------------------//
int main()
{
  int status = 0;
  const char *filename = NULL;
  bool verbose = true;
  avlTot tot;
  avlSurf surf;
  avlBody body;
  avlStrp strp;
  avlEle ele;
  avlHinge hinge;
  avlCnc cnc;
  avlVm vm;
  avlDermatM matM;
  avlDermatS matS;
  avlDermatB matB;
  avlCpOml cpoml;

#if 1
  printf("=========================================\n");
  printf( "avlRead_TOT:\n" );
  filename = "ft-mrf.dat";
  avlInit_TOT(&tot);
  status = avlRead_TOT( filename, &tot, verbose );
  printf( "status = %d\n", status );
  avlFree_TOT(&tot);
  if (status != 0) return EXIT_FAILURE;
#endif

#if 1
  printf("=========================================\n");
  printf( "avlRead_SURF:\n" );
  filename = "fn-mrf.dat";
  avlInit_SURF(&surf);
  status = avlRead_SURF( filename, &surf, verbose );
  printf( "status = %d\n", status );
  avlFree_SURF(&surf);
  if (status != 0) return EXIT_FAILURE;
#endif

#if 1
  printf("=========================================\n");
  printf( "avlRead_BODY:\n" );
  filename = "fb-mrf.dat";
  avlInit_BODY(&body);
  status = avlRead_BODY( filename, &body, verbose );
  printf( "status = %d\n", status );
  avlFree_BODY(&body);
  if (status != 0) return EXIT_FAILURE;
#endif

#if 1
  printf("=========================================\n");
  printf( "avlRead_STRP:\n" );
  filename = "fs-mrf.dat";
  avlInit_STRP(&strp);
  status = avlRead_STRP( filename, &strp, verbose );
  printf( "status = %d\n", status );
  avlFree_STRP(&strp);
  if (status != 0) return EXIT_FAILURE;
#endif

#if 1
  printf("=========================================\n");
  printf( "avlRead_ELE:\n" );
  filename = "fe-mrf.dat";
  avlInit_ELE(&ele);
  status = avlRead_ELE( filename, &ele, verbose );
  printf( "status = %d\n", status );
  avlFree_ELE(&ele);
  if (status != 0) return EXIT_FAILURE;
#endif

#if 1
  printf("=========================================\n");
  printf( "avlRead_HINGE:\n" );
  filename = "hm-mrf.dat";
  avlInit_HINGE(&hinge);
  status = avlRead_HINGE( filename, &hinge, verbose );
  printf( "status = %d\n", status );
  avlFree_HINGE(&hinge);
  if (status != 0) return EXIT_FAILURE;
#endif

#if 1
  printf("=========================================\n");
  printf( "avlRead_CNC:\n" );
  filename = "cnc-mrf.dat";
  avlInit_CNC(&cnc);
  status = avlRead_CNC( filename, &cnc, verbose );
  printf( "status = %d\n", status );
  avlFree_CNC(&cnc);
  if (status != 0) return EXIT_FAILURE;
#endif

#if 1
  printf("=========================================\n");
  printf( "avlRead_VM:\n" );
  filename = "vm-mrf.dat";
  avlInit_VM(&vm);
  status = avlRead_VM( filename, &vm, verbose );
  printf( "status = %d\n", status );
  avlFree_VM(&vm);
  if (status != 0) return EXIT_FAILURE;
#endif

#if 1
  printf("=========================================\n");
  printf( "avlRead_DERMATM:\n" );
  filename = "sm-mrf.dat";
  avlInit_DERMATM(&matM);
  status = avlRead_DERMATM( filename, &matM, verbose );
  printf( "status = %d\n", status );
  avlFree_DERMATM(&matM);
  if (status != 0) return EXIT_FAILURE;
#endif

#if 1
  printf("=========================================\n");
  printf( "avlRead_DERMATS:\n" );
  filename = "st-mrf.dat";
  avlInit_DERMATS(&matS);
  status = avlRead_DERMATS( filename, &matS, verbose );
  printf( "status = %d\n", status );
  avlFree_DERMATS(&matS);
  if (status != 0) return EXIT_FAILURE;
#endif

#if 1
  printf("=========================================\n");
  printf( "avlRead_DERMATB:\n" );
  filename = "sb-mrf.dat";
  avlInit_DERMATB(&matB);
  status = avlRead_DERMATB( filename, &matB, verbose );
  printf( "status = %d\n", status );
  avlFree_DERMATB(&matB);
  if (status != 0) return EXIT_FAILURE;
#endif

#if 1
  printf("=========================================\n");
  printf( "avlRead_CPOML:\n" );
  filename = "cpoml.dat";
  avlInit_CPOML(&cpoml);
  status = avlRead_CPOML( filename, &cpoml, verbose );
  printf( "status = %d\n", status );
  avlFree_CPOML(&cpoml);
  if (status != 0) return EXIT_FAILURE;
#endif

  exit(0);
}
