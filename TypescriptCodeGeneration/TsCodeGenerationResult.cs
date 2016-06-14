using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    public class TsCodeGenerationResult
    {
        public string Path { get; set; }
        public FileStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
