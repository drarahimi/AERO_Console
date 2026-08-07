//**********************************************************************
//   Module:  avlRead_VM.h
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

#ifndef AVLREAD_VM_H
#define AVLREAD_VM_H

#ifdef __cplusplus
extern "C" {
#else
#include <stdbool.h>
#endif

#define AVL_VM_NSTRP_DATA 3

typedef struct
{
  // data name
  const char *name;
  // nStrp in length
  double *val;

} avlVmStrpData;

typedef struct
{
  char *name;

  // 'Surface #, # strips
  int iSurf, nStrp;

  // 2Ymin/Bref, 2Ymax/Bref
  double YminRef, YmaxRef;

  // Strip data
  avlVmStrpData data[AVL_VM_NSTRP_DATA];

} avlVmSurf;

typedef struct
{
  char *name;

  double Mach, alpha, CLtot, beta, Sref, Bref;

  int nSurf;
  avlVmSurf *surf;
} avlVm;

int avlRead_VM( const char *filename, avlVm *vm, bool verbose );
void avlInit_VM( avlVm *vm );
void avlFree_VM( avlVm *vm );

#ifdef __cplusplus
}
#endif

#endif
