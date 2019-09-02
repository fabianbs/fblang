; ModuleID = 'IncludingTest'
source_filename = "IncludingTest"

%"Vector<int>.getIterator:coroutineFrameTy" = type { i32, %"Vector<int>"*, { i32*, i64 }, i64, i32 }
%"Vector<int>" = type { i8*, { i64, [0 x i32] }*, i64 }
%interface = type { i8*, i8* }
%"Vector<int>.reversed:coroutineFrameTy" = type { i32, %"Vector<int>"*, i64 }
%"ReadOnlyVector<int>" = type { i8*, i8*, %"Vector<int>"* }
%"ReadOnlyVector2<int>" = type { i8*, i8*, %"Vector<int>"* }
%string = type { i8*, i64 }

@"Vector<int>_vtable" = constant { i64, i1 (i8*)* } { i64 -2522720733479600265, i1 (i8*)* @"Vector<int>.isInstanceOf" }, !type !0
@"Vector<int>.getIterator:iterator.vtable_vtable" = constant { i1 (%"Vector<int>.getIterator:coroutineFrameTy"*, i32*)* } { i1 (%"Vector<int>.getIterator:coroutineFrameTy"*, i32*)* @"Vector<int>.getIterator:iterator.tryGetNext" }, !type !1
@"Vector<int>.reversed:iterable.vtable_vtable" = constant { %interface (i8*)* } { %interface (i8*)* @"Vector<int>.reversed:iterable.getIterator" }, !type !2
@"Vector<int>.reversed:iterator.vtable_vtable" = constant { i1 (%"Vector<int>.reversed:coroutineFrameTy"*, i32*)* } { i1 (%"Vector<int>.reversed:coroutineFrameTy"*, i32*)* @"Vector<int>.reversed:iterator.tryGetNext" }, !type !3
@"ReadOnlyVector<int>_vtable" = constant { i64, i1 (i8*)* } { i64 -7098520825868353675, i1 (i8*)* @"ReadOnlyVector<int>.isInstanceOf" }, !type !4
@"ReadOnlyVector<int>#interface_IReadOnlyList<int>_vtable" = local_unnamed_addr constant { i64 (%"ReadOnlyVector<int>"*)*, i32 (%"ReadOnlyVector<int>"*, i64)*, %interface (%"ReadOnlyVector<int>"*)*, %interface (%"ReadOnlyVector<int>"*)* } { i64 (%"ReadOnlyVector<int>"*)* @"_Z26ReadOnlyVector<int>.length:v->z", i32 (%"ReadOnlyVector<int>"*, i64)* @"_Z31ReadOnlyVector<int>.operator []:z->i", %interface (%"ReadOnlyVector<int>"*)* @"_Z31ReadOnlyVector<int>.getIterator:v->iterator int", %interface (%"ReadOnlyVector<int>"*)* @"_Z28ReadOnlyVector<int>.reversed:v->iterable int" }, !type !5
@"ReadOnlyVector2<int>_vtable" = constant { i64, i1 (i8*)* } { i64 -2666564205247242377, i1 (i8*)* @"ReadOnlyVector2<int>.isInstanceOf" }, !type !6
@"ReadOnlyVector2<int>#interface_IReadOnlyList<int>_vtable" = local_unnamed_addr constant { i64 (%"ReadOnlyVector2<int>"*)*, i32 (%"ReadOnlyVector2<int>"*, i64)*, %interface (%"ReadOnlyVector2<int>"*)*, %interface (%"ReadOnlyVector2<int>"*)* } { i64 (%"ReadOnlyVector2<int>"*)* @"_Z27ReadOnlyVector2<int>.length:v->z", i32 (%"ReadOnlyVector2<int>"*, i64)* @"_Z32ReadOnlyVector2<int>.operator []:z->i", %interface (%"ReadOnlyVector2<int>"*)* @"_Z32ReadOnlyVector2<int>.getIterator:v->iterator int", %interface (%"ReadOnlyVector2<int>"*)* @"_Z29ReadOnlyVector2<int>.reversed:v->iterable int" }, !type !7

define void @main() local_unnamed_addr {
"_Z15Vector<int>.add:i->v.exit":
  %tmp1 = alloca <2 x i64>, align 16
  %tmpcast = bitcast <2 x i64>* %tmp1 to %string*
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %gc_new88 = alloca %"Vector<int>", align 8
  %gc_new88.repack96 = getelementptr inbounds %"Vector<int>", %"Vector<int>"* %gc_new88, i64 0, i32 1
  %0 = getelementptr inbounds %"Vector<int>", %"Vector<int>"* %gc_new88, i64 0, i32 0
  store i8* bitcast ({ i64, i1 (i8*)* }* @"Vector<int>_vtable" to i8*), i8** %0, align 8
  %gc_new83112 = alloca [24 x i8], align 8
  %gc_new83112.sub = getelementptr inbounds [24 x i8], [24 x i8]* %gc_new83112, i64 0, i64 0
  %gc_new83112.repack120 = getelementptr inbounds [24 x i8], [24 x i8]* %gc_new83112, i64 0, i64 8
  %1 = bitcast [24 x i8]* %gc_new83112 to i64*
  call void @llvm.memset.p0i8.i64(i8* nonnull align 8 %gc_new83112.sub, i8 0, i64 24, i1 false)
  store i64 4, i64* %1, align 8
  %2 = ptrtoint [24 x i8]* %gc_new83112 to i64
  %3 = insertelement <2 x i64> <i64 undef, i64 1>, i64 %2, i32 0
  %4 = bitcast { i64, [0 x i32] }** %gc_new88.repack96 to <2 x i64>*
  store <2 x i64> %3, <2 x i64>* %4, align 8
  %5 = bitcast i8* %gc_new83112.repack120 to i32*
  store i32 42, i32* %5, align 8
  %6 = tail call i8* @gc_new(i64 28)
  %7 = bitcast i8* %6 to i64*
  store i64 5, i64* %7, align 4
  %8 = getelementptr i8, i8* %6, i64 8
  %9 = bitcast i8* %8 to i32*
  store i32 2, i32* %9, align 4
  %10 = getelementptr i8, i8* %6, i64 12
  %11 = bitcast i8* %10 to i32*
  store i32 5, i32* %11, align 4
  %12 = getelementptr i8, i8* %6, i64 16
  %13 = bitcast i8* %12 to i32*
  store i32 3, i32* %13, align 4
  %14 = getelementptr i8, i8* %6, i64 20
  %15 = bitcast i8* %14 to i32*
  store i32 7, i32* %15, align 4
  %16 = getelementptr i8, i8* %6, i64 24
  %17 = bitcast i8* %16 to i32*
  store i32 65, i32* %17, align 4
  %18 = insertvalue { i32*, i64 } undef, i32* %9, 0
  %19 = insertvalue { i32*, i64 } %18, i64 5, 1
  call void @"_Z15Vector<int>.add:int*->v"(%"Vector<int>"* nonnull %gc_new88, { i32*, i64 } %19)
  %20 = load <2 x i64>, <2 x i64>* %4, align 8
  %21 = extractelement <2 x i64> %20, i32 0
  %22 = inttoptr i64 %21 to { i64, [0 x i32] }*
  %23 = extractelement <2 x i64> %20, i32 1
  %24 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %22, i64 0, i32 0
  %25 = load i64, i64* %24, align 4
  %26 = icmp ult i64 %23, %25
  %27 = select i1 %26, i64 %23, i64 %25
  %28 = icmp eq i64 %27, 0
  br i1 %28, label %loopEnd, label %loop.preheader

loop.preheader:                                   ; preds = %"_Z15Vector<int>.add:i->v.exit"
  %29 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %22, i64 0, i32 1, i64 0
  %30 = load i32, i32* %29, align 4
  call void @to_str(i32 %30, %string* nonnull %tmpcast)
  %31 = load <2 x i64>, <2 x i64>* %tmp1, align 16
  %.fca.0.load81146 = extractelement <2 x i64> %31, i32 0
  %32 = inttoptr i64 %.fca.0.load81146 to i8*
  %.fca.1.load82147 = extractelement <2 x i64> %31, i32 1
  tail call void @cprintln(i8* %32, i64 %.fca.1.load82147)
  %33 = icmp eq i64 %27, 1
  br i1 %33, label %loopEnd, label %"Vector<int>.getIterator:iterator.tryGetNext.exit68"

"Vector<int>.getIterator:iterator.tryGetNext.exit68": ; preds = %loop.preheader, %"Vector<int>.getIterator:iterator.tryGetNext.exit68"
  %34 = phi i64 [ %39, %"Vector<int>.getIterator:iterator.tryGetNext.exit68" ], [ 1, %loop.preheader ]
  %35 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %22, i64 0, i32 1, i64 %34
  %36 = load i32, i32* %35, align 4
  call void @to_str(i32 %36, %string* nonnull %tmpcast)
  %37 = load <2 x i64>, <2 x i64>* %tmp1, align 16
  %.fca.0.load144 = extractelement <2 x i64> %37, i32 0
  %38 = inttoptr i64 %.fca.0.load144 to i8*
  %.fca.1.load145 = extractelement <2 x i64> %37, i32 1
  tail call void @cprintln(i8* %38, i64 %.fca.1.load145)
  %39 = add nuw i64 %34, 1
  %40 = icmp ugt i64 %27, %39
  br i1 %40, label %"Vector<int>.getIterator:iterator.tryGetNext.exit68", label %loopEnd

loopEnd:                                          ; preds = %"Vector<int>.getIterator:iterator.tryGetNext.exit68", %loop.preheader, %"_Z15Vector<int>.add:i->v.exit"
  %41 = load i64, i64* %24, align 4
  %42 = icmp ult i64 %23, %41
  %43 = select i1 %42, i64 %23, i64 %41
  %44 = icmp eq i64 %43, 0
  br i1 %44, label %loopEnd5, label %loop4.preheader

loop4.preheader:                                  ; preds = %loopEnd
  %45 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %22, i64 0, i32 1, i64 0
  %46 = load i32, i32* %45, align 4
  call void @to_str(i32 %46, %string* nonnull %tmpcast)
  %47 = load <2 x i64>, <2 x i64>* %tmp1, align 16
  %.fca.0.load2279142 = extractelement <2 x i64> %47, i32 0
  %48 = inttoptr i64 %.fca.0.load2279142 to i8*
  %.fca.1.load2580143 = extractelement <2 x i64> %47, i32 1
  tail call void @cprintln(i8* %48, i64 %.fca.1.load2580143)
  %49 = icmp eq i64 %43, 1
  br i1 %49, label %loopEnd5, label %"Vector<int>.getIterator:iterator.tryGetNext.exit46"

"Vector<int>.getIterator:iterator.tryGetNext.exit46": ; preds = %loop4.preheader, %"Vector<int>.getIterator:iterator.tryGetNext.exit46"
  %50 = phi i64 [ %55, %"Vector<int>.getIterator:iterator.tryGetNext.exit46" ], [ 1, %loop4.preheader ]
  %51 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %22, i64 0, i32 1, i64 %50
  %52 = load i32, i32* %51, align 4
  call void @to_str(i32 %52, %string* nonnull %tmpcast)
  %53 = load <2 x i64>, <2 x i64>* %tmp1, align 16
  %.fca.0.load22140 = extractelement <2 x i64> %53, i32 0
  %54 = inttoptr i64 %.fca.0.load22140 to i8*
  %.fca.1.load25141 = extractelement <2 x i64> %53, i32 1
  tail call void @cprintln(i8* %54, i64 %.fca.1.load25141)
  %55 = add nuw i64 %50, 1
  %56 = icmp ugt i64 %43, %55
  br i1 %56, label %"Vector<int>.getIterator:iterator.tryGetNext.exit46", label %loopEnd5

loopEnd5:                                         ; preds = %"Vector<int>.getIterator:iterator.tryGetNext.exit46", %loop4.preheader, %loopEnd
  ret void
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #0

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #0

; Function Attrs: norecurse
define void @"_Z16Vector<int>.ctor:v->v"(%"Vector<int>"* nocapture %this) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %"Vector<int>", %"Vector<int>"* %this, i64 0, i32 2
  store i64 0, i64* %0, align 4
  %1 = getelementptr %"Vector<int>", %"Vector<int>"* %this, i64 0, i32 1
  %2 = tail call i8* @gc_new(i64 24)
  %3 = bitcast i8* %2 to i64*
  store i64 4, i64* %3, align 4
  %4 = bitcast { i64, [0 x i32] }** %1 to i8**
  store i8* %2, i8** %4, align 8
  ret void
}

; Function Attrs: norecurse nounwind readonly
define i64 @"_Z18Vector<int>.length:v->z"(%"Vector<int>"* nocapture readonly %this) local_unnamed_addr #2 {
entry:
  %0 = getelementptr %"Vector<int>", %"Vector<int>"* %this, i64 0, i32 2
  %1 = load i64, i64* %0, align 4
  ret i64 %1
}

define i32 @"_Z23Vector<int>.operator []:z->i"(%"Vector<int>"* nocapture readonly %this, i64 %ind) local_unnamed_addr {
entry:
  %0 = getelementptr %"Vector<int>", %"Vector<int>"* %this, i64 0, i32 1
  %1 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %0, align 8
  %2 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %1, i64 0, i32 0
  %3 = load i64, i64* %2, align 4
  tail call void @throwIfOutOfBounds64(i64 %ind, i64 %3)
  %4 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %1, i64 0, i32 1, i64 %ind
  %5 = load i32, i32* %4, align 4
  ret i32 %5
}

define void @"_Z15Vector<int>.add:i->v"(%"Vector<int>"* nocapture %this, i32 %val) local_unnamed_addr {
entry:
  %0 = getelementptr %"Vector<int>", %"Vector<int>"* %this, i64 0, i32 2
  %1 = load i64, i64* %0, align 4
  %2 = add i64 %1, 1
  %3 = getelementptr %"Vector<int>", %"Vector<int>"* %this, i64 0, i32 1
  %4 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %3, align 8
  %5 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %4, i64 0, i32 0
  %6 = load i64, i64* %5, align 4
  %7 = icmp ugt i64 %2, %6
  br i1 %7, label %then.i, label %"_Z26Vector<int>.ensureCapacity:z->v.exit"

then.i:                                           ; preds = %entry
  %8 = shl i64 %6, 1
  %9 = icmp ugt i64 %2, %8
  %extract.t10.i = trunc i64 %8 to i32
  br i1 %9, label %loop.i, label %loopEnd.i

loop.i:                                           ; preds = %then.i, %loop.i
  %cap.0.i = phi i64 [ %10, %loop.i ], [ %8, %then.i ]
  %10 = shl i64 %cap.0.i, 1
  %11 = icmp ugt i64 %2, %10
  br i1 %11, label %loop.i, label %loopEnd.loopexit.i

loopEnd.loopexit.i:                               ; preds = %loop.i
  %extract.t.le.i = trunc i64 %10 to i32
  br label %loopEnd.i

loopEnd.i:                                        ; preds = %loopEnd.loopexit.i, %then.i
  %cap.1.off0.i = phi i32 [ %extract.t10.i, %then.i ], [ %extract.t.le.i, %loopEnd.loopexit.i ]
  %12 = shl i32 %cap.1.off0.i, 2
  %13 = add i32 %12, 8
  %14 = sext i32 %13 to i64
  %15 = tail call i8* @gc_new(i64 %14)
  %16 = bitcast i8* %15 to i64*
  %17 = sext i32 %cap.1.off0.i to i64
  store i64 %17, i64* %16, align 4
  %18 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %3, align 8
  %19 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %18, i64 0, i32 0
  %20 = load i64, i64* %19, align 4
  %21 = icmp eq i64 %20, 0
  br i1 %21, label %deconstruction_end.i, label %CompilerInfrastructure.Expressions.UnOp_destination.i

deconstruction_end.i:                             ; preds = %CompilerInfrastructure.Expressions.UnOp_destination.i, %loopEnd.i
  %22 = bitcast { i64, [0 x i32] }** %3 to i8**
  store i8* %15, i8** %22, align 8
  %.pre = load i64, i64* %0, align 4
  %23 = bitcast i8* %15 to { i64, [0 x i32] }*
  %.pre1 = add i64 %.pre, 1
  br label %"_Z26Vector<int>.ensureCapacity:z->v.exit"

CompilerInfrastructure.Expressions.UnOp_destination.i: ; preds = %loopEnd.i
  %24 = icmp ugt i64 %20, %17
  %25 = select i1 %24, i64 %17, i64 %20
  %26 = getelementptr i8, i8* %15, i64 8
  %27 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %18, i64 0, i32 1, i64 0
  %28 = shl i64 %25, 2
  %29 = bitcast i32* %27 to i8*
  tail call void @llvm.memmove.p0i8.p0i8.i64(i8* align 1 %26, i8* align 1 %29, i64 %28, i1 false)
  br label %deconstruction_end.i

"_Z26Vector<int>.ensureCapacity:z->v.exit":       ; preds = %entry, %deconstruction_end.i
  %.pre-phi3 = phi i64* [ %5, %entry ], [ %16, %deconstruction_end.i ]
  %.pre-phi = phi i64 [ %2, %entry ], [ %.pre1, %deconstruction_end.i ]
  %30 = phi { i64, [0 x i32] }* [ %4, %entry ], [ %23, %deconstruction_end.i ]
  %31 = phi i64 [ %1, %entry ], [ %.pre, %deconstruction_end.i ]
  store i64 %.pre-phi, i64* %0, align 4
  %32 = load i64, i64* %.pre-phi3, align 4
  tail call void @throwIfOutOfBounds64(i64 %31, i64 %32)
  %33 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %30, i64 0, i32 1, i64 %31
  store i32 %val, i32* %33, align 4
  ret void
}

define void @"_Z15Vector<int>.add:int*->v"(%"Vector<int>"* nocapture %this, { i32*, i64 } %val) local_unnamed_addr {
entry:
  %val.fca.0.extract = extractvalue { i32*, i64 } %val, 0
  %val.fca.1.extract = extractvalue { i32*, i64 } %val, 1
  %0 = getelementptr %"Vector<int>", %"Vector<int>"* %this, i64 0, i32 2
  %1 = load i64, i64* %0, align 4
  %2 = add i64 %1, %val.fca.1.extract
  %3 = getelementptr %"Vector<int>", %"Vector<int>"* %this, i64 0, i32 1
  %4 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %3, align 8
  %5 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %4, i64 0, i32 0
  %6 = load i64, i64* %5, align 4
  %7 = icmp ugt i64 %2, %6
  br i1 %7, label %then.i, label %"_Z26Vector<int>.ensureCapacity:z->v.exit"

then.i:                                           ; preds = %entry
  %8 = shl i64 %6, 1
  %9 = icmp ugt i64 %2, %8
  %extract.t10.i = trunc i64 %8 to i32
  br i1 %9, label %loop.i, label %loopEnd.i

loop.i:                                           ; preds = %then.i, %loop.i
  %cap.0.i = phi i64 [ %10, %loop.i ], [ %8, %then.i ]
  %10 = shl i64 %cap.0.i, 1
  %11 = icmp ugt i64 %2, %10
  br i1 %11, label %loop.i, label %loopEnd.loopexit.i

loopEnd.loopexit.i:                               ; preds = %loop.i
  %extract.t.le.i = trunc i64 %10 to i32
  br label %loopEnd.i

loopEnd.i:                                        ; preds = %loopEnd.loopexit.i, %then.i
  %cap.1.off0.i = phi i32 [ %extract.t10.i, %then.i ], [ %extract.t.le.i, %loopEnd.loopexit.i ]
  %12 = shl i32 %cap.1.off0.i, 2
  %13 = add i32 %12, 8
  %14 = sext i32 %13 to i64
  %15 = tail call i8* @gc_new(i64 %14)
  %16 = bitcast i8* %15 to i64*
  %17 = sext i32 %cap.1.off0.i to i64
  store i64 %17, i64* %16, align 4
  %18 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %3, align 8
  %19 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %18, i64 0, i32 0
  %20 = load i64, i64* %19, align 4
  %21 = icmp eq i64 %20, 0
  br i1 %21, label %deconstruction_end.i, label %CompilerInfrastructure.Expressions.UnOp_destination.i

deconstruction_end.i:                             ; preds = %CompilerInfrastructure.Expressions.UnOp_destination.i, %loopEnd.i
  %22 = bitcast { i64, [0 x i32] }** %3 to i8**
  store i8* %15, i8** %22, align 8
  %23 = bitcast i8* %15 to { i64, [0 x i32] }*
  br label %"_Z26Vector<int>.ensureCapacity:z->v.exit"

CompilerInfrastructure.Expressions.UnOp_destination.i: ; preds = %loopEnd.i
  %24 = icmp ugt i64 %20, %17
  %25 = select i1 %24, i64 %17, i64 %20
  %26 = getelementptr i8, i8* %15, i64 8
  %27 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %18, i64 0, i32 1, i64 0
  %28 = shl i64 %25, 2
  %29 = bitcast i32* %27 to i8*
  tail call void @llvm.memmove.p0i8.p0i8.i64(i8* align 1 %26, i8* align 1 %29, i64 %28, i1 false)
  br label %deconstruction_end.i

"_Z26Vector<int>.ensureCapacity:z->v.exit":       ; preds = %entry, %deconstruction_end.i
  %30 = phi { i64, [0 x i32] }* [ %4, %entry ], [ %23, %deconstruction_end.i ]
  %31 = icmp eq i64 %val.fca.1.extract, 0
  br i1 %31, label %loopEnd, label %loop.preheader

loop.preheader:                                   ; preds = %"_Z26Vector<int>.ensureCapacity:z->v.exit"
  %32 = load i32, i32* %val.fca.0.extract, align 4
  %33 = load i64, i64* %0, align 4
  %34 = add i64 %33, 1
  store i64 %34, i64* %0, align 4
  %35 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %30, i64 0, i32 0
  %36 = load i64, i64* %35, align 4
  tail call void @throwIfOutOfBounds64(i64 %33, i64 %36)
  %37 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %30, i64 0, i32 1, i64 %33
  store i32 %32, i32* %37, align 4
  %38 = icmp eq i64 %val.fca.1.extract, 1
  br i1 %38, label %loopEnd, label %loop.loop_crit_edge

loop.loop_crit_edge:                              ; preds = %loop.preheader, %loop.loop_crit_edge
  %39 = phi i64 [ %47, %loop.loop_crit_edge ], [ 1, %loop.preheader ]
  %.pre = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %3, align 8
  %40 = getelementptr i32, i32* %val.fca.0.extract, i64 %39
  %41 = load i32, i32* %40, align 4
  %42 = load i64, i64* %0, align 4
  %43 = add i64 %42, 1
  store i64 %43, i64* %0, align 4
  %44 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %.pre, i64 0, i32 0
  %45 = load i64, i64* %44, align 4
  tail call void @throwIfOutOfBounds64(i64 %42, i64 %45)
  %46 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %.pre, i64 0, i32 1, i64 %42
  store i32 %41, i32* %46, align 4
  %47 = add nuw i64 %39, 1
  %48 = icmp ugt i64 %val.fca.1.extract, %47
  br i1 %48, label %loop.loop_crit_edge, label %loopEnd

loopEnd:                                          ; preds = %loop.loop_crit_edge, %loop.preheader, %"_Z26Vector<int>.ensureCapacity:z->v.exit"
  ret void
}

; Function Attrs: norecurse nounwind readonly
define { i32*, i64 } @"_Z17Vector<int>.slice:v->int*"(%"Vector<int>"* nocapture readonly %this) local_unnamed_addr #2 {
entry:
  %0 = getelementptr %"Vector<int>", %"Vector<int>"* %this, i64 0, i32 1
  %1 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %0, align 8
  %2 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %1, i64 0, i32 0
  %3 = load i64, i64* %2, align 4
  %4 = getelementptr %"Vector<int>", %"Vector<int>"* %this, i64 0, i32 2
  %5 = load i64, i64* %4, align 4
  %6 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %1, i64 0, i32 1, i64 0
  %7 = icmp ult i64 %5, %3
  %8 = select i1 %7, i64 %5, i64 %3
  %9 = insertvalue { i32*, i64 } undef, i32* %6, 0
  %10 = insertvalue { i32*, i64 } %9, i64 %8, 1
  ret { i32*, i64 } %10
}

define i32 @"_Z19Vector<int>.popBack:v->i"(%"Vector<int>"* nocapture %this) local_unnamed_addr {
entry:
  %0 = getelementptr %"Vector<int>", %"Vector<int>"* %this, i64 0, i32 2
  %1 = load i64, i64* %0, align 4
  %2 = add i64 %1, -1
  store i64 %2, i64* %0, align 4
  %3 = getelementptr %"Vector<int>", %"Vector<int>"* %this, i64 0, i32 1
  %4 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %3, align 8
  %5 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %4, i64 0, i32 0
  %6 = load i64, i64* %5, align 4
  tail call void @throwIfOutOfBounds64(i64 %2, i64 %6)
  %7 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %4, i64 0, i32 1, i64 %2
  %8 = load i32, i32* %7, align 4
  %9 = load i64, i64* %0, align 4
  %10 = add i64 %9, -1
  %11 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %3, align 8
  %12 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %11, i64 0, i32 0
  %13 = load i64, i64* %12, align 4
  tail call void @throwIfOutOfBounds64(i64 %10, i64 %13)
  %14 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %11, i64 0, i32 1, i64 %10
  store i32 0, i32* %14, align 4
  ret i32 %8
}

; Function Attrs: norecurse
define %interface @"_Z23Vector<int>.getIterator:v->iterator int"(%"Vector<int>"* %this) local_unnamed_addr #1 {
entry:
  %0 = tail call i8* @gc_new(i64 48)
  %1 = bitcast i8* %0 to i32*
  store i32 0, i32* %1, align 4
  %2 = getelementptr i8, i8* %0, i64 8
  %3 = bitcast i8* %2 to %"Vector<int>"**
  store %"Vector<int>"* %this, %"Vector<int>"** %3, align 8
  %.fca.0.insert = insertvalue %interface undef, i8* %0, 0
  %.fca.1.insert = insertvalue %interface %.fca.0.insert, i8* bitcast ({ i1 (%"Vector<int>.getIterator:coroutineFrameTy"*, i32*)* }* @"Vector<int>.getIterator:iterator.vtable_vtable" to i8*), 1
  ret %interface %.fca.1.insert
}

; Function Attrs: norecurse
define %interface @"_Z20Vector<int>.reversed:v->iterable int"(%"Vector<int>"* %this) local_unnamed_addr #1 {
entry:
  %0 = tail call i8* @gc_new(i64 8)
  %1 = bitcast i8* %0 to %"Vector<int>"**
  store %"Vector<int>"* %this, %"Vector<int>"** %1, align 8
  %.fca.0.insert = insertvalue %interface undef, i8* %0, 0
  %.fca.1.insert = insertvalue %interface %.fca.0.insert, i8* bitcast ({ %interface (i8*)* }* @"Vector<int>.reversed:iterable.vtable_vtable" to i8*), 1
  ret %interface %.fca.1.insert
}

; Function Attrs: norecurse
declare noalias nonnull i8* @gc_new(i64) local_unnamed_addr #1

; Function Attrs: argmemonly norecurse nounwind readnone
define i1 @"Vector<int>.isInstanceOf"(i8* nocapture nonnull readonly %other) #3 {
entry:
  %0 = icmp eq i8* %other, bitcast ({ i64, i1 (i8*)* }* @"Vector<int>_vtable" to i8*)
  ret i1 %0
}

; Function Attrs: argmemonly nounwind
declare void @llvm.memmove.p0i8.p0i8.i64(i8* nocapture, i8* nocapture readonly, i64, i1) #4

; Function Attrs: uwtable
declare void @throwIfOutOfBounds64(i64, i64) local_unnamed_addr #5

; Function Attrs: norecurse nounwind
define internal i1 @"Vector<int>.getIterator:iterator.tryGetNext"(%"Vector<int>.getIterator:coroutineFrameTy"* nocapture %coro_frame, i32* nocapture %ret) #6 {
entry:
  %0 = getelementptr %"Vector<int>.getIterator:coroutineFrameTy", %"Vector<int>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 0
  %1 = load i32, i32* %0, align 4
  switch i32 %1, label %fallThrough [
    i32 0, label %resumePoint0
    i32 1, label %loopCondition
  ]

resumePoint0:                                     ; preds = %entry
  %2 = getelementptr %"Vector<int>.getIterator:coroutineFrameTy", %"Vector<int>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 4
  store i32 0, i32* %2, align 4
  %3 = getelementptr %"Vector<int>.getIterator:coroutineFrameTy", %"Vector<int>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 1
  %4 = load %"Vector<int>"*, %"Vector<int>"** %3, align 8
  %5 = getelementptr %"Vector<int>", %"Vector<int>"* %4, i64 0, i32 1
  %6 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %5, align 8
  %7 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %6, i64 0, i32 0
  %8 = load i64, i64* %7, align 4
  %9 = getelementptr %"Vector<int>", %"Vector<int>"* %4, i64 0, i32 2
  %10 = load i64, i64* %9, align 4
  %11 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %6, i64 0, i32 1, i64 0
  %12 = icmp ult i64 %10, %8
  %13 = select i1 %12, i64 %10, i64 %8
  %.repack = getelementptr %"Vector<int>.getIterator:coroutineFrameTy", %"Vector<int>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 2, i32 0
  store i32* %11, i32** %.repack, align 8
  %.repack6 = getelementptr %"Vector<int>.getIterator:coroutineFrameTy", %"Vector<int>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 2, i32 1
  store i64 %13, i64* %.repack6, align 8
  %14 = getelementptr %"Vector<int>.getIterator:coroutineFrameTy", %"Vector<int>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 3
  store i64 0, i64* %14, align 4
  %15 = icmp eq i64 %13, 0
  br i1 %15, label %fallThrough, label %loop

fallThrough:                                      ; preds = %resumePoint0, %loopCondition, %entry
  ret i1 false

loopCondition:                                    ; preds = %entry
  %16 = getelementptr %"Vector<int>.getIterator:coroutineFrameTy", %"Vector<int>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 3
  %17 = load i64, i64* %16, align 4
  %18 = add i64 %17, 1
  store i64 %18, i64* %16, align 4
  %.elt1 = getelementptr %"Vector<int>.getIterator:coroutineFrameTy", %"Vector<int>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 2, i32 1
  %.unpack2 = load i64, i64* %.elt1, align 8
  %19 = icmp ugt i64 %.unpack2, %18
  br i1 %19, label %loopCondition.loop_crit_edge, label %fallThrough

loopCondition.loop_crit_edge:                     ; preds = %loopCondition
  %.elt.phi.trans.insert = getelementptr %"Vector<int>.getIterator:coroutineFrameTy", %"Vector<int>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 2, i32 0
  %.unpack.pre = load i32*, i32** %.elt.phi.trans.insert, align 8
  %.pre = getelementptr %"Vector<int>.getIterator:coroutineFrameTy", %"Vector<int>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 4
  br label %loop

loop:                                             ; preds = %loopCondition.loop_crit_edge, %resumePoint0
  %.pre-phi13 = phi i32* [ %.pre, %loopCondition.loop_crit_edge ], [ %2, %resumePoint0 ]
  %20 = phi i64 [ %18, %loopCondition.loop_crit_edge ], [ 0, %resumePoint0 ]
  %.unpack = phi i32* [ %.unpack.pre, %loopCondition.loop_crit_edge ], [ %11, %resumePoint0 ]
  %21 = getelementptr i32, i32* %.unpack, i64 %20
  %22 = load i32, i32* %21, align 4
  store i32 %22, i32* %.pre-phi13, align 4
  store i32 %22, i32* %ret, align 4
  store i32 1, i32* %0, align 4
  ret i1 true
}

; Function Attrs: norecurse
define internal %interface @"Vector<int>.reversed:iterable.getIterator"(i8* nocapture readonly %iterableBase_raw) #1 {
entry:
  %0 = tail call i8* @gc_new(i64 24)
  %1 = bitcast i8* %0 to i32*
  store i32 0, i32* %1, align 4
  %2 = getelementptr i8, i8* %0, i64 8
  %3 = bitcast i8* %2 to %"Vector<int>"**
  %4 = bitcast i8* %iterableBase_raw to %"Vector<int>"**
  %5 = load %"Vector<int>"*, %"Vector<int>"** %4, align 8
  store %"Vector<int>"* %5, %"Vector<int>"** %3, align 8
  %.fca.0.insert = insertvalue %interface undef, i8* %0, 0
  %.fca.1.insert = insertvalue %interface %.fca.0.insert, i8* bitcast ({ i1 (%"Vector<int>.reversed:coroutineFrameTy"*, i32*)* }* @"Vector<int>.reversed:iterator.vtable_vtable" to i8*), 1
  ret %interface %.fca.1.insert
}

define internal i1 @"Vector<int>.reversed:iterator.tryGetNext"(%"Vector<int>.reversed:coroutineFrameTy"* nocapture %coro_frame, i32* nocapture %ret) {
entry:
  %0 = getelementptr %"Vector<int>.reversed:coroutineFrameTy", %"Vector<int>.reversed:coroutineFrameTy"* %coro_frame, i64 0, i32 0
  %1 = load i32, i32* %0, align 4
  switch i32 %1, label %fallThrough [
    i32 0, label %resumePoint0
    i32 1, label %loopCondition
  ]

resumePoint0:                                     ; preds = %entry
  %2 = getelementptr %"Vector<int>.reversed:coroutineFrameTy", %"Vector<int>.reversed:coroutineFrameTy"* %coro_frame, i64 0, i32 1
  %3 = load %"Vector<int>"*, %"Vector<int>"** %2, align 8
  %4 = getelementptr %"Vector<int>", %"Vector<int>"* %3, i64 0, i32 2
  %5 = load i64, i64* %4, align 4
  %6 = getelementptr %"Vector<int>.reversed:coroutineFrameTy", %"Vector<int>.reversed:coroutineFrameTy"* %coro_frame, i64 0, i32 2
  store i64 %5, i64* %6, align 4
  %7 = icmp eq i64 %5, 0
  br i1 %7, label %fallThrough, label %loop

fallThrough:                                      ; preds = %resumePoint0, %loopCondition, %entry
  ret i1 false

loopCondition:                                    ; preds = %entry
  %8 = getelementptr %"Vector<int>.reversed:coroutineFrameTy", %"Vector<int>.reversed:coroutineFrameTy"* %coro_frame, i64 0, i32 1
  %9 = load %"Vector<int>"*, %"Vector<int>"** %8, align 8
  %10 = getelementptr %"Vector<int>", %"Vector<int>"* %9, i64 0, i32 2
  %11 = load i64, i64* %10, align 4
  %12 = add i64 %11, -1
  store i64 %12, i64* %10, align 4
  %13 = getelementptr %"Vector<int>.reversed:coroutineFrameTy", %"Vector<int>.reversed:coroutineFrameTy"* %coro_frame, i64 0, i32 2
  %14 = load i64, i64* %13, align 4
  %15 = icmp eq i64 %14, 0
  br i1 %15, label %fallThrough, label %loopCondition.loop_crit_edge

loopCondition.loop_crit_edge:                     ; preds = %loopCondition
  %.pre = load %"Vector<int>"*, %"Vector<int>"** %8, align 8
  br label %loop

loop:                                             ; preds = %loopCondition.loop_crit_edge, %resumePoint0
  %16 = phi %"Vector<int>"* [ %3, %resumePoint0 ], [ %.pre, %loopCondition.loop_crit_edge ]
  %17 = phi i64 [ %5, %resumePoint0 ], [ %14, %loopCondition.loop_crit_edge ]
  %18 = add i64 %17, -1
  %19 = getelementptr %"Vector<int>", %"Vector<int>"* %16, i64 0, i32 1
  %20 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %19, align 8
  %21 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %20, i64 0, i32 0
  %22 = load i64, i64* %21, align 4
  tail call void @throwIfOutOfBounds64(i64 %18, i64 %22)
  %23 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %20, i64 0, i32 1, i64 %18
  %24 = load i32, i32* %23, align 4
  store i32 %24, i32* %ret, align 4
  store i32 1, i32* %0, align 4
  ret i1 true
}

; Function Attrs: norecurse
define void @"_Z24ReadOnlyVector<int>.ctor:v->v"(%"ReadOnlyVector<int>"* nocapture %this) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %"ReadOnlyVector<int>", %"ReadOnlyVector<int>"* %this, i64 0, i32 2
  %1 = tail call i8* @gc_new(i64 24)
  %2 = bitcast i8* %1 to i8**
  store i8* bitcast ({ i64, i1 (i8*)* }* @"Vector<int>_vtable" to i8*), i8** %2, align 8
  %3 = getelementptr i8, i8* %1, i64 16
  %4 = bitcast i8* %3 to i64*
  store i64 0, i64* %4, align 4
  %5 = getelementptr i8, i8* %1, i64 8
  %6 = tail call i8* @gc_new(i64 24)
  %7 = bitcast i8* %6 to i64*
  store i64 4, i64* %7, align 4
  %8 = bitcast i8* %5 to i8**
  store i8* %6, i8** %8, align 8
  %9 = bitcast %"Vector<int>"** %0 to i8**
  store i8* %1, i8** %9, align 8
  ret void
}

; Function Attrs: norecurse nounwind
define void @"_Z24ReadOnlyVector<int>.ctor:Vector<int>->v"(%"ReadOnlyVector<int>"* nocapture %this, %"Vector<int>"* nonnull %underlying) local_unnamed_addr #6 {
entry:
  %0 = getelementptr %"ReadOnlyVector<int>", %"ReadOnlyVector<int>"* %this, i64 0, i32 2
  store %"Vector<int>"* %underlying, %"Vector<int>"** %0, align 8
  ret void
}

; Function Attrs: norecurse nounwind readonly
define i64 @"_Z26ReadOnlyVector<int>.length:v->z"(%"ReadOnlyVector<int>"* nocapture readonly %this) #2 {
entry:
  %0 = getelementptr %"ReadOnlyVector<int>", %"ReadOnlyVector<int>"* %this, i64 0, i32 2
  %1 = load %"Vector<int>"*, %"Vector<int>"** %0, align 8
  %2 = getelementptr %"Vector<int>", %"Vector<int>"* %1, i64 0, i32 2
  %3 = load i64, i64* %2, align 4
  ret i64 %3
}

define i32 @"_Z31ReadOnlyVector<int>.operator []:z->i"(%"ReadOnlyVector<int>"* nocapture readonly %this, i64 %ind) {
entry:
  %0 = getelementptr %"ReadOnlyVector<int>", %"ReadOnlyVector<int>"* %this, i64 0, i32 2
  %1 = load %"Vector<int>"*, %"Vector<int>"** %0, align 8
  %2 = getelementptr %"Vector<int>", %"Vector<int>"* %1, i64 0, i32 1
  %3 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %2, align 8
  %4 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %3, i64 0, i32 0
  %5 = load i64, i64* %4, align 4
  tail call void @throwIfOutOfBounds64(i64 %ind, i64 %5)
  %6 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %3, i64 0, i32 1, i64 %ind
  %7 = load i32, i32* %6, align 4
  ret i32 %7
}

; Function Attrs: norecurse
define %interface @"_Z31ReadOnlyVector<int>.getIterator:v->iterator int"(%"ReadOnlyVector<int>"* nocapture readonly %this) #1 {
entry:
  %0 = getelementptr %"ReadOnlyVector<int>", %"ReadOnlyVector<int>"* %this, i64 0, i32 2
  %1 = load %"Vector<int>"*, %"Vector<int>"** %0, align 8
  %2 = tail call i8* @gc_new(i64 48)
  %3 = bitcast i8* %2 to i32*
  store i32 0, i32* %3, align 4
  %4 = getelementptr i8, i8* %2, i64 8
  %5 = bitcast i8* %4 to %"Vector<int>"**
  store %"Vector<int>"* %1, %"Vector<int>"** %5, align 8
  %.fca.0.insert.i = insertvalue %interface undef, i8* %2, 0
  %.fca.1.insert.i = insertvalue %interface %.fca.0.insert.i, i8* bitcast ({ i1 (%"Vector<int>.getIterator:coroutineFrameTy"*, i32*)* }* @"Vector<int>.getIterator:iterator.vtable_vtable" to i8*), 1
  ret %interface %.fca.1.insert.i
}

; Function Attrs: norecurse
define %interface @"_Z28ReadOnlyVector<int>.reversed:v->iterable int"(%"ReadOnlyVector<int>"* nocapture readonly %this) #1 {
entry:
  %0 = getelementptr %"ReadOnlyVector<int>", %"ReadOnlyVector<int>"* %this, i64 0, i32 2
  %1 = load %"Vector<int>"*, %"Vector<int>"** %0, align 8
  %2 = tail call i8* @gc_new(i64 8)
  %3 = bitcast i8* %2 to %"Vector<int>"**
  store %"Vector<int>"* %1, %"Vector<int>"** %3, align 8
  %.fca.0.insert.i = insertvalue %interface undef, i8* %2, 0
  %.fca.1.insert.i = insertvalue %interface %.fca.0.insert.i, i8* bitcast ({ %interface (i8*)* }* @"Vector<int>.reversed:iterable.vtable_vtable" to i8*), 1
  ret %interface %.fca.1.insert.i
}

; Function Attrs: argmemonly norecurse nounwind readnone
define i1 @"ReadOnlyVector<int>.isInstanceOf"(i8* nocapture nonnull readonly %other) #3 {
entry:
  %0 = icmp eq i8* %other, bitcast ({ i64, i1 (i8*)* }* @"ReadOnlyVector<int>_vtable" to i8*)
  ret i1 %0
}

; Function Attrs: argmemonly norecurse nounwind readnone
define i1 @"IReadOnlyList<int>.isInstanceOf"(i8* nocapture nonnull readnone %other) local_unnamed_addr #3 {
entry:
  ret i1 false
}

; Function Attrs: argmemonly norecurse nounwind readnone
define i1 @"iterator int.isInstanceOf"(i8* nocapture nonnull readnone %other) local_unnamed_addr #3 {
entry:
  ret i1 false
}

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

declare void @to_str(i32, %string* nocapture writeonly) local_unnamed_addr

; Function Attrs: argmemonly norecurse nounwind readnone
define i1 @"ReadOnlyVector2<int>.isInstanceOf"(i8* nocapture nonnull readonly %other) #3 {
entry:
  %0 = icmp eq i8* %other, bitcast ({ i64, i1 (i8*)* }* @"ReadOnlyVector2<int>_vtable" to i8*)
  ret i1 %0
}

; Function Attrs: norecurse
define %interface @"_Z32ReadOnlyVector2<int>.getIterator:v->iterator int"(%"ReadOnlyVector2<int>"* nocapture readonly) #1 {
  %2 = bitcast %"ReadOnlyVector2<int>"* %0 to %"ReadOnlyVector<int>"*
  %3 = tail call %interface @"_Z31ReadOnlyVector<int>.getIterator:v->iterator int"(%"ReadOnlyVector<int>"* nocapture readonly %2) #1
  ret %interface %3
}

define i32 @"_Z32ReadOnlyVector2<int>.operator []:z->i"(%"ReadOnlyVector2<int>"* nocapture readonly, i64) {
  %3 = bitcast %"ReadOnlyVector2<int>"* %0 to %"ReadOnlyVector<int>"*
  %4 = tail call i32 @"_Z31ReadOnlyVector<int>.operator []:z->i"(%"ReadOnlyVector<int>"* nocapture readonly %3, i64 %1)
  ret i32 %4
}

; Function Attrs: norecurse nounwind
define void @"_Z25ReadOnlyVector2<int>.ctor:Vector<int>->v"(%"ReadOnlyVector2<int>"* nocapture, %"Vector<int>"* nonnull) local_unnamed_addr #6 {
  %3 = bitcast %"ReadOnlyVector2<int>"* %0 to %"ReadOnlyVector<int>"*
  tail call void @"_Z24ReadOnlyVector<int>.ctor:Vector<int>->v"(%"ReadOnlyVector<int>"* nocapture %3, %"Vector<int>"* nonnull %1) #6
  ret void
}

; Function Attrs: norecurse
define void @"_Z25ReadOnlyVector2<int>.ctor:v->v"(%"ReadOnlyVector2<int>"* nocapture) local_unnamed_addr #1 {
  %2 = bitcast %"ReadOnlyVector2<int>"* %0 to %"ReadOnlyVector<int>"*
  tail call void @"_Z24ReadOnlyVector<int>.ctor:v->v"(%"ReadOnlyVector<int>"* nocapture %2) #1
  ret void
}

; Function Attrs: norecurse nounwind readonly
define i64 @"_Z27ReadOnlyVector2<int>.length:v->z"(%"ReadOnlyVector2<int>"* nocapture readonly) #2 {
  %2 = bitcast %"ReadOnlyVector2<int>"* %0 to %"ReadOnlyVector<int>"*
  %3 = tail call i64 @"_Z26ReadOnlyVector<int>.length:v->z"(%"ReadOnlyVector<int>"* nocapture readonly %2) #2
  ret i64 %3
}

; Function Attrs: norecurse
define %interface @"_Z29ReadOnlyVector2<int>.reversed:v->iterable int"(%"ReadOnlyVector2<int>"* nocapture readonly) #1 {
  %2 = bitcast %"ReadOnlyVector2<int>"* %0 to %"ReadOnlyVector<int>"*
  %3 = tail call %interface @"_Z28ReadOnlyVector<int>.reversed:v->iterable int"(%"ReadOnlyVector<int>"* nocapture readonly %2) #1
  ret %interface %3
}

; Function Attrs: argmemonly nounwind
declare void @llvm.memset.p0i8.i64(i8* nocapture writeonly, i8, i64, i1) #4

attributes #0 = { inaccessiblememonly nounwind }
attributes #1 = { norecurse }
attributes #2 = { norecurse nounwind readonly }
attributes #3 = { argmemonly norecurse nounwind readnone }
attributes #4 = { argmemonly nounwind }
attributes #5 = { uwtable }
attributes #6 = { norecurse nounwind }

!0 = !{i64 0, !"Vector<int>"}
!1 = !{i64 0, !"Vector<int>.getIterator:iterator.vtable"}
!2 = !{i64 0, !"Vector<int>.reversed:iterable.vtable"}
!3 = !{i64 0, !"Vector<int>.reversed:iterator.vtable"}
!4 = !{i64 0, !"ReadOnlyVector<int>"}
!5 = !{i64 0, !"ReadOnlyVector<int>#interface_IReadOnlyList<int>"}
!6 = !{i64 0, !"ReadOnlyVector2<int>"}
!7 = !{i64 0, !"ReadOnlyVector2<int>#interface_IReadOnlyList<int>"}
