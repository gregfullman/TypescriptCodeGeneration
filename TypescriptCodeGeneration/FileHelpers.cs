using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptCodeGeneration
{
    public enum FileStatus
    {
        Invalid,
        Added,
        Updated,
        Unchanged
    }

    public static class FileHelpers
    {
        static char[] pathSplit = { '/', '\\' };

        public async static Task<TsCodeGenerationResult> WriteAllTextRetry(string fileName, string contents, Func<string, bool> ensureWriteAccess, bool withBOM = true)
        {
            TsCodeGenerationResult result = new TsCodeGenerationResult { Path = fileName, Status = FileStatus.Invalid };
            if (!string.IsNullOrEmpty(fileName))
            {
                int retryCount = 500;
                FileStatus returnStatus = FileStatus.Invalid;
                try
                {
                    var dir = Path.GetDirectoryName(fileName);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                    }
                    if (!File.Exists(fileName))
                    {
                        result.Status = FileStatus.Added;
                    }
                    else
                    {
                        // Check to see if the file should be changed
                        if (!CompareContentToExistingFile(contents, fileName))
                        {
                            result.Status = FileStatus.Updated;
                        }
                        else
                        {
                            result.Status = FileStatus.Unchanged;
                        }
                    }
                    if(result.Status == FileStatus.Added || result.Status == FileStatus.Updated)
                        File.WriteAllText(fileName, contents, new UTF8Encoding(withBOM));
                }
                catch (IOException ioe)
                {
                    result.Status = FileStatus.Unchanged;
                    result.ErrorMessage = string.Format("Unable to write to file: {0}, {1}", fileName, ioe.Message);
                }
                catch(UnauthorizedAccessException)
                {
                    try
                    {
                        if (ensureWriteAccess != null && ensureWriteAccess(fileName))
                        {
                            File.WriteAllText(fileName, contents, new UTF8Encoding(withBOM));
                            result.Status = FileStatus.Updated;
                        }
                        else
                        {
                            result.Status = FileStatus.Unchanged;
                            result.ErrorMessage = string.Format("Unable to write to file: {0}", fileName);
                        }
                    }
                    catch(IOException ioee)
                    {
                        result.Status = FileStatus.Unchanged;
                        result.ErrorMessage = string.Format("Unable to write to file (after attempting ensure write): {0}, {1}", fileName, ioee.Message);
                    }
                }
            }
            return result;
        }

        private static bool CompareContentToExistingFile(string content, string existingFile)
        {
            var existingContent = File.ReadAllText(existingFile);
            return content == existingContent;
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
