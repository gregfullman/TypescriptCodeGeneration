using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TypescriptCodeGeneration
{
    public class CSharpNamespace : CSharpFileElement
    {
        // TODO: hopefully no projects have the same namespace...
        public string ProjectPath { get; set; }

        public override void WriteTypescript(TsOutputer outputter)
        {
            base.GetTypescriptComment(outputter);
            outputter.AppendLine("declare namespace " + DisplayText + " {");
            outputter.IncreaseIndent();
            base.WriteTypescript(outputter);
            outputter.DecreaseIndent();
            outputter.AppendLine("}");
            outputter.OutputBuilder.AppendLine();
        }

        public override void Merge(ICSharpFileElement additionalContent)
        {
            // TODO: merge summary?
            if (additionalContent is CSharpNamespace)
            {
                foreach (var ele in additionalContent.Children)
                {
                    ICSharpFileElement existing;
                    if (this.Children.TryGetValue(ele.Key, out existing))
                    {
                        existing.Merge(ele.Value);
                    }
                    else
                    {
                        this.Children.Add(ele);
                    }
                }
            }
        }

        public Dictionary<string, CSharpReference> GetReferences()
        {
            Dictionary<string, CSharpReference> referenceMap = new Dictionary<string, CSharpReference>();
            foreach (var cls in Children.Where(x => x.Value is CSharpClass))
            {
                var clsObj = cls.Value as CSharpClass;
                foreach (var refObj in clsObj.References)
                {
                    CSharpReference refCls;
                    if (!referenceMap.TryGetValue(refObj.Key, out refCls))
                    {
                        refCls = new CSharpReference { FullyQualifiedName = refObj.Key, DeclaringLocations = refObj.Value };
                        refCls.Referencers = new HashSet<string>();
                        referenceMap.Add(refObj.Key, refCls);
                    }
                    refCls.Referencers.Add(cls.Key);
                }
            }
            return referenceMap;
        }
    }
}
