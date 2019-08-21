/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Contexts.DataFlowAnalysis {
    public interface IDataFlowValue {
    }
    [Serializable]
    public sealed class TopBottomElement : IDataFlowValue {
        public static readonly IDataFlowValue
            Top = new TopBottomElement(),
            Bottom = new TopBottomElement();
        private TopBottomElement() {

        }
    }
    public static class DataFlowValueHelper {
        public static bool IsTopOrBottom(this IDataFlowValue val) {
            return val is TopBottomElement;
        }
        public static bool IsTop(this IDataFlowValue val) {
            return val == TopBottomElement.Top;
        }
        public static bool IsBottom(this IDataFlowValue val) {
            return val == TopBottomElement.Bottom;
        }
    }
    public static class DataFlowValue {
        public static IDataFlowValue Top => TopBottomElement.Top;
        public static IDataFlowValue Bottom => TopBottomElement.Bottom;
    }
}
