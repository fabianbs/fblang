; ModuleID = 'Complex'
source_filename = "Complex"

%Complex = type { double, double }
%string = type { i8*, i64 }

@0 = private unnamed_addr constant [2 x i8] c"0\00"
@1 = private unnamed_addr constant [2 x i8] c"i\00"
@2 = private unnamed_addr constant [4 x i8] c" - \00"
@3 = private unnamed_addr constant [4 x i8] c" + \00"

; Function Attrs: norecurse nounwind
define void @"_Z12Complex.ctor:d,d->v"(%Complex* nocapture %this, double %_real, double %_img) local_unnamed_addr #0 {
deconstruction_end:
  %0 = getelementptr %Complex, %Complex* %this, i64 0, i32 0
  store double %_real, double* %0, align 8
  %1 = getelementptr %Complex, %Complex* %this, i64 0, i32 1
  store double %_img, double* %1, align 8
  ret void
}

; Function Attrs: norecurse nounwind readonly
define %Complex @"_Z18Complex.operator +:Complex&->Complex"(%Complex* nocapture readonly %this, %Complex* nocapture nonnull readonly %other) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %Complex, %Complex* %this, i64 0, i32 0
  %1 = load double, double* %0, align 8
  %.elt = getelementptr inbounds %Complex, %Complex* %other, i64 0, i32 0
  %.unpack = load double, double* %.elt, align 8
  %.elt3 = getelementptr inbounds %Complex, %Complex* %other, i64 0, i32 1
  %.unpack4 = load double, double* %.elt3, align 8
  %2 = fadd double %1, %.unpack
  %3 = getelementptr %Complex, %Complex* %this, i64 0, i32 1
  %4 = load double, double* %3, align 8
  %5 = fadd double %.unpack4, %4
  %.fca.0.insert = insertvalue %Complex undef, double %2, 0
  %.fca.1.insert = insertvalue %Complex %.fca.0.insert, double %5, 1
  ret %Complex %.fca.1.insert
}

; Function Attrs: norecurse nounwind readonly
define %Complex @"_Z18Complex.operator *:Complex&->Complex"(%Complex* nocapture readonly %this, %Complex* nocapture nonnull readonly %other) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %Complex, %Complex* %this, i64 0, i32 0
  %1 = load double, double* %0, align 8
  %.elt = getelementptr inbounds %Complex, %Complex* %other, i64 0, i32 0
  %.unpack = load double, double* %.elt, align 8
  %.elt3 = getelementptr inbounds %Complex, %Complex* %other, i64 0, i32 1
  %.unpack4 = load double, double* %.elt3, align 8
  %2 = fmul double %1, %.unpack
  %3 = getelementptr %Complex, %Complex* %this, i64 0, i32 1
  %4 = load double, double* %3, align 8
  %5 = fmul double %.unpack4, %4
  %6 = fsub double %2, %5
  %7 = fmul double %1, %.unpack4
  %8 = fmul double %.unpack, %4
  %9 = fadd double %7, %8
  %.fca.0.insert = insertvalue %Complex undef, double %6, 0
  %.fca.1.insert = insertvalue %Complex %.fca.0.insert, double %9, 1
  ret %Complex %.fca.1.insert
}

define %string @"_Z23Complex.operator string:v->s"(%Complex* nocapture readonly %this) local_unnamed_addr {
entry:
  %retPtr.i87 = alloca %string, align 8
  %retPtr.i80 = alloca <2 x i64>, align 16
  %tmpcast109 = bitcast <2 x i64>* %retPtr.i80 to %string*
  %retPtr.i73 = alloca %string, align 8
  %retPtr.i66 = alloca <2 x i64>, align 16
  %tmpcast108 = bitcast <2 x i64>* %retPtr.i66 to %string*
  %retPtr.i = alloca <2 x i64>, align 16
  %tmpcast110 = bitcast <2 x i64>* %retPtr.i to %string*
  %0 = alloca [4 x %string], align 16
  %1 = alloca %string, align 8
  %2 = alloca [3 x %string], align 16
  %3 = alloca %string, align 8
  %tmp = alloca <2 x i64>, align 16
  %tmpcast = bitcast <2 x i64>* %tmp to %string*
  %.sub65 = getelementptr inbounds [3 x %string], [3 x %string]* %2, i64 0, i64 0
  %.sub = getelementptr inbounds [4 x %string], [4 x %string]* %0, i64 0, i64 0
  %4 = getelementptr %Complex, %Complex* %this, i64 0, i32 0
  %5 = load double, double* %4, align 8
  %6 = fcmp oeq double %5, 0.000000e+00
  %7 = getelementptr %Complex, %Complex* %this, i64 0, i32 1
  %8 = load double, double* %7, align 8
  %9 = fcmp oeq double %8, 0.000000e+00
  br i1 %6, label %then, label %else

then:                                             ; preds = %entry
  br i1 %9, label %then1, label %else6

else:                                             ; preds = %entry
  br i1 %9, label %then7, label %else9

then1:                                            ; preds = %then
  ret %string { i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i32 0, i32 0), i64 1 }

else6:                                            ; preds = %then
  call void @dto_str(double %8, %string* nonnull %tmpcast)
  %10 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load94 = extractelement <2 x i64> %10, i32 0
  %11 = inttoptr i64 %.fca.0.load94 to i8*
  %.fca.1.load95 = extractelement <2 x i64> %10, i32 1
  %12 = bitcast <2 x i64>* %retPtr.i to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %12)
  call void @strconcat(i8* %11, i64 %.fca.1.load95, i8* getelementptr inbounds ([2 x i8], [2 x i8]* @1, i64 0, i64 0), i64 1, %string* nonnull %tmpcast110)
  %13 = load <2 x i64>, <2 x i64>* %retPtr.i, align 16
  %.fca.0.load.i96 = extractelement <2 x i64> %13, i32 0
  %14 = inttoptr i64 %.fca.0.load.i96 to i8*
  %.fca.1.load.i97 = extractelement <2 x i64> %13, i32 1
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %12)
  %oldret52 = insertvalue %string undef, i8* %14, 0
  %oldret54 = insertvalue %string %oldret52, i64 %.fca.1.load.i97, 1
  ret %string %oldret54

then7:                                            ; preds = %else
  call void @dto_str(double %5, %string* nonnull %tmpcast)
  %15 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load1498 = extractelement <2 x i64> %15, i32 0
  %16 = inttoptr i64 %.fca.0.load1498 to i8*
  %.fca.1.load1799 = extractelement <2 x i64> %15, i32 1
  %.fca.0.insert15 = insertvalue %string undef, i8* %16, 0
  %.fca.1.insert18 = insertvalue %string %.fca.0.insert15, i64 %.fca.1.load1799, 1
  ret %string %.fca.1.insert18

else9:                                            ; preds = %else
  %17 = fcmp olt double %8, 0.000000e+00
  %. = select i1 %17, %string { i8* getelementptr inbounds ([4 x i8], [4 x i8]* @2, i32 0, i32 0), i64 3 }, %string { i8* getelementptr inbounds ([4 x i8], [4 x i8]* @3, i32 0, i32 0), i64 3 }
  %..fca.0.extract = extractvalue %string %., 0
  %..fca.1.extract = extractvalue %string %., 1
  %18 = fsub double -0.000000e+00, %8
  %19 = select i1 %17, double %18, double %8
  %20 = fcmp oeq double %19, 0.000000e+00
  call void @dto_str(double %5, %string* nonnull %tmpcast)
  %21 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load20104 = extractelement <2 x i64> %21, i32 0
  %22 = inttoptr i64 %.fca.0.load20104 to i8*
  %.fca.1.load23105 = extractelement <2 x i64> %21, i32 1
  %23 = bitcast <2 x i64>* %retPtr.i66 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %23)
  call void @strconcat(i8* %22, i64 %.fca.1.load23105, i8* %..fca.0.extract, i64 %..fca.1.extract, %string* nonnull %tmpcast108)
  %24 = load <2 x i64>, <2 x i64>* %retPtr.i66, align 16
  %.fca.0.load.i68106 = extractelement <2 x i64> %24, i32 0
  %25 = inttoptr i64 %.fca.0.load.i68106 to i8*
  %.fca.1.load.i70107 = extractelement <2 x i64> %24, i32 1
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %23)
  br i1 %20, label %then10, label %else12

then10:                                           ; preds = %else9
  %26 = bitcast [3 x %string]* %2 to i8*
  call void @llvm.lifetime.start.p0i8(i64 48, i8* nonnull %26)
  %27 = bitcast [3 x %string]* %2 to <2 x i64>*
  store <2 x i64> %21, <2 x i64>* %27, align 16
  %28 = getelementptr inbounds [3 x %string], [3 x %string]* %2, i64 0, i64 1, i32 0
  %29 = ptrtoint i8* %..fca.0.extract to i64
  %30 = insertelement <2 x i64> undef, i64 %29, i32 0
  %31 = insertelement <2 x i64> %30, i64 %..fca.1.extract, i32 1
  %32 = bitcast i8** %28 to <2 x i64>*
  store <2 x i64> %31, <2 x i64>* %32, align 16
  %33 = getelementptr inbounds [3 x %string], [3 x %string]* %2, i64 0, i64 2, i32 0
  %34 = bitcast i8** %33 to <2 x i64>*
  store <2 x i64> <i64 ptrtoint ([2 x i8]* @1 to i64), i64 1>, <2 x i64>* %34, align 16
  %35 = bitcast %string* %3 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %35)
  call void @strmulticoncat(%string* nonnull %.sub65, i64 3, %string* nonnull %3)
  %.fca.1.gep57 = getelementptr inbounds %string, %string* %3, i64 0, i32 1
  %.fca.1.load58 = load i64, i64* %.fca.1.gep57, align 8
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %35)
  call void @llvm.lifetime.end.p0i8(i64 48, i8* nonnull %26)
  %36 = bitcast %string* %retPtr.i73 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %36)
  call void @strconcat(i8* %25, i64 %.fca.1.load.i70107, i8* getelementptr inbounds ([2 x i8], [2 x i8]* @1, i64 0, i64 0), i64 1, %string* nonnull %retPtr.i73)
  %.fca.0.gep.i74 = getelementptr inbounds %string, %string* %retPtr.i73, i64 0, i32 0
  %.fca.0.load.i75 = load i8*, i8** %.fca.0.gep.i74, align 8
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %36)
  %oldret44 = insertvalue %string undef, i8* %.fca.0.load.i75, 0
  %oldret46 = insertvalue %string %oldret44, i64 %.fca.1.load58, 1
  ret %string %oldret46

else12:                                           ; preds = %else9
  %37 = load double, double* %7, align 8
  %38 = fcmp olt double %37, 0.000000e+00
  %39 = fsub double -0.000000e+00, %37
  %40 = select i1 %38, double %39, double %37
  call void @dto_str(double %40, %string* nonnull %tmpcast)
  %41 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load26100 = extractelement <2 x i64> %41, i32 0
  %42 = inttoptr i64 %.fca.0.load26100 to i8*
  %.fca.1.load29101 = extractelement <2 x i64> %41, i32 1
  %43 = bitcast <2 x i64>* %retPtr.i80 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %43)
  call void @strconcat(i8* %25, i64 %.fca.1.load.i70107, i8* %42, i64 %.fca.1.load29101, %string* nonnull %tmpcast109)
  %44 = load <2 x i64>, <2 x i64>* %retPtr.i80, align 16
  %.fca.0.load.i82102 = extractelement <2 x i64> %44, i32 0
  %45 = inttoptr i64 %.fca.0.load.i82102 to i8*
  %.fca.1.load.i84103 = extractelement <2 x i64> %44, i32 1
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %43)
  %46 = bitcast [4 x %string]* %0 to i8*
  call void @llvm.lifetime.start.p0i8(i64 64, i8* nonnull %46)
  %47 = bitcast [4 x %string]* %0 to <2 x i64>*
  store <2 x i64> %21, <2 x i64>* %47, align 16
  %48 = getelementptr inbounds [4 x %string], [4 x %string]* %0, i64 0, i64 1, i32 0
  %49 = ptrtoint i8* %..fca.0.extract to i64
  %50 = insertelement <2 x i64> undef, i64 %49, i32 0
  %51 = insertelement <2 x i64> %50, i64 %..fca.1.extract, i32 1
  %52 = bitcast i8** %48 to <2 x i64>*
  store <2 x i64> %51, <2 x i64>* %52, align 16
  %53 = getelementptr inbounds [4 x %string], [4 x %string]* %0, i64 0, i64 2, i32 0
  %54 = bitcast i8** %53 to <2 x i64>*
  store <2 x i64> %41, <2 x i64>* %54, align 16
  %55 = getelementptr inbounds [4 x %string], [4 x %string]* %0, i64 0, i64 3, i32 0
  %56 = bitcast i8** %55 to <2 x i64>*
  store <2 x i64> <i64 ptrtoint ([2 x i8]* @1 to i64), i64 1>, <2 x i64>* %56, align 16
  %57 = bitcast %string* %1 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %57)
  call void @strmulticoncat(%string* nonnull %.sub, i64 4, %string* nonnull %1)
  %.fca.1.gep62 = getelementptr inbounds %string, %string* %1, i64 0, i32 1
  %.fca.1.load63 = load i64, i64* %.fca.1.gep62, align 8
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %57)
  call void @llvm.lifetime.end.p0i8(i64 64, i8* nonnull %46)
  %58 = bitcast %string* %retPtr.i87 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %58)
  call void @strconcat(i8* %45, i64 %.fca.1.load.i84103, i8* getelementptr inbounds ([2 x i8], [2 x i8]* @1, i64 0, i64 0), i64 1, %string* nonnull %retPtr.i87)
  %.fca.0.gep.i88 = getelementptr inbounds %string, %string* %retPtr.i87, i64 0, i32 0
  %.fca.0.load.i89 = load i8*, i8** %.fca.0.gep.i88, align 8
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %58)
  %oldret = insertvalue %string undef, i8* %.fca.0.load.i89, 0
  %oldret38 = insertvalue %string %oldret, i64 %.fca.1.load63, 1
  ret %string %oldret38
}

; Function Attrs: norecurse nounwind readonly
define i1 @"_Z19Complex.operator ==:Complex&->b"(%Complex* nocapture readonly %this, %Complex* nocapture nonnull readonly %other) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %Complex, %Complex* %this, i64 0, i32 0
  %1 = load double, double* %0, align 8
  %.elt = getelementptr inbounds %Complex, %Complex* %other, i64 0, i32 0
  %.unpack = load double, double* %.elt, align 8
  %2 = fcmp oeq double %1, %.unpack
  br i1 %2, label %checkRHS, label %endLAND

checkRHS:                                         ; preds = %entry
  %.elt1 = getelementptr inbounds %Complex, %Complex* %other, i64 0, i32 1
  %.unpack2 = load double, double* %.elt1, align 8
  %3 = getelementptr %Complex, %Complex* %this, i64 0, i32 1
  %4 = load double, double* %3, align 8
  %5 = fcmp oeq double %4, %.unpack2
  br label %endLAND

endLAND:                                          ; preds = %checkRHS, %entry
  %6 = phi i1 [ %5, %checkRHS ], [ false, %entry ]
  ret i1 %6
}

define void @main() local_unnamed_addr {
entry:
  %0 = alloca <2 x double>, align 16
  %tmpcast53 = bitcast <2 x double>* %0 to %Complex*
  %1 = alloca <2 x double>, align 16
  %tmpcast52 = bitcast <2 x double>* %1 to %Complex*
  %2 = alloca <2 x double>, align 16
  %tmpcast = bitcast <2 x double>* %2 to %Complex*
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  store <2 x double> <double 5.000000e+00, double 7.000000e+00>, <2 x double>* %2, align 16
  %3 = call %string @"_Z23Complex.operator string:v->s"(%Complex* nonnull %tmpcast)
  %4 = extractvalue %string %3, 0
  %5 = extractvalue %string %3, 1
  tail call void @cprintln(i8* %4, i64 %5)
  store <2 x double> <double -6.000000e+00, double 1.700000e+01>, <2 x double>* %1, align 16
  %6 = call %string @"_Z23Complex.operator string:v->s"(%Complex* nonnull %tmpcast52)
  %7 = extractvalue %string %6, 0
  %8 = extractvalue %string %6, 1
  tail call void @cprintln(i8* %7, i64 %8)
  store <2 x double> <double 4.000000e+00, double 6.000000e+00>, <2 x double>* %0, align 16
  %9 = call %string @"_Z23Complex.operator string:v->s"(%Complex* nonnull %tmpcast53)
  %10 = extractvalue %string %9, 0
  %11 = extractvalue %string %9, 1
  tail call void @cprintln(i8* %10, i64 %11)
  ret void
}

declare void @strconcat(i8* nocapture readonly, i64, i8* nocapture readonly, i64, %string* nocapture writeonly) local_unnamed_addr

declare void @dto_str(double, %string* nocapture writeonly) local_unnamed_addr

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #2

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #2

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

declare void @strmulticoncat(%string* nocapture readonly, i64, %string* nocapture writeonly) local_unnamed_addr

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.start.p0i8(i64, i8* nocapture) #3

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.end.p0i8(i64, i8* nocapture) #3

attributes #0 = { norecurse nounwind }
attributes #1 = { norecurse nounwind readonly }
attributes #2 = { inaccessiblememonly nounwind }
attributes #3 = { argmemonly nounwind }
