//**********************************************************************
//   Module:  avlRead_CNC.c
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

#include "avlRead_CNC.h"
#define AVL_MRF_INTERNAL
#include "read_util.h"

//----------------------------------------------------------------------------//
// CNC: CNC loading file
//
int avlRead_CNC( const char *filename, avlCnc* cnc, bool verbose )
{
  char *string = NULL, *version = NULL;
  int i, istrp;
  bool isOK;
  size_t stringsize = 128;
  double val[8];
  FILE *fp = NULL;

  const char *names[] = {"XM", "YM", "ZM", "CNCM", "CLM", "CHM", "DYM", "ASM"};

  avlLineBuffer line;
  line.line = NULL;
  line.linesize = 0;

  if (cnc == NULL) return -1;

  avlFree_CNC( cnc );

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

  // CNC
  isOK = getLine_string1( fp, "CNC", &line, string );
  if (isOK)
  {
    if (strcmp(string, "CNC") != 0)
    {
      printf( "ERROR in %s: expected 'CNC' file ID but got '%s'\n", __func__, string );
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

  // 'Strip Loadings:  XM, YM, ZM, CNCM, CLM, CHM, DYM, ASM'
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // # of strips
  cnc->nStrp = 0;
  isOK = getLine_int1( fp, "# of strips", &line, &cnc->nStrp );
  if (isOK)
  {
    if (verbose) printf( "# strips = %d\n", cnc->nStrp );
  }
  else
    goto error;

  for (i = 0; i < AVL_NCNC_DATA; i++)
  {
    cnc->data[i].name = names[i];
    cnc->data[i].val = (double*)malloc(cnc->nStrp*sizeof(double));
    if (cnc->data[i].val == NULL)
    {
      printf( "ERROR: malloc error on 'val'\n" );
      goto error;
    }
  }

  for (istrp = 0; istrp < cnc->nStrp; istrp++)
  {
    // 'XM, YM, ZM, CNCM, CLM, CHM, DYM, ASM'
    isOK = getLine_realn( fp, "XM, YM, ZM, CNCM, CLM, CHM, DYM, ASM", &line, val, 8 );
    if (isOK)
    {
      for (i = 0; i < AVL_NCNC_DATA; i++)
        cnc->data[i].val[istrp] = val[i];

      if (verbose)
        printf( "%d  XM = %lf  YM = %lf  ZM = %lf  CNCM = %lf  CLM = %lf  CHM = %lf  DYM = %lf  ASM = %lf\n",
          istrp+1, val[0], val[1], val[2], val[3], val[4], val[5], val[6], val[7] );
    }
    else
      goto error;
  }

  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  if (verbose) printf( "CNC file read OK\n" );
  return 0;

error:
  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  avlFree_CNC(cnc);
  return -1;
}

//----------------------------------------------------------------------------//
//
void avlInit_CNC( avlCnc* cnc )
{
  int i;
  if (cnc == NULL) return;
  for (i = 0; i < AVL_NCNC_DATA; i++)
  {
    cnc->data[i].name = NULL;
    cnc->data[i].val = NULL;
  }
  cnc->nStrp = 0;
}


//----------------------------------------------------------------------------//
//
void avlFree_CNC( avlCnc* cnc )
{
  int i;
  for (i = 0; i < AVL_NCNC_DATA; i++)
    avl_free(cnc->data[i].val);

  avlInit_CNC( cnc );
}
