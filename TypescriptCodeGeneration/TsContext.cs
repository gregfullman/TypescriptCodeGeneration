using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    class TsContext
    {
        private IDictionary<string, string> mappings;

        public TsContext()
        {
            mappings = new Dictionary<string, string>(TypeMapping.DefaultMappings);
        }


        public void RegisterType(string type, string output)
        {
            if (mappings.ContainsKey(type))
                mappings[type] = output;
            else
                mappings.Add(type, output);
        }

        public string GetTsType(ITypeSymbol typeSymbol, Dictionary<string, HashSet<string>> references)
        { 
            return TypeMapping.GetTsType(typeSymbol, mappings, references); 
        }
    }
}
