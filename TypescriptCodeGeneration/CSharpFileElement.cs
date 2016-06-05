using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;


namespace TypescriptCodeGeneration
{
    public interface ICSharpFileElement
    {
        string DisplayText { get; set; }
        void SetSummaryViaXml(string xmlString);
        string Summary { get; }
        void GetTypescriptComment(TsOutputer outputter);
        bool ShouldWrite { get; }
        void WriteTypescript(TsOutputer outputter);

        // TODO: not sure if this is needed
        ICSharpFileElement Parent { get; set; }

        IDictionary<string, ICSharpFileElement> Children { get; }

        void Merge(ICSharpFileElement ele);
    }


    public abstract class CSharpFileElement : ICSharpFileElement
    {
        public ICSharpFileElement Parent { get; set; }

        public string DisplayText { get; set; }

        private Dictionary<string, ICSharpFileElement> _children;
        public IDictionary<string, ICSharpFileElement> Children
        {
            get
            {
                if (_children == null)
                    _children = new Dictionary<string, ICSharpFileElement>();
                return _children;
            }
        }

        public virtual void SetSummaryViaXml(string xmlString)
        {
            // parse the xml
            if (!string.IsNullOrEmpty(xmlString))
            {
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.LoadXml(xmlString);
                    var summaryNode = xmlDoc.SelectSingleNode("member/summary");
                    if (summaryNode != null)
                    {
                        _summary = summaryNode.InnerText;
                    }
                }
                catch (XmlException xe)
                {
                }
            }
        }

        private string _summary;
        public string Summary
        {
            get
            {
                return _summary ?? "";
            }
        }

        public virtual void GetTypescriptComment(TsOutputer outputter)
        {
            if (string.IsNullOrEmpty(Summary))
                return;
            outputter.AppendLine("/** " + Summary.Trim() + " */");
        }

        public virtual bool ShouldWrite
        {
            get
            {
                bool hasContent = false;
                foreach (var child in Children)
                    hasContent |= child.Value.ShouldWrite;
                return hasContent;
            }
        }

        public virtual void Merge(ICSharpFileElement ele)
        {
        }

        public virtual void WriteTypescript(TsOutputer outputter)
        {
            foreach (var child in Children.Values)
                child.WriteTypescript(outputter);
        }
    }
}
