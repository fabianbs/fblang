; ModuleID = 'SwitchTest'
source_filename = "SwitchTest"

%"Pair<int, string>" = type { i8*, i32, %string }
%string = type { i8*, i64 }
%"Pair<int, int>" = type { i8*, i32, i32 }

@0 = private unnamed_addr constant [23 x i8] c"Key equals Value with \00"
@1 = private unnamed_addr constant [6 x i8] c"Key: \00"
@2 = private unnamed_addr constant [10 x i8] c"; Value: \00"
@3 = private unnamed_addr constant [7 x i8] c"<null>\00"
@"Pair<int, string>_vtable" = constant { i64, i1 (i8*)* } { i64 -23125620515022919, i1 (i8*)* @"Pair<int, string>.isInstanceOf" }, !type !0
@4 = private unnamed_addr constant [6 x i8] c"Hello\00"
@"Pair<int, int>_vtable" = constant { i64, i1 (i8*)* } { i64 -18015586538064151, i1 (i8*)* @"Pair<int, int>.isInstanceOf" }, !type !1
@5 = private unnamed_addr constant [3 x i8] c"42\00"
@6 = private unnamed_addr constant [17 x i8] c"Key: 42; Value: \00"

; Function Attrs: norecurse nounwind
define void @"_Z22Pair<int, string>.ctor:i,s->v"(%"Pair<int, string>"* nocapture %this, i32 %key, %string %value) local_unnamed_addr #0 {
entry:
  %0 = getelementptr %"Pair<int, string>", %"Pair<int, string>"* %this, i64 0, i32 1
  store i32 %key, i32* %0, align 4
  %.repack = getelementptr %"Pair<int, string>", %"Pair<int, string>"* %this, i64 0, i32 2, i32 0
  %value.elt = extractvalue %string %value, 0
  store i8* %value.elt, i8** %.repack, align 8
  %.repack1 = getelementptr %"Pair<int, string>", %"Pair<int, string>"* %this, i64 0, i32 2, i32 1
  %value.elt2 = extractvalue %string %value, 1
  store i64 %value.elt2, i64* %.repack1, align 8
  ret void
}

; Function Attrs: nounwind
define void @"_Z29Pair<int, string>.operator <-:i,s->v"(%"Pair<int, string>"* nocapture readonly %this, i32* nocapture nonnull %ky, %string* nocapture nonnull %val) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %"Pair<int, string>", %"Pair<int, string>"* %this, i64 0, i32 1
  %1 = load i32, i32* %0, align 4
  store i32 %1, i32* %ky, align 4
  %.elt = getelementptr %"Pair<int, string>", %"Pair<int, string>"* %this, i64 0, i32 2, i32 0
  %.unpack = load i8*, i8** %.elt, align 8
  %.elt1 = getelementptr %"Pair<int, string>", %"Pair<int, string>"* %this, i64 0, i32 2, i32 1
  %.unpack2 = load i64, i64* %.elt1, align 8
  %val.repack = getelementptr inbounds %string, %string* %val, i64 0, i32 0
  store i8* %.unpack, i8** %val.repack, align 8
  %val.repack4 = getelementptr inbounds %string, %string* %val, i64 0, i32 1
  store i64 %.unpack2, i64* %val.repack4, align 8
  %2 = bitcast i32* %ky to i8*
  tail call void @llvm.lifetime.end.p0i8(i64 8, i8* nonnull %2)
  %3 = bitcast %string* %val to i8*
  tail call void @llvm.lifetime.end.p0i8(i64 8, i8* nonnull %3)
  ret void
}

; Function Attrs: norecurse nounwind
define void @"_Z19Pair<int, int>.ctor:i,i->v"(%"Pair<int, int>"* nocapture %this, i32 %key, i32 %value) local_unnamed_addr #0 {
entry:
  %0 = getelementptr %"Pair<int, int>", %"Pair<int, int>"* %this, i64 0, i32 1
  store i32 %key, i32* %0, align 4
  %1 = getelementptr %"Pair<int, int>", %"Pair<int, int>"* %this, i64 0, i32 2
  store i32 %value, i32* %1, align 4
  ret void
}

; Function Attrs: nounwind
define void @"_Z26Pair<int, int>.operator <-:i,i->v"(%"Pair<int, int>"* nocapture readonly %this, i32* nocapture nonnull %ky, i32* nocapture nonnull %val) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %"Pair<int, int>", %"Pair<int, int>"* %this, i64 0, i32 1
  %1 = load i32, i32* %0, align 4
  store i32 %1, i32* %ky, align 4
  %2 = getelementptr %"Pair<int, int>", %"Pair<int, int>"* %this, i64 0, i32 2
  %3 = load i32, i32* %2, align 4
  store i32 %3, i32* %val, align 4
  %4 = bitcast i32* %ky to i8*
  tail call void @llvm.lifetime.end.p0i8(i64 8, i8* nonnull %4)
  %5 = bitcast i32* %val to i8*
  tail call void @llvm.lifetime.end.p0i8(i64 8, i8* nonnull %5)
  ret void
}

define internal fastcc void @"_Z3bar:Pair<int, int>->v"(%"Pair<int, int>"* readonly %p) unnamed_addr {
entry:
  %retPtr.i69 = alloca %string, align 8
  %retPtr.i62 = alloca <2 x i64>, align 16
  %tmpcast87 = bitcast <2 x i64>* %retPtr.i62 to %string*
  %retPtr.i55 = alloca <2 x i64>, align 16
  %tmpcast86 = bitcast <2 x i64>* %retPtr.i55 to %string*
  %retPtr.i = alloca <2 x i64>, align 16
  %tmpcast88 = bitcast <2 x i64>* %retPtr.i to %string*
  %0 = alloca [4 x %string], align 16
  %1 = alloca %string, align 8
  %tmp = alloca <2 x i64>, align 16
  %tmpcast = bitcast <2 x i64>* %tmp to %string*
  %.sub = getelementptr inbounds [4 x %string], [4 x %string]* %0, i64 0, i64 0
  %2 = icmp eq %"Pair<int, int>"* %p, null
  br i1 %2, label %match14, label %endRecursivePatternMatch

switchEnd:                                        ; preds = %match14, %match12, %match
  ret void

endRecursivePatternMatch:                         ; preds = %entry
  %3 = getelementptr %"Pair<int, int>", %"Pair<int, int>"* %p, i64 0, i32 1
  %4 = load i32, i32* %3, align 4
  %5 = getelementptr %"Pair<int, int>", %"Pair<int, int>"* %p, i64 0, i32 2
  %6 = load i32, i32* %5, align 4
  %7 = icmp eq i32 %4, %6
  call void @to_str(i32 %4, %string* nonnull %tmpcast)
  %8 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load84 = extractelement <2 x i64> %8, i32 0
  %9 = inttoptr i64 %.fca.0.load84 to i8*
  %.fca.1.load85 = extractelement <2 x i64> %8, i32 1
  br i1 %7, label %match, label %match12

match:                                            ; preds = %endRecursivePatternMatch
  %10 = bitcast <2 x i64>* %retPtr.i to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %10)
  call void @strconcat(i8* getelementptr inbounds ([23 x i8], [23 x i8]* @0, i64 0, i64 0), i64 22, i8* %9, i64 %.fca.1.load85, %string* nonnull %tmpcast88)
  %11 = load <2 x i64>, <2 x i64>* %retPtr.i, align 16
  %.fca.0.load.i76 = extractelement <2 x i64> %11, i32 0
  %12 = inttoptr i64 %.fca.0.load.i76 to i8*
  %.fca.1.load.i77 = extractelement <2 x i64> %11, i32 1
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %10)
  tail call void @cprintln(i8* %12, i64 %.fca.1.load.i77)
  br label %switchEnd

match12:                                          ; preds = %endRecursivePatternMatch
  %13 = bitcast <2 x i64>* %retPtr.i55 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %13)
  call void @strconcat(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @1, i64 0, i64 0), i64 5, i8* %9, i64 %.fca.1.load85, %string* nonnull %tmpcast86)
  %14 = load <2 x i64>, <2 x i64>* %retPtr.i55, align 16
  %.fca.0.load.i5778 = extractelement <2 x i64> %14, i32 0
  %15 = inttoptr i64 %.fca.0.load.i5778 to i8*
  %.fca.1.load.i5979 = extractelement <2 x i64> %14, i32 1
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %13)
  %16 = bitcast <2 x i64>* %retPtr.i62 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %16)
  call void @strconcat(i8* %15, i64 %.fca.1.load.i5979, i8* getelementptr inbounds ([10 x i8], [10 x i8]* @2, i64 0, i64 0), i64 9, %string* nonnull %tmpcast87)
  %17 = load <2 x i64>, <2 x i64>* %retPtr.i62, align 16
  %.fca.0.load.i6480 = extractelement <2 x i64> %17, i32 0
  %18 = inttoptr i64 %.fca.0.load.i6480 to i8*
  %.fca.1.load.i6681 = extractelement <2 x i64> %17, i32 1
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %16)
  call void @to_str(i32 %6, %string* nonnull %tmpcast)
  %19 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load2482 = extractelement <2 x i64> %19, i32 0
  %20 = inttoptr i64 %.fca.0.load2482 to i8*
  %.fca.1.load2783 = extractelement <2 x i64> %19, i32 1
  %21 = bitcast [4 x %string]* %0 to i8*
  call void @llvm.lifetime.start.p0i8(i64 64, i8* nonnull %21)
  %22 = bitcast [4 x %string]* %0 to <2 x i64>*
  store <2 x i64> <i64 ptrtoint ([6 x i8]* @1 to i64), i64 5>, <2 x i64>* %22, align 16
  %23 = getelementptr inbounds [4 x %string], [4 x %string]* %0, i64 0, i64 1, i32 0
  %24 = bitcast i8** %23 to <2 x i64>*
  store <2 x i64> %8, <2 x i64>* %24, align 16
  %25 = getelementptr inbounds [4 x %string], [4 x %string]* %0, i64 0, i64 2, i32 0
  %26 = bitcast i8** %25 to <2 x i64>*
  store <2 x i64> <i64 ptrtoint ([10 x i8]* @2 to i64), i64 9>, <2 x i64>* %26, align 16
  %27 = getelementptr inbounds [4 x %string], [4 x %string]* %0, i64 0, i64 3, i32 0
  %28 = bitcast i8** %27 to <2 x i64>*
  store <2 x i64> %19, <2 x i64>* %28, align 16
  %29 = bitcast %string* %1 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %29)
  call void @strmulticoncat(%string* nonnull %.sub, i64 4, %string* nonnull %1)
  %.fca.1.gep53 = getelementptr inbounds %string, %string* %1, i64 0, i32 1
  %.fca.1.load54 = load i64, i64* %.fca.1.gep53, align 8
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %29)
  call void @llvm.lifetime.end.p0i8(i64 64, i8* nonnull %21)
  %30 = bitcast %string* %retPtr.i69 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %30)
  call void @strconcat(i8* %18, i64 %.fca.1.load.i6681, i8* %20, i64 %.fca.1.load2783, %string* nonnull %retPtr.i69)
  %.fca.0.gep.i70 = getelementptr inbounds %string, %string* %retPtr.i69, i64 0, i32 0
  %.fca.0.load.i71 = load i8*, i8** %.fca.0.gep.i70, align 8
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %30)
  tail call void @cprintln(i8* %.fca.0.load.i71, i64 %.fca.1.load54)
  br label %switchEnd

match14:                                          ; preds = %entry
  tail call void @cprintln(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @3, i64 0, i64 0), i64 6)
  br label %switchEnd
}

define void @main() local_unnamed_addr {
entry:
  %retPtr.i16 = alloca %string, align 8
  %retPtr.i9 = alloca <2 x i64>, align 16
  %tmpcast = bitcast <2 x i64>* %retPtr.i9 to %string*
  %0 = alloca [4 x %string], align 16
  %1 = alloca %string, align 8
  %.sub = getelementptr inbounds [4 x %string], [4 x %string]* %0, i64 0, i64 0
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %2 = bitcast <2 x i64>* %retPtr.i9 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %2)
  %3 = getelementptr %string, %string* %tmpcast, i32 0, i32 0
  store i8* getelementptr inbounds ([17 x i8], [17 x i8]* @6, i32 0, i32 0), i8** %3
  %4 = getelementptr %string, %string* %tmpcast, i32 0, i32 1
  store i64 16, i64* %4
  %5 = load <2 x i64>, <2 x i64>* %retPtr.i9, align 16
  %.fca.0.load.i1125 = extractelement <2 x i64> %5, i32 0
  %6 = inttoptr i64 %.fca.0.load.i1125 to i8*
  %.fca.1.load.i1326 = extractelement <2 x i64> %5, i32 1
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %2)
  %7 = bitcast [4 x %string]* %0 to i8*
  call void @llvm.lifetime.start.p0i8(i64 64, i8* nonnull %7)
  %8 = bitcast [4 x %string]* %0 to <2 x i64>*
  store <2 x i64> <i64 ptrtoint ([6 x i8]* @1 to i64), i64 5>, <2 x i64>* %8, align 16
  %9 = getelementptr inbounds [4 x %string], [4 x %string]* %0, i64 0, i64 1, i32 0
  %10 = bitcast i8** %9 to <2 x i64>*
  store <2 x i64> <i64 ptrtoint ([3 x i8]* @5 to i64), i64 2>, <2 x i64>* %10, align 16
  %11 = getelementptr inbounds [4 x %string], [4 x %string]* %0, i64 0, i64 2, i32 0
  %12 = bitcast i8** %11 to <2 x i64>*
  store <2 x i64> <i64 ptrtoint ([10 x i8]* @2 to i64), i64 9>, <2 x i64>* %12, align 16
  %13 = getelementptr inbounds [4 x %string], [4 x %string]* %0, i64 0, i64 3, i32 0
  %14 = bitcast i8** %13 to <2 x i64>*
  store <2 x i64> <i64 ptrtoint ([6 x i8]* @4 to i64), i64 5>, <2 x i64>* %14, align 16
  %15 = bitcast %string* %1 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %15)
  call void @strmulticoncat(%string* nonnull %.sub, i64 4, %string* nonnull %1)
  %.fca.1.gep = getelementptr inbounds %string, %string* %1, i64 0, i32 1
  %.fca.1.load = load i64, i64* %.fca.1.gep, align 8
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %15)
  call void @llvm.lifetime.end.p0i8(i64 64, i8* nonnull %7)
  %16 = bitcast %string* %retPtr.i16 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %16)
  call void @strconcat(i8* %6, i64 %.fca.1.load.i1326, i8* getelementptr inbounds ([6 x i8], [6 x i8]* @4, i64 0, i64 0), i64 5, %string* nonnull %retPtr.i16)
  %.fca.0.gep.i17 = getelementptr inbounds %string, %string* %retPtr.i16, i64 0, i32 0
  %.fca.0.load.i18 = load i8*, i8** %.fca.0.gep.i17, align 8
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %16)
  tail call void @cprintln(i8* %.fca.0.load.i18, i64 %.fca.1.load)
  tail call void @cprintln(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @3, i64 0, i64 0), i64 6)
  %17 = tail call i8* @gc_new(i64 16)
  %18 = bitcast i8* %17 to %"Pair<int, int>"*
  %19 = bitcast i8* %17 to i8**
  store i8* bitcast ({ i64, i1 (i8*)* }* @"Pair<int, int>_vtable" to i8*), i8** %19, align 8
  %20 = getelementptr i8, i8* %17, i64 8
  %21 = bitcast i8* %20 to i32*
  store i32 42, i32* %21, align 4
  %22 = getelementptr i8, i8* %17, i64 12
  %23 = bitcast i8* %22 to i32*
  store i32 14, i32* %23, align 4
  %24 = tail call i8* @gc_new(i64 16)
  %25 = bitcast i8* %24 to %"Pair<int, int>"*
  %26 = bitcast i8* %24 to i8**
  store i8* bitcast ({ i64, i1 (i8*)* }* @"Pair<int, int>_vtable" to i8*), i8** %26, align 8
  %27 = getelementptr i8, i8* %24, i64 8
  %28 = bitcast i8* %27 to i32*
  store i32 42, i32* %28, align 4
  %29 = getelementptr i8, i8* %24, i64 12
  %30 = bitcast i8* %29 to i32*
  store i32 42, i32* %30, align 4
  tail call void @cprintln(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @5, i64 0, i64 0), i64 2)
  tail call fastcc void @"_Z3bar:Pair<int, int>->v"(%"Pair<int, int>"* nonnull %18)
  tail call fastcc void @"_Z3bar:Pair<int, int>->v"(%"Pair<int, int>"* nonnull %25)
  ret void
}

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

declare void @strconcat(i8* nocapture readonly, i64, i8* nocapture readonly, i64, %string* nocapture writeonly) local_unnamed_addr

declare void @to_str(i32, %string* nocapture writeonly) local_unnamed_addr

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.end.p0i8(i64, i8* nocapture) #2

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #3

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #3

; Function Attrs: norecurse
declare noalias nonnull i8* @gc_new(i64) local_unnamed_addr #4

; Function Attrs: argmemonly norecurse nounwind readnone
define i1 @"Pair<int, string>.isInstanceOf"(i8* nocapture nonnull readonly %other) #5 {
entry:
  %0 = icmp eq i8* %other, bitcast ({ i64, i1 (i8*)* }* @"Pair<int, string>_vtable" to i8*)
  ret i1 %0
}

; Function Attrs: argmemonly norecurse nounwind readnone
define i1 @"Pair<int, int>.isInstanceOf"(i8* nocapture nonnull readonly %other) #5 {
entry:
  %0 = icmp eq i8* %other, bitcast ({ i64, i1 (i8*)* }* @"Pair<int, int>_vtable" to i8*)
  ret i1 %0
}

declare void @strmulticoncat(%string* nocapture readonly, i64, %string* nocapture writeonly) local_unnamed_addr

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.start.p0i8(i64, i8* nocapture) #2

attributes #0 = { norecurse nounwind }
attributes #1 = { nounwind }
attributes #2 = { argmemonly nounwind }
attributes #3 = { inaccessiblememonly nounwind }
attributes #4 = { norecurse }
attributes #5 = { argmemonly norecurse nounwind readnone }

!0 = !{i64 0, !"Pair<int, string>"}
!1 = !{i64 0, !"Pair<int, int>"}
