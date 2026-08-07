
      subroutine ba2wa_mat(alfa,beta,p,p_a,p_b)
c-------------------------------------------------------
c     calculates body axis to wind axis transformation matrix.
c     Input:    alfa, beta
c     Output:   p(3,3) p_a(3,3) p_b(3,3)
c      
c      X_wa     [       ]  X_ba
c               [       ]  
c      Y_wa  =  [   p   ]  Y_ba
c               [       ]
c      Z_wa     [       ]  Z_ba
c
c-------------------------------------------------------

      real alfa, beta
      real p(3,3), p_a(3,3), p_b(3,3)

      sina = sin(alfa)
      cosa = cos(alfa)

      sinb = sin(beta)
      cosb = cos(beta)

      p(1,1) =  cosa*cosb*binv
      p(1,2) =      -sinb*binv
      p(1,3) =  sina*cosb*binv

      p(2,1) =  cosa*sinb
      p(2,2) =       cosb
      p(2,3) =  sina*sinb

      p(3,1) = -sina
      p(3,2) = 0.
      p(3,3) =  cosa

      p_a(1,1) = -sina*cosb
      p_a(1,2) = 0.
      p_a(1,3) =  cosa*cosb

      p_a(2,1) = -sina*sinb
      p_a(2,2) = 0.
      p_a(2,3) =  cosa*sinb

      p_a(3,1) = -cosa
      p_a(3,2) = 0.
      p_a(3,3) = -sina


      p_b(1,1) = -cosa*sinb
      p_b(1,2) =      -cosb
      p_b(1,3) = -sina*sinb

      p_b(2,1) =  cosa*cosb
      p_b(2,2) =      -sinb
      p_b(2,3) =  sina*cosb

      p_b(3,1) = 0.
      p_b(3,2) = 0.
      p_b(3,3) = 0.

      return
      end ! ba2wa_mat


      subroutine ba2sa_mat(alfa,p,p_a)
c-------------------------------------------------------
c     calculates body axis to stability axis transformation matrix.
c     Input:    alfa
c     Output:   p(3,3) p_a(3,3)
c
c      X_sa     [       ]  X_ba
c               [       ]  
c      Y_sa  =  [   p   ]  Y_ba
c               [       ]
c      Z_sa     [       ]  Z_ba
c
c-------------------------------------------------------

      real alfa 
      real p(3,3), p_a(3,3)

      sina = sin(alfa)
      cosa = cos(alfa)

      p(1,1) =  cosa
      p(1,2) =  0.0
      p(1,3) =  sina

      p(2,1) =  0.0
      p(2,2) =  1.0
      p(2,3) =  0.0

      p(3,1) = -sina
      p(3,2) =  0.0
      p(3,3) =  cosa

      p_a(1,1) = -sina
      p_a(1,2) =  0.0
      p_a(1,3) =  cosa

      p_a(2,1) = 0.0
      p_a(2,2) = 0.0
      p_a(2,3) = 0.0

      p_a(3,1) = -cosa
      p_a(3,2) =  0.0
      p_a(3,3) = -sina

      return
      end ! ba2sa_mat
