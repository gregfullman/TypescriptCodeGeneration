using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    public class CSharpReference
    {
        public string FullyQualifiedName { get; set; }

        public HashSet<string> DeclaringLocations { get; set; }

        public HashSet<string> Referencers { get; set; }

        public void Merge(CSharpReference anotherRef)
        {
            foreach (var loc in anotherRef.DeclaringLocations)
                DeclaringLocations.Add(loc);

            foreach (var refers in anotherRef.Referencers)
                Referencers.Add(refers);
        }
    }
}
