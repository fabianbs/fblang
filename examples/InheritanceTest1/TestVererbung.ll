; ModuleID = 'TestVererbung'
source_filename = "TestVererbung"

%BaseClass = type { i8*, i32 }
%DeriveredClass = type { %BaseClass, i32 }

@BaseClass_vtable = constant { i32 (%BaseClass*)*, i64, i1 (i8*)* } { i32 (%BaseClass*)* @"_Z18BaseClass.getValue:v->i", i64 -4828498853749317769, i1 (i8*)* @BaseClass.isInstanceOf }, !type !0
@DeriveredClass_vtable = constant { i32 (%DeriveredClass*)*, i64, i1 (i8*)* } { i32 (%DeriveredClass*)* @"_Z23DeriveredClass.getValue:v->i", i64 -5044173419723177993, i1 (i8*)* @DeriveredClass.isInstanceOf }, !type !0, !type !1
@0 = private unnamed_addr constant [3 x i8] c"36\00"

; Function Attrs: norecurse nounwind
define void @"_Z14BaseClass.ctor:v->v"(%BaseClass* nocapture %this) local_unnamed_addr #0 {
entry:
  %0 = getelementptr %BaseClass, %BaseClass* %this, i64 0, i32 1
  store i32 3, i32* %0, align 4
  ret void
}

; Function Attrs: norecurse nounwind readonly
define i32 @"_Z18BaseClass.getValue:v->i"(%BaseClass* nocapture readonly %this) #1 {
entry:
  %0 = getelementptr %BaseClass, %BaseClass* %this, i64 0, i32 1
  %1 = load i32, i32* %0, align 4
  %2 = mul i32 %1, %1
  ret i32 %2
}

; Function Attrs: norecurse nounwind
define void @"_Z19DeriveredClass.ctor:v->v"(%DeriveredClass* nocapture %this) local_unnamed_addr #0 {
entry:
  %0 = getelementptr %DeriveredClass, %DeriveredClass* %this, i64 0, i32 0, i32 1
  store i32 3, i32* %0, align 4
  %1 = getelementptr %DeriveredClass, %DeriveredClass* %this, i64 0, i32 1
  store i32 4, i32* %1, align 4
  ret void
}

; Function Attrs: norecurse nounwind readonly
define i32 @"_Z23DeriveredClass.getValue:v->i"(%DeriveredClass* nocapture readonly %this) #1 {
entry:
  %0 = getelementptr %DeriveredClass, %DeriveredClass* %this, i64 0, i32 0, i32 1
  %1 = load i32, i32* %0, align 4
  %2 = mul i32 %1, %1
  %3 = getelementptr %DeriveredClass, %DeriveredClass* %this, i64 0, i32 1
  %4 = load i32, i32* %3, align 4
  %5 = mul i32 %2, %4
  ret i32 %5
}

define void @main() local_unnamed_addr {
entry:
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  tail call void @cprintln(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @0, i64 0, i64 0), i64 2)
  ret void
}

; Function Attrs: argmemonly norecurse nounwind readnone
define i1 @BaseClass.isInstanceOf(i8* nocapture nonnull readonly %other) #2 {
entry:
  %0 = icmp eq i8* %other, bitcast ({ i32 (%BaseClass*)*, i64, i1 (i8*)* }* @BaseClass_vtable to i8*)
  ret i1 %0
}

; Function Attrs: argmemonly norecurse nounwind readonly
define i1 @DeriveredClass.isInstanceOf(i8* nocapture nonnull readonly %other) #3 {
entry:
  %0 = icmp eq i8* %other, bitcast ({ i32 (%DeriveredClass*)*, i64, i1 (i8*)* }* @DeriveredClass_vtable to i8*)
  br i1 %0, label %returnTrue, label %hashEntry

hashEntry:                                        ; preds = %entry
  %1 = getelementptr i8, i8* %other, i64 8
  %2 = bitcast i8* %1 to i64*
  %3 = load i64, i64* %2, align 4
  %cond = icmp eq i64 %3, -4828498853749317769
  %4 = icmp eq i8* %other, bitcast ({ i32 (%BaseClass*)*, i64, i1 (i8*)* }* @BaseClass_vtable to i8*)
  %spec.select = and i1 %4, %cond
  br label %returnTrue

returnTrue:                                       ; preds = %hashEntry, %entry
  %merge = phi i1 [ true, %entry ], [ %spec.select, %hashEntry ]
  ret i1 %merge
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #4

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #4

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

attributes #0 = { norecurse nounwind }
attributes #1 = { norecurse nounwind readonly }
attributes #2 = { argmemonly norecurse nounwind readnone }
attributes #3 = { argmemonly norecurse nounwind readonly }
attributes #4 = { inaccessiblememonly nounwind }

!0 = !{i64 0, !"BaseClass"}
!1 = !{i64 0, !"DeriveredClass"}
