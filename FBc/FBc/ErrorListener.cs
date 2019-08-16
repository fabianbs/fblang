using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Sharpen;

namespace FBc {
    class ConsoleErrorListener<T> : Antlr4.Runtime.IAntlrErrorListener<T> {
        
        public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] T offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e) {
            string IntTokenName(int tok) {
                var ret = recognizer.Vocabulary.GetDisplayName(tok);
                //if (!string.IsNullOrWhiteSpace(ret))
                    return ret;
            }
            string TokenName(T tok) {
                if (tok is int i) {
                    return IntTokenName(i);
                }
                else if (tok is IToken itok) {
                    return IntTokenName(itok.Type);
                }
                return "<TOKEN>";
            }
            Console.Error.WriteLine("line " + line + ":" + charPositionInLine + " " + msg);
            if (e is NoViableAltException alt) {
                Console.Error.WriteLine($"\t> {TokenName(offendingSymbol)} <=> {{{string.Join(", ", alt.GetExpectedTokens().ToArray().Select(x => IntTokenName(x)))}}}");
                Console.Error.WriteLine($"\t>> {{{alt.DeadEndConfigs.ConflictingAlts}}}");
            }
           
        }
        public static ConsoleErrorListener<T> Instance {
            get;
        } = new ConsoleErrorListener<T>();
    }
}
