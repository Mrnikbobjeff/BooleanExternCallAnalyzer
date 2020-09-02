using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using BooleanPInvokeAnalyzer;
using System.Runtime.InteropServices;

namespace BooleanPInvokeAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void EmptyText_NoDiagnostic()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void BoolParameter_SingleDiagnostic()
        {
            var test = @"
    using System;
    using System.Runtime.InteropServices;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            [DllImport(""kernel32.dll"")]
            public static extern bool Test(bool flag);
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "BooleanPInvokeAnalyzer",
                Message = String.Format("Mark bool parameter in '{0}' with MarshalAsAttribute", "ConsoleApplication1.TypeName.Test(bool)"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 9, 39)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }


        [TestMethod]
        public void BoolParameter_SingleFix()
        {
            var test = @"
using System;
using System.Runtime.InteropServices;
namespace ConsoleApplication1
{
    class TypeName
    {
        [DllImport(""kernel32.dll"")]
        public static extern bool Test(bool flag, [MarshalAs(UnmanagedType.Bool)] bool flag2);
    }
}";

            var fixtest = @"
using System;
using System.Runtime.InteropServices;
namespace ConsoleApplication1
{
    class TypeName
    {
        [DllImport(""kernel32.dll"")]
        public static extern bool Test([MarshalAsAttribute(UnmanagedType.Bool)] bool flag, [MarshalAs(UnmanagedType.Bool)] bool flag2);
    }
}";
            
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new BooleanPInvokeAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new BooleanPInvokeAnalyzerAnalyzer();
        }
    }
}
