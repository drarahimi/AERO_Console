//**********************************************************************
//   Module:  avlRead_CNC.h
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

#ifndef AVLREAD_CNC_H
#define AVLREAD_CNC_H

#ifdef __cplusplus
extern "C" {
#else
#include <stdbool.h>
#endif

#define AVL_NCNC_DATA 8

typedef struct
{
  // data name
  const char *name;

  // nstrp in length
  double *val;

} avlCNCData;

typedef struct
{
  int nStrp;

  // Strip data
  avlCNCData data[AVL_NCNC_DATA];

} avlCnc;


int avlRead_CNC( const char *filename, avlCnc *cnc, bool verbose );
void avlInit_CNC( avlCnc* cnc );
void avlFree_CNC( avlCnc *cnc );

#ifdef __cplusplus
}
#endif

#endif
