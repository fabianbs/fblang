/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

grammar FBlang;

/*
 * Parser Rules
 */
topStmt: stmt NewLines?EOF;
topInstruction:instruction NewLines? EOF;


program: global* NewLines? EOF;
//module: NewLines? '#module' '{' global* NewLines?'}';
global: NewLines? (namespace |globalMethod|('global' mainMethodDef)|globalMacro|typeDef);
globNspVis: Public|'internal';
namespace:  globNspVis? 'namespace' Ident? OpenBracket global* NewLines?'}';
globalMethod: NewLines? 'global' methodDef;
globalMacro: NewLines? 'global' macroDef;
typeDef: NewLines?'def' (classDef|voidDef|intfDef|enumDef|extensionDef); // other

/**
 * ClassDef & VoidDef & ActorDef
 */
voidDef: visibility? 'void' Ident genericDef? (':'intfList)? classBody;
classDef: visibility? (Abstract|Final)? (Class|Actor|typeQualifier) Ident genericDef? (':'intfList)? classBody;
genericDef: LT genericFormalArglist GT;
genericFormalArgument: genericLiteralParameter | genericTypeParameter;
genericLiteralParameter: GenLit Unsigned? primitiveName Ident;
genericTypeParameter: typeQualifier? Ident (':' intfList)?;
genericFormalArglist: genericFormalArgument (',' genericFormalArgument)*;
intfList: typeQualifier (','typeQualifier)*;
classBody: NewLines*OpenBracket memberDef* NewLines*'}';
memberDef: NewLines* (fieldDef stmtEnd | methodDef | typeDef | macroDef);

/**
 * intfDef
 */
intfDef: visibility? 'interface' Ident genericDef? (':'intfList)? intfBody;
intfBody: NewLines?OpenBracket (methodSig stmtEnd)* NewLines?'}';
methodSig:NewLines? methodName genericDef? ':' argTypeList? ('->' (localTypeIdent | Async?))?;
argTypeList: argTypeIdent (',' argTypeIdent)*;

/**
 * enumDef
 */
enumDef: visibility? 'enum' Ident OpenBracket enumItems NewLines? '}';
enumItems: enumItem (',' enumItem)*;
enumItem: NewLines? Ident ('=' expr)?;

/**
 * ExtensionDef
 */
extensionDef: 'extension' genericDef? typeIdent (':' intfList)? classBody;

/**
 * Little Components
 */
visibility: globNspVis | 'private' | 'protected';
typeQualifier: typeName genericActualParameters?;
typeModifier: ReferenceCapability? (Async|Iterator|Iterable);
typeIdent: typeModifier* (ReferenceCapability? (typeQualifier|Async) (ExclamationMark|Dollar)?) typeSpecifier*;

typeName:Ident|primitiveName;
primitiveName:'nativehandle'|'bool'|'byte'|'char'|'short'|'ushort'|'int'|'zint'|'uint'|'long'|'ulong'|'biglong'|'ubiglong'|'float'|'double'|'string';
typeSpecifier: typeModifier* ReferenceCapability?(fsArray|(array|assocArray) ExclamationMark?) ;
fsArray: '[' (IntLit|Ident) ']'Dollar;
array: '['']';
assocArray: '[' typeIdent ']';
genericActualParameters: LT genericActualParameter(',' genericActualParameter)* GT;
genericActualParameter: Ident| typeIdent |BoolLit| Minus?(IntLit|FloatLit)| StringLit|CharLit;

localTypeIdent: typeIdent Pointer? Amp?;
argTypeIdent: localTypeIdent Dots?;

/**
 * MethodDef
 */
methodDef:NewLines? visibility? (Virtual|Abstract|Static|Init|Copy)?  (localTypeIdent|Async)? methodName genericDef? '(' argList? NewLines?')' (blockInstruction|NewLines?'->'NewLines? expr);
mainMethodDef: NewLines? primitiveName? 'main' '(' cmdLineParamList? NewLines? ')' blockInstruction;
methodName: 'operator' overloadableOperator|Ident;
overloadableOperator: '('')'|'['']'|LT|GT|LE|GE|Equals|LeftArrow|'+'|Minus|Pointer|Div|Percent|IncDec|'bool'|'string'|LShift|srShift|urShift|Amp|'|'|Xor|Tilde|ExclamationMark;

argList: typedParam (',' typedParam)*;
typedParam: NewLines? (Final|Volatile)? argTypeIdent (ExclamationMark|Dollar)? Ident (',' Ident)*;
cmdLineParamList: cmdLineParams (',' cmdLineParams)*;
cmdLineParams: NewLines? typeIdent cmdLineParam (',' cmdLineParam)*;
cmdLineParam: Ident (NewLines?':'NewLines? StringLit)? (NewLines?'='NewLines? literal)?;

/**
 * MacroDef
 */
macroDef:NewLines? visibility? 'macro' macroName=Ident '(' macroArgs? ')' blockInstruction;
macroArg: Dots? (macroExpressionArg|macroStatementArg);
macroArgs: macroArg (',' macroArg)*;
macroExpressionArg: Ident;
macroStatementArg: OpenBracket Ident '}';

/**
 * FieldDef
 */

fieldDef: visibility?  (varType names (('=' expr)|(LeftArrow(expr|typeIdent)))?) 
					  | includingFieldDef;
includingFieldDef: Including varType Ident 
					(NewLines? As typeQualifier)? 
					(NewLines?'=' expr)? 
					(NewLines? Except methodSig (NewLines?','methodSig)*)?;
varType: (Static(Final|Volatile)? | (Final|Volatile)Static?)? typeIdent ;
names: Ident (',' Ident)*;


/**
 * Instructions
 */
//newLines:NewLines;
blockInstruction :NewLines*OpenBracket instruction*? NewLines*'}';
//instructionNoIf:NewLines* (inst| blockInstruction);
instruction: NewLines* (inst| blockInstruction);
inst:		  ifStmt
			 |whileLoop
			 |doWhileLoop
			 |forLoop
			 |foreachLoop
			 |concForeachLoop
			 |switchStmt
			 |tryCatchStmt
			 |stmt stmtEnd
;
tryCatchStmt:'try' blockInstruction ('catch' ('(' decl ')')? blockInstruction)+ (Finally blockInstruction)?
			|'try' blockInstruction Finally blockInstruction
;
stmt: declaration|assignExp|awaitStmt|deferStmt|deconstructStmt|shiftStmt|superStmt|callStmt|macroStmt|incStmt|returnStmt|yieldStmt|breakStmt|continueStmt|identStmt;
stmtEnd: ';'|NewLines ';'|{_input.La(1)==NewLines||_input.La(1)==OpenBracket}?;
identStmt: Ident;
ifNoElseStmt:'if' NewLines* '(' expr NewLines* ')'NewLines* instruction NewLines*;
ifStmt: ifNoElseStmt (Else instruction NewLines*)?;

whileLoop: 'while' NewLines* '('expr NewLines* ')' instruction NewLines*;
doWhileLoop: Do instruction NewLines? 'while' '(' expr ')' stmtEnd;
forLoop: 'for' '(' (forInit)? ',' expr? ',' stmt? ')' instruction;
forInit: declaration|assignExp|deconstructStmt;
foreachLoop: 'for' Simd? '(' foreachInit ':' expr ')'instruction;
concForeachLoop: 'concurrentFor' Simd? '(' decl ':' expr ')'instruction;
foreachInit: decl|actualArglist;
superStmt: Super '(' actualArglist? NewLines?')';
// Advanced Pattern Matching
switchStmt: 'switch' '(' expr')' OpenBracket case* NewLines?'}';
case: NewLines?'case' casePattern ('|' casePattern )* instruction						#normalCase
	//| NewLines?'case' (casePattern '|')* nonRecursiveCasePattern blockInstruction		#biCase
	| NewLines? Else  instruction														#elseCase
;
recursiveCasePattern: typeIdent? '('casePatternList')';
casePatternList: casePattern (',' casePattern)*;
nonRecursiveCasePattern: literal | sdecl | Percent;
casePattern: nonRecursiveCasePattern|recursiveCasePattern;

localVarTy: (Final|Volatile)? (localTypeIdent|Var Amp?);
localVarName: Ident|MacroLocalIdent;
sdecl: Public? localVarTy localVarName ('=' ex)?;
decl: Public? localVarTy localVarName (',' localVarName)*;
declaration : decl(('=' expr)|(LeftArrow(expr|typeIdent)))?;
//assignStmt: assignEx;
awaitStmt: Await ex;
deferStmt: Defer ex;
returnStmt: 'return' expr?;
yieldStmt: 'yield' expr?;
deconstructStmt: actualArglist LeftArrow (expr|typeIdent);
callStmt:  ex '('actualArglist?NewLines?')'												#normalCallStmt
		| ('['(InternalCall|ExternalCall)']')? Ident '(' actualArglist?NewLines? ')'	#internalExternalCallStmt
		;
macroStmt: (ex Dot)? At Ident '(' actualArglist?NewLines?')'							#macroCall
		  |(ex Dot)? At Ident instruction												#macroCapture
		  ;
shiftStmt: ex (LShift|srShift|urShift) ex;
incStmt: (pre=IncDec ex)|(ex post=IncDec);
breakStmt:'break';
continueStmt:'continue';

/**
 * Expressions
 */

assignExp: ex assignOp expr;
expr: assignExp | ex;
// expr: NewLines? exp;
ex:     NewLines?'(' expr NewLines?')'													#subExpr
	  | NewLines? '[' actualArglist  NewLines?']'										#arrInitializerExpr
	  | NewLines?literal																#literalExpr
	  | NewLines?This																	#thisExpr
	  | NewLines?Super																	#superExpr
	  |lhs=ex Dot rhs=ex																#memberAccessExpr//TODO typeIdent '.' ex
	  | ex '(' actualArglist? NewLines? ')'												#callExpr
	  | '['(InternalCall|ExternalCall)']' Ident '(' actualArglist? NewLines? ')'		#internalExternalCallExpr
	  | ex '[' actualArglist NewLines?']'												#indexerExpr
	  | ex '[' offset=expr? ':' count=expr? ']'											#sliceExpr
	  |NewLines?'(' localTypeIdent ')' ex												#typecastExpr
	  |NewLines?(Minus|Tilde|ExclamationMark|Defer|Await|IncDec) ex						#preUnopExpr
	  |ex (Dots|IncDec)																	#postUnopExpr
	  |'new' typeIdent? '(' actualArglist? NewLines?')'									#newObjExpr
	  |'new' typeIdent? '[' actualArglist NewLines?']'									#newArrExpr
	  |(Reduce|'concurrentReduce') reduction '(' collection=expr (':' seed=expr)? ')'	#reduceExpr
	  |'(' names? ')' '->' (expr|blockInstruction)										#lambdaExpr
	  | Ident '->' (expr|blockInstruction)												#lambdaFnExpr
	  |ex (Pointer|Div|Percent) ex														#mulExpr
	  |ex ('+'|Minus) ex																#addExpr
	  |ex (LShift|srShift|urShift) ex													#shiftExpr
	  |ex orderOp ex																	#orderExpr
	  |NewLines?lhs=typeIdent 'is' rhs=typeIdent										#isTypeExpr
	  |ex As localTypeIdent																#asTypeExpr
	  |ex 'instanceof' typeIdent														#instanceofExpr
	  |ex (Equals|NotEquals|LT GT) ex													#equalsExpr
	  |concForeachLoop																	#concurrentForExpr
	  |ex Amp ex																		#andExpr
	  |ex Xor ex																		#xorExpr
	  |ex '|' ex																		#orExpr
	  |ex '&&' ex																		#landExpr
	  |ex '||' ex																		#lorExpr
	  |ex 'to' ex																		#contiguousRangeExpr
	  |ex '?' ex ':' ex																	#conditionalExpr
	  |Public? Var localVarName '=' ex													#declExpr
	  //|<assoc=right>expr assignOp expr
	  ;
orderOp: LT|LE|GE|GT;
//name: Ident ('.') Ident;
srShift: GT GT;
urShift: GT GT GT;
assignOp: ModifierAssignOp|'=';
literal:  Ident|MacroLocalIdent|StringLit|CharLit|Minus?(IntLit|FloatLit)|BoolLit| Null|Default;
actualArglist: expr (',' expr)*;
reductionOperator: '+'|Minus|Pointer|Div|Percent|Amp|'|'|Xor;
reduction: reductionOperator|Ident;
/*
 * Lexer Rules
 */

WS
	:	(' '|'\t') -> channel(HIDDEN)
	;
Public: 'public';
ReferenceCapability: 'unique' | 'const' | 'immutable';
Class: 'class';
Actor: 'actor';
Abstract: 'abstract';
Virtual: 'virtual';
Final: 'final';
GenLit: 'Literal';
Unsigned:'unsigned';
Loop:'loop';
Do:'do';
Defer:'defer';
Volatile: 'volatile';
Static: 'static';
Async: 'async';
Finally: 'finally';
Default: 'default';
Simd: 'simd';
Else: 'else';
This: 'this';
Super: 'super';
Init: 'init';
Copy: 'copy';
As: 'as';
Except: 'except';
Including: 'including';
InternalCall:'InternalCall';
ExternalCall:'ExternalCall';
Amp: '&';
Dots: '...';
Pointer: '*';
Tilde:'~';
At:'@';
Xor:'^';
Percent:'%';
Await:'await';
Null: 'null';//maybe nil or none instead of null?
BoolLit: 'true'|'false';
Iterable: 'iterable';
Iterator: 'iterator';
Reduce: 'reduce';
Var: 'var';
Ident: [_a-zA-Z][_a-zA-Z0-9]*;
MacroLocalIdent: Percent Ident;
OpenBracket: '{';
ExclamationMark: '!';
Dollar: '$';
Minus: '-';
LeftArrow:'<-';
Equals: '==';
NotEquals: '!=';
LT: '<';
LE:'<=';
GE:'>=';
GT:'>';
LShift:'<<';
Div: '/';


fragment NEW_LINE: ('\r'? '\n')|'\r';
NewLines: NEW_LINE (WS* NEW_LINE)*;
fragment L: 'l'|'L';
fragment S: 's'|'S';
fragment B: 'b'|'B';
fragment U: 'u'|'U';
fragment X: 'x'|'X';
fragment Z: 'z'|'Z';
fragment DecimalInt: (([1-9][0-9]*)|'0')(L|S|B|U L|U S|U|Z)?;
fragment HexByte: [0-9a-fA-F][0-9a-fA-F];
fragment HexInt: '0'X HexByte+;
fragment BinInt: '0'B [01]+;
IntLit: DecimalInt | HexInt | BinInt;
StringLit: '"' (~["\\] | '\\"')*? '"';
CharLit: '\'' ('\\\''|'\\'?.|'\\u'HexByte HexByte) '\'';

FloatLit: ((DecimalInt?'.')?DecimalInt ([eE]('+'|Minus)?DecimalInt)[fF]?)
		| ('0x' (HexByte+?'.')? HexByte+ ([eE]('+'|Minus)? HexByte+)[fF]?)
		| ('0b' ([01]+?'.')? [01]+ ([eE]('+'|Minus)? [01]+)[fF]?);

Dot: '.';
IncDec:'++'|'--';
ModifierAssignOp: '+='|'-='|'*='|'/='|'%='|'|='|'&='|'^='|'<<='|'>>='|'>>>=';

LineComment: '//'.*?NEW_LINE->channel(HIDDEN);
BlockComment: '/*' .*? '*/'->channel(HIDDEN);
