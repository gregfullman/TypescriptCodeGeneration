using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypescriptCodeGeneration;

namespace TSCodeGenUnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var task = TsCodeGenerator.GenerateCode(@"C:\Users\greg.fullman\Documents\Visual Studio 2015\Projects\ConsoleApplication1\ConsoleApplication1.sln",
                                                 null,
                                                 null);
            task.Wait();
            var result = task.Result;
            Assert.IsTrue(true);
        }
    }
}
