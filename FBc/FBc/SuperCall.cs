/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FBc {
    [Serializable]
    public class SuperCall : ExpressionStmt {
        public SuperCall(Position pos, IType baseTp, IMethod ctor, ICollection<IExpression> args)
            : base(pos, FBSemantics.Instance.CreateCall(pos,
                        PrimitiveType.Void,
                        ctor,
                        new BaseExpression(pos, baseTp),
                        args
                    )) {
        }
    }
}
