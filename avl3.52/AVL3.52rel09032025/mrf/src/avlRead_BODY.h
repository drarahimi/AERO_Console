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

#ifndef AVLREAD_BODY_H
#define AVLREAD_BODY_H

#ifdef __cplusplus
extern "C" {
#else
#include <stdbool.h>
#endif

typedef struct
{
  char *name;
  double Length, Asurf, Vol, CL, CD, Cm, CY, Cn, Cl;
} avlBodyProp;

typedef struct
{
  double Sref, Bref, Cref;
  double Xref, Yref, Zref;
  int nbody;
  avlBodyProp *body;
} avlBody;

int avlRead_BODY( const char *filename, avlBody *body, bool verbose );
void avlInit_BODY( avlBody *body );
void avlFree_BODY( avlBody *body );

#ifdef __cplusplus
}
#endif

#endif
