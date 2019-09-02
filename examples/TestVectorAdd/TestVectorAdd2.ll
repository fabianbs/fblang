; ModuleID = 'TestVectorAdd2'
source_filename = "TestVectorAdd2"

%string = type { i8*, i64 }

@0 = private unnamed_addr constant [3 x i8] c"[ \00"
@1 = private unnamed_addr constant [2 x i8] c" \00"
@2 = private unnamed_addr constant [2 x i8] c"]\00"

define void @main() local_unnamed_addr {
entry:
  %retPtr.i = alloca <2 x i64>, align 16
  %tmp = alloca <2 x i64>, align 16
  %tmpcast = bitcast <2 x i64>* %tmp to %string*
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %gc_new10 = alloca [132 x i8], align 8
  %gc_new10.repack18 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new10, i64 0, i64 8
  %gc_new10.repack34 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new10, i64 0, i64 24
  %gc_new10.repack50 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new10, i64 0, i64 40
  %gc_new10.repack66 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new10, i64 0, i64 56
  %gc_new10.repack82 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new10, i64 0, i64 72
  %gc_new10.repack98 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new10, i64 0, i64 88
  %gc_new10.repack114 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new10, i64 0, i64 104
  %gc_new10.repack130 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new10, i64 0, i64 120
  %0 = bitcast [132 x i8]* %gc_new10 to { i64, [0 x i32] }*
  %1 = bitcast [132 x i8]* %gc_new10 to i64*
  store i64 31, i64* %1, align 8
  %2 = bitcast i8* %gc_new10.repack18 to <4 x i32>*
  store <4 x i32> <i32 0, i32 1, i32 2, i32 3>, <4 x i32>* %2, align 8
  %3 = bitcast i8* %gc_new10.repack34 to <4 x i32>*
  store <4 x i32> <i32 4, i32 5, i32 6, i32 7>, <4 x i32>* %3, align 8
  %4 = bitcast i8* %gc_new10.repack50 to <4 x i32>*
  store <4 x i32> <i32 8, i32 9, i32 10, i32 11>, <4 x i32>* %4, align 8
  %5 = bitcast i8* %gc_new10.repack66 to <4 x i32>*
  store <4 x i32> <i32 12, i32 13, i32 14, i32 15>, <4 x i32>* %5, align 8
  %6 = bitcast i8* %gc_new10.repack82 to <4 x i32>*
  store <4 x i32> <i32 16, i32 17, i32 18, i32 19>, <4 x i32>* %6, align 8
  %7 = bitcast i8* %gc_new10.repack98 to <4 x i32>*
  store <4 x i32> <i32 20, i32 21, i32 22, i32 23>, <4 x i32>* %7, align 8
  %8 = bitcast i8* %gc_new10.repack114 to <4 x i32>*
  store <4 x i32> <i32 24, i32 25, i32 26, i32 27>, <4 x i32>* %8, align 8
  %9 = bitcast i8* %gc_new10.repack130 to <3 x i32>*
  store <3 x i32> <i32 28, i32 29, i32 30>, <3 x i32>* %9, align 8
  %gc_new9142 = alloca [132 x i8], align 8
  %gc_new9142.repack150 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new9142, i64 0, i64 8
  %gc_new9142.repack166 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new9142, i64 0, i64 24
  %gc_new9142.repack182 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new9142, i64 0, i64 40
  %gc_new9142.repack198 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new9142, i64 0, i64 56
  %gc_new9142.repack214 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new9142, i64 0, i64 72
  %gc_new9142.repack230 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new9142, i64 0, i64 88
  %gc_new9142.repack246 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new9142, i64 0, i64 104
  %gc_new9142.repack262 = getelementptr inbounds [132 x i8], [132 x i8]* %gc_new9142, i64 0, i64 120
  %10 = bitcast [132 x i8]* %gc_new9142 to { i64, [0 x i32] }*
  %11 = bitcast [132 x i8]* %gc_new9142 to i64*
  store i64 31, i64* %11, align 8
  %12 = bitcast i8* %gc_new9142.repack150 to <4 x i32>*
  store <4 x i32> <i32 30, i32 29, i32 28, i32 27>, <4 x i32>* %12, align 8
  %13 = bitcast i8* %gc_new9142.repack166 to <4 x i32>*
  store <4 x i32> <i32 26, i32 25, i32 24, i32 23>, <4 x i32>* %13, align 8
  %14 = bitcast i8* %gc_new9142.repack182 to <4 x i32>*
  store <4 x i32> <i32 22, i32 21, i32 20, i32 19>, <4 x i32>* %14, align 8
  %15 = bitcast i8* %gc_new9142.repack198 to <4 x i32>*
  store <4 x i32> <i32 18, i32 17, i32 16, i32 15>, <4 x i32>* %15, align 8
  %16 = bitcast i8* %gc_new9142.repack214 to <4 x i32>*
  store <4 x i32> <i32 14, i32 13, i32 12, i32 11>, <4 x i32>* %16, align 8
  %17 = bitcast i8* %gc_new9142.repack230 to <4 x i32>*
  store <4 x i32> <i32 10, i32 9, i32 8, i32 7>, <4 x i32>* %17, align 8
  %18 = bitcast i8* %gc_new9142.repack246 to <4 x i32>*
  store <4 x i32> <i32 6, i32 5, i32 4, i32 3>, <4 x i32>* %18, align 8
  %19 = bitcast i8* %gc_new9142.repack262 to <3 x i32>*
  store <3 x i32> <i32 2, i32 1, i32 0>, <3 x i32>* %19, align 8
  %20 = call { i64, [0 x i32] }* @"_Z6vecAdd:int[],int[]->int[]"({ i64, [0 x i32] }* nonnull %0, { i64, [0 x i32] }* nonnull %10)
  tail call void @cprint(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @0, i64 0, i64 0), i64 2)
  %21 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %20, i64 0, i32 0
  %22 = load i64, i64* %21, align 4
  %23 = icmp eq i64 %22, 0
  br i1 %23, label %loopEnd, label %loop.preheader

loop.preheader:                                   ; preds = %entry
  %24 = bitcast <2 x i64>* %retPtr.i to i8*
  %tmpcast.i = bitcast <2 x i64>* %retPtr.i to %string*
  br label %loop

loop:                                             ; preds = %loop.preheader, %loop
  %i_tmp.0 = phi i64 [ %31, %loop ], [ 0, %loop.preheader ]
  %25 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %20, i64 0, i32 1, i64 %i_tmp.0
  %26 = load i32, i32* %25, align 4
  call void @to_str(i32 %26, %string* nonnull %tmpcast)
  %27 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load274 = extractelement <2 x i64> %27, i32 0
  %28 = inttoptr i64 %.fca.0.load274 to i8*
  %.fca.1.load275 = extractelement <2 x i64> %27, i32 1
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %24)
  call void @strconcat(i8* %28, i64 %.fca.1.load275, i8* getelementptr inbounds ([2 x i8], [2 x i8]* @1, i64 0, i64 0), i64 1, %string* nonnull %tmpcast.i)
  %29 = load <2 x i64>, <2 x i64>* %retPtr.i, align 16
  %.fca.0.load3.i = extractelement <2 x i64> %29, i32 0
  %30 = inttoptr i64 %.fca.0.load3.i to i8*
  %.fca.1.load4.i = extractelement <2 x i64> %29, i32 1
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %24)
  tail call void @cprint(i8* %30, i64 %.fca.1.load4.i)
  %31 = add nuw i64 %i_tmp.0, 1
  %32 = icmp ugt i64 %22, %31
  br i1 %32, label %loop, label %loopEnd

loopEnd:                                          ; preds = %loop, %entry
  tail call void @cprintln(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i64 0, i64 0), i64 1)
  ret void
}

define noalias nonnull { i64, [0 x i32] }* @"_Z6vecAdd:int[],int[]->int[]"({ i64, [0 x i32] }* nocapture readonly %arr1, { i64, [0 x i32] }* nocapture readonly %arr2) local_unnamed_addr {
entry:
  %0 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %arr1, i64 0, i32 0
  %1 = load i64, i64* %0, align 4
  %2 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %arr2, i64 0, i32 0
  %3 = load i64, i64* %2, align 4
  %4 = icmp ugt i64 %1, %3
  %.off0.v = select i1 %4, i64 %3, i64 %1
  %.off0 = trunc i64 %.off0.v to i32
  %5 = shl i32 %.off0, 2
  %6 = add i32 %5, 8
  %7 = sext i32 %6 to i64
  %8 = tail call i8* @gc_new(i64 %7)
  %9 = bitcast i8* %8 to { i64, [0 x i32] }*
  %10 = bitcast i8* %8 to i64*
  %11 = sext i32 %.off0 to i64
  store i64 %11, i64* %10, align 4
  %12 = and i64 %.off0.v, 4294967295
  %13 = icmp eq i32 %.off0, 0
  br i1 %13, label %loopEnd, label %loop.preheader

loop.preheader:                                   ; preds = %entry
  %14 = bitcast { i64, [0 x i32] }* %arr1 to i8*
  %15 = bitcast { i64, [0 x i32] }* %arr2 to i8*
  br label %loop

loop:                                             ; preds = %loop.loop_crit_edge, %loop.preheader
  %.off014 = phi i32 [ %extract.t, %loop.loop_crit_edge ], [ %.off0, %loop.preheader ]
  %range.sroa.0.0 = phi i64 [ %28, %loop.loop_crit_edge ], [ 0, %loop.preheader ]
  %16 = trunc i64 %range.sroa.0.0 to i32
  tail call void @throwIfOutOfBounds(i32 %16, i32 %.off014)
  %17 = sext i32 %16 to i64
  %18 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %9, i64 0, i32 1, i64 %17
  tail call void @throwIfNull(i8* %14, i8* null, i64 0)
  %19 = load i64, i64* %0, align 4
  %20 = trunc i64 %19 to i32
  tail call void @throwIfOutOfBounds(i32 %16, i32 %20)
  %21 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %arr1, i64 0, i32 1, i64 %17
  %22 = load i32, i32* %21, align 4
  tail call void @throwIfNull(i8* %15, i8* null, i64 0)
  %23 = load i64, i64* %2, align 4
  %24 = trunc i64 %23 to i32
  tail call void @throwIfOutOfBounds(i32 %16, i32 %24)
  %25 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %arr2, i64 0, i32 1, i64 %17
  %26 = load i32, i32* %25, align 4
  %27 = add i32 %26, %22
  store i32 %27, i32* %18, align 4
  %28 = add nuw nsw i64 %range.sroa.0.0, 1
  %29 = icmp ult i64 %28, %12
  br i1 %29, label %loop.loop_crit_edge, label %loopEnd, !llvm.loop !0

loop.loop_crit_edge:                              ; preds = %loop
  %.pre = load i64, i64* %10, align 4
  %extract.t = trunc i64 %.pre to i32
  br label %loop

loopEnd:                                          ; preds = %loop, %entry
  ret { i64, [0 x i32] }* %9
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #0

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #0

; Function Attrs: norecurse
declare noalias nonnull i8* @gc_new(i64) local_unnamed_addr #1

declare void @cprint(i8* nocapture readonly, i64) local_unnamed_addr

declare void @strconcat(i8* nocapture readonly, i64, i8* nocapture readonly, i64, %string* nocapture writeonly) local_unnamed_addr

declare void @to_str(i32, %string* nocapture writeonly) local_unnamed_addr

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

; Function Attrs: uwtable
declare void @throwIfNull(i8* nocapture readnone, i8* readonly, i64) local_unnamed_addr #2

; Function Attrs: uwtable
declare void @throwIfOutOfBounds(i32, i32) local_unnamed_addr #2

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.start.p0i8(i64, i8* nocapture) #3

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.end.p0i8(i64, i8* nocapture) #3

attributes #0 = { inaccessiblememonly nounwind }
attributes #1 = { norecurse }
attributes #2 = { uwtable }
attributes #3 = { argmemonly nounwind }

!0 = distinct !{!0, !1, !2}
!1 = !{!"llvm.loop.vectorize.enable", i32 1}
!2 = !{!"llvm.loop.vectorize.width", i32 8}
