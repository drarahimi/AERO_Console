//**********************************************************************
//   Module:  avlRead_ELE.h
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

#ifndef AVLREAD_ELE_H
#define AVLREAD_ELE_H

#ifdef __cplusplus
extern "C" {
#else
#include <stdbool.h>
#endif

typedef struct
{
  // Strip #, # Chordwise, First Vortex
  int iStrip, nChord, iFirst;

  // Xle, Ave. Chord, Incidence (deg), Yle, Strip Width, Strip Area, Zle, Strip Dihed (deg)
  double Xle_ave, Cave, Incidence, Yle, StrpWidth, StrpArea, Zle, StrpDihed;

  // cl, cd, cdv, cn, ca, cnc, wake dnwsh, cmLE, cm c/4
  double cl, cd, cdv, cn, ca, cnc, wake_dnwsh, cmLE, cm_c4;

  // nChord in length
  double *I, *X, *Y, *Z, *DX, *Slope, *dCp;

} avlEleStrp;

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

  int nStrp;
  avlEleStrp *strp;

} avlEleSurf;

typedef struct
{
  double Sref, Bref, Cref;
  double Xref, Yref, Zref;
  int nSurf;
  avlEleSurf *surf;
} avlEle;

int avlRead_ELE( const char *filename, avlEle *ele, bool verbose );
void avlInit_ELE( avlEle *ele );
void avlFree_ELE( avlEle *ele );

#ifdef __cplusplus
}
#endif

#endif
