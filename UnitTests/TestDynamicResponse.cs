using netmockery;
using System;
using System.Collections.Generic;
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
            Assert.ThrowsAny<Exception>(
                () => Eval("dlkj d")
            );
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
}
