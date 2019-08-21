/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Compiler;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace FBc {
    abstract class AbstractASTSerializer : AbstractCodeGenerator, IDisposable {

        bool isDisposed = false;
        IncrementalHash hash;
        XDocument doc;
        readonly Stack<XElement> elems = new Stack<XElement>();
        private readonly TextWriter fout;

        public AbstractASTSerializer(TextWriter fout, TemplateBehavior declaresTypeTemplates, TemplateBehavior declaresMethodTemplates)
            : base(declaresTypeTemplates, declaresMethodTemplates) {
            this.fout = fout;

            /*var settings = new XmlWriterSettings {
                CloseOutput = true,
                Indent = true,
                NewLineHandling = NewLineHandling.Entitize
            };*/

            //output = XmlWriter.Create(fout, settings);

            //output.WriteStartDocument();
        }
        protected void WriteStartElement(string name) {

            var nw = //doc.CreateElement(name);
                new XElement(name);
            if (elems.TryPeek(out var parent))
                parent.Add(nw);
            // elems.Peek().AppendChild(nw);
            elems.Push(nw);
            hash.AppendData(Encoding.UTF8.GetBytes(name));
        }
        protected void WriteAttributeString(string attName, object value) {
            string valueStr = value?.ToString() ?? "";
            elems.Peek().SetAttributeValue(attName, valueStr);
            //elems.Peek().SetAttribute(attName, valueStr);
            hash.AppendData(Encoding.UTF8.GetBytes(attName + valueStr));
        }
        protected void WriteEndElement() {
            elems.TryPop(out _);
        }
        protected void WriteString(string value) {
            var nod =// doc.CreateTextNode(value);
                new XText(value);
            //elems.Peek().AppendChild(nod);
            elems.Peek().Add(nod);
            hash.AppendData(Encoding.UTF8.GetBytes(value));
        }
        protected void WriteElementString(string tagname, string value) {
            //var el = doc.CreateElement(tagname);
            //var cont = doc.CreateTextNode(value);
            //el.AppendChild(cont);
            //elems.Peek().AppendChild(el);
            elems.Peek().Add(new XElement(tagname, value));
            hash.AppendData(Encoding.UTF8.GetBytes(tagname + value));
        }
        protected sealed override bool DoCodeGen(Module mod) {
            try {
                
                hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                WriteStartElement("Module");
                WriteAttributeString("Name", mod.Name);
                var ret = base.CodeGen(mod);
                elems.Peek().SetAttributeValue("Hash", Convert.ToBase64String(hash.GetHashAndReset()));
                //output.WriteAttributeString("Hash", Convert.ToBase64String(hash.GetHashAndReset()));
                doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), elems.Peek());
                WriteEndElement();

                //doc.WriteContentTo(output);
                doc.Save(fout);
                return ret;
            }
            catch (Exception e) {
                e.Message.Report();
                return false;
            }
        }
        public void Dispose() {
            if (!isDisposed) {
                isDisposed = true;
                fout.Dispose();
            }
        }
    }
}
