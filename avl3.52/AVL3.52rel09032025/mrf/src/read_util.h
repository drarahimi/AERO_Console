//**********************************************************************
//   Module:  avlRead_util.c
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

#ifndef AVL_MRF_INTERNAL
#error "This header is only intended for avlmrf internal use"
#endif

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>

typedef struct
{
  char *line;
  size_t linesize;
} avlLineBuffer;

// safe free
static
void avl_free( void *ptr )
{
  if (ptr == NULL) return;
  free(ptr);
}

// replace newline (\n) with end-of-string (\0), and remove trailing white spaces
static
char* chop_newline( char *line )
{
  int len;
  char *ptr;
  if (line == NULL) return NULL;
  ptr = strchr(line, '\n');
  if (ptr != NULL) *ptr = '\0';
  while ((len = strlen(line)) > 0 && line[len-1] == ' ') line[len-1] = '\0';
  return line;
}

static
bool getLine_line( FILE *fp, avlLineBuffer *line )
{
  bool success = false;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    success = true;
  }
  else
  {
    printf( "ERROR on read in %s\n", __func__ );
  }

  return success;
}

static
bool getLine_int1( FILE *fp, const char *msg, avlLineBuffer *line, int *val1 )
{
  bool success = false;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    size_t nread = sscanf( line->line, "%d", val1 );
    if (nread == 1)
    {
      success = true;
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
}

static
bool getLine_int2( FILE *fp, const char *msg, avlLineBuffer *line, int *val1, int *val2 )
{
  bool success = false;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    size_t nread = sscanf( line->line, "%d %d", val1, val2 );
    if (nread == 2)
    {
      success = true;
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
}

static
bool getLine_int3( FILE *fp, const char *msg, avlLineBuffer *line, int *val1, int *val2, int *val3 )
{
  bool success = false;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    size_t nread = sscanf( line->line, "%d %d %d", val1, val2, val3 );
    if (nread == 3)
    {
      success = true;
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
}

static
bool getLine_int4( FILE *fp, const char *msg, avlLineBuffer *line, int *val1, int *val2, int *val3, int *val4 )
{
  bool success = false;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    size_t nread = sscanf( line->line, "%d %d %d %d", val1, val2, val3, val4 );
    if (nread == 4)
    {
      success = true;
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
}

static
bool getLine_intn( FILE *fp, const char *msg, avlLineBuffer *line, int *valn, int nn )
{
  //printf( "%s: nn = %d\n", __func__, nn );
  int n;
  bool success = false;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  //printf( "%s: readsize = %zu  linesize = %zu  linesize0 = %zu\n", __func__, readsize, linesize, linesize0 );
  if (readsize > 0)
  {
    char *ws = " ";   // whitespace delimiter
    char *token = NULL;
    size_t nread = 0;

    token = strtok( line->line, ws );
    if (token == NULL) goto error;
    nread = sscanf( token, "%d", &(valn[0]) );
    if (nread == 1)
    {
      for (n = 1; n < nn; n++)
      {
        token = strtok( NULL, ws );
        if (token == NULL) goto error;
        nread += sscanf( token, "%d", &(valn[n]) );
      }
      if (nread == nn)
      {
        success = true;
      }
      else
      {
        printf( "ERROR: expected %s; got %s\n", msg, line->line );
      }
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
error:
  printf( "ERROR: expected %s; got %s\n", msg, line->line );
  return false;
}

static
bool getLine_real1( FILE *fp, const char *msg, avlLineBuffer *line, double *val1 )
{
  bool success = false;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    size_t nread = sscanf( line->line, "%lf", val1 );
    if (nread == 1)
    {
      success = true;
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
}

static
bool getLine_real2( FILE *fp, const char *msg, avlLineBuffer *line, double *val1, double *val2 )
{
  bool success = false;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    size_t nread = sscanf( line->line, "%lf %lf", val1, val2 );
    if (nread == 2)
    {
      success = true;
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
}

static
bool getLine_real3( FILE *fp, const char *msg, avlLineBuffer *line, double *val1, double *val2, double *val3 )
{
  bool success = false;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    size_t nread = sscanf( line->line, "%lf %lf %lf", val1, val2, val3 );
    if (nread == 3)
    {
      success = true;
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
}

static
bool getLine_real4( FILE *fp, const char *msg, avlLineBuffer *line, double *val1, double *val2, double *val3, double *val4 )
{
  bool success = false;
  //printf( "%s: line = %p  linesize = %zu\n", __func__, line, linesize );

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    size_t nread = sscanf( line->line, "%lf %lf %lf %lf", val1, val2, val3, val4 );
    if (nread == 4)
    {
      success = true;
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
}

static
bool getLine_realn( FILE *fp, const char *msg, avlLineBuffer *line, double *valn, int nn )
{
  //printf( "%s: nn = %d\n", __func__, nn );
  int n;
  bool success = false;
  char *ws = " ";   // whitespace delimiter
  char *token;
  size_t nread = 0;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {

    token = strtok( line->line, ws );
    if (token == NULL) goto error;
    nread = sscanf( token, "%lf", &(valn[0]) );
    if (nread == 1)
    {
      for (n = 1; n < nn; n++)
      {
        token = strtok( NULL, ws );
        if (token == NULL) goto error;
        nread += sscanf( token, "%lf", &(valn[n]) );
      }
      if (nread == nn)
      {
        success = true;
      }
      else
      {
        printf( "ERROR: expected %s; got %s\n", msg, line->line );
      }
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
error:
  printf( "ERROR: expected %s; got %s\n", msg, line->line );
  return false;
}

static
bool getLine_string1( FILE *fp, const char *msg, avlLineBuffer *line, char *string1 )
{
  bool success = false;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    size_t nread = sscanf( line->line, "%s", string1 );
    if (nread == 1)
    {
      success = true;
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
}

static
bool getLine_string2( FILE *fp, const char *msg, avlLineBuffer *line, char *string1, char *string2 )
{
  bool success = false;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    size_t nread = sscanf( line->line, "%s %s", string1, string2 );
    if (nread == 2)
    {
      success = true;
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
}

static
bool getLine_real1_string1( FILE *fp, const char *msg, avlLineBuffer *line, double *val1, char *string1 )
{
  bool success = false;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    size_t nread = sscanf( line->line, "%lf %s", val1, string1 );
    if (nread == 2)
    {
      success = true;
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
}

static
bool getLine_int1_realn( FILE *fp, const char *msg, avlLineBuffer *line, int *val1, double *valn, int nn )
{
  int n;
  bool success = false;
  char *ws = " ";   // whitespace delimiter
  char *token;
  size_t nread;

  size_t readsize = getline( &line->line, &line->linesize, fp );
  if (readsize > 0)
  {
    token = strtok( line->line, ws );
    if (token == NULL) goto error;
    nread = sscanf( token, "%d", val1 );
    if (nread == 1)
    {
      for (n = 0; n < nn; n++)
      {
        token = strtok( NULL, ws );
        if (token == NULL) goto error;
        nread += sscanf( token, "%lf", &(valn[n]) );
      }
      if (nread == nn+1)
      {
        success = true;
      }
      else
      {
        printf( "ERROR: expected %s; got %s\n", msg, line->line );
      }
    }
    else
    {
      printf( "ERROR: expected %s; got %s\n", msg, line->line );
    }
  }

  return success;
error:
  printf( "ERROR: expected %s; got %s\n", msg, line->line );
  return false;
}
