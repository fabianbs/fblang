; ModuleID = 'TestVectorReduce2'
source_filename = "TestVectorReduce2"

@0 = private unnamed_addr constant [12 x i8] c"The sum is \00"
@1 = private unnamed_addr constant [4 x i8] c"465\00"

define void @main() local_unnamed_addr {
entry:
  tail call void @initExceptionHandling()
  tail call void @gc_init()
  tail call void @cprint(i8* getelementptr inbounds ([12 x i8], [12 x i8]* @0, i64 0, i64 0), i64 11)
  tail call void @cprintln(i8* getelementptr inbounds ([4 x i8], [4 x i8]* @1, i64 0, i64 0), i64 3)
  ret void
}

; Function Attrs: inaccessiblememonly nounwind
declare void @initExceptionHandling() local_unnamed_addr #0

; Function Attrs: inaccessiblememonly nounwind
declare void @gc_init() local_unnamed_addr #0

declare void @cprint(i8* nocapture readonly, i64) local_unnamed_addr

declare void @cprintln(i8* nocapture readonly, i64) local_unnamed_addr

attributes #0 = { inaccessiblememonly nounwind }
