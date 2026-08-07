//**********************************************************************
//   Module:  avlRead_SURF.h
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

#ifndef AVLREAD_SURF_H
#define AVLREAD_SURF_H

#ifdef __cplusplus
extern "C" {
#else
#include <stdbool.h>
#endif

typedef struct
{
  char *name;

  // Surface Forces (referred to Sref,Cref,Bref about Xref,Yref,Zref)
  double Area, CL, CD, Cm, CY, Cn, Cl, CDi, CDv;

  // Surface Forces (referred to Ssurf, Cave about root LE on hinge axis)
  double Ssurf, Cave, cl, cd, cdv;

} avlSurfProp;

typedef struct
{
  double Sref, Bref, Cref;
  double Xref, Yref, Zref;
  int nSurf;
  avlSurfProp *surf;
} avlSurf;

int avlRead_SURF( const char *filename, avlSurf *surf, bool verbose );
void avlInit_SURF( avlSurf *surf );
void avlFree_SURF( avlSurf *surf );

#ifdef __cplusplus
}
#endif

#endif
