using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;

namespace UnitTests
{
    public class TestEndpoint
    {
        [Fact]
        public void EmptyEndpoint()
        {
            //var endpoint = new Endpoint();
        }

        [Fact]
        public void EndpointMatches()
        {
            var endpoint = new Endpoint("Mock service", "^/NHNPersonvern/");
            Assert.True(endpoint.Matches("/NHNPersonvern/"));
            Assert.False(endpoint.Matches("/Foobar/"));
        }

        [Fact]
        public void ResolveMoreThanOneMatcher()
        {
            var endpoint = new Endpoint("Mock service", "^/NHNPersonvern/");
            endpoint.Add(new RegexMatcher("foo"), new LiteralResponse("foobar"));
            endpoint.Add(new AnyMatcher(), new LiteralResponse("foobar"));

            bool singleMatch;
            var firstmatch = endpoint.Resolve(new Microsoft.AspNetCore.Http.PathString(""), new Microsoft.AspNetCore.Http.QueryString(""), "foo", null, out singleMatch);
            Assert.False(singleMatch);
            Assert.IsType(typeof(RegexMatcher), firstmatch.Item1);
        }

        [Fact]
        public void ResolveOnlyOne()
        {
            var endpoint = new Endpoint("Mock service", "^/NHNPersonvern/");
            endpoint.Add(new RegexMatcher("foo"), new LiteralResponse("foobar"));
            endpoint.Add(new AnyMatcher(), new LiteralResponse("foobar"));

            bool singleMatch;
            var firstmatch = endpoint.Resolve(new Microsoft.AspNetCore.Http.PathString(""), new Microsoft.AspNetCore.Http.QueryString(), "bar", null, out singleMatch);
            Assert.True(singleMatch);
            Assert.IsType(typeof(AnyMatcher), firstmatch.Item1);

        }

        [Fact]
        public void AddAfterAnyFails()
        {
            var endpoint = new Endpoint("Mock service", "^/NHNPersonvern/");
            endpoint.Add(new AnyMatcher(), new LiteralResponse("foobar"));

            Assert.Throws(typeof(ArgumentException), () => {
                endpoint.Add(new RegexMatcher("foo"), new LiteralResponse("foobar"));
            });
        }
    }
}
