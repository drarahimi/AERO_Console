//**********************************************************************
//   Module:  avlRead_BODY.h
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

#ifndef AVLREAD_STRP_H
#define AVLREAD_STRP_H

#ifdef __cplusplus
extern "C" {
#else
#include <stdbool.h>
#endif

#define AVL_NSTRP_DATA 14

typedef struct
{
  // data name
  char *name;

  // nSpan in length
  double *val;

} avlStrpData;

typedef struct
{
  char *name;

  // 'Surface #, # Chordwise, # Spanwise, First strip'
  int iSurf, nChord, nSpan, iStrp;

  // Surface area Ssurf, Ave. chord Cave
  double Ssurf, Cave;

  //Forces referred to Sref, Cref, Bref about Xref, Yref, Zref
  double CLsurf, Clsurf, CYsurf, Cmsurf, CDsurf, Cnsurf, CDisurf, CDvsurf;

  // Forces referred to Ssurf, Cave
  double CL_srf, CD_srf;

  // Strip index
  int *j;

  // Strip data
  avlStrpData data[AVL_NSTRP_DATA];

} avlStrpSurf;

typedef struct
{
  double Sref, Bref, Cref;
  double Xref, Yref, Zref;
  int nSurf;
  avlStrpSurf *surf;
} avlStrp;

int avlRead_STRP( const char *filename, avlStrp *strp, bool verbose );
void avlInit_STRP( avlStrp *strp );
void avlFree_STRP( avlStrp *strp );

#ifdef __cplusplus
}
#endif

#endif
