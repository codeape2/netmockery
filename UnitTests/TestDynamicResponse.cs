﻿using netmockery;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


namespace UnitTests
{
    using static TestUtils;

    public class TestDynamicResponse 
    {
        static public string Eval(string code, RequestInfo requestInfo = null, DateTime? now = null)
        {
            if (requestInfo == null)
            {
                requestInfo = new RequestInfo();
            }
            if (now != null)
            {
                requestInfo.SetStaticNow(now.Value);
            }
            return new LiteralDynamicResponseCreator(code, new Endpoint("a", "b")).GetBody(requestInfo);
        }

        [Fact]
        public void CanExecuteCode()
        {
            Assert.Equal("42", Eval("(40+2).ToString()"));
        }

        [Fact]
        public void GetNowWorks()
        {
            Assert.Equal("System.DateTime", Eval("GetNow().GetType().ToString()"));
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
        public void CanAccessQueryString()
        {
            Assert.Equal("foobar?ama", Eval("RequestBody + RequestPath + QueryString", new RequestInfo { RequestBody = "foo", RequestPath = "bar", QueryString = "?ama" }));
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
        }


        [Fact(Skip = "Does not work on DotNetCore")]
        public void RuntimeErrorsIncludeLineNumber()
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
        public void UsingSystemIO()
        {
            Assert.Equal("File", Eval("typeof(System.IO.File).Name"));
        }

        [Fact]
        public void UsingSystemXmlLinq()
        {
            Assert.Equal("OK", Eval("using System.Xml.Linq; return \"OK\";"));
        }

        [Fact]
        public void UsingSystemDiagnosticsDebug()
        {
            Assert.Equal("OK", Eval("using System.Diagnostics; Debug.Assert(true); return \"OK\";"));
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
            var rootedPath = RootedPath("d", "foobar");
            var code = DynamicResponseCreatorBase.CreateCorrectPathsInLoadStatements("#load \"foo.csscript\"", rootedPath);
            if (IsWindows)
            {
                Assert.Equal($"#load \"d:\\foobar\\foo.csscript\"", code);
            }
            else
            {
                Assert.Equal($"#load \"/d/foobar/foo.csscript\"", code);
            }            
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
            dc.AddFile("a/main.csscript", "#load \"../b/lib.csscript\"\nreturn f;");
            dc.AddFile("b/lib.csscript", "var f = \"foo\";");
        }

        [Fact]
        public void ScriptCanLoadAnotherScript()
        {
            var drc = new FileDynamicResponseCreator("a/main.csscript", new Endpoint("a", "b") { Directory = dc.DirectoryName });
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
            dc.AddFile("a/main.csscript", "#load \"../b/lib.csscript\"\nreturn f;");
            dc.AddFile("b/lib.csscript", "var f = \"foo\";");
        }

        [Fact]
        public void ScriptCanLoadAnotherScript()
        {
            var drc = new FileDynamicResponseCreator("a/main.csscript", new Endpoint("a", "b") { Directory = dc.DirectoryName });
            var body = drc.GetBody(new RequestInfo());
            Assert.Equal("foo", body);
        }

        public void Dispose()
        {
            dc.Dispose();
        }
    }
}
