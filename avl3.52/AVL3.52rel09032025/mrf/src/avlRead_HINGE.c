//**********************************************************************
//   Module:  avlRead_HINGE.c
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

#include "avlRead_HINGE.h"
#define AVL_MRF_INTERNAL
#include "read_util.h"

//----------------------------------------------------------------------------//
// HINGE: hinge moments
//
int avlRead_HINGE( const char *filename, avlHinge *hinge, bool verbose )
{
  int icont;
  char *string=NULL, *version=NULL;
  size_t stringsize = 128;
  bool isOK;
  FILE *fp = NULL;
  avlLineBuffer line;
  line.line = NULL;
  line.linesize = 0;
  
  if (hinge == NULL) return -1;

  avlFree_HINGE( hinge );

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

  // HINGE
  isOK = getLine_string1( fp, "HINGE", &line, string );
  if (isOK)
  {
    if (strcmp(string, "HINGE") != 0)
    {
      printf( "ERROR in %s: expected 'HINGE' file ID but got '%s'\n", __func__, string );
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

  // 'Sref, Cref'
  isOK = getLine_real2( fp, "Sref, Cref", &line, &hinge->Sref, &hinge->Cref );
  if (isOK)
  {
    if (verbose) printf( "Sref = %lf  Cref = %lf\n", hinge->Sref, hinge->Cref );
  }
  else
    goto error;

  // # of controls
  isOK = getLine_int1( fp, "# of controls", &line, &hinge->nCont );
  if (isOK)
  {
    if (verbose) printf( "# controls = %d\n", hinge->nCont );
  }
  else
    goto error;

  // allocated controls
  hinge->cont = (avlHingeControl *) malloc( hinge->nCont * sizeof(avlHingeControl) );
  if (hinge->cont == NULL)
  {
    hinge->nCont = 0;
    printf( "ERROR: malloc error on 'avlHingeControl'\n" );
    goto error;
  }
  for (icont = 0; icont < hinge->nCont; icont++)
    hinge->cont[icont].name = NULL;

  for (icont = 0; icont < hinge->nCont; icont++)
  {
    // 'Chinge, Control'
    isOK = getLine_real1_string1( fp, "Chinge, Control", &line, &hinge->cont[icont].Chinge, string );
    if (isOK)
    {
      hinge->cont[icont].name = strdup(string);
      if (hinge->cont[icont].name == NULL) goto error;
      if (verbose) printf( "Chinge = %e  Control = '%s'\n", hinge->cont[icont].Chinge, string );
    }
    else
      goto error;
  }

  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  if (verbose) printf( "HINGE file read OK\n" );
  return 0;

error:
  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  avlFree_HINGE( hinge );
  return -1;
}


//----------------------------------------------------------------------------//
//
void avlInit_HINGE( avlHinge *hinge )
{
  if (hinge == NULL) return;

  hinge->cont = NULL;
  hinge->nCont = 0;

  hinge->Sref = hinge->Cref = 0;
}


//----------------------------------------------------------------------------//
//
void avlFree_HINGE( avlHinge *hinge )
{
  int i;

  if (hinge == NULL) return;

  for (i = 0; i < hinge->nCont; i++)
    avl_free(hinge->cont[i].name);
  avl_free(hinge->cont);

  avlInit_HINGE( hinge );
}
