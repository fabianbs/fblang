; ModuleID = 'IntegratedHashMapTest3'
source_filename = "IntegratedHashMapTest3.fbs"

%Point = type { i32, i32 }
%string = type { i8*, i64 }
%"::HashMap<Point, int>" = type { %"::HashMap<Point, int>::slotTy"*, %"::HashMap<Point, int>::bucketTy"*, i64, i64, i64 }
%"::HashMap<Point, int>::slotTy" = type { i64, i64, i8 }
%"::HashMap<Point, int>::bucketTy" = type { %Point*, i32 }

@0 = private unnamed_addr constant [2 x i8] c"(\00"
@1 = private unnamed_addr constant [3 x i8] c", \00"
@2 = private unnamed_addr constant [2 x i8] c")\00"
@3 = private unnamed_addr constant [15 x i8] c"map[(2, 1)] = \00"
@4 = private unnamed_addr constant [34 x i8] c"Key not found in associated array\00"
@5 = private unnamed_addr constant [5 x i8] c" => \00"

; Function Attrs: norecurse nounwind
define void @"_Z10Point.ctor:i,i->v"(%Point* nocapture %this, i32 %x, i32 %y) local_unnamed_addr #0 {
entry:
  %0 = getelementptr %Point, %Point* %this, i64 0, i32 0
  store i32 %x, i32* %0, align 4
  %1 = getelementptr %Point, %Point* %this, i64 0, i32 1
  store i32 %y, i32* %1, align 4
  ret void
}

define %string @"_Z21Point.operator string:v->s"(%Point* nocapture readonly %this) local_unnamed_addr {
entry:
  %0 = alloca [5 x %string], align 8
  %1 = alloca %string, align 8
  %tmp = alloca %string, align 8
  %.sub = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 0
  %2 = getelementptr %Point, %Point* %this, i64 0, i32 0
  %3 = load i32, i32* %2, align 4
  call void @to_str(i32 %3, %string* nonnull %tmp)
  %.fca.0.gep = getelementptr inbounds %string, %string* %tmp, i64 0, i32 0
  %.fca.0.load = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.gep = getelementptr inbounds %string, %string* %tmp, i64 0, i32 1
  %.fca.1.load = load i64, i64* %.fca.1.gep, align 8
  %4 = getelementptr %Point, %Point* %this, i64 0, i32 1
  %5 = load i32, i32* %4, align 4
  call void @to_str(i32 %5, %string* nonnull %tmp)
  %.fca.0.load2 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load5 = load i64, i64* %.fca.1.gep, align 8
  %6 = bitcast [5 x %string]* %0 to i8*
  call void @llvm.lifetime.start.p0i8(i64 80, i8* nonnull %6)
  %7 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 0, i32 0
  %8 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 0, i32 1
  store i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i64 0, i64 0), i8** %7, align 8
  store i64 1, i64* %8, align 8
  %9 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 1, i32 0
  %10 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 1, i32 1
  store i8* %.fca.0.load, i8** %9, align 8
  store i64 %.fca.1.load, i64* %10, align 8
  %11 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 2, i32 0
  %12 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 2, i32 1
  store i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i64 0, i64 0), i8** %11, align 8
  store i64 2, i64* %12, align 8
  %13 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 3, i32 0
  %14 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 3, i32 1
  store i8* %.fca.0.load2, i8** %13, align 8
  store i64 %.fca.1.load5, i64* %14, align 8
  %15 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 4, i32 0
  %16 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 4, i32 1
  store i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i64 0, i64 0), i8** %15, align 8
  store i64 1, i64* %16, align 8
  %17 = bitcast %string* %1 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %17)
  call void @strmulticoncat(%string* nonnull %.sub, i64 5, %string* nonnull %1)
  %.fca.0.gep21 = getelementptr inbounds %string, %string* %1, i64 0, i32 0
  %.fca.0.load22 = load i8*, i8** %.fca.0.gep21, align 8
  %.fca.0.insert = insertvalue %string undef, i8* %.fca.0.load22, 0
  %.fca.1.gep23 = getelementptr inbounds %string, %string* %1, i64 0, i32 1
  %.fca.1.load24 = load i64, i64* %.fca.1.gep23, align 8
  %.fca.1.insert = insertvalue %string %.fca.0.insert, i64 %.fca.1.load24, 1
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %17)
  call void @llvm.lifetime.end.p0i8(i64 80, i8* nonnull %6)
  ret %string %.fca.1.insert
}

; Function Attrs: norecurse nounwind readonly
define i64 @"_Z17Point.getHashCode:v->z"(%Point* nocapture readonly %this) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %Point, %Point* %this, i64 0, i32 0
  %1 = load i32, i32* %0, align 4
  %2 = zext i32 %1 to i64
  %3 = shl nuw i64 %2, 32
  %4 = getelementptr %Point, %Point* %this, i64 0, i32 1
  %5 = load i32, i32* %4, align 4
  %6 = zext i32 %5 to i64
  %7 = or i64 %3, %6
  ret i64 %7
}

; Function Attrs: norecurse nounwind readonly
define i1 @"_Z12Point.equals:Point->b"(%Point* nocapture readonly %this, %Point* nocapture nonnull readonly %p) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %Point, %Point* %this, i64 0, i32 0
  %1 = load i32, i32* %0, align 4
  %2 = getelementptr %Point, %Point* %p, i64 0, i32 0
  %3 = load i32, i32* %2, align 4
  %4 = icmp eq i32 %1, %3
  br i1 %4, label %checkRHS, label %endLAND

checkRHS:                                         ; preds = %entry
  %5 = getelementptr %Point, %Point* %this, i64 0, i32 1
  %6 = load i32, i32* %5, align 4
  %7 = getelementptr %Point, %Point* %p, i64 0, i32 1
  %8 = load i32, i32* %7, align 4
  %9 = icmp eq i32 %6, %8
  br label %endLAND

endLAND:                                          ; preds = %checkRHS, %entry
  %10 = phi i1 [ %9, %checkRHS ], [ false, %entry ]
  ret i1 %10
}

define void @main() local_unnamed_addr {
entry:
  %retPtr.i = alloca %string, align 8
  %0 = alloca [5 x %string], align 8
  %1 = alloca %string, align 8
  %tmp.i = alloca %string, align 8
  %tmp = alloca %string, align 8
  %.sub = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 0
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %gc_new5053 = alloca %"::HashMap<Point, int>", align 8
  %gc_new5053.repack61 = getelementptr inbounds %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %gc_new5053, i64 0, i32 1
  %gc_new5053.repack69 = getelementptr inbounds %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %gc_new5053, i64 0, i32 2
  %gc_new5053.repack77 = getelementptr inbounds %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %gc_new5053, i64 0, i32 3
  %gc_new93 = alloca [100 x i8], align 1
  %gc_new93.sub = getelementptr inbounds [100 x i8], [100 x i8]* %gc_new93, i64 0, i64 0
  %2 = bitcast i64* %gc_new5053.repack77 to i8*
  call void @llvm.memset.p0i8.i64(i8* nonnull align 8 %2, i8 0, i64 16, i1 false)
  %gc_new51193 = alloca [80 x i8], align 1
  %gc_new51193.sub = getelementptr inbounds [80 x i8], [80 x i8]* %gc_new51193, i64 0, i64 0
  call void @llvm.memset.p0i8.i64(i8* nonnull align 1 %gc_new93.sub, i8 0, i64 100, i1 false)
  %3 = bitcast %"::HashMap<Point, int>"* %gc_new5053 to i8**
  call void @llvm.memset.p0i8.i64(i8* nonnull align 1 %gc_new51193.sub, i8 0, i64 80, i1 false)
  store i8* %gc_new93.sub, i8** %3, align 8
  %4 = bitcast %"::HashMap<Point, int>::bucketTy"** %gc_new5053.repack61 to i8**
  store i8* %gc_new51193.sub, i8** %4, align 8
  store i64 5, i64* %gc_new5053.repack69, align 8
  %5 = tail call i8* @gc_new(i64 8)
  %6 = bitcast i8* %5 to %Point*
  %7 = bitcast i8* %5 to i32*
  store i32 2, i32* %7, align 4
  %8 = getelementptr i8, i8* %5, i64 4
  %9 = bitcast i8* %8 to i32*
  store i32 1, i32* %9, align 4
  call fastcc void @"_Z33::HashMap<Point, int>.operator []:Point,i->i"(%"::HashMap<Point, int>"* nonnull %gc_new5053, %Point* nonnull %6, i32 3)
  %10 = tail call i8* @gc_new(i64 8)
  %11 = bitcast i8* %10 to %Point*
  %12 = bitcast i8* %10 to i32*
  store i32 2, i32* %12, align 4
  %13 = getelementptr i8, i8* %10, i64 4
  %14 = bitcast i8* %13 to i32*
  store i32 4, i32* %14, align 4
  call fastcc void @"_Z33::HashMap<Point, int>.operator []:Point,i->i"(%"::HashMap<Point, int>"* nonnull %gc_new5053, %Point* nonnull %11, i32 4)
  %15 = tail call i8* @gc_new(i64 8)
  %16 = bitcast i8* %15 to %Point*
  %17 = bitcast i8* %15 to i32*
  store i32 2, i32* %17, align 4
  %18 = getelementptr i8, i8* %15, i64 4
  %19 = bitcast i8* %18 to i32*
  store i32 1, i32* %19, align 4
  call fastcc void @"_Z33::HashMap<Point, int>.operator []:Point,i->i"(%"::HashMap<Point, int>"* nonnull %gc_new5053, %Point* nonnull %16, i32 -3)
  %this.idx.i = getelementptr inbounds %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %gc_new5053, i64 0, i32 0
  %this.idx.val.i = load %"::HashMap<Point, int>::slotTy"*, %"::HashMap<Point, int>::slotTy"** %this.idx.i, align 8
  %this.idx1.val.i = load %"::HashMap<Point, int>::bucketTy"*, %"::HashMap<Point, int>::bucketTy"** %gc_new5053.repack61, align 8
  %this.idx2.val.i = load i64, i64* %gc_new5053.repack69, align 8
  %20 = urem i64 8589934593, %this.idx2.val.i
  br label %firstLoop.us.i.i

firstLoop.us.i.i:                                 ; preds = %stateFreeMerge.us.i.i, %entry
  %21 = phi i64 [ %39, %stateFreeMerge.us.i.i ], [ %20, %entry ]
  %22 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %21, i32 2
  %23 = load i8, i8* %22, align 1
  %24 = icmp eq i8 %23, 0
  br i1 %24, label %notFull.i, label %stateNotFree.us.i.i

stateNotFree.us.i.i:                              ; preds = %firstLoop.us.i.i
  %25 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %21, i32 1
  %26 = load i64, i64* %25, align 4
  %27 = icmp eq i64 %26, 8589934593
  br i1 %27, label %hashCodesEqual.us.i.i, label %stateFreeMerge.us.i.i

hashCodesEqual.us.i.i:                            ; preds = %stateNotFree.us.i.i
  %28 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %21, i32 0
  %29 = load i64, i64* %28, align 4
  %30 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i, i64 %29, i32 0
  %31 = load %Point*, %Point** %30, align 8
  %32 = icmp eq %Point* %31, null
  br i1 %32, label %stateFreeMerge.us.i.i, label %bothNotNull.us.i.i

bothNotNull.us.i.i:                               ; preds = %hashCodesEqual.us.i.i
  %33 = getelementptr %Point, %Point* %31, i64 0, i32 0
  %34 = load i32, i32* %33, align 4
  %35 = icmp eq i32 %34, 2
  br i1 %35, label %lhsNullMerge.us.i.i, label %stateFreeMerge.us.i.i

lhsNullMerge.us.i.i:                              ; preds = %bothNotNull.us.i.i
  %36 = getelementptr %Point, %Point* %31, i64 0, i32 1
  %37 = load i32, i32* %36, align 4
  %38 = icmp eq i32 %37, 1
  br i1 %38, label %nullMerge.i, label %stateFreeMerge.us.i.i

stateFreeMerge.us.i.i:                            ; preds = %lhsNullMerge.us.i.i, %bothNotNull.us.i.i, %hashCodesEqual.us.i.i, %stateNotFree.us.i.i
  %39 = add i64 %21, 1
  %40 = icmp ult i64 %39, %this.idx2.val.i
  br i1 %40, label %firstLoop.us.i.i, label %secondLoop.preheader.i10.i

secondLoop.preheader.i10.i:                       ; preds = %stateFreeMerge.us.i.i
  %41 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 0, i32 2
  %42 = load i8, i8* %41, align 1
  %43 = icmp eq i8 %42, 0
  br i1 %43, label %notFull.i, label %stateNotFree2.us.i.i

stateNotFree2.us.i.i:                             ; preds = %secondLoop.preheader.i10.i, %stateFreeMerge3.us.i.i
  %.pre.pre = phi i8 [ %61, %stateFreeMerge3.us.i.i ], [ %42, %secondLoop.preheader.i10.i ]
  %44 = phi i64 [ %59, %stateFreeMerge3.us.i.i ], [ 0, %secondLoop.preheader.i10.i ]
  %45 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %44, i32 1
  %46 = load i64, i64* %45, align 4
  %47 = icmp eq i64 %46, 8589934593
  br i1 %47, label %hashCodesEqual4.us.i.i, label %stateFreeMerge3.us.i.i

hashCodesEqual4.us.i.i:                           ; preds = %stateNotFree2.us.i.i
  %48 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %44, i32 0
  %49 = load i64, i64* %48, align 4
  %50 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i, i64 %49, i32 0
  %51 = load %Point*, %Point** %50, align 8
  %52 = icmp eq %Point* %51, null
  br i1 %52, label %stateFreeMerge3.us.i.i, label %bothNotNull8.us.i.i

bothNotNull8.us.i.i:                              ; preds = %hashCodesEqual4.us.i.i
  %53 = getelementptr %Point, %Point* %51, i64 0, i32 0
  %54 = load i32, i32* %53, align 4
  %55 = icmp eq i32 %54, 2
  br i1 %55, label %lhsNullMerge7.us.i.i, label %stateFreeMerge3.us.i.i

lhsNullMerge7.us.i.i:                             ; preds = %bothNotNull8.us.i.i
  %56 = getelementptr %Point, %Point* %51, i64 0, i32 1
  %57 = load i32, i32* %56, align 4
  %58 = icmp eq i32 %57, 1
  br i1 %58, label %nullMerge.i, label %stateFreeMerge3.us.i.i

stateFreeMerge3.us.i.i:                           ; preds = %lhsNullMerge7.us.i.i, %bothNotNull8.us.i.i, %hashCodesEqual4.us.i.i, %stateNotFree2.us.i.i
  %59 = add i64 %44, 1
  %60 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %59, i32 2
  %61 = load i8, i8* %60, align 1
  %62 = icmp eq i8 %61, 0
  br i1 %62, label %notFull.i, label %stateNotFree2.us.i.i

nullMerge.i:                                      ; preds = %lhsNullMerge.us.i.i, %lhsNullMerge7.us.i.i
  %63 = phi i64 [ %49, %lhsNullMerge7.us.i.i ], [ %29, %lhsNullMerge.us.i.i ]
  %64 = phi i8 [ %.pre.pre, %lhsNullMerge7.us.i.i ], [ %23, %lhsNullMerge.us.i.i ]
  %65 = icmp eq i8 %64, 1
  br i1 %65, label %"_Z33::HashMap<Point, int>.operator []:Point->i.exit", label %notFull.i

notFull.i:                                        ; preds = %firstLoop.us.i.i, %stateFreeMerge3.us.i.i, %secondLoop.preheader.i10.i, %nullMerge.i
  tail call void @throwException(i8* getelementptr inbounds ([34 x i8], [34 x i8]* @4, i64 0, i64 0), i64 33)
  unreachable

"_Z33::HashMap<Point, int>.operator []:Point->i.exit": ; preds = %nullMerge.i
  %66 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i, i64 %63, i32 1
  %67 = load i32, i32* %66, align 4
  call void @to_str(i32 %67, %string* nonnull %tmp)
  %.fca.0.gep = getelementptr inbounds %string, %string* %tmp, i64 0, i32 0
  %.fca.0.load = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.gep = getelementptr inbounds %string, %string* %tmp, i64 0, i32 1
  %.fca.1.load = load i64, i64* %.fca.1.gep, align 8
  %68 = bitcast %string* %retPtr.i to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %68)
  call void @strconcat(i8* getelementptr inbounds ([15 x i8], [15 x i8]* @3, i64 0, i64 0), i64 14, i8* %.fca.0.load, i64 %.fca.1.load, %string* nonnull %retPtr.i)
  %.fca.0.gep.i620 = getelementptr inbounds %string, %string* %retPtr.i, i64 0, i32 0
  %.fca.0.load.i621 = load i8*, i8** %.fca.0.gep.i620, align 8
  %.fca.1.gep.i622 = getelementptr inbounds %string, %string* %retPtr.i, i64 0, i32 1
  %.fca.1.load.i623 = load i64, i64* %.fca.1.gep.i622, align 8
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %68)
  tail call void @cprintln(i8* %.fca.0.load.i621, i64 %.fca.1.load.i623)
  %.idx17.val = load i64, i64* %gc_new5053.repack77, align 8
  %69 = icmp eq i64 %.idx17.val, 0
  br i1 %69, label %loopEnd, label %"_Z32::HashMap<Point, int>.tryGetNext:z,Point&,i->b.exit20"

"_Z32::HashMap<Point, int>.tryGetNext:z,Point&,i->b.exit20": ; preds = %"_Z33::HashMap<Point, int>.operator []:Point->i.exit"
  %70 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i, i64 0, i32 0
  %71 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i, i64 0, i32 1
  %72 = bitcast %string* %tmp.i to i8*
  %.fca.0.gep.i = getelementptr inbounds %string, %string* %tmp.i, i64 0, i32 0
  %.fca.1.gep.i = getelementptr inbounds %string, %string* %tmp.i, i64 0, i32 1
  %73 = bitcast [5 x %string]* %0 to i8*
  %74 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 0, i32 0
  %75 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 0, i32 1
  %76 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 1, i32 0
  %77 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 1, i32 1
  %78 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 2, i32 0
  %79 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 2, i32 1
  %80 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 3, i32 0
  %81 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 3, i32 1
  %82 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 4, i32 0
  %83 = getelementptr inbounds [5 x %string], [5 x %string]* %0, i64 0, i64 4, i32 1
  %84 = bitcast %string* %1 to i8*
  %.fca.0.gep475 = getelementptr inbounds %string, %string* %1, i64 0, i32 0
  %.fca.1.gep477 = getelementptr inbounds %string, %string* %1, i64 0, i32 1
  br label %loop

loop:                                             ; preds = %"_Z32::HashMap<Point, int>.tryGetNext:z,Point&,i->b.exit", %"_Z32::HashMap<Point, int>.tryGetNext:z,Point&,i->b.exit20"
  %"%value.1.in" = phi i32* [ %71, %"_Z32::HashMap<Point, int>.tryGetNext:z,Point&,i->b.exit20" ], [ %91, %"_Z32::HashMap<Point, int>.tryGetNext:z,Point&,i->b.exit" ]
  %"%key.1.in" = phi %Point** [ %70, %"_Z32::HashMap<Point, int>.tryGetNext:z,Point&,i->b.exit20" ], [ %90, %"_Z32::HashMap<Point, int>.tryGetNext:z,Point&,i->b.exit" ]
  %"%state.1" = phi i64 [ 1, %"_Z32::HashMap<Point, int>.tryGetNext:z,Point&,i->b.exit20" ], [ %92, %"_Z32::HashMap<Point, int>.tryGetNext:z,Point&,i->b.exit" ]
  %"%key.1" = load %Point*, %Point** %"%key.1.in", align 8
  %"%value.1" = load i32, i32* %"%value.1.in", align 4
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %72)
  %85 = getelementptr %Point, %Point* %"%key.1", i64 0, i32 0
  %86 = load i32, i32* %85, align 4
  call void @to_str(i32 %86, %string* nonnull %tmp.i)
  %.fca.0.load.i = load i8*, i8** %.fca.0.gep.i, align 8
  %.fca.1.load.i = load i64, i64* %.fca.1.gep.i, align 8
  %87 = getelementptr %Point, %Point* %"%key.1", i64 0, i32 1
  %88 = load i32, i32* %87, align 4
  call void @to_str(i32 %88, %string* nonnull %tmp.i)
  %.fca.0.load2.i = load i8*, i8** %.fca.0.gep.i, align 8
  %.fca.1.load5.i = load i64, i64* %.fca.1.gep.i, align 8
  call void @llvm.lifetime.start.p0i8(i64 80, i8* nonnull %73)
  store i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i64 0, i64 0), i8** %74, align 8
  store i64 1, i64* %75, align 8
  store i8* %.fca.0.load.i, i8** %76, align 8
  store i64 %.fca.1.load.i, i64* %77, align 8
  store i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i64 0, i64 0), i8** %78, align 8
  store i64 2, i64* %79, align 8
  store i8* %.fca.0.load2.i, i8** %80, align 8
  store i64 %.fca.1.load5.i, i64* %81, align 8
  store i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i64 0, i64 0), i8** %82, align 8
  store i64 1, i64* %83, align 8
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %84)
  call void @strmulticoncat(%string* nonnull %.sub, i64 5, %string* nonnull %1)
  %.fca.0.load476 = load i8*, i8** %.fca.0.gep475, align 8
  %.fca.1.load478 = load i64, i64* %.fca.1.gep477, align 8
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %84)
  call void @llvm.lifetime.end.p0i8(i64 80, i8* nonnull %73)
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %72)
  tail call void @cprint(i8* %.fca.0.load476, i64 %.fca.1.load478)
  tail call void @cprint(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i64 0, i64 0), i64 4)
  call void @to_str(i32 %"%value.1", %string* nonnull %tmp)
  %.fca.0.load8 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load11 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load8, i64 %.fca.1.load11)
  %89 = icmp ult i64 %"%state.1", %.idx17.val
  br i1 %89, label %"_Z32::HashMap<Point, int>.tryGetNext:z,Point&,i->b.exit", label %loopEnd

"_Z32::HashMap<Point, int>.tryGetNext:z,Point&,i->b.exit": ; preds = %loop
  %90 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i, i64 %"%state.1", i32 0
  %91 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i, i64 %"%state.1", i32 1
  %92 = add i64 %"%state.1", 1
  br label %loop

loopEnd:                                          ; preds = %loop, %"_Z33::HashMap<Point, int>.operator []:Point->i.exit"
  ret void
}

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.end.p0i8(i64, i8* nocapture) #2

declare void @strconcat(i8* nocapture readonly, i64, i8* nocapture readonly, i64, %string* nocapture writeonly) local_unnamed_addr

declare void @to_str(i32, %string* nocapture writeonly) local_unnamed_addr

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #3

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #3

; Function Attrs: norecurse
declare noalias nonnull i8* @gc_new(i64) local_unnamed_addr #4

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.start.p0i8(i64, i8* nocapture) #2

; Function Attrs: nounwind readnone
declare i64 @nextPrime(i64) local_unnamed_addr #5

; Function Attrs: argmemonly nounwind
declare void @llvm.memcpy.p0i8.p0i8.i64(i8* nocapture writeonly, i8* nocapture readonly, i64, i1) #2

define internal fastcc void @"_Z33::HashMap<Point, int>.operator []:Point,i->i"(%"::HashMap<Point, int>"* nocapture %this, %Point* %key, i32 %value) unnamed_addr {
entry:
  %0 = getelementptr %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %this, i64 0, i32 2
  %1 = load i64, i64* %0, align 4
  %2 = uitofp i64 %1 to float
  %3 = fmul float %2, 0x3FE99999A0000000
  %4 = getelementptr %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %this, i64 0, i32 3
  %5 = load i64, i64* %4, align 4
  %6 = getelementptr %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %this, i64 0, i32 4
  %7 = load i64, i64* %6, align 4
  %8 = add i64 %7, %5
  %9 = uitofp i64 %8 to float
  %10 = fcmp ugt float %3, %9
  br i1 %10, label %"::HashMap<Point, int>.ensureCapacity.exit", label %doRehash.i

doRehash.i:                                       ; preds = %entry
  %11 = icmp ult i64 %5, %7
  br i1 %11, label %doRehash.i.i, label %doIncreaseCapacity.i.i

doIncreaseCapacity.i.i:                           ; preds = %doRehash.i
  %12 = shl i64 %1, 1
  %13 = tail call i64 @nextPrime(i64 %12)
  store i64 %13, i64* %0, align 4
  br label %doRehash.i.i

doRehash.i.i:                                     ; preds = %doIncreaseCapacity.i.i, %doRehash.i
  %14 = phi i64 [ %1, %doRehash.i ], [ %13, %doIncreaseCapacity.i.i ]
  %15 = getelementptr %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %this, i64 0, i32 0
  %16 = load %"::HashMap<Point, int>::slotTy"*, %"::HashMap<Point, int>::slotTy"** %15, align 8
  %17 = getelementptr %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %this, i64 0, i32 1
  %18 = load %"::HashMap<Point, int>::bucketTy"*, %"::HashMap<Point, int>::bucketTy"** %17, align 8
  %19 = mul i64 %14, 20
  %20 = tail call i8* @gc_new(i64 %19)
  %21 = shl i64 %14, 4
  %22 = tail call i8* @gc_new(i64 %21)
  %23 = bitcast %"::HashMap<Point, int>"* %this to i8**
  store i8* %20, i8** %23, align 8
  %24 = bitcast %"::HashMap<Point, int>::bucketTy"** %17 to i8**
  store i8* %22, i8** %24, align 8
  %25 = shl i64 %5, 4
  %26 = bitcast %"::HashMap<Point, int>::bucketTy"* %18 to i8*
  tail call void @llvm.memcpy.p0i8.p0i8.i64(i8* nonnull align 1 %22, i8* align 1 %26, i64 %25, i1 false)
  %27 = icmp eq i64 %5, 0
  br i1 %27, label %"::HashMap<Point, int>.rehash.exit.i", label %loop.i.i

loop.i.i:                                         ; preds = %doRehash.i.i, %loopCond.i.i
  %28 = phi i64 [ %118, %loopCond.i.i ], [ 0, %doRehash.i.i ]
  %29 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %16, i64 %28, i32 2
  %30 = load i8, i8* %29, align 1
  %31 = icmp eq i8 %30, 1
  br i1 %31, label %full.i.i, label %loopCond.i.i

full.i.i:                                         ; preds = %loop.i.i
  %32 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %16, i64 %28, i32 0
  %33 = load i64, i64* %32, align 4
  %34 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %16, i64 %28, i32 1
  %35 = load i64, i64* %34, align 4
  %36 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %18, i64 %33, i32 0
  %37 = load %Point*, %Point** %36, align 8
  %this.idx.val.i.i = load %"::HashMap<Point, int>::slotTy"*, %"::HashMap<Point, int>::slotTy"** %15, align 8
  %this.idx1.val.i.i = load %"::HashMap<Point, int>::bucketTy"*, %"::HashMap<Point, int>::bucketTy"** %17, align 8
  %this.idx2.val.i.i = load i64, i64* %0, align 4
  %38 = urem i64 %35, %this.idx2.val.i.i
  %39 = icmp eq %Point* %37, null
  %40 = getelementptr %Point, %Point* %37, i64 0, i32 0
  %41 = getelementptr %Point, %Point* %37, i64 0, i32 1
  br i1 %39, label %firstLoop.i51, label %firstLoop.us.i45

firstLoop.us.i45:                                 ; preds = %full.i.i, %stateFreeMerge.us.i50
  %42 = phi i64 [ %62, %stateFreeMerge.us.i50 ], [ %38, %full.i.i ]
  %43 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %42, i32 2
  %44 = load i8, i8* %43, align 1
  %45 = icmp eq i8 %44, 0
  br i1 %45, label %"::HashMap<Point, int>.search.exit73", label %stateNotFree.us.i46

stateNotFree.us.i46:                              ; preds = %firstLoop.us.i45
  %46 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %42, i32 1
  %47 = load i64, i64* %46, align 4
  %48 = icmp eq i64 %47, %35
  br i1 %48, label %hashCodesEqual.us.i47, label %stateFreeMerge.us.i50

hashCodesEqual.us.i47:                            ; preds = %stateNotFree.us.i46
  %49 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %42, i32 0
  %50 = load i64, i64* %49, align 4
  %51 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i.i, i64 %50, i32 0
  %52 = load %Point*, %Point** %51, align 8
  %53 = icmp eq %Point* %52, null
  br i1 %53, label %stateFreeMerge.us.i50, label %bothNotNull.us.i48

bothNotNull.us.i48:                               ; preds = %hashCodesEqual.us.i47
  %54 = getelementptr %Point, %Point* %52, i64 0, i32 0
  %55 = load i32, i32* %54, align 4
  %56 = load i32, i32* %40, align 4
  %57 = icmp eq i32 %55, %56
  br i1 %57, label %lhsNullMerge.us.i49, label %stateFreeMerge.us.i50

lhsNullMerge.us.i49:                              ; preds = %bothNotNull.us.i48
  %58 = getelementptr %Point, %Point* %52, i64 0, i32 1
  %59 = load i32, i32* %58, align 4
  %60 = load i32, i32* %41, align 4
  %61 = icmp eq i32 %59, %60
  br i1 %61, label %"::HashMap<Point, int>.search.exit73", label %stateFreeMerge.us.i50

stateFreeMerge.us.i50:                            ; preds = %lhsNullMerge.us.i49, %bothNotNull.us.i48, %hashCodesEqual.us.i47, %stateNotFree.us.i46
  %62 = add i64 %42, 1
  %63 = icmp ult i64 %62, %this.idx2.val.i.i
  br i1 %63, label %firstLoop.us.i45, label %secondLoop.preheader.i56

firstLoop.i51:                                    ; preds = %full.i.i, %stateFreeMerge.i55
  %64 = phi i64 [ %71, %stateFreeMerge.i55 ], [ %38, %full.i.i ]
  %65 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %64, i32 2
  %66 = load i8, i8* %65, align 1
  %67 = icmp eq i8 %66, 0
  br i1 %67, label %"::HashMap<Point, int>.search.exit73", label %stateNotFree.i54

stateNotFree.i54:                                 ; preds = %firstLoop.i51
  %68 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %64, i32 1
  %69 = load i64, i64* %68, align 4
  %70 = icmp eq i64 %69, %35
  br i1 %70, label %hashCodesEqual.i63, label %stateFreeMerge.i55

stateFreeMerge.i55:                               ; preds = %hashCodesEqual.i63, %stateNotFree.i54
  %71 = add i64 %64, 1
  %72 = icmp ult i64 %71, %this.idx2.val.i.i
  br i1 %72, label %firstLoop.i51, label %secondLoop.preheader.i56

secondLoop.preheader.i56:                         ; preds = %stateFreeMerge.us.i50, %stateFreeMerge.i55
  %73 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 0, i32 2
  %74 = load i8, i8* %73, align 1
  %75 = icmp eq i8 %74, 0
  br i1 %75, label %"::HashMap<Point, int>.search.exit73", label %stateNotFree2.lr.ph.i57

stateNotFree2.lr.ph.i57:                          ; preds = %secondLoop.preheader.i56
  br i1 %39, label %stateNotFree2.i68, label %stateNotFree2.us.i58

stateNotFree2.us.i58:                             ; preds = %stateNotFree2.lr.ph.i57, %stateFreeMerge3.us.i62
  %76 = phi i64 [ %93, %stateFreeMerge3.us.i62 ], [ 0, %stateNotFree2.lr.ph.i57 ]
  %77 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %76, i32 1
  %78 = load i64, i64* %77, align 4
  %79 = icmp eq i64 %78, %35
  br i1 %79, label %hashCodesEqual4.us.i59, label %stateFreeMerge3.us.i62

hashCodesEqual4.us.i59:                           ; preds = %stateNotFree2.us.i58
  %80 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %76, i32 0
  %81 = load i64, i64* %80, align 4
  %82 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i.i, i64 %81, i32 0
  %83 = load %Point*, %Point** %82, align 8
  %84 = icmp eq %Point* %83, null
  br i1 %84, label %stateFreeMerge3.us.i62, label %bothNotNull8.us.i60

bothNotNull8.us.i60:                              ; preds = %hashCodesEqual4.us.i59
  %85 = getelementptr %Point, %Point* %83, i64 0, i32 0
  %86 = load i32, i32* %85, align 4
  %87 = load i32, i32* %40, align 4
  %88 = icmp eq i32 %86, %87
  br i1 %88, label %lhsNullMerge7.us.i61, label %stateFreeMerge3.us.i62

lhsNullMerge7.us.i61:                             ; preds = %bothNotNull8.us.i60
  %89 = getelementptr %Point, %Point* %83, i64 0, i32 1
  %90 = load i32, i32* %89, align 4
  %91 = load i32, i32* %41, align 4
  %92 = icmp eq i32 %90, %91
  br i1 %92, label %"::HashMap<Point, int>.search.exit73", label %stateFreeMerge3.us.i62

stateFreeMerge3.us.i62:                           ; preds = %lhsNullMerge7.us.i61, %bothNotNull8.us.i60, %hashCodesEqual4.us.i59, %stateNotFree2.us.i58
  %93 = add i64 %76, 1
  %94 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %93, i32 2
  %95 = load i8, i8* %94, align 1
  %96 = icmp eq i8 %95, 0
  br i1 %96, label %"::HashMap<Point, int>.search.exit73", label %stateNotFree2.us.i58

hashCodesEqual.i63:                               ; preds = %stateNotFree.i54
  %97 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %64, i32 0
  %98 = load i64, i64* %97, align 4
  %99 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i.i, i64 %98, i32 0
  %100 = load %Point*, %Point** %99, align 8
  %101 = icmp eq %Point* %100, null
  br i1 %101, label %"::HashMap<Point, int>.search.exit73", label %stateFreeMerge.i55

stateNotFree2.i68:                                ; preds = %stateNotFree2.lr.ph.i57, %stateFreeMerge3.i69
  %102 = phi i64 [ %106, %stateFreeMerge3.i69 ], [ 0, %stateNotFree2.lr.ph.i57 ]
  %103 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %102, i32 1
  %104 = load i64, i64* %103, align 4
  %105 = icmp eq i64 %104, %35
  br i1 %105, label %hashCodesEqual4.i70, label %stateFreeMerge3.i69

stateFreeMerge3.i69:                              ; preds = %hashCodesEqual4.i70, %stateNotFree2.i68
  %106 = add i64 %102, 1
  %107 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %106, i32 2
  %108 = load i8, i8* %107, align 1
  %109 = icmp eq i8 %108, 0
  br i1 %109, label %"::HashMap<Point, int>.search.exit73", label %stateNotFree2.i68

hashCodesEqual4.i70:                              ; preds = %stateNotFree2.i68
  %110 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %102, i32 0
  %111 = load i64, i64* %110, align 4
  %112 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i.i, i64 %111, i32 0
  %113 = load %Point*, %Point** %112, align 8
  %114 = icmp eq %Point* %113, null
  br i1 %114, label %"::HashMap<Point, int>.search.exit73", label %stateFreeMerge3.i69

"::HashMap<Point, int>.search.exit73":            ; preds = %lhsNullMerge.us.i49, %firstLoop.us.i45, %hashCodesEqual.i63, %firstLoop.i51, %lhsNullMerge7.us.i61, %stateFreeMerge3.us.i62, %hashCodesEqual4.i70, %stateFreeMerge3.i69, %secondLoop.preheader.i56
  %.lcssa1.i71.sink = phi i64 [ 0, %secondLoop.preheader.i56 ], [ %106, %stateFreeMerge3.i69 ], [ %102, %hashCodesEqual4.i70 ], [ %93, %stateFreeMerge3.us.i62 ], [ %76, %lhsNullMerge7.us.i61 ], [ %64, %firstLoop.i51 ], [ %64, %hashCodesEqual.i63 ], [ %42, %firstLoop.us.i45 ], [ %42, %lhsNullMerge.us.i49 ]
  %115 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %.lcssa1.i71.sink, i32 0
  store i64 %33, i64* %115, align 4
  %116 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %.lcssa1.i71.sink, i32 1
  store i64 %35, i64* %116, align 4
  %117 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i.i, i64 %.lcssa1.i71.sink, i32 2
  store i8 1, i8* %117, align 1
  br label %loopCond.i.i

loopCond.i.i:                                     ; preds = %"::HashMap<Point, int>.search.exit73", %loop.i.i
  %118 = add nuw i64 %28, 1
  %119 = icmp ult i64 %118, %1
  br i1 %119, label %loop.i.i, label %"::HashMap<Point, int>.rehash.exit.i"

"::HashMap<Point, int>.rehash.exit.i":            ; preds = %loopCond.i.i, %doRehash.i.i
  store i64 0, i64* %6, align 4
  br label %"::HashMap<Point, int>.ensureCapacity.exit"

"::HashMap<Point, int>.ensureCapacity.exit":      ; preds = %entry, %"::HashMap<Point, int>.rehash.exit.i"
  %120 = icmp eq %Point* %key, null
  br i1 %120, label %entry.split, label %notNull

entry.split:                                      ; preds = %"::HashMap<Point, int>.ensureCapacity.exit"
  %121 = load i64, i64* %4, align 4
  %this.idx.i = getelementptr %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %this, i64 0, i32 0
  %this.idx.val.i = load %"::HashMap<Point, int>::slotTy"*, %"::HashMap<Point, int>::slotTy"** %this.idx.i, align 8
  %this.idx1.i = getelementptr %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %this, i64 0, i32 1
  %this.idx1.val.i = load %"::HashMap<Point, int>::bucketTy"*, %"::HashMap<Point, int>::bucketTy"** %this.idx1.i, align 8
  %this.idx2.val.i = load i64, i64* %0, align 4
  br label %firstLoop.i

firstLoop.i:                                      ; preds = %stateFreeMerge.i, %entry.split
  %122 = phi i64 [ %129, %stateFreeMerge.i ], [ 0, %entry.split ]
  %123 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %122, i32 2
  %124 = load i8, i8* %123, align 1
  %125 = icmp eq i8 %124, 0
  br i1 %125, label %"::HashMap<Point, int>.search.exit.thread", label %stateNotFree.i

stateNotFree.i:                                   ; preds = %firstLoop.i
  %126 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %122, i32 1
  %127 = load i64, i64* %126, align 4
  %128 = icmp eq i64 %127, 0
  br i1 %128, label %hashCodesEqual.i, label %stateFreeMerge.i

stateFreeMerge.i:                                 ; preds = %hashCodesEqual.i, %stateNotFree.i
  %129 = add nuw i64 %122, 1
  %130 = icmp ult i64 %129, %this.idx2.val.i
  br i1 %130, label %firstLoop.i, label %secondLoop.preheader.i

secondLoop.preheader.i:                           ; preds = %stateFreeMerge.i
  %131 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 0, i32 2
  %132 = load i8, i8* %131, align 1
  %133 = icmp eq i8 %132, 0
  br i1 %133, label %"::HashMap<Point, int>.search.exit.thread", label %stateNotFree2.i

hashCodesEqual.i:                                 ; preds = %stateNotFree.i
  %134 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %122, i32 0
  %135 = load i64, i64* %134, align 4
  %136 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i, i64 %135, i32 0
  %137 = load %Point*, %Point** %136, align 8
  %138 = icmp eq %Point* %137, null
  br i1 %138, label %"::HashMap<Point, int>.search.exit", label %stateFreeMerge.i

stateNotFree2.i:                                  ; preds = %secondLoop.preheader.i, %stateFreeMerge3.i
  %.pre.pre = phi i8 [ %145, %stateFreeMerge3.i ], [ %132, %secondLoop.preheader.i ]
  %139 = phi i64 [ %143, %stateFreeMerge3.i ], [ 0, %secondLoop.preheader.i ]
  %140 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %139, i32 1
  %141 = load i64, i64* %140, align 4
  %142 = icmp eq i64 %141, 0
  br i1 %142, label %hashCodesEqual4.i, label %stateFreeMerge3.i

stateFreeMerge3.i:                                ; preds = %hashCodesEqual4.i, %stateNotFree2.i
  %143 = add i64 %139, 1
  %144 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %143, i32 2
  %145 = load i8, i8* %144, align 1
  %146 = icmp eq i8 %145, 0
  br i1 %146, label %"::HashMap<Point, int>.search.exit.thread", label %stateNotFree2.i

hashCodesEqual4.i:                                ; preds = %stateNotFree2.i
  %147 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %139, i32 0
  %148 = load i64, i64* %147, align 4
  %149 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i, i64 %148, i32 0
  %150 = load %Point*, %Point** %149, align 8
  %151 = icmp eq %Point* %150, null
  br i1 %151, label %"hashCodesEqual4.i.::HashMap<Point, int>.search.exit.loopexit_crit_edge", label %stateFreeMerge3.i

"hashCodesEqual4.i.::HashMap<Point, int>.search.exit.loopexit_crit_edge": ; preds = %hashCodesEqual4.i
  %.phi.trans.insert.phi.trans.insert = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %139, i32 2
  br label %"::HashMap<Point, int>.search.exit"

"::HashMap<Point, int>.search.exit.thread":       ; preds = %firstLoop.i, %stateFreeMerge3.i, %secondLoop.preheader.i
  %.pre-phi.ph = phi i8* [ %131, %secondLoop.preheader.i ], [ %144, %stateFreeMerge3.i ], [ %123, %firstLoop.i ]
  %.sink.ph = phi i64 [ 0, %secondLoop.preheader.i ], [ %143, %stateFreeMerge3.i ], [ %122, %firstLoop.i ]
  %152 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %.sink.ph, i32 0
  br label %notFull.i

"::HashMap<Point, int>.search.exit":              ; preds = %hashCodesEqual.i, %"hashCodesEqual4.i.::HashMap<Point, int>.search.exit.loopexit_crit_edge"
  %.pre-phi330 = phi i64* [ %147, %"hashCodesEqual4.i.::HashMap<Point, int>.search.exit.loopexit_crit_edge" ], [ %134, %hashCodesEqual.i ]
  %153 = phi i64 [ %148, %"hashCodesEqual4.i.::HashMap<Point, int>.search.exit.loopexit_crit_edge" ], [ %135, %hashCodesEqual.i ]
  %.pre-phi = phi i8* [ %.phi.trans.insert.phi.trans.insert, %"hashCodesEqual4.i.::HashMap<Point, int>.search.exit.loopexit_crit_edge" ], [ %123, %hashCodesEqual.i ]
  %154 = phi i8 [ %.pre.pre, %"hashCodesEqual4.i.::HashMap<Point, int>.search.exit.loopexit_crit_edge" ], [ %124, %hashCodesEqual.i ]
  %.sink = phi i64 [ %139, %"hashCodesEqual4.i.::HashMap<Point, int>.search.exit.loopexit_crit_edge" ], [ %122, %hashCodesEqual.i ]
  %155 = icmp eq i8 %154, 1
  br i1 %155, label %full-replace.i, label %notFull.i

notFull.i:                                        ; preds = %"::HashMap<Point, int>.search.exit.thread", %"::HashMap<Point, int>.search.exit"
  %156 = phi i64* [ %152, %"::HashMap<Point, int>.search.exit.thread" ], [ %.pre-phi330, %"::HashMap<Point, int>.search.exit" ]
  %.sink127 = phi i64 [ %.sink.ph, %"::HashMap<Point, int>.search.exit.thread" ], [ %.sink, %"::HashMap<Point, int>.search.exit" ]
  %157 = phi i8 [ 0, %"::HashMap<Point, int>.search.exit.thread" ], [ %154, %"::HashMap<Point, int>.search.exit" ]
  %.pre-phi126 = phi i8* [ %.pre-phi.ph, %"::HashMap<Point, int>.search.exit.thread" ], [ %.pre-phi, %"::HashMap<Point, int>.search.exit" ]
  store i64 %121, i64* %156, align 4
  %158 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i, i64 %.sink127, i32 1
  store i64 0, i64* %158, align 4
  store i8 1, i8* %.pre-phi126, align 1
  %159 = load %"::HashMap<Point, int>::bucketTy"*, %"::HashMap<Point, int>::bucketTy"** %this.idx1.i, align 8
  %160 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %159, i64 %121, i32 0
  store %Point* null, %Point** %160, align 8
  %161 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %159, i64 %121, i32 1
  store i32 %value, i32* %161, align 4
  %162 = add i64 %121, 1
  store i64 %162, i64* %4, align 4
  %163 = icmp eq i8 %157, 2
  br i1 %163, label %decrease-deletedCount.i, label %nullMerge

full-replace.i:                                   ; preds = %"::HashMap<Point, int>.search.exit"
  %164 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i, i64 %153, i32 0
  store %Point* null, %Point** %164, align 8
  %165 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i, i64 %153, i32 1
  store i32 %value, i32* %165, align 4
  br label %nullMerge

decrease-deletedCount.i:                          ; preds = %notFull.i
  %166 = load i64, i64* %6, align 4
  %167 = add i64 %166, -1
  store i64 %167, i64* %6, align 4
  br label %nullMerge

notNull:                                          ; preds = %"::HashMap<Point, int>.ensureCapacity.exit"
  %168 = getelementptr %Point, %Point* %key, i64 0, i32 0
  %169 = load i32, i32* %168, align 4
  %170 = zext i32 %169 to i64
  %171 = shl nuw i64 %170, 32
  %172 = getelementptr %Point, %Point* %key, i64 0, i32 1
  %173 = load i32, i32* %172, align 4
  %174 = zext i32 %173 to i64
  %175 = or i64 %171, %174
  %176 = load i64, i64* %4, align 4
  %this.idx.i1 = getelementptr %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %this, i64 0, i32 0
  %this.idx.val.i2 = load %"::HashMap<Point, int>::slotTy"*, %"::HashMap<Point, int>::slotTy"** %this.idx.i1, align 8
  %this.idx1.i3 = getelementptr %"::HashMap<Point, int>", %"::HashMap<Point, int>"* %this, i64 0, i32 1
  %this.idx1.val.i4 = load %"::HashMap<Point, int>::bucketTy"*, %"::HashMap<Point, int>::bucketTy"** %this.idx1.i3, align 8
  %this.idx2.val.i6 = load i64, i64* %0, align 4
  %177 = urem i64 %175, %this.idx2.val.i6
  br label %firstLoop.us.i

firstLoop.us.i:                                   ; preds = %notNull, %stateFreeMerge.us.i
  %178 = phi i64 [ %196, %stateFreeMerge.us.i ], [ %177, %notNull ]
  %179 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i2, i64 %178, i32 2
  %180 = load i8, i8* %179, align 1
  %181 = icmp eq i8 %180, 0
  br i1 %181, label %"::HashMap<Point, int>.search.exit44.thread", label %stateNotFree.us.i

stateNotFree.us.i:                                ; preds = %firstLoop.us.i
  %182 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i2, i64 %178, i32 1
  %183 = load i64, i64* %182, align 4
  %184 = icmp eq i64 %183, %175
  br i1 %184, label %hashCodesEqual.us.i, label %stateFreeMerge.us.i

hashCodesEqual.us.i:                              ; preds = %stateNotFree.us.i
  %185 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i2, i64 %178, i32 0
  %186 = load i64, i64* %185, align 4
  %187 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i4, i64 %186, i32 0
  %188 = load %Point*, %Point** %187, align 8
  %189 = icmp eq %Point* %188, null
  br i1 %189, label %stateFreeMerge.us.i, label %bothNotNull.us.i

bothNotNull.us.i:                                 ; preds = %hashCodesEqual.us.i
  %190 = getelementptr %Point, %Point* %188, i64 0, i32 0
  %191 = load i32, i32* %190, align 4
  %192 = icmp eq i32 %191, %169
  br i1 %192, label %lhsNullMerge.us.i, label %stateFreeMerge.us.i

lhsNullMerge.us.i:                                ; preds = %bothNotNull.us.i
  %193 = getelementptr %Point, %Point* %188, i64 0, i32 1
  %194 = load i32, i32* %193, align 4
  %195 = icmp eq i32 %194, %173
  br i1 %195, label %"::HashMap<Point, int>.search.exit44", label %stateFreeMerge.us.i

stateFreeMerge.us.i:                              ; preds = %lhsNullMerge.us.i, %bothNotNull.us.i, %hashCodesEqual.us.i, %stateNotFree.us.i
  %196 = add i64 %178, 1
  %197 = icmp ult i64 %196, %this.idx2.val.i6
  br i1 %197, label %firstLoop.us.i, label %secondLoop.preheader.i39

secondLoop.preheader.i39:                         ; preds = %stateFreeMerge.us.i
  %198 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i2, i64 0, i32 2
  %199 = load i8, i8* %198, align 1
  %200 = icmp eq i8 %199, 0
  br i1 %200, label %"::HashMap<Point, int>.search.exit44.thread", label %stateNotFree2.us.i

stateNotFree2.us.i:                               ; preds = %secondLoop.preheader.i39, %stateFreeMerge3.us.i
  %.pre.pre327 = phi i8 [ %218, %stateFreeMerge3.us.i ], [ %199, %secondLoop.preheader.i39 ]
  %201 = phi i64 [ %216, %stateFreeMerge3.us.i ], [ 0, %secondLoop.preheader.i39 ]
  %202 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i2, i64 %201, i32 1
  %203 = load i64, i64* %202, align 4
  %204 = icmp eq i64 %203, %175
  br i1 %204, label %hashCodesEqual4.us.i, label %stateFreeMerge3.us.i

hashCodesEqual4.us.i:                             ; preds = %stateNotFree2.us.i
  %205 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i2, i64 %201, i32 0
  %206 = load i64, i64* %205, align 4
  %207 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i4, i64 %206, i32 0
  %208 = load %Point*, %Point** %207, align 8
  %209 = icmp eq %Point* %208, null
  br i1 %209, label %stateFreeMerge3.us.i, label %bothNotNull8.us.i

bothNotNull8.us.i:                                ; preds = %hashCodesEqual4.us.i
  %210 = getelementptr %Point, %Point* %208, i64 0, i32 0
  %211 = load i32, i32* %210, align 4
  %212 = icmp eq i32 %211, %169
  br i1 %212, label %lhsNullMerge7.us.i, label %stateFreeMerge3.us.i

lhsNullMerge7.us.i:                               ; preds = %bothNotNull8.us.i
  %213 = getelementptr %Point, %Point* %208, i64 0, i32 1
  %214 = load i32, i32* %213, align 4
  %215 = icmp eq i32 %214, %173
  br i1 %215, label %"lhsNullMerge7.us.i.::HashMap<Point, int>.search.exit44.loopexit_crit_edge", label %stateFreeMerge3.us.i

"lhsNullMerge7.us.i.::HashMap<Point, int>.search.exit44.loopexit_crit_edge": ; preds = %lhsNullMerge7.us.i
  %.phi.trans.insert.phi.trans.insert326 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i2, i64 %201, i32 2
  br label %"::HashMap<Point, int>.search.exit44"

stateFreeMerge3.us.i:                             ; preds = %lhsNullMerge7.us.i, %bothNotNull8.us.i, %hashCodesEqual4.us.i, %stateNotFree2.us.i
  %216 = add i64 %201, 1
  %217 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i2, i64 %216, i32 2
  %218 = load i8, i8* %217, align 1
  %219 = icmp eq i8 %218, 0
  br i1 %219, label %"::HashMap<Point, int>.search.exit44.thread", label %stateNotFree2.us.i

"::HashMap<Point, int>.search.exit44.thread":     ; preds = %firstLoop.us.i, %stateFreeMerge3.us.i, %secondLoop.preheader.i39
  %.pre-phi331.ph = phi i8* [ %198, %secondLoop.preheader.i39 ], [ %217, %stateFreeMerge3.us.i ], [ %179, %firstLoop.us.i ]
  %.sink202.ph = phi i64 [ 0, %secondLoop.preheader.i39 ], [ %216, %stateFreeMerge3.us.i ], [ %178, %firstLoop.us.i ]
  %220 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i2, i64 %.sink202.ph, i32 0
  br label %notFull.i7

"::HashMap<Point, int>.search.exit44":            ; preds = %lhsNullMerge.us.i, %"lhsNullMerge7.us.i.::HashMap<Point, int>.search.exit44.loopexit_crit_edge"
  %.pre-phi621 = phi i64* [ %205, %"lhsNullMerge7.us.i.::HashMap<Point, int>.search.exit44.loopexit_crit_edge" ], [ %185, %lhsNullMerge.us.i ]
  %221 = phi i64 [ %206, %"lhsNullMerge7.us.i.::HashMap<Point, int>.search.exit44.loopexit_crit_edge" ], [ %186, %lhsNullMerge.us.i ]
  %.pre-phi331 = phi i8* [ %.phi.trans.insert.phi.trans.insert326, %"lhsNullMerge7.us.i.::HashMap<Point, int>.search.exit44.loopexit_crit_edge" ], [ %179, %lhsNullMerge.us.i ]
  %222 = phi i8 [ %.pre.pre327, %"lhsNullMerge7.us.i.::HashMap<Point, int>.search.exit44.loopexit_crit_edge" ], [ %180, %lhsNullMerge.us.i ]
  %.sink202 = phi i64 [ %201, %"lhsNullMerge7.us.i.::HashMap<Point, int>.search.exit44.loopexit_crit_edge" ], [ %178, %lhsNullMerge.us.i ]
  %223 = icmp eq i8 %222, 1
  br i1 %223, label %full-replace.i8, label %notFull.i7

notFull.i7:                                       ; preds = %"::HashMap<Point, int>.search.exit44.thread", %"::HashMap<Point, int>.search.exit44"
  %224 = phi i64* [ %220, %"::HashMap<Point, int>.search.exit44.thread" ], [ %.pre-phi621, %"::HashMap<Point, int>.search.exit44" ]
  %.sink202333 = phi i64 [ %.sink202.ph, %"::HashMap<Point, int>.search.exit44.thread" ], [ %.sink202, %"::HashMap<Point, int>.search.exit44" ]
  %225 = phi i8 [ 0, %"::HashMap<Point, int>.search.exit44.thread" ], [ %222, %"::HashMap<Point, int>.search.exit44" ]
  %.pre-phi331332 = phi i8* [ %.pre-phi331.ph, %"::HashMap<Point, int>.search.exit44.thread" ], [ %.pre-phi331, %"::HashMap<Point, int>.search.exit44" ]
  store i64 %176, i64* %224, align 4
  %226 = getelementptr %"::HashMap<Point, int>::slotTy", %"::HashMap<Point, int>::slotTy"* %this.idx.val.i2, i64 %.sink202333, i32 1
  store i64 %175, i64* %226, align 4
  store i8 1, i8* %.pre-phi331332, align 1
  %227 = load %"::HashMap<Point, int>::bucketTy"*, %"::HashMap<Point, int>::bucketTy"** %this.idx1.i3, align 8
  %228 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %227, i64 %176, i32 0
  store %Point* %key, %Point** %228, align 8
  %229 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %227, i64 %176, i32 1
  store i32 %value, i32* %229, align 4
  %230 = add i64 %176, 1
  store i64 %230, i64* %4, align 4
  %231 = icmp eq i8 %225, 2
  br i1 %231, label %decrease-deletedCount.i9, label %nullMerge

full-replace.i8:                                  ; preds = %"::HashMap<Point, int>.search.exit44"
  %232 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i4, i64 %221, i32 0
  store %Point* %key, %Point** %232, align 8
  %233 = getelementptr %"::HashMap<Point, int>::bucketTy", %"::HashMap<Point, int>::bucketTy"* %this.idx1.val.i4, i64 %221, i32 1
  store i32 %value, i32* %233, align 4
  br label %nullMerge

decrease-deletedCount.i9:                         ; preds = %notFull.i7
  %234 = load i64, i64* %6, align 4
  %235 = add i64 %234, -1
  store i64 %235, i64* %6, align 4
  br label %nullMerge

nullMerge:                                        ; preds = %decrease-deletedCount.i9, %full-replace.i8, %notFull.i7, %decrease-deletedCount.i, %full-replace.i, %notFull.i
  ret void
}

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

; Function Attrs: noreturn uwtable
declare void @throwException(i8* readonly, i64) local_unnamed_addr #6

declare void @cprint(i8* nocapture readonly, i64) local_unnamed_addr

; Function Attrs: argmemonly nounwind
declare void @llvm.memset.p0i8.i64(i8* nocapture writeonly, i8, i64, i1) #2

declare void @strmulticoncat(%string* nocapture readonly, i64, %string* nocapture writeonly) local_unnamed_addr

attributes #0 = { norecurse nounwind }
attributes #1 = { norecurse nounwind readonly }
attributes #2 = { argmemonly nounwind }
attributes #3 = { inaccessiblememonly nounwind }
attributes #4 = { norecurse }
attributes #5 = { nounwind readnone }
attributes #6 = { noreturn uwtable }
