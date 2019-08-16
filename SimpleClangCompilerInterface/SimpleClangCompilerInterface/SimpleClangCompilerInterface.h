#pragma once
#include <clang/Driver/Compilation.h>
#include <clang/Driver/Driver.h>
#include <clang/Frontend/FrontendDiagnostic.h>
#include <clang/Basic/DiagnosticOptions.h>
#include <clang/Frontend/TextDiagnosticPrinter.h>
#include <clang/Basic/VirtualFileSystem.h>
#include <clang/CodeGen/CodeGenAction.h>
#include <clang/Frontend/CompilerInstance.h>
#include <clang/Frontend/CompilerInvocation.h>

#include <llvm/Support/Host.h>
#include <llvm/Support/Program.h>
#include <llvm/Support/raw_ostream.h>
#include <llvm/Support/Path.h>
#include <llvm/InitializePasses.h>
//#include <llvm/ExecutionEngine/ExecutionEngine.h>
//#include <llvm/ExecutionEngine/MCJIT.h>
//#include <llvm/ExecutionEngine/SectionMemoryManager.h>
#include <llvm/IR/DataLayout.h>
#include <llvm/IR/LLVMContext.h>
#include <llvm/IR/PassManager.h>
#include <llvm/Passes/PassBuilder.h>
#include <llvm/Support/MemoryBuffer.h>
#include <llvm/Support/TargetSelect.h>
#include <vector>
#include <memory>
#include <atomic>

// Code copied from "http://fdiv.net/2012/08/15/compiling-code-clang-api", updated to clang 7 and changed a little bit
extern "C" __declspec(dllexport) bool compile(const char** srcNames, uint32_t srcNamec, const char* outputName, const char** dynamicLibs, uint32_t dynamicLibc, bool enableWarnings) {
	//llvm::errs() << "Compiling...";
	auto clangExe = llvm::sys::findProgramByName("clang");
	if (clangExe) {

		std::vector<const char *> args;
		args.push_back(clangExe.get().c_str());
		for (uint32_t i = 0; i < srcNamec; ++i) {
			args.push_back(srcNames[i]);
		}
		for (uint32_t i = 0; i < dynamicLibc; ++i) {
			args.push_back("-L");
			args.push_back(dynamicLibs[i]);
		}
		args.push_back("-o");
		args.push_back(outputName);

		if (!enableWarnings) {
			args.push_back("-w");
		}

		clang::TextDiagnosticPrinter *DiagClient = new clang::TextDiagnosticPrinter(llvm::outs(), new clang::DiagnosticOptions());
		clang::IntrusiveRefCntPtr<clang::DiagnosticIDs> DiagID(new clang::DiagnosticIDs());
		clang::IntrusiveRefCntPtr<clang::DiagnosticOptions> DiagOpt(new clang::DiagnosticOptions());
		clang::DiagnosticsEngine Diags(DiagID, DiagOpt, DiagClient);

		clang::driver::Driver TheDriver(args[0], llvm::sys::getDefaultTargetTriple(), Diags);

		std::unique_ptr<clang::driver::Compilation> C(TheDriver.BuildCompilation(args));

		//TheDriver.PrintActions(*C);

		int Res = 0;
		llvm::SmallVector<std::pair<int, const clang::driver::Command*>, 1> FailingCommand;
		if (C)
			Res = TheDriver.ExecuteCompilation(*C, FailingCommand);
		if (Res < 0)//TODO report error
			return false;
		return true;
	}
	else {
		llvm::errs() << clangExe.getError().message();
		return false;
	}
}

/*
std::atomic_bool isLLVMInit = false;
void LLVMInit() {
	bool old = false;
	if (isLLVMInit.compare_exchange_strong(old, true, std::memory_order_acq_rel)) {
		// We have not initialized any pass managers for any device yet.
		// Run the global LLVM pass initialization functions.
		llvm::InitializeNativeTarget();
		llvm::InitializeNativeTargetAsmPrinter();
		llvm::InitializeNativeTargetAsmParser();

		auto& Registry = *llvm::PassRegistry::getPassRegistry();

		llvm::initializeCore(Registry);
		llvm::initializeScalarOpts(Registry);
		llvm::initializeVectorization(Registry);
		llvm::initializeIPO(Registry);
		llvm::initializeAnalysis(Registry);
		llvm::initializeTransformUtils(Registry);
		llvm::initializeInstCombine(Registry);
		llvm::initializeInstrumentation(Registry);
		llvm::initializeTarget(Registry);
	}
}
extern "C" __declspec(dllexport) compileModules(llvm::Module ** mods, uint32_t modc, const char* outputName, const char** dynamicLibs, uint32_t dynamicLibc) {
	LLVMInit();

	clang::DiagnosticOptions diagnosticOptions;
	std::unique_ptr<clang::TextDiagnosticPrinter> textDiagnosticPrinter = std::make_unique<clang::TextDiagnosticPrinter>(llvm::outs(), &diagnosticOptions);
	std::unique_ptr <clang::DiagnosticIDs> diagIDs;

	std::unique_ptr<clang::DiagnosticsEngine> diagnosticsEngine =
		std::make_unique<clang::DiagnosticsEngine>(diagIDs, &diagnosticOptions, textDiagnosticPrinter.get());

	clang::CompilerInstance compilerInstance;
	auto& compilerInvocation = compilerInstance.getInvocation();

	std::vector<const char *> args;
	//args.push_back(clangExe.get().c_str());

	for (uint32_t i = 0; i < dynamicLibc; ++i) {
		args.push_back("-L");
		args.push_back(dynamicLibs[i]);
	}
	args.push_back("-o");
	args.push_back(outputName);

	clang::CompilerInvocation::CreateFromArgs(compilerInvocation, args.data(), args.data() + args.size(), *diagnosticsEngine.release());

	auto* languageOptions = compilerInvocation.getLangOpts();
	auto& preprocessorOptions = compilerInvocation.getPreprocessorOpts();
	auto& targetOptions = compilerInvocation.getTargetOpts();
	auto& frontEndOptions = compilerInvocation.getFrontendOpts();
	auto& codeGenOptions = compilerInvocation.getCodeGenOpts();

	frontEndOptions.Inputs.clear();
	compilerInstance.

}*/