using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    public class CSharpClass : CSharpFileElement
    {
        private Dictionary<string, HashSet<string>> _references;
        /// <summary>
        /// The key for this dictionary indicates the fully qualified name for the reference.
        /// The value HashSet contains the locations for all declarations of the reference.
        /// </summary>
        public Dictionary<string, HashSet<string>> References
        {
            get
            {
                if (_references == null)
                    _references = new Dictionary<string, HashSet<string>>();
                return _references;
            }
        }

        public void AddReferences(Dictionary<string, HashSet<string>> references)
        {
            foreach (var kvp in references)
            {
                if (!References.ContainsKey(kvp.Key))
                    References.Add(kvp.Key, kvp.Value);
                else
                    References[kvp.Key].AddRange(kvp.Value);
            }
        }

        public bool IsPartial { get; set; }

        public bool IsReferenced { get; set; }

        public string ExtendedInterface { get; set; }

        private string GetInterfaceExtensionString()
        {
            string result = "";
            if (!string.IsNullOrWhiteSpace(ExtendedInterface))
                result = " extends " + ExtendedInterface + " ";
            return result;
        }

        public override void WriteTypescript(TsOutputer outputter)
        {
            if (Children.Count > 0 || IsReferenced)
            {
                base.GetTypescriptComment(outputter);

                outputter.AppendLine("interface " + DisplayText + GetInterfaceExtensionString() + " {");
                outputter.IncreaseIndent();
                base.WriteTypescript(outputter);
                outputter.DecreaseIndent();
                outputter.AppendLine("}");
                outputter.OutputBuilder.AppendLine();
            }
        }

        public override void Merge(ICSharpFileElement ele)
        {
            if (ele is CSharpClass)
            {
                var clsObj = ele as CSharpClass;
                foreach (var prop in clsObj.Children)
                {
                    this.Children.Add(prop);
                }
            }
        }
    }
}
