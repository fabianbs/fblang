/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Compiler;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Type = CompilerInfrastructure.Structure.Types.Type;

namespace FBc {
    class ASTSerializer : AbstractASTSerializer {
        readonly Stack<bool> writeBody = new Stack<bool>();
        public ASTSerializer(TextWriter fout)
            : base(fout,TemplateBehavior.Declare, TemplateBehavior.Declare) {
           
            writeBody.Push(false);
        }
        
        void DeclareMembers(IStructTypeContext ctx) {
            foreach (var fld in ctx.InstanceContext.LocalContext.Variables.Concat(ctx.StaticContext.LocalContext.Variables)) {
                DeclareFieldImpl(fld.Value);
            }
            foreach (var met in ctx.InstanceContext.LocalContext.Methods.Concat(ctx.StaticContext.LocalContext.Methods)) {
                DeclareMethodImpl(met.Value);
            }
            foreach (var tm in ctx.InstanceContext.LocalContext.MethodTemplates.Concat(ctx.StaticContext.LocalContext.MethodTemplates)) {
                DeclareMethodTemplateImpl(tm.Value);
            }
            foreach (var tp in ctx.InstanceContext.LocalContext.Types.Concat(ctx.StaticContext.LocalContext.Types)) {
                DeclareTypeImpl(tp.Value);
            }
            foreach (var ttp in ctx.InstanceContext.LocalContext.TypeTemplates.Concat(ctx.StaticContext.LocalContext.TypeTemplates)) {
                DeclareTypeTemplateImpl(ttp.Value);
            }
        }
        protected override bool DeclareTypeMembers(IContext ctx) {
            if (ctx is Module)
                return base.DeclareTypeMembers(ctx);
            return true;
        }

        protected override bool DeclareTypeImpl(IType ty) {
            if (!writeBody.Peek() && ty.Visibility < Visibility.Protected)// do not serialize if not necessary
                return true;
            WriteStartElement("Type");
            WriteAttributeString("Name", ty.Signature.Name);
            WriteAttributeString("Flags", ty.TypeSpecifiers.ToString());
            WriteAttributeString("Visibility", ty.Visibility.ToString());
            DeclareMembers(ty.Context);
            WriteEndElement();
            return true;
        }
        void WriteLiteral(ILiteral lit, string tagname = "Literal", params (string name, object value)[] attributes) {
            WriteStartElement(tagname);
            if (attributes != null) {
                foreach (var (name, value) in attributes) {
                    WriteAttributeString(name, value.ToString());
                }
            }
            WriteTypeRef(lit.ReturnType, "LiteralType");
            WriteElementString("LiteralValue", lit.ToString());

            WriteEndElement();
        }
        void WriteTypeRef(IType tp, string tagname, params (string name, object value)[] attributes) {
            WriteStartElement(tagname);
            const string agg = "AggregateType";
            const string kind = "Kind";
            if (attributes != null) {
                foreach (var (name, value) in attributes) {
                    WriteAttributeString(name, value.ToString());
                }
            }
            switch (tp) {
                case ByRefType brt:
                    WriteTypeRef(brt.UnderlyingType, agg, (kind, "ByRef"));
                    break;
                case FixedSizedArrayType fsa:
                    WriteTypeRef(fsa.ItemType, agg, (kind, "FixedSizedArray"), ("Length", fsa.ArrayLength));
                    break;
                case VarArgType vat:
                    WriteTypeRef(vat.ItemType, agg, (kind, "VarArg"));
                    break;
                case SpanType stp:
                    WriteTypeRef(stp.ItemType, agg, (kind, "Span"));
                    break;
                case ArrayType art:
                    WriteTypeRef(art.ItemType, agg, (kind, "Array"));
                    break;
                case RefConstrainedType rct:
                    WriteTypeRef(rct.UnderlyingType, agg, (kind, "ReferenceCapability"), ("Capability", rct.ReferenceCapability));
                    break;
                case AwaitableType atp:
                    WriteTypeRef(atp.ItemType, agg, (kind, "Awaitable"));
                    break;
                case PrimitiveType prim:
                    WriteStartElement("PrimitiveType");
                    WriteString(prim.Signature.ToString());
                    WriteEndElement();
                    break;
                case EnumType etp:
                    WriteStartElement("EnumType");
                    WriteString(etp.Signature.ToString());
                    WriteEndElement();
                    break;
                case GenericTypeParameter gtp:
                    WriteStartElement("GenericTypeParameter");
                    WriteString(gtp.Signature.ToString());
                    WriteEndElement();
                    break;
                default:
                    //classtype
                    WriteStartElement("ClassType");
                    WriteString(tp.Signature.Name);
                    int index = 0;
                    foreach (var genArg in tp.Signature.GenericActualArguments) {
                        if (genArg is IType ty) {
                            WriteTypeRef(ty, "ActualTypeParameter", ("Index", index));
                        }
                        else if (genArg is ILiteral lit) {
                            WriteLiteral(lit, "ActualLiteralparameter", ("Index", index));
                        }
                        index++;
                    }
                    WriteEndElement();
                    break;
            }

            WriteEndElement();
        }
        void WriteGenericParameter(IGenericParameter gen) {
            if (gen is GenericTypeParameter genTp) {
                WriteStartElement("TypeParameter");
                WriteAttributeString("Name", genTp.Signature.Name);
                if (genTp.SuperType != null) {
                    WriteTypeRef(genTp.SuperType, "Supertype");
                }
                foreach (var intf in genTp.ImplementingInterfaces) {
                    WriteTypeRef(intf, "ImplementingInterface");
                }
            }
            else if (gen is GenericLiteralParameter genLit) {
                WriteStartElement("LiteralParameter");
                WriteAttributeString("Unsigned", genLit.IsUnsigned.ToString());
                WriteAttributeString("Name", genLit.Name);
                WriteTypeRef(genLit.ReturnType, "LiteralType");
            }
            else {
                throw new ArgumentException($"Invalid generic parameter: {gen}");
            }
            WriteEndElement();
        }
        protected override bool DeclareTypeTemplateImpl(ITypeTemplate<IType> ty) {
            if (!writeBody.Peek() && ty.Visibility < Visibility.Protected)// do not serialize if not necessary
                return true;
            WriteStartElement("TypeTemplate");
            WriteAttributeString("Name", ty.Signature.Name);
            WriteAttributeString("Flags", ty.TypeSpecifiers.ToString());
            WriteAttributeString("GenericParameters", ty.Signature.GenericFormalparameter.Count.ToString());
            WriteAttributeString("Visibility", ty.Visibility.ToString());
            foreach (var gen in ty.Signature.GenericFormalparameter) {
                WriteGenericParameter(gen);
            }
            writeBody.Push(true);
            DeclareMembers(ty.Context);
            writeBody.Pop();
            WriteEndElement();
            return true;
        }
        void WriteBody(IStatement body) {
            //TODO
        }
        protected override bool DeclareMethodImpl(IMethod met) {
            if (!writeBody.Peek() && met.Visibility < Visibility.Protected && met.Signature.Name != "main" || met.IsInternal() || met.IsExternal())// do not serialize if not necessary
                return true;
            WriteStartElement("Method");
            WriteAttributeString("Name", met.Signature.Name);
            WriteAttributeString("Flags", met.Specifiers.ToString());
            WriteAttributeString("Visibility", met.Visibility.ToString());
            int index = 0;
            foreach (var arg in met.Arguments) {
                WriteVariable(arg, "Argument", ("Index", index));
                index++;
            }
            WriteTypeRef(met.ReturnType ?? Type.GetPrimitive(PrimitiveName.Void), "ReturnType");
            if (writeBody.Peek() && met.Body != null) {
                WriteStartElement("Body");
                WriteBody(met.Body.Instruction);
                WriteEndElement();
            }
            WriteEndElement();
            return true;
        }
        protected override bool DeclareMethodTemplateImpl(IMethodTemplate<IMethod> met) {
            if (!writeBody.Peek() && met.Visibility < Visibility.Protected)// do not serialize if not necessary
                return true;
            WriteStartElement("MethodTemplate");
            WriteAttributeString("Name", met.Signature.Name);
            WriteAttributeString("Flags", met.Specifiers.ToString());
            WriteAttributeString("Visibility", met.Visibility.ToString());
            int index = 0;
            foreach (var arg in met.Arguments) {
                WriteVariable(arg, "Argument", ("Index", index));
                index++;
            }
            foreach (var gen in met.Signature.GenericFormalparameter) {
                WriteGenericParameter(gen);
            }
            WriteTypeRef(met.ReturnType ?? Type.GetPrimitive(PrimitiveName.Void), "ReturnType");
            if (met.Body != null) {
                WriteStartElement("Body");
                WriteBody(met.Body.Instruction);
                WriteEndElement();
            }
            WriteEndElement();
            return true;
        }
        protected override bool ImplementMethod(IMethod met) => true;
        protected override bool ImplementMethodTemplate(IMethodTemplate<IMethod> met) => true;
        protected override bool ImplementMethodImpl(IMethod met) {
            return true;
        }
        protected override bool ImplementMethodTemplateImpl(IMethodTemplate<IMethod> met) => true;
        void WriteVariable(IVariable vr, string tagname, params (string name, object value)[] attributes) {
            WriteStartElement(tagname);
            if (attributes != null) {
                foreach (var (name, value) in attributes) {
                    WriteAttributeString(name, value.ToString());
                }
            }
            WriteAttributeString("Name", vr.Signature.Name);
            WriteAttributeString("Flags", vr.VariableSpecifiers.ToString());
            WriteTypeRef(vr.Type, "VarType");

            WriteEndElement();
        }
        protected override bool DeclareFieldImpl(IVariable fld) {
            WriteVariable(fld, "Field", ("Visibility", fld.Visibility));
            return true;
        }
        
    }
}
