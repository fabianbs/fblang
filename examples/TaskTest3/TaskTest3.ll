; ModuleID = 'TaskTest3'
source_filename = "TaskTest3"

%"printUpToAsync:coroutineFrameTy" = type { i32, i8*, i8*, i32, { i64, [0 x %":Task"*] }*, i64, i64, i32, i64, i64, i32 }
%":Task" = type opaque
%string = type { i8*, i64 }

define void @main() local_unnamed_addr {
entry:
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %0 = tail call i8* @gc_new(i64 80)
  %1 = bitcast i8* %0 to i32*
  store i32 0, i32* %1, align 4
  %2 = getelementptr i8, i8* %0, i64 16
  %3 = bitcast i8* %2 to i8**
  store i8* null, i8** %3, align 8
  %4 = getelementptr i8, i8* %0, i64 24
  %5 = bitcast i8* %4 to i32*
  store i32 20, i32* %5, align 4
  %6 = getelementptr i8, i8* %0, i64 8
  %7 = bitcast i8* %6 to i8**
  %8 = tail call i1 @runImmediatelyTask(i1 (i8*)* bitcast (i1 (%"printUpToAsync:coroutineFrameTy"*)* @":coro:printUpToAsync" to i1 (i8*)*), i8* nonnull %0, i8** %7)
  %9 = load i8*, i8** %7, align 8
  tail call void @waitTask(i8* %9)
  ret void
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #0

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #0

declare void @waitTask(i8* nocapture) local_unnamed_addr

; Function Attrs: norecurse
declare noalias nonnull i8* @gc_new(i64) local_unnamed_addr #1

define internal i1 @":coro:printUpToAsync"(%"printUpToAsync:coroutineFrameTy"* nocapture %coro_frame_raw) {
entry:
  %0 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 0
  %1 = load i32, i32* %0, align 4
  switch i32 %1, label %fallThrough [
    i32 0, label %resumePoint0
    i32 1, label %entry.loopCond3_crit_edge
  ]

entry.loopCond3_crit_edge:                        ; preds = %entry
  %.pre9 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 8
  br label %loopCond3

resumePoint0:                                     ; preds = %entry
  %2 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 3
  %3 = load i32, i32* %2, align 4
  %4 = shl i32 %3, 3
  %5 = add i32 %4, 8
  %6 = sext i32 %5 to i64
  %7 = tail call i8* @gc_new(i64 %6)
  %8 = bitcast i8* %7 to i64*
  %9 = sext i32 %3 to i64
  store i64 %9, i64* %8, align 4
  %10 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 4
  %11 = bitcast { i64, [0 x %":Task"*] }** %10 to i8**
  store i8* %7, i8** %11, align 8
  %12 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 7
  store i32 0, i32* %12, align 4
  %13 = load i32, i32* %2, align 4
  %14 = zext i32 %13 to i64
  %15 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 5
  store i64 0, i64* %15, align 4
  %16 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 6
  store i64 %14, i64* %16, align 4
  %17 = icmp eq i32 %13, 0
  br i1 %17, label %loopEnd, label %loop.preheader

loop.preheader:                                   ; preds = %resumePoint0
  %18 = bitcast i8* %7 to { i64, [0 x %":Task"*] }*
  br label %loop

fallThrough:                                      ; preds = %loopEnd, %loopCond3, %entry
  ret i1 true

loop:                                             ; preds = %loop.loop_crit_edge, %loop.preheader
  %19 = phi i8* [ %7, %loop.preheader ], [ %35, %loop.loop_crit_edge ]
  %20 = phi { i64, [0 x %":Task"*] }* [ %18, %loop.preheader ], [ %.pre, %loop.loop_crit_edge ]
  %.off011 = phi i32 [ 0, %loop.preheader ], [ %extract.t12, %loop.loop_crit_edge ]
  store i32 %.off011, i32* %12, align 4
  %21 = getelementptr { i64, [0 x %":Task"*] }, { i64, [0 x %":Task"*] }* %20, i64 0, i32 0
  tail call void @throwIfNull(i8* %19, i8* null, i64 0)
  %22 = load i64, i64* %21, align 4
  %23 = trunc i64 %22 to i32
  tail call void @throwIfOutOfBounds(i32 %.off011, i32 %23)
  %24 = sext i32 %.off011 to i64
  %25 = getelementptr { i64, [0 x %":Task"*] }, { i64, [0 x %":Task"*] }* %20, i64 0, i32 1, i64 %24
  %26 = tail call i8* @gc_new(i64 4)
  %27 = load i32, i32* %12, align 4
  %28 = bitcast i8* %26 to i32*
  store i32 %27, i32* %28, align 4
  %29 = tail call i8* @runAsync(i1 (i8*)* nonnull @":async", i8* nonnull %26)
  %30 = bitcast %":Task"** %25 to i8**
  store i8* %29, i8** %30, align 8
  %31 = load i64, i64* %15, align 4
  %32 = add i64 %31, 1
  %33 = load i64, i64* %16, align 4
  store i64 %32, i64* %15, align 4
  %34 = icmp ult i64 %32, %33
  br i1 %34, label %loop.loop_crit_edge, label %loopEnd.loopexit

loop.loop_crit_edge:                              ; preds = %loop
  %.pre = load { i64, [0 x %":Task"*] }*, { i64, [0 x %":Task"*] }** %10, align 8
  %35 = bitcast { i64, [0 x %":Task"*] }* %.pre to i8*
  %extract.t12 = trunc i64 %32 to i32
  br label %loop

loopEnd.loopexit:                                 ; preds = %loop
  %.pre4 = load i32, i32* %2, align 4
  br label %loopEnd

loopEnd:                                          ; preds = %loopEnd.loopexit, %resumePoint0
  %36 = phi i32 [ %.pre4, %loopEnd.loopexit ], [ 0, %resumePoint0 ]
  %37 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 10
  store i32 0, i32* %37, align 4
  %38 = zext i32 %36 to i64
  %39 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 8
  store i64 0, i64* %39, align 4
  %40 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 9
  store i64 %38, i64* %40, align 4
  %41 = icmp eq i32 %36, 0
  br i1 %41, label %fallThrough, label %loop1

loop1:                                            ; preds = %loopCond3.loop1_crit_edge, %loopEnd
  %.pre-phi8 = phi { i64, [0 x %":Task"*] }** [ %.pre7, %loopCond3.loop1_crit_edge ], [ %10, %loopEnd ]
  %.pre-phi6 = phi i32* [ %.pre5, %loopCond3.loop1_crit_edge ], [ %37, %loopEnd ]
  %.pre-phi = phi i64* [ %.pre-phi10, %loopCond3.loop1_crit_edge ], [ %39, %loopEnd ]
  %.off0 = phi i32 [ %extract.t, %loopCond3.loop1_crit_edge ], [ 0, %loopEnd ]
  store i32 %.off0, i32* %.pre-phi6, align 4
  %42 = load { i64, [0 x %":Task"*] }*, { i64, [0 x %":Task"*] }** %.pre-phi8, align 8
  %43 = getelementptr { i64, [0 x %":Task"*] }, { i64, [0 x %":Task"*] }* %42, i64 0, i32 0
  %44 = bitcast { i64, [0 x %":Task"*] }* %42 to i8*
  tail call void @throwIfNull(i8* %44, i8* null, i64 0)
  %45 = load i64, i64* %43, align 4
  %46 = trunc i64 %45 to i32
  tail call void @throwIfOutOfBounds(i32 %.off0, i32 %46)
  %47 = sext i32 %.off0 to i64
  %48 = getelementptr { i64, [0 x %":Task"*] }, { i64, [0 x %":Task"*] }* %42, i64 0, i32 1, i64 %47
  %49 = bitcast %":Task"** %48 to i8**
  %50 = load i8*, i8** %49, align 8
  %51 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 1
  %52 = load i8*, i8** %51, align 8
  %53 = tail call i1 @taskAwaiterEnqueue(i8* %50, i8* %52)
  br i1 %53, label %doSuspend, label %loopCond3

loopCond3:                                        ; preds = %entry.loopCond3_crit_edge, %loop1
  %.pre-phi10 = phi i64* [ %.pre9, %entry.loopCond3_crit_edge ], [ %.pre-phi, %loop1 ]
  %54 = load i64, i64* %.pre-phi10, align 4
  %55 = add i64 %54, 1
  %56 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 9
  %57 = load i64, i64* %56, align 4
  store i64 %55, i64* %.pre-phi10, align 4
  %58 = icmp ult i64 %55, %57
  br i1 %58, label %loopCond3.loop1_crit_edge, label %fallThrough

loopCond3.loop1_crit_edge:                        ; preds = %loopCond3
  %.pre5 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 10
  %.pre7 = getelementptr %"printUpToAsync:coroutineFrameTy", %"printUpToAsync:coroutineFrameTy"* %coro_frame_raw, i64 0, i32 4
  %extract.t = trunc i64 %55 to i32
  br label %loop1

doSuspend:                                        ; preds = %loop1
  store i32 1, i32* %0, align 4
  ret i1 false
}

declare i1 @runImmediatelyTask(i1 (i8*)* readonly, i8*, i8** writeonly) local_unnamed_addr

; Function Attrs: uwtable
declare void @throwIfNull(i8* nocapture readnone, i8* readonly, i64) local_unnamed_addr #2

; Function Attrs: uwtable
declare void @throwIfOutOfBounds(i32, i32) local_unnamed_addr #2

define internal i1 @":async"(i8* nocapture readonly) {
entry:
  %tmp = alloca <2 x i64>, align 16
  %tmpcast = bitcast <2 x i64>* %tmp to %string*
  %1 = bitcast i8* %0 to i32*
  %2 = load i32, i32* %1, align 4
  call void @uto_str(i32 %2, %string* nonnull %tmpcast)
  %3 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load1 = extractelement <2 x i64> %3, i32 0
  %4 = inttoptr i64 %.fca.0.load1 to i8*
  %.fca.1.load2 = extractelement <2 x i64> %3, i32 1
  tail call void @cprintln(i8* %4, i64 %.fca.1.load2)
  ret i1 true
}

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

declare void @uto_str(i32, %string* nocapture writeonly) local_unnamed_addr

declare noalias i8* @runAsync(i1 (i8*)* readonly, i8*) local_unnamed_addr

declare i1 @taskAwaiterEnqueue(i8* nocapture, i8*) local_unnamed_addr

attributes #0 = { inaccessiblememonly nounwind }
attributes #1 = { norecurse }
attributes #2 = { uwtable }
