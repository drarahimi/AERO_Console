//**********************************************************************
//   Module:  avlRead_BODY.c
//
//   Copyright (C) 2022 Mark Drela, Harold Youngren, Steven Allmaras
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

#include "avlRead_BODY.h"
#define AVL_MRF_INTERNAL
#include "read_util.h"

//----------------------------------------------------------------------------//
// BODY: body forces
//
int avlRead_BODY( const char *filename, avlBody *body, bool verbose )
{
  int n, ibody;
  char *string = NULL, *version = NULL;
  double val[9];
  bool isOK;
  size_t stringsize = 128;
  FILE *fp = NULL;
  avlLineBuffer line;
  line.line = NULL;
  line.linesize = 0;

  if (body == NULL) return -1;

  avlFree_BODY( body );

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

  // BODY
  isOK = getLine_string1( fp, "BODY", &line, string );
  if (isOK)
  {
    if (strcmp(string, "BODY") != 0)
    {
      printf( "ERROR in %s: expected 'BODY' file ID but got '%s'\n", __func__, string );
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
  isOK = getLine_real3( fp, "Sref, Cref, Bref", &line, &body->Sref, &body->Cref, &body->Bref );
  if (isOK)
  {
    if (verbose) printf( "Sref = %lf  Cref = %lf  Bref = %lf\n", body->Sref, body->Cref, body->Bref );
  }
  else
    goto error;

  // 'Xref, Yref, Zref'
  isOK = getLine_real3( fp, "Xref, Yref, Zref", &line, &body->Xref, &body->Yref, &body->Zref );
  if (isOK)
  {
    if (verbose) printf( "Xref = %lf  Yref = %lf  Zref = %lf\n", body->Xref, body->Yref, body->Zref );
  }
  else
    goto error;

  // # of bodies
  body->nbody = 0;
  isOK = getLine_int1( fp, "# of bodies", &line, &body->nbody );
  if (isOK)
  {
    if (verbose) printf( "# bodies = %d\n", body->nbody );
  }
  else
    goto error;

  // allocated bodies
  body->body = (avlBodyProp *) malloc( body->nbody * sizeof(avlBodyProp) );
  if (body->body == NULL)
  {
    body->nbody = 0;
    printf( "ERROR: malloc error on 'avlBodyProp'\n" );
    goto error;
  }
  for (ibody = 0; ibody < body->nbody; ibody++)
    body->body[ibody].name = NULL;


  for (ibody = 0; ibody < body->nbody; ibody++)
  {
    // BODY
    isOK = getLine_string1( fp, "BODY", &line, string );
    if (isOK)
    {
      if (strcmp(string, "BODY") != 0)
      {
        printf( "ERROR: expected BODY keyword but got '%s'\n", string );
        goto error;
      }
      if (verbose) printf( "%s\n", string );
    }
    else
      goto error;

    // body name
    isOK = getLine_line( fp, &line );
    if (isOK)
    {
      body->body[ibody].name = strdup(chop_newline(line.line));
      if (body->body[ibody].name == NULL) goto error;
      if (verbose) printf( "%s\n", body->body[ibody].name );
    }
    else
      goto error;


    // 'Ibdy Length Asurf Vol CL CD Cm CY Cn Cl'
    isOK = getLine_int1_realn( fp, "Ibdy Length Asurf Vol CL CD Cm CY Cn Cl", &line, &n, val, 9 );
    if (isOK)
    {
      body->body[ibody].Length = val[0];
      body->body[ibody].Asurf = val[1];
      body->body[ibody].Vol = val[2];
      body->body[ibody].CL = val[3];
      body->body[ibody].CD = val[4];
      body->body[ibody].Cm = val[5];
      body->body[ibody].CY = val[6];
      body->body[ibody].Cn = val[7];
      body->body[ibody].Cl = val[8];
      if (verbose)
      {
        printf( "%d  Length = %lf  Asurf = %lf  Vol = %lf  CL = %lf  ", n,
                                                                        body->body[ibody].Length,
                                                                        body->body[ibody].Asurf,
                                                                        body->body[ibody].Vol,
                                                                        body->body[ibody].CL );
        printf( "CD = %lf  Cm = %lf  CY = %lf  Cn = %lf  Cl = %lf\n", body->body[ibody].CD,
                                                                      body->body[ibody].Cm,
                                                                      body->body[ibody].CY,
                                                                      body->body[ibody].Cn,
                                                                      body->body[ibody].Cl );
      }
    }
    else
      goto error;
  }

  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  if (verbose) printf( "BODY file read OK\n" );
  return 0;

error:
  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  fclose( fp );
  avlFree_BODY(body);
  return -1;
}

//----------------------------------------------------------------------------//
//
void avlInit_BODY( avlBody *body )
{
  if (body == NULL) return;
  body->body = NULL;
  body->nbody = 0;
  body->Sref = body->Bref = body->Cref = 0;
  body->Xref = body->Yref = body->Zref = 0;
}

//----------------------------------------------------------------------------//
//
void avlFree_BODY( avlBody *body )
{
  int i;
  if (body == NULL) return;
  for (i = 0; i < body->nbody; i++)
    avl_free(body->body[i].name);
  avl_free(body->body);
  avlInit_BODY(body);
}
