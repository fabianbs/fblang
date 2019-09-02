/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Structure.Summaries;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using static CompilerInfrastructure.Utils.CoreExtensions;

namespace CompilerInfrastructure {
    [Serializable]
    public class Module : HierarchialContext, IEquatable<Module>/*,ISerializable*/ {
        public Module(string moduleName = "", string filename = "", IEnumerable<Module> referenceLibraries = null)
            : this(moduleName, filename, Guid.NewGuid(), referenceLibraries) {
        }
        public Module(string modulename, string filename, Guid guid, IEnumerable<Module> referenceLibraries = null)
            : base(null, referenceLibraries ?? Enumerable.Empty<IContext>(), Context.DefiningRules.All, true) {
            ModuleName = modulename;
            Filename = string.IsNullOrEmpty(filename) ? modulename : filename;
            ID = guid;
        }
        /* protected Module(SerializationInfo info, StreamingContext context):base(info, context) {
             ID = (Guid)info.GetValue(nameof(ID), typeof(Guid));
             ModuleName = info.GetString(nameof(ModuleName));
         }
         public override void GetObjectData(SerializationInfo info, StreamingContext context) {
             base.GetObjectData(info, context);
             info.AddValue(nameof(ID), ID);
             info.AddValue(nameof(ModuleName), ModuleName);
         }*/

        public Guid ID {
            get;
        }
        public string ModuleName {
            get;
        }
        public string Filename { get; }

        protected override Module TheModule => this;
        public override bool Equals(object obj) => Equals(obj as Module);
        public bool Equals(Module other) => other != null && ID.Equals(other.ID);
        public override int GetHashCode() => HashCode.Combine(ID);


        public static bool operator ==(Module module1, Module module2) => EqualityComparer<Module>.Default.Equals(module1, module2);
        public static bool operator !=(Module module1, Module module2) => !(module1 == module2);


    }
}
