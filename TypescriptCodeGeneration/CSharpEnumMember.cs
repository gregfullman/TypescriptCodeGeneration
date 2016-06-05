using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    public class CSharpEnumMember : CSharpFileElement
    {
        public override void WriteTypescript(TsOutputer outputter)
        {
            base.GetTypescriptComment(outputter);
            outputter.AppendLine(this.DisplayText + ",");
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
