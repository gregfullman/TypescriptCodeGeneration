using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    public class CSharpEnum : CSharpFileElement
    {
        public override void WriteTypescript(TsOutputer outputter)
        {
            base.GetTypescriptComment(outputter);
            // TODO: const?
            outputter.AppendLine("enum " + DisplayText + " {");
            outputter.IncreaseIndent();
            base.WriteTypescript(outputter);
            // remove last comma
            int index = outputter.OutputBuilder.Length - 1;
            while (char.IsWhiteSpace(outputter.OutputBuilder[index]))
                index--;
            if (outputter.OutputBuilder[index] == ',')
                outputter.OutputBuilder.Remove(index, 1);

            outputter.DecreaseIndent();
            outputter.AppendLine("}");
        }
    }
}
