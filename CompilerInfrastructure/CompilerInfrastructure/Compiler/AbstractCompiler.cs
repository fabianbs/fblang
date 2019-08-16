using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Compiler {
    /// <summary>
    /// An abstract class, which implements the <see cref="ICompiler"/>-interface and extends it a bit
    /// </summary>
    public abstract class AbstractCompiler : ICompiler {
        /// <summary>
        /// A flag, which indicates whether code should actually be generated. When turned off, 
        /// only the lexical-, syntax- and semantic analyses are performed
        /// </summary>
        public abstract bool DoGenerateCode {
            get;
        }
        /// <summary>
        /// Performs the actual compilation
        /// </summary>
        /// <param name="sources">The paths to the source-files</param>
        /// <param name="destination">The destination path</param>
        /// <param name="references">The paths to the reference-libraries</param>
        /// <returns>True on success, false otherwise</returns>
        public virtual bool Compile(string[] sources, string destination, string[] references) {
            var doCodeGen = Analyze(sources, references, out var mod);
            if (doCodeGen && DoGenerateCode) {
                var cg = GetCodeGenerator(destination);
                try {
                    return cg != null && cg.CodeGen(mod);
                }
                finally {
                    if (cg is IDisposable disp)
                        disp.Dispose();
                }
            }
            return doCodeGen;
        }
        /// <summary>
        /// Performs the lexical-, syntax- and semantic analyses and builds a <see cref="Module"/>
        /// which contains all relevant information. This method is called by <see cref="Compile(string[], string, string[])"/>. Do not call it yourself!
        /// </summary>
        /// <param name="sources">The paths to the source-files</param>
        /// <param name="references">The paths to the reference-libraries</param>
        /// <param name="mod">The module, which is built</param>
        /// <returns>True, when no errors were reported, false otherwise</returns>
        protected abstract bool Analyze(string[] sources, string[] references, out Module mod);
        /// <summary>
        /// Creates or retrieves a <see cref="ICodeGenerator"/>, which can generate the actual output-code from a <see cref="Module"/>.
        /// This method is called by <see cref="Compile(string[], string, string[])"/>
        /// </summary>
        /// <param name="destination">The destination path</param>
        /// <returns>The code-generator</returns>
        protected abstract ICodeGenerator GetCodeGenerator(string destination);
    }
}
