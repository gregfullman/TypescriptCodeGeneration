using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    public static class FileHelpers
    {
        static char[] pathSplit = { '/', '\\' };

        public async static Task WriteAllTextRetry(string fileName, string contents, bool withBOM = true)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            int retryCount = 500;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                File.WriteAllText(fileName, contents, new UTF8Encoding(withBOM));
            }
            catch (IOException)
            {
                // TODO: alert
            }
        }


        public static string RelativePath(string absolutePath, string relativeTo)
        {
            relativeTo = relativeTo.Replace("\\/", "\\");

            string[] absDirs = absolutePath.Split(pathSplit);
            string[] relDirs = relativeTo.Split(pathSplit);

            // Get the shortest of the two paths
            int len = Math.Min(absDirs.Length, relDirs.Length);

            // Use to determine where in the loop we exited
            int lastCommonRoot = -1;
            int index;

            // Find common root
            for (index = 0; index < len; index++)
            {
                if (absDirs[index].Equals(relDirs[index], StringComparison.OrdinalIgnoreCase)) lastCommonRoot = index;
                else break;
            }

            // If we didn't find a common prefix then throw
            if (lastCommonRoot == -1)
            {
                return relativeTo;
            }

            // Build up the relative path
            StringBuilder relativePath = new StringBuilder();

            // Add on the ..
            for (index = lastCommonRoot + 2; index < absDirs.Length; index++)
            {
                if (absDirs[index].Length > 0) relativePath.Append("..\\");
            }

            // Add on the folders
            for (index = lastCommonRoot + 1; index < relDirs.Length - 1; index++)
            {
                relativePath.Append(relDirs[index] + "\\");
            }
            relativePath.Append(relDirs[relDirs.Length - 1]);

            return relativePath.Replace('\\', '/').ToString();
        }
    }
}
