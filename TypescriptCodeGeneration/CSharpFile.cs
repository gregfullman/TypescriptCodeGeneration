using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TypescriptCodeGeneration
{
    public class CSharpFile
    {
        private readonly string _sourceFile;

        private Dictionary<string, CSharpNamespace> _elements;
        public Dictionary<string, CSharpNamespace> Elements
        {
            get
            {
                if (_elements == null)
                    _elements = new Dictionary<string, CSharpNamespace>();
                return _elements;
            }
        }

        public CSharpFile(string sourceFile)
        {
            _sourceFile = sourceFile;
        }

        // get the references
        public Dictionary<string, CSharpReference> GetReferences()
        {
            Dictionary<string, CSharpReference> referenceMap = new Dictionary<string, CSharpReference>();
            foreach(var ns in Elements.Values)
                foreach(var refKvp in ns.GetReferences())
                    if(!referenceMap.ContainsKey(refKvp.Key))
                        referenceMap.Add(refKvp.Key, refKvp.Value);
            return referenceMap;
        }
    }
}
