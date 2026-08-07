      LOGICAL LPLOT,LGRID,LLAND,LCURS,LSLOPE
      CHARACTER*32 NAME
      
      COMMON/IPLO_C/ NAME
      COMMON/IPLO_X/ IDEV,IDEVRP,IPSLU,NCOLOR,
     &               LPLOT,LGRID,LLAND,LCURS,LSLOPE
      COMMON/RPLO_X/ SIZE,SCRNFR,PAR,CSIZE,
     &               XWIND,YWIND,
     &               XPAGE,YPAGE,XMARG,YMARG,
     &               XYOFF(2),XYFAC(2)
C       
C   SIZE        Plot width in inches
C   SCRNFR      Fraction of screen taken up by graphics window
C   PAR         Plot height/width aspect ratio
C   CSIZE       Character size/plot width ratio
C   XWIND,YWIND Dimensions of X-window
C   XPAGE,YPAGE Dimensions of PostScript output page
C   XMARG,YMARG Margins on plot page
C   XYOFF(.)    x,y plot offsets  for current x-y plot, if any
C   XYFAC(.)    x,y scale factors for current x-y plot, if any
C
C   IDEV        Screen window flag
C   IDEVRP      Replotting flag
C   IPSLU       PostScript output-file flag
C   NCOLOR      Number of colors in spectrum
C
C   LPLOT       T if a plot is active
C   LGRID       T if grid is to be overlaid on x,y plots
C   LLAND       T if plots are in Landscape mode

