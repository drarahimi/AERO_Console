//**********************************************************************
//   Module:  avlRead_STRP.c
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

#include "avlRead_STRP.h"
#define AVL_MRF_INTERNAL
#include "read_util.h"

#ifdef _MSC_VER
#define strtok_r  strtok_s
#endif

//----------------------------------------------------------------------------//
// STRP: strip and surfaces forces
//
int avlRead_STRP( const char *filename, avlStrp *strp, bool verbose )
{
  int i, n, isurf, istrp;
  char *string = NULL, *version = NULL;
  char *token, *rest;
  double val[14];
  bool isOK;
  size_t stringsize = 128;
  avlStrpSurf *surf = NULL;
  FILE *fp = NULL;
  avlLineBuffer line;
  line.line = NULL;
  line.linesize = 0;

  if (strp == NULL) return -1;

  avlFree_STRP( strp );

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

  // STRP
  isOK = getLine_string1( fp, "STRP", &line, string );
  if (isOK)
  {
    if (strcmp(string, "STRP") != 0)
    {
      printf( "ERROR in %s: expected 'STRP' file ID but got '%s'\n", __func__, string );
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
  isOK = getLine_real3( fp, "Sref, Cref, Bref", &line, &strp->Sref, &strp->Cref, &strp->Bref );
  if (isOK)
  {
    if (verbose) printf( "Sref = %lf  Cref = %lf  Bref = %lf\n", strp->Sref, strp->Cref, strp->Bref );
  }
  else
    goto error;

  // 'Xref, Yref, Zref'
  isOK = getLine_real3( fp, "Xref, Yref, Zref", &line, &strp->Xref, &strp->Yref, &strp->Zref );
  if (isOK)
  {
    if (verbose) printf( "Xref = %lf  Yref = %lf  Zref = %lf\n", strp->Xref, strp->Yref, strp->Zref );
  }
  else
    goto error;

  // 'Surface and Strip Forces by surface (referred to Sref,Cref,Bref about Xref,Yref,Zref)'
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // # of surfaces
  strp->nSurf = 0;
  isOK = getLine_int1( fp, "# of surfaces", &line, &strp->nSurf );
  if (isOK)
  {
    if (verbose) printf( "# surfaces = %d\n", strp->nSurf );
  }
  else
    goto error;

  // allocated surfaces
  strp->surf = (avlStrpSurf *) malloc( strp->nSurf * sizeof(avlStrpSurf) );
  if (strp->surf == NULL)
  {
    strp->nSurf = 0;
    printf( "ERROR: malloc error on 'avlBodyProp'\n" );
    goto error;
  }
  for (isurf = 0; isurf < strp->nSurf; isurf++)
  {
    strp->surf[isurf].name = NULL;
    strp->surf[isurf].j = NULL;
    for (i = 0; i < AVL_NSTRP_DATA; i++)
    {
      strp->surf[isurf].data[i].name = NULL;
      strp->surf[isurf].data[i].val = NULL;
    }
  }

  for (isurf = 0; isurf < strp->nSurf; isurf++)
  {
    surf = &strp->surf[isurf];

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
      surf->name = strdup(chop_newline(line.line));
      if (surf->name == NULL) goto error;
      if (verbose) printf( "%s\n", surf->name );
    }
    else
      goto error;

    // 'Surface #, # Chordwise, # Spanwise, First strip'
    isOK = getLine_int4( fp, "Surface #, # Chordwise, # Spanwise, First strip", &line,
                                                                                &surf->iSurf,
                                                                                &surf->nChord,
                                                                                &surf->nSpan,
                                                                                &surf->iStrp );
    if (isOK)
    {
      if (verbose) printf( "surf # = %d  # Chordwise = %d  # Spanwise = %d  1st strip = %d\n", surf->iSurf,
                                                                                               surf->nChord,
                                                                                               surf->nSpan,
                                                                                               surf->iStrp );
    }
    else
      goto error;

    // 'Surface area, Ave. chord'
    isOK = getLine_real2( fp, "Surface area, Ave. chord", &line, &surf->Ssurf, &surf->Cave );
    if (isOK)
    {
      if (verbose) printf( "Surface area = %lf  Ave. chord = %lf\n", surf->Ssurf, surf->Cave );
    }
    else
      goto error;

    // 'CLsurf, Clsurf, CYsurf, Cmsurf, CDsurf, Cnsurf, CDisurf, CDvsurf'
    isOK = getLine_realn( fp, "CLsurf, Clsurf, CYsurf, Cmsurf, CDsurf, Cnsurf, CDisurf, CDvsurf", &line, val, 8 );
    if (isOK)
    {
      surf->CLsurf  = val[0];
      surf->Clsurf  = val[1];
      surf->CYsurf  = val[2];
      surf->Cmsurf  = val[3];
      surf->CDsurf  = val[4];
      surf->Cnsurf  = val[5];
      surf->CDisurf = val[6];
      surf->CDvsurf = val[7];
      if (verbose)
      {
        printf( "CLsurf = %lf  Clsurf = %lf  CYsurf = %lf  Cmsurf = %lf  ", surf->CLsurf, surf->Clsurf, surf->CYsurf, surf->Cmsurf );
        printf( "CDsurf = %lf  Cnsurf = %lf  CDisurf = %lf  CDvsurf = %lf\n", surf->CDsurf, surf->Cnsurf, surf->CDisurf, surf->CDvsurf );
      }
    }
    else
      goto error;

    // 'CL_srf CD_srf'
    isOK = getLine_real2( fp, "CL_srf CD_srf", &line, &surf->CL_srf, &surf->CD_srf );
    if (isOK)
    {
      if (verbose) printf( "CL_srf = %lf  CD_srf = %lf\n", surf->CL_srf, surf->CD_srf );
    }
    else
      goto error;

    // Strip Forces referred to Strip Area, Chord
    isOK = getLine_line( fp, &line );
    if (isOK)
    {
      if (verbose) printf( "%s\n", chop_newline(line.line) );
    }
    else
      goto error;

    // j, Xle, Yle, Zle, Chord, Area, c_cl, ai, cl_norm, cl, cd, cdv, cm_c/4, cm_LE, C.P.x/c
    isOK = getLine_line( fp, &line );
    if (isOK)
    {
      if (verbose) printf( "%s\n", chop_newline(line.line) );
    }
    else
      goto error;

    surf->j = (int*)malloc(surf->nSpan*sizeof(int));
    if (surf->j == NULL)
    {
      printf( "ERROR: malloc error on 'j'\n" );
      goto error;
    }

    i = 0;
    rest = chop_newline(line.line);
    strtok_r(rest, ", ", &rest); // skip "j"
    while ((token = strtok_r(rest, ", ", &rest)))
    {
      surf->data[i].name = strdup(token);
      if (surf->data[i].name == NULL)
      {
        printf( "ERROR: malloc error on 'name'\n" );
        goto error;
      }
      surf->data[i].val = (double*)malloc(surf->nSpan*sizeof(double));
      if (surf->data[i].val == NULL)
      {
        printf( "ERROR: malloc error on 'val'\n" );
        goto error;
      }
      i++;
    }

    for (istrp = 0; istrp < surf->nSpan; istrp++)
    {
      isOK = getLine_int1_realn( fp, "j, Xle, Yle, Zle, Chord, Area, c_cl, ai, cl_norm, cl, cd, cdv, cm_c/4, cm_LE, C.P.x/c\n", &line, &i, val, AVL_NSTRP_DATA );
      if (isOK)
      {
        surf->j[istrp] = i;
        for (n = 0; n < AVL_NSTRP_DATA; n++)
          surf->data[n].val[istrp] = val[n];

        if (verbose)
        {
          printf( "%d ", surf->j[istrp] );
          for (n = 0; n < AVL_NSTRP_DATA; n++)
            printf( "%lf ", surf->data[n].val[istrp] );
          printf( "\n" );
        }
      }
      else
        goto error;
    }
  }

  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  if (verbose) printf( "STRP file read OK\n" );
  return 0;

error:
  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  avlFree_STRP(strp);
  return -1;
}


//----------------------------------------------------------------------------//
//
void avlInit_STRP( avlStrp *strp )
{
  if (strp == NULL) return;
  strp->surf = NULL;
  strp->nSurf = 0;
  strp->Sref = strp->Bref = strp->Cref = 0;
  strp->Xref = strp->Yref = strp->Zref = 0;
}


//----------------------------------------------------------------------------//
//
void avlFree_STRP( avlStrp *strp )
{
  int i, n;

  if (strp == NULL) return;

  for (i = 0; i < strp->nSurf; i++)
  {
    avl_free(strp->surf[i].name);
    avl_free(strp->surf[i].j);
    for (n = 0; n < AVL_NSTRP_DATA; n++)
    {
      avl_free(strp->surf[i].data[n].name);
      avl_free(strp->surf[i].data[n].val);
    }
  }
  avl_free(strp->surf);

  avlInit_STRP( strp );
}
