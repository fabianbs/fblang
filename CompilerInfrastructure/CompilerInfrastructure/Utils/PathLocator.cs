using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public static class PathLocator {
        static readonly Lazy<string[]> pathFolders = new Lazy<string[]>(() => {
            try {
                return Environment.GetEnvironmentVariable("path").Split(';');
            }
            catch {
                return null;
            }
        });

        public static bool TryLocateFile(string name, out string fullName) {
            if (!File.Exists(name)) {
                foreach (var nom in pathFolders.Value.Select(x => Path.Combine(x, name))) {
                    if (File.Exists(nom)) {
                        fullName = nom;
                        return true;
                    }
                }
                fullName = string.Empty;
                return false;
            }
            else {
                fullName = Path.GetFullPath(name);
                return true;
            }
        }
    }
}
