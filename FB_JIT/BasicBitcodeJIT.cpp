#include <cstdio>
#include <cstdlib>
#include <llvm/ExecutionEngine/ExecutionEngine.h>
#include <llvm/ExecutionEngine/GenericValue.h>
#include <llvm/ExecutionEngine/MCJIT.h>
//#include <llvm\ExecutionEngine\Interpreter.h>
#include <llvm\IR\Verifier.h>
#include <llvm/IR/Argument.h>
#include <llvm/IR/BasicBlock.h>
#include <llvm/IR/Constants.h>
#include <llvm/IR/DerivedTypes.h>
#include <llvm/IR/Function.h>
#include <llvm/IR/InstrTypes.h>
#include <llvm/IR/Instructions.h>
#include <llvm/IR/LLVMContext.h>
#include <llvm/IR/Module.h>
#include <llvm/IR/Type.h>
#include <llvm\IR\LegacyPassManager.h>
#include <llvm\IR\IRBuilder.h>
#include <llvm\IRReader\IRReader.h>
#include <llvm\Support\SourceMgr.h>
#include <llvm/Support/TargetSelect.h>
#include <vector>
#include <string>
#include <iostream>
using namespace llvm;
int main(int argc, char** argv, char *const* envp) {

	


	if (argc > 1) {
		LLVMContext context;
		SMDiagnostic smd;
		auto M = parseIRFile(argv[1], smd, context);
		//dynamische Bibiliotheken laden
		//auto nmd = M->getNamedMetadata("dynamic libraries");
		//for (auto op:nmd->operands()) {
		//	for (auto opop : op->operands()) {
		//		opop.get()->
		//	}
		//}

		/*if (verifyModule(*M, &llvm::errs())) {

			getchar();
			return 1;
		}*/
		errs() << "verifying... ";
		if (!M) {
			llvm::errs() << "Error: could not parse file: " << argv[1] << "\r\n";
			getchar();
			return 2;
		}
		if (verifyModule(*M, &errs())) {
			errs() << argv[0] << ": Error verifying Module!\n\n\n";
			getchar();
			return 1;
		}

		errs() << "OK\n";
		Function *parameterlessMain = 0, *cmdLineArgsMain = 0;
		std::vector<Function*> mains;

		for (auto& fn : M->functions()) {
			if (fn.getName() == "main") {
				mains.push_back(&fn);
				auto fntp = fn.getFunctionType();
				if (fntp->getFunctionNumParams() == 0)
					parameterlessMain = &fn;
				else if (fntp->getFunctionNumParams() == 2) {
					if (fntp->getFunctionParamType(0)->isIntegerTy() && fntp->getFunctionParamType(0) == IntegerType::getInt32Ty(context)) {
						if (fntp->getFunctionParamType(1)->isPointerTy() && fntp->getFunctionParamType(1) == PointerType::getInt8PtrTy(context)->getPointerTo()) {
							cmdLineArgsMain = &fn;
						}
					}
				}
			}
		}
		//errs() << "Starting initializing..." << "\r\n";
		//getchar();
		InitializeNativeTarget();
		InitializeNativeTargetAsmPrinter();
		std::string errStr;
		ExecutionEngine *EE =
			EngineBuilder(std::move(M))
			.setErrorStr(&errStr)
			.setEngineKind(EngineKind::JIT)
			.setVerifyModules(true)
			.setOptLevel(CodeGenOpt::Aggressive)
			.create();

		if (!EE) {
			errs() << argv[0] << ": Failed to construct ExecutionEngine: " << errStr
				<< "\n";
			getchar();
			return 1;
		}

		EE->finalizeObject();

		int res = 1;

		if (argc == 2) {
			if (parameterlessMain) {
				std::vector<GenericValue> actualArgs;
				auto ret = EE->runFunction(parameterlessMain, actualArgs).IntVal;
				errs() << "Press any key to continue...\r\n";
				getchar();
				return 0;
			}
			else if (cmdLineArgsMain) {
				std::vector<std::string> actualArgv;
				actualArgv.push_back(argv[1]);
				//llvm::outs() << "Run with one argument\r\n";
				llvm::outs().flush();
				res = EE->runFunctionAsMain(cmdLineArgsMain, actualArgv, envp);
			}
		}
		else if (cmdLineArgsMain) {
			std::vector<std::string> actualArgv;
			for (int i = 1; i < argc; i++) {
				actualArgv.push_back(argv[i]);
			}
			//llvm::outs() << "Run with " << actualArgv.size() << " aruments\r\n";
			llvm::outs().flush();
			res = EE->runFunctionAsMain(cmdLineArgsMain, actualArgv, envp);
		}
		else
			errs() << "Ungültiges Modul!" << "\r\n";
		errs() << "Press any key to continue...\r\n";
		getchar();
		return res;
	}
	else {
		errs() << "Geben Sie eine LLVM-Bitcode Datei an!" << "\r\n";
		getchar();
		return 1;
	}
}