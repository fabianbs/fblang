; ModuleID = 'IntegratedHashMapTest'
source_filename = "IntegratedHashMapTest.fbs"

%string = type { i8*, i64 }
%"::HashMap<string, int>" = type { %"::HashMap<string, int>::slotTy"*, %"::HashMap<string, int>::bucketTy"*, i64, i64, i64 }
%"::HashMap<string, int>::slotTy" = type { i64, i64, i8 }
%"::HashMap<string, int>::bucketTy" = type { %string, i32 }

@0 = private unnamed_addr constant [5 x i8] c"eins\00"
@1 = private unnamed_addr constant [5 x i8] c"zwei\00"
@2 = private unnamed_addr constant [6 x i8] c"f\C3\BCnf\00"
@3 = private unnamed_addr constant [6 x i8] c"sechs\00"
@4 = private unnamed_addr constant [29 x i8] c"map.getOrElse(\22drei\22, -1) = \00"
@5 = private unnamed_addr constant [5 x i8] c"drei\00"
@6 = private unnamed_addr constant [16 x i8] c"map[\22f\C3\BCnf\22] = \00"
@7 = private unnamed_addr constant [34 x i8] c"Key not found in associated array\00"
@8 = private unnamed_addr constant [15 x i8] c"map[\22drei\22] = \00"
@9 = private unnamed_addr constant [5 x i8] c" => \00"

define void @main() local_unnamed_addr {
entry:
  %tmp = alloca <2 x i64>, align 16
  %tmpcast = bitcast <2 x i64>* %tmp to %string*
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %gc_new89 = alloca %"::HashMap<string, int>", align 16
  %gc_new89.repack105 = getelementptr inbounds %"::HashMap<string, int>", %"::HashMap<string, int>"* %gc_new89, i64 0, i32 2
  %gc_new89.repack113 = getelementptr inbounds %"::HashMap<string, int>", %"::HashMap<string, int>"* %gc_new89, i64 0, i32 3
  %gc_new89.repack121 = getelementptr inbounds %"::HashMap<string, int>", %"::HashMap<string, int>"* %gc_new89, i64 0, i32 4
  %0 = bitcast i64* %gc_new89.repack113 to i8*
  call void @llvm.memset.p0i8.i64(i8* nonnull align 8 %0, i8 0, i64 16, i1 false)
  %gc_new439 = alloca [100 x i8], align 1
  %gc_new439.repack = getelementptr inbounds [100 x i8], [100 x i8]* %gc_new439, i64 0, i64 0
  %gc_new438539 = alloca [120 x i8], align 1
  %gc_new438539.repack = getelementptr inbounds [120 x i8], [120 x i8]* %gc_new438539, i64 0, i64 0
  call void @llvm.memset.p0i8.i64(i8* nonnull align 1 %gc_new439.repack, i8 0, i64 100, i1 false)
  %1 = ptrtoint [100 x i8]* %gc_new439 to i64
  %2 = insertelement <2 x i64> undef, i64 %1, i32 0
  %3 = ptrtoint [120 x i8]* %gc_new438539 to i64
  %4 = insertelement <2 x i64> %2, i64 %3, i32 1
  %5 = bitcast %"::HashMap<string, int>"* %gc_new89 to <2 x i64>*
  call void @llvm.memset.p0i8.i64(i8* nonnull align 1 %gc_new438539.repack, i8 0, i64 120, i1 false)
  store <2 x i64> %4, <2 x i64>* %5, align 16
  store i64 5, i64* %gc_new89.repack105, align 16
  call fastcc void @"_Z34::HashMap<string, int>.operator []:s,i->i"(%"::HashMap<string, int>"* nonnull %gc_new89, %string { i8* getelementptr inbounds ([5 x i8], [5 x i8]* @0, i32 0, i32 0), i64 4 }, i32 1)
  call fastcc void @"_Z34::HashMap<string, int>.operator []:s,i->i"(%"::HashMap<string, int>"* nonnull %gc_new89, %string { i8* getelementptr inbounds ([5 x i8], [5 x i8]* @1, i32 0, i32 0), i64 4 }, i32 2)
  %6 = bitcast i64* %gc_new89.repack105 to <2 x i64>*
  %7 = load <2 x i64>, <2 x i64>* %6, align 16
  %8 = extractelement <2 x i64> %7, i32 0
  %9 = extractelement <2 x i64> %7, i32 1
  %10 = uitofp i64 %8 to float
  %11 = fmul float %10, 0x3FE99999A0000000
  %12 = load i64, i64* %gc_new89.repack121, align 16
  %13 = add i64 %12, %9
  %14 = uitofp i64 %13 to float
  %15 = fcmp ugt float %11, %14
  br i1 %15, label %"entry.::HashMap<string, int>.ensureCapacity.4.exit_crit_edge.i", label %doRehash.i.i

"entry.::HashMap<string, int>.ensureCapacity.4.exit_crit_edge.i": ; preds = %entry
  %16 = load <2 x i64>, <2 x i64>* %5, align 16
  %this.idx.val.i.i.pre436 = extractelement <2 x i64> %16, i32 0
  %17 = inttoptr i64 %this.idx.val.i.i.pre436 to %"::HashMap<string, int>::slotTy"*
  %this.idx2.val.i.i.pre437 = extractelement <2 x i64> %16, i32 1
  %18 = inttoptr i64 %this.idx2.val.i.i.pre437 to %"::HashMap<string, int>::bucketTy"*
  %phitmp = add i64 %12, -1
  br label %"::HashMap<string, int>.ensureCapacity.4.exit.i"

doRehash.i.i:                                     ; preds = %entry
  %19 = icmp ult i64 %9, %12
  br i1 %19, label %doRehash.i.i.i, label %doIncreaseCapacity.i.i.i

doIncreaseCapacity.i.i.i:                         ; preds = %doRehash.i.i
  %20 = shl i64 %8, 1
  %21 = tail call i64 @nextPrime(i64 %20)
  br label %doRehash.i.i.i

doRehash.i.i.i:                                   ; preds = %doIncreaseCapacity.i.i.i, %doRehash.i.i
  %22 = phi i64 [ %8, %doRehash.i.i ], [ %21, %doIncreaseCapacity.i.i.i ]
  %23 = load <2 x i64>, <2 x i64>* %5, align 16
  %24 = extractelement <2 x i64> %23, i32 0
  %25 = inttoptr i64 %24 to %"::HashMap<string, int>::slotTy"*
  %26 = extractelement <2 x i64> %23, i32 1
  %27 = inttoptr i64 %26 to %"::HashMap<string, int>::bucketTy"*
  %28 = mul i64 %22, 20
  %29 = tail call i8* @gc_new(i64 %28)
  %30 = mul i64 %22, 24
  %31 = tail call i8* @gc_new(i64 %30)
  %32 = ptrtoint i8* %29 to i64
  %33 = insertelement <2 x i64> undef, i64 %32, i32 0
  %34 = ptrtoint i8* %31 to i64
  %35 = insertelement <2 x i64> %33, i64 %34, i32 1
  store <2 x i64> %35, <2 x i64>* %5, align 16
  %36 = mul i64 %9, 24
  %37 = inttoptr i64 %26 to i8*
  tail call void @llvm.memcpy.p0i8.p0i8.i64(i8* nonnull align 1 %31, i8* align 1 %37, i64 %36, i1 false)
  %38 = icmp eq i64 %9, 0
  %39 = bitcast i8* %29 to %"::HashMap<string, int>::slotTy"*
  %40 = bitcast i8* %31 to %"::HashMap<string, int>::bucketTy"*
  br i1 %38, label %"::HashMap<string, int>.rehash.3.exit.i.i", label %loop.i.i.i

loop.i.i.i:                                       ; preds = %doRehash.i.i.i, %loopCond.i.i.i
  %41 = phi i64 [ %55, %loopCond.i.i.i ], [ 0, %doRehash.i.i.i ]
  %42 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %25, i64 %41, i32 2
  %43 = load i8, i8* %42, align 1
  %44 = icmp eq i8 %43, 1
  br i1 %44, label %full.i.i.i, label %loopCond.i.i.i

full.i.i.i:                                       ; preds = %loop.i.i.i
  %45 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %25, i64 %41, i32 0
  %46 = load i64, i64* %45, align 4
  %47 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %25, i64 %41, i32 1
  %48 = load i64, i64* %47, align 4
  %.elt.i.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %27, i64 %46, i32 0, i32 0
  %.unpack.i.i.i = load i8*, i8** %.elt.i.i.i, align 8
  %49 = insertvalue %string undef, i8* %.unpack.i.i.i, 0
  %.elt1.i.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %27, i64 %46, i32 0, i32 1
  %.unpack2.i.i.i = load i64, i64* %.elt1.i.i.i, align 8
  %50 = insertvalue %string %49, i64 %.unpack2.i.i.i, 1
  %51 = tail call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* nonnull %39, %"::HashMap<string, int>::bucketTy"* nonnull %40, i64 %8, %string %50, i64 %48)
  %52 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %51, i64 0, i32 0
  store i64 %46, i64* %52, align 4
  %53 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %51, i64 0, i32 1
  store i64 %48, i64* %53, align 4
  %54 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %51, i64 0, i32 2
  store i8 1, i8* %54, align 1
  br label %loopCond.i.i.i

loopCond.i.i.i:                                   ; preds = %full.i.i.i, %loop.i.i.i
  %55 = add nuw i64 %41, 1
  %56 = icmp ult i64 %55, %22
  br i1 %56, label %loop.i.i.i, label %"::HashMap<string, int>.rehash.3.exit.i.i"

"::HashMap<string, int>.rehash.3.exit.i.i":       ; preds = %loopCond.i.i.i, %doRehash.i.i.i
  store i64 0, i64* %gc_new89.repack121, align 16
  br label %"::HashMap<string, int>.ensureCapacity.4.exit.i"

"::HashMap<string, int>.ensureCapacity.4.exit.i": ; preds = %"::HashMap<string, int>.rehash.3.exit.i.i", %"entry.::HashMap<string, int>.ensureCapacity.4.exit_crit_edge.i"
  %57 = phi i64 [ %phitmp, %"entry.::HashMap<string, int>.ensureCapacity.4.exit_crit_edge.i" ], [ -1, %"::HashMap<string, int>.rehash.3.exit.i.i" ]
  %58 = phi %"::HashMap<string, int>::bucketTy"* [ %18, %"entry.::HashMap<string, int>.ensureCapacity.4.exit_crit_edge.i" ], [ %40, %"::HashMap<string, int>.rehash.3.exit.i.i" ]
  %this.idx.val.i.i = phi %"::HashMap<string, int>::slotTy"* [ %17, %"entry.::HashMap<string, int>.ensureCapacity.4.exit_crit_edge.i" ], [ %39, %"::HashMap<string, int>.rehash.3.exit.i.i" ]
  %59 = tail call i64 @str_hash(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @2, i64 0, i64 0), i64 5)
  %60 = tail call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i.i, %"::HashMap<string, int>::bucketTy"* %58, i64 %8, %string { i8* getelementptr inbounds ([6 x i8], [6 x i8]* @2, i32 0, i32 0), i64 5 }, i64 %59)
  %61 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %60, i64 0, i32 2
  %62 = load i8, i8* %61, align 1
  %63 = icmp eq i8 %62, 1
  br i1 %63, label %"_Z29::HashMap<string, int>.insert:s,i,b->b.exit", label %notFull.i.i

notFull.i.i:                                      ; preds = %"::HashMap<string, int>.ensureCapacity.4.exit.i"
  %64 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %60, i64 0, i32 0
  store i64 %9, i64* %64, align 4
  %65 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %60, i64 0, i32 1
  store i64 %59, i64* %65, align 4
  store i8 1, i8* %61, align 1
  %.repack.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %58, i64 %9, i32 0, i32 0
  store i8* getelementptr inbounds ([6 x i8], [6 x i8]* @2, i64 0, i64 0), i8** %.repack.i.i, align 8
  %.repack1.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %58, i64 %9, i32 0, i32 1
  store i64 5, i64* %.repack1.i.i, align 8
  %66 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %58, i64 %9, i32 1
  store i32 5, i32* %66, align 4
  %67 = add i64 %9, 1
  store i64 %67, i64* %gc_new89.repack113, align 8
  %68 = icmp eq i8 %62, 2
  br i1 %68, label %decrease-deletedCount.i.i, label %"_Z29::HashMap<string, int>.insert:s,i,b->b.exit"

decrease-deletedCount.i.i:                        ; preds = %notFull.i.i
  store i64 %57, i64* %gc_new89.repack121, align 16
  br label %"_Z29::HashMap<string, int>.insert:s,i,b->b.exit"

"_Z29::HashMap<string, int>.insert:s,i,b->b.exit": ; preds = %"::HashMap<string, int>.ensureCapacity.4.exit.i", %notFull.i.i, %decrease-deletedCount.i.i
  call fastcc void @"_Z34::HashMap<string, int>.operator []:s,i->i"(%"::HashMap<string, int>"* nonnull %gc_new89, %string { i8* getelementptr inbounds ([6 x i8], [6 x i8]* @3, i32 0, i32 0), i64 5 }, i32 6)
  tail call void @cprint(i8* getelementptr inbounds ([29 x i8], [29 x i8]* @4, i64 0, i64 0), i64 28)
  %69 = load <2 x i64>, <2 x i64>* %5, align 16
  %.idx.val434 = extractelement <2 x i64> %69, i32 0
  %70 = inttoptr i64 %.idx.val434 to %"::HashMap<string, int>::slotTy"*
  %.idx37.val435 = extractelement <2 x i64> %69, i32 1
  %71 = inttoptr i64 %.idx37.val435 to %"::HashMap<string, int>::bucketTy"*
  %.idx38.val = load i64, i64* %gc_new89.repack105, align 16
  %72 = tail call i64 @str_hash(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i64 0, i64 0), i64 4)
  %73 = urem i64 %72, %.idx38.val
  br label %firstLoop.i.i

firstLoop.i.i:                                    ; preds = %stateFreeMerge.i.i, %"_Z29::HashMap<string, int>.insert:s,i,b->b.exit"
  %74 = phi i64 [ %73, %"_Z29::HashMap<string, int>.insert:s,i,b->b.exit" ], [ %81, %stateFreeMerge.i.i ]
  %75 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %70, i64 %74, i32 2
  %76 = load i8, i8* %75, align 1
  %77 = icmp eq i8 %76, 0
  br i1 %77, label %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit", label %stateNotFree.i.i

stateNotFree.i.i:                                 ; preds = %firstLoop.i.i
  %78 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %70, i64 %74, i32 1
  %79 = load i64, i64* %78, align 4
  %80 = icmp eq i64 %79, %72
  br i1 %80, label %hashCodesEqual.i.i, label %stateFreeMerge.i.i

stateFreeMerge.i.i:                               ; preds = %hashCodesEqual.i.i, %stateNotFree.i.i
  %81 = add i64 %74, 1
  %82 = icmp ult i64 %81, %.idx38.val
  br i1 %82, label %firstLoop.i.i, label %secondLoop.preheader.i.i

secondLoop.preheader.i.i:                         ; preds = %stateFreeMerge.i.i
  %83 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %70, i64 0, i32 2
  %84 = load i8, i8* %83, align 1
  %85 = icmp eq i8 %84, 0
  br i1 %85, label %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit", label %stateNotFree2.i.i

hashCodesEqual.i.i:                               ; preds = %stateNotFree.i.i
  %86 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %70, i64 %74, i32 0
  %87 = load i64, i64* %86, align 4
  %.elt3.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %71, i64 %87, i32 0, i32 0
  %.unpack4.i.i = load i8*, i8** %.elt3.i.i, align 8
  %.elt5.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %71, i64 %87, i32 0, i32 1
  %.unpack6.i.i = load i64, i64* %.elt5.i.i, align 8
  %88 = tail call i1 @strequals(i8* %.unpack4.i.i, i64 %.unpack6.i.i, i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i64 0, i64 0), i64 4)
  br i1 %88, label %"::HashMap<string, int>.search.6.exit.i", label %stateFreeMerge.i.i

stateNotFree2.i.i:                                ; preds = %secondLoop.preheader.i.i, %stateFreeMerge3.i.i
  %.pre.pre = phi i8 [ %95, %stateFreeMerge3.i.i ], [ %84, %secondLoop.preheader.i.i ]
  %89 = phi i64 [ %93, %stateFreeMerge3.i.i ], [ 0, %secondLoop.preheader.i.i ]
  %90 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %70, i64 %89, i32 1
  %91 = load i64, i64* %90, align 4
  %92 = icmp eq i64 %91, %72
  br i1 %92, label %hashCodesEqual4.i.i, label %stateFreeMerge3.i.i

stateFreeMerge3.i.i:                              ; preds = %hashCodesEqual4.i.i, %stateNotFree2.i.i
  %93 = add i64 %89, 1
  %94 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %70, i64 %93, i32 2
  %95 = load i8, i8* %94, align 1
  %96 = icmp eq i8 %95, 0
  br i1 %96, label %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit", label %stateNotFree2.i.i

hashCodesEqual4.i.i:                              ; preds = %stateNotFree2.i.i
  %97 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %70, i64 %89, i32 0
  %98 = load i64, i64* %97, align 4
  %.elt.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %71, i64 %98, i32 0, i32 0
  %.unpack.i.i = load i8*, i8** %.elt.i.i, align 8
  %.elt1.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %71, i64 %98, i32 0, i32 1
  %.unpack2.i.i = load i64, i64* %.elt1.i.i, align 8
  %99 = tail call i1 @strequals(i8* %.unpack.i.i, i64 %.unpack2.i.i, i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i64 0, i64 0), i64 4)
  br i1 %99, label %"::HashMap<string, int>.search.6.exit.i", label %stateFreeMerge3.i.i

"::HashMap<string, int>.search.6.exit.i":         ; preds = %hashCodesEqual.i.i, %hashCodesEqual4.i.i
  %100 = phi i64 [ %98, %hashCodesEqual4.i.i ], [ %87, %hashCodesEqual.i.i ]
  %101 = phi i8 [ %.pre.pre, %hashCodesEqual4.i.i ], [ %76, %hashCodesEqual.i.i ]
  %102 = icmp eq i8 %101, 1
  br i1 %102, label %full.i, label %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit"

full.i:                                           ; preds = %"::HashMap<string, int>.search.6.exit.i"
  %103 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %71, i64 %100, i32 1
  %104 = load i32, i32* %103, align 4
  br label %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit"

"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit": ; preds = %firstLoop.i.i, %stateFreeMerge3.i.i, %secondLoop.preheader.i.i, %"::HashMap<string, int>.search.6.exit.i", %full.i
  %105 = phi i32 [ %104, %full.i ], [ -1, %"::HashMap<string, int>.search.6.exit.i" ], [ -1, %secondLoop.preheader.i.i ], [ -1, %stateFreeMerge3.i.i ], [ -1, %firstLoop.i.i ]
  call void @to_str(i32 %105, %string* nonnull %tmpcast)
  %106 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load426 = extractelement <2 x i64> %106, i32 0
  %107 = inttoptr i64 %.fca.0.load426 to i8*
  %.fca.1.load427 = extractelement <2 x i64> %106, i32 1
  tail call void @cprintln(i8* %107, i64 %.fca.1.load427)
  tail call void @cprint(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @6, i64 0, i64 0), i64 15)
  %108 = tail call fastcc i32* @"_Z34::HashMap<string, int>.operator []:s->i"(%"::HashMap<string, int>::slotTy"* nonnull %70, %"::HashMap<string, int>::bucketTy"* %71, i64 %.idx38.val, %string { i8* getelementptr inbounds ([6 x i8], [6 x i8]* @2, i32 0, i32 0), i64 5 })
  %109 = load i32, i32* %108, align 4
  call void @to_str(i32 %109, %string* nonnull %tmpcast)
  %110 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load12428 = extractelement <2 x i64> %110, i32 0
  %111 = inttoptr i64 %.fca.0.load12428 to i8*
  %.fca.1.load15429 = extractelement <2 x i64> %110, i32 1
  tail call void @cprintln(i8* %111, i64 %.fca.1.load15429)
  call fastcc void @"_Z34::HashMap<string, int>.operator []:s,i->i"(%"::HashMap<string, int>"* nonnull %gc_new89, %string { i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i32 0, i32 0), i64 4 }, i32 33)
  tail call void @cprint(i8* getelementptr inbounds ([15 x i8], [15 x i8]* @8, i64 0, i64 0), i64 14)
  %112 = load <2 x i64>, <2 x i64>* %5, align 16
  %.idx39.val432 = extractelement <2 x i64> %112, i32 0
  %113 = inttoptr i64 %.idx39.val432 to %"::HashMap<string, int>::slotTy"*
  %.idx40.val433 = extractelement <2 x i64> %112, i32 1
  %114 = inttoptr i64 %.idx40.val433 to %"::HashMap<string, int>::bucketTy"*
  %.idx41.val = load i64, i64* %gc_new89.repack105, align 16
  %115 = tail call fastcc i32* @"_Z34::HashMap<string, int>.operator []:s->i"(%"::HashMap<string, int>::slotTy"* %113, %"::HashMap<string, int>::bucketTy"* %114, i64 %.idx41.val, %string { i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i32 0, i32 0), i64 4 })
  %116 = load i32, i32* %115, align 4
  call void @to_str(i32 %116, %string* nonnull %tmpcast)
  %117 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load18430 = extractelement <2 x i64> %117, i32 0
  %118 = inttoptr i64 %.fca.0.load18430 to i8*
  %.fca.1.load21431 = extractelement <2 x i64> %117, i32 1
  tail call void @cprintln(i8* %118, i64 %.fca.1.load21431)
  %.idx48.val = load i64, i64* %gc_new89.repack113, align 8
  %119 = icmp eq i64 %.idx48.val, 0
  br i1 %119, label %loopEnd, label %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit"

"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit": ; preds = %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit"
  %.elt.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %114, i64 0, i32 0, i32 0
  %.elt1.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %114, i64 0, i32 0, i32 1
  %120 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %114, i64 0, i32 1
  br label %loop

loop:                                             ; preds = %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit57", %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit"
  %"%value.1.in" = phi i32* [ %120, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit" ], [ %124, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit57" ]
  %"%key.sroa.6.1.in" = phi i64* [ %.elt1.i, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit" ], [ %.elt1.i51, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit57" ]
  %"%key.sroa.0.1.in" = phi i8** [ %.elt.i, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit" ], [ %.elt.i49, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit57" ]
  %"%state.1" = phi i64 [ 1, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit" ], [ %125, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit57" ]
  %"%key.sroa.0.1" = load i8*, i8** %"%key.sroa.0.1.in", align 8
  %"%key.sroa.6.1" = load i64, i64* %"%key.sroa.6.1.in", align 8
  %"%value.1" = load i32, i32* %"%value.1.in", align 4
  tail call void @cprint(i8* %"%key.sroa.0.1", i64 %"%key.sroa.6.1")
  tail call void @cprint(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @9, i64 0, i64 0), i64 4)
  call void @to_str(i32 %"%value.1", %string* nonnull %tmpcast)
  %121 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load24424 = extractelement <2 x i64> %121, i32 0
  %122 = inttoptr i64 %.fca.0.load24424 to i8*
  %.fca.1.load27425 = extractelement <2 x i64> %121, i32 1
  tail call void @cprintln(i8* %122, i64 %.fca.1.load27425)
  %123 = icmp ult i64 %"%state.1", %.idx48.val
  br i1 %123, label %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit57", label %loopEnd

"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit57": ; preds = %loop
  %.elt.i49 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %114, i64 %"%state.1", i32 0, i32 0
  %.elt1.i51 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %114, i64 %"%state.1", i32 0, i32 1
  %124 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %114, i64 %"%state.1", i32 1
  %125 = add i64 %"%state.1", 1
  br label %loop

loopEnd:                                          ; preds = %loop, %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit"
  ret void
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #0

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #0

; Function Attrs: norecurse
declare noalias nonnull i8* @gc_new(i64) local_unnamed_addr #1

; Function Attrs: argmemonly readonly
define internal fastcc nonnull %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* readonly %this.0.0.val, %"::HashMap<string, int>::bucketTy"* nocapture readonly %this.0.1.val, i64 %this.0.2.val, %string %key, i64 %hashCode) unnamed_addr #2 {
entry:
  %0 = urem i64 %hashCode, %this.0.2.val
  %1 = extractvalue %string %key, 0
  %2 = extractvalue %string %key, 1
  br label %firstLoop

firstLoop:                                        ; preds = %entry, %stateFreeMerge
  %3 = phi i64 [ %0, %entry ], [ %11, %stateFreeMerge ]
  %4 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %3, i32 2
  %5 = load i8, i8* %4, align 1
  %6 = icmp eq i8 %5, 0
  br i1 %6, label %stateFree, label %stateNotFree

stateFree:                                        ; preds = %firstLoop
  %7 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %3
  ret %"::HashMap<string, int>::slotTy"* %7

stateNotFree:                                     ; preds = %firstLoop
  %8 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %3, i32 1
  %9 = load i64, i64* %8, align 4
  %10 = icmp eq i64 %9, %hashCode
  br i1 %10, label %hashCodesEqual, label %stateFreeMerge

stateFreeMerge:                                   ; preds = %hashCodesEqual, %stateNotFree
  %11 = add i64 %3, 1
  %12 = icmp ult i64 %11, %this.0.2.val
  br i1 %12, label %firstLoop, label %secondLoop.preheader

secondLoop.preheader:                             ; preds = %stateFreeMerge
  %13 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 0, i32 2
  %14 = load i8, i8* %13, align 1
  %15 = icmp eq i8 %14, 0
  br i1 %15, label %stateFree1, label %stateNotFree2

hashCodesEqual:                                   ; preds = %stateNotFree
  %16 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %3, i32 0
  %17 = load i64, i64* %16, align 4
  %.elt8 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.0.1.val, i64 %17, i32 0, i32 0
  %.unpack9 = load i8*, i8** %.elt8, align 8
  %.elt10 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.0.1.val, i64 %17, i32 0, i32 1
  %.unpack11 = load i64, i64* %.elt10, align 8
  %18 = tail call i1 @strequals(i8* %.unpack9, i64 %.unpack11, i8* %1, i64 %2)
  br i1 %18, label %keysEqual, label %stateFreeMerge

keysEqual:                                        ; preds = %hashCodesEqual
  %19 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %3
  ret %"::HashMap<string, int>::slotTy"* %19

stateFree1:                                       ; preds = %stateFreeMerge3, %secondLoop.preheader
  %.lcssa = phi i64 [ 0, %secondLoop.preheader ], [ %25, %stateFreeMerge3 ]
  %20 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %.lcssa
  ret %"::HashMap<string, int>::slotTy"* %20

stateNotFree2:                                    ; preds = %secondLoop.preheader, %stateFreeMerge3
  %21 = phi i64 [ %25, %stateFreeMerge3 ], [ 0, %secondLoop.preheader ]
  %22 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %21, i32 1
  %23 = load i64, i64* %22, align 4
  %24 = icmp eq i64 %23, %hashCode
  br i1 %24, label %hashCodesEqual4, label %stateFreeMerge3

stateFreeMerge3:                                  ; preds = %hashCodesEqual4, %stateNotFree2
  %25 = add i64 %21, 1
  %26 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %25, i32 2
  %27 = load i8, i8* %26, align 1
  %28 = icmp eq i8 %27, 0
  br i1 %28, label %stateFree1, label %stateNotFree2

hashCodesEqual4:                                  ; preds = %stateNotFree2
  %29 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %21, i32 0
  %30 = load i64, i64* %29, align 4
  %.elt = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.0.1.val, i64 %30, i32 0, i32 0
  %.unpack = load i8*, i8** %.elt, align 8
  %.elt6 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.0.1.val, i64 %30, i32 0, i32 1
  %.unpack7 = load i64, i64* %.elt6, align 8
  %31 = tail call i1 @strequals(i8* %.unpack, i64 %.unpack7, i8* %1, i64 %2)
  br i1 %31, label %keysEqual5, label %stateFreeMerge3

keysEqual5:                                       ; preds = %hashCodesEqual4
  %32 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %21
  ret %"::HashMap<string, int>::slotTy"* %32
}

; Function Attrs: argmemonly
declare i1 @strequals(i8* nocapture readonly, i64, i8* nocapture readonly, i64) local_unnamed_addr #3

; Function Attrs: nounwind readnone
declare i64 @nextPrime(i64) local_unnamed_addr #4

; Function Attrs: argmemonly nounwind
declare void @llvm.memcpy.p0i8.p0i8.i64(i8* nocapture writeonly, i8* nocapture readonly, i64, i1) #5

define internal fastcc void @"_Z34::HashMap<string, int>.operator []:s,i->i"(%"::HashMap<string, int>"* nocapture %this, %string %key, i32 %value) unnamed_addr {
entry:
  %0 = getelementptr %"::HashMap<string, int>", %"::HashMap<string, int>"* %this, i64 0, i32 2
  %1 = load i64, i64* %0, align 4
  %2 = uitofp i64 %1 to float
  %3 = fmul float %2, 0x3FE99999A0000000
  %4 = getelementptr %"::HashMap<string, int>", %"::HashMap<string, int>"* %this, i64 0, i32 3
  %5 = load i64, i64* %4, align 4
  %6 = getelementptr %"::HashMap<string, int>", %"::HashMap<string, int>"* %this, i64 0, i32 4
  %7 = load i64, i64* %6, align 4
  %8 = add i64 %7, %5
  %9 = uitofp i64 %8 to float
  %10 = fcmp ugt float %3, %9
  br i1 %10, label %"entry.::HashMap<string, int>.ensureCapacity.exit_crit_edge", label %doRehash.i

"entry.::HashMap<string, int>.ensureCapacity.exit_crit_edge": ; preds = %entry
  %.pre2 = getelementptr %"::HashMap<string, int>", %"::HashMap<string, int>"* %this, i64 0, i32 0
  %.pre3 = getelementptr %"::HashMap<string, int>", %"::HashMap<string, int>"* %this, i64 0, i32 1
  br label %"::HashMap<string, int>.ensureCapacity.exit"

doRehash.i:                                       ; preds = %entry
  %11 = icmp ult i64 %5, %7
  br i1 %11, label %doRehash.i.i, label %doIncreaseCapacity.i.i

doIncreaseCapacity.i.i:                           ; preds = %doRehash.i
  %12 = shl i64 %1, 1
  %13 = tail call i64 @nextPrime(i64 %12)
  br label %doRehash.i.i

doRehash.i.i:                                     ; preds = %doIncreaseCapacity.i.i, %doRehash.i
  %14 = phi i64 [ %1, %doRehash.i ], [ %13, %doIncreaseCapacity.i.i ]
  %15 = getelementptr %"::HashMap<string, int>", %"::HashMap<string, int>"* %this, i64 0, i32 0
  %16 = load %"::HashMap<string, int>::slotTy"*, %"::HashMap<string, int>::slotTy"** %15, align 8
  %17 = getelementptr %"::HashMap<string, int>", %"::HashMap<string, int>"* %this, i64 0, i32 1
  %18 = load %"::HashMap<string, int>::bucketTy"*, %"::HashMap<string, int>::bucketTy"** %17, align 8
  %19 = mul i64 %14, 20
  %20 = tail call i8* @gc_new(i64 %19)
  %21 = mul i64 %14, 24
  %22 = tail call i8* @gc_new(i64 %21)
  %23 = bitcast %"::HashMap<string, int>"* %this to i8**
  store i8* %20, i8** %23, align 8
  %24 = bitcast %"::HashMap<string, int>::bucketTy"** %17 to i8**
  store i8* %22, i8** %24, align 8
  %25 = mul i64 %5, 24
  %26 = bitcast %"::HashMap<string, int>::bucketTy"* %18 to i8*
  tail call void @llvm.memcpy.p0i8.p0i8.i64(i8* nonnull align 1 %22, i8* align 1 %26, i64 %25, i1 false)
  %27 = icmp eq i64 %5, 0
  br i1 %27, label %"::HashMap<string, int>.rehash.exit.i", label %loop.i.i

loop.i.i:                                         ; preds = %doRehash.i.i, %loopCond.i.i
  %28 = phi i64 [ %42, %loopCond.i.i ], [ 0, %doRehash.i.i ]
  %29 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %16, i64 %28, i32 2
  %30 = load i8, i8* %29, align 1
  %31 = icmp eq i8 %30, 1
  br i1 %31, label %full.i.i, label %loopCond.i.i

full.i.i:                                         ; preds = %loop.i.i
  %32 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %16, i64 %28, i32 0
  %33 = load i64, i64* %32, align 4
  %34 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %16, i64 %28, i32 1
  %35 = load i64, i64* %34, align 4
  %.elt.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %18, i64 %33, i32 0, i32 0
  %.unpack.i.i = load i8*, i8** %.elt.i.i, align 8
  %36 = insertvalue %string undef, i8* %.unpack.i.i, 0
  %.elt1.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %18, i64 %33, i32 0, i32 1
  %.unpack2.i.i = load i64, i64* %.elt1.i.i, align 8
  %37 = insertvalue %string %36, i64 %.unpack2.i.i, 1
  %this.idx.val.i.i = load %"::HashMap<string, int>::slotTy"*, %"::HashMap<string, int>::slotTy"** %15, align 8
  %this.idx3.val.i.i = load %"::HashMap<string, int>::bucketTy"*, %"::HashMap<string, int>::bucketTy"** %17, align 8
  %this.idx4.val.i.i = load i64, i64* %0, align 4
  %38 = tail call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i.i, %"::HashMap<string, int>::bucketTy"* %this.idx3.val.i.i, i64 %this.idx4.val.i.i, %string %37, i64 %35)
  %39 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %38, i64 0, i32 0
  store i64 %33, i64* %39, align 4
  %40 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %38, i64 0, i32 1
  store i64 %35, i64* %40, align 4
  %41 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %38, i64 0, i32 2
  store i8 1, i8* %41, align 1
  br label %loopCond.i.i

loopCond.i.i:                                     ; preds = %full.i.i, %loop.i.i
  %42 = add nuw i64 %28, 1
  %43 = icmp ult i64 %42, %14
  br i1 %43, label %loop.i.i, label %"::HashMap<string, int>.rehash.exit.i"

"::HashMap<string, int>.rehash.exit.i":           ; preds = %loopCond.i.i, %doRehash.i.i
  store i64 0, i64* %6, align 4
  %.pre = load i64, i64* %4, align 4
  %this.idx8.val.i.pre = load i64, i64* %0, align 4
  br label %"::HashMap<string, int>.ensureCapacity.exit"

"::HashMap<string, int>.ensureCapacity.exit":     ; preds = %"entry.::HashMap<string, int>.ensureCapacity.exit_crit_edge", %"::HashMap<string, int>.rehash.exit.i"
  %this.idx7.i.pre-phi = phi %"::HashMap<string, int>::bucketTy"** [ %.pre3, %"entry.::HashMap<string, int>.ensureCapacity.exit_crit_edge" ], [ %17, %"::HashMap<string, int>.rehash.exit.i" ]
  %this.idx.i.pre-phi = phi %"::HashMap<string, int>::slotTy"** [ %.pre2, %"entry.::HashMap<string, int>.ensureCapacity.exit_crit_edge" ], [ %15, %"::HashMap<string, int>.rehash.exit.i" ]
  %this.idx8.val.i = phi i64 [ %1, %"entry.::HashMap<string, int>.ensureCapacity.exit_crit_edge" ], [ %this.idx8.val.i.pre, %"::HashMap<string, int>.rehash.exit.i" ]
  %44 = phi i64 [ %5, %"entry.::HashMap<string, int>.ensureCapacity.exit_crit_edge" ], [ %.pre, %"::HashMap<string, int>.rehash.exit.i" ]
  %45 = extractvalue %string %key, 0
  %46 = extractvalue %string %key, 1
  %47 = tail call i64 @str_hash(i8* %45, i64 %46)
  %this.idx.val.i = load %"::HashMap<string, int>::slotTy"*, %"::HashMap<string, int>::slotTy"** %this.idx.i.pre-phi, align 8
  %this.idx7.val.i = load %"::HashMap<string, int>::bucketTy"*, %"::HashMap<string, int>::bucketTy"** %this.idx7.i.pre-phi, align 8
  %48 = tail call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i, %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i, i64 %this.idx8.val.i, %string %key, i64 %47)
  %49 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %48, i64 0, i32 2
  %50 = load i8, i8* %49, align 1
  %51 = icmp eq i8 %50, 1
  %52 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %48, i64 0, i32 0
  br i1 %51, label %full-replace.i, label %notFull.i

notFull.i:                                        ; preds = %"::HashMap<string, int>.ensureCapacity.exit"
  store i64 %44, i64* %52, align 4
  %53 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %48, i64 0, i32 1
  store i64 %47, i64* %53, align 4
  store i8 1, i8* %49, align 1
  %54 = load %"::HashMap<string, int>::bucketTy"*, %"::HashMap<string, int>::bucketTy"** %this.idx7.i.pre-phi, align 8
  %.repack.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %54, i64 %44, i32 0, i32 0
  store i8* %45, i8** %.repack.i, align 8
  %.repack1.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %54, i64 %44, i32 0, i32 1
  store i64 %46, i64* %.repack1.i, align 8
  %55 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %54, i64 %44, i32 1
  store i32 %value, i32* %55, align 4
  %56 = add i64 %44, 1
  store i64 %56, i64* %4, align 4
  %57 = icmp eq i8 %50, 2
  br i1 %57, label %decrease-deletedCount.i, label %"::HashMap<string, int>.insertInternal.exit"

full-replace.i:                                   ; preds = %"::HashMap<string, int>.ensureCapacity.exit"
  %58 = load i64, i64* %52, align 4
  %.repack3.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i, i64 %58, i32 0, i32 0
  store i8* %45, i8** %.repack3.i, align 8
  %.repack5.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i, i64 %58, i32 0, i32 1
  store i64 %46, i64* %.repack5.i, align 8
  %59 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i, i64 %58, i32 1
  store i32 %value, i32* %59, align 4
  br label %"::HashMap<string, int>.insertInternal.exit"

decrease-deletedCount.i:                          ; preds = %notFull.i
  %60 = load i64, i64* %6, align 4
  %61 = add i64 %60, -1
  store i64 %61, i64* %6, align 4
  br label %"::HashMap<string, int>.insertInternal.exit"

"::HashMap<string, int>.insertInternal.exit":     ; preds = %notFull.i, %full-replace.i, %decrease-deletedCount.i
  ret void
}

; Function Attrs: argmemonly nounwind readonly
declare i64 @str_hash(i8* nocapture, i64) local_unnamed_addr #6

declare void @cprint(i8* nocapture readonly, i64) local_unnamed_addr

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

declare void @to_str(i32, %string* nocapture writeonly) local_unnamed_addr

define internal fastcc i32* @"_Z34::HashMap<string, int>.operator []:s->i"(%"::HashMap<string, int>::slotTy"* nocapture readonly %this.0.0.val, %"::HashMap<string, int>::bucketTy"* readonly %this.0.1.val, i64 %this.0.2.val, %string %key) unnamed_addr {
entry:
  %0 = extractvalue %string %key, 0
  %1 = extractvalue %string %key, 1
  %2 = tail call i64 @str_hash(i8* %0, i64 %1)
  %3 = urem i64 %2, %this.0.2.val
  br label %firstLoop.i

firstLoop.i:                                      ; preds = %stateFreeMerge.i, %entry
  %4 = phi i64 [ %3, %entry ], [ %11, %stateFreeMerge.i ]
  %5 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %4, i32 2
  %6 = load i8, i8* %5, align 1
  %7 = icmp eq i8 %6, 0
  br i1 %7, label %notFull, label %stateNotFree.i

stateNotFree.i:                                   ; preds = %firstLoop.i
  %8 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %4, i32 1
  %9 = load i64, i64* %8, align 4
  %10 = icmp eq i64 %9, %2
  br i1 %10, label %hashCodesEqual.i, label %stateFreeMerge.i

stateFreeMerge.i:                                 ; preds = %hashCodesEqual.i, %stateNotFree.i
  %11 = add i64 %4, 1
  %12 = icmp ult i64 %11, %this.0.2.val
  br i1 %12, label %firstLoop.i, label %secondLoop.preheader.i

secondLoop.preheader.i:                           ; preds = %stateFreeMerge.i
  %13 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 0, i32 2
  %14 = load i8, i8* %13, align 1
  %15 = icmp eq i8 %14, 0
  br i1 %15, label %notFull, label %stateNotFree2.i

hashCodesEqual.i:                                 ; preds = %stateNotFree.i
  %16 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %4, i32 0
  %17 = load i64, i64* %16, align 4
  %.elt8.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.0.1.val, i64 %17, i32 0, i32 0
  %.unpack9.i = load i8*, i8** %.elt8.i, align 8
  %.elt10.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.0.1.val, i64 %17, i32 0, i32 1
  %.unpack11.i = load i64, i64* %.elt10.i, align 8
  %18 = tail call i1 @strequals(i8* %.unpack9.i, i64 %.unpack11.i, i8* %0, i64 %1)
  br i1 %18, label %"::HashMap<string, int>.search.7.exit", label %stateFreeMerge.i

stateNotFree2.i:                                  ; preds = %secondLoop.preheader.i, %stateFreeMerge3.i
  %.pre.pre = phi i8 [ %25, %stateFreeMerge3.i ], [ %14, %secondLoop.preheader.i ]
  %19 = phi i64 [ %23, %stateFreeMerge3.i ], [ 0, %secondLoop.preheader.i ]
  %20 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %19, i32 1
  %21 = load i64, i64* %20, align 4
  %22 = icmp eq i64 %21, %2
  br i1 %22, label %hashCodesEqual4.i, label %stateFreeMerge3.i

stateFreeMerge3.i:                                ; preds = %hashCodesEqual4.i, %stateNotFree2.i
  %23 = add i64 %19, 1
  %24 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %23, i32 2
  %25 = load i8, i8* %24, align 1
  %26 = icmp eq i8 %25, 0
  br i1 %26, label %notFull, label %stateNotFree2.i

hashCodesEqual4.i:                                ; preds = %stateNotFree2.i
  %27 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %this.0.0.val, i64 %19, i32 0
  %28 = load i64, i64* %27, align 4
  %.elt.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.0.1.val, i64 %28, i32 0, i32 0
  %.unpack.i = load i8*, i8** %.elt.i, align 8
  %.elt6.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.0.1.val, i64 %28, i32 0, i32 1
  %.unpack7.i = load i64, i64* %.elt6.i, align 8
  %29 = tail call i1 @strequals(i8* %.unpack.i, i64 %.unpack7.i, i8* %0, i64 %1)
  br i1 %29, label %"::HashMap<string, int>.search.7.exit", label %stateFreeMerge3.i

"::HashMap<string, int>.search.7.exit":           ; preds = %hashCodesEqual.i, %hashCodesEqual4.i
  %30 = phi i64 [ %28, %hashCodesEqual4.i ], [ %17, %hashCodesEqual.i ]
  %31 = phi i8 [ %.pre.pre, %hashCodesEqual4.i ], [ %6, %hashCodesEqual.i ]
  %32 = icmp eq i8 %31, 1
  br i1 %32, label %full, label %notFull

full:                                             ; preds = %"::HashMap<string, int>.search.7.exit"
  %33 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.0.1.val, i64 %30, i32 1
  ret i32* %33

notFull:                                          ; preds = %firstLoop.i, %stateFreeMerge3.i, %secondLoop.preheader.i, %"::HashMap<string, int>.search.7.exit"
  tail call void @throwException(i8* getelementptr inbounds ([34 x i8], [34 x i8]* @7, i64 0, i64 0), i64 33)
  unreachable
}

; Function Attrs: noreturn uwtable
declare void @throwException(i8* readonly, i64) local_unnamed_addr #7

; Function Attrs: argmemonly nounwind
declare void @llvm.memset.p0i8.i64(i8* nocapture writeonly, i8, i64, i1) #5

attributes #0 = { inaccessiblememonly nounwind }
attributes #1 = { norecurse }
attributes #2 = { argmemonly readonly }
attributes #3 = { argmemonly }
attributes #4 = { nounwind readnone }
attributes #5 = { argmemonly nounwind }
attributes #6 = { argmemonly nounwind readonly }
attributes #7 = { noreturn uwtable }
