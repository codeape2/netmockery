using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;

namespace UnitTests
{
    public class TestMatchHttpMethod
    {
        [Fact]
        public void MatchAnyMethod()
        {
            var matcher = new JSONRequestMatcher().CreateRequestMatcher() as AnyMatcher;
            Assert.NotNull(matcher);
            Assert.True(matcher.MatchesHttpMethod("POST"));
            Assert.True(matcher.MatchesHttpMethod("GET"));
            Assert.True(matcher.MatchesHttpMethod("PUT"));
            Assert.True(matcher.MatchesHttpMethod("HEAD"));
        }

        [Fact]
        public void MatchOnlySpecific()
        {
            var jsonMatcher = new JSONRequestMatcher
            {
                methods = "POST PUT"
            };

            var matcher = jsonMatcher.CreateRequestMatcher() as AnyMatcher;
            Assert.NotNull(matcher);
            Assert.True(matcher.MatchesHttpMethod("POST"));
            Assert.False(matcher.MatchesHttpMethod("GET"));
            Assert.True(matcher.MatchesHttpMethod("PUT"));
            Assert.False(matcher.MatchesHttpMethod("HEAD"));
        }

        [Fact]
        public void IsCaseInsensitive()
        {
            var jsonMatcher = new JSONRequestMatcher
            {
                methods = "poST Put"
            };

            var matcher = jsonMatcher.CreateRequestMatcher() as AnyMatcher;
            Assert.NotNull(matcher);
            Assert.True(matcher.MatchesHttpMethod("post"));
            Assert.True(matcher.MatchesHttpMethod("PUT"));
        }

        [Fact]
        public void RegexMatcher()
        {
            var matcher = (new JSONRequestMatcher { methods = "post", regex = "foobar" }).CreateRequestMatcher() as RegexMatcher;
            Assert.NotNull(matcher);
            Assert.True(matcher.MatchesHttpMethod("post"));
            Assert.False(matcher.MatchesHttpMethod("get"));
        }

        [Fact]
        public void TestCaseDefaultMethodIsGet()
        {
            var testCase = (new JSONTest { requestpath = "/foo/bar" }).CreateTestCase(".");
            Assert.Equal("GET", testCase.Method);
        }

        [Fact]
        public void CanSpecifyMethodInJson()
        {
            var testCase = (new JSONTest { requestpath = "/foo/bar", method = "POST" }).CreateTestCase(".");
            Assert.Equal("POST", testCase.Method);
        }

        [Fact]
        public async Task Foo()
        {
            var endpoint = (new JSONEndpoint
            {
                name = "endpoint",
                pathregex = "/",
                responses = new[]
                {
                    new JSONResponse { match = new JSONRequestMatcher { methods = "POST" }, literal = "Response from POST" },
                    new JSONResponse { match = new JSONRequestMatcher {methods ="GET" }, literal = "Response from GET" }
                }
            }).CreateEndpoint(".", null);
            Assert.NotNull(endpoint);
            Assert.Equal(2, endpoint.Responses.Count());

            var getTestCase = new NetmockeryTestCase { Method = "GET", RequestPath = "/", ExpectedResponseBody = "Response from GET" };
            Assert.True((await getTestCase.ExecuteAsync(EndpointCollection.WithEndpoints(endpoint))).OK);

            var postTestCase = new NetmockeryTestCase { Method = "POST", RequestPath = "/", ExpectedResponseBody = "Response from POST" };
            Assert.True((await postTestCase.ExecuteAsync(EndpointCollection.WithEndpoints(endpoint))).OK);
        }
    }
}
