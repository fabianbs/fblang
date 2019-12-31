; ModuleID = 'ByRefForTest2'
source_filename = "ByRefForTest2.fbs"

%"iota:coroutineFrameTy" = type { i32, i32 }
%string = type { i8*, i64 }

@"iota:iterator.vtable_vtable" = local_unnamed_addr constant { i1 (%"iota:coroutineFrameTy"*, i32*)* } { i1 (%"iota:coroutineFrameTy"*, i32*)* @"iota:iterator.tryGetNext" }, !type !0

define void @main() local_unnamed_addr {
entry:
  %tmp = alloca %string, align 8
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %gc_new2425 = alloca [56 x i8], align 8
  %0 = bitcast [56 x i8]* %gc_new2425 to { i64, [0 x i32] }*
  %1 = bitcast [56 x i8]* %gc_new2425 to i64*
  %2 = getelementptr inbounds [56 x i8], [56 x i8]* %gc_new2425, i64 0, i64 8
  call void @llvm.memset.p0i8.i64(i8* nonnull align 8 %2, i8 0, i64 48, i1 false)
  store i64 12, i64* %1, align 8
  br label %loop

loop:                                             ; preds = %entry, %"iota:iterator.tryGetNext.exit"
  %gc_new81.sroa.12.0 = phi i8 [ 0, %entry ], [ %gc_new81.sroa.12.1, %"iota:iterator.tryGetNext.exit" ]
  %gc_new81.sroa.11.0 = phi i8 [ 0, %entry ], [ %gc_new81.sroa.11.1, %"iota:iterator.tryGetNext.exit" ]
  %gc_new81.sroa.10.0 = phi i8 [ 0, %entry ], [ %gc_new81.sroa.10.1, %"iota:iterator.tryGetNext.exit" ]
  %gc_new81.sroa.6.0 = phi i8 [ 0, %entry ], [ %gc_new81.sroa.6.1, %"iota:iterator.tryGetNext.exit" ]
  %3 = phi i64 [ 12, %entry ], [ %8, %"iota:iterator.tryGetNext.exit" ]
  %4 = phi i32 [ 0, %entry ], [ %9, %"iota:iterator.tryGetNext.exit" ]
  %i_tmp.0 = phi i64 [ 0, %entry ], [ %10, %"iota:iterator.tryGetNext.exit" ]
  %5 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %i_tmp.0
  switch i32 %4, label %"iota:iterator.tryGetNext.exit" [
    i32 0, label %loop.i
    i32 1, label %entry.loop_crit_edge.i
  ]

entry.loop_crit_edge.i:                           ; preds = %loop
  %gc_new81.sroa.12.4.insert.ext = zext i8 %gc_new81.sroa.12.0 to i32
  %gc_new81.sroa.12.4.insert.shift = shl nuw i32 %gc_new81.sroa.12.4.insert.ext, 24
  %gc_new81.sroa.11.4.insert.ext = zext i8 %gc_new81.sroa.11.0 to i32
  %gc_new81.sroa.11.4.insert.shift = shl nuw nsw i32 %gc_new81.sroa.11.4.insert.ext, 16
  %gc_new81.sroa.11.4.insert.insert = or i32 %gc_new81.sroa.11.4.insert.shift, %gc_new81.sroa.12.4.insert.shift
  %gc_new81.sroa.10.4.insert.ext = zext i8 %gc_new81.sroa.10.0 to i32
  %gc_new81.sroa.10.4.insert.shift = shl nuw nsw i32 %gc_new81.sroa.10.4.insert.ext, 8
  %gc_new81.sroa.10.4.insert.insert = or i32 %gc_new81.sroa.11.4.insert.insert, %gc_new81.sroa.10.4.insert.shift
  %gc_new81.sroa.6.4.insert.ext = zext i8 %gc_new81.sroa.6.0 to i32
  %gc_new81.sroa.6.4.insert.insert = or i32 %gc_new81.sroa.10.4.insert.insert, %gc_new81.sroa.6.4.insert.ext
  br label %loop.i

loop.i:                                           ; preds = %loop, %entry.loop_crit_edge.i
  %6 = phi i32 [ %gc_new81.sroa.6.4.insert.insert, %entry.loop_crit_edge.i ], [ %4, %loop ]
  %7 = add i32 %6, 1
  %gc_new81.sroa.6.4.extract.trunc = trunc i32 %7 to i8
  %gc_new81.sroa.10.4.extract.shift = lshr i32 %7, 8
  %gc_new81.sroa.10.4.extract.trunc = trunc i32 %gc_new81.sroa.10.4.extract.shift to i8
  %gc_new81.sroa.11.4.extract.shift = lshr i32 %7, 16
  %gc_new81.sroa.11.4.extract.trunc = trunc i32 %gc_new81.sroa.11.4.extract.shift to i8
  %gc_new81.sroa.12.4.extract.shift = lshr i32 %7, 24
  %gc_new81.sroa.12.4.extract.trunc = trunc i32 %gc_new81.sroa.12.4.extract.shift to i8
  store i32 %6, i32* %5, align 4
  %.pre = load i64, i64* %1, align 8
  br label %"iota:iterator.tryGetNext.exit"

"iota:iterator.tryGetNext.exit":                  ; preds = %loop, %loop.i
  %gc_new81.sroa.12.1 = phi i8 [ %gc_new81.sroa.12.0, %loop ], [ %gc_new81.sroa.12.4.extract.trunc, %loop.i ]
  %gc_new81.sroa.11.1 = phi i8 [ %gc_new81.sroa.11.0, %loop ], [ %gc_new81.sroa.11.4.extract.trunc, %loop.i ]
  %gc_new81.sroa.10.1 = phi i8 [ %gc_new81.sroa.10.0, %loop ], [ %gc_new81.sroa.10.4.extract.trunc, %loop.i ]
  %gc_new81.sroa.6.1 = phi i8 [ %gc_new81.sroa.6.0, %loop ], [ %gc_new81.sroa.6.4.extract.trunc, %loop.i ]
  %8 = phi i64 [ %3, %loop ], [ %.pre, %loop.i ]
  %9 = phi i32 [ %4, %loop ], [ 1, %loop.i ]
  %10 = add i64 %i_tmp.0, 1
  %11 = icmp ugt i64 %8, %10
  br i1 %11, label %loop, label %loopEnd

loopEnd:                                          ; preds = %"iota:iterator.tryGetNext.exit"
  %12 = icmp eq i64 %8, 0
  br i1 %12, label %loopEnd5, label %loop4.preheader

loop4.preheader:                                  ; preds = %loopEnd
  %.fca.0.gep = getelementptr inbounds %string, %string* %tmp, i64 0, i32 0
  %.fca.1.gep = getelementptr inbounds %string, %string* %tmp, i64 0, i32 1
  %13 = add i64 %8, -1
  %xtraiter = and i64 %8, 7
  %14 = icmp ult i64 %13, 7
  br i1 %14, label %loopEnd5.loopexit.unr-lcssa, label %loop4.preheader.new

loop4.preheader.new:                              ; preds = %loop4.preheader
  %unroll_iter = sub i64 %8, %xtraiter
  br label %loop4

loop4:                                            ; preds = %loop4, %loop4.preheader.new
  %i_tmp2.0 = phi i64 [ 0, %loop4.preheader.new ], [ %38, %loop4 ]
  %niter = phi i64 [ %unroll_iter, %loop4.preheader.new ], [ %niter.nsub.7, %loop4 ]
  %15 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %i_tmp2.0
  %16 = load i32, i32* %15, align 8
  call void @to_str(i32 %16, %string* nonnull %tmp)
  %.fca.0.load = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load, i64 %.fca.1.load)
  %17 = or i64 %i_tmp2.0, 1
  %18 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %17
  %19 = load i32, i32* %18, align 4
  call void @to_str(i32 %19, %string* nonnull %tmp)
  %.fca.0.load.1 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.1 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.1, i64 %.fca.1.load.1)
  %20 = or i64 %i_tmp2.0, 2
  %21 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %20
  %22 = load i32, i32* %21, align 8
  call void @to_str(i32 %22, %string* nonnull %tmp)
  %.fca.0.load.2 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.2 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.2, i64 %.fca.1.load.2)
  %23 = or i64 %i_tmp2.0, 3
  %24 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %23
  %25 = load i32, i32* %24, align 4
  call void @to_str(i32 %25, %string* nonnull %tmp)
  %.fca.0.load.3 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.3 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.3, i64 %.fca.1.load.3)
  %26 = or i64 %i_tmp2.0, 4
  %27 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %26
  %28 = load i32, i32* %27, align 8
  call void @to_str(i32 %28, %string* nonnull %tmp)
  %.fca.0.load.4 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.4 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.4, i64 %.fca.1.load.4)
  %29 = or i64 %i_tmp2.0, 5
  %30 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %29
  %31 = load i32, i32* %30, align 4
  call void @to_str(i32 %31, %string* nonnull %tmp)
  %.fca.0.load.5 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.5 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.5, i64 %.fca.1.load.5)
  %32 = or i64 %i_tmp2.0, 6
  %33 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %32
  %34 = load i32, i32* %33, align 8
  call void @to_str(i32 %34, %string* nonnull %tmp)
  %.fca.0.load.6 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.6 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.6, i64 %.fca.1.load.6)
  %35 = or i64 %i_tmp2.0, 7
  %36 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %35
  %37 = load i32, i32* %36, align 4
  call void @to_str(i32 %37, %string* nonnull %tmp)
  %.fca.0.load.7 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.7 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.7, i64 %.fca.1.load.7)
  %38 = add i64 %i_tmp2.0, 8
  %niter.nsub.7 = add i64 %niter, -8
  %niter.ncmp.7 = icmp eq i64 %niter.nsub.7, 0
  br i1 %niter.ncmp.7, label %loopEnd5.loopexit.unr-lcssa, label %loop4

loopEnd5.loopexit.unr-lcssa:                      ; preds = %loop4, %loop4.preheader
  %i_tmp2.0.unr = phi i64 [ 0, %loop4.preheader ], [ %38, %loop4 ]
  %lcmp.mod = icmp eq i64 %xtraiter, 0
  br i1 %lcmp.mod, label %loopEnd5, label %loop4.epil

loop4.epil:                                       ; preds = %loopEnd5.loopexit.unr-lcssa, %loop4.epil
  %i_tmp2.0.epil = phi i64 [ %41, %loop4.epil ], [ %i_tmp2.0.unr, %loopEnd5.loopexit.unr-lcssa ]
  %epil.iter = phi i64 [ %epil.iter.sub, %loop4.epil ], [ %xtraiter, %loopEnd5.loopexit.unr-lcssa ]
  %39 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %0, i64 0, i32 1, i64 %i_tmp2.0.epil
  %40 = load i32, i32* %39, align 4
  call void @to_str(i32 %40, %string* nonnull %tmp)
  %.fca.0.load.epil = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load.epil = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load.epil, i64 %.fca.1.load.epil)
  %41 = add nuw i64 %i_tmp2.0.epil, 1
  %epil.iter.sub = add nsw i64 %epil.iter, -1
  %epil.iter.cmp = icmp eq i64 %epil.iter.sub, 0
  br i1 %epil.iter.cmp, label %loopEnd5, label %loop4.epil, !llvm.loop !1

loopEnd5:                                         ; preds = %loop4.epil, %loopEnd5.loopexit.unr-lcssa, %loopEnd
  ret void
}

; Function Attrs: norecurse nounwind
define internal i1 @"iota:iterator.tryGetNext"(%"iota:coroutineFrameTy"* nocapture %coro_frame, i32* nocapture %ret) #0 {
entry:
  %0 = getelementptr %"iota:coroutineFrameTy", %"iota:coroutineFrameTy"* %coro_frame, i64 0, i32 0
  %1 = load i32, i32* %0, align 4
  switch i32 %1, label %fallThrough [
    i32 0, label %resumePoint0
    i32 1, label %entry.loop_crit_edge
  ]

entry.loop_crit_edge:                             ; preds = %entry
  %.phi.trans.insert = getelementptr %"iota:coroutineFrameTy", %"iota:coroutineFrameTy"* %coro_frame, i64 0, i32 1
  %.pre = load i32, i32* %.phi.trans.insert, align 4
  br label %loop

resumePoint0:                                     ; preds = %entry
  %2 = getelementptr %"iota:coroutineFrameTy", %"iota:coroutineFrameTy"* %coro_frame, i64 0, i32 1
  store i32 0, i32* %2, align 4
  br label %loop

fallThrough:                                      ; preds = %entry
  ret i1 false

loop:                                             ; preds = %entry.loop_crit_edge, %resumePoint0
  %.pre-phi = phi i32* [ %.phi.trans.insert, %entry.loop_crit_edge ], [ %2, %resumePoint0 ]
  %3 = phi i32 [ %.pre, %entry.loop_crit_edge ], [ 0, %resumePoint0 ]
  %4 = add i32 %3, 1
  store i32 %4, i32* %.pre-phi, align 4
  store i32 %3, i32* %ret, align 4
  store i32 1, i32* %0, align 4
  ret i1 true
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #1

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #1

; Function Attrs: argmemonly norecurse nounwind readnone
define i1 @"iterator int.isInstanceOf"(i8* nocapture nonnull readnone %other) local_unnamed_addr #2 {
entry:
  ret i1 false
}

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

declare void @to_str(i32, %string* nocapture writeonly) local_unnamed_addr

; Function Attrs: argmemonly nounwind
declare void @llvm.memset.p0i8.i64(i8* nocapture writeonly, i8, i64, i1) #3

attributes #0 = { norecurse nounwind }
attributes #1 = { inaccessiblememonly nounwind }
attributes #2 = { argmemonly norecurse nounwind readnone }
attributes #3 = { argmemonly nounwind }

!0 = !{i64 0, !"iota:iterator.vtable"}
!1 = distinct !{!1, !2}
!2 = !{!"llvm.loop.unroll.disable"}
