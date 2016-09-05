using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using netmockery;


namespace UnitTests
{
    public class TestEndpointCollection
    {
        EndpointCollection endpointCollection = new EndpointCollection();
        public TestEndpointCollection()
        {
            endpointCollection.Add(new Endpoint("foo", "^/foo/"));
            endpointCollection.Add(new Endpoint("bar", "^/bar"));
            endpointCollection.Add(new Endpoint("barista", "^/barista"));
        }

        [Fact]
        public void NamesMustBeUnique()
        {
            Assert.Throws<ArgumentException>(() => endpointCollection.Add(new Endpoint("foo", "bar")));
        }

        [Fact]
        public void WorksAsExpected()
        {
            Assert.Equal("foo", endpointCollection.Resolve("/foo/").Name);
        }

        [Fact]

        public void MoreThanOne()
        {
            Assert.Throws<InvalidOperationException>(() => endpointCollection.Resolve("/barista"));
        }

        [Fact]
        public void Zero()
        {
            Assert.Null(endpointCollection.Resolve("/kjlkj/"));
        }
    }

}
