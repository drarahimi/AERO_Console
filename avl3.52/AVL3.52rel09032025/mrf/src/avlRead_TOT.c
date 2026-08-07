//**********************************************************************
//   Module:  avlRead_TOT.c
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

#include "avlRead_TOT.h"
#define AVL_MRF_INTERNAL
#include "read_util.h"

//----------------------------------------------------------------------------//
// TOT: total forces
//
int avlRead_TOT2( FILE *fp, avlTot *tot, bool verbose, char *fileID )
{
  int icont, idesign;
  char *string = NULL, *version = NULL;
  size_t stringsize = 128;
  bool isOK;
  avlLineBuffer line;
  line.line = NULL;
  line.linesize = 0;

  if (tot == NULL) return -1;

  avlFree_TOT( tot );

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

  // file ID
  isOK = getLine_string1( fp, "file ID", &line, string );
  if (isOK)
  {
    if (strcmp(string, fileID) != 0)
    {
      printf( "ERROR in %s: expected '%s' file ID but got '%s'\n", __func__, fileID, string );
      goto error;
    }
    if (verbose) printf( "file ID = %s\n", string );
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

  // 'Vortex Lattice Output -- Total Forces'
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // configuration
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    tot->name = strdup(chop_newline(line.line));
    if (tot->name == NULL) goto error;
    if (verbose) printf( "%s\n", tot->name );
  }
  else
    goto error;

  // # of surfaces
  isOK = getLine_int1( fp, "# of surfaces", &line, &tot->nSurf );
  if (isOK)
  {
    if (verbose) printf( "# surfaces = %d\n", tot->nSurf );
  }
  else
    goto error;

  // # of strips
  isOK = getLine_int1( fp, "# of strips", &line, &tot->nStrp );
  if (isOK)
  {
    if (verbose) printf( "# strips = %d\n", tot->nStrp );
  }
  else
    goto error;

  // # of vortices
  isOK = getLine_int1( fp, "# of vortices", &line, &tot->nVort );
  if (isOK)
  {
    if (verbose) printf( "# vortices = %d\n", tot->nVort );
  }
  else
    goto error;

  // 'Sref, Cref, Bref'
  isOK = getLine_real3( fp, "Sref, Cref, Bref", &line, &tot->Sref, &tot->Cref, &tot->Bref );
  if (isOK)
  {
    if (verbose) printf( "Sref = %lf  Cref = %lf  Bref = %lf\n", tot->Sref, tot->Cref, tot->Bref );
  }
  else
    goto error;

  // 'Xref, Yref, Zref'
  isOK = getLine_real3( fp, "Xref, Yref, Zref", &line, &tot->Xref, &tot->Yref, &tot->Zref );
  if (isOK)
  {
    if (verbose) printf( "Xref = %lf  Yref = %lf  Zref = %lf\n", tot->Xref, tot->Yref, tot->Zref );
  }
  else
    goto error;

  // axes orientation
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    if (verbose) printf( "%s\n", chop_newline(line.line) );
  }
  else
    goto error;

  // run title
  isOK = getLine_line( fp, &line );
  if (isOK)
  {
    tot->title = strdup(chop_newline(line.line));
    if (tot->title == NULL) goto error;
    if (verbose) printf( "%s\n", tot->title );
  }
  else
    goto error;

  // 'Alpha, pb/2V, p'b/2V'
  isOK = getLine_real3( fp, "Alpha, pb/2V, p'b/2V", &line, &tot->Alpha, &tot->pb_2V, &tot->pPb_2V );
  if (isOK)
  {
    if (verbose) printf( "Alpha = %lf  pb/2V = %lf  p'b/2V = %lf\n", tot->Alpha, tot->pb_2V, tot->pPb_2V );
  }
  else
    goto error;

  // 'Beta, qc/2V'
  isOK = getLine_real2( fp, "Beta, qc/2V", &line, &tot->Beta, &tot->qc_2V );
  if (isOK)
  {
    if (verbose) printf( "Beta = %lf  qc/2V = %lf\n", tot->Beta, tot->qc_2V );
  }
  else
    goto error;

  // 'Mach, rb/2V, r'b/2V'
  isOK = getLine_real3( fp, "Mach, rb/2V, r'b/2V", &line, &tot->Mach, &tot->rb_2V, &tot->rPb_2V );
  if (isOK)
  {
    if (verbose) printf( "Mach = %lf  rb/2V = %lf  r'b/2V = %lf\n", tot->Mach, tot->rb_2V, tot->rPb_2V );
  }
  else
    goto error;

  // 'CXtot, Cltot, Cl'tot'
  isOK = getLine_real3( fp, "CXtot, Cltot, Cl'tot", &line, &tot->CXtot, &tot->Cltot, &tot->ClPtot );
  if (isOK)
  {
    if (verbose) printf( "CXtot = %lf  Cltot = %lf  Cl'tot = %lf\n", tot->CXtot, tot->Cltot, tot->ClPtot );
  }
  else
    goto error;

  // 'CYtot, Cmtot'
  isOK = getLine_real2( fp, "CYtot, Cmtot", &line, &tot->CYtot, &tot->Cmtot );
  if (isOK)
  {
    if (verbose) printf( "CYtot = %lf  Cmtot/2V = %lf\n", tot->CYtot, tot->Cmtot );
  }
  else
    goto error;

  // 'CZtot, Cntot, Cn'tot'
  isOK = getLine_real3( fp, "CZtot, Cntot, Cn'tot", &line, &tot->CZtot, &tot->Cntot, &tot->CnPtot );
  if (isOK)
  {
    if (verbose) printf( "CZtot = %lf  Cntot = %lf  Cn'tot = %lf\n", tot->CZtot, tot->Cntot, tot->CnPtot );
  }
  else
    goto error;

  // 'CLtot'
  isOK = getLine_real1( fp, "CLtot", &line, &tot->CLtot );
  if (isOK)
  {
    if (verbose) printf( "CLtot = %lf\n", tot->CLtot );
  }
  else
    goto error;

  // 'CDtot'
  isOK = getLine_real1( fp, "CDtot", &line, &tot->CDtot );
  if (isOK)
  {
    if (verbose) printf( "CDtot = %lf\n", tot->CDtot );
  }
  else
    goto error;

  // 'CDvis, CDind'
  isOK = getLine_real2( fp, "CDvis, CDind", &line, &tot->CDvis, &tot->CDind );
  if (isOK)
  {
    if (verbose) printf( "CDvis = %lf  CDind = %lf\n", tot->CDvis, tot->CDind );
  }
  else
    goto error;

  // 'Trefftz Plane: CLff, CDff, CYff, e'
  isOK = getLine_real4( fp, "Trefftz Plane: CLff, CDff, CYff, e", &line, &tot->CLff, &tot->CDff, &tot->CYff, &tot->e );
  if (isOK)
  {
    if (verbose) printf( "Trefftz Plane: CLff = %lf  CDff = %lf  CYff = %lf  e = %lf\n", tot->CLff, tot->CDff, tot->CYff, tot->e );
  }
  else
    goto error;

  // CONTROL
  isOK = getLine_string1( fp, "CONTROL", &line, string );
  if (isOK)
  {
    if (strcmp(string, "CONTROL") != 0)
    {
      printf( "ERROR: expected CONTROL keyword but got '%s'\n", string );
      goto error;
    }
    if (verbose) printf( "%s\n", string );
  }
  else
    goto error;

  // # control vars
  isOK = getLine_int1( fp, "# control vars", &line, &tot->nCont );
  if (isOK)
  {
    if (verbose) printf( "# control vars = %d\n", tot->nCont );
  }
  else
    goto error;

  // allocate controls
  tot->cont = (avlTotVar*)malloc( tot->nCont * sizeof(avlTotVar));
  if (tot->cont == NULL)
  {
    tot->nCont = 0;
    printf( "ERROR: malloc error on 'avlTotVar'\n" );
    goto error;
  }
  for (icont = 0; icont < tot->nCont; icont++)
    tot->cont[icont].name = NULL;

  // control value-name pairs
  for (icont = 0; icont < tot->nCont; icont++)
  {
    isOK = getLine_real1_string1( fp, "control value-name pairs", &line, &tot->cont[icont].val, string );
    if (isOK)
    {
      tot->cont[icont].name = strdup(string);
      if (tot->cont[icont].name == NULL) goto error;
      if (verbose) printf( "control val-name = %d %lf %s\n", icont, tot->cont[icont].val, tot->cont[icont].name );
    }
    else
      goto error;
  }

  // DESIGN
  isOK = getLine_string1( fp, "DESIGN", &line, string );
  if (isOK)
  {
    if (strcmp(string, "DESIGN") != 0)
    {
      printf( "ERROR: expected DESIGN keyword but got '%s'\n", string );
      goto error;
    }
    if (verbose) printf( "%s\n", string );
  }
  else
    goto error;

  // # design vars
  isOK = getLine_int1( fp, "# design vars", &line, &tot->nDesign );
  if (isOK)
  {
    if (verbose) printf( "# design vars = %d\n", tot->nDesign );
  }
  else
    goto error;

  // allocate controls
  tot->design = (avlTotVar*)malloc( tot->nDesign * sizeof(avlTotVar));
  if (tot->design == NULL)
  {
    tot->nDesign = 0;
    printf( "ERROR: malloc error on 'avlTotVar'\n" );
    goto error;
  }
  for (idesign = 0; idesign < tot->nDesign; idesign++)
    tot->design[idesign].name = NULL;


  // design value-name pairs
  for (idesign = 0; idesign < tot->nDesign; idesign++)
  {
    isOK = getLine_real1_string1( fp, "design value-name pairs", &line, &tot->design[idesign].val, string );
    if (isOK)
    {
      tot->design[idesign].name = strdup(string);
      if (tot->design[idesign].name == NULL) goto error;
      if (verbose) printf( "design val-name = %d %lf %s\n", idesign, tot->design[idesign].val, tot->design[idesign].name );
    }
    else
      goto error;
  }

  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  return 0;

error:
  avl_free( line.line );
  avl_free( string );
  avl_free( version );
  return -1;
}


//----------------------------------------------------------------------------//
int avlRead_TOT( const char *filename, avlTot *tot, bool verbose )
{
  int status;
  FILE *fp = NULL;

  fp = fopen( filename, "r" );
  if (fp == NULL)
  {
    printf( "ERROR: unable to open %s\n", filename );
    return -1;
  }

  status = avlRead_TOT2( fp, tot, verbose, "TOT" );

  fclose( fp );

  if (verbose && (status == 0)) printf( "TOT file read OK\n" );
  return status;
}


//----------------------------------------------------------------------------//
//
void avlInit_TOT( avlTot *tot )
{
  if (tot == NULL) return;

  tot->cont = NULL;
  tot->nCont = 0;

  tot->design = NULL;
  tot->nDesign = 0;

  tot->name = NULL;
  tot->title = NULL;

  tot->nSurf = tot->nStrp = tot->nVort = 0;

  tot->Sref = tot->Bref = tot->Cref = 0;
  tot->Xref = tot->Yref = tot->Zref = 0;

  tot->Alpha = tot->pb_2V = tot->pPb_2V = 0;
  tot->Beta = tot->qc_2V = 0;
  tot->Mach = tot->rb_2V = tot->rPb_2V = 0;
  tot->CXtot = tot->Cltot = tot->ClPtot = 0;
  tot->CYtot = tot->Cmtot = 0;
  tot->CZtot = tot->Cntot = tot->CnPtot = 0;
  tot->CLtot = tot->CDtot = 0;
  tot->CDvis = tot->CDind = 0;
  tot->CLff = tot->CDff = tot->CYff = tot->e = 0;
}


//----------------------------------------------------------------------------//
//
void avlFree_TOT( avlTot *tot )
{
  int i;

  if (tot == NULL) return;

  for (i = 0; i < tot->nCont; i++)
    avl_free(tot->cont[i].name);
  avl_free(tot->cont);

  for (i = 0; i < tot->nDesign; i++)
    avl_free(tot->design[i].name);
  avl_free(tot->design);

  avl_free(tot->name);
  avl_free(tot->title);

  avlInit_TOT( tot );
}

