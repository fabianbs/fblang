; ModuleID = 'ByRefForTest'
source_filename = "ByRefForTest.fbs"

%string = type { i8*, i64 }

define void @main() local_unnamed_addr {
entry:
  %tmp = alloca %string, align 8
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %gc_new18 = alloca [56 x i8], align 8
  %0 = bitcast [56 x i8]* %gc_new18 to { i64, [0 x i32] }*
  %1 = bitcast [56 x i8]* %gc_new18 to i64*
  %2 = getelementptr inbounds [56 x i8], [56 x i8]* %gc_new18, i64 0, i64 8
  call void @llvm.memset.p0i8.i64(i8* nonnull align 8 %2, i8 0, i64 48, i1 false)
  store i64 12, i64* %1, align 8
  br label %loop

loop:                                             ; preds = %entry, %loop
  %i_tmp.0 = phi i64 [ 0, %entry ], [ %4, %loop ]
  %3 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %i_tmp.0
  store i32 42, i32* %3, align 4
  %4 = add i64 %i_tmp.0, 1
  %5 = load i64, i64* %1, align 8
  %6 = icmp ugt i64 %5, %4
  br i1 %6, label %loop, label %loopEnd

loopEnd:                                          ; preds = %loop
  %7 = icmp eq i64 %5, 0
  br i1 %7, label %loopEnd4, label %loop3.preheader

loop3.preheader:                                  ; preds = %loopEnd
  %.fca.0.gep = getelementptr inbounds %string, %string* %tmp, i64 0, i32 0
  %.fca.1.gep = getelementptr inbounds %string, %string* %tmp, i64 0, i32 1
  %8 = add i64 %5, -1
  %xtraiter = and i64 %5, 7
  %9 = icmp ult i64 %8, 7
  br i1 %9, label %loopEnd4.loopexit.unr-lcssa, label %loop3.preheader.new

loop3.preheader.new:                              ; preds = %loop3.preheader
  %unroll_iter = sub i64 %5, %xtraiter
  br label %loop3

loop3:                                            ; preds = %loop3, %loop3.preheader.new
  %i_tmp1.0 = phi i64 [ 0, %loop3.preheader.new ], [ %33, %loop3 ]
  %niter = phi i64 [ %unroll_iter, %loop3.preheader.new ], [ %niter.nsub.7, %loop3 ]
  %10 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %i_tmp1.0
  %11 = load i32, i32* %10, align 8
  call void @to_str(i32 %11, %string* nonnull %tmp)
  %.fca.0.load = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load, i64 %.fca.1.load)
  %12 = or i64 %i_tmp1.0, 1
  %13 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %12
  %14 = load i32, i32* %13, align 4
  call void @to_str(i32 %14, %string* nonnull %tmp)
  %.fca.0.load.1 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.1 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.1, i64 %.fca.1.load.1)
  %15 = or i64 %i_tmp1.0, 2
  %16 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %15
  %17 = load i32, i32* %16, align 8
  call void @to_str(i32 %17, %string* nonnull %tmp)
  %.fca.0.load.2 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.2 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.2, i64 %.fca.1.load.2)
  %18 = or i64 %i_tmp1.0, 3
  %19 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %18
  %20 = load i32, i32* %19, align 4
  call void @to_str(i32 %20, %string* nonnull %tmp)
  %.fca.0.load.3 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.3 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.3, i64 %.fca.1.load.3)
  %21 = or i64 %i_tmp1.0, 4
  %22 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %21
  %23 = load i32, i32* %22, align 8
  call void @to_str(i32 %23, %string* nonnull %tmp)
  %.fca.0.load.4 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.4 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.4, i64 %.fca.1.load.4)
  %24 = or i64 %i_tmp1.0, 5
  %25 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %24
  %26 = load i32, i32* %25, align 4
  call void @to_str(i32 %26, %string* nonnull %tmp)
  %.fca.0.load.5 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.5 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.5, i64 %.fca.1.load.5)
  %27 = or i64 %i_tmp1.0, 6
  %28 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %27
  %29 = load i32, i32* %28, align 8
  call void @to_str(i32 %29, %string* nonnull %tmp)
  %.fca.0.load.6 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.6 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.6, i64 %.fca.1.load.6)
  %30 = or i64 %i_tmp1.0, 7
  %31 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %30
  %32 = load i32, i32* %31, align 4
  call void @to_str(i32 %32, %string* nonnull %tmp)
  %.fca.0.load.7 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.7 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.7, i64 %.fca.1.load.7)
  %33 = add i64 %i_tmp1.0, 8
  %niter.nsub.7 = add i64 %niter, -8
  %niter.ncmp.7 = icmp eq i64 %niter.nsub.7, 0
  br i1 %niter.ncmp.7, label %loopEnd4.loopexit.unr-lcssa, label %loop3

loopEnd4.loopexit.unr-lcssa:                      ; preds = %loop3, %loop3.preheader
  %i_tmp1.0.unr = phi i64 [ 0, %loop3.preheader ], [ %33, %loop3 ]
  %lcmp.mod = icmp eq i64 %xtraiter, 0
  br i1 %lcmp.mod, label %loopEnd4, label %loop3.epil

loop3.epil:                                       ; preds = %loopEnd4.loopexit.unr-lcssa, %loop3.epil
  %i_tmp1.0.epil = phi i64 [ %36, %loop3.epil ], [ %i_tmp1.0.unr, %loopEnd4.loopexit.unr-lcssa ]
  %epil.iter = phi i64 [ %epil.iter.sub, %loop3.epil ], [ %xtraiter, %loopEnd4.loopexit.unr-lcssa ]
  %34 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %i_tmp1.0.epil
  %35 = load i32, i32* %34, align 4
  call void @to_str(i32 %35, %string* nonnull %tmp)
  %.fca.0.load.epil = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.epil = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.epil, i64 %.fca.1.load.epil)
  %36 = add nuw i64 %i_tmp1.0.epil, 1
  %epil.iter.sub = add nsw i64 %epil.iter, -1
  %epil.iter.cmp = icmp eq i64 %epil.iter.sub, 0
  br i1 %epil.iter.cmp, label %loopEnd4, label %loop3.epil, !llvm.loop !0

loopEnd4:                                         ; preds = %loop3.epil, %loopEnd4.loopexit.unr-lcssa, %loopEnd
  ret void
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #0

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #0

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

declare void @to_str(i32, %string* nocapture writeonly) local_unnamed_addr

; Function Attrs: argmemonly nounwind
declare void @llvm.memset.p0i8.i64(i8* nocapture writeonly, i8, i64, i1) #1

attributes #0 = { inaccessiblememonly nounwind }
attributes #1 = { argmemonly nounwind }

!0 = distinct !{!0, !1}
!1 = !{!"llvm.loop.unroll.disable"}
