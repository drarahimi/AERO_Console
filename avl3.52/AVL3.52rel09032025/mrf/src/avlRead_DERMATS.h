//**********************************************************************
//   Module:  avlRead_DESMATM.h
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

#ifndef AVLREAD_DESMATS_H
#define AVLREAD_DESMATS_H

#include "avlRead_TOT.h"

#ifdef __cplusplus
extern "C" {
#else
#include <stdbool.h>
#endif

typedef struct
{
  char *wrt;
  // z' force CL  : CLd*
  // y force CY   : CYd*
  // x' mom.  Cl' : Cld*
  // y mom.  Cm   : Cmd*
  // z' mom.  Cn  : Cnd*
  // Trefftz drag : CDffd*
  // span eff.    : ed*
  double CLd;
  double CYd;
  double Cld;
  double Cmd;
  double Cnd;
  double CDffd;
  double ed;
} avlDermatSControl;

typedef struct
{
  char *wrt;
  double CLg;
  double CYg;
  double Clg;
  double Cmg;
  double Cng;
  double CDffg;
  double eg;
} avlDermatSDesign;


typedef struct
{
  // Total forces
  avlTot tot;

  // Stability-axis derivatives...
  // alpha, beta
  // z' force CL  : CLa, CLb
  // y force CY   : CYa, CYb
  // x' mom.  Cl' : Cla, Clb
  // y  mom.  Cm  : Cma, Cmb
  // z' mom.  Cn' : Cna, Cnb
  double CLa, CLb;
  double CYa, CYb;
  double Cla, Clb;
  double Cma, Cmb;
  double Cna, Cnb;

  // roll rate  p, pitch rate  q, yaw rate  r
  // z' force CL  : CLp, CLq, CLr
  // y force CY   : CYp, CYq, CYr
  // x' mom.  Cl' : Clp, Clq, Clr
  // y  mom.  Cm  : Cmp, Cmq, Cmr
  // z' mom.  Cn' : Cnp, Cnq, Cnr
  double CLp, CLq, CLr;
  double CYp, CYq, CYr;
  double Clp, Clq, Clr;
  double Cmp, Cmq, Cmr;
  double Cnp, Cnq, Cnr;

  int nCont;
  avlDermatSControl *cont;

  int nDesign;
  avlDermatSDesign *design;

  // Neutral point  Xnp
  double Xnp;

  // Clb Cnr / Clr Cnb
  double spiral;

} avlDermatS;

int avlRead_DERMATS( const char *filename, avlDermatS *mat, bool verbose );
void avlInit_DERMATS( avlDermatS *mat );
void avlFree_DERMATS( avlDermatS *mat );

#ifdef __cplusplus
}
#endif

#endif
