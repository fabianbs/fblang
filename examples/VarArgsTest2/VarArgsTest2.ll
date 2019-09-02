; ModuleID = 'VarArgsTest2'
source_filename = "VarArgsTest2"

%"var_arg<int>" = type { { i32*, i64 }*, i32, i64, i64 }
%string = type { i8*, i64 }

@0 = private unnamed_addr constant [3 x i8] c"15\00"
@1 = private unnamed_addr constant [2 x i8] c"7\00"

; Function Attrs: nounwind readonly
define internal fastcc i32 @"_Z3bar:i,int...->i"(i32 %first, %"var_arg<int>" %rest) unnamed_addr #0 {
entry:
  %rest.fca.0.extract = extractvalue %"var_arg<int>" %rest, 0
  %rest.fca.2.extract = extractvalue %"var_arg<int>" %rest, 2
  %rest.fca.3.extract = extractvalue %"var_arg<int>" %rest, 3
  %0 = icmp eq i64 %rest.fca.2.extract, 0
  br i1 %0, label %then, label %else

then:                                             ; preds = %entry
  ret i32 %first

else:                                             ; preds = %entry
  %1 = icmp eq i64 %rest.fca.3.extract, 0
  br i1 %1, label %.loopexit, label %.preheader.preheader

.preheader.preheader:                             ; preds = %else
  br label %.preheader

.preheader:                                       ; preds = %.preheader.preheader, %11
  %2 = phi i32 [ %12, %11 ], [ 0, %.preheader.preheader ]
  %3 = phi i64 [ %9, %11 ], [ 0, %.preheader.preheader ]
  %4 = icmp ult i32 %2, 3
  br i1 %4, label %5, label %.loopexit.loopexit

; <label>:5:                                      ; preds = %.preheader
  %6 = sext i32 %2 to i64
  %7 = getelementptr { i32*, i64 }, { i32*, i64 }* %rest.fca.0.extract, i64 %6, i32 1
  %8 = load i64, i64* %7, align 4
  %9 = add i64 %8, %3
  %10 = icmp ugt i64 %9, %rest.fca.3.extract
  br i1 %10, label %.loopexit.loopexit, label %11

; <label>:11:                                     ; preds = %5
  %12 = add nuw i32 %2, 1
  %13 = icmp ugt i64 %rest.fca.3.extract, %9
  br i1 %13, label %.preheader, label %.loopexit.loopexit

.loopexit.loopexit:                               ; preds = %.preheader, %5, %11
  %.ph = phi i64 [ %3, %.preheader ], [ %9, %11 ], [ %3, %5 ]
  %.ph78 = phi i32 [ %2, %.preheader ], [ %12, %11 ], [ %2, %5 ]
  br label %.loopexit

.loopexit:                                        ; preds = %.loopexit.loopexit, %else
  %14 = phi i64 [ 0, %else ], [ %.ph, %.loopexit.loopexit ]
  %15 = phi i32 [ 0, %else ], [ %.ph78, %.loopexit.loopexit ]
  %16 = icmp ugt i64 %rest.fca.3.extract, %14
  %17 = select i1 %16, i64 %rest.fca.3.extract, i64 %14
  %18 = sub i64 %17, %14
  %19 = sext i32 %15 to i64
  %20 = getelementptr { i32*, i64 }, { i32*, i64 }* %rest.fca.0.extract, i64 %19, i32 0
  %21 = load i32*, i32** %20, align 8
  %22 = getelementptr i32, i32* %21, i64 %18
  %23 = load i32, i32* %22, align 4
  %24 = add i64 %rest.fca.3.extract, 1
  %25 = add i64 %rest.fca.2.extract, -1
  %26 = icmp ult i64 %rest.fca.2.extract, %25
  %27 = select i1 %26, i64 %rest.fca.2.extract, i64 %25
  %28 = insertvalue %"var_arg<int>" %rest, i64 %27, 2
  %29 = insertvalue %"var_arg<int>" %28, i64 %24, 3
  %30 = tail call fastcc i32 @"_Z3bar:i,int...->i"(i32 %23, %"var_arg<int>" %29)
  %31 = add i32 %30, %first
  ret i32 %31
}

define void @main() local_unnamed_addr {
"_Z3sum:i,int...->i.exit130":
  %vararg.spans15 = alloca [3 x { i32*, i64 }], align 16
  %varArg.SingleElems13 = alloca [1 x i32], align 4
  %varArg.SingleElems11 = alloca [1 x i32], align 4
  %tmp = alloca <2 x i64>, align 16
  %tmpcast = bitcast <2 x i64>* %tmp to %string*
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %0 = tail call i8* @gc_new(i64 16)
  %1 = bitcast i8* %0 to i64*
  store i64 2, i64* %1, align 4
  %2 = getelementptr i8, i8* %0, i64 8
  %3 = bitcast i8* %2 to i32*
  store i32 3, i32* %3, align 4
  %4 = getelementptr i8, i8* %0, i64 12
  %5 = bitcast i8* %4 to i32*
  store i32 4, i32* %5, align 4
  tail call void @cprintln(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @0, i64 0, i64 0), i64 2)
  tail call void @cprintln(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @0, i64 0, i64 0), i64 2)
  store <2 x i64> <i64 ptrtoint ([2 x i8]* @1 to i64), i64 1>, <2 x i64>* %tmp, align 16
  tail call void @cprintln(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @1, i64 0, i64 0), i64 1)
  %6 = getelementptr inbounds [1 x i32], [1 x i32]* %varArg.SingleElems11, i64 0, i64 0
  store i32 2, i32* %6, align 4
  %7 = getelementptr inbounds [1 x i32], [1 x i32]* %varArg.SingleElems13, i64 0, i64 0
  store i32 5, i32* %7, align 4
  %8 = getelementptr inbounds [3 x { i32*, i64 }], [3 x { i32*, i64 }]* %vararg.spans15, i64 0, i64 0
  %9 = ptrtoint [1 x i32]* %varArg.SingleElems11 to i64
  %10 = insertelement <2 x i64> <i64 undef, i64 1>, i64 %9, i32 0
  %11 = bitcast [3 x { i32*, i64 }]* %vararg.spans15 to <2 x i64>*
  store <2 x i64> %10, <2 x i64>* %11, align 16
  %12 = getelementptr inbounds [3 x { i32*, i64 }], [3 x { i32*, i64 }]* %vararg.spans15, i64 0, i64 1
  %13 = ptrtoint i8* %2 to i64
  %14 = insertelement <2 x i64> <i64 undef, i64 2>, i64 %13, i32 0
  %15 = bitcast { i32*, i64 }* %12 to <2 x i64>*
  store <2 x i64> %14, <2 x i64>* %15, align 16
  %.fca.1.insert92.fca.0.gep = getelementptr inbounds [3 x { i32*, i64 }], [3 x { i32*, i64 }]* %vararg.spans15, i64 0, i64 2, i32 0
  %16 = ptrtoint [1 x i32]* %varArg.SingleElems13 to i64
  %17 = insertelement <2 x i64> <i64 undef, i64 1>, i64 %16, i32 0
  %18 = bitcast i32** %.fca.1.insert92.fca.0.gep to <2 x i64>*
  store <2 x i64> %17, <2 x i64>* %18, align 16
  %.fca.0.insert99 = insertvalue %"var_arg<int>" undef, { i32*, i64 }* %8, 0
  %.fca.1.insert102 = insertvalue %"var_arg<int>" %.fca.0.insert99, i32 3, 1
  %.fca.2.insert103 = insertvalue %"var_arg<int>" %.fca.1.insert102, i64 4, 2
  %.fca.3.insert104 = insertvalue %"var_arg<int>" %.fca.2.insert103, i64 0, 3
  %19 = call fastcc i32 @"_Z3bar:i,int...->i"(i32 1, %"var_arg<int>" %.fca.3.insert104)
  call void @to_str(i32 %19, %string* nonnull %tmpcast)
  %20 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load46156 = extractelement <2 x i64> %20, i32 0
  %21 = inttoptr i64 %.fca.0.load46156 to i8*
  %.fca.1.load49157 = extractelement <2 x i64> %20, i32 1
  call void @cprintln(i8* %21, i64 %.fca.1.load49157)
  ret void
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #1

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #1

; Function Attrs: norecurse
declare noalias nonnull i8* @gc_new(i64) local_unnamed_addr #2

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

declare void @to_str(i32, %string* nocapture writeonly) local_unnamed_addr

attributes #0 = { nounwind readonly }
attributes #1 = { inaccessiblememonly nounwind }
attributes #2 = { norecurse }
