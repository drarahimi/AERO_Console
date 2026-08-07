//**********************************************************************
//   Module:  avlRead_ELE.c
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

#include "avlRead_ELE.h"
#define AVL_MRF_INTERNAL
#include "read_util.h"


//----------------------------------------------------------------------------//
// ELE: individual vortex strengths
//
int avlRead_ELE( const char *filename, avlEle *ele, bool verbose )
{
  int iv, n, istrp, isurf;
  double val[9];
  char *string = NULL;
  char *version = NULL;
  size_t stringsize = 128;
  bool isOK;
  avlEleSurf *surf = NULL;
  avlEleStrp *strp = NULL;
  FILE *fp = NULL;
  avlLineBuffer line;
  line.line = NULL;
  line.linesize = 0;

  if (ele == NULL) return -1;

  avlFree_ELE( ele );

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

  // ELE
  isOK = getLine_string1( fp, "ELE", &line, string );
  if (isOK)
  {
    if (strcmp(string, "ELE") != 0)
    {
      printf( "ERROR in %s: expected 'ELE' file ID but got '%s'\n", __func__, string );
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
  isOK = getLine_real3( fp, "Sref, Cref, Bref", &line, &ele->Sref, &ele->Cref, &ele->Bref );
  if (isOK)
  {
    if (verbose) printf( "Sref = %lf  Cref = %lf  Bref = %lf\n", ele->Sref, ele->Cref, ele->Bref );
  }
  else
    goto error;

  // 'Xref, Yref, Zref'
  isOK = getLine_real3( fp, "Xref, Yref, Zref", &line, &ele->Xref, &ele->Yref, &ele->Zref );
  if (isOK)
  {
    if (verbose) printf( "Xref = %lf  Yref = %lf  Zref = %lf\n", ele->Xref, ele->Yref, ele->Zref );
  }
  else
    goto error;

  // 'Vortex Strengths (by surface, by strip)'
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // # of surfaces
  isOK = getLine_int1( fp, "# of surfaces", &line, &ele->nSurf );
  if (isOK)
  {
    if (verbose) printf( "# surfaces = %d\n", ele->nSurf );
  }
  else
    goto error;

  // allocated surfaces
  ele->surf = (avlEleSurf *) malloc( ele->nSurf * sizeof(avlEleSurf) );
  if (ele->surf == NULL)
  {
    ele->nSurf = 0;
    printf( "ERROR: malloc error on 'avlBodyProp'\n" );
    goto error;
  }
  for (isurf = 0; isurf < ele->nSurf; isurf++)
  {
    ele->surf[isurf].name = NULL;
    ele->surf[isurf].nStrp = 0;
    ele->surf[isurf].strp = NULL;
  }

  for (isurf = 0; isurf < ele->nSurf; isurf++)
  {
    surf = &ele->surf[isurf];

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
      if (verbose) printf( "%s\n", chop_newline(line.line) );
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


    surf->nStrp = surf->nSpan;
    // allocated strips
    surf->strp = (avlEleStrp *) malloc( surf->nStrp * sizeof(avlEleStrp) );
    if (surf->strp == NULL)
    {
      surf->nStrp  = 0;
      printf( "ERROR: malloc error on 'avlEleStrp'\n" );
      goto error;
    }
    for (istrp = 0; istrp < surf->nStrp; istrp++)
    {
      surf->strp[istrp].I = NULL;
      surf->strp[istrp].X = NULL;
      surf->strp[istrp].Y = NULL;
      surf->strp[istrp].Z = NULL;
      surf->strp[istrp].DX = NULL;
      surf->strp[istrp].Slope = NULL;
      surf->strp[istrp].dCp = NULL;
    }

    for (istrp = 0; istrp < surf->nStrp; istrp++)
    {
      surf->strp[istrp].I = (double*)malloc(surf->nChord*sizeof(double));
      if (surf->strp[istrp].I == NULL) { printf( "ERROR: malloc error on 'I'\n" ); goto error; }
      surf->strp[istrp].X = (double*)malloc(surf->nChord*sizeof(double));
      if (surf->strp[istrp].X == NULL) { printf( "ERROR: malloc error on 'X'\n" ); goto error; }
      surf->strp[istrp].Y = (double*)malloc(surf->nChord*sizeof(double));
      if (surf->strp[istrp].Y == NULL) { printf( "ERROR: malloc error on 'Y'\n" ); goto error; }
      surf->strp[istrp].Z = (double*)malloc(surf->nChord*sizeof(double));
      if (surf->strp[istrp].Z == NULL) { printf( "ERROR: malloc error on 'Z'\n" ); goto error; }
      surf->strp[istrp].DX = (double*)malloc(surf->nChord*sizeof(double));
      if (surf->strp[istrp].DX == NULL) { printf( "ERROR: malloc error on 'DX'\n" ); goto error; }
      surf->strp[istrp].Slope = (double*)malloc(surf->nChord*sizeof(double));
      if (surf->strp[istrp].Slope == NULL) { printf( "ERROR: malloc error on 'Slope'\n" ); goto error; }
      surf->strp[istrp].dCp = (double*)malloc(surf->nChord*sizeof(double));
      if (surf->strp[istrp].dCp == NULL) { printf( "ERROR: malloc error on 'dCp'\n" ); goto error; }
    }

    for (istrp = 0; istrp < surf->nStrp; istrp++)
    {
      strp = &surf->strp[istrp];

      // STRIP
      isOK = getLine_string1( fp, "STRIP", &line, string );
      if (isOK)
      {
        if (strcmp(string, "STRIP") != 0)
        {
          printf( "ERROR: expected STRIP keyword but got '%s'\n", string );
          goto error;
        }
        if (verbose) printf( "%s\n", string );
      }
      else
        goto error;

      // 'Strip #, # Chordwise, First Vortex'
      isOK = getLine_int3( fp, "Strip #, # Chordwise, First Vortex", &line, &strp->iStrip, &strp->nChord, &strp->iFirst );
      if (isOK)
      {
        if (verbose) printf( "Strip # = %d  # Chordwise = %d  1st vortex = %d\n", strp->iStrip, strp->nChord, strp->iFirst );
      }
      else
        goto error;

      // 'Xle, Ave. Chord, Incidence (deg), Yle, Strip Width, Strip Area, Zle, Strip Dihed (deg)'
      isOK = getLine_realn( fp, "Xle, Ave. Chord, Incidence (deg), Yle, Strip Width, Strip Area, Zle, Strip Dihed (deg)", &line, val, 8 );
      if (isOK)
      {
        strp->Xle_ave   = val[0];
        strp->Cave      = val[1];
        strp->Incidence = val[2];
        strp->Yle       = val[3];
        strp->StrpWidth = val[4];
        strp->StrpArea  = val[5];
        strp->Zle       = val[6];
        strp->StrpDihed = val[7];
        if (verbose)
        {
          printf( "Xle = %lf  Ave. chord = %lf  Incidence = %lf deg  Yle = %lf  ", strp->Xle_ave, strp->Cave, strp->Incidence, strp->Yle );
          printf( "Strip Width = %lf  Strip Area = %lf  Zle = %lf  Strip Dihed = %lf deg\n", strp->StrpWidth, strp->StrpArea, strp->Zle, strp->StrpDihed );
        }
      }
      else
        goto error;

      // 'cl, cd, cdv, cn, ca, cnc, wake dnwsh, cmLE, cm c/4'
      isOK = getLine_realn( fp, "cl, cd, cdv, cn, ca, cnc, wake dnwsh, cmLE, cm c/4", &line, val, 9 );
      if (isOK)
      {
        strp->cl         = val[0];
        strp->cd         = val[1];
        strp->cdv        = val[2];
        strp->cn         = val[3];
        strp->ca         = val[4];
        strp->cnc        = val[5];
        strp->wake_dnwsh = val[6];
        strp->cmLE       = val[7];
        strp->cm_c4      = val[8];
        if (verbose)
        {
          printf( "cl = %lf  cd = %lf  cdv = %lf  cn = %lf  ca = %lf  ", strp->cl,
                                                                         strp->cd,
                                                                         strp->cdv,
                                                                         strp->cn,
                                                                         strp->ca );
          printf( "cnc = %lf  wake dnwsh = %lf  cmLE = %lf  cm c/4 = %lf\n", strp->cnc,
                                                                             strp->wake_dnwsh,
                                                                             strp->cmLE,
                                                                             strp->cm_c4 );
        }
      }
      else
        goto error;

      for (iv = 0; iv < strp->nChord; iv++)
      {
        // I, X, Y, Z, DX, Slope, dCp
        isOK = getLine_int1_realn( fp, "I, X, Y, Z, DX, Slope, dCp", &line, &n, val, 6 );
        if (isOK)
        {
          strp->I[iv]     = n;
          strp->X[iv]     = val[0];
          strp->Y[iv]     = val[1];
          strp->Z[iv]     = val[2];
          strp->DX[iv]    = val[3];
          strp->Slope[iv] = val[4];
          strp->dCp[iv]   = val[5];
          if (verbose)
          {
            printf( "%d ", n );
            for (n = 0; n < 6; n++)
              printf( "%lf ", val[n] );
            printf( "\n" );
          }
        }
        else
          goto error;
      }
    }
  }

  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  if (verbose) printf( "ELE file read OK\n" );
  return 0;

error:
  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  avlFree_ELE(ele);
  return -1;
}


//----------------------------------------------------------------------------//
//
void avlInit_ELE( avlEle *ele )
{
  if (ele == NULL) return;
  ele->surf = NULL;
  ele->nSurf = 0;
  ele->Sref = ele->Bref = ele->Cref = 0;
  ele->Xref = ele->Yref = ele->Zref = 0;
}


//----------------------------------------------------------------------------//
//
void avlFree_ELE( avlEle *ele )
{
  int i, n;

  if (ele == NULL) return;

  for (i = 0; i < ele->nSurf; i++)
  {
    avl_free(ele->surf[i].name);
    for (n = 0; n < ele->surf[i].nStrp; n++)
    {
      avl_free(ele->surf[i].strp[n].I);
      avl_free(ele->surf[i].strp[n].X);
      avl_free(ele->surf[i].strp[n].Y);
      avl_free(ele->surf[i].strp[n].Z);
      avl_free(ele->surf[i].strp[n].DX);
      avl_free(ele->surf[i].strp[n].Slope);
      avl_free(ele->surf[i].strp[n].dCp);
    }
    avl_free(ele->surf[i].strp);
  }
  avl_free(ele->surf);

  avlInit_ELE( ele );
}

