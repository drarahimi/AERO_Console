//**********************************************************************
//   Module:  avlRead_TOT.h
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

#ifndef AVLREAD_TOT_H
#define AVLREAD_TOT_H

#ifdef __cplusplus
extern "C" {
#else
#include <stdbool.h>
#endif

typedef struct
{
  char *name;
  double val;
} avlTotVar;

typedef struct
{
  char *name;

  // # surfaces, # strips, # vortices
  int nSurf, nStrp, nVort;

  double Sref, Bref, Cref;
  double Xref, Yref, Zref;

  char *title;

  // Alpha, pb/2V, p'b/2V
  double Alpha, pb_2V, pPb_2V;

  // Beta, qc/2V
  double Beta, qc_2V;

  // Mach, rb/2V, r'b/2V
  double Mach, rb_2V, rPb_2V;

  // CXtot, Cltot, Cl'tot
  double CXtot, Cltot, ClPtot;

  // CYtot, Cmtot
  double CYtot, Cmtot;

  // CZtot, Cntot, Cn'tot
  double CZtot, Cntot, CnPtot;

  // CLtot, CDtot
  double CLtot, CDtot;

  // CDvis, CDind
  double CDvis, CDind;

  // Trefftz Plane: CLff, CDff, CYff, e
  double CLff, CDff, CYff, e;

  int nCont;
  avlTotVar *cont;

  int nDesign;
  avlTotVar *design;
} avlTot;


int avlRead_TOT( const char *filename, avlTot *tot, bool verbose );
void avlInit_TOT( avlTot *tot );
void avlFree_TOT( avlTot *tot );

#ifdef __cplusplus
}
#endif

#endif
