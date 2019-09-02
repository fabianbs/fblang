; ModuleID = 'TestVectorReduce'
source_filename = "TestVectorReduce"

@0 = private unnamed_addr constant [12 x i8] c"The sum is \00"
@1 = private unnamed_addr constant [4 x i8] c"465\00"

define void @main() local_unnamed_addr {
loop.i.preheader:
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  tail call void @cprint(i8* getelementptr inbounds ([12 x i8], [12 x i8]* @0, i64 0, i64 0), i64 11)
  tail call void @cprintln(i8* getelementptr inbounds ([4 x i8], [4 x i8]* @1, i64 0, i64 0), i64 3)
  ret void
}

; Function Attrs: norecurse nounwind readonly
define i32 @"_Z12vecReduceAdd:int[]->i"({ i64, [0 x i32] }* nocapture readonly %arr) local_unnamed_addr #0 {
entry:
  %0 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %arr, i64 0, i32 0
  %1 = load i64, i64* %0, align 4
  %2 = icmp eq i64 %1, 0
  br i1 %2, label %loopEnd, label %loop.preheader

loop.preheader:                                   ; preds = %entry
  %min.iters.check = icmp ult i64 %1, 8
  br i1 %min.iters.check, label %loop.preheader74, label %vector.ph

vector.ph:                                        ; preds = %loop.preheader
  %n.vec = and i64 %1, -8
  br label %vector.body

vector.body:                                      ; preds = %vector.body, %vector.ph
  %index = phi i64 [ 0, %vector.ph ], [ %index.next, %vector.body ]
  %vec.phi = phi <8 x i32> [ zeroinitializer, %vector.ph ], [ %5, %vector.body ]
  %3 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %arr, i64 0, i32 1, i64 %index
  %4 = bitcast i32* %3 to <8 x i32>*
  %wide.load = load <8 x i32>, <8 x i32>* %4, align 4
  %5 = add <8 x i32> %wide.load, %vec.phi
  %index.next = add i64 %index, 8
  %6 = icmp eq i64 %index.next, %n.vec
  br i1 %6, label %middle.block, label %vector.body, !llvm.loop !0

middle.block:                                     ; preds = %vector.body
  %.lcssa77 = phi <8 x i32> [ %5, %vector.body ]
  %rdx.shuf = shufflevector <8 x i32> %.lcssa77, <8 x i32> undef, <8 x i32> <i32 4, i32 5, i32 6, i32 7, i32 undef, i32 undef, i32 undef, i32 undef>
  %bin.rdx = add <8 x i32> %rdx.shuf, %.lcssa77
  %rdx.shuf6 = shufflevector <8 x i32> %bin.rdx, <8 x i32> undef, <8 x i32> <i32 2, i32 3, i32 undef, i32 undef, i32 undef, i32 undef, i32 undef, i32 undef>
  %bin.rdx7 = add <8 x i32> %rdx.shuf6, %bin.rdx
  %rdx.shuf8 = shufflevector <8 x i32> %bin.rdx7, <8 x i32> undef, <8 x i32> <i32 1, i32 undef, i32 undef, i32 undef, i32 undef, i32 undef, i32 undef, i32 undef>
  %bin.rdx9 = add <8 x i32> %rdx.shuf8, %bin.rdx7
  %7 = extractelement <8 x i32> %bin.rdx9, i32 0
  %cmp.n = icmp eq i64 %1, %n.vec
  br i1 %cmp.n, label %loopEnd, label %loop.preheader74

loop.preheader74:                                 ; preds = %middle.block, %loop.preheader
  %res.0.ph = phi i32 [ 0, %loop.preheader ], [ %7, %middle.block ]
  %i_tmp.0.ph = phi i64 [ 0, %loop.preheader ], [ %n.vec, %middle.block ]
  br label %loop

loop:                                             ; preds = %loop.preheader74, %loop
  %res.0 = phi i32 [ %10, %loop ], [ %res.0.ph, %loop.preheader74 ]
  %i_tmp.0 = phi i64 [ %11, %loop ], [ %i_tmp.0.ph, %loop.preheader74 ]
  %8 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %arr, i64 0, i32 1, i64 %i_tmp.0
  %9 = load i32, i32* %8, align 4
  %10 = add i32 %9, %res.0
  %11 = add nuw i64 %i_tmp.0, 1
  %12 = icmp ugt i64 %1, %11
  br i1 %12, label %loop, label %loopEnd, !llvm.loop !4

loopEnd:                                          ; preds = %loop, %middle.block, %entry
  %res.1 = phi i32 [ 0, %entry ], [ %7, %middle.block ], [ %10, %loop ]
  ret i32 %res.1
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #1

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #1

declare void @cprint(i8* nocapture readonly, i64) local_unnamed_addr

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

attributes #0 = { norecurse nounwind readonly }
attributes #1 = { inaccessiblememonly nounwind }

!0 = distinct !{!0, !1, !2, !3}
!1 = !{!"llvm.loop.vectorize.enable", i32 1}
!2 = !{!"llvm.loop.vectorize.width", i32 8}
!3 = !{!"llvm.loop.isvectorized", i32 1}
!4 = distinct !{!4, !1, !2, !5, !3}
!5 = !{!"llvm.loop.unroll.runtime.disable"}
