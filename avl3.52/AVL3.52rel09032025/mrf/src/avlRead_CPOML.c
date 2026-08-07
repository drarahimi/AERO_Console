//**********************************************************************
//   Module:  avlRead_CPOML.c
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

#include "avlRead_CPOML.h"
#define AVL_MRF_INTERNAL
#include "read_util.h"

//----------------------------------------------------------------------------//
// CPOML: upper and lower grid and pressure coefficients on surface OML
//
int avlRead_CPOML( const char *filename, avlCpOml *cpoml, bool verbose )
{
  int i, j, k, isurf, isec;
  char *string = NULL;
  char *version = NULL;
  size_t stringsize = 128;
  bool isOK;
  avlCpOmlpSurf *surf=NULL;
  FILE *fp = NULL;
  avlLineBuffer line;
  line.line = NULL;
  line.linesize = 0;

  if (cpoml == NULL) return -1;

  avlFree_CPOML( cpoml );

  fp = fopen( filename, "r" );
  if (fp == NULL)
  {
    printf( "ERROR: unable to open %s\n", filename );
    return -1;
  }

  string = (char *) malloc( stringsize * sizeof(char) );
  if (string == NULL)
  {
    printf( "ERROR: malloc error on 'string'\n" );
    goto error;
  }

  version = (char *) malloc( stringsize * sizeof(char) );
  if (version == NULL)
  {
    printf( "ERROR: malloc error on 'version'\n" );
    goto error;
  }

  // CPOML
  isOK = getLine_string1( fp, "CPOML", &line, string );
  if (isOK)
  {
    if (strcmp(string, "CPOML") != 0)
    {
      printf( "ERROR in %s: expected 'CPOML' file ID but got '%s'\n", __func__, string );
      goto error;
    }
    if (verbose) printf( "%s\n", string );
  }
  else
    goto error;

  // VERSION
  isOK = getLine_string2( fp, "VERSION", &line, string, version );
  if (isOK)
  {
    if (strcmp(string, "VERSION") != 0)
    {
      printf( "ERROR: expected VERSION keyword but got '%s'\n", string );
      goto error;
    }
    if (strcmp(version, "1.0") != 0)
    {
      printf( "ERROR: unexpected VERSION number '%s'\n", version );
      goto error;
    }
    if (verbose) printf( "VERSION = %s\n", version );
  }
  else
    goto error;

  // # of surfaces
  isOK = getLine_int1( fp, "# of surfaces", &line, &cpoml->nsurf );
  if (isOK)
  {
    if (verbose) printf( "# surfaces = %d\n", cpoml->nsurf );
  }
  else
    goto error;

  // allocated surfaces
  cpoml->surf = (avlCpOmlpSurf *) malloc( cpoml->nsurf * sizeof(avlCpOmlpSurf) );
  if (cpoml->surf == NULL)
  {
    cpoml->nsurf = 0;
    printf( "ERROR: malloc error on 'avlStrpSurf'\n" );
    goto error;
  }
  for (isurf = 0; isurf < cpoml->nsurf; isurf++)
  {
    cpoml->surf[isurf].name = NULL;
    cpoml->surf[isurf].component = 0;
    cpoml->surf[isurf].nChord = 0;
    cpoml->surf[isurf].nSpan = 0;
    cpoml->surf[isurf].imags = 0;
    cpoml->surf[isurf].nSec = 0;
    cpoml->surf[isurf].icnt = NULL;

    cpoml->surf[isurf].vert.xlo = NULL;
    cpoml->surf[isurf].vert.xup = NULL;
    cpoml->surf[isurf].vert.ylo = NULL;
    cpoml->surf[isurf].vert.yup = NULL;
    cpoml->surf[isurf].vert.zlo = NULL;
    cpoml->surf[isurf].vert.zup = NULL;

    cpoml->surf[isurf].elem.xlo = NULL;
    cpoml->surf[isurf].elem.xup = NULL;
    cpoml->surf[isurf].elem.ylo = NULL;
    cpoml->surf[isurf].elem.yup = NULL;
    cpoml->surf[isurf].elem.zlo = NULL;
    cpoml->surf[isurf].elem.zup = NULL;

    cpoml->surf[isurf].cp_lo = NULL;
    cpoml->surf[isurf].cp_up = NULL;
  }

  for (isurf = 0; isurf < cpoml->nsurf; isurf++)
  {
    surf = &cpoml->surf[isurf];

    // SURFACE
    isOK = getLine_string1( fp, "SURFACE", &line, string );
    if (isOK)
    {
      if (strcmp(string, "SURFACE") != 0)
      {
        printf( "ERROR: expected 'SURFACE' keyword but got '%s'\n", string );
        goto error;
      }
      if (verbose) printf( "%s\n", string );
    }
    else
      goto error;

    // surface name
    isOK = getLine_line( fp, &line );
    if (isOK)
    {
      surf->name = strdup(chop_newline(line.line));
      if (surf->name == NULL) goto error;
      if (verbose) printf( "%s\n", surf->name );
    }
    else
      goto error;

    // component
    isOK = getLine_int1( fp, "component", &line, &surf->component );
    if (isOK)
    {
      if (verbose) printf( "component = %d\n", surf->component );
    }
    else
      goto error;

    // elements: nspan x nchord
    isOK = getLine_int2( fp, "elements: nspan x nchord", &line, &surf->nSpan, &surf->nChord );
    if (isOK)
    {
      if (verbose) printf( "elements: nspan = %d   nchord = %d\n", surf->nSpan, surf->nChord );
    }
    else
      goto error;

    // Y-duplicate flag
    isOK = getLine_int1( fp, "Y-duplicate flag", &line, &surf->imags );
    if (isOK)
    {
      if (verbose) printf( "Y-duplicate flag = %d\n", surf->imags );
    }
    else
      goto error;

    // section indices
    isOK = getLine_int1( fp, "# section indices", &line, &surf->nSec );
    if (isOK)
    {
      if (verbose) printf( "# section indices = %d\n", surf->nSec );
    }
    else
      goto error;

    surf->icnt = (int*)malloc(surf->nSec*sizeof(int));
    if (surf->icnt == NULL) { printf( "ERROR: malloc error on 'icnt'\n" ); goto error; }

    isOK = getLine_intn( fp, "section indices", &line, surf->icnt, surf->nSec );
    if (isOK)
    {
      if (verbose)
      {
        printf( "section indices = " );
        for (isec = 0; isec < surf->nSec; isec++)
          printf( "%d ", surf->icnt[isec] );
        printf( "\n" );
      }
    }
    else
      goto error;

    // VERTEX_GRID (x_lo, x_up, y_lo, y_up, z_lo, z_up)
    isOK = getLine_string1( fp, "VERTEX_GRID (x_lo, x_up, y_lo, y_up, z_lo, z_up)", &line, string );
    if (isOK)
    {
      if (strcmp(string, "VERTEX_GRID") != 0)
      {
        printf( "ERROR: expected 'VERTEX_GRID' keyword but got '%s'\n", string );
        goto error;
      }
      if (verbose) printf( "%s\n", string );
    }
    else
      goto error;

    k = (surf->nSpan+1)*(surf->nChord+1);
    surf->vert.xlo = (double*)malloc(k*sizeof(double));
    if (surf->vert.xlo == NULL) { printf( "ERROR: malloc error on 'vert.xlo'\n" ); goto error; }
    surf->vert.xup = (double*)malloc(k*sizeof(double));
    if (surf->vert.xup == NULL) { printf( "ERROR: malloc error on 'vert.xup'\n" ); goto error; }
    surf->vert.ylo = (double*)malloc(k*sizeof(double));
    if (surf->vert.ylo == NULL) { printf( "ERROR: malloc error on 'vert.ylo'\n" ); goto error; }
    surf->vert.yup = (double*)malloc(k*sizeof(double));
    if (surf->vert.yup == NULL) { printf( "ERROR: malloc error on 'vert.yup'\n" ); goto error; }
    surf->vert.zlo = (double*)malloc(k*sizeof(double));
    if (surf->vert.zlo == NULL) { printf( "ERROR: malloc error on 'vert.zlo'\n" ); goto error; }
    surf->vert.zup = (double*)malloc(k*sizeof(double));
    if (surf->vert.zup == NULL) { printf( "ERROR: malloc error on 'vert.zup'\n" ); goto error; }

    double val[8];
    for (j = 0; j <= surf->nSpan; j++)
    {
      for (i = 0; i <= surf->nChord; i++)
      {
        isOK = getLine_realn( fp, "VERTEX_GRID", &line, val, 6 );
        if (isOK)
        {
          k = surf->nChord*j + i;
          surf->vert.xlo[k] = val[0];
          surf->vert.xup[k] = val[1];
          surf->vert.ylo[k] = val[2];
          surf->vert.yup[k] = val[3];
          surf->vert.zlo[k] = val[4];
          surf->vert.zup[k] = val[5];
          if (verbose)
          {
            printf( "%d %d %lf %lf %lf %lf %lf %lf\n", i, j,
                                                       surf->vert.xlo[k],
                                                       surf->vert.xup[k],
                                                       surf->vert.ylo[k],
                                                       surf->vert.yup[k],
                                                       surf->vert.zlo[k],
                                                       surf->vert.zup[k] );
          }
        }
        else
        {
          printf( "ERROR: read error on vertex grid; (i,j) = (%d,%d)\n", i, j );
          goto error;
        }
      }
    }
    if (verbose) printf( "vertex grid read OK\n" );

    // ELEMENT_CP (x_lo, x_up, y_lo, y_up, z_lo, z_up, cp_lo, cp_up)
    isOK = getLine_string1( fp, "ELEMENT_CP (x_lo, x_up, y_lo, y_up, z_lo, z_up, cp_lo, cp_up)", &line, string );
    if (isOK)
    {
      if (strcmp(string, "ELEMENT_CP") != 0)
      {
        printf( "ERROR: expected 'ELEMENT_CP' keyword but got '%s'\n", string );
        goto error;
      }
      if (verbose) printf( "%s\n", string );
    }
    else
      goto error;

    k = surf->nSpan*surf->nChord;
    surf->elem.xlo = (double*)malloc(k*sizeof(double));
    if (surf->elem.xlo == NULL) { printf( "ERROR: malloc error on 'elem.xlo'\n" ); goto error; }
    surf->elem.xup = (double*)malloc(k*sizeof(double));
    if (surf->elem.xup == NULL) { printf( "ERROR: malloc error on 'elem.xup'\n" ); goto error; }
    surf->elem.ylo = (double*)malloc(k*sizeof(double));
    if (surf->elem.ylo == NULL) { printf( "ERROR: malloc error on 'elem.ylo'\n" ); goto error; }
    surf->elem.yup = (double*)malloc(k*sizeof(double));
    if (surf->elem.yup == NULL) { printf( "ERROR: malloc error on 'elem.yup'\n" ); goto error; }
    surf->elem.zlo = (double*)malloc(k*sizeof(double));
    if (surf->elem.zlo == NULL) { printf( "ERROR: malloc error on 'elem.zlo'\n" ); goto error; }
    surf->elem.zup = (double*)malloc(k*sizeof(double));
    if (surf->elem.zup == NULL) { printf( "ERROR: malloc error on 'elem.zup'\n" ); goto error; }

    surf->cp_lo = (double*)malloc(k*sizeof(double));
    if (surf->cp_lo == NULL) { printf( "ERROR: malloc error on 'cp_lo'\n" ); goto error; }
    surf->cp_up = (double*)malloc(k*sizeof(double));
    if (surf->cp_up == NULL) { printf( "ERROR: malloc error on 'cp_up'\n" ); goto error; }

    for (j = 0; j < surf->nSpan; j++)
    {
      for (i = 0; i < surf->nChord; i++)
      {
        isOK = getLine_realn( fp, "ELEMENT_CP", &line, val, 8 );
        if (isOK)
        {
          k = surf->nChord*j + i;
          surf->elem.xlo[k] = val[0];
          surf->elem.xup[k] = val[1];
          surf->elem.ylo[k] = val[2];
          surf->elem.yup[k] = val[3];
          surf->elem.zlo[k] = val[4];
          surf->elem.zup[k] = val[5];

          surf->cp_lo[k] = val[6];
          surf->cp_up[k] = val[7];

          if (verbose)
          {
            printf( "%d %d %lf %lf %lf %lf %lf %lf %lf %lf\n", i, j,
                                                               surf->elem.xlo[k],
                                                               surf->elem.xup[k],
                                                               surf->elem.ylo[k],
                                                               surf->elem.yup[k],
                                                               surf->elem.zlo[k],
                                                               surf->elem.zup[k],
                                                               surf->cp_lo[k],
                                                               surf->cp_up[k] );
          }
        }
        else
        {
          printf( "ERROR: read error on element Cp's; (i,j) = (%d,%d)\n", i, j );
          goto error;
        }
      }
    }
    if (verbose) printf( "element Cp's read OK\n" );

  }  // isurf

  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  if (verbose) printf( "CPOML file read OK\n" );
  return 0;

error:
  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  avlFree_CPOML(cpoml);
  return -1;
}


//----------------------------------------------------------------------------//
//
void avlInit_CPOML( avlCpOml *cpoml )
{
  if (cpoml == NULL) return;
  cpoml->surf = NULL;
  cpoml->nsurf = 0;
}


//----------------------------------------------------------------------------//
//
void avlFree_CPOML( avlCpOml *cpoml )
{
  int isurf;

  if (cpoml == NULL) return;

  for (isurf = 0; isurf < cpoml->nsurf; isurf++)
  {
    avl_free(cpoml->surf[isurf].name);
    avl_free(cpoml->surf[isurf].icnt);

    avl_free(cpoml->surf[isurf].vert.xlo);
    avl_free(cpoml->surf[isurf].vert.xup);
    avl_free(cpoml->surf[isurf].vert.ylo);
    avl_free(cpoml->surf[isurf].vert.yup);
    avl_free(cpoml->surf[isurf].vert.zlo);
    avl_free(cpoml->surf[isurf].vert.zup);

    avl_free(cpoml->surf[isurf].elem.xlo);
    avl_free(cpoml->surf[isurf].elem.xup);
    avl_free(cpoml->surf[isurf].elem.ylo);
    avl_free(cpoml->surf[isurf].elem.yup);
    avl_free(cpoml->surf[isurf].elem.zlo);
    avl_free(cpoml->surf[isurf].elem.zup);

    avl_free(cpoml->surf[isurf].cp_lo);
    avl_free(cpoml->surf[isurf].cp_up);
  }
  avl_free(cpoml->surf);

  avlInit_CPOML( cpoml );
}
