//**********************************************************************
//   Module:  avlRead_DERMATB.c
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

#include "avlRead_DERMATB.h"
#define AVL_MRF_INTERNAL
#include "read_util.h"

int avlRead_TOT2( FILE *fp, avlTot *tot, bool verbose, char *fileID );

//----------------------------------------------------------------------------//
// DERMATB: stability derivative matrix in body axes
//
int avlRead_DERMATB( const char *filename, avlDermatB *mat, bool verbose )
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

  avlFree_DERMATB( mat );

  fp = fopen( filename, "r" );
  if (fp == NULL)
  {
    printf( "ERROR: unable to open %s\n", filename );
    return -1;
  }

  status = avlRead_TOT2( fp, &mat->tot, verbose, "DERMATB" );
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

  // 'Geometry-axis derivatives...'
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // 'axial   vel. u, sideslip vel. v, normal  vel. w'
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // 'x force CX : CXu, CXv, CXw'
  isOK = getLine_real3( fp, "x force CX : CXu, CXv, CXw", &line, &mat->CXu, &mat->CXv, &mat->CXw );
  if (isOK)
  {
    if (verbose) printf( "x force CX : CXu = %lf  CXv = %lf  CXw = %lf\n", mat->CXu, mat->CXv, mat->CXw );
  }
  else
    goto error;

  // 'y force CY : CYu, CYv, CYw'
  isOK = getLine_real3( fp, "y force CY : CYu, CYv, CYw", &line, &mat->CYu, &mat->CYv, &mat->CYw );
  if (isOK)
  {
    if (verbose) printf( "y force CY : CYu = %lf  CYv = %lf  CYw = %lf\n", mat->CYu, mat->CYv, mat->CYw );
  }
  else
    goto error;

  // 'z force CZ : CZu, CZv, CZw'
  isOK = getLine_real3( fp, "z force CZ : CZu, CZv, CZw", &line, &mat->CZu, &mat->CZv, &mat->CZw );
  if (isOK)
  {
    if (verbose) printf( "z force CZ : CZu = %lf  CZv = %lf  CZw = %lf\n", mat->CZu, mat->CZv, mat->CZw );
  }
  else
    goto error;

  // 'x mom.  Cl : Clu, Clv, Clw'
  isOK = getLine_real3( fp, "x mom.  Cl : Clu, Clv, Clw", &line, &mat->Clu, &mat->Clv, &mat->Clw );
  if (isOK)
  {
    if (verbose) printf( "x mom.  Cl : Clu = %lf  Clv = %lf  Clw = %lf\n", mat->Clu, mat->Clv, mat->Clw );
  }
  else
    goto error;

  // 'y mom.  Cm : Cmu, Cmv, Cmw'
  isOK = getLine_real3( fp, "y mom.  Cm : Cmu, Cmv, Cmw", &line, &mat->Cmu, &mat->Cmv, &mat->Cmw );
  if (isOK)
  {
    if (verbose) printf( "y mom.  Cm : Cmu = %lf  Cmv = %lf  Cmw = %lf\n", mat->Cmu, mat->Cmv, mat->Cmw );
  }
  else
    goto error;

  // 'z mom.  Cn : Cnu, Cnv, Cnw'
  isOK = getLine_real3( fp, "z mom.  Cn : Cnu, Cnv, Cnw", &line, &mat->Cnu, &mat->Cnv, &mat->Cnw );
  if (isOK)
  {
    if (verbose) printf( "z mom.  Cn : Cnu = %lf  Cnv = %lf  Cnw = %lf\n", mat->Cnu, mat->Cnv, mat->Cnw );
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

  // 'x force CX : CXp, CXq ,CXr'
  isOK = getLine_real3( fp, "x force CX : CXp, CXq, CXr", &line, &mat->CXp, &mat->CXq, &mat->CXr );
  if (isOK)
  {
    if (verbose) printf( "x force CX : CXp = %lf  CXq = %lf  CXr = %lf\n", mat->CXp, mat->CXq, mat->CXr );
  }
  else
    goto error;

  // 'y force CY : CYp, CYq, CYr'
  isOK = getLine_real3( fp, "y force CY : CYp, CYq, CYr", &line, &mat->CYp, &mat->CYq, &mat->CYr );
  if (isOK)
  {
    if (verbose) printf( "y force CY : CYp = %lf  CYq = %lf  CYr = %lf\n", mat->CYp, mat->CYq, mat->CYr );
  }
  else
    goto error;

  // 'z force CZ : CZp, CZq, CZr'
  isOK = getLine_real3( fp, "z force CZ : CZp, CZq, CZr", &line, &mat->CZp, &mat->CZq, &mat->CZr );
  if (isOK)
  {
    if (verbose) printf( "z force CZ : CZp = %lf  CZq = %lf  CZr = %lf\n", mat->CZp, mat->CZq, mat->CZr );
  }
  else
    goto error;

  // 'x mom.  Cl : Clp, Clq, Clr'
  isOK = getLine_real3( fp, "x mom.  Cl : Clp, Clq, Clr", &line, &mat->Clp, &mat->Clq, &mat->Clr );
  if (isOK)
  {
    if (verbose) printf( "x mom.  Cl : Clp = %lf  Clq = %lf  Clr = %lf\n", mat->Clp, mat->Clq, mat->Clr );
  }
  else
    goto error;

  // 'y mom.  Cm : Cmp, Cmq, Cmr'
  isOK = getLine_real3( fp, "y mom.  Cm : Cmp, Cmq, Cmr", &line, &mat->Cmp, &mat->Cmq, &mat->Cmr );
  if (isOK)
  {
    if (verbose) printf( "y mom.  Cm : Cmp = %lf  Cmq = %lf  Cmr = %lf\n", mat->Cmp, mat->Cmq, mat->Cmr );
  }
  else
    goto error;

  // 'z mom.  Cn : Cnp, Cnq, Cnr'
  isOK = getLine_real3( fp, "z mom.  Cn : Cnp, Cnq, Cnr", &line, &mat->Cnp, &mat->Cnq, &mat->Cnr );
  if (isOK)
  {
    if (verbose) printf( "z mom.  Cn : Cnp = %lf  Cnq = %lf  Cnr = %lf\n", mat->Cnp, mat->Cnq, mat->Cnr );
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
    // allocate controls
    mat->cont = (avlDermatBControl*)malloc( mat->nCont * sizeof(avlDermatBControl));
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
        if (verbose) printf( "control name = %d %s\n", icont, mat->cont[icont].wrt );
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

    // 'x force CX : CXd*'
    isOK = getLine_realn( fp, "x force CX : CXd*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].CXd = val[icont];

      if (verbose)
      {
        printf( "x force CX : CXd%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "CXd%02d = %lf  ", icont+1, val[icont] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'y force CY : CYd*'
    isOK = getLine_realn( fp, "y force CY : CYd*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].CYd = val[icont];

      if (verbose)
      {
        printf( "y force CY : CYd%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "CYd%02d = %lf  ", icont+1, val[icont] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'z force CZ : CZd*'
    isOK = getLine_realn( fp, "z force CZ : CZd*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].CZd = val[icont];

      if (verbose)
      {
        printf( "z force CZ : CZd%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "CZd%02d = %lf  ", icont+1, val[icont] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'x mom.  Cl : Cld*'
    isOK = getLine_realn( fp, "x mom.  Cl : Cld*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].Cld = val[icont];

      if (verbose)
      {
        printf( "x mom.  Cl : Cld%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "Cld%02d = %lf  ", icont+1, val[icont] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'y mom.  Cm : Cmd*'
    isOK = getLine_realn( fp, "y mom.  Cm : Cmd*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].Cmd = val[icont];

      if (verbose)
      {
        printf( "y mom.  Cm : Cmd%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "Cmd%02d = %lf  ", icont+1, val[icont] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'z mom.  Cn : Cnd*'
    isOK = getLine_realn( fp, "z mom.  Cn : Cnd*", &line, val, mat->nCont );
    if (isOK)
    {
      for (icont = 0; icont < mat->nCont; icont++)
        mat->cont[icont].Cnd = val[icont];

      if (verbose)
      {
        printf( "z mom.  Cn : Cnd%02d = %lf  ", 1, val[0] );
        for (icont = 1; icont < mat->nCont; icont++)
          printf( "Cnd%02d = %lf  ", icont+1, val[icont] );
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
    mat->design = (avlDermatBDesign*)malloc( mat->nDesign * sizeof(avlDermatBDesign));
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

    // 'x force CX : CXg*'
    isOK = getLine_realn( fp, "x force CX : CXg*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].CXg = val[idesign];

      if (verbose)
      {
        printf( "x force CX : CXg%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "CXg%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'y force CY : CYg*'
    isOK = getLine_realn( fp, "y force CY : CYg*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].CYg = val[idesign];

      if (verbose)
      {
        printf( "y force CY : CYg%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "CYg%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'z force CZ : CZg*'
    isOK = getLine_realn( fp, "z force CZ : CZg*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].CZg = val[idesign];

      if (verbose)
      {
        printf( "z force CZ : CZg%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "CZg%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'x mom.  Cl : Clg*'
    isOK = getLine_realn( fp, "x mom.  Cl : Clg*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].Clg = val[idesign];

      if (verbose)
      {
        printf( "x mom.  Cl : Clg%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "Clg%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'y mom.  Cm : Cmg*'
    isOK = getLine_realn( fp, "y mom.  Cm : Cmg*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].Cmg = val[idesign];

      if (verbose)
      {
        printf( "y mom.  Cm : Cmg%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "Cmg%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // 'z mom.  Cn : Cng*'
    isOK = getLine_realn( fp, "z mom.  Cn : Cng*", &line, val, mat->nDesign );
    if (isOK)
    {
      for (idesign = 0; idesign < mat->nDesign; idesign++)
        mat->design[idesign].Cng = val[idesign];

      if (verbose)
      {
        printf( "z mom.  Cn : Cng%02d = %lf  ", 1, val[0] );
        for (idesign = 1; idesign < mat->nDesign; idesign++)
          printf( "Cng%02d = %lf  ", idesign+1, val[idesign] );
        printf( "\n" );
      }
    }
    else
      goto error;
  }

  avl_free( val );
  avl_free( line.line );
  avl_free( string );
  fclose( fp );
  if (verbose) printf( "DERMATB file read OK\n" );
  return 0;

error:
  avl_free( val );
  avl_free( line.line );
  avl_free( string );
  fclose( fp );
  avlFree_DERMATB( mat );
  return -1;
}


//----------------------------------------------------------------------------//
//
void avlInit_DERMATB( avlDermatB *mat )
{
  if (mat == NULL) return;

  avlInit_TOT( &mat->tot );

  mat->nCont = 0;
  mat->cont = NULL;

  mat->nDesign = 0;
  mat->design = NULL;

  mat->CXu = mat->CXv = mat->CXw = 0;
  mat->CYu = mat->CYv = mat->CYw = 0;
  mat->CZu = mat->CZv = mat->CZw = 0;
  mat->Clu = mat->Clv = mat->Clw = 0;
  mat->Cmu = mat->Cmv = mat->Cmw = 0;
  mat->Cnu = mat->Cnv = mat->Cnw = 0;

  mat->CXp = mat->CXq = mat->CXr = 0;
  mat->CYp = mat->CYq = mat->CYr = 0;
  mat->CZp = mat->CZq = mat->CZr = 0;
  mat->Clp = mat->Clq = mat->Clr = 0;
  mat->Cmp = mat->Cmq = mat->Cmr = 0;
  mat->Cnp = mat->Cnq = mat->Cnr = 0;

}


//----------------------------------------------------------------------------//
//
void avlFree_DERMATB( avlDermatB *mat )
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

  avlInit_DERMATB( mat );
}

