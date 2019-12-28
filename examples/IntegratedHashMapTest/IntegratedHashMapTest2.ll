; ModuleID = 'IntegratedHashMapTest2'
source_filename = "IntegratedHashMapTest2.fbs"

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
@8 = private unnamed_addr constant [5 x i8] c" => \00"
@9 = private unnamed_addr constant [29 x i8] c"map.getOrElse(\22zwei\22, -1) = \00"

define void @main() local_unnamed_addr {
entry:
  %retPtr.i = alloca %string, align 8
  %tmp = alloca %string, align 8
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %gc_new178 = alloca %"::HashMap<string, int>", align 8
  %gc_new178.repack186 = getelementptr inbounds %"::HashMap<string, int>", %"::HashMap<string, int>"* %gc_new178, i64 0, i32 1
  %gc_new178.repack194 = getelementptr inbounds %"::HashMap<string, int>", %"::HashMap<string, int>"* %gc_new178, i64 0, i32 2
  %gc_new178.repack202 = getelementptr inbounds %"::HashMap<string, int>", %"::HashMap<string, int>"* %gc_new178, i64 0, i32 3
  %gc_new178.repack210 = getelementptr inbounds %"::HashMap<string, int>", %"::HashMap<string, int>"* %gc_new178, i64 0, i32 4
  %0 = bitcast i64* %gc_new178.repack202 to i8*
  call void @llvm.memset.p0i8.i64(i8* nonnull align 8 %0, i8 0, i64 16, i1 false)
  %1 = tail call i8* @gc_new(i64 100)
  %2 = tail call i8* @gc_new(i64 120)
  %3 = bitcast %"::HashMap<string, int>"* %gc_new178 to i8**
  store i8* %1, i8** %3, align 8
  %4 = bitcast %"::HashMap<string, int>::bucketTy"** %gc_new178.repack186 to i8**
  store i8* %2, i8** %4, align 8
  store i64 5, i64* %gc_new178.repack194, align 8
  call fastcc void @"::HashMap<string, int>.ensureCapacity"(%"::HashMap<string, int>"* nonnull %gc_new178)
  %5 = tail call i64 @str_hash(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @0, i64 0, i64 0), i64 4)
  %6 = load i64, i64* %gc_new178.repack202, align 8
  %this.idx.i.i = getelementptr inbounds %"::HashMap<string, int>", %"::HashMap<string, int>"* %gc_new178, i64 0, i32 0
  %this.idx.val.i.i = load %"::HashMap<string, int>::slotTy"*, %"::HashMap<string, int>::slotTy"** %this.idx.i.i, align 8
  %this.idx7.val.i.i = load %"::HashMap<string, int>::bucketTy"*, %"::HashMap<string, int>::bucketTy"** %gc_new178.repack186, align 8
  %this.idx8.val.i.i = load i64, i64* %gc_new178.repack194, align 8
  %7 = call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i.i, %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i, i64 %this.idx8.val.i.i, %string { i8* getelementptr inbounds ([5 x i8], [5 x i8]* @0, i32 0, i32 0), i64 4 }, i64 %5)
  %8 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %7, i64 0, i32 2
  %9 = load i8, i8* %8, align 1
  %10 = icmp eq i8 %9, 1
  %11 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %7, i64 0, i32 0
  br i1 %10, label %full.i.i, label %notFull.i.i

full.i.i:                                         ; preds = %entry
  %12 = load i64, i64* %11, align 4
  %.repack3.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i, i64 %12, i32 0, i32 0
  store i8* getelementptr inbounds ([5 x i8], [5 x i8]* @0, i64 0, i64 0), i8** %.repack3.i.i, align 8
  %.repack5.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i, i64 %12, i32 0, i32 1
  store i64 4, i64* %.repack5.i.i, align 8
  %13 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i, i64 %12, i32 1
  store i32 1, i32* %13, align 4
  br label %"_Z34::HashMap<string, int>.operator []:s,i->i.exit"

notFull.i.i:                                      ; preds = %entry
  store i64 %6, i64* %11, align 4
  %14 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %7, i64 0, i32 1
  store i64 %5, i64* %14, align 4
  store i8 1, i8* %8, align 1
  %.repack.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i, i64 %6, i32 0, i32 0
  store i8* getelementptr inbounds ([5 x i8], [5 x i8]* @0, i64 0, i64 0), i8** %.repack.i.i, align 8
  %.repack1.i.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i, i64 %6, i32 0, i32 1
  store i64 4, i64* %.repack1.i.i, align 8
  %15 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i, i64 %6, i32 1
  store i32 1, i32* %15, align 4
  %16 = add i64 %6, 1
  store i64 %16, i64* %gc_new178.repack202, align 8
  %17 = icmp eq i8 %9, 2
  br i1 %17, label %decrease-deletedCount.i.i, label %"_Z34::HashMap<string, int>.operator []:s,i->i.exit"

decrease-deletedCount.i.i:                        ; preds = %notFull.i.i
  %18 = load i64, i64* %gc_new178.repack210, align 8
  %19 = add i64 %18, -1
  store i64 %19, i64* %gc_new178.repack210, align 8
  br label %"_Z34::HashMap<string, int>.operator []:s,i->i.exit"

"_Z34::HashMap<string, int>.operator []:s,i->i.exit": ; preds = %full.i.i, %notFull.i.i, %decrease-deletedCount.i.i
  call fastcc void @"::HashMap<string, int>.ensureCapacity"(%"::HashMap<string, int>"* nonnull %gc_new178)
  %20 = tail call i64 @str_hash(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @1, i64 0, i64 0), i64 4)
  %21 = load i64, i64* %gc_new178.repack202, align 8
  %this.idx.val.i.i79 = load %"::HashMap<string, int>::slotTy"*, %"::HashMap<string, int>::slotTy"** %this.idx.i.i, align 8
  %this.idx7.val.i.i81 = load %"::HashMap<string, int>::bucketTy"*, %"::HashMap<string, int>::bucketTy"** %gc_new178.repack186, align 8
  %this.idx8.val.i.i83 = load i64, i64* %gc_new178.repack194, align 8
  %22 = call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i.i79, %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i81, i64 %this.idx8.val.i.i83, %string { i8* getelementptr inbounds ([5 x i8], [5 x i8]* @1, i32 0, i32 0), i64 4 }, i64 %20)
  %23 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %22, i64 0, i32 2
  %24 = load i8, i8* %23, align 1
  %25 = icmp eq i8 %24, 1
  %26 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %22, i64 0, i32 0
  br i1 %25, label %full.i.i86, label %notFull.i.i89

full.i.i86:                                       ; preds = %"_Z34::HashMap<string, int>.operator []:s,i->i.exit"
  %27 = load i64, i64* %26, align 4
  %.repack3.i.i84 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i81, i64 %27, i32 0, i32 0
  store i8* getelementptr inbounds ([5 x i8], [5 x i8]* @1, i64 0, i64 0), i8** %.repack3.i.i84, align 8
  %.repack5.i.i85 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i81, i64 %27, i32 0, i32 1
  store i64 4, i64* %.repack5.i.i85, align 8
  %28 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i81, i64 %27, i32 1
  store i32 2, i32* %28, align 4
  br label %"_Z34::HashMap<string, int>.operator []:s,i->i.exit91"

notFull.i.i89:                                    ; preds = %"_Z34::HashMap<string, int>.operator []:s,i->i.exit"
  store i64 %21, i64* %26, align 4
  %29 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %22, i64 0, i32 1
  store i64 %20, i64* %29, align 4
  store i8 1, i8* %23, align 1
  %.repack.i.i87 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i81, i64 %21, i32 0, i32 0
  store i8* getelementptr inbounds ([5 x i8], [5 x i8]* @1, i64 0, i64 0), i8** %.repack.i.i87, align 8
  %.repack1.i.i88 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i81, i64 %21, i32 0, i32 1
  store i64 4, i64* %.repack1.i.i88, align 8
  %30 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i81, i64 %21, i32 1
  store i32 2, i32* %30, align 4
  %31 = add i64 %21, 1
  store i64 %31, i64* %gc_new178.repack202, align 8
  %32 = icmp eq i8 %24, 2
  br i1 %32, label %decrease-deletedCount.i.i90, label %"_Z34::HashMap<string, int>.operator []:s,i->i.exit91"

decrease-deletedCount.i.i90:                      ; preds = %notFull.i.i89
  %33 = load i64, i64* %gc_new178.repack210, align 8
  %34 = add i64 %33, -1
  store i64 %34, i64* %gc_new178.repack210, align 8
  br label %"_Z34::HashMap<string, int>.operator []:s,i->i.exit91"

"_Z34::HashMap<string, int>.operator []:s,i->i.exit91": ; preds = %full.i.i86, %notFull.i.i89, %decrease-deletedCount.i.i90
  call fastcc void @"::HashMap<string, int>.ensureCapacity"(%"::HashMap<string, int>"* nonnull %gc_new178)
  %35 = tail call i64 @str_hash(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @2, i64 0, i64 0), i64 5)
  %36 = load i64, i64* %gc_new178.repack202, align 8
  %this.idx.val.i.i93 = load %"::HashMap<string, int>::slotTy"*, %"::HashMap<string, int>::slotTy"** %this.idx.i.i, align 8
  %this.idx7.val.i.i95 = load %"::HashMap<string, int>::bucketTy"*, %"::HashMap<string, int>::bucketTy"** %gc_new178.repack186, align 8
  %this.idx8.val.i.i97 = load i64, i64* %gc_new178.repack194, align 8
  %37 = call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i.i93, %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i95, i64 %this.idx8.val.i.i97, %string { i8* getelementptr inbounds ([6 x i8], [6 x i8]* @2, i32 0, i32 0), i64 5 }, i64 %35)
  %38 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %37, i64 0, i32 2
  %39 = load i8, i8* %38, align 1
  %40 = icmp eq i8 %39, 1
  br i1 %40, label %"_Z29::HashMap<string, int>.insert:s,i,b->b.exit", label %notFull.i.i100

notFull.i.i100:                                   ; preds = %"_Z34::HashMap<string, int>.operator []:s,i->i.exit91"
  %41 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %37, i64 0, i32 0
  store i64 %36, i64* %41, align 4
  %42 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %37, i64 0, i32 1
  store i64 %35, i64* %42, align 4
  store i8 1, i8* %38, align 1
  %.repack.i.i98 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i95, i64 %36, i32 0, i32 0
  store i8* getelementptr inbounds ([6 x i8], [6 x i8]* @2, i64 0, i64 0), i8** %.repack.i.i98, align 8
  %.repack1.i.i99 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i95, i64 %36, i32 0, i32 1
  store i64 5, i64* %.repack1.i.i99, align 8
  %43 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i95, i64 %36, i32 1
  store i32 5, i32* %43, align 4
  %44 = add i64 %36, 1
  store i64 %44, i64* %gc_new178.repack202, align 8
  %45 = icmp eq i8 %39, 2
  br i1 %45, label %decrease-deletedCount.i.i101, label %"_Z29::HashMap<string, int>.insert:s,i,b->b.exit"

decrease-deletedCount.i.i101:                     ; preds = %notFull.i.i100
  %46 = load i64, i64* %gc_new178.repack210, align 8
  %47 = add i64 %46, -1
  store i64 %47, i64* %gc_new178.repack210, align 8
  br label %"_Z29::HashMap<string, int>.insert:s,i,b->b.exit"

"_Z29::HashMap<string, int>.insert:s,i,b->b.exit": ; preds = %"_Z34::HashMap<string, int>.operator []:s,i->i.exit91", %notFull.i.i100, %decrease-deletedCount.i.i101
  call fastcc void @"::HashMap<string, int>.ensureCapacity"(%"::HashMap<string, int>"* nonnull %gc_new178)
  %48 = tail call i64 @str_hash(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @3, i64 0, i64 0), i64 5)
  %49 = load i64, i64* %gc_new178.repack202, align 8
  %this.idx.val.i.i103 = load %"::HashMap<string, int>::slotTy"*, %"::HashMap<string, int>::slotTy"** %this.idx.i.i, align 8
  %this.idx7.val.i.i105 = load %"::HashMap<string, int>::bucketTy"*, %"::HashMap<string, int>::bucketTy"** %gc_new178.repack186, align 8
  %this.idx8.val.i.i107 = load i64, i64* %gc_new178.repack194, align 8
  %50 = call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i.i103, %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i105, i64 %this.idx8.val.i.i107, %string { i8* getelementptr inbounds ([6 x i8], [6 x i8]* @3, i32 0, i32 0), i64 5 }, i64 %48)
  %51 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %50, i64 0, i32 2
  %52 = load i8, i8* %51, align 1
  %53 = icmp eq i8 %52, 1
  %54 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %50, i64 0, i32 0
  br i1 %53, label %full.i.i110, label %notFull.i.i113

full.i.i110:                                      ; preds = %"_Z29::HashMap<string, int>.insert:s,i,b->b.exit"
  %55 = load i64, i64* %54, align 4
  %.repack3.i.i108 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i105, i64 %55, i32 0, i32 0
  store i8* getelementptr inbounds ([6 x i8], [6 x i8]* @3, i64 0, i64 0), i8** %.repack3.i.i108, align 8
  %.repack5.i.i109 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i105, i64 %55, i32 0, i32 1
  store i64 5, i64* %.repack5.i.i109, align 8
  %56 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i105, i64 %55, i32 1
  store i32 6, i32* %56, align 4
  br label %"_Z34::HashMap<string, int>.operator []:s,i->i.exit115"

notFull.i.i113:                                   ; preds = %"_Z29::HashMap<string, int>.insert:s,i,b->b.exit"
  store i64 %49, i64* %54, align 4
  %57 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %50, i64 0, i32 1
  store i64 %48, i64* %57, align 4
  store i8 1, i8* %51, align 1
  %.repack.i.i111 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i105, i64 %49, i32 0, i32 0
  store i8* getelementptr inbounds ([6 x i8], [6 x i8]* @3, i64 0, i64 0), i8** %.repack.i.i111, align 8
  %.repack1.i.i112 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i105, i64 %49, i32 0, i32 1
  store i64 5, i64* %.repack1.i.i112, align 8
  %58 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i105, i64 %49, i32 1
  store i32 6, i32* %58, align 4
  %59 = add i64 %49, 1
  store i64 %59, i64* %gc_new178.repack202, align 8
  %60 = icmp eq i8 %52, 2
  br i1 %60, label %decrease-deletedCount.i.i114, label %"_Z34::HashMap<string, int>.operator []:s,i->i.exit115"

decrease-deletedCount.i.i114:                     ; preds = %notFull.i.i113
  %61 = load i64, i64* %gc_new178.repack210, align 8
  %62 = add i64 %61, -1
  store i64 %62, i64* %gc_new178.repack210, align 8
  br label %"_Z34::HashMap<string, int>.operator []:s,i->i.exit115"

"_Z34::HashMap<string, int>.operator []:s,i->i.exit115": ; preds = %full.i.i110, %notFull.i.i113, %decrease-deletedCount.i.i114
  tail call void @cprint(i8* getelementptr inbounds ([29 x i8], [29 x i8]* @4, i64 0, i64 0), i64 28)
  %63 = tail call i64 @str_hash(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i64 0, i64 0), i64 4)
  %64 = call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i.i103, %"::HashMap<string, int>::bucketTy"* nonnull %this.idx7.val.i.i105, i64 %this.idx8.val.i.i107, %string { i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i32 0, i32 0), i64 4 }, i64 %63)
  %65 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %64, i64 0, i32 2
  %66 = load i8, i8* %65, align 1
  %67 = icmp eq i8 %66, 1
  br i1 %67, label %full.i, label %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit"

full.i:                                           ; preds = %"_Z34::HashMap<string, int>.operator []:s,i->i.exit115"
  %68 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %64, i64 0, i32 0
  %69 = load i64, i64* %68, align 4
  %70 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i105, i64 %69, i32 1
  %71 = load i32, i32* %70, align 4
  br label %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit"

"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit": ; preds = %"_Z34::HashMap<string, int>.operator []:s,i->i.exit115", %full.i
  %72 = phi i32 [ %71, %full.i ], [ -1, %"_Z34::HashMap<string, int>.operator []:s,i->i.exit115" ]
  call void @to_str(i32 %72, %string* nonnull %tmp)
  %.fca.0.gep = getelementptr inbounds %string, %string* %tmp, i64 0, i32 0
  %.fca.0.load = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.gep = getelementptr inbounds %string, %string* %tmp, i64 0, i32 1
  %.fca.1.load = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load, i64 %.fca.1.load)
  tail call void @cprint(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @6, i64 0, i64 0), i64 15)
  %73 = tail call i64 @str_hash(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @2, i64 0, i64 0), i64 5)
  %74 = call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i.i103, %"::HashMap<string, int>::bucketTy"* nonnull %this.idx7.val.i.i105, i64 %this.idx8.val.i.i107, %string { i8* getelementptr inbounds ([6 x i8], [6 x i8]* @2, i32 0, i32 0), i64 5 }, i64 %73)
  %75 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %74, i64 0, i32 2
  %76 = load i8, i8* %75, align 1
  %77 = icmp eq i8 %76, 1
  br i1 %77, label %"_Z34::HashMap<string, int>.operator []:s->i.exit", label %notFull.i117

notFull.i117:                                     ; preds = %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit"
  tail call void @throwException(i8* getelementptr inbounds ([34 x i8], [34 x i8]* @7, i64 0, i64 0), i64 33)
  unreachable

"_Z34::HashMap<string, int>.operator []:s->i.exit": ; preds = %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit"
  %78 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %74, i64 0, i32 0
  %79 = load i64, i64* %78, align 4
  %80 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i105, i64 %79, i32 1
  %81 = load i32, i32* %80, align 4
  call void @to_str(i32 %81, %string* nonnull %tmp)
  %.fca.0.load21 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load24 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load21, i64 %.fca.1.load24)
  call fastcc void @"::HashMap<string, int>.ensureCapacity"(%"::HashMap<string, int>"* nonnull %gc_new178)
  %82 = tail call i64 @str_hash(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i64 0, i64 0), i64 4)
  %83 = load i64, i64* %gc_new178.repack202, align 8
  %this.idx.val.i.i119 = load %"::HashMap<string, int>::slotTy"*, %"::HashMap<string, int>::slotTy"** %this.idx.i.i, align 8
  %this.idx7.val.i.i121 = load %"::HashMap<string, int>::bucketTy"*, %"::HashMap<string, int>::bucketTy"** %gc_new178.repack186, align 8
  %this.idx8.val.i.i123 = load i64, i64* %gc_new178.repack194, align 8
  %84 = call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i.i119, %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %this.idx8.val.i.i123, %string { i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i32 0, i32 0), i64 4 }, i64 %82)
  %85 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %84, i64 0, i32 2
  %86 = load i8, i8* %85, align 1
  %87 = icmp eq i8 %86, 1
  %88 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %84, i64 0, i32 0
  br i1 %87, label %full.i.i126, label %notFull.i.i129

full.i.i126:                                      ; preds = %"_Z34::HashMap<string, int>.operator []:s->i.exit"
  %89 = load i64, i64* %88, align 4
  %.repack3.i.i124 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %89, i32 0, i32 0
  store i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i64 0, i64 0), i8** %.repack3.i.i124, align 8
  %.repack5.i.i125 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %89, i32 0, i32 1
  store i64 4, i64* %.repack5.i.i125, align 8
  %90 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %89, i32 1
  store i32 33, i32* %90, align 4
  br label %"_Z34::HashMap<string, int>.operator []:s,i->i.exit131"

notFull.i.i129:                                   ; preds = %"_Z34::HashMap<string, int>.operator []:s->i.exit"
  store i64 %83, i64* %88, align 4
  %91 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %84, i64 0, i32 1
  store i64 %82, i64* %91, align 4
  store i8 1, i8* %85, align 1
  %.repack.i.i127 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %83, i32 0, i32 0
  store i8* getelementptr inbounds ([5 x i8], [5 x i8]* @5, i64 0, i64 0), i8** %.repack.i.i127, align 8
  %.repack1.i.i128 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %83, i32 0, i32 1
  store i64 4, i64* %.repack1.i.i128, align 8
  %92 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %83, i32 1
  store i32 33, i32* %92, align 4
  %93 = add i64 %83, 1
  store i64 %93, i64* %gc_new178.repack202, align 8
  %94 = icmp eq i8 %86, 2
  br i1 %94, label %decrease-deletedCount.i.i130, label %"_Z34::HashMap<string, int>.operator []:s,i->i.exit131"

decrease-deletedCount.i.i130:                     ; preds = %notFull.i.i129
  %95 = load i64, i64* %gc_new178.repack210, align 8
  %96 = add i64 %95, -1
  store i64 %96, i64* %gc_new178.repack210, align 8
  br label %"_Z34::HashMap<string, int>.operator []:s,i->i.exit131"

"_Z34::HashMap<string, int>.operator []:s,i->i.exit131": ; preds = %full.i.i126, %notFull.i.i129, %decrease-deletedCount.i.i130
  %97 = phi i64 [ %83, %full.i.i126 ], [ %93, %notFull.i.i129 ], [ %93, %decrease-deletedCount.i.i130 ]
  %98 = icmp eq i64 %97, 0
  br i1 %98, label %loopEnd, label %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit"

"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit": ; preds = %"_Z34::HashMap<string, int>.operator []:s,i->i.exit131"
  %.elt.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 0, i32 0, i32 0
  %.elt1.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 0, i32 0, i32 1
  %99 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 0, i32 1
  br label %loop

loop:                                             ; preds = %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit140", %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit"
  %"%value.1.in" = phi i32* [ %99, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit" ], [ %101, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit140" ]
  %"%key.sroa.6.1.in" = phi i64* [ %.elt1.i, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit" ], [ %.elt1.i134, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit140" ]
  %"%key.sroa.0.1.in" = phi i8** [ %.elt.i, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit" ], [ %.elt.i132, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit140" ]
  %"%state.1" = phi i64 [ 1, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit" ], [ %102, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit140" ]
  %"%key.sroa.0.1" = load i8*, i8** %"%key.sroa.0.1.in", align 8
  %"%key.sroa.6.1" = load i64, i64* %"%key.sroa.6.1.in", align 8
  %"%value.1" = load i32, i32* %"%value.1.in", align 4
  tail call void @cprint(i8* %"%key.sroa.0.1", i64 %"%key.sroa.6.1")
  tail call void @cprint(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @8, i64 0, i64 0), i64 4)
  call void @to_str(i32 %"%value.1", %string* nonnull %tmp)
  %.fca.0.load27 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load30 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load27, i64 %.fca.1.load30)
  %100 = icmp ult i64 %"%state.1", %97
  br i1 %100, label %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit140", label %loopEnd

"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit140": ; preds = %loop
  %.elt.i132 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %"%state.1", i32 0, i32 0
  %.elt1.i134 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %"%state.1", i32 0, i32 1
  %101 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %"%state.1", i32 1
  %102 = add i64 %"%state.1", 1
  br label %loop

loopEnd:                                          ; preds = %loop, %"_Z34::HashMap<string, int>.operator []:s,i->i.exit131"
  %103 = tail call i64 @str_hash(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @1, i64 0, i64 0), i64 4)
  %104 = call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i.i119, %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %this.idx8.val.i.i123, %string { i8* getelementptr inbounds ([5 x i8], [5 x i8]* @1, i32 0, i32 0), i64 4 }, i64 %103)
  %105 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %104, i64 0, i32 2
  %106 = load i8, i8* %105, align 1
  %107 = icmp eq i8 %106, 1
  br i1 %107, label %full.i145, label %"_Z29::HashMap<string, int>.remove:s->b.exit"

full.i145:                                        ; preds = %loopEnd
  %108 = load i64, i64* %gc_new178.repack210, align 8
  %109 = add i64 %108, 1
  store i64 %109, i64* %gc_new178.repack210, align 8
  %110 = add i64 %97, -1
  store i64 %110, i64* %gc_new178.repack202, align 8
  %.elt.i141 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %110, i32 0, i32 0
  %.unpack.i142 = load i8*, i8** %.elt.i141, align 8
  %111 = insertvalue %string undef, i8* %.unpack.i142, 0
  %.elt1.i143 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %110, i32 0, i32 1
  %.unpack2.i144 = load i64, i64* %.elt1.i143, align 8
  %112 = insertvalue %string %111, i64 %.unpack2.i144, 1
  %113 = tail call i64 @str_hash(i8* %.unpack.i142, i64 %.unpack2.i144)
  %114 = call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i.i119, %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %this.idx8.val.i.i123, %string %112, i64 %113)
  %115 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %104, i64 0, i32 0
  %116 = load i64, i64* %115, align 4
  %117 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %114, i64 0, i32 0
  store i64 %116, i64* %117, align 4
  %118 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %110
  %119 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %116
  %120 = bitcast %"::HashMap<string, int>::bucketTy"* %119 to i8*
  %121 = bitcast %"::HashMap<string, int>::bucketTy"* %118 to i8*
  call void @llvm.memmove.p0i8.p0i8.i64(i8* align 8 %120, i8* align 8 %121, i64 24, i1 false)
  call void @llvm.memset.p0i8.i64(i8* align 8 %121, i8 0, i64 24, i1 false)
  store i8 2, i8* %105, align 1
  br label %"_Z29::HashMap<string, int>.remove:s->b.exit"

"_Z29::HashMap<string, int>.remove:s->b.exit":    ; preds = %loopEnd, %full.i145
  %.idx71.val = phi i64 [ %97, %loopEnd ], [ %110, %full.i145 ]
  %122 = tail call i64 @str_hash(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @1, i64 0, i64 0), i64 4)
  %123 = call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i.i119, %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %this.idx8.val.i.i123, %string { i8* getelementptr inbounds ([5 x i8], [5 x i8]* @1, i32 0, i32 0), i64 4 }, i64 %122)
  %124 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %123, i64 0, i32 2
  %125 = load i8, i8* %124, align 1
  %126 = icmp eq i8 %125, 1
  br i1 %126, label %full.i147, label %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit149"

full.i147:                                        ; preds = %"_Z29::HashMap<string, int>.remove:s->b.exit"
  %127 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %123, i64 0, i32 0
  %128 = load i64, i64* %127, align 4
  %129 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %128, i32 1
  %130 = load i32, i32* %129, align 4
  br label %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit149"

"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit149": ; preds = %"_Z29::HashMap<string, int>.remove:s->b.exit", %full.i147
  %131 = phi i32 [ %130, %full.i147 ], [ -1, %"_Z29::HashMap<string, int>.remove:s->b.exit" ]
  call void @to_str(i32 %131, %string* nonnull %tmp)
  %.fca.0.load33 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load36 = load i64, i64* %.fca.1.gep, align 8
  %132 = bitcast %string* %retPtr.i to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %132)
  call void @strconcat(i8* getelementptr inbounds ([29 x i8], [29 x i8]* @9, i64 0, i64 0), i64 28, i8* %.fca.0.load33, i64 %.fca.1.load36, %string* nonnull %retPtr.i)
  %.fca.0.gep.i = getelementptr inbounds %string, %string* %retPtr.i, i64 0, i32 0
  %.fca.0.load.i = load i8*, i8** %.fca.0.gep.i, align 8
  %.fca.1.gep.i = getelementptr inbounds %string, %string* %retPtr.i, i64 0, i32 1
  %.fca.1.load.i = load i64, i64* %.fca.1.gep.i, align 8
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %132)
  tail call void @cprintln(i8* %.fca.0.load.i, i64 %.fca.1.load.i)
  %133 = icmp eq i64 %.idx71.val, 0
  br i1 %133, label %loopEnd6, label %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit158"

"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit158": ; preds = %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit149"
  %.elt.i150 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 0, i32 0, i32 0
  %.elt1.i152 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 0, i32 0, i32 1
  %134 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 0, i32 1
  br label %loop5

loop5:                                            ; preds = %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit167", %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit158"
  %"%value3.1.in" = phi i32* [ %134, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit158" ], [ %136, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit167" ]
  %"%key2.sroa.6.1.in" = phi i64* [ %.elt1.i152, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit158" ], [ %.elt1.i161, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit167" ]
  %"%key2.sroa.0.1.in" = phi i8** [ %.elt.i150, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit158" ], [ %.elt.i159, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit167" ]
  %"%state1.1" = phi i64 [ 1, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit158" ], [ %137, %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit167" ]
  %"%key2.sroa.0.1" = load i8*, i8** %"%key2.sroa.0.1.in", align 8
  %"%key2.sroa.6.1" = load i64, i64* %"%key2.sroa.6.1.in", align 8
  %"%value3.1" = load i32, i32* %"%value3.1.in", align 4
  tail call void @cprint(i8* %"%key2.sroa.0.1", i64 %"%key2.sroa.6.1")
  tail call void @cprint(i8* getelementptr inbounds ([5 x i8], [5 x i8]* @8, i64 0, i64 0), i64 4)
  call void @to_str(i32 %"%value3.1", %string* nonnull %tmp)
  %.fca.0.load39 = load i8*, i8** %.fca.0.gep, align 8
  %.fca.1.load42 = load i64, i64* %.fca.1.gep, align 8
  tail call void @cprintln(i8* %.fca.0.load39, i64 %.fca.1.load42)
  %135 = icmp ult i64 %"%state1.1", %.idx71.val
  br i1 %135, label %"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit167", label %loopEnd6

"_Z33::HashMap<string, int>.tryGetNext:z,s,i->b.exit167": ; preds = %loop5
  %.elt.i159 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %"%state1.1", i32 0, i32 0
  %.elt1.i161 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %"%state1.1", i32 0, i32 1
  %136 = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %this.idx7.val.i.i121, i64 %"%state1.1", i32 1
  %137 = add i64 %"%state1.1", 1
  br label %loop5

loopEnd6:                                         ; preds = %loop5, %"_Z32::HashMap<string, int>.getOrElse:s,i->i.exit149"
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

define internal fastcc void @"::HashMap<string, int>.ensureCapacity"(%"::HashMap<string, int>"* nocapture %this) unnamed_addr {
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
  br i1 %10, label %exit, label %doRehash

exit:                                             ; preds = %entry, %"::HashMap<string, int>.rehash.exit"
  ret void

doRehash:                                         ; preds = %entry
  %11 = icmp ult i64 %5, %7
  br i1 %11, label %doRehash.i, label %doIncreaseCapacity.i

doIncreaseCapacity.i:                             ; preds = %doRehash
  %12 = shl i64 %1, 1
  %13 = tail call i64 @nextPrime(i64 %12)
  store i64 %13, i64* %0, align 4
  br label %doRehash.i

doRehash.i:                                       ; preds = %doRehash, %doIncreaseCapacity.i
  %14 = phi i64 [ %1, %doRehash ], [ %13, %doIncreaseCapacity.i ]
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
  br i1 %27, label %"::HashMap<string, int>.rehash.exit", label %loop.i

loop.i:                                           ; preds = %doRehash.i, %loopCond.i
  %28 = phi i64 [ %42, %loopCond.i ], [ 0, %doRehash.i ]
  %29 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %16, i64 %28, i32 2
  %30 = load i8, i8* %29, align 1
  %31 = icmp eq i8 %30, 1
  br i1 %31, label %full.i, label %loopCond.i

full.i:                                           ; preds = %loop.i
  %32 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %16, i64 %28, i32 0
  %33 = load i64, i64* %32, align 4
  %34 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %16, i64 %28, i32 1
  %35 = load i64, i64* %34, align 4
  %.elt.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %18, i64 %33, i32 0, i32 0
  %.unpack.i = load i8*, i8** %.elt.i, align 8
  %36 = insertvalue %string undef, i8* %.unpack.i, 0
  %.elt1.i = getelementptr %"::HashMap<string, int>::bucketTy", %"::HashMap<string, int>::bucketTy"* %18, i64 %33, i32 0, i32 1
  %.unpack2.i = load i64, i64* %.elt1.i, align 8
  %37 = insertvalue %string %36, i64 %.unpack2.i, 1
  %this.idx.val.i = load %"::HashMap<string, int>::slotTy"*, %"::HashMap<string, int>::slotTy"** %15, align 8
  %this.idx3.val.i = load %"::HashMap<string, int>::bucketTy"*, %"::HashMap<string, int>::bucketTy"** %17, align 8
  %this.idx4.val.i = load i64, i64* %0, align 4
  %38 = tail call fastcc %"::HashMap<string, int>::slotTy"* @"::HashMap<string, int>.search"(%"::HashMap<string, int>::slotTy"* %this.idx.val.i, %"::HashMap<string, int>::bucketTy"* %this.idx3.val.i, i64 %this.idx4.val.i, %string %37, i64 %35)
  %39 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %38, i64 0, i32 0
  store i64 %33, i64* %39, align 4
  %40 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %38, i64 0, i32 1
  store i64 %35, i64* %40, align 4
  %41 = getelementptr %"::HashMap<string, int>::slotTy", %"::HashMap<string, int>::slotTy"* %38, i64 0, i32 2
  store i8 1, i8* %41, align 1
  br label %loopCond.i

loopCond.i:                                       ; preds = %full.i, %loop.i
  %42 = add nuw i64 %28, 1
  %43 = icmp ult i64 %42, %1
  br i1 %43, label %loop.i, label %"::HashMap<string, int>.rehash.exit"

"::HashMap<string, int>.rehash.exit":             ; preds = %loopCond.i, %doRehash.i
  store i64 0, i64* %6, align 4
  br label %exit
}

; Function Attrs: argmemonly nounwind readonly
declare i64 @str_hash(i8* nocapture, i64) local_unnamed_addr #6

declare void @cprint(i8* nocapture readonly, i64) local_unnamed_addr

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

declare void @to_str(i32, %string* nocapture writeonly) local_unnamed_addr

; Function Attrs: noreturn uwtable
declare void @throwException(i8* readonly, i64) local_unnamed_addr #7

declare void @strconcat(i8* nocapture readonly, i64, i8* nocapture readonly, i64, %string* nocapture writeonly) local_unnamed_addr

; Function Attrs: argmemonly nounwind
declare void @llvm.memmove.p0i8.p0i8.i64(i8* nocapture, i8* nocapture readonly, i64, i1) #5

; Function Attrs: argmemonly nounwind
declare void @llvm.memset.p0i8.i64(i8* nocapture writeonly, i8, i64, i1) #5

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.start.p0i8(i64, i8* nocapture) #5

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.end.p0i8(i64, i8* nocapture) #5

attributes #0 = { inaccessiblememonly nounwind }
attributes #1 = { norecurse }
attributes #2 = { argmemonly readonly }
attributes #3 = { argmemonly }
attributes #4 = { nounwind readnone }
attributes #5 = { argmemonly nounwind }
attributes #6 = { argmemonly nounwind readonly }
attributes #7 = { noreturn uwtable }
