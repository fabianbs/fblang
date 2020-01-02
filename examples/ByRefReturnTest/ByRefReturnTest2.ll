; ModuleID = 'ByRefReturnTest'
source_filename = "ByRefReturnTest.fbs"

%Box = type { i32, double }
%string = type { i8*, i64 }

@0 = private unnamed_addr constant [3 x i8] c"42\00"
@1 = private unnamed_addr constant [3 x i8] c"43\00"

; Function Attrs: norecurse nounwind
define void @"_Z8Box.ctor:i->v"(%Box* nocapture %this, i32 %val) local_unnamed_addr #0 {
entry:
  %0 = getelementptr %Box, %Box* %this, i64 0, i32 0
  store i32 %val, i32* %0, align 4
  ret void
}

; Function Attrs: norecurse nounwind readnone
define nonnull i32* @"_Z9Box.value:v->i"(%Box* readnone %this) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %Box, %Box* %this, i64 0, i32 0
  ret i32* %0
}

; Function Attrs: norecurse nounwind readnone
define nonnull double* @"_Z9Box.value:v->d"(%Box* readnone %this) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %Box, %Box* %this, i64 0, i32 1
  ret double* %0
}

define void @main() local_unnamed_addr {
entry:
  %tmp = alloca %string, align 8
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %0 = getelementptr inbounds %string, %string* %tmp, i64 0, i32 0
  %1 = getelementptr inbounds %string, %string* %tmp, i64 0, i32 1
  tail call void @cprintln(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @0, i64 0, i64 0), i64 2)
  store i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i64 0, i64 0), i8** %0, align 8
  store i64 2, i64* %1, align 8
  tail call void @cprintln(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i64 0, i64 0), i64 2)
  call void @dto_str(double 0.000000e+00, %string* nonnull %tmp)
  %.fca.0.load15 = load i8*, i8** %0, align 8
  %.fca.1.load18 = load i64, i64* %1, align 8
  tail call void @cprintln(i8* %.fca.0.load15, i64 %.fca.1.load18)
  call void @dto_str(double 4.224000e+01, %string* nonnull %tmp)
  %.fca.0.load21 = load i8*, i8** %0, align 8
  %.fca.1.load24 = load i64, i64* %1, align 8
  tail call void @cprintln(i8* %.fca.0.load21, i64 %.fca.1.load24)
  tail call void @cprintln(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i64 0, i64 0), i64 2)
  ret void
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #2

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #2

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

declare void @dto_str(double, %string* nocapture writeonly) local_unnamed_addr

attributes #0 = { norecurse nounwind }
attributes #1 = { norecurse nounwind readnone }
attributes #2 = { inaccessiblememonly nounwind }
