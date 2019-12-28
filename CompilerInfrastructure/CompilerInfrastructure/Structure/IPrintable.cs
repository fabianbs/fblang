/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompilerInfrastructure.Structure {
    public interface IPrintable {
        void PrintPrefix(TextWriter tw);
        void PrintValue(TextWriter tw);
        void PrintSuffix(TextWriter tw);
    }
    public static class PrintableHelper {
        public static void PrintTo(this IPrintable p, TextWriter tw) {
            if (p != null) {
                p.PrintPrefix(tw);
                p.PrintValue(tw);
                p.PrintSuffix(tw);
            }
        }
        public static string PrintString(this IPrintable p) {
            using var sw = new StringWriter();
            p.PrintTo(sw);
            return sw.ToString();
        }
    }
}
