//**********************************************************************
//   Module:  avlRead_SURF.c
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

#include "avlRead_SURF.h"
#define AVL_MRF_INTERNAL
#include "read_util.h"

//----------------------------------------------------------------------------//
// SURF: surface forces
//
int avlRead_SURF( const char *filename, avlSurf *surf, bool verbose )
{
  int isurf, n;
  double val[9];
  char *string = NULL, *version = NULL;
  size_t stringsize = 128;
  bool isOK;
  FILE *fp = NULL;
  avlLineBuffer line;
  line.line = NULL;
  line.linesize = 0;

  if (surf == NULL) return -1;

  avlFree_SURF( surf );

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

  // SURF
  isOK = getLine_string1( fp, "SURF", &line, string );
  if (isOK)
  {
    if (strcmp(string, "SURF") != 0)
    {
      printf( "ERROR in %s: expected 'SURF' file ID but got '%s'\n", __func__, string );
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

  // 'axis orientation'
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // 'Sref, Cref, Bref'
  isOK = getLine_real3( fp, "Sref, Cref, Bref", &line, &surf->Sref, &surf->Cref, &surf->Bref );
  if (isOK)
  {
    if (verbose) printf( "Sref = %lf  Cref = %lf  Bref = %lf\n", surf->Sref, surf->Cref, surf->Bref );
  }
  else
    goto error;

  // 'Xref, Yref, Zref'
  isOK = getLine_real3( fp, "Xref, Yref, Zref", &line, &surf->Xref, &surf->Yref, &surf->Zref );
  if (isOK)
  {
    if (verbose) printf( "Xref = %lf  Yref = %lf  Zref = %lf\n", surf->Xref, surf->Yref, surf->Zref );
  }
  else
    goto error;

  // # of surfaces
  isOK = getLine_int1( fp, "# of surfaces", &line, &surf->nSurf );
  if (isOK)
  {
    if (verbose) printf( "# surfaces = %d\n", surf->nSurf );
  }
  else
    goto error;

  // allocated surfaces
  surf->surf = (avlSurfProp *) malloc( surf->nSurf * sizeof(avlSurfProp) );
  if (surf->surf == NULL)
  {
    surf->nSurf = 0;
    printf( "ERROR: malloc error on 'avlHingeControl'\n" );
    goto error;
  }
  for (isurf = 0; isurf < surf->nSurf; isurf++)
    surf->surf[isurf].name = NULL;

  for (isurf = 0; isurf < surf->nSurf; isurf++)
  {
    // SURFACE
    isOK = getLine_string1( fp, "SURFACE", &line, string );
    if (isOK)
    {
      if (strcmp(string, "SURFACE") != 0)
      {
        printf( "ERROR: expected SURFACE keyword but got '%s'\n", string );
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
      surf->surf[isurf].name = strdup(chop_newline(line.line));
      if (surf->surf[isurf].name == NULL) goto error;
      if (verbose) printf( "%s\n", surf->surf[isurf].name );
    }
    else
      goto error;

    // 'n Area CL CD Cm CY Cn Cl CDi CDv'
    isOK = getLine_int1_realn( fp, "n Area CL CD Cm CY Cn Cl CDi CDv", &line, &n, val, 9 );
    if (isOK)
    {
      surf->surf[isurf].Area = val[0];
      surf->surf[isurf].CL   = val[1];
      surf->surf[isurf].CD   = val[2];
      surf->surf[isurf].Cm   = val[3];
      surf->surf[isurf].CY   = val[4];
      surf->surf[isurf].Cn   = val[5];
      surf->surf[isurf].Cl   = val[6];
      surf->surf[isurf].CDi  = val[7];
      surf->surf[isurf].CDv  = val[8];
      if (verbose)
      {
        printf( "%d  Area = %lf  CL = %lf  CD = %lf  Cm = %lf  ", n,
                                                                  surf->surf[isurf].Area,
                                                                  surf->surf[isurf].CL,
                                                                  surf->surf[isurf].CD,
                                                                  surf->surf[isurf].Cm);
        printf( "CY = %lf  Cn = %lf  Cl = %lf  CDi = %lf  CDv = %lf\n", surf->surf[isurf].CY,
                                                                        surf->surf[isurf].Cn,
                                                                        surf->surf[isurf].Cl,
                                                                        surf->surf[isurf].CDi,
                                                                        surf->surf[isurf].CDv  );
      }
    }
    else
      goto error;

    // 'n Ssurf Cave cl cd cdv '
    isOK = getLine_int1_realn( fp, "n Ssurf Cave cl cd cdv", &line, &n, val, 5 );
    if (isOK)
    {
      surf->surf[isurf].Ssurf = val[0];
      surf->surf[isurf].Cave  = val[1];
      surf->surf[isurf].cl    = val[2];
      surf->surf[isurf].cd    = val[3];
      surf->surf[isurf].cdv   = val[4];
      if (verbose)
        printf( "%d  Ssurf = %lf  Cave = %lf  cl = %lf  cd = %lf  cdv = %lf\n", n,
                                                                                surf->surf[isurf].Ssurf,
                                                                                surf->surf[isurf].Cave,
                                                                                surf->surf[isurf].cl,
                                                                                surf->surf[isurf].cd,
                                                                                surf->surf[isurf].cdv );
    }
    else
      goto error;
  }

  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  if (verbose) printf( "SURF file read OK\n" );
  return 0;

error:
  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  avlFree_SURF(surf);
  return -1;
}


//----------------------------------------------------------------------------//
//
void avlInit_SURF( avlSurf *surf )
{
  if (surf == NULL) return;
  surf->surf = NULL;
  surf->nSurf = 0;
  surf->Sref = surf->Bref = surf->Cref = 0;
  surf->Xref = surf->Yref = surf->Zref = 0;
}


//----------------------------------------------------------------------------//
//
void avlFree_SURF( avlSurf *surf )
{
  int i;

  if (surf == NULL) return;

  for (i = 0; i < surf->nSurf; i++)
    avl_free(surf->surf[i].name);
  avl_free(surf->surf);

  avlInit_SURF( surf );
}
