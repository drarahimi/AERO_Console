//**********************************************************************
//   Module:  avlRead_DERMATM.c
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

#include "avlRead_DERMATM.h"
#define AVL_MRF_INTERNAL
#include "read_util.h"

int avlRead_TOT2( FILE *fp, avlTot *tot, bool verbose, char *fileID );

//----------------------------------------------------------------------------//
// DERMATM: stability derivative matrix about stability axes
//
int avlRead_DERMATM( const char *filename, avlDermatM *mat, bool verbose )
{
  int status, icont, idesign;
  char *string = NULL;
  double *val = NULL;
  size_t stringsize = 128;
  bool isOK;
  FILE *fp = NULL;
  avlLineBuffer line;
  line.line = NULL;
  line.linesize = 0;

  if (mat == 0) return -1;

  avlFree_DERMATM( mat );

  fp = fopen( filename, "r" );
  if (fp == NULL)
  {
    printf( "ERROR: unable to open %s\n", filename );
    return -1;
  }

  status = avlRead_TOT2( fp, &mat->tot, verbose, "DERMATM" );
  if (status != 0) goto error;

  string = (char *) malloc( stringsize * sizeof(char) );
  if (string == NULL)
  {
    printf( "ERROR: malloc error on 'string'\n" );
    goto error;
  }

  // blank line
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // 'Derivatives...'
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // 'alpha, beta'
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // 'z force CL   : CLa, CLb'
  isOK = getLine_real2( fp, "z force CL   : CLa, CLb", &line, &mat->CLa, &mat->CLb );
  if (isOK)
  {
    if (verbose)
      printf( "z force CL   : CLa = %lf  CLb = %lf\n", mat->CLa, mat->CLb );
  }
  else
    goto error;

  // 'y force CY   : CYa, CYb'
  isOK = getLine_real2( fp, "y force CY   : CYa, CYb", &line, &mat->CYa, &mat->CYb );
  if (isOK)
  {
    if (verbose)
      printf( "y force CY   : CYa = %lf  CYb = %lf\n", mat->CYa, mat->CYb );
  }
  else
    goto error;

  // 'roll  x mom. : Cla, Clb'
  isOK = getLine_real2( fp, "roll  x mom. : Cla, Clb", &line, &mat->Cla, &mat->Clb );
  if (isOK)
  {
    if (verbose)
      printf( "roll  x mom. : Cla = %lf  Clb = %lf\n", mat->Cla, mat->Clb );
  }
  else
    goto error;

  // 'pitch y mom. : Cma, Cmb'
  isOK = getLine_real2( fp, "pitch y mom. : Cma, Cmb", &line, &mat->Cma, &mat->Cmb );
  if (isOK)
  {
    if (verbose)
      printf( "pitch y mom. : Cma = %lf  Cmb = %lf\n", mat->Cma, mat->Cmb );
  }
  else
    goto error;

  // 'yaw   z mom. : Cna, Cnb'
  isOK = getLine_real2( fp, "yaw   z mom. : Cna, Cnb", &line, &mat->Cna, &mat->Cnb );
  if (isOK)
  {
    if (verbose)
      printf( "yaw   z mom. : Cna = %lf  Cnb = %lf\n", mat->Cna, mat->Cnb );
  }
  else
    goto error;

  // 'roll rate  p, pitch rate  q, yaw rate  r'
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // 'z force      : CLp, CLq, CLr'
  isOK = getLine_real3( fp, "z force      : CLp, CLq, CLr", &line, &mat->CLp, &mat->CLq, &mat->CLr );
  if (isOK)
  {
    if (verbose) printf( "z force      : CLp = %lf  CLq = %lf  CLr = %lf\n", mat->CLp, mat->CLq, mat->CLr );
  }
  else
    goto error;

  // 'y force      : CYp, CYq, CYr'
  isOK = getLine_real3( fp, "y force      : CYp, CYq, CYr", &line, &mat->CYp, &mat->CYq, &mat->CYr );
  if (isOK)
  {
    if (verbose) printf( "y force      : CYp = %lf  CYq = %lf  CYr = %lf\n", mat->CYp, mat->CYq, mat->CYr );
  }
  else
    goto error;

  // 'roll  x mom. : Clp, Clq, Clr'
  isOK = getLine_real3( fp, "roll  x mom. : Clp, Clq, Clr", &line, &mat->Clp, &mat->Clq, &mat->Clr );
  if (isOK)
  {
    if (verbose) printf( "roll  x mom. : Clp = %lf  Clq = %lf  Clr = %lf\n", mat->Clp, mat->Clq, mat->Clr );
  }
  else
    goto error;

  // 'pitch y mom. : Cmp, Cmq, Cmr'
  isOK = getLine_real3( fp, "pitch y mom. : Cmp, Cmq, Cmr", &line, &mat->Cmp, &mat->Cmq, &mat->Cmr );
  if (isOK)
  {
    if (verbose) printf( "pitch y mom. : Cmp = %lf  Cmq = %lf  Cmr = %lf\n", mat->Cmp, mat->Cmq, mat->Cmr );
  }
  else
    goto error;

  // 'yaw   z mom. : Cnp, Cnq, Cnr'
  isOK = getLine_real3( fp, "yaw   z mom. : Cnp, Cnq, Cnr", &line, &mat->Cnp, &mat->Cnq, &mat->Cnr );
  if (isOK)
  {
    if (verbose) printf( "yaw   z mom. : Cnp = %lf  Cnq = %lf  Cnr = %lf\n", mat->Cnp, mat->Cnq, mat->Cnr );
  }
  else
    goto error;

  // # control vars
  isOK = getLine_int1( fp, "# control vars", &line, &mat->nCont );
  if (isOK)
  {
    if (verbose) printf( "# control vars = %d\n", mat->nCont );
  }
  else
    goto error;

  if (mat->nCont > 0)
  {
    mat->cont = (avlDermatMControl*)malloc( mat->nCont * sizeof(avlDermatMControl));
    if (mat->cont == NULL)
    {
      mat->nCont = 0;
      printf( "ERROR: malloc error on 'avlDermatBControl'\n" );
      goto error;
    }
    for (icont = 0; icont < mat->nCont; icont++)
      mat->cont[icont].wrt = NULL;
  
    // control value names
    for (icont = 0; icont < mat->nCont; icont++)
    {
      isOK = getLine_string1( fp, "control value names", &line, string );
      if (isOK)
      {
        mat->cont[icont].wrt = strdup(string);
        if (mat->cont[icont].wrt == NULL) goto error;
        if (verbose) printf( "control name = %d %s\n", icont, string );
      }
      else
        goto error;
    }

    // allocate the val array
    val = (double*)malloc( mat->nCont * sizeof(double));
    if (val == NULL)
    {
      printf( "ERROR: malloc error on 'val'\n" );
      goto error;
    }

    // 'z force      : CLd*'
    isOK = getLine_realn( fp, "z force      : CLd*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].CLd = val[icont];

      if (verbose)
      {
        printf( "z force      : CLd%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "CLd%02d = %lf  ", icont+1, val[icont] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'y force      : CYd*'
    isOK = getLine_realn( fp, "y force      : CYd*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].CYd = val[icont];

      if (verbose)
      {
        printf( "y force      : CYd%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "CYd%02d = %lf  ", icont+1, val[icont] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'roll  x mom. : Cld*'
    isOK = getLine_realn( fp, "roll  x mom. : Cld*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].Cld = val[icont];

      if (verbose)
      {
        printf( "roll  x mom. : Cld%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "Cld%02d = %lf  ", icont+1, val[icont] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'pitch y mom. : Cmd*'
    isOK = getLine_realn( fp, "pitch y mom. : Cmd*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].Cmd = val[icont];

      if (verbose)
      {
        printf( "pitch y mom. : Cmd%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "Cmd%02d = %lf  ", icont+1, val[icont] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'yaw   z mom. : Cnd*'
    isOK = getLine_realn( fp, "yaw   z mom. : Cnd*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].Cnd = val[icont];

      if (verbose)
      {
        printf( "yaw   z mom. : Cnd%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "Cnd%02d = %lf  ", icont+1, val[icont] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'Trefftz drag : CDffd*'
    isOK = getLine_realn( fp, "Trefftz drag : CDffd*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].CDffd = val[icont];

      if (verbose)
      {
        printf( "Trefftz drag : CDffd%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "CDffd%02d = %lf  ", icont+1, val[icont] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'span eff.    : ed*'
    isOK = getLine_realn( fp, "span eff.    : ed*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].ed = val[icont];

      if (verbose)
      {
        printf( "span eff.    : ed%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "ed%02d = %lf  ", icont+1, val[icont] );
        printf( "\n" );
      }
    }
    else
      goto error;
  }

  // # design vars
  isOK = getLine_int1( fp, "# design vars", &line, &mat->nDesign );
  if (isOK)
  {
    if (verbose) printf( "# design vars = %d\n", mat->nDesign );
  }
  else
    goto error;

  if (mat->nDesign > 0)
  {
    // allocate design
    mat->design = (avlDermatMDesign*)malloc( mat->nDesign * sizeof(avlDermatMDesign));
    if (mat->design == NULL)
    {
      mat->nDesign = 0;
      printf( "ERROR: malloc error on 'avlDermatBDesign'\n" );
      goto error;
    }
    for (idesign = 0; idesign < mat->nDesign; idesign++)
      mat->design[idesign].wrt = NULL;
  
    // design value names
    for (idesign = 0; idesign < mat->nDesign; idesign++)
    {
      isOK = getLine_string1( fp, "design value names", &line, string );
      if (isOK)
      {
        mat->design[idesign].wrt = strdup(string);
        if (mat->design[idesign].wrt == NULL) goto error;
        if (verbose) printf( "design name = %d %s\n", idesign, string );
      }
      else
        goto error;
    }

    // allocate the val array
    avl_free(val);
    val = (double*)malloc( mat->nDesign * sizeof(double));
    if (val == NULL)
    {
      printf( "ERROR: malloc error on 'val'\n" );
      goto error;
    }

    // 'z force      : CLg*'
    isOK = getLine_realn( fp, "z force      : CLg*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].CLg = val[idesign];

      if (verbose)
      {
        printf( "z force      : CLg%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "CLg%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'y force      : CYg*'
    isOK = getLine_realn( fp, "y force      : CYg*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].CYg = val[idesign];

      if (verbose)
      {
        printf( "y force      : CYg%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "CYg%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'roll  x mom. : Clg*'
    isOK = getLine_realn( fp, "roll  x mom. : Clg*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].Clg = val[idesign];

      if (verbose)
      {
        printf( "roll  x mom. : Clg%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "Clg%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'pitch y mom. : Cmg*'
    isOK = getLine_realn( fp, "pitch y mom. : Cmg*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].Cmg = val[idesign];

      if (verbose)
      {
        printf( "pitch y mom. : Cmg%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "Cmg%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'yaw   z mom. : Cng*'
    isOK = getLine_realn( fp, "yaw   z mom. : Cng*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].Cng = val[idesign];

      if (verbose)
      {
        printf( "yaw   z mom. : Cng%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "Cng%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'Trefftz drag : CDffg*'
    isOK = getLine_realn( fp, "Trefftz drag : CDffg*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].CDffg = val[idesign];

      if (verbose)
      {
        printf( "Trefftz drag : CDffg%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "CDffg%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'span eff.    : eg*'
    isOK = getLine_realn( fp, "span eff.    : eg*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].eg = val[idesign];

      if (verbose)
      {
        printf( "span eff.    : eg%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "eg%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;
  }

  // 'Neutral point  Xnp'
  isOK = getLine_real1( fp, "Neutral point  Xnp", &line, &mat->Xnp );
  if (isOK)
  {
    if (verbose)
      printf( "Neutral point  Xnp = %lf\n", mat->Xnp );
  }
  else
    goto error;

  // 'Clb Cnr / Clr Cnb'
  isOK = getLine_real1( fp, "Clb Cnr / Clr Cnb", &line, &mat->spiral );
  if (isOK)
  {
    if (verbose)
      printf( "Clb Cnr / Clr Cnb = %lf\n", mat->spiral );
  }
  else
    goto error;

  avl_free( val );
  avl_free( line.line );
  avl_free( string );
  fclose( fp );
  if (verbose) printf( "DERMATM file read OK\n" );
  return 0;

error:
  avl_free( val );
  avl_free( line.line );
  avl_free( string );
  fclose( fp );
  return -1;
}


//----------------------------------------------------------------------------//
//
void avlInit_DERMATM( avlDermatM *mat )
{
  if (mat == NULL) return;

  avlInit_TOT( &mat->tot );

  mat->nCont = 0;
  mat->cont = NULL;

  mat->nDesign = 0;
  mat->design = NULL;

  mat->CLa = mat->CLb = 0;
  mat->CYa = mat->CYb = 0;
  mat->Cla = mat->Clb = 0;
  mat->Cma = mat->Cmb = 0;
  mat->Cna = mat->Cnb = 0;

  mat->CLp = mat->CLq = mat->CLr = 0;
  mat->CYp = mat->CYq = mat->CYr = 0;
  mat->Clp = mat->Clq = mat->Clr = 0;
  mat->Cmp = mat->Cmq = mat->Cmr = 0;
  mat->Cnp = mat->Cnq = mat->Cnr = 0;

  mat->Xnp = 0;
  mat->spiral = 0;

}


//----------------------------------------------------------------------------//
//
void avlFree_DERMATM( avlDermatM *mat )
{
  int i;

  if (mat == NULL) return;

  avlFree_TOT( &mat->tot );

  for (i = 0; i < mat->nCont; i++)
    avl_free(mat->cont[i].wrt);
  avl_free(mat->cont);

  for (i = 0; i < mat->nDesign; i++)
    avl_free(mat->design[i].wrt);
  avl_free(mat->design);

  avlInit_DERMATM( mat );

}
