!=============================================================================80
! Allocates AIC arrays that may be too big for COMMONS
!=============================================================================80
subroutine avlheap_init()

  use avl_heap_inc

! Allocate AIC variable storage

  if (.not. allocated(AICN)) then
    allocate(AICN(NVX,NVX))
    allocate(WC_GAM(3,NVX,NVX))
    allocate(WV_GAM(3,NVX,NVX))
  endif
end subroutine avlheap_init

!=============================================================================80
! Free AIC array storage
!=============================================================================80
subroutine avlheap_clean()

  use avl_heap_inc

! Deallocate heap storage for AIC's 

  if (allocated(AICN)) then
    deallocate(AICN)
    deallocate(WC_GAM)
    deallocate(WV_GAM)
  endif

end subroutine avlheap_clean

