//**********************************************************************
//   Module:  avlRead_DESMATB.h
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

#ifndef AVLREAD_DESMATB_H
#define AVLREAD_DESMATB_H

#include "avlRead_TOT.h"

#include <stdbool.h>

typedef struct
{
  char *wrt;
  double CXd;
  double CYd;
  double CZd;
  double Cld;
  double Cmd;
  double Cnd;
} avlDermatBControl;

typedef struct
{
  char *wrt;
  double CXg;
  double CYg;
  double CZg;
  double Clg;
  double Cmg;
  double Cng;
} avlDermatBDesign;


typedef struct
{
  // Total forces
  avlTot tot;

  // Geometry-axis derivatives...
  // axial   vel. u, sideslip vel. v, normal  vel. w
  // x force CX : CXu, CXv, CXw
  // y force CY : CYu, CYv, CYw
  // z force CZ : CZu, CZv, CZw
  // x mom.  Cl : Clu, Clv, Clw
  // y mom.  Cm : Cmu, Cmv, Cmw
  // z mom.  Cn : Cnu, Cnv, Cnw
  double CXu, CXv, CXw;
  double CYu, CYv, CYw;
  double CZu, CZv, CZw;
  double Clu, Clv, Clw;
  double Cmu, Cmv, Cmw;
  double Cnu, Cnv, Cnw;

  // roll rate  p, pitch rate  q, yaw rate  r
  // x force CX : CXp, CXq ,CXr
  // y force CY : CYp, CYq, CYr
  // z force CZ : CZp, CZq, CZr
  // x mom.  Cl : Clp, Clq, Clr
  // y mom.  Cm : Cmp, Cmq, Cmr
  // z mom.  Cn : Cnp, Cnq, Cnr
  double CXp, CXq, CXr;
  double CYp, CYq, CYr;
  double CZp, CZq, CZr;
  double Clp, Clq, Clr;
  double Cmp, Cmq, Cmr;
  double Cnp, Cnq, Cnr;

  int nCont;
  avlDermatBControl *cont;

  int nDesign;
  avlDermatBDesign *design;

} avlDermatB;

int avlRead_DERMATB( const char *filename, avlDermatB *mat, bool verbose );
void avlInit_DERMATB( avlDermatB *mat );
void avlFree_DERMATB( avlDermatB *mat );

#endif
