; ModuleID = 'TaskTest4'
source_filename = "TaskTest4"

%":concurrentForLoopBody.frameTy" = type {}
%string = type { i8*, i64 }

define void @main() local_unnamed_addr {
entry:
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %0 = tail call i8* @gc_new(i64 0)
  %1 = tail call i8* @runLoopAsync(i1 (i8*, i64, i64)* bitcast (i1 (%":concurrentForLoopBody.frameTy"*, i64, i64)* @":concurrentForLoopBody" to i1 (i8*, i64, i64)*), i8* nonnull %0, i64 1, i64 25, i64 4)
  tail call void @waitTask(i8* %1)
  ret void
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #0

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #0

define internal i1 @":concurrentForLoopBody"(%":concurrentForLoopBody.frameTy"* nocapture readnone, i64, i64) {
entry:
  %tmp = alloca <2 x i64>, align 16
  %tmpcast = bitcast <2 x i64>* %tmp to %string*
  %3 = icmp eq i64 %2, 0
  br i1 %3, label %loopEnd, label %loop.preheader

loop.preheader:                                   ; preds = %entry
  %4 = trunc i64 %1 to i32
  %5 = add i64 %2, %1
  br label %loop

loop:                                             ; preds = %loop.preheader, %loop
  %i.0 = phi i32 [ %10, %loop ], [ %4, %loop.preheader ]
  call void @uto_str(i32 %i.0, %string* nonnull %tmpcast)
  %6 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load3 = extractelement <2 x i64> %6, i32 0
  %7 = inttoptr i64 %.fca.0.load3 to i8*
  %.fca.1.load4 = extractelement <2 x i64> %6, i32 1
  tail call void @cprintln(i8* %7, i64 %.fca.1.load4)
  %8 = zext i32 %i.0 to i64
  %9 = add nuw nsw i64 %8, 1
  %10 = trunc i64 %9 to i32
  %11 = icmp ult i64 %9, %5
  br i1 %11, label %loop, label %loopEnd

loopEnd:                                          ; preds = %loop, %entry
  ret i1 true
}

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

declare void @uto_str(i32, %string* nocapture writeonly) local_unnamed_addr

; Function Attrs: norecurse
declare noalias nonnull i8* @gc_new(i64) local_unnamed_addr #1

declare noalias i8* @runLoopAsync(i1 (i8*, i64, i64)* readonly, i8*, i64, i64, i64) local_unnamed_addr

declare void @waitTask(i8* nocapture) local_unnamed_addr

attributes #0 = { inaccessiblememonly nounwind }
attributes #1 = { norecurse }
