/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure {
    [Serializable]
    public enum Visibility {
        Private,
        Internal,
        Protected,
        Public
    }
    public static class VisibilityHelper {
        public static bool CanCall(this (Visibility memberVis, IContext memberDefCtx) member, IContext callingCtx) {
            switch (member.memberVis) {
                case Visibility.Private: {
                    var callingType = callingCtx.NextTypeContext()?.Type;
                    var memberDefType = member.memberDefCtx.NextTypeContext()?.Type;
                    return member.memberDefCtx.NextTypeContext() == callingCtx.NextTypeContext() ||
                        callingType != null && memberDefType != null && callingType.IsNestedIn(memberDefType);
                }

                case Visibility.Internal:
                    return callingCtx.Module == member.memberDefCtx.Module;
                case Visibility.Protected: {
                    var callingType = callingCtx.NextTypeContext()?.Type;
                    var memberDefType = member.memberDefCtx.NextTypeContext()?.Type;
                    return callingCtx.Module == member.memberDefCtx.Module ||
                        callingType != null && memberDefType != null && (callingType.IsSubTypeOf(memberDefType) || callingType.IsNestedIn(memberDefType));
                }
                case Visibility.Public:
                    return true;
            }
            return false;
        }
        public static Visibility MinimumVisibility(IContext memberDefCtx, IContext callingCtx) {
            var callingType = callingCtx.NextTypeContext()?.Type;
            var memberDefType = memberDefCtx.NextTypeContext()?.Type;
            if (callingType != null && memberDefType != null && (callingType == memberDefType || callingType.IsNestedIn(memberDefType)))
                return Visibility.Private;
            if (callingCtx.Module == memberDefCtx.Module)
                return Visibility.Internal;
            if (callingType != null && memberDefType != null && callingType.IsSubTypeOf(memberDefType))
                return Visibility.Protected;
            return Visibility.Public;
        }
    }
}
