using netmockery;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


namespace UnitTests
{
    using Microsoft.CodeAnalysis.Scripting;
    using static TestUtils;

    public class TestDynamicResponse 
    {
        static public async Task<string> EvalAsync(string code, RequestInfo requestInfo = null, DateTime? now = null, string[] extraReferences = null)
        {
            if (requestInfo == null)
            {
                requestInfo = new RequestInfo();
            }
            if (now != null)
            {
                requestInfo.SetStaticNow(now.Value);
            }
            var drc = new LiteralDynamicResponseCreator(code, new Endpoint("a", "b"));

            if (extraReferences != null)
            {
                foreach (var reference in extraReferences)
                {
                    drc.AddReference(reference);
                }
            }
            return await drc.GetBodyAsync(requestInfo);
        }

        [Fact]
        public async Task CanExecuteCode()
        {
            Assert.Equal("42", await EvalAsync("(40+2).ToString()"));
        }

        [Fact]
        public async Task GetNowWorks()
        {
            Assert.Equal("System.DateTime", await EvalAsync("GetNow().GetType().ToString()"));
        }

        [Fact]
        public async Task CompilationErrorsAreThrown()
        {
            //TODO: Create exception type for compilation error
            var e = await Assert.ThrowsAnyAsync<Exception>(
                () => EvalAsync("dlkj d")
            );

            Assert.Contains("; expected", e.ToString());
        }

        [Fact]
        public async Task CanAccessGlobalVariables()
        {
            Assert.Equal("foobar", await EvalAsync("RequestBody + RequestPath", new RequestInfo { RequestBody = "foo", RequestPath = "bar" }));
        }

        [Fact]
        public async Task CanAccessQueryString()
        {
            Assert.Equal("foobar?ama", await EvalAsync("RequestBody + RequestPath + QueryString", new RequestInfo { RequestBody = "foo", RequestPath = "bar", QueryString = "?ama" }));
        }

        [Fact]
        public async Task MultilineScript()
        {
            var code = @"var a = 2; var b = 3; (a + b).ToString()";
            Assert.Equal("5", await EvalAsync(code));
        }

        [Fact]
        public async Task MultilineScriptWithReturnStatement()
        {
            var code = @"var a = 2; var b = 3; return (a + b).ToString();";
            Assert.Equal("5", await EvalAsync(code));
        }


        [Fact]
        public async Task RuntimeErrorsAreThrown()
        {
            var ex = await Assert.ThrowsAsync<DivideByZeroException>(
                () => EvalAsync("var i = 0; (4 / i).ToString()")
            );
            Assert.NotNull(ex);
        }

#if NET462
        [Fact]
        public async Task RuntimeErrorsIncludeLineNumber()
        {
            var ex = await Assert.ThrowsAsync<DivideByZeroException>(
                () => EvalAsync("var i = 0; (4 / i).ToString()")
            );
            Assert.Contains("in :line 1", ex.StackTrace);
        }
#endif

        [Fact]
        public async Task UsingSystemLinq()
        {
            Assert.Equal("0123", await EvalAsync("using System.Linq; string.Join(\"\", Enumerable.Range(0, 4))"));
        }

        [Fact]
        public async Task ScriptCanUseTuples()
        {
            await EvalAsync("using System; var t = Tuple.Create(1, 2); return t.ToString();");
        }

        [Fact]
        public async Task UsingSystemIO()
        {
            Assert.Equal("File", await EvalAsync("typeof(System.IO.File).Name"));
        }

        [Fact]
        public async Task UsingSystemXmlLinq()
        {
            await AssertScriptCanIncludeUsingStatementAsync("System.Xml.Linq");
        }

        [Fact]
        public async Task UsingSystemXml()
        {
            await AssertScriptCanIncludeUsingStatementAsync("System.Xml");

            Assert.Equal("True", await EvalAsync("using System.Xml; var xnm = typeof(XmlNamespaceManager); return true.ToString();"));
        }

        [Fact]
        public async Task UsingSystemXmlXPath()
        {
            await AssertScriptCanIncludeUsingStatementAsync("System.Xml.XPath");
        }

        [Fact]
        public async Task UsingRegex()
        {
            await AssertScriptCanIncludeUsingStatementAsync("System.Text.RegularExpressions");
        }

        [Fact]
        public async Task UsingSystemDiagnosticsDebug()
        {
            await AssertScriptCanIncludeUsingStatementAsync("System.Diagnostics");
        }

        [Fact]
        public async Task UsingNewtonsoftJson()
        {
            await AssertScriptCanIncludeUsingStatementAsync("Newtonsoft.Json");
        }

        [Fact]
        public async Task UsingSystemDynamic()
        {
            await AssertScriptCanIncludeUsingStatementAsync("System.Dynamic");
        }

        [Fact]
        public async Task ScriptsCanUseDynamicKeyword()
        {
            Assert.Equal("abc", await EvalAsync(@"
using System.Dynamic;
dynamic a = new ExpandoObject();
a.Foobar = ""abc"";
return a.Foobar;
"));
        }

        [Fact]
        public async Task ScriptsCanCompareDynamic()
        {
            Assert.Equal("True", await EvalAsync(@"
dynamic a = ""abc"";
var b = ""abc"";
return (a == b).ToString();
"));
        }

        [Fact]
        public async Task ScriptsCanUseExpandoObject()
        {
            Assert.Equal("True", await EvalAsync(@"
using System.Dynamic;
var eo = new ExpandoObject();
return (eo != null).ToString();
"));
        }

        private async Task AssertScriptCanIncludeUsingStatementAsync(string namespaceName)
        {
            Assert.Equal("OK", await EvalAsync($"using {namespaceName}; return \"OK\";"));
        }

        [Fact]
        public async Task ScriptsCanSetParams()
        {
            var endpoint = new Endpoint("foo", "bar");
            endpoint.AddParameter(new EndpointParameter { Name = "param", Value = "abc" });

            Assert.Equal("abc", await EvalAsync("return GetParam(\"param\");", new RequestInfo { Endpoint = endpoint }));

            await EvalAsync("SetParam(\"param\", \"def\"); return \"\";", new RequestInfo { Endpoint = endpoint });

            Assert.Equal("def", await EvalAsync("return GetParam(\"param\");", new RequestInfo { Endpoint = endpoint }));
        }

        [Fact]
        public async Task ScriptsCanReferenceFrameworkAssemblies()
        {
            var asmRef = @"System.IO.Compression.ZipFile.dll";
            Assert.Equal(
                "System.IO.Compression.ZipFile", 
                await EvalAsync(
                    "using System.IO.Compression; return typeof(ZipFile).ToString();", 
                    extraReferences: new[] { asmRef }
                )
            );

            // net462: Reference extra by assembly name
        }

        [Fact]
        public async Task ScriptsCanHandleEndpointObjects()
        {
            var endpoint = new Endpoint("foo", "bar");

            endpoint.ScriptObjects["obj"] = new Dictionary<string, string>();
            var obj = (Dictionary <string, string>) endpoint.ScriptObjects["obj"];

            obj["a"] = "b";
            Assert.Equal(
                "b", 
                await EvalAsync("using System.Collections.Generic; return ((Dictionary<string, string>)Endpoint.ScriptObjects[\"obj\"])[\"a\"];", new RequestInfo { Endpoint = endpoint })
            );

            obj["a"] = "c";
            Assert.Equal(
                "c",
                await EvalAsync("using System.Collections.Generic; return ((Dictionary<string, string>)Endpoint.ScriptObjects[\"obj\"])[\"a\"];", new RequestInfo { Endpoint = endpoint })
            );
        }

        [Fact]
        public async Task ScriptsCanSetResponseStatusCode()
        {
            var endpoint = new Endpoint("foo", "bar");
            var responseCreator = new LiteralDynamicResponseCreator("StatusCode = 102; return \"\";", endpoint);
            var response = new TestableHttpResponse();
            await responseCreator.CreateResponseAsync(new TestableHttpRequest("/", null), new byte[0], response, endpoint);

            Assert.Equal(102, (int) response.HttpStatusCode);
        }

        [Fact]
        public async Task ScriptsCanSetContentType()
        {
            var endpoint = new Endpoint("foo", "bar");
            var responseCreator = new LiteralDynamicResponseCreator("ContentType = \"image/png\"; return \"\";", endpoint);
            var response = new TestableHttpResponse();
            await responseCreator.CreateResponseAsync(new TestableHttpRequest("/", null), new byte[0], response, endpoint);

            Assert.Equal("image/png", response.ContentType);
        }

        [Fact]
        public async Task ScriptWithoutReturnValueThrowsException()
        {   
            var endpoint = new Endpoint("foo", "bar");
            var responseCreator = new LiteralDynamicResponseCreator("ContentType = \"image/png\";", endpoint);
            var response = new TestableHttpResponse();
            await Assert.ThrowsAsync(
                typeof(ArgumentNullException),
                async () => await responseCreator.CreateResponseAsync(new TestableHttpRequest("/", null), new byte[0], response, endpoint)
            );
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
        public async Task ScriptCanLoadAnotherScript()
        {
            var drc = new FileDynamicResponseCreator("a/main.csscript", new Endpoint("a", "b") { Directory = dc.DirectoryName });
            var body = await drc.GetBodyAsync(new RequestInfo());
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
        public async Task ScriptCanLoadAnotherScript()
        {
            var drc = new FileDynamicResponseCreator("a/main.csscript", new Endpoint("a", "b") { Directory = dc.DirectoryName });
            var body = await drc.GetBodyAsync(new RequestInfo());
            Assert.Equal("foo", body);
        }

        public void Dispose()
        {
            dc.Dispose();
        }
    }
}
