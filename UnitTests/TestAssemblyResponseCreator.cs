using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;


namespace UnitTests
{
    public class TestAssemblyResponseCreator
    {
        [Fact]
        public void WorksGivenAssemblyObject()
        {
            var responseCreator = new AssemblyResponseCreator(new Endpoint("a", "b")) {
                Assembly = this.GetType().Assembly,
                ClassName = "UnitTests.MyResponseCreator",
                MethodName = "GenerateIt"
            };
            Assert.Equal("Hello, foo!", responseCreator.GetBody(new RequestInfo { RequestPath = "foo" }));
        }

        [Fact]
        public void WorksGivenAssemblyName()
        {
            var responseCreator = new AssemblyResponseCreator(new Endpoint("a", "b"))
            {
                AssemblyFilename = this.GetType().Assembly.Location,
                ClassName = "UnitTests.MyResponseCreator",
                MethodName = "GenerateIt"
            };
            Assert.Equal("Hello, foo!", responseCreator.GetBody(new RequestInfo { RequestPath = "foo" }));
        }
    }

    public static class MyResponseCreator
    {
        public static string GenerateIt(string requestPath, string requestBody)
        {
            return $"Hello, {requestPath}!";
        }
    }
}
