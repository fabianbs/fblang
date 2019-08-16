using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CompilerInfrastructure;
using CompilerInfrastructure.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace FBc {
    static class TreeHelper {

        public static Position Position(this ParserRuleContext ctx, string fileName) {
            return (fileName, ctx.Start.Line, ctx.Start.Column);
        }
        public static Position Position(this ITerminalNode ctx, string fileName) {
            return (fileName, ctx.Symbol.Line, ctx.Symbol.Column);
        }
        public static Visibility GetVisibility(this FBlangParser.VisibilityContext ctx, Visibility dflt = Visibility.Private) {
            if (ctx is null || !Enum.TryParse<Visibility>(ctx.GetText(), true, out var ret)) {
                return dflt;
            }
            else
                return ret;
        }
    }
}
