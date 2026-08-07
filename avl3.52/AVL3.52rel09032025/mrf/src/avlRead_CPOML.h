//**********************************************************************
//   Module:  avlRead_CPOML.h
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

#ifndef AVLREAD_CPOML_H
#define AVLREAD_CPOML_H

#ifdef __cplusplus
extern "C" {
#else
#include <stdbool.h>
#endif

typedef struct
{
  double *xlo, *xup, *ylo, *yup, *zlo, *zup;
} avlCpOmlpCoord;

typedef struct
{
  char *name;

  // Component number
  int component;

  // Chordwise, Spanwise elements
  int nChord, nSpan;

  // Y-duplicate flag
  int imags;

  // section indices
  int nSec;
  int *icnt;

  // VERTEX_GRID
  avlCpOmlpCoord vert;

  // ELEMENT_CENTROID
  avlCpOmlpCoord elem;

  // ELEMENT CP
  double *cp_lo, *cp_up;

} avlCpOmlpSurf;

typedef struct
{
  int nsurf;
  avlCpOmlpSurf *surf;
} avlCpOml;

int avlRead_CPOML( const char *filename, avlCpOml *cpoml, bool verbose );
void avlInit_CPOML( avlCpOml *cpoml );
void avlFree_CPOML( avlCpOml *cpoml );

#endif
