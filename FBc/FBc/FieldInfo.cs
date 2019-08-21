/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace FBc {
    readonly struct FieldInfo {
        public readonly FBlangParser.ExprContext DefaultValue;
        public readonly bool IsIncluding;
        public readonly IType IncludingInterface;
        public readonly ISet<Method.Signature> NotInclude;
        public FieldInfo(FBlangParser.ExprContext defaultValue, bool isIncluding=false,IType includesInterface=null,ISet< Method.Signature> notInclude=null) {
            DefaultValue = defaultValue;
            IsIncluding = isIncluding;
            IncludingInterface = includesInterface;
            if(IsIncluding && includesInterface is null) {
                throw new ArgumentException();
            }
            NotInclude = notInclude ?? Set.Empty<Method.Signature>();
        }
    }
}
