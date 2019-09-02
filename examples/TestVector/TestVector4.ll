; ModuleID = 'TestVector4'
source_filename = "TestVector4"

%"Vector<Data>.getIterator:coroutineFrameTy" = type { i32, %"Vector<Data>"*, { %Data**, i64 }, i64, %Data* }
%"Vector<Data>" = type { i8*, { i64, [0 x %Data*] }*, i64 }
%Data = type { i32 }
%interface = type { i8*, i8* }
%"Vector<Data>.reversed:coroutineFrameTy" = type { i32, %"Vector<Data>"*, i64 }
%string = type { i8*, i64 }

@"Vector<Data>_vtable" = constant { i64, i1 (i8*)* } { i64 -2531054053530550786, i1 (i8*)* @"Vector<Data>.isInstanceOf" }, !type !0
@"Vector<Data>.getIterator:iterator.vtable_vtable" = constant { i1 (%"Vector<Data>.getIterator:coroutineFrameTy"*, %Data**)* } { i1 (%"Vector<Data>.getIterator:coroutineFrameTy"*, %Data**)* @"Vector<Data>.getIterator:iterator.tryGetNext" }, !type !1
@"Vector<Data>.reversed:iterable.vtable_vtable" = constant { %interface (i8*)* } { %interface (i8*)* @"Vector<Data>.reversed:iterable.getIterator" }, !type !2
@"Vector<Data>.reversed:iterator.vtable_vtable" = constant { i1 (%"Vector<Data>.reversed:coroutineFrameTy"*, %Data**)* } { i1 (%"Vector<Data>.reversed:coroutineFrameTy"*, %Data**)* @"Vector<Data>.reversed:iterator.tryGetNext" }, !type !3
@0 = private unnamed_addr constant [2 x i8] c"+\00"

; Function Attrs: norecurse nounwind
define void @"_Z9Data.ctor:j->v"(%Data* nocapture %this, i32 %val) local_unnamed_addr #0 {
entry:
  %0 = getelementptr %Data, %Data* %this, i64 0, i32 0
  store i32 %val, i32* %0, align 4
  ret void
}

; Function Attrs: norecurse nounwind readonly
define i32 @"_Z13Data.getValue:v->j"(%Data* nocapture readonly %this) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %Data, %Data* %this, i64 0, i32 0
  %1 = load i32, i32* %0, align 4
  ret i32 %1
}

define void @main() local_unnamed_addr {
"_Z16Vector<Data>.add:Data->v.exit65":
  %tmp = alloca <2 x i64>, align 16
  %tmpcast = bitcast <2 x i64>* %tmp to %string*
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %gc_new83 = alloca %"Vector<Data>", align 16
  %gc_new83.repack91 = getelementptr inbounds %"Vector<Data>", %"Vector<Data>"* %gc_new83, i64 0, i32 1
  %gc_new83.repack99 = getelementptr inbounds %"Vector<Data>", %"Vector<Data>"* %gc_new83, i64 0, i32 2
  %gc_new79107 = alloca [40 x i8], align 16
  %gc_new79107.repack123 = getelementptr inbounds [40 x i8], [40 x i8]* %gc_new79107, i64 0, i64 16
  %0 = ptrtoint [40 x i8]* %gc_new79107 to i64
  %1 = insertelement <2 x i64> <i64 ptrtoint ({ i64, i1 (i8*)* }* @"Vector<Data>_vtable" to i64), i64 undef>, i64 %0, i32 1
  %2 = bitcast %"Vector<Data>"* %gc_new83 to <2 x i64>*
  store <2 x i64> %1, <2 x i64>* %2, align 16
  %gc_new166 = alloca i32, align 4
  store i32 1, i32* %gc_new166, align 4
  %3 = ptrtoint i32* %gc_new166 to i64
  %4 = insertelement <2 x i64> <i64 4, i64 undef>, i64 %3, i32 1
  %5 = bitcast [40 x i8]* %gc_new79107 to <2 x i64>*
  store <2 x i64> %4, <2 x i64>* %5, align 16
  %gc_new80147 = alloca i32, align 4
  store i32 3, i32* %gc_new80147, align 4
  %gc_new82152 = alloca i32, align 4
  store i32 7, i32* %gc_new82152, align 4
  %6 = ptrtoint i32* %gc_new80147 to i64
  %7 = insertelement <2 x i64> undef, i64 %6, i32 0
  %8 = ptrtoint i32* %gc_new82152 to i64
  %9 = insertelement <2 x i64> %7, i64 %8, i32 1
  %10 = bitcast i8* %gc_new79107.repack123 to <2 x i64>*
  store <2 x i64> %9, <2 x i64>* %10, align 16
  %gc_new81157 = alloca i32, align 4
  store i32 5, i32* %gc_new81157, align 4
  store i64 4, i64* %gc_new83.repack99, align 16
  %11 = getelementptr inbounds [40 x i8], [40 x i8]* %gc_new79107, i64 0, i64 32
  %12 = bitcast i8* %11 to i32**
  store i32* %gc_new81157, i32** %12, align 16
  %13 = tail call i8* @gc_new(i64 24)
  %14 = bitcast i8* %13 to i64*
  store i64 2, i64* %14, align 4
  %15 = tail call i8* @gc_new(i64 4)
  %16 = bitcast i8* %15 to i32*
  store i32 4, i32* %16, align 4
  %17 = getelementptr i8, i8* %13, i64 8
  %18 = bitcast i8* %17 to i8**
  store i8* %15, i8** %18, align 8
  %19 = tail call i8* @gc_new(i64 4)
  %20 = bitcast i8* %19 to i32*
  store i32 7, i32* %20, align 4
  %21 = getelementptr i8, i8* %13, i64 16
  %22 = bitcast i8* %21 to i8**
  store i8* %19, i8** %22, align 8
  %23 = bitcast i8* %17 to %Data**
  %24 = insertvalue { %Data**, i64 } undef, %Data** %23, 0
  %25 = insertvalue { %Data**, i64 } %24, i64 2, 1
  call void @"_Z16Vector<Data>.add:Data*->v"(%"Vector<Data>"* nonnull %gc_new83, { %Data**, i64 } %25)
  %26 = bitcast { i64, [0 x %Data*] }** %gc_new83.repack91 to <2 x i64>*
  %27 = load <2 x i64>, <2 x i64>* %26, align 8
  %28 = extractelement <2 x i64> %27, i32 0
  %29 = inttoptr i64 %28 to { i64, [0 x %Data*] }*
  %30 = extractelement <2 x i64> %27, i32 1
  %31 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %29, i64 0, i32 0
  %32 = load i64, i64* %31, align 4
  %33 = icmp ult i64 %30, %32
  %34 = select i1 %33, i64 %30, i64 %32
  %35 = icmp eq i64 %34, 0
  br i1 %35, label %loopEnd, label %loop

loop:                                             ; preds = %"_Z16Vector<Data>.add:Data->v.exit65", %loop
  %i_tmp.0 = phi i64 [ %42, %loop ], [ 0, %"_Z16Vector<Data>.add:Data->v.exit65" ]
  %36 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %29, i64 0, i32 1, i64 %i_tmp.0
  %37 = load %Data*, %Data** %36, align 8
  %38 = getelementptr %Data, %Data* %37, i64 0, i32 0
  %39 = load i32, i32* %38, align 4
  call void @strmul(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i64 0, i64 0), i64 1, i32 %39, %string* nonnull %tmpcast)
  %40 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load172 = extractelement <2 x i64> %40, i32 0
  %41 = inttoptr i64 %.fca.0.load172 to i8*
  %.fca.1.load173 = extractelement <2 x i64> %40, i32 1
  tail call void @cprintln(i8* %41, i64 %.fca.1.load173)
  %42 = add nuw i64 %i_tmp.0, 1
  %43 = icmp ugt i64 %34, %42
  br i1 %43, label %loop, label %loopEnd.loopexit

loopEnd.loopexit:                                 ; preds = %loop
  %.pre = load i64, i64* %31, align 4
  br label %loopEnd

loopEnd:                                          ; preds = %"_Z16Vector<Data>.add:Data->v.exit65", %loopEnd.loopexit
  %44 = phi i64 [ %.pre, %loopEnd.loopexit ], [ %32, %"_Z16Vector<Data>.add:Data->v.exit65" ]
  tail call void @throwIfOutOfBounds64(i64 3, i64 %44)
  %45 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %29, i64 0, i32 1, i64 3
  %46 = load %Data*, %Data** %45, align 8
  %47 = getelementptr %Data, %Data* %46, i64 0, i32 0
  %48 = load i32, i32* %47, align 4
  call void @uto_str(i32 %48, %string* nonnull %tmpcast)
  %49 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load13170 = extractelement <2 x i64> %49, i32 0
  %50 = inttoptr i64 %.fca.0.load13170 to i8*
  %.fca.1.load16171 = extractelement <2 x i64> %49, i32 1
  tail call void @cprintln(i8* %50, i64 %.fca.1.load16171)
  ret void
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #2

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #2

; Function Attrs: norecurse
define void @"_Z17Vector<Data>.ctor:v->v"(%"Vector<Data>"* nocapture %this) local_unnamed_addr #3 {
entry:
  %0 = getelementptr %"Vector<Data>", %"Vector<Data>"* %this, i64 0, i32 2
  store i64 0, i64* %0, align 4
  %1 = getelementptr %"Vector<Data>", %"Vector<Data>"* %this, i64 0, i32 1
  %2 = tail call i8* @gc_new(i64 40)
  %3 = bitcast i8* %2 to i64*
  store i64 4, i64* %3, align 4
  %4 = bitcast { i64, [0 x %Data*] }** %1 to i8**
  store i8* %2, i8** %4, align 8
  ret void
}

; Function Attrs: norecurse nounwind readonly
define i64 @"_Z19Vector<Data>.length:v->z"(%"Vector<Data>"* nocapture readonly %this) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %"Vector<Data>", %"Vector<Data>"* %this, i64 0, i32 2
  %1 = load i64, i64* %0, align 4
  ret i64 %1
}

define %Data* @"_Z24Vector<Data>.operator []:z->Data"(%"Vector<Data>"* nocapture readonly %this, i64 %ind) local_unnamed_addr {
entry:
  %0 = getelementptr %"Vector<Data>", %"Vector<Data>"* %this, i64 0, i32 1
  %1 = load { i64, [0 x %Data*] }*, { i64, [0 x %Data*] }** %0, align 8
  %2 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %1, i64 0, i32 0
  %3 = load i64, i64* %2, align 4
  tail call void @throwIfOutOfBounds64(i64 %ind, i64 %3)
  %4 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %1, i64 0, i32 1, i64 %ind
  %5 = load %Data*, %Data** %4, align 8
  ret %Data* %5
}

define void @"_Z16Vector<Data>.add:Data->v"(%"Vector<Data>"* nocapture %this, %Data* %val) local_unnamed_addr {
entry:
  %0 = getelementptr %"Vector<Data>", %"Vector<Data>"* %this, i64 0, i32 2
  %1 = load i64, i64* %0, align 4
  %2 = add i64 %1, 1
  %3 = getelementptr %"Vector<Data>", %"Vector<Data>"* %this, i64 0, i32 1
  %4 = load { i64, [0 x %Data*] }*, { i64, [0 x %Data*] }** %3, align 8
  %5 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %4, i64 0, i32 0
  %6 = load i64, i64* %5, align 4
  %7 = icmp ugt i64 %2, %6
  br i1 %7, label %then.i, label %"_Z27Vector<Data>.ensureCapacity:z->v.exit"

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
  %.lcssa = phi i64 [ %10, %loop.i ]
  %extract.t.le.i = trunc i64 %.lcssa to i32
  br label %loopEnd.i

loopEnd.i:                                        ; preds = %loopEnd.loopexit.i, %then.i
  %cap.1.off0.i = phi i32 [ %extract.t10.i, %then.i ], [ %extract.t.le.i, %loopEnd.loopexit.i ]
  %12 = shl i32 %cap.1.off0.i, 3
  %13 = add i32 %12, 8
  %14 = sext i32 %13 to i64
  %15 = tail call i8* @gc_new(i64 %14)
  %16 = bitcast i8* %15 to i64*
  %17 = sext i32 %cap.1.off0.i to i64
  store i64 %17, i64* %16, align 4
  %18 = load { i64, [0 x %Data*] }*, { i64, [0 x %Data*] }** %3, align 8
  %19 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %18, i64 0, i32 0
  %20 = load i64, i64* %19, align 4
  %21 = icmp eq i64 %20, 0
  br i1 %21, label %deconstruction_end.i, label %CompilerInfrastructure.Expressions.UnOp_destination.i

deconstruction_end.i:                             ; preds = %CompilerInfrastructure.Expressions.UnOp_destination.i, %loopEnd.i
  %22 = bitcast { i64, [0 x %Data*] }** %3 to i8**
  store i8* %15, i8** %22, align 8
  %.pre = load i64, i64* %0, align 4
  %23 = bitcast i8* %15 to { i64, [0 x %Data*] }*
  %.pre1 = add i64 %.pre, 1
  br label %"_Z27Vector<Data>.ensureCapacity:z->v.exit"

CompilerInfrastructure.Expressions.UnOp_destination.i: ; preds = %loopEnd.i
  %24 = icmp ugt i64 %20, %17
  %25 = select i1 %24, i64 %17, i64 %20
  %26 = getelementptr i8, i8* %15, i64 8
  %27 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %18, i64 0, i32 1, i64 0
  %28 = shl i64 %25, 3
  %29 = bitcast %Data** %27 to i8*
  tail call void @llvm.memmove.p0i8.p0i8.i64(i8* align 1 %26, i8* align 1 %29, i64 %28, i1 false)
  br label %deconstruction_end.i

"_Z27Vector<Data>.ensureCapacity:z->v.exit":      ; preds = %entry, %deconstruction_end.i
  %.pre-phi3 = phi i64* [ %5, %entry ], [ %16, %deconstruction_end.i ]
  %.pre-phi = phi i64 [ %2, %entry ], [ %.pre1, %deconstruction_end.i ]
  %30 = phi { i64, [0 x %Data*] }* [ %4, %entry ], [ %23, %deconstruction_end.i ]
  %31 = phi i64 [ %1, %entry ], [ %.pre, %deconstruction_end.i ]
  store i64 %.pre-phi, i64* %0, align 4
  %32 = load i64, i64* %.pre-phi3, align 4
  tail call void @throwIfOutOfBounds64(i64 %31, i64 %32)
  %33 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %30, i64 0, i32 1, i64 %31
  store %Data* %val, %Data** %33, align 8
  ret void
}

define void @"_Z16Vector<Data>.add:Data*->v"(%"Vector<Data>"* nocapture %this, { %Data**, i64 } %val) local_unnamed_addr {
entry:
  %val.fca.0.extract = extractvalue { %Data**, i64 } %val, 0
  %val.fca.1.extract = extractvalue { %Data**, i64 } %val, 1
  %0 = getelementptr %"Vector<Data>", %"Vector<Data>"* %this, i64 0, i32 2
  %1 = load i64, i64* %0, align 4
  %2 = add i64 %1, %val.fca.1.extract
  %3 = getelementptr %"Vector<Data>", %"Vector<Data>"* %this, i64 0, i32 1
  %4 = load { i64, [0 x %Data*] }*, { i64, [0 x %Data*] }** %3, align 8
  %5 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %4, i64 0, i32 0
  %6 = load i64, i64* %5, align 4
  %7 = icmp ugt i64 %2, %6
  br i1 %7, label %then.i, label %"_Z27Vector<Data>.ensureCapacity:z->v.exit"

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
  %.lcssa = phi i64 [ %10, %loop.i ]
  %extract.t.le.i = trunc i64 %.lcssa to i32
  br label %loopEnd.i

loopEnd.i:                                        ; preds = %loopEnd.loopexit.i, %then.i
  %cap.1.off0.i = phi i32 [ %extract.t10.i, %then.i ], [ %extract.t.le.i, %loopEnd.loopexit.i ]
  %12 = shl i32 %cap.1.off0.i, 3
  %13 = add i32 %12, 8
  %14 = sext i32 %13 to i64
  %15 = tail call i8* @gc_new(i64 %14)
  %16 = bitcast i8* %15 to i64*
  %17 = sext i32 %cap.1.off0.i to i64
  store i64 %17, i64* %16, align 4
  %18 = load { i64, [0 x %Data*] }*, { i64, [0 x %Data*] }** %3, align 8
  %19 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %18, i64 0, i32 0
  %20 = load i64, i64* %19, align 4
  %21 = icmp eq i64 %20, 0
  br i1 %21, label %deconstruction_end.i, label %CompilerInfrastructure.Expressions.UnOp_destination.i

deconstruction_end.i:                             ; preds = %CompilerInfrastructure.Expressions.UnOp_destination.i, %loopEnd.i
  %22 = bitcast { i64, [0 x %Data*] }** %3 to i8**
  store i8* %15, i8** %22, align 8
  %23 = bitcast i8* %15 to { i64, [0 x %Data*] }*
  br label %"_Z27Vector<Data>.ensureCapacity:z->v.exit"

CompilerInfrastructure.Expressions.UnOp_destination.i: ; preds = %loopEnd.i
  %24 = icmp ugt i64 %20, %17
  %25 = select i1 %24, i64 %17, i64 %20
  %26 = getelementptr i8, i8* %15, i64 8
  %27 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %18, i64 0, i32 1, i64 0
  %28 = shl i64 %25, 3
  %29 = bitcast %Data** %27 to i8*
  tail call void @llvm.memmove.p0i8.p0i8.i64(i8* align 1 %26, i8* align 1 %29, i64 %28, i1 false)
  br label %deconstruction_end.i

"_Z27Vector<Data>.ensureCapacity:z->v.exit":      ; preds = %entry, %deconstruction_end.i
  %30 = phi { i64, [0 x %Data*] }* [ %4, %entry ], [ %23, %deconstruction_end.i ]
  %31 = icmp eq i64 %val.fca.1.extract, 0
  br i1 %31, label %loopEnd, label %loop.preheader

loop.preheader:                                   ; preds = %"_Z27Vector<Data>.ensureCapacity:z->v.exit"
  %32 = load %Data*, %Data** %val.fca.0.extract, align 8
  %33 = load i64, i64* %0, align 4
  %34 = add i64 %33, 1
  store i64 %34, i64* %0, align 4
  %35 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %30, i64 0, i32 0
  %36 = load i64, i64* %35, align 4
  tail call void @throwIfOutOfBounds64(i64 %33, i64 %36)
  %37 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %30, i64 0, i32 1, i64 %33
  store %Data* %32, %Data** %37, align 8
  %38 = icmp eq i64 %val.fca.1.extract, 1
  br i1 %38, label %loopEnd, label %loop.loop_crit_edge

loop.loop_crit_edge:                              ; preds = %loop.preheader, %loop.loop_crit_edge
  %39 = phi i64 [ %47, %loop.loop_crit_edge ], [ 1, %loop.preheader ]
  %.pre = load { i64, [0 x %Data*] }*, { i64, [0 x %Data*] }** %3, align 8
  %40 = getelementptr %Data*, %Data** %val.fca.0.extract, i64 %39
  %41 = load %Data*, %Data** %40, align 8
  %42 = load i64, i64* %0, align 4
  %43 = add i64 %42, 1
  store i64 %43, i64* %0, align 4
  %44 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %.pre, i64 0, i32 0
  %45 = load i64, i64* %44, align 4
  tail call void @throwIfOutOfBounds64(i64 %42, i64 %45)
  %46 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %.pre, i64 0, i32 1, i64 %42
  store %Data* %41, %Data** %46, align 8
  %47 = add nuw i64 %39, 1
  %48 = icmp ugt i64 %val.fca.1.extract, %47
  br i1 %48, label %loop.loop_crit_edge, label %loopEnd

loopEnd:                                          ; preds = %loop.loop_crit_edge, %loop.preheader, %"_Z27Vector<Data>.ensureCapacity:z->v.exit"
  ret void
}

; Function Attrs: norecurse nounwind readonly
define { %Data**, i64 } @"_Z18Vector<Data>.slice:v->Data*"(%"Vector<Data>"* nocapture readonly %this) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %"Vector<Data>", %"Vector<Data>"* %this, i64 0, i32 1
  %1 = load { i64, [0 x %Data*] }*, { i64, [0 x %Data*] }** %0, align 8
  %2 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %1, i64 0, i32 0
  %3 = load i64, i64* %2, align 4
  %4 = getelementptr %"Vector<Data>", %"Vector<Data>"* %this, i64 0, i32 2
  %5 = load i64, i64* %4, align 4
  %6 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %1, i64 0, i32 1, i64 0
  %7 = icmp ult i64 %5, %3
  %8 = select i1 %7, i64 %5, i64 %3
  %9 = insertvalue { %Data**, i64 } undef, %Data** %6, 0
  %10 = insertvalue { %Data**, i64 } %9, i64 %8, 1
  ret { %Data**, i64 } %10
}

define %Data* @"_Z20Vector<Data>.popBack:v->Data"(%"Vector<Data>"* nocapture %this) local_unnamed_addr {
entry:
  %0 = getelementptr %"Vector<Data>", %"Vector<Data>"* %this, i64 0, i32 2
  %1 = load i64, i64* %0, align 4
  %2 = add i64 %1, -1
  store i64 %2, i64* %0, align 4
  %3 = getelementptr %"Vector<Data>", %"Vector<Data>"* %this, i64 0, i32 1
  %4 = load { i64, [0 x %Data*] }*, { i64, [0 x %Data*] }** %3, align 8
  %5 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %4, i64 0, i32 0
  %6 = load i64, i64* %5, align 4
  tail call void @throwIfOutOfBounds64(i64 %2, i64 %6)
  %7 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %4, i64 0, i32 1, i64 %2
  %8 = load %Data*, %Data** %7, align 8
  %9 = load i64, i64* %0, align 4
  %10 = add i64 %9, -1
  %11 = load { i64, [0 x %Data*] }*, { i64, [0 x %Data*] }** %3, align 8
  %12 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %11, i64 0, i32 0
  %13 = load i64, i64* %12, align 4
  tail call void @throwIfOutOfBounds64(i64 %10, i64 %13)
  %14 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %11, i64 0, i32 1, i64 %10
  store %Data* null, %Data** %14, align 8
  ret %Data* %8
}

; Function Attrs: norecurse
define %interface @"_Z24Vector<Data>.getIterator:v->iterator Data"(%"Vector<Data>"* %this) local_unnamed_addr #3 {
entry:
  %0 = tail call i8* @gc_new(i64 48)
  %1 = bitcast i8* %0 to i32*
  store i32 0, i32* %1, align 4
  %2 = getelementptr i8, i8* %0, i64 8
  %3 = bitcast i8* %2 to %"Vector<Data>"**
  store %"Vector<Data>"* %this, %"Vector<Data>"** %3, align 8
  %.fca.0.insert = insertvalue %interface undef, i8* %0, 0
  %.fca.1.insert = insertvalue %interface %.fca.0.insert, i8* bitcast ({ i1 (%"Vector<Data>.getIterator:coroutineFrameTy"*, %Data**)* }* @"Vector<Data>.getIterator:iterator.vtable_vtable" to i8*), 1
  ret %interface %.fca.1.insert
}

; Function Attrs: norecurse
define %interface @"_Z21Vector<Data>.reversed:v->iterable Data"(%"Vector<Data>"* %this) local_unnamed_addr #3 {
entry:
  %0 = tail call i8* @gc_new(i64 8)
  %1 = bitcast i8* %0 to %"Vector<Data>"**
  store %"Vector<Data>"* %this, %"Vector<Data>"** %1, align 8
  %.fca.0.insert = insertvalue %interface undef, i8* %0, 0
  %.fca.1.insert = insertvalue %interface %.fca.0.insert, i8* bitcast ({ %interface (i8*)* }* @"Vector<Data>.reversed:iterable.vtable_vtable" to i8*), 1
  ret %interface %.fca.1.insert
}

; Function Attrs: norecurse
declare noalias nonnull i8* @gc_new(i64) local_unnamed_addr #3

; Function Attrs: argmemonly norecurse nounwind readnone
define i1 @"Vector<Data>.isInstanceOf"(i8* nocapture nonnull readonly %other) #4 {
entry:
  %0 = icmp eq i8* %other, bitcast ({ i64, i1 (i8*)* }* @"Vector<Data>_vtable" to i8*)
  ret i1 %0
}

; Function Attrs: argmemonly nounwind
declare void @llvm.memmove.p0i8.p0i8.i64(i8* nocapture, i8* nocapture readonly, i64, i1) #5

; Function Attrs: uwtable
declare void @throwIfOutOfBounds64(i64, i64) local_unnamed_addr #6

; Function Attrs: norecurse nounwind
define internal i1 @"Vector<Data>.getIterator:iterator.tryGetNext"(%"Vector<Data>.getIterator:coroutineFrameTy"* nocapture %coro_frame, %Data** nocapture %ret) #0 {
entry:
  %0 = getelementptr %"Vector<Data>.getIterator:coroutineFrameTy", %"Vector<Data>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 0
  %1 = load i32, i32* %0, align 4
  switch i32 %1, label %fallThrough [
    i32 0, label %resumePoint0
    i32 1, label %loopCondition
  ]

resumePoint0:                                     ; preds = %entry
  %2 = getelementptr %"Vector<Data>.getIterator:coroutineFrameTy", %"Vector<Data>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 4
  store %Data* null, %Data** %2, align 8
  %3 = getelementptr %"Vector<Data>.getIterator:coroutineFrameTy", %"Vector<Data>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 1
  %4 = load %"Vector<Data>"*, %"Vector<Data>"** %3, align 8
  %5 = getelementptr %"Vector<Data>", %"Vector<Data>"* %4, i64 0, i32 1
  %6 = load { i64, [0 x %Data*] }*, { i64, [0 x %Data*] }** %5, align 8
  %7 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %6, i64 0, i32 0
  %8 = load i64, i64* %7, align 4
  %9 = getelementptr %"Vector<Data>", %"Vector<Data>"* %4, i64 0, i32 2
  %10 = load i64, i64* %9, align 4
  %11 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %6, i64 0, i32 1, i64 0
  %12 = icmp ult i64 %10, %8
  %13 = select i1 %12, i64 %10, i64 %8
  %.repack = getelementptr %"Vector<Data>.getIterator:coroutineFrameTy", %"Vector<Data>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 2, i32 0
  store %Data** %11, %Data*** %.repack, align 8
  %.repack6 = getelementptr %"Vector<Data>.getIterator:coroutineFrameTy", %"Vector<Data>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 2, i32 1
  store i64 %13, i64* %.repack6, align 8
  %14 = getelementptr %"Vector<Data>.getIterator:coroutineFrameTy", %"Vector<Data>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 3
  store i64 0, i64* %14, align 4
  %15 = icmp eq i64 %13, 0
  br i1 %15, label %fallThrough, label %loop

fallThrough:                                      ; preds = %resumePoint0, %loopCondition, %entry
  ret i1 false

loopCondition:                                    ; preds = %entry
  %16 = getelementptr %"Vector<Data>.getIterator:coroutineFrameTy", %"Vector<Data>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 3
  %17 = load i64, i64* %16, align 4
  %18 = add i64 %17, 1
  store i64 %18, i64* %16, align 4
  %.elt1 = getelementptr %"Vector<Data>.getIterator:coroutineFrameTy", %"Vector<Data>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 2, i32 1
  %.unpack2 = load i64, i64* %.elt1, align 8
  %19 = icmp ugt i64 %.unpack2, %18
  br i1 %19, label %loopCondition.loop_crit_edge, label %fallThrough

loopCondition.loop_crit_edge:                     ; preds = %loopCondition
  %.elt.phi.trans.insert = getelementptr %"Vector<Data>.getIterator:coroutineFrameTy", %"Vector<Data>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 2, i32 0
  %.unpack.pre = load %Data**, %Data*** %.elt.phi.trans.insert, align 8
  %.pre = getelementptr %"Vector<Data>.getIterator:coroutineFrameTy", %"Vector<Data>.getIterator:coroutineFrameTy"* %coro_frame, i64 0, i32 4
  br label %loop

loop:                                             ; preds = %loopCondition.loop_crit_edge, %resumePoint0
  %.pre-phi13 = phi %Data** [ %.pre, %loopCondition.loop_crit_edge ], [ %2, %resumePoint0 ]
  %20 = phi i64 [ %18, %loopCondition.loop_crit_edge ], [ 0, %resumePoint0 ]
  %.unpack = phi %Data** [ %.unpack.pre, %loopCondition.loop_crit_edge ], [ %11, %resumePoint0 ]
  %21 = getelementptr %Data*, %Data** %.unpack, i64 %20
  %22 = load %Data*, %Data** %21, align 8
  store %Data* %22, %Data** %.pre-phi13, align 8
  store %Data* %22, %Data** %ret, align 8
  store i32 1, i32* %0, align 4
  ret i1 true
}

; Function Attrs: norecurse
define internal %interface @"Vector<Data>.reversed:iterable.getIterator"(i8* nocapture readonly %iterableBase_raw) #3 {
entry:
  %0 = tail call i8* @gc_new(i64 24)
  %1 = bitcast i8* %0 to i32*
  store i32 0, i32* %1, align 4
  %2 = getelementptr i8, i8* %0, i64 8
  %3 = bitcast i8* %2 to %"Vector<Data>"**
  %4 = bitcast i8* %iterableBase_raw to %"Vector<Data>"**
  %5 = load %"Vector<Data>"*, %"Vector<Data>"** %4, align 8
  store %"Vector<Data>"* %5, %"Vector<Data>"** %3, align 8
  %.fca.0.insert = insertvalue %interface undef, i8* %0, 0
  %.fca.1.insert = insertvalue %interface %.fca.0.insert, i8* bitcast ({ i1 (%"Vector<Data>.reversed:coroutineFrameTy"*, %Data**)* }* @"Vector<Data>.reversed:iterator.vtable_vtable" to i8*), 1
  ret %interface %.fca.1.insert
}

define internal i1 @"Vector<Data>.reversed:iterator.tryGetNext"(%"Vector<Data>.reversed:coroutineFrameTy"* nocapture %coro_frame, %Data** nocapture %ret) {
entry:
  %0 = getelementptr %"Vector<Data>.reversed:coroutineFrameTy", %"Vector<Data>.reversed:coroutineFrameTy"* %coro_frame, i64 0, i32 0
  %1 = load i32, i32* %0, align 4
  switch i32 %1, label %fallThrough [
    i32 0, label %resumePoint0
    i32 1, label %loopCondition
  ]

resumePoint0:                                     ; preds = %entry
  %2 = getelementptr %"Vector<Data>.reversed:coroutineFrameTy", %"Vector<Data>.reversed:coroutineFrameTy"* %coro_frame, i64 0, i32 1
  %3 = load %"Vector<Data>"*, %"Vector<Data>"** %2, align 8
  %4 = getelementptr %"Vector<Data>", %"Vector<Data>"* %3, i64 0, i32 2
  %5 = load i64, i64* %4, align 4
  %6 = getelementptr %"Vector<Data>.reversed:coroutineFrameTy", %"Vector<Data>.reversed:coroutineFrameTy"* %coro_frame, i64 0, i32 2
  store i64 %5, i64* %6, align 4
  %7 = icmp eq i64 %5, 0
  br i1 %7, label %fallThrough, label %loop

fallThrough:                                      ; preds = %resumePoint0, %loopCondition, %entry
  ret i1 false

loopCondition:                                    ; preds = %entry
  %8 = getelementptr %"Vector<Data>.reversed:coroutineFrameTy", %"Vector<Data>.reversed:coroutineFrameTy"* %coro_frame, i64 0, i32 1
  %9 = load %"Vector<Data>"*, %"Vector<Data>"** %8, align 8
  %10 = getelementptr %"Vector<Data>", %"Vector<Data>"* %9, i64 0, i32 2
  %11 = load i64, i64* %10, align 4
  %12 = add i64 %11, -1
  store i64 %12, i64* %10, align 4
  %13 = getelementptr %"Vector<Data>.reversed:coroutineFrameTy", %"Vector<Data>.reversed:coroutineFrameTy"* %coro_frame, i64 0, i32 2
  %14 = load i64, i64* %13, align 4
  %15 = icmp eq i64 %14, 0
  br i1 %15, label %fallThrough, label %loopCondition.loop_crit_edge

loopCondition.loop_crit_edge:                     ; preds = %loopCondition
  %.pre = load %"Vector<Data>"*, %"Vector<Data>"** %8, align 8
  br label %loop

loop:                                             ; preds = %loopCondition.loop_crit_edge, %resumePoint0
  %16 = phi %"Vector<Data>"* [ %3, %resumePoint0 ], [ %.pre, %loopCondition.loop_crit_edge ]
  %17 = phi i64 [ %5, %resumePoint0 ], [ %14, %loopCondition.loop_crit_edge ]
  %18 = add i64 %17, -1
  %19 = getelementptr %"Vector<Data>", %"Vector<Data>"* %16, i64 0, i32 1
  %20 = load { i64, [0 x %Data*] }*, { i64, [0 x %Data*] }** %19, align 8
  %21 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %20, i64 0, i32 0
  %22 = load i64, i64* %21, align 4
  tail call void @throwIfOutOfBounds64(i64 %18, i64 %22)
  %23 = getelementptr { i64, [0 x %Data*] }, { i64, [0 x %Data*] }* %20, i64 0, i32 1, i64 %18
  %24 = load %Data*, %Data** %23, align 8
  store %Data* %24, %Data** %ret, align 8
  store i32 1, i32* %0, align 4
  ret i1 true
}

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

declare void @strmul(i8* nocapture readonly, i64, i32, %string* nocapture writeonly) local_unnamed_addr

declare void @uto_str(i32, %string* nocapture writeonly) local_unnamed_addr

attributes #0 = { norecurse nounwind }
attributes #1 = { norecurse nounwind readonly }
attributes #2 = { inaccessiblememonly nounwind }
attributes #3 = { norecurse }
attributes #4 = { argmemonly norecurse nounwind readnone }
attributes #5 = { argmemonly nounwind }
attributes #6 = { uwtable }

!0 = !{i64 0, !"Vector<Data>"}
!1 = !{i64 0, !"Vector<Data>.getIterator:iterator.vtable"}
!2 = !{i64 0, !"Vector<Data>.reversed:iterable.vtable"}
!3 = !{i64 0, !"Vector<Data>.reversed:iterator.vtable"}
