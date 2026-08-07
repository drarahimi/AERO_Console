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

#ifndef AVLREAD_DESMATM_H
#define AVLREAD_DESMATM_H

#include "avlRead_TOT.h"

#ifdef __cplusplus
extern "C" {
#else
#include <stdbool.h>
#endif

typedef struct
{
  char *wrt;
  // z force      : CLd*
  // y force      : CYd*
  // roll  x mom. : Cld*
  // pitch y mom. : Cmd*
  // yaw   z mom. : Cnd*
  // Trefftz drag : CDffd*
  // span eff.    : ed*
  double CLd;
  double CYd;
  double Cld;
  double Cmd;
  double Cnd;
  double CDffd;
  double ed;
} avlDermatMControl;

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
} avlDermatMDesign;


typedef struct
{
  // Total forces
  avlTot tot;

  // Derivatives...
  // alpha, beta
  // z force CL   : CLa, CLb
  // y force CY   : CYa, CYb
  // roll  x mom. : Cla, Clb
  // pitch y mom. : Cma, Cmb
  // yaw   z mom. : Cna, Cnb
  double CLa, CLb;
  double CYa, CYb;
  double Cla, Clb;
  double Cma, Cmb;
  double Cna, Cnb;

  // roll rate  p, pitch rate  q, yaw rate  r
  // z force      : CLp, CLq, CLr
  // y force      : CYp, CYq, CYr
  // roll  x mom. : Clp, Clq, Clr
  // pitch y mom. : Cmp, Cmq, Cmr
  // yaw   z mom. : Cnp, Cnq, Cnr
  double CLp, CLq, CLr;
  double CYp, CYq, CYr;
  double Clp, Clq, Clr;
  double Cmp, Cmq, Cmr;
  double Cnp, Cnq, Cnr;

  int nCont;
  avlDermatMControl *cont;

  int nDesign;
  avlDermatMDesign *design;

  // Neutral point  Xnp
  double Xnp;

  // Clb Cnr / Clr Cnb
  double spiral;

} avlDermatM;

int avlRead_DERMATM( const char *filename, avlDermatM *mat, bool verbose );
void avlInit_DERMATM( avlDermatM *mat );
void avlFree_DERMATM( avlDermatM *mat );

#ifdef __cplusplus
}
#endif

#endif
