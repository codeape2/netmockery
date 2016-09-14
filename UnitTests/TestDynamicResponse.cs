using netmockery;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


namespace UnitTests
{
    public class TestDynamicResponse
    {
        static public string Eval(string code, RequestInfo requestInfo = null)
        {
            if (requestInfo == null)
            {
                requestInfo = new RequestInfo();
            }
            return new LiteralDynamicResponseCreator(code).GetBody(requestInfo);
            //return DynamicResponseCreatorBase.Evaluate(code, requestInfo);
        }
        [Fact]
        public void CanExecuteCode()
        {
            Assert.Equal("42", Eval("(40+2).ToString()"));
        }

        [Fact]
        public void CompilationErrorsAreThrown()
        {
            var e = Assert.ThrowsAny<AggregateException>(
                () => Eval("dlkj d")
            );

            Assert.Contains("; expected", e.InnerException.ToString());
        }

        [Fact]
        public void CanAccessGlobalVariables()
        {
            Assert.Equal("foobar", Eval("RequestBody + RequestPath", new RequestInfo { RequestBody = "foo", RequestPath = "bar" }));
        }

        [Fact]
        public void MultilineScript()
        {
            var code = @"var a = 2; var b = 3; (a + b).ToString()";
            Assert.Equal("5", Eval(code));
        }

        [Fact]
        public void MultilineScriptWithReturnStatement()
        {
            var code = @"var a = 2; var b = 3; return (a + b).ToString();";
            Assert.Equal("5", Eval(code));
        }


        [Fact]
        public void RuntimeErrorsAreThrown()
        {
            var ex = Assert.Throws<AggregateException>(
                () => Eval("var i = 0; (4 / i).ToString()")
            );
            Assert.Equal(1, ex.InnerExceptions.Count);
            Assert.IsType<DivideByZeroException>(ex.InnerExceptions[0]);
            Assert.Contains("in :line 1", ex.InnerException.StackTrace);
        }

        [Fact]
        public void UsingSystemLinq()
        {
            Assert.Equal("0123", Eval("using System.Linq; string.Join(\"\", Enumerable.Range(0, 4))"));
        }

        [Fact]
        public void UsingSystemXmlLinq()
        {
            Assert.Equal("OK", Eval("using System.Xml.Linq; return \"OK\";"));
        }
    }

    public class TestCheckScriptModifications : IDisposable
    {
        private DirectoryCreator dc;
        public TestCheckScriptModifications()
        {
            dc = new DirectoryCreator();
            dc.AddFile("abc.txt", "abc");
        }

        public void Dispose()
        {
            dc.Dispose();
        }
        [Fact]
        public void ReplacementWorks()
        {
            var code = DynamicResponseCreatorBase.CreateCorrectPathsInLoadStatements("#load \"foo.csscript\"", "C:\\dev");
            Assert.Equal("#load \"C:\\dev\\foo.csscript\"", code);
        }

        [Fact]
        public void IncludesWork()
        {
            var code = DynamicResponseCreatorBase.ExecuteIncludes("aaa; #include \"abc.txt\"", dc.DirectoryName);
            Assert.Equal("aaa; abc", code);
        }
    }

    public class TestLoadScript : IDisposable
    {
        DirectoryCreator dc = new DirectoryCreator();

        public TestLoadScript()
        {
            dc.AddFile("a\\main.csscript", "#load \"..\\b\\lib.csscript\"\nreturn f;");
            dc.AddFile("b\\lib.csscript", "var f = \"foo\";");
        }

        [Fact]
        public void ScriptCanLoadAnotherScript()
        {
            var drc = new FileDynamicResponseCreator(Path.Combine(dc.DirectoryName, "a\\main.csscript"));
            var body = drc.GetBody(new RequestInfo());
            Assert.Equal("foo", body);
        }

        public void Dispose()
        {
            dc.Dispose();
        }
    }

    public class TestLoadScriptRelativePath : IDisposable
    {
        DirectoryCreator dc = new DirectoryCreator("_tmpscriptdir");

        public TestLoadScriptRelativePath()
        {
            dc.AddFile("a\\main.csscript", "#load \"..\\b\\lib.csscript\"\nreturn f;");
            dc.AddFile("b\\lib.csscript", "var f = \"foo\";");
        }

        [Fact]
        public void ScriptCanLoadAnotherScript()
        {
            var drc = new FileDynamicResponseCreator(Path.Combine(dc.DirectoryName, "a\\main.csscript"));
            var body = drc.GetBody(new RequestInfo());
            Assert.Equal("foo", body);
        }

        public void Dispose()
        {
            dc.Dispose();
        }
    }
}
