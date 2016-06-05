using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    public class TsOutputer
    {

        
        private StringBuilder output;
        public StringBuilder OutputBuilder
        {
            get { return output; }
        }

        private int currentIndentLength = 0;
        private static string Space = "\t";


        public TsOutputer()
        {
            output = new StringBuilder();
        }

        public void AppendLine(string line)
        {
            output.AppendLine(String.Concat(Enumerable.Repeat(Space, currentIndentLength)) + line);
        }

        public void IncreaseIndent() { currentIndentLength++; }

        public void DecreaseIndent() { currentIndentLength--; }

        public void ResetIndent() { currentIndentLength=0; }


        public string GetContent() { return this.output.ToString(); }

    }
}
