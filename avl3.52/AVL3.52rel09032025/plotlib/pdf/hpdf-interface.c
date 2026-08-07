/*
 *  XPLOT Libharu PDF interface
 *
 */

#include <stdlib.h>
#include <stdio.h>
#include <string.h>

#include <setjmp.h>
#include "hpdf.h"
#include "hpdf_version.h"

/* Set single or double precision arguments to match plotlib fortran args */

#ifdef  DBL_ARGS
#define FLOATARG     double     
#else
#define FLOATARG     float     
#endif

/* Handle various system requirements for trailing underscores, or other 
   fortran-to-C interface shenanigans thru defines for routine names  
   The provided set gives the option of setting a compile flag -DUNDERSCORE
   to include underscores on C routine name symbols */

#ifdef UNDERSCORE

#define MSKBITS                   mskbits_
#define PDF_INIT                  pdf_init_		   
#define PDF_PAGE		  pdf_page_		  
#define PDF_MOVETO		  pdf_moveto_		  
#define PDF_LINETO		  pdf_lineto_		  
#define PDF_SETLINEWIDTH	  pdf_setlinewidth_	  
#define PDF_SETDASH		  pdf_setdash_		  
#define PDF_SETDASH0		  pdf_setdash0_		  
#define PDF_SETCOLOR		  pdf_setcolor_		  
#define PDF_SETGRAY		  pdf_setgray_		  
#define PDF_STROKE		  pdf_stroke_		  
#define PDF_FILLSTROKE		  pdf_fillstroke_		  
#define PDF_CLOSEPATH		  pdf_closepath_		  
#define PDF_CLOSEPATHSTROKE	  pdf_closepathstroke_	  
#define PDF_CLOSEPATHFILLSTROKE	  pdf_closepathfillstroke_  
#define PDF_GETCURRENTPOS	  pdf_getcurrentpos_
#define PDF_SAVEFILE              pdf_savefile_             
#define PDF_ENABLED               pdf_enabled_             

#else

#define MSKBITS                   mskbits
#define PDF_INIT                  pdf_init		   
#define PDF_PAGE		  pdf_page		  
#define PDF_MOVETO		  pdf_moveto		  
#define PDF_LINETO		  pdf_lineto		  
#define PDF_SETLINEWIDTH	  pdf_setlinewidth	  
#define PDF_SETDASH		  pdf_setdash		  
#define PDF_SETDASH0		  pdf_setdash0		  
#define PDF_SETCOLOR		  pdf_setcolor		  
#define PDF_SETGRAY		  pdf_setgray		  
#define PDF_STROKE		  pdf_stroke		  
#define PDF_FILLSTROKE		  pdf_fillstroke		  
#define PDF_CLOSEPATH		  pdf_closepath		  
#define PDF_CLOSEPATHSTROKE	  pdf_closepathstroke	  
#define PDF_CLOSEPATHFILLSTROKE	  pdf_closepathfillstroke  
#define PDF_GETCURRENTPOS	  pdf_getcurrentpos
#define PDF_SAVEFILE              pdf_savefile             
#define PDF_ENABLED               pdf_enabled

#endif


jmp_buf env;

#ifdef HPDF_DLL
void  __stdcall
#else
void
#endif
error_handler  (HPDF_STATUS   error_no,
                HPDF_STATUS   detail_no,
                void         *user_data)
{
    printf ("ERROR: error_no=%04X, detail_no=%u\n", (HPDF_UINT)error_no,
                (HPDF_UINT)detail_no);
    longjmp(env, 1);
}

/* global variables */
    HPDF_Doc  pdf;
    HPDF_Page page;


int
PDF_INIT  ()
{
    pdf = HPDF_New (error_handler, NULL);
    if (!pdf) {
        printf ("error: cannot create PdfDoc object\n");
        return 1;
    }

    if (setjmp(env)) {
        HPDF_Free (pdf);
        return 1;
    }
#ifdef DEBUG
    printf("Built  with library HPDF_VERSION %s\n", HPDF_VERSION_TEXT);
    printf("Linked with library HPDF_VERSION %s\n", HPDF_GetVersion());
#endif
    return 1;
}


void
PDF_PAGE  (int   *orient)
{
#ifdef DEBUG
    printf(" PDF_PAGE orient %d\n",*orient);
#endif

    /* add a new page object. */
    page = HPDF_AddPage (pdf);
    
    /* set Letter page, Portrait or Landscape */
    if (*orient == 0)
      HPDF_Page_SetSize( page, HPDF_PAGE_SIZE_LETTER, HPDF_PAGE_PORTRAIT );
    else
      HPDF_Page_SetSize( page, HPDF_PAGE_SIZE_LETTER, HPDF_PAGE_LANDSCAPE );

    /* set line properties */
    HPDF_Page_SetLineCap  ( page,HPDF_ROUND_END );
    HPDF_Page_SetLineJoin ( page, HPDF_ROUND_JOIN);

#ifdef DEBUG
    printf(" Page size:  %f %f\n",HPDF_Page_GetWidth(page),HPDF_Page_GetHeight(page));
#endif
}


void
PDF_MOVETO  (FLOATARG      *x,
             FLOATARG      *y)
{
  HPDF_REAL   rx, ry;
#ifdef DEBUG
  printf(" PDF_MOVETO x %f y %f\n",*x,*y);
#endif
    rx = *x;
    ry = *y;
    HPDF_Page_MoveTo (page, rx, ry);
}

void
PDF_LINETO  (FLOATARG      *x,
             FLOATARG      *y)
{
  HPDF_REAL   rx, ry;
#ifdef DEBUG
  printf(" PDF_LINETO x %f y %f\n",*x,*y);
#endif
    rx = *x;
    ry = *y;
    HPDF_Page_LineTo (page, rx, ry);
}

void
PDF_SETLINEWIDTH  (FLOATARG   *w)
{
  HPDF_REAL   width;
#ifdef DEBUG
  printf(" PDF_SETLINEWIDTH %f\n",*w);
#endif 
   width = *w;
   HPDF_Page_SetLineWidth (page, width);
}

void
PDF_SETDASH  (int   *ptn,
              int   *num )
{
#if (HPDF_VERSION_ID < 20400)
    HPDF_UINT16  p[8], phase=0;
    //    printf("HPDF_UINT16\n");
#else
    HPDF_REAL    p[8], phase=0.0;
    //    printf("HPDF_REAL\n");
#endif
    HPDF_UINT    n;

#ifdef DEBUG
    printf(" PDF_SETDASH num %d ptn ",*num);
    for (int i = 0; i<*num; i++) { printf("%d ",ptn[i]); }
    printf("\n");
#endif

    n = *num;
    for (int i = 0; i<n; i++) { p[i] = ptn[i]; }
  
    if (n == 0) 
      HPDF_Page_SetDash (page, NULL, 0, 0);  
    else 
      HPDF_Page_SetDash (page, p, n, 0);
}

void
PDF_SETCOLOR  (FLOATARG   *r,
	       FLOATARG   *g,
	       FLOATARG   *b)
{
    HPDF_REAL   rd, gr, bl;
#ifdef DEBUG
  printf(" PDF_SETCOLOR %f %f %f\n",*r,*g,*b);
#endif
    rd = *r;
    gr = *g;
    bl = *b;
    HPDF_Page_SetRGBStroke (page, rd, gr, bl);
    HPDF_Page_SetRGBFill (page, rd, gr, bl);
}

void
PDF_SETGRAY  (FLOATARG   *gray)
{
    HPDF_REAL   gr;
#ifdef DEBUG
  printf(" PDF_SETGRAY %f\n",*gray);
#endif
    gr = *gray;
    HPDF_Page_SetGrayStroke (page, gr);
    HPDF_Page_SetGrayFill (page, gr);
}

void
PDF_STROKE ()
{
#ifdef DEBUG
  printf(" PDF_STROKE\n");
#endif
    HPDF_Page_Stroke (page);
}

void
PDF_FILLSTROKE ()
{
#ifdef DEBUG
  printf(" PDF_FILLSTROKE\n");
#endif
    HPDF_Page_FillStroke (page);
}

void
PDF_CLOSEPATH ()
{
#ifdef DEBUG
  printf(" PDF_CLOSEPATH\n");
#endif
    HPDF_Page_ClosePath (page);
}

void
PDF_CLOSEPATHSTROKE ()
{
#ifdef DEBUG
  printf(" PDF_CLOSEPATHSTROKE\n");
#endif
    HPDF_Page_ClosePathStroke (page);
}

void
PDF_CLOSEPATHFILLSTROKE ()
{
#ifdef DEBUG
  printf(" PDF_CLOSEPATHFILLSTROKE\n");
#endif
    HPDF_Page_ClosePathFillStroke (page);
}

void
PDF_GETCURRENTPOS  (FLOATARG      *x,
                    FLOATARG      *y)
{
    HPDF_Point  pos;
    pos = HPDF_Page_GetCurrentPos (page);
    *x = pos.x;
    *y = pos.y;
#ifdef DEBUG
    printf("CurrentPos %f %f \n",*x,*y); 
#endif
}

void
PDF_SAVEFILE ( char  *fname,
  	       int    nchar )
{
#ifdef DEBUG
    printf("SaveToFile %s \n", fname); 
#endif
    /* save the pdf to a file */
    HPDF_SaveToFile (pdf, fname);
    /* clean up */
    HPDF_Free (pdf);
}

/*  function to query PDF is available */
void
PDF_ENABLED (int *istat )
{
  *istat = 1;
}
