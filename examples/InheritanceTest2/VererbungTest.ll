; ModuleID = 'VererbungTest'
source_filename = "VererbungTest"

%IntContainer = type { i8*, { i64, [0 x i32] }*, i32 }
%IntStack = type { %IntContainer, i32 }
%IntStackIterator = type { %IntStack*, i32 }
%string = type { i8*, i64 }

@IntContainer_vtable = constant { i1 (%IntContainer*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* } { i1 (%IntContainer*, i32)* @"_Z21IntContainer.contains:i->b", i32 (%IntContainer*, i32)* @"_Z24IntContainer.operator []:i->i", void (%IntContainer*, i32, i32)* @"_Z24IntContainer.operator []:i,i->v", i64 -1946787124084753745, i1 (i8*)* @IntContainer.isInstanceOf }, !type !0
@IntStack_vtable = constant { i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* } { i1 (%IntStack*, i32)* @"_Z17IntStack.contains:i->b", i32 (%IntContainer*, i32)* @"_Z24IntContainer.operator []:i->i", void (%IntContainer*, i32, i32)* @"_Z24IntContainer.operator []:i,i->v", i64 -1405194357285322769, i1 (i8*)* @IntStack.isInstanceOf }, !type !0, !type !1
@0 = private unnamed_addr constant [19 x i8] c"stack enthaelt 4: \00"
@1 = private unnamed_addr constant [15 x i8] c"cont.set(3, 4)\00"
@2 = private unnamed_addr constant [5 x i8] c"true\00"
@3 = private unnamed_addr constant [6 x i8] c"false\00"
@4 = private unnamed_addr constant [23 x i8] c"stack enthaelt 3: true\00"

; Function Attrs: norecurse
define void @"_Z17IntContainer.ctor:i->v"(%IntContainer* nocapture %this, i32 %cap) local_unnamed_addr #0 {
entry:
  %0 = getelementptr %IntContainer, %IntContainer* %this, i64 0, i32 2
  %1 = icmp sgt i32 %cap, 0
  %2 = select i1 %1, i32 %cap, i32 0
  store i32 %2, i32* %0, align 4
  %3 = getelementptr %IntContainer, %IntContainer* %this, i64 0, i32 1
  %4 = shl i32 %cap, 2
  %5 = add i32 %4, 8
  %6 = sext i32 %5 to i64
  %7 = tail call i8* @gc_new(i64 %6)
  %8 = bitcast i8* %7 to i64*
  %9 = sext i32 %cap to i64
  store i64 %9, i64* %8, align 4
  %10 = bitcast { i64, [0 x i32] }** %3 to i8**
  store i8* %7, i8** %10, align 8
  ret void
}

define i1 @"_Z21IntContainer.contains:i->b"(%IntContainer* nocapture readonly %this, i32 %val) {
entry:
  %0 = getelementptr %IntContainer, %IntContainer* %this, i64 0, i32 2
  %1 = load i32, i32* %0, align 4
  %2 = icmp sgt i32 %1, 0
  br i1 %2, label %loop.preheader, label %loopEnd

loop.preheader:                                   ; preds = %entry
  %3 = getelementptr %IntContainer, %IntContainer* %this, i64 0, i32 1
  br label %loop

loop:                                             ; preds = %loop.preheader, %else
  %i.0 = phi i32 [ %13, %else ], [ 0, %loop.preheader ]
  %4 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %3, align 8
  %5 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %4, i64 0, i32 0
  %6 = bitcast { i64, [0 x i32] }* %4 to i8*
  tail call void @throwIfNull(i8* %6, i8* null, i64 0)
  %7 = load i64, i64* %5, align 4
  %8 = trunc i64 %7 to i32
  tail call void @throwIfOutOfBounds(i32 %i.0, i32 %8)
  %9 = sext i32 %i.0 to i64
  %10 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %4, i64 0, i32 1, i64 %9
  %11 = load i32, i32* %10, align 4
  %12 = icmp eq i32 %11, %val
  br i1 %12, label %loopEnd, label %else

loopEnd:                                          ; preds = %loop, %else, %entry
  %merge = phi i1 [ false, %entry ], [ false, %else ], [ true, %loop ]
  ret i1 %merge

else:                                             ; preds = %loop
  %13 = add nuw i32 %i.0, 1
  %14 = load i32, i32* %0, align 4
  %15 = icmp slt i32 %13, %14
  br i1 %15, label %loop, label %loopEnd
}

define i32 @"_Z24IntContainer.operator []:i->i"(%IntContainer* nocapture readonly %this, i32 %index) {
entry:
  %0 = icmp sgt i32 %index, -1
  br i1 %0, label %checkRHS, label %else

checkRHS:                                         ; preds = %entry
  %1 = getelementptr %IntContainer, %IntContainer* %this, i64 0, i32 2
  %2 = load i32, i32* %1, align 4
  %3 = icmp sgt i32 %2, %index
  br i1 %3, label %then, label %else

then:                                             ; preds = %checkRHS
  %4 = getelementptr %IntContainer, %IntContainer* %this, i64 0, i32 1
  %5 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %4, align 8
  %6 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %5, i64 0, i32 0
  %7 = bitcast { i64, [0 x i32] }* %5 to i8*
  tail call void @throwIfNull(i8* %7, i8* null, i64 0)
  %8 = load i64, i64* %6, align 4
  %9 = trunc i64 %8 to i32
  tail call void @throwIfOutOfBounds(i32 %index, i32 %9)
  %10 = sext i32 %index to i64
  %11 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %5, i64 0, i32 1, i64 %10
  %12 = load i32, i32* %11, align 4
  ret i32 %12

else:                                             ; preds = %entry, %checkRHS
  ret i32 -1
}

define void @"_Z24IntContainer.operator []:i,i->v"(%IntContainer* nocapture readonly %this, i32 %index, i32 %val) {
entry:
  %0 = icmp sgt i32 %index, -1
  br i1 %0, label %checkRHS, label %endIf

checkRHS:                                         ; preds = %entry
  %1 = getelementptr %IntContainer, %IntContainer* %this, i64 0, i32 2
  %2 = load i32, i32* %1, align 4
  %3 = icmp sgt i32 %2, %index
  br i1 %3, label %then, label %endIf

then:                                             ; preds = %checkRHS
  %4 = getelementptr %IntContainer, %IntContainer* %this, i64 0, i32 1
  %5 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %4, align 8
  %6 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %5, i64 0, i32 0
  %7 = bitcast { i64, [0 x i32] }* %5 to i8*
  tail call void @throwIfNull(i8* %7, i8* null, i64 0)
  %8 = load i64, i64* %6, align 4
  %9 = trunc i64 %8 to i32
  tail call void @throwIfOutOfBounds(i32 %index, i32 %9)
  %10 = sext i32 %index to i64
  %11 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %5, i64 0, i32 1, i64 %10
  store i32 %val, i32* %11, align 4
  br label %endIf

endIf:                                            ; preds = %checkRHS, %entry, %then
  ret void
}

; Function Attrs: norecurse
define void @"_Z13IntStack.ctor:v->v"(%IntStack* nocapture %this) local_unnamed_addr #0 {
entry:
  %0 = getelementptr %IntStack, %IntStack* %this, i64 0, i32 0, i32 2
  store i32 4, i32* %0, align 4
  %1 = getelementptr %IntStack, %IntStack* %this, i64 0, i32 0, i32 1
  %2 = tail call i8* @gc_new(i64 24)
  %3 = bitcast i8* %2 to i64*
  store i64 4, i64* %3, align 4
  %4 = bitcast { i64, [0 x i32] }** %1 to i8**
  store i8* %2, i8** %4, align 8
  %5 = getelementptr %IntStack, %IntStack* %this, i64 0, i32 1
  store i32 0, i32* %5, align 4
  ret void
}

define i1 @"_Z17IntStack.contains:i->b"(%IntStack* nocapture readonly %this, i32 %val) {
entry:
  %0 = getelementptr %IntStack, %IntStack* %this, i64 0, i32 1
  %1 = load i32, i32* %0, align 4
  %2 = icmp sgt i32 %1, 0
  br i1 %2, label %loop.preheader, label %loopEnd

loop.preheader:                                   ; preds = %entry
  %3 = getelementptr %IntStack, %IntStack* %this, i64 0, i32 0, i32 1
  br label %loop

loop:                                             ; preds = %loop.preheader, %else
  %i.0 = phi i32 [ %13, %else ], [ 0, %loop.preheader ]
  %4 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %3, align 8
  %5 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %4, i64 0, i32 0
  %6 = bitcast { i64, [0 x i32] }* %4 to i8*
  tail call void @throwIfNull(i8* %6, i8* null, i64 0)
  %7 = load i64, i64* %5, align 4
  %8 = trunc i64 %7 to i32
  tail call void @throwIfOutOfBounds(i32 %i.0, i32 %8)
  %9 = sext i32 %i.0 to i64
  %10 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %4, i64 0, i32 1, i64 %9
  %11 = load i32, i32* %10, align 4
  %12 = icmp eq i32 %11, %val
  br i1 %12, label %loopEnd, label %else

loopEnd:                                          ; preds = %loop, %else, %entry
  %merge = phi i1 [ false, %entry ], [ false, %else ], [ true, %loop ]
  ret i1 %merge

else:                                             ; preds = %loop
  %13 = add nuw i32 %i.0, 1
  %14 = load i32, i32* %0, align 4
  %15 = icmp slt i32 %13, %14
  br i1 %15, label %loop, label %loopEnd
}

; Function Attrs: norecurse nounwind readonly
define i32 @"_Z16IntStack.getSize:v->i"(%IntStack* nocapture readonly %this) local_unnamed_addr #1 {
entry:
  %0 = getelementptr %IntStack, %IntStack* %this, i64 0, i32 1
  %1 = load i32, i32* %0, align 4
  ret i32 %1
}

define void @"_Z13IntStack.push:i->v"(%IntStack* nocapture %this, i32 %val) local_unnamed_addr {
entry:
  %0 = getelementptr %IntStack, %IntStack* %this, i64 0, i32 1
  %1 = load i32, i32* %0, align 4
  %2 = add i32 %1, 1
  %3 = getelementptr %IntStack, %IntStack* %this, i64 0, i32 0, i32 2
  %4 = load i32, i32* %3, align 4
  %5 = icmp slt i32 %2, %4
  br i1 %5, label %"entry._Z23IntStack.ensureCapacity:i->v.exit_crit_edge", label %loop.i

"entry._Z23IntStack.ensureCapacity:i->v.exit_crit_edge": ; preds = %entry
  %.phi.trans.insert = getelementptr %IntStack, %IntStack* %this, i64 0, i32 0, i32 1
  %.pre3 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %.phi.trans.insert, align 8
  %6 = bitcast { i64, [0 x i32] }* %.pre3 to i8*
  br label %"_Z23IntStack.ensureCapacity:i->v.exit"

loop.i:                                           ; preds = %entry, %loop.i
  %7 = phi i32 [ %8, %loop.i ], [ %4, %entry ]
  %8 = shl i32 %7, 1
  %9 = icmp slt i32 %2, %8
  br i1 %9, label %loopEnd.i, label %loop.i

loopEnd.i:                                        ; preds = %loop.i
  %.lcssa66 = phi i32 [ %7, %loop.i ]
  %.lcssa = phi i32 [ %8, %loop.i ]
  store i32 %.lcssa, i32* %3, align 4
  %10 = shl i32 %.lcssa66, 3
  %11 = add i32 %10, 8
  %12 = sext i32 %11 to i64
  %13 = tail call i8* @gc_new(i64 %12)
  %14 = bitcast i8* %13 to i64*
  %15 = sext i32 %.lcssa to i64
  store i64 %15, i64* %14, align 4
  %16 = getelementptr %IntStack, %IntStack* %this, i64 0, i32 0, i32 1
  %17 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %16, align 8
  %18 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %17, i64 0, i32 0
  %19 = load i64, i64* %18, align 4
  %20 = icmp eq i64 %19, 0
  br i1 %20, label %deconstruction_end.i, label %CompilerInfrastructure.Expressions.UnOp_destination.i

deconstruction_end.i:                             ; preds = %CompilerInfrastructure.Expressions.UnOp_destination.i, %loopEnd.i
  %21 = bitcast { i64, [0 x i32] }** %16 to i8**
  store i8* %13, i8** %21, align 8
  %.pre = load i32, i32* %0, align 4
  %22 = bitcast i8* %13 to { i64, [0 x i32] }*
  %.pre4 = add i32 %.pre, 1
  br label %"_Z23IntStack.ensureCapacity:i->v.exit"

CompilerInfrastructure.Expressions.UnOp_destination.i: ; preds = %loopEnd.i
  %23 = icmp ugt i64 %19, %15
  %24 = select i1 %23, i64 %15, i64 %19
  %25 = getelementptr i8, i8* %13, i64 8
  %26 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %17, i64 0, i32 1, i64 0
  %27 = shl i64 %24, 2
  %28 = bitcast i32* %26 to i8*
  tail call void @llvm.memmove.p0i8.p0i8.i64(i8* align 1 %25, i8* align 1 %28, i64 %27, i1 false)
  br label %deconstruction_end.i

"_Z23IntStack.ensureCapacity:i->v.exit":          ; preds = %"entry._Z23IntStack.ensureCapacity:i->v.exit_crit_edge", %deconstruction_end.i
  %.pre-phi = phi i32 [ %2, %"entry._Z23IntStack.ensureCapacity:i->v.exit_crit_edge" ], [ %.pre4, %deconstruction_end.i ]
  %29 = phi i8* [ %6, %"entry._Z23IntStack.ensureCapacity:i->v.exit_crit_edge" ], [ %13, %deconstruction_end.i ]
  %30 = phi { i64, [0 x i32] }* [ %.pre3, %"entry._Z23IntStack.ensureCapacity:i->v.exit_crit_edge" ], [ %22, %deconstruction_end.i ]
  %31 = phi i32 [ %1, %"entry._Z23IntStack.ensureCapacity:i->v.exit_crit_edge" ], [ %.pre, %deconstruction_end.i ]
  store i32 %.pre-phi, i32* %0, align 4
  %32 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %30, i64 0, i32 0
  tail call void @throwIfNull(i8* %29, i8* null, i64 0)
  %33 = load i64, i64* %32, align 4
  %34 = trunc i64 %33 to i32
  tail call void @throwIfOutOfBounds(i32 %31, i32 %34)
  %35 = sext i32 %31 to i64
  %36 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %30, i64 0, i32 1, i64 %35
  store i32 %val, i32* %36, align 4
  ret void
}

define i32 @"_Z12IntStack.pop:v->i"(%IntStack* nocapture %this) local_unnamed_addr {
entry:
  %0 = getelementptr %IntStack, %IntStack* %this, i64 0, i32 1
  %1 = load i32, i32* %0, align 4
  %2 = icmp sgt i32 %1, 0
  br i1 %2, label %then, label %else

then:                                             ; preds = %entry
  %3 = add i32 %1, -1
  store i32 %3, i32* %0, align 4
  %4 = getelementptr %IntStack, %IntStack* %this, i64 0, i32 0, i32 1
  %5 = load { i64, [0 x i32] }*, { i64, [0 x i32] }** %4, align 8
  %6 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %5, i64 0, i32 0
  %7 = bitcast { i64, [0 x i32] }* %5 to i8*
  tail call void @throwIfNull(i8* %7, i8* null, i64 0)
  %8 = load i64, i64* %6, align 4
  %9 = trunc i64 %8 to i32
  tail call void @throwIfOutOfBounds(i32 %3, i32 %9)
  %10 = sext i32 %3 to i64
  %11 = getelementptr { i64, [0 x i32] }, { i64, [0 x i32] }* %5, i64 0, i32 1, i64 %10
  %12 = load i32, i32* %11, align 4
  ret i32 %12

else:                                             ; preds = %entry
  ret i32 -1
}

; Function Attrs: norecurse
define noalias nonnull %IntStackIterator* @"_Z20IntStack.getIterator:v->IntStackIterator"(%IntStack* %this) local_unnamed_addr #0 {
entry:
  %0 = tail call i8* @gc_new(i64 16)
  %1 = bitcast i8* %0 to %IntStackIterator*
  %2 = bitcast i8* %0 to %IntStack**
  store %IntStack* %this, %IntStack** %2, align 8
  %3 = getelementptr i8, i8* %0, i64 8
  %4 = bitcast i8* %3 to i32*
  store i32 0, i32* %4, align 4
  ret %IntStackIterator* %1
}

; Function Attrs: norecurse nounwind
define void @"_Z21IntStackIterator.ctor:IntStack->v"(%IntStackIterator* nocapture %this, %IntStack* %s) local_unnamed_addr #2 {
entry:
  %0 = getelementptr %IntStackIterator, %IntStackIterator* %this, i64 0, i32 0
  store %IntStack* %s, %IntStack** %0, align 8
  %1 = getelementptr %IntStackIterator, %IntStackIterator* %this, i64 0, i32 1
  store i32 0, i32* %1, align 4
  ret void
}

define i1 @"_Z27IntStackIterator.tryGetNext:i->b"(%IntStackIterator* nocapture %this, i32* nocapture nonnull %ret) local_unnamed_addr {
entry:
  %0 = getelementptr %IntStackIterator, %IntStackIterator* %this, i64 0, i32 1
  %1 = load i32, i32* %0, align 4
  %2 = getelementptr %IntStackIterator, %IntStackIterator* %this, i64 0, i32 0
  %3 = load %IntStack*, %IntStack** %2, align 8
  %4 = getelementptr %IntStack, %IntStack* %3, i64 0, i32 1
  %5 = load i32, i32* %4, align 4
  %6 = icmp slt i32 %1, %5
  br i1 %6, label %then, label %else

then:                                             ; preds = %entry
  %7 = bitcast %IntStack* %3 to { i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }**
  %8 = load { i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }*, { i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }** %7, align 8
  %9 = getelementptr { i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }, { i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }* %8, i64 0, i32 1
  %10 = load i32 (%IntContainer*, i32)*, i32 (%IntContainer*, i32)** %9, align 8
  %11 = getelementptr inbounds %IntStack, %IntStack* %3, i64 0, i32 0
  %12 = tail call i32 %10(%IntContainer* %11, i32 %1)
  store i32 %12, i32* %ret, align 4
  %13 = load i32, i32* %0, align 4
  %14 = add i32 %13, 1
  store i32 %14, i32* %0, align 4
  ret i1 true

else:                                             ; preds = %entry
  ret i1 false
}

define void @main() local_unnamed_addr {
"_Z27IntStackIterator.tryGetNext:i->b.exit46.preheader":
  %retPtr.i80 = alloca <2 x i64>, align 16
  %retPtr.i74 = alloca <2 x i64>, align 16
  %retPtr.i = alloca <2 x i64>, align 16
  %tmp = alloca <2 x i64>, align 16
  %tmpcast = bitcast <2 x i64>* %tmp to %string*
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  %0 = tail call i8* @gc_new(i64 32)
  %1 = bitcast i8* %0 to i8**
  store i8* bitcast ({ i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }* @IntStack_vtable to i8*), i8** %1, align 8
  %2 = getelementptr i8, i8* %0, i64 16
  %3 = bitcast i8* %2 to i32*
  store i32 4, i32* %3, align 4
  %4 = getelementptr i8, i8* %0, i64 8
  %5 = tail call i8* @gc_new(i64 24)
  %6 = bitcast i8* %5 to i64*
  store i64 4, i64* %6, align 4
  %7 = bitcast i8* %4 to i8**
  store i8* %5, i8** %7, align 8
  %8 = getelementptr i8, i8* %0, i64 24
  %9 = bitcast i8* %8 to i32*
  %10 = getelementptr i8, i8* %5, i64 8
  %11 = bitcast i8* %10 to i32*
  store i32 1, i32* %11, align 4
  %12 = getelementptr i8, i8* %5, i64 12
  %13 = bitcast i8* %12 to i32*
  store i32 2, i32* %13, align 4
  store i32 3, i32* %9, align 4
  %14 = getelementptr i8, i8* %5, i64 16
  %15 = bitcast i8* %14 to i32*
  store i32 3, i32* %15, align 4
  %16 = bitcast i8* %0 to %IntContainer*
  %17 = bitcast <2 x i64>* %retPtr.i to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %17)
  %tmpcast.i = bitcast <2 x i64>* %retPtr.i to %string*
  %18 = getelementptr %string, %string* %tmpcast.i, i32 0, i32 0
  store i8* getelementptr inbounds ([23 x i8], [23 x i8]* @4, i32 0, i32 0), i8** %18
  %19 = getelementptr %string, %string* %tmpcast.i, i32 0, i32 1
  store i64 22, i64* %19
  %20 = load <2 x i64>, <2 x i64>* %retPtr.i, align 16
  %.fca.0.load3.i = extractelement <2 x i64> %20, i32 0
  %21 = inttoptr i64 %.fca.0.load3.i to i8*
  %.fca.1.load4.i = extractelement <2 x i64> %20, i32 1
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %17)
  tail call void @cprintln(i8* %21, i64 %.fca.1.load4.i)
  %22 = bitcast i8* %0 to %IntStack*
  %23 = tail call i1 @"_Z17IntStack.contains:i->b"(%IntStack* nonnull %22, i32 4)
  %24 = select i1 %23, %string { i8* getelementptr inbounds ([5 x i8], [5 x i8]* @2, i32 0, i32 0), i64 4 }, %string { i8* getelementptr inbounds ([6 x i8], [6 x i8]* @3, i32 0, i32 0), i64 5 }
  %25 = extractvalue %string %24, 0
  %26 = extractvalue %string %24, 1
  %27 = bitcast <2 x i64>* %retPtr.i74 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %27)
  %tmpcast.i75 = bitcast <2 x i64>* %retPtr.i74 to %string*
  call void @strconcat(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @0, i64 0, i64 0), i64 18, i8* %25, i64 %26, %string* nonnull %tmpcast.i75)
  %28 = load <2 x i64>, <2 x i64>* %retPtr.i74, align 16
  %.fca.0.load3.i76 = extractelement <2 x i64> %28, i32 0
  %29 = inttoptr i64 %.fca.0.load3.i76 to i8*
  %.fca.1.load4.i77 = extractelement <2 x i64> %28, i32 1
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %27)
  tail call void @cprintln(i8* %29, i64 %.fca.1.load4.i77)
  tail call void @cprintln(i8* getelementptr inbounds ([15 x i8], [15 x i8]* @1, i64 0, i64 0), i64 14)
  tail call void @"_Z24IntContainer.operator []:i,i->v"(%IntContainer* nonnull %16, i32 3, i32 4)
  %30 = tail call i1 @"_Z17IntStack.contains:i->b"(%IntStack* nonnull %22, i32 4)
  %31 = select i1 %30, %string { i8* getelementptr inbounds ([5 x i8], [5 x i8]* @2, i32 0, i32 0), i64 4 }, %string { i8* getelementptr inbounds ([6 x i8], [6 x i8]* @3, i32 0, i32 0), i64 5 }
  %32 = extractvalue %string %31, 0
  %33 = extractvalue %string %31, 1
  %34 = bitcast <2 x i64>* %retPtr.i80 to i8*
  call void @llvm.lifetime.start.p0i8(i64 16, i8* nonnull %34)
  %tmpcast.i81 = bitcast <2 x i64>* %retPtr.i80 to %string*
  call void @strconcat(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @0, i64 0, i64 0), i64 18, i8* %32, i64 %33, %string* nonnull %tmpcast.i81)
  %35 = load <2 x i64>, <2 x i64>* %retPtr.i80, align 16
  %.fca.0.load3.i82 = extractelement <2 x i64> %35, i32 0
  %36 = inttoptr i64 %.fca.0.load3.i82 to i8*
  %.fca.1.load4.i83 = extractelement <2 x i64> %35, i32 1
  call void @llvm.lifetime.end.p0i8(i64 16, i8* nonnull %34)
  tail call void @cprintln(i8* %36, i64 %.fca.1.load4.i83)
  %37 = bitcast i8* %0 to { i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }**
  %38 = tail call i32 @"_Z24IntContainer.operator []:i->i"(%IntContainer* nonnull %16, i32 0)
  call void @to_str(i32 %38, %string* nonnull %tmpcast)
  %39 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load5372 = extractelement <2 x i64> %39, i32 0
  %40 = inttoptr i64 %.fca.0.load5372 to i8*
  %.fca.1.load5473 = extractelement <2 x i64> %39, i32 1
  tail call void @cprintln(i8* %40, i64 %.fca.1.load5473)
  br label %"_Z27IntStackIterator.tryGetNext:i->b.exit46"

"_Z27IntStackIterator.tryGetNext:i->b.exit46":    ; preds = %"_Z27IntStackIterator.tryGetNext:i->b.exit46._Z27IntStackIterator.tryGetNext:i->b.exit46_crit_edge", %"_Z27IntStackIterator.tryGetNext:i->b.exit46.preheader"
  %41 = phi { i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }* [ %.pre, %"_Z27IntStackIterator.tryGetNext:i->b.exit46._Z27IntStackIterator.tryGetNext:i->b.exit46_crit_edge" ], [ @IntStack_vtable, %"_Z27IntStackIterator.tryGetNext:i->b.exit46.preheader" ]
  %42 = phi i32 [ %46, %"_Z27IntStackIterator.tryGetNext:i->b.exit46._Z27IntStackIterator.tryGetNext:i->b.exit46_crit_edge" ], [ 1, %"_Z27IntStackIterator.tryGetNext:i->b.exit46.preheader" ]
  %43 = getelementptr { i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }, { i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }* %41, i64 0, i32 1
  %44 = load i32 (%IntContainer*, i32)*, i32 (%IntContainer*, i32)** %43, align 8
  %45 = tail call i32 %44(%IntContainer* nonnull %16, i32 %42)
  %46 = add nuw nsw i32 %42, 1
  call void @to_str(i32 %45, %string* nonnull %tmpcast)
  %47 = load <2 x i64>, <2 x i64>* %tmp, align 16
  %.fca.0.load70 = extractelement <2 x i64> %47, i32 0
  %48 = inttoptr i64 %.fca.0.load70 to i8*
  %.fca.1.load71 = extractelement <2 x i64> %47, i32 1
  tail call void @cprintln(i8* %48, i64 %.fca.1.load71)
  %49 = load i32, i32* %9, align 4
  %50 = icmp slt i32 %46, %49
  br i1 %50, label %"_Z27IntStackIterator.tryGetNext:i->b.exit46._Z27IntStackIterator.tryGetNext:i->b.exit46_crit_edge", label %loopEnd

"_Z27IntStackIterator.tryGetNext:i->b.exit46._Z27IntStackIterator.tryGetNext:i->b.exit46_crit_edge": ; preds = %"_Z27IntStackIterator.tryGetNext:i->b.exit46"
  %.pre = load { i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }*, { i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }** %37, align 8
  br label %"_Z27IntStackIterator.tryGetNext:i->b.exit46"

loopEnd:                                          ; preds = %"_Z27IntStackIterator.tryGetNext:i->b.exit46"
  ret void
}

; Function Attrs: argmemonly norecurse nounwind readnone
define i1 @IntContainer.isInstanceOf(i8* nocapture nonnull readonly %other) #3 {
entry:
  %0 = icmp eq i8* %other, bitcast ({ i1 (%IntContainer*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }* @IntContainer_vtable to i8*)
  ret i1 %0
}

; Function Attrs: norecurse
declare noalias nonnull i8* @gc_new(i64) local_unnamed_addr #0

; Function Attrs: uwtable
declare void @throwIfNull(i8* nocapture readnone, i8* readonly, i64) local_unnamed_addr #4

; Function Attrs: uwtable
declare void @throwIfOutOfBounds(i32, i32) local_unnamed_addr #4

; Function Attrs: argmemonly norecurse nounwind readonly
define i1 @IntStack.isInstanceOf(i8* nocapture nonnull readonly %other) #5 {
entry:
  %0 = icmp eq i8* %other, bitcast ({ i1 (%IntStack*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }* @IntStack_vtable to i8*)
  br i1 %0, label %returnTrue, label %hashEntry

hashEntry:                                        ; preds = %entry
  %1 = getelementptr i8, i8* %other, i64 24
  %2 = bitcast i8* %1 to i64*
  %3 = load i64, i64* %2, align 4
  %cond = icmp eq i64 %3, -1946787124084753745
  %4 = icmp eq i8* %other, bitcast ({ i1 (%IntContainer*, i32)*, i32 (%IntContainer*, i32)*, void (%IntContainer*, i32, i32)*, i64, i1 (i8*)* }* @IntContainer_vtable to i8*)
  %spec.select = and i1 %4, %cond
  br label %returnTrue

returnTrue:                                       ; preds = %hashEntry, %entry
  %merge = phi i1 [ true, %entry ], [ %spec.select, %hashEntry ]
  ret i1 %merge
}

; Function Attrs: argmemonly nounwind
declare void @llvm.memmove.p0i8.p0i8.i64(i8* nocapture, i8* nocapture readonly, i64, i1) #6

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #7

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #7

declare void @strconcat(i8* nocapture readonly, i64, i8* nocapture readonly, i64, %string* nocapture writeonly) local_unnamed_addr

declare void @to_str(i32, %string* nocapture writeonly) local_unnamed_addr

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.start.p0i8(i64, i8* nocapture) #6

; Function Attrs: argmemonly nounwind
declare void @llvm.lifetime.end.p0i8(i64, i8* nocapture) #6

attributes #0 = { norecurse }
attributes #1 = { norecurse nounwind readonly }
attributes #2 = { norecurse nounwind }
attributes #3 = { argmemonly norecurse nounwind readnone }
attributes #4 = { uwtable }
attributes #5 = { argmemonly norecurse nounwind readonly }
attributes #6 = { argmemonly nounwind }
attributes #7 = { inaccessiblememonly nounwind }

!0 = !{i64 0, !"IntContainer"}
!1 = !{i64 0, !"IntStack"}
