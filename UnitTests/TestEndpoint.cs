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
            endpoint.Add(new RegexMatcher("foo"), new LiteralResponse("foobar", new Endpoint("a", "b")));
            endpoint.Add(new AnyMatcher(), new LiteralResponse("foobar", new Endpoint("a", "b")));

            var firstmatch = endpoint.Resolve("GET", new Microsoft.AspNetCore.Http.PathString(""), new Microsoft.AspNetCore.Http.QueryString(""), "foo", null);
            Assert.False(firstmatch.SingleMatch);
            Assert.IsType<RegexMatcher>(firstmatch.RequestMatcher);
        }

        [Fact]
        public void ResolveOnlyOne()
        {
            var endpoint = new Endpoint("Mock service", "^/NHNPersonvern/");
            endpoint.Add(new RegexMatcher("foo"), new LiteralResponse("foobar", new Endpoint("a", "b")));
            endpoint.Add(new AnyMatcher(), new LiteralResponse("foobar", new Endpoint("a", "b")));

            var firstmatch = endpoint.Resolve("GET", new Microsoft.AspNetCore.Http.PathString(""), new Microsoft.AspNetCore.Http.QueryString(), "bar", null);
            Assert.True(firstmatch.SingleMatch);
            Assert.IsType<AnyMatcher>(firstmatch.RequestMatcher);

        }

        [Fact]
        public void AddAfterAnyFails()
        {
            var endpoint = new Endpoint("Mock service", "^/NHNPersonvern/");
            endpoint.Add(new AnyMatcher(), new LiteralResponse("foobar", new Endpoint("a", "b")));

            Assert.Throws<ArgumentException>(() => {
                endpoint.Add(new RegexMatcher("foo"), new LiteralResponse("foobar", new Endpoint("a", "b")));
            });
        }
    }
}
