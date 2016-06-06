using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    public interface ITypescriptGeneratorTargetProvider
    {
        string GetTargetFilename(string namespaceStr, string containingProject);
        string GetDefaultTargetReferencesFilename(string containingProject);
    }
}
