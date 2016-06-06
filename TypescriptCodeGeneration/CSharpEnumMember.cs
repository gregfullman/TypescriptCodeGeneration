using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    public class CSharpEnumMember : CSharpFileElement
    {
        public object Value { get; set; }

        private string GetValueString()
        {
            string result = "";
            if(Value != null)
            {
                result = string.Format(" = {0}", Value.ToString());
            }
            return result;
        }

        public override void WriteTypescript(TsOutputer outputter)
        {
            base.GetTypescriptComment(outputter);
            outputter.AppendLine(this.DisplayText + GetValueString() + ",");
        }

        public override bool ShouldWrite
        {
            get
            {
                return true;
            }
        }
    }
}
