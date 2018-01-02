using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypescriptCodeGeneration;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace TSCodeGenUnitTests
{
    [TestClass]
    public class UnitTest1
    {
        private TestTypescriptTargetProvider _tgtProvider;

        public UnitTest1()
        {
            _tgtProvider = new TestTypescriptTargetProvider();
        }

        [TestMethod]
        public void TestMethod1()
        {
            var task = TsCodeGenerator.GenerateCode(@"C:\Users\greg.fullman\Documents\Visual Studio 2015\Projects\ConsoleApplication4\ConsoleApplication4.sln",
                                                 null,
                                                 null,
                                                 true);
            task.Wait();
            var result = task.Result;
            Assert.IsTrue(true);
        }
    }

    public class TestTypescriptTargetProvider : ITypescriptGeneratorTargetProvider
    {
        private string GetTargetProjectDirectory(string sourceProject)
        {
            string result = null;
            var dirName = Path.GetDirectoryName(sourceProject);
            var projName = Path.GetFileName(dirName);

            string[] projPieces = projName.Split(new[] { '.' });

            if (projPieces.Length > 4 &&
               projPieces[3].Equals("Services", StringComparison.OrdinalIgnoreCase))
            {
                // check to see if there's a corresponding UI project that will match up with this services project
                string uiProj = string.Format("{0}.UI.Mvc.{1}", string.Join(".", projPieces.Take(3)), projPieces[projPieces.Length - 1]);
                var uiProjDirName = dirName.Replace(projName, uiProj);
                if (Directory.Exists(uiProjDirName))
                {
                    result = uiProjDirName;
                }
            }

            if (result == null)
            {
                // going to the general UI project
                string uiProj = string.Format("{0}.UI", string.Join(".", projPieces.Take(3)));
                var uiProjDirName = dirName.Replace(projName, uiProj);
                if (Directory.Exists(uiProjDirName))
                {
                    result = uiProjDirName;
                }
            }

            return result;
        }

        public string GetDefaultTargetReferencesFilename(string containingProject)
        {
            var tgtDir = GetTargetProjectDirectory(containingProject);
            if(tgtDir != null)
            {
                return Path.Combine(tgtDir, "Scripts", "_references.d.ts");
            }
            return null;
        }

        public string GetTargetFilename(string namespaceStr, string containingProject)
        {
            var tgtDir = GetTargetProjectDirectory(containingProject);
            if (tgtDir != null)
            {
                return Path.Combine(tgtDir, "Scripts", string.Format("{0}.d.ts", namespaceStr));
            }
            return null;
        }

        public bool EnsureFileIsWritable(string filename)
        {
            return false;
        }
    }
}
