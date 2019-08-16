using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace FBc {
    class CmdLineArgument : BasicVariable {
        bool hasShortForm, hasLongForm;
        string shortForm=null, longForm=null;


        public CmdLineArgument(Position pos, IType type, string name, IExpression defaultValue, string description)
            : base(pos, type, Variable.Specifier.LocalVariable, name, null, Visibility.Private) {
            DefaultValue = defaultValue;
            Description = description;
        }
        void InitializeShortLongForms() {
            var parts = Signature.Name.Split('_', 2);
            if (parts.Length == 1) {
                shortForm = parts[0];
                longForm = "";
                hasLongForm = false;
                hasShortForm = true;
            }
            else if (parts[0].Length == 0) {
                shortForm = "";
                hasShortForm = false;
                longForm = parts[1];
                hasLongForm = true;
            }
            else {
                shortForm = parts[0];
                longForm = parts[1];
                hasShortForm = hasLongForm = true;
            }
        }
        public string Description { get; }
        public string ShortForm {
            get {
                if (shortForm is null) {
                    InitializeShortLongForms();
                }
                return shortForm;
            }
        }
        public string LongForm {
            get {
                if (longForm is null)
                    InitializeShortLongForms();
                return longForm;
            }
        }
        public bool HasShortForm {
            get {
                if (shortForm is null)
                    InitializeShortLongForms();
                return hasShortForm;
            }
        }
        public bool HasLongForm {
            get {
                if (longForm is null)
                    InitializeShortLongForms();
                return hasLongForm;
            }
        }
    }
}
